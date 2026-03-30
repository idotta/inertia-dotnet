using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore;

/// <summary>
/// Handles exceptions by rendering Inertia error pages when configured.
/// Implements <see cref="IExceptionHandler"/> for use with <c>app.UseExceptionHandler()</c>.
/// </summary>
internal sealed class InertiaExceptionHandler : IExceptionHandler
{
    private readonly InertiaOptions _options;

    public InertiaExceptionHandler(IOptions<InertiaOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (_options.ExceptionHandler is null)
            return false;

        var statusCode = DeriveStatusCode(exception);

        var context = new InertiaExceptionContext
        {
            Exception = exception,
            HttpContext = httpContext,
            StatusCode = statusCode,
        };

        var result = _options.ExceptionHandler(context);
        if (result is null)
            return false;

        if (result.RedirectUrl is not null)
        {
            httpContext.Response.Redirect(result.RedirectUrl);
            return true;
        }

        if (result.Component is not null)
        {
            var inertia = httpContext.RequestServices.GetRequiredService<IInertia>();

            if (result.IncludeSharedData && _options.SharedPropsProvider is { } spp)
                inertia.Share(spp(httpContext, httpContext.RequestServices));

            if (result.CustomRootView is not null && inertia is InertiaFactory factory)
                factory.SetRootView(result.CustomRootView);

            var response = inertia.Render(result.Component, result.Props ?? new Dictionary<string, object?>());
            await response.ExecuteAsync(httpContext);
            httpContext.Response.StatusCode = statusCode;
            return true;
        }

        return false;
    }

    private static int DeriveStatusCode(Exception exception)
    {
        return exception is BadHttpRequestException e ? e.StatusCode : StatusCodes.Status500InternalServerError;
    }
}
