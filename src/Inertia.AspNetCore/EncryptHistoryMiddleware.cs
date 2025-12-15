using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Middleware that enables encryption of the browser history state.
/// This provides additional security for sensitive data in Inertia responses.
/// </summary>
/// <remarks>
/// When enabled, this middleware calls IInertia.EncryptHistory() to mark the
/// response for history encryption. The client-side Inertia library will then
/// encrypt the history state before storing it in the browser.
/// </remarks>
public class EncryptHistoryMiddleware : IMiddleware
{
    private readonly Core.IInertia _inertia;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptHistoryMiddleware"/> class.
    /// </summary>
    /// <param name="inertia">The Inertia factory instance.</param>
    public EncryptHistoryMiddleware(Core.IInertia inertia)
    {
        _inertia = inertia;
    }

    /// <summary>
    /// Processes the HTTP request and enables history encryption.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _inertia.EncryptHistory();
        await next(context);
    }
}
