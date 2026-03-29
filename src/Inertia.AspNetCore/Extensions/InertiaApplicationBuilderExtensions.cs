using Microsoft.AspNetCore.Builder;

namespace Inertia.AspNetCore;

/// <summary>
/// Extension methods for adding Inertia.js middleware to the request pipeline.
/// </summary>
public static class InertiaApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Inertia middleware to the request pipeline. Handles versioning, shared props,
    /// 302→303 conversion, empty response handling, and fragment redirects.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder for chaining.</returns>
    public static IApplicationBuilder UseInertia(this IApplicationBuilder app)
    {
        return app.UseMiddleware<InertiaMiddleware>();
    }

    /// <summary>
    /// Adds the Inertia history encryption middleware to the request pipeline.
    /// When used, must be placed before <see cref="UseInertia"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder for chaining.</returns>
    public static IApplicationBuilder UseInertiaEncryptHistory(this IApplicationBuilder app)
    {
        return app.UseMiddleware<EncryptHistoryMiddleware>();
    }
}
