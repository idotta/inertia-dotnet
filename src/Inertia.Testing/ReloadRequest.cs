using Inertia.Core;
using Microsoft.AspNetCore.Http;

namespace Inertia.Testing;

/// <summary>
/// Helper class for simulating Inertia partial reload requests in tests.
/// </summary>
public class ReloadRequest
{
    private readonly string _url;
    private readonly string _component;
    private readonly string? _version;
    private readonly Dictionary<string, string> _headers;

    /// <summary>
    /// Create a new reload request instance.
    /// </summary>
    /// <param name="url">The URL to request.</param>
    /// <param name="component">The component name for partial reloads.</param>
    /// <param name="version">The asset version.</param>
    public ReloadRequest(string url, string component, string? version = null)
    {
        _url = url;
        _component = component;
        _version = version;
        _headers = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(_version))
        {
            _headers[InertiaHeaders.Version] = _version;
        }
    }

    /// <summary>
    /// Configure the request to reload only the specified properties.
    /// </summary>
    /// <param name="props">The properties to reload (can be array or comma-separated string).</param>
    /// <returns>This instance for method chaining.</returns>
    public ReloadRequest ReloadOnly(params string[] props)
    {
        if (props.Length > 0)
        {
            _headers[InertiaHeaders.PartialComponent] = _component;
            _headers[InertiaHeaders.PartialData] = string.Join(",", props);
        }
        return this;
    }

    /// <summary>
    /// Configure the request to reload only the specified properties.
    /// </summary>
    /// <param name="props">The properties to reload as an enumerable.</param>
    /// <returns>This instance for method chaining.</returns>
    public ReloadRequest ReloadOnly(IEnumerable<string> props)
    {
        return ReloadOnly(props.ToArray());
    }

    /// <summary>
    /// Configure the request to reload all properties except the specified ones.
    /// </summary>
    /// <param name="props">The properties to exclude (can be array or comma-separated string).</param>
    /// <returns>This instance for method chaining.</returns>
    public ReloadRequest ReloadExcept(params string[] props)
    {
        if (props.Length > 0)
        {
            _headers[InertiaHeaders.PartialComponent] = _component;
            _headers[InertiaHeaders.PartialExcept] = string.Join(",", props);
        }
        return this;
    }

    /// <summary>
    /// Configure the request to reload all properties except the specified ones.
    /// </summary>
    /// <param name="props">The properties to exclude as an enumerable.</param>
    /// <returns>This instance for method chaining.</returns>
    public ReloadRequest ReloadExcept(IEnumerable<string> props)
    {
        return ReloadExcept(props.ToArray());
    }

    /// <summary>
    /// Get the configured headers for the request.
    /// </summary>
    /// <returns>A dictionary of headers to apply to the request.</returns>
    public Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>(_headers);
    }

    /// <summary>
    /// Apply the configured headers to an HttpRequest.
    /// </summary>
    /// <param name="request">The HTTP request to modify.</param>
    public void ApplyToRequest(HttpRequest request)
    {
        // Set the Inertia header
        request.Headers[InertiaHeaders.Inertia] = "true";

        // Apply all configured headers
        foreach (var header in _headers)
        {
            request.Headers[header.Key] = header.Value;
        }
    }

    /// <summary>
    /// Get the URL for the request.
    /// </summary>
    public string Url => _url;

    /// <summary>
    /// Get the component name for the request.
    /// </summary>
    public string Component => _component;

    /// <summary>
    /// Get the version for the request.
    /// </summary>
    public string? Version => _version;
}
