using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inertia.AspNetCore;

/// <summary>
/// An action/endpoint result that forces the client to do an external full-page visit.
/// For Inertia requests: returns 409 with X-Inertia-Location header.
/// For non-Inertia requests: returns a standard 302 redirect.
/// </summary>
public sealed class InertiaLocationResult : IActionResult, IResult
{
    /// <summary>The target URL for the external redirect.</summary>
    public string Url { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaLocationResult"/> with the specified URL.
    /// </summary>
    /// <param name="url">The URL to redirect to. Must not be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is null or empty.</exception>
    public InertiaLocationResult(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        Url = url;
    }

    /// <inheritdoc />
    public Task ExecuteResultAsync(ActionContext context)
        => Execute(context.HttpContext);

    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
        => Execute(httpContext);

    private Task Execute(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.ContainsKey(InertiaHeaderNames.Inertia))
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            httpContext.Response.Headers[InertiaHeaderNames.Location] = Url;
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status302Found;
            httpContext.Response.Headers.Location = Url;
        }

        return Task.CompletedTask;
    }
}
