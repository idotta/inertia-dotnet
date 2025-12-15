using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Provides context information about the current property being resolved.
/// This information is available to objects implementing IProvidesInertiaProperty.
/// </summary>
public class PropertyContext
{
    /// <summary>
    /// Gets the property key being resolved.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets all properties in the current render context.
    /// </summary>
    public IDictionary<string, object?> Props { get; }

    /// <summary>
    /// Gets the HTTP request associated with this context.
    /// </summary>
    public HttpRequest Request { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyContext"/> class.
    /// </summary>
    /// <param name="key">The property key being resolved.</param>
    /// <param name="props">All properties in the current render context.</param>
    /// <param name="request">The HTTP request.</param>
    public PropertyContext(string key, IDictionary<string, object?> props, HttpRequest request)
    {
        Key = key;
        Props = props;
        Request = request;
    }
}
