using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Provides context information about the current Inertia render operation.
/// This information is available to objects implementing IProvidesInertiaProperties.
/// </summary>
public class RenderContext
{
    /// <summary>
    /// Gets the component being rendered.
    /// </summary>
    public string Component { get; }

    /// <summary>
    /// Gets the HTTP request associated with this context.
    /// </summary>
    public HttpRequest Request { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderContext"/> class.
    /// </summary>
    /// <param name="component">The component being rendered.</param>
    /// <param name="request">The HTTP request.</param>
    public RenderContext(string component, HttpRequest request)
    {
        Component = component;
        Request = request;
    }
}
