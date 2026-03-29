using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.AspNetCore;

/// <summary>
/// Extension methods for mapping Inertia page endpoints.
/// </summary>
public static class InertiaEndpointExtensions
{
    /// <summary>
    /// Maps an Inertia page endpoint that renders the specified component.
    /// Equivalent to Laravel's <c>Route::inertia('/path', 'Component', $props)</c>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The URL pattern for the endpoint.</param>
    /// <param name="component">The JavaScript page component name.</param>
    /// <param name="props">Optional props to pass to the component.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> for further endpoint configuration.</returns>
    public static RouteHandlerBuilder MapInertia(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string component,
        object? props = null)
    {
        return endpoints.MapMethods(pattern, ["GET", "HEAD"], (HttpContext context) =>
        {
            var inertia = context.RequestServices.GetRequiredService<IInertia>();
            return inertia.Render(component, props);
        });
    }
}
