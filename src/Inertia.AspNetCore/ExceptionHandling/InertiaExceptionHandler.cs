using System.Net.Http;
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

        if (httpContext.Response.HasStarted)
            return false;

        var statusCode = DeriveStatusCode(exception);
        if (statusCode < 400 || statusCode > 599)
            statusCode = StatusCodes.Status500InternalServerError;

        var context = new InertiaExceptionContext
        {
            Exception = exception,
            HttpContext = httpContext,
            StatusCode = statusCode,
        };

        InertiaExceptionResult? result;
        try
        {
            result = _options.ExceptionHandler(context);
        }
        catch
        {
            return false;
        }

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

            httpContext.Response.StatusCode = statusCode;
            var response = inertia.Render(result.Component, result.Props ?? new Dictionary<string, object?>());
            await response.ExecuteAsync(httpContext).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    private static int DeriveStatusCode(Exception exception) => exception switch
    {
        BadHttpRequestException e => e.StatusCode,
        InertiaHttpException e => e.StatusCode,
        HttpRequestException { StatusCode: { } statusCode } => (int)statusCode,
        _ => StatusCodes.Status500InternalServerError,
    };
}
