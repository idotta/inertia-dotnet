using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inertia.AspNetCore;

/// <summary>
/// An action/endpoint result that redirects to the previous URL.
/// Implements both <see cref="IActionResult"/> (MVC) and <see cref="IResult"/> (minimal APIs).
/// </summary>
public sealed class InertiaBackResult : IActionResult, IResult
{
    /// <summary>The resolved redirect URL.</summary>
    public string Url { get; }

    /// <summary>The HTTP status code for the redirect.</summary>
    public int StatusCode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaBackResult"/>.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <param name="statusCode">The HTTP redirect status code.</param>
    public InertiaBackResult(string url, int statusCode = 302)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        Url = url;
        StatusCode = statusCode;
    }

    /// <inheritdoc />
    public Task ExecuteResultAsync(ActionContext context)
        => Execute(context.HttpContext);

    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
        => Execute(httpContext);

    private Task Execute(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCode;
        httpContext.Response.Headers.Location = Url;
        return Task.CompletedTask;
    }
}
