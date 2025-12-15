using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inertia.Core;

/// <summary>
/// Represents an Inertia response that can be rendered as JSON (for Inertia requests)
/// or as an HTML page (for initial page loads).
/// </summary>
public class InertiaResponse
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Gets the name of the frontend component to render.
    /// </summary>
    public string Component { get; }

    /// <summary>
    /// Gets the properties/data to pass to the component.
    /// </summary>
    public Dictionary<string, object?> Props { get; }

    /// <summary>
    /// Gets the name of the root view template.
    /// </summary>
    public string RootView { get; }

    /// <summary>
    /// Gets the current asset version for cache busting.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets or sets the current URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the browser history should be cleared on the next visit.
    /// </summary>
    public bool ClearHistory { get; }

    /// <summary>
    /// Gets a value indicating whether the browser history should be encrypted.
    /// </summary>
    public bool EncryptHistory { get; }

    /// <summary>
    /// Gets additional data to pass to the view (not sent to the component).
    /// </summary>
    public Dictionary<string, object?> ViewData { get; } = new();

    /// <summary>
    /// Gets the URL resolver callback.
    /// </summary>
    public Func<string>? UrlResolver { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InertiaResponse"/> class.
    /// </summary>
    /// <param name="component">The name of the frontend component.</param>
    /// <param name="props">The properties to pass to the component.</param>
    /// <param name="rootView">The name of the root view template.</param>
    /// <param name="version">The asset version.</param>
    /// <param name="encryptHistory">Whether to encrypt the browser history.</param>
    /// <param name="urlResolver">Optional URL resolver callback.</param>
    public InertiaResponse(
        string component,
        Dictionary<string, object?> props,
        string rootView = "app",
        string version = "",
        bool encryptHistory = false,
        Func<string>? urlResolver = null)
    {
        Component = component;
        Props = props;
        RootView = rootView;
        Version = version;
        EncryptHistory = encryptHistory;
        UrlResolver = urlResolver;
        
        // ClearHistory is typically retrieved from session in Laravel
        // For now, we'll set it to false and handle session integration later
        ClearHistory = false;
    }

    /// <summary>
    /// Adds additional properties to the response.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    /// <returns>This instance for method chaining.</returns>
    public InertiaResponse With(string key, object? value)
    {
        Props[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple properties to the response.
    /// </summary>
    /// <param name="props">A dictionary containing properties to add.</param>
    /// <returns>This instance for method chaining.</returns>
    public InertiaResponse With(IDictionary<string, object?> props)
    {
        foreach (var kvp in props)
        {
            Props[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Adds data to pass to the view (not sent to the component).
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="value">The data value.</param>
    /// <returns>This instance for method chaining.</returns>
    public InertiaResponse WithViewData(string key, object? value)
    {
        ViewData[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple view data items.
    /// </summary>
    /// <param name="viewData">A dictionary containing view data to add.</param>
    /// <returns>This instance for method chaining.</returns>
    public InertiaResponse WithViewData(IDictionary<string, object?> viewData)
    {
        foreach (var kvp in viewData)
        {
            ViewData[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Serializes the Inertia page data to JSON.
    /// </summary>
    /// <returns>A JSON string representing the page data.</returns>
    public async Task<string> ToJsonAsync()
    {
        var page = await BuildPageDataAsync();
        return JsonSerializer.Serialize(page, DefaultJsonOptions);
    }

    /// <summary>
    /// Builds the page data object that will be sent to the client.
    /// </summary>
    /// <returns>A dictionary containing the page data.</returns>
    public async Task<Dictionary<string, object?>> BuildPageDataAsync()
    {
        // Resolve the URL
        var url = UrlResolver?.Invoke() ?? Url;

        var page = new Dictionary<string, object?>
        {
            ["component"] = Component,
            ["props"] = Props,
            ["url"] = url,
            ["version"] = Version
        };

        // Only include these if they're true/set
        if (ClearHistory)
        {
            page["clearHistory"] = true;
        }

        if (EncryptHistory)
        {
            page["encryptHistory"] = true;
        }

        // Additional page metadata would be added here in future phases
        // (mergeProps, deferredProps, scrollProps, onceProps, etc.)

        return await Task.FromResult(page);
    }


}
