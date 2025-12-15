namespace Inertia.Core;

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
    /// When using ASP.NET Core, this will be an HttpRequest object.
    /// </summary>
    public object Request { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderContext"/> class.
    /// </summary>
    /// <param name="component">The component being rendered.</param>
    /// <param name="request">The HTTP request (typically HttpRequest for ASP.NET Core).</param>
    public RenderContext(string component, object request)
    {
        Component = component;
        Request = request;
    }
}
