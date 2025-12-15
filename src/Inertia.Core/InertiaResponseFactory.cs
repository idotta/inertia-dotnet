using Microsoft.Extensions.Options;

namespace Inertia.Core;

/// <summary>
/// Factory implementation for creating Inertia responses.
/// Maintains shared state such as shared props, version, and root view.
/// </summary>
public class InertiaResponseFactory : IInertia
{
    private readonly InertiaOptions _options;
    private readonly Dictionary<string, object?> _sharedProps = new();
    private string? _version;
    private Func<string>? _versionProvider;
    private string? _rootView;
    private bool? _encryptHistory;
    private Func<string>? _urlResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="InertiaResponseFactory"/> class.
    /// </summary>
    /// <param name="options">The Inertia configuration options.</param>
    public InertiaResponseFactory(IOptions<InertiaOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<InertiaResponse> RenderAsync(string component, object? props = null)
    {
        // Validate component exists if configured
        if (_options.EnsurePagesExist)
        {
            FindComponentOrFail(component);
        }

        // Convert props to dictionary
        var propsDict = props != null ? ConvertToDictionary(props) : new Dictionary<string, object?>();

        // Merge with shared props
        var mergedProps = new Dictionary<string, object?>(_sharedProps);
        foreach (var kvp in propsDict)
        {
            mergedProps[kvp.Key] = kvp.Value;
        }

        // Create the response
        var response = new InertiaResponse(
            component,
            mergedProps,
            _rootView ?? _options.RootView,
            GetVersion(),
            _encryptHistory ?? _options.History.Encrypt,
            _urlResolver
        );

        return await Task.FromResult(response);
    }

    /// <inheritdoc/>
    public object Location(string url)
    {
        // This will be fully implemented in Phase 3 when we have access to HttpContext
        // For now, return a simple object that can be handled by middleware
        return new InertiaLocationResult(url);
    }

    /// <inheritdoc/>
    public void Share(string key, object? value)
    {
        _sharedProps[key] = value;
    }

    /// <inheritdoc/>
    public void Share(object props)
    {
        var propsDict = ConvertToDictionary(props);
        foreach (var kvp in propsDict)
        {
            _sharedProps[kvp.Key] = kvp.Value;
        }
    }

    /// <inheritdoc/>
    public object? GetShared(string? key = null, object? defaultValue = null)
    {
        if (key == null)
        {
            return _sharedProps;
        }

        return _sharedProps.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <inheritdoc/>
    public void FlushShared()
    {
        _sharedProps.Clear();
    }

    /// <inheritdoc/>
    public void SetVersion(string version)
    {
        _version = version;
        _versionProvider = null;
    }

    /// <inheritdoc/>
    public void SetVersion(Func<string> versionProvider)
    {
        _versionProvider = versionProvider;
        _version = null;
    }

    /// <inheritdoc/>
    public string GetVersion()
    {
        if (_versionProvider != null)
        {
            return _versionProvider();
        }

        return _version ?? string.Empty;
    }

    /// <inheritdoc/>
    public void SetRootView(string viewName)
    {
        _rootView = viewName;
    }

    /// <inheritdoc/>
    public void ClearHistory()
    {
        // This will be implemented in Phase 3 with session support
        // For now, just a placeholder
        throw new NotImplementedException("ClearHistory will be implemented in Phase 3 with session support");
    }

    /// <inheritdoc/>
    public void EncryptHistory(bool encrypt = true)
    {
        _encryptHistory = encrypt;
    }

    /// <inheritdoc/>
    public void ResolveUrlUsing(Func<string>? urlResolver)
    {
        _urlResolver = urlResolver;
    }

    /// <summary>
    /// Validates that the component exists.
    /// </summary>
    /// <param name="component">The component name to validate.</param>
    /// <exception cref="ComponentNotFoundException">Thrown when the component is not found.</exception>
    private void FindComponentOrFail(string component)
    {
        // Component validation logic will be implemented in Phase 7
        // For now, we'll just check if page paths are configured
        if (_options.PagePaths.Count == 0)
        {
            return; // No validation if no paths configured
        }

        // Basic validation - check if any file exists with the component name
        foreach (var path in _options.PagePaths)
        {
            foreach (var extension in _options.PageExtensions)
            {
                var fullPath = Path.Combine(path, $"{component}{extension}");
                if (File.Exists(fullPath))
                {
                    return; // Component found
                }
            }
        }

        throw new ComponentNotFoundException($"Inertia page component [{component}] not found.");
    }

    /// <summary>
    /// Converts an object to a dictionary.
    /// </summary>
    private static Dictionary<string, object?> ConvertToDictionary(object obj)
    {
        if (obj is Dictionary<string, object?> dict)
        {
            return dict;
        }

        var result = new Dictionary<string, object?>();
        var properties = obj.GetType().GetProperties();
        
        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            result[property.Name] = value;
        }

        return result;
    }
}

/// <summary>
/// Represents a location redirect result for Inertia responses.
/// </summary>
public class InertiaLocationResult
{
    /// <summary>
    /// Gets the URL to redirect to.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InertiaLocationResult"/> class.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    public InertiaLocationResult(string url)
    {
        Url = url;
    }
}

/// <summary>
/// Exception thrown when a component is not found.
/// </summary>
public class ComponentNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ComponentNotFoundException(string message) : base(message)
    {
    }
}
