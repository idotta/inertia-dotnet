using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.AspNetCore;

/// <summary>
/// Middleware that enables history encryption for all Inertia responses.
/// Register before <see cref="InertiaMiddleware"/> in the pipeline.
/// </summary>
internal sealed class EncryptHistoryMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var inertia = context.RequestServices.GetRequiredService<IInertia>();
        inertia.EncryptHistory();
        return next(context);
    }
}
