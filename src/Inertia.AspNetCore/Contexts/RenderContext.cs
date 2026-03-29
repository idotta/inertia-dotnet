using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Provides context about the current Inertia render operation to objects
/// implementing <see cref="IInertiaPropertyProvider"/>.
/// </summary>
public sealed class RenderContext
{
    /// <summary>The Inertia page component name being rendered.</summary>
    public string Component { get; }

    /// <summary>The current HTTP context for the request.</summary>
    public HttpContext HttpContext { get; }

    /// <summary>Creates a new render context instance.</summary>
    /// <param name="component">The component name being rendered.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    public RenderContext(string component, HttpContext httpContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);
        ArgumentNullException.ThrowIfNull(httpContext);

        Component = component;
        HttpContext = httpContext;
    }
}
