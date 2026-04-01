using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore;

/// <summary>
/// Inertia.js middleware. Handles versioning, shared props, root view resolution,
/// 302→303 conversion, empty response handling, and fragment redirects.
/// </summary>
internal sealed class InertiaMiddleware : IMiddleware
{
    private readonly InertiaOptions _options;

    public InertiaMiddleware(IOptions<InertiaOptions> options)
    {
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Register OnStarting to set Vary header — handles body-writing responses
        // where HasStarted becomes true before post-processing
        context.Response.OnStarting(static state =>
        {
            ((HttpResponse)state).Headers.Vary = InertiaHeaderNames.Inertia;
            return Task.CompletedTask;
        }, context.Response);

        var factory = GetFactory(context);
        var isInertia = IsInertiaRequest(context.Request);

        // --- PRE-PIPELINE SETUP ---

        // 1. Set version from delegate
        if (_options.VersionProvider is { } vp)
            factory.SetVersion(vp(context));

        // 2. Share props from delegate
        if (_options.SharedPropsProvider is { } spp)
            factory.Share(spp(context, context.RequestServices));

        // 2b. Share validation errors from delegate (wrapped as AlwaysProp for partial-reload inclusion)
        if (_options.ValidationErrorProvider is { } vep)
        {
            var errorBag = context.Request.Headers[InertiaHeaderNames.ErrorBag].FirstOrDefault();
            factory.Share("errors", Prop.Always<IDictionary<string, object?>>(() => vep(context, errorBag)));
        }

        // 2c. Share once-props from delegate
        if (_options.SharedOncePropsProvider is { } sopp)
        {
            foreach (var (key, value) in sopp(context, context.RequestServices))
            {
                if (value is IOnceable)
                    factory.Share(key, value);
                else if (value is Delegate d)
                    factory.Share(key, new OnceProp<object?>(() => d.DynamicInvoke()));
                else
                    factory.Share(key, new OnceProp<object?>(() => value));
            }
        }

        // 3. Set root view from delegate
        if (_options.RootViewProvider is { } rvp)
            factory.SetRootView(rvp(context));

        // 3b. Apply static SSR path exclusions from config
        if (_options.SsrExcludePaths is { Length: > 0 } ssrExcludePaths)
        {
            var ssrState = context.RequestServices.GetService<SsrState>();
            ssrState?.ExcludePaths(ssrExcludePaths);
        }

        // 4. Version mismatch — GET + Inertia only — SHORT CIRCUIT
        if (isInertia && HttpMethods.IsGet(context.Request.Method))
        {
            var clientVersion = context.Request.Headers[InertiaHeaderNames.Version].FirstOrDefault() ?? "";
            if (clientVersion != factory.GetVersion())
            {
                factory.ReflashAllTempData();
                SetVary(context.Response);
                await HandleVersionChange(context);
                return;
            }
        }

        // --- EXECUTE DOWNSTREAM ---
        await next(context);

        // --- POST-PIPELINE ---

        // Set Vary on responses where body hasn't been written (redirects, empty)
        // For body responses, OnStarting callback already handles it
        if (!context.Response.HasStarted)
            SetVary(context.Response);

        var statusCode = context.Response.StatusCode;
        var isRedirect = statusCode is >= 300 and < 400;

        // 5. Reflash flash data on redirect
        if (isRedirect)
            factory.ReflashAllTempData();

        // 6. Early exit for non-Inertia requests
        if (!isInertia)
            return;

        // 7. Empty response (2xx, no body written)
        if (statusCode is >= 200 and < 300 && !context.Response.HasStarted)
        {
            await HandleEmptyResponse(context);
            return;
        }

        // 8. 302→303 for state-changing methods (PUT, PATCH, DELETE)
        if (statusCode == 302 && IsStateChangingMethod(context.Request.Method))
            context.Response.StatusCode = 303;

        // 9. Fragment redirect (redirect + fragment + not prefetch)
        if (isRedirect && HasFragment(context.Response) && !IsPrefetch(context.Request))
            HandleFragmentRedirect(context);
    }

    private static InertiaFactory GetFactory(HttpContext ctx)
        => (InertiaFactory)ctx.RequestServices.GetRequiredService<IInertia>();

    private static bool IsInertiaRequest(HttpRequest req)
        => req.IsInertia();

    private static bool IsStateChangingMethod(string method)
        => HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    private static bool IsPrefetch(HttpRequest req)
        => req.Headers["Purpose"].FirstOrDefault() == "prefetch"
        || req.Headers["Sec-Purpose"].FirstOrDefault() == "prefetch";

    private static bool HasFragment(HttpResponse resp)
        => resp.Headers.Location.FirstOrDefault()?.Contains('#') == true;

    private static void SetVary(HttpResponse response)
        => response.Headers.Vary= InertiaHeaderNames.Inertia;

    private async Task HandleVersionChange(HttpContext ctx)
    {
        if (_options.OnVersionChange is { } handler)
        {
            var result = handler(ctx);
            await result.ExecuteAsync(ctx);
        }
        else
        {
            // Default: 409 Conflict + X-Inertia-Location = current URL
            var url = $"{ctx.Request.Path}{ctx.Request.QueryString}";
            ctx.Response.StatusCode = StatusCodes.Status409Conflict;
            ctx.Response.Headers[InertiaHeaderNames.Location] = url;
        }
    }

    private async Task HandleEmptyResponse(HttpContext ctx)
    {
        if (_options.OnEmptyResponse is { } handler)
        {
            var result = handler(ctx);
            await result.ExecuteAsync(ctx);
        }
        else
        {
            // Default: redirect back to referer (matching PHP), or 204 if no referer
            var referer = ctx.Request.Headers.Referer.FirstOrDefault();
            if (referer is not null)
            {
                ctx.Response.StatusCode = StatusCodes.Status302Found;
                ctx.Response.Headers.Location = referer;
            }
            else
            {
                ctx.Response.StatusCode = StatusCodes.Status302Found;
                ctx.Response.Headers.Location = "/";
            }
        }
    }

    private static void HandleFragmentRedirect(HttpContext ctx)
    {
        var location = ctx.Response.Headers.Location.FirstOrDefault();
        ctx.Response.StatusCode = StatusCodes.Status409Conflict;
        ctx.Response.Headers.Remove("Location");
        ctx.Response.Headers[InertiaHeaderNames.Redirect] = location;
    }
}
