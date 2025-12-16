namespace Inertia.Core;

/// <summary>
/// Provides context information about the current Inertia render operation.
/// This information is available to objects implementing IProvidesInertiaProperties.
/// </summary>
/// <typeparam name="TRequest">The type of the HTTP request object.</typeparam>
public class RenderContext<TRequest>
{
    /// <summary>
    /// Gets the component being rendered.
    /// </summary>
    public string Component { get; }

    /// <summary>
    /// Gets the HTTP request associated with this context.
    /// </summary>
    public TRequest Request { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderContext{TRequest}"/> class.
    /// </summary>
    /// <param name="component">The component being rendered.</param>
    /// <param name="request">The HTTP request.</param>
    public RenderContext(string component, TRequest request)
    {
        Component = component;
        Request = request;
    }
}
