using Microsoft.AspNetCore.Builder;

namespace Inertia.AspNetCore;

/// <summary>
/// Extension methods for adding Inertia middleware to the application pipeline.
/// </summary>
public static class InertiaApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Inertia middleware to the application's request pipeline.
    /// This should be called after UseRouting() and before UseEndpoints().
    /// </summary>
    /// <typeparam name="THandler">The type of the HandleInertiaRequests implementation.</typeparam>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> so that additional calls can be chained.</returns>
    public static IApplicationBuilder UseInertia<THandler>(this IApplicationBuilder app)
        where THandler : HandleInertiaRequests
    {
        return app.UseMiddleware<InertiaMiddleware>();
    }

    /// <summary>
    /// Adds the Inertia middleware to the application's request pipeline.
    /// This should be called after UseRouting() and before UseEndpoints().
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> so that additional calls can be chained.</returns>
    public static IApplicationBuilder UseInertia(this IApplicationBuilder app)
    {
        return app.UseMiddleware<InertiaMiddleware>();
    }
}
