using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Provides context about the current property being resolved to objects
/// implementing <see cref="IInertiaPropertyValueProvider"/>.
/// </summary>
public sealed class PropertyContext
{
    /// <summary>The property key being resolved.</summary>
    public string Key { get; }

    /// <summary>All props currently being resolved for the Inertia response.</summary>
    public IReadOnlyDictionary<string, object?> Props { get; }

    /// <summary>The current HTTP context for the request.</summary>
    public HttpContext HttpContext { get; }

    /// <summary>Creates a new property context instance.</summary>
    /// <param name="key">The property key being resolved.</param>
    /// <param name="props">All resolved props so far.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    public PropertyContext(string key, IReadOnlyDictionary<string, object?> props, HttpContext httpContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(props);
        ArgumentNullException.ThrowIfNull(httpContext);

        Key = key;
        Props = props;
        HttpContext = httpContext;
    }
}
