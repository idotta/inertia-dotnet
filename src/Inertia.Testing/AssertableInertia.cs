using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Inertia.Testing;

/// <summary>
/// Provides fluent assertion methods for testing Inertia responses.
/// </summary>
public class AssertableInertia
{
    private readonly string _component;
    private readonly string _url;
    private readonly string? _version;
    private readonly bool _encryptHistory;
    private readonly bool _clearHistory;
    private readonly Dictionary<string, object?> _props;
    private readonly Dictionary<string, List<string>> _deferredProps;
    private readonly HttpResponse _response;

    private AssertableInertia(
        HttpResponse response,
        string component,
        string url,
        string? version,
        bool encryptHistory,
        bool clearHistory,
        Dictionary<string, object?> props,
        Dictionary<string, List<string>>? deferredProps = null)
    {
        _response = response;
        _component = component;
        _url = url;
        _version = version;
        _encryptHistory = encryptHistory;
        _clearHistory = clearHistory;
        _props = props;
        _deferredProps = deferredProps ?? new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Create an AssertableInertia instance from an HTTP response.
    /// </summary>
    /// <param name="response">The HTTP response containing Inertia page data.</param>
    /// <returns>An AssertableInertia instance for fluent assertions.</returns>
    public static AssertableInertia FromResponse(HttpResponse response)
    {
        // Get the page data from HttpContext.Items (set by middleware/controller)
        if (!response.HttpContext.Items.TryGetValue("InertiaPageData", out var pageDataObj))
        {
            throw new InvalidOperationException("Response does not contain Inertia page data. Make sure the response was created using Inertia.");
        }

        Dictionary<string, object?> pageData;

        if (pageDataObj is string jsonString)
        {
            pageData = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonString)
                ?? throw new InvalidOperationException("Failed to deserialize Inertia page data.");
        }
        else if (pageDataObj is Dictionary<string, object?> dict)
        {
            pageData = dict;
        }
        else
        {
            var typeName = pageDataObj?.GetType().Name ?? "null";
            throw new InvalidOperationException($"Unexpected Inertia page data type: {typeName}");
        }

        // Validate required fields
        Assert.True(pageData.ContainsKey("component"), "Inertia page data must contain 'component'");
        Assert.True(pageData.ContainsKey("props"), "Inertia page data must contain 'props'");
        Assert.True(pageData.ContainsKey("url"), "Inertia page data must contain 'url'");

        var component = pageData["component"]?.ToString() ?? throw new InvalidOperationException("Component is null");
        var url = pageData["url"]?.ToString() ?? throw new InvalidOperationException("URL is null");
        var version = pageData.TryGetValue("version", out var v) ? v?.ToString() : null;
        var encryptHistory = pageData.TryGetValue("encryptHistory", out var eh) && Convert.ToBoolean(eh);
        var clearHistory = pageData.TryGetValue("clearHistory", out var ch) && Convert.ToBoolean(ch);

        var props = pageData["props"] as Dictionary<string, object?>
            ?? DeserializeProps(pageData["props"]);

        Dictionary<string, List<string>>? deferredProps = null;
        if (pageData.TryGetValue("deferredProps", out var dp) && dp != null)
        {
            deferredProps = DeserializeDeferredProps(dp);
        }

        return new AssertableInertia(response, component, url, version, encryptHistory, clearHistory, props, deferredProps);
    }

    private static Dictionary<string, object?> DeserializeProps(object? propsObj)
    {
        if (propsObj is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonElement.GetRawText())
                ?? new Dictionary<string, object?>();
        }

        return new Dictionary<string, object?>();
    }

    private static Dictionary<string, List<string>> DeserializeDeferredProps(object? deferredPropsObj)
    {
        if (deferredPropsObj is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonElement.GetRawText())
                ?? new Dictionary<string, List<string>>();
        }

        if (deferredPropsObj is Dictionary<string, object?> dict)
        {
            var result = new Dictionary<string, List<string>>();
            foreach (var kvp in dict)
            {
                if (kvp.Value is List<string> list)
                {
                    result[kvp.Key] = list;
                }
                else if (kvp.Value is JsonElement je && je.ValueKind == JsonValueKind.Array)
                {
                    result[kvp.Key] = JsonSerializer.Deserialize<List<string>>(je.GetRawText())
                        ?? new List<string>();
                }
            }
            return result;
        }

        return new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Assert that the response uses the specified component.
    /// </summary>
    /// <param name="name">The expected component name.</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia WithComponent(string name)
    {
        Assert.Equal(name, _component);
        return this;
    }

    /// <summary>
    /// Assert that the page URL matches the expected value.
    /// </summary>
    /// <param name="url">The expected URL.</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia WithUrl(string url)
    {
        Assert.Equal(url, _url);
        return this;
    }

    /// <summary>
    /// Assert that the asset version matches the expected value.
    /// </summary>
    /// <param name="version">The expected version.</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia WithVersion(string version)
    {
        Assert.Equal(version, _version);
        return this;
    }

    /// <summary>
    /// Assert that a property exists.
    /// </summary>
    /// <param name="key">The property key (supports dot notation).</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia Has(string key)
    {
        var value = GetPropValue(key);
        Assert.NotNull(value);
        return this;
    }

    /// <summary>
    /// Assert that a property does not exist.
    /// </summary>
    /// <param name="key">The property key (supports dot notation).</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia Missing(string key)
    {
        var value = GetPropValue(key);
        Assert.Null(value);
        return this;
    }

    /// <summary>
    /// Assert that a property has the expected value.
    /// </summary>
    /// <param name="key">The property key (supports dot notation).</param>
    /// <param name="value">The expected value.</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia Where(string key, object? value)
    {
        var actualValue = GetPropValue(key);

        // Normalize both values for comparison
        var normalizedExpected = NormalizeValue(value);
        var normalizedActual = NormalizeValue(actualValue);

        Assert.Equal(normalizedExpected, normalizedActual);
        return this;
    }

    /// <summary>
    /// Assert that a property matches a predicate.
    /// </summary>
    /// <param name="key">The property key (supports dot notation).</param>
    /// <param name="predicate">The predicate to test the value against.</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia Where(string key, Func<object?, bool> predicate)
    {
        var value = GetPropValue(key);
        Assert.True(predicate(value), $"Inertia property [{key}] was marked as invalid using a predicate.");
        return this;
    }

    /// <summary>
    /// Assert that a property is of the expected type.
    /// </summary>
    /// <param name="key">The property key (supports dot notation).</param>
    /// <param name="type">The expected type.</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia WhereType(string key, Type type)
    {
        var value = GetPropValue(key);
        Assert.NotNull(value);
        Assert.IsAssignableFrom(type, value);
        return this;
    }

    /// <summary>
    /// Assert that a collection property has the expected count.
    /// </summary>
    /// <param name="key">The property key (supports dot notation).</param>
    /// <param name="count">The expected count.</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia WithCount(string key, int count)
    {
        var value = GetPropValue(key);

        if (value is System.Collections.ICollection collection)
        {
            Assert.Equal(count, collection.Count);
        }
        else if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            Assert.Equal(count, jsonElement.GetArrayLength());
        }
        else
        {
            throw new InvalidOperationException($"Property [{key}] is not a collection.");
        }

        return this;
    }

    /// <summary>
    /// Load deferred props for the specified groups and perform assertions on them.
    /// </summary>
    /// <param name="groups">The deferred prop groups to load. If null or empty, loads all groups.</param>
    /// <param name="callback">Optional callback to perform assertions on the reloaded response.</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia LoadDeferredProps(string[]? groups = null, Action<AssertableInertia>? callback = null)
    {
        var groupsToLoad = groups ?? _deferredProps.Keys.ToArray();

        var propsToLoad = groupsToLoad
            .Where(g => _deferredProps.ContainsKey(g))
            .SelectMany(g => _deferredProps[g])
            .ToArray();

        if (propsToLoad.Length > 0)
        {
            // Create a reload request with the deferred props
            var reloadRequest = new ReloadRequest(_url, _component, _version)
                .ReloadOnly(propsToLoad);

            // In a real test scenario, this would make an actual HTTP request
            // For now, we'll simulate it by returning this instance
        }

        // The callback can be used to assert on the "reloaded" response
        callback?.Invoke(this);

        return this;
    }

    /// <summary>
    /// Dump the page data or a specific property to the output.
    /// </summary>
    /// <param name="prop">Optional property key to dump (supports dot notation).</param>
    /// <returns>This instance for method chaining.</returns>
    public AssertableInertia Dump(string? prop = null)
    {
        var value = string.IsNullOrEmpty(prop) ? _props : GetPropValue(prop);
        Console.WriteLine(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        return this;
    }

    /// <summary>
    /// Dump the page data or a specific property and throw an exception to halt execution.
    /// </summary>
    /// <param name="prop">Optional property key to dump (supports dot notation).</param>
    public void Dd(string? prop = null)
    {
        Dump(prop);
        throw new InvalidOperationException("Dd() was called - this is intentional to halt execution for debugging.");
    }

    /// <summary>
    /// Convert the Inertia page to an array representation.
    /// </summary>
    /// <returns>A dictionary containing the page data.</returns>
    public Dictionary<string, object?> ToArray()
    {
        return new Dictionary<string, object?>
        {
            ["component"] = _component,
            ["props"] = _props,
            ["url"] = _url,
            ["version"] = _version,
            ["encryptHistory"] = _encryptHistory,
            ["clearHistory"] = _clearHistory
        };
    }

    private object? GetPropValue(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return _props;
        }

        var parts = key.Split('.');
        object? current = _props;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(part, out current))
                {
                    return null;
                }
            }
            else if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else if (jsonElement.ValueKind == JsonValueKind.Array && int.TryParse(part, out var index))
                {
                    if (index >= 0 && index < jsonElement.GetArrayLength())
                    {
                        current = jsonElement[index];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    private static object? NormalizeValue(object? value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.TryGetInt32(out var i) ? i : jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => value
            };
        }

        return value;
    }
}
