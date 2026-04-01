using System.Net;
using System.Text.Json;
using Inertia.AspNetCore;
using Xunit;

namespace Inertia.Testing;

/// <summary>
/// Provides fluent assertions for Inertia.js page responses.
/// Created via <see cref="InertiaTestExtensions"/> or <see cref="FromJson"/>.
/// </summary>
public sealed class AssertableInertia
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [ThreadStatic]
    private static PageExistenceConfig? _pageExistenceConfig;

    /// <summary>
    /// Configuration for component file existence checking in tests.
    /// </summary>
    public sealed record PageExistenceConfig(
        bool EnsurePagesExist, string[] PagePaths, string[] PageExtensions);

    /// <summary>
    /// Configures page existence checking for component assertions.
    /// Call in test setup to enable file validation. Pass <c>null</c> to reset.
    /// </summary>
    public static void Configure(PageExistenceConfig? config)
        => _pageExistenceConfig = config;

    /// <summary>
    /// Configures page existence checking from an <see cref="InertiaOptions"/> instance.
    /// </summary>
    public static void Configure(InertiaOptions options)
        => _pageExistenceConfig = new PageExistenceConfig(
            options.TestingEnsurePagesExist, options.PagePaths, options.PageExtensions);

    private readonly string _component;
    private readonly string _url;
    private readonly string _version;
    private readonly bool _encryptHistory;
    private readonly bool _clearHistory;
    private readonly JsonElement _props;
    private readonly IDictionary<string, IReadOnlyList<string>> _deferredProps;
    private readonly JsonElement? _flash;
    private readonly HttpClient? _httpClient;
    private readonly bool _isScope;
    private readonly HashSet<string>? _interactedKeys;
    private bool _etcCalled;

    private AssertableInertia(
        string component,
        string url,
        string version,
        bool encryptHistory,
        bool clearHistory,
        JsonElement props,
        IDictionary<string, IReadOnlyList<string>> deferredProps,
        JsonElement? flash,
        HttpClient? httpClient)
    {
        _component = component;
        _url = url;
        _version = version;
        _encryptHistory = encryptHistory;
        _clearHistory = clearHistory;
        _props = props;
        _deferredProps = deferredProps;
        _flash = flash;
        _httpClient = httpClient;
    }

    private AssertableInertia(JsonElement scopedProps, HttpClient? httpClient)
    {
        _component = "";
        _url = "";
        _version = "";
        _props = scopedProps;
        _deferredProps = new Dictionary<string, IReadOnlyList<string>>();
        _httpClient = httpClient;
        _isScope = true;
        _interactedKeys = new HashSet<string>();
    }

    // ---- Factory methods ----

    /// <summary>Creates an <see cref="AssertableInertia"/> from a JSON string.</summary>
    public static AssertableInertia FromJson(string json, HttpClient? httpClient = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        ValidatePageStructure(root);

        var component = root.GetProperty("component").GetString()!;
        var url = root.GetProperty("url").GetString()!;
        var version = root.GetProperty("version").GetString()!;
        var props = root.GetProperty("props").Clone();

        var encryptHistory = root.TryGetProperty("encryptHistory", out var eh) && eh.GetBoolean();
        var clearHistory = root.TryGetProperty("clearHistory", out var ch) && ch.GetBoolean();

        var deferredProps = ParseDeferredProps(root);
        JsonElement? flash = root.TryGetProperty("flash", out var f) ? f.Clone() : null;

        return new AssertableInertia(
            component, url, version, encryptHistory, clearHistory,
            props, deferredProps, flash, httpClient);
    }

    /// <summary>Creates an <see cref="AssertableInertia"/> from an HTTP response.</summary>
    internal static async Task<AssertableInertia> FromResponseAsync(
        HttpResponseMessage response, HttpClient? httpClient = null)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var hasInertiaHeader = response.Headers.Contains(InertiaHeaderNames.Inertia);

        if (hasInertiaHeader && contentType == "application/json")
        {
            var json = await response.Content.ReadAsStringAsync();
            return FromJson(json, httpClient);
        }

        // Fallback: try to extract data-page from HTML
        var body = await response.Content.ReadAsStringAsync();
        var pageJson = ExtractDataPageFromHtml(body);
        if (pageJson is not null)
            return FromJson(pageJson, httpClient);

        Assert.Fail("Not a valid Inertia response.");
        return null!; // unreachable
    }

    // ---- Page-level assertions ----

    /// <summary>
    /// Asserts the Inertia page component matches the expected value.
    /// When <paramref name="shouldExist"/> is <c>true</c> (or <c>null</c> with
    /// <see cref="PageExistenceConfig.EnsurePagesExist"/> enabled), validates that a matching
    /// component file exists on disk.
    /// </summary>
    public AssertableInertia Component(string expected, bool? shouldExist = null)
    {
        Assert.True(expected == _component,
            $"Unexpected Inertia page component. Expected: {expected}, Actual: {_component}");

        var shouldCheck = shouldExist
            ?? (_pageExistenceConfig?.EnsurePagesExist ?? false);

        if (shouldCheck)
            ValidateComponentFileExists(expected);

        return this;
    }

    /// <summary>Asserts the page URL matches the expected value.</summary>
    public AssertableInertia Url(string expected)
    {
        Assert.True(expected == _url,
            $"Unexpected Inertia page url. Expected: {expected}, Actual: {_url}");
        return this;
    }

    /// <summary>Asserts the asset version matches the expected value.</summary>
    public AssertableInertia Version(string expected)
    {
        Assert.True(expected == _version,
            $"Unexpected Inertia asset version. Expected: {expected}, Actual: {_version}");
        return this;
    }

    // ---- Prop assertions ----

    /// <summary>Asserts that a prop exists at the given dot-notation path.</summary>
    public AssertableInertia Has(string path)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out _),
            $"Property [{path}] does not exist.");
        TrackInteraction(path);
        return this;
    }

    /// <summary>Asserts that a prop has the given number of items (array length or object property count).</summary>
    public AssertableInertia Has(string path, int expectedCount)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");

        var actualCount = element.ValueKind switch
        {
            JsonValueKind.Array => element.GetArrayLength(),
            JsonValueKind.Object => CountObjectProperties(element),
            _ => throw new Xunit.Sdk.XunitException(
                $"Property [{path}] is not an array or object (was {element.ValueKind})."),
        };

        Assert.True(expectedCount == actualCount,
            $"Property [{path}] expected {expectedCount} items but has {actualCount}.");
        TrackInteraction(path);
        return this;
    }

    /// <summary>Asserts that all given prop paths exist.</summary>
    public AssertableInertia HasAll(params string[] paths)
    {
        foreach (var path in paths)
            Has(path);
        return this;
    }

    /// <summary>Asserts that a prop does not exist at the given dot-notation path.</summary>
    public AssertableInertia Missing(string path)
    {
        Assert.False(
            TryGetNestedProperty(_props, path, out _),
            $"Property [{path}] was found but was expected to be missing.");
        TrackInteraction(path);
        return this;
    }

    /// <summary>Asserts that none of the given prop paths exist.</summary>
    public AssertableInertia MissingAll(params string[] paths)
    {
        foreach (var path in paths)
            Missing(path);
        return this;
    }

    /// <summary>Asserts that a prop exists at the given path with the expected value.</summary>
    public AssertableInertia Where(string path, object? expected)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");

        Assert.True(
            JsonElementEquals(element, expected),
            $"Property [{path}] does not match expected value. " +
            $"Expected: {FormatValue(expected)}, Actual: {element.GetRawText()}");
        TrackInteraction(path);
        return this;
    }

    /// <summary>Asserts that a prop exists at the given path and passes the assertion callback.</summary>
    public AssertableInertia Where(string path, Action<JsonElement> assertion)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");

        assertion(element);
        TrackInteraction(path);
        return this;
    }

    /// <summary>Asserts that a prop exists at the given path with a value that does NOT match the expected value.</summary>
    public AssertableInertia WhereNot(string path, object? expected)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");

        Assert.False(
            JsonElementEquals(element, expected),
            $"Property [{path}] should not match value {FormatValue(expected)}, but it does.");
        TrackInteraction(path);
        return this;
    }

    /// <summary>
    /// Asserts that a prop at the given path has the expected JSON value kind.
    /// Accepted types: <c>"string"</c>, <c>"integer"</c>, <c>"number"</c>, <c>"boolean"</c>,
    /// <c>"array"</c>, <c>"object"</c>, <c>"null"</c>.
    /// </summary>
    public AssertableInertia WhereType(string path, string expectedType)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");

        var actualKind = element.ValueKind;
        var matches = expectedType.ToLowerInvariant() switch
        {
            "string" => actualKind == JsonValueKind.String,
            "integer" => actualKind == JsonValueKind.Number && element.TryGetInt64(out _),
            "number" or "double" => actualKind == JsonValueKind.Number,
            "boolean" or "bool" => actualKind is JsonValueKind.True or JsonValueKind.False,
            "array" => actualKind == JsonValueKind.Array,
            "object" => actualKind == JsonValueKind.Object,
            "null" => actualKind == JsonValueKind.Null,
            _ => throw new ArgumentException(
                $"Unknown type '{expectedType}'. Expected one of: string, integer, number, boolean, array, object, null.",
                nameof(expectedType)),
        };

        Assert.True(matches,
            $"Property [{path}] expected type '{expectedType}' but was '{actualKind}'.");
        TrackInteraction(path);
        return this;
    }

    /// <summary>
    /// Asserts that the array or string at the given path contains the expected value.
    /// For arrays, checks if any element matches. For strings, checks substring containment.
    /// </summary>
    public AssertableInertia WhereContains(string path, object? expected)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");

        var contains = element.ValueKind switch
        {
            JsonValueKind.Array => element.EnumerateArray().Any(item => JsonElementEquals(item, expected)),
            JsonValueKind.String when expected is string s =>
                element.GetString()?.Contains(s, StringComparison.Ordinal) ?? false,
            _ => throw new Xunit.Sdk.XunitException(
                $"Property [{path}] is not an array or string (was {element.ValueKind})."),
        };

        Assert.True(contains,
            $"Property [{path}] does not contain expected value {FormatValue(expected)}.");
        TrackInteraction(path);
        return this;
    }

    /// <summary>Asserts that at least one of the given prop paths exists.</summary>
    public AssertableInertia HasAny(params string[] paths)
    {
        var found = paths.Any(path => TryGetNestedProperty(_props, path, out _));

        Assert.True(found,
            $"None of the expected properties [{string.Join(", ", paths)}] exist.");

        foreach (var path in paths)
        {
            if (TryGetNestedProperty(_props, path, out _))
            {
                TrackInteraction(path);
                break;
            }
        }

        return this;
    }

    // ---- Scoping ----

    /// <summary>
    /// Creates a scoped assertion context for the data at the given path.
    /// Within the scope, interaction tracking is enforced unless <see cref="Etc"/> is called.
    /// </summary>
    public AssertableInertia Scope(string path, Action<AssertableInertia> callback)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");

        TrackInteraction(path);

        var scoped = new AssertableInertia(element, _httpClient);
        callback(scoped);
        scoped.VerifyInteracted();

        return this;
    }

    /// <summary>
    /// Creates a scoped assertion context for the first element of the array at the given path.
    /// </summary>
    public AssertableInertia First(string path, Action<AssertableInertia> callback)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");
        Assert.True(element.ValueKind == JsonValueKind.Array,
            $"Property [{path}] is not an array.");
        Assert.True(element.GetArrayLength() > 0,
            $"Property [{path}] is an empty array.");

        TrackInteraction(path);

        var scoped = new AssertableInertia(element[0], _httpClient);
        callback(scoped);
        scoped.VerifyInteracted();

        return this;
    }

    /// <summary>
    /// Creates a scoped assertion context for the first element of the current array.
    /// Only valid when the current scope is an array.
    /// </summary>
    public AssertableInertia First(Action<AssertableInertia> callback)
    {
        Assert.True(_props.ValueKind == JsonValueKind.Array,
            "Current scope is not an array.");
        Assert.True(_props.GetArrayLength() > 0,
            "Current scope is an empty array.");

        var scoped = new AssertableInertia(_props[0], _httpClient);
        callback(scoped);
        scoped.VerifyInteracted();

        return this;
    }

    /// <summary>
    /// Iterates each element of the array at the given path, creating a scoped assertion context for each.
    /// </summary>
    public AssertableInertia Each(string path, Action<AssertableInertia> callback)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");
        Assert.True(element.ValueKind == JsonValueKind.Array,
            $"Property [{path}] is not an array.");

        TrackInteraction(path);

        for (var i = 0; i < element.GetArrayLength(); i++)
        {
            var scoped = new AssertableInertia(element[i], _httpClient);
            callback(scoped);
            scoped.VerifyInteracted();
        }

        return this;
    }

    /// <summary>
    /// Iterates each element of the current array, creating a scoped assertion context for each.
    /// Only valid when the current scope is an array.
    /// </summary>
    public AssertableInertia Each(Action<AssertableInertia> callback)
    {
        Assert.True(_props.ValueKind == JsonValueKind.Array,
            "Current scope is not an array.");

        for (var i = 0; i < _props.GetArrayLength(); i++)
        {
            var scoped = new AssertableInertia(_props[i], _httpClient);
            callback(scoped);
            scoped.VerifyInteracted();
        }

        return this;
    }

    /// <summary>
    /// Marks all remaining properties as interacted, disabling the completeness check for this scope.
    /// </summary>
    public AssertableInertia Etc()
    {
        _etcCalled = true;
        return this;
    }

    // ---- Flash assertions ----

    /// <summary>Asserts that flash data contains the given key.</summary>
    public AssertableInertia HasFlash(string key)
    {
        Assert.True(
            _flash.HasValue && TryGetNestedProperty(_flash.Value, key, out _),
            $"Inertia Flash Data is missing key [{key}].");
        return this;
    }

    /// <summary>Asserts that flash data contains the given key with the expected value.</summary>
    public AssertableInertia HasFlash(string key, object? expected)
    {
        JsonElement element = default;
        Assert.True(
            _flash.HasValue && TryGetNestedProperty(_flash.Value, key, out element),
            $"Inertia Flash Data is missing key [{key}].");

        Assert.True(
            JsonElementEquals(element, expected),
            $"Inertia Flash Data [{key}] does not match expected value.");
        return this;
    }

    /// <summary>Asserts that flash data does not contain the given key.</summary>
    public AssertableInertia MissingFlash(string key)
    {
        var exists = _flash.HasValue && TryGetNestedProperty(_flash.Value, key, out _);
        Assert.False(exists, $"Inertia Flash Data has unexpected key [{key}].");
        return this;
    }

    // ---- Reload methods ----

    /// <summary>Reloads the page and runs assertions on the reloaded response.</summary>
    public async Task<AssertableInertia> ReloadAsync(
        Action<AssertableInertia>? callback = null,
        string[]? only = null,
        string[]? except = null)
    {
        if (_httpClient is null)
            throw new InvalidOperationException(
                "HttpClient is required for reload requests. Pass it via AssertInertia(callback, httpClient).");

        var onlyStr = only is not null ? string.Join(",", only) : null;
        var exceptStr = except is not null ? string.Join(",", except) : null;

        var reloadRequest = new ReloadRequest(_httpClient, _url, _component, _version, onlyStr, exceptStr);
        var response = await reloadRequest.ExecuteAsync();
        var assertable = await FromResponseAsync(response, _httpClient);

        // Verify page identity matches
        assertable.Component(_component);
        assertable.Url(_url);
        assertable.Version(_version);

        callback?.Invoke(assertable);

        return this;
    }

    /// <summary>Reloads the page requesting only the specified prop.</summary>
    public Task<AssertableInertia> ReloadOnlyAsync(
        string only, Action<AssertableInertia>? callback = null)
        => ReloadOnlyAsync([only], callback);

    /// <summary>Reloads the page requesting only the specified props.</summary>
    public async Task<AssertableInertia> ReloadOnlyAsync(
        string[] only, Action<AssertableInertia>? callback = null)
    {
        return await ReloadAsync(
            callback: assertable =>
            {
                assertable.HasAll(only);
                callback?.Invoke(assertable);
            },
            only: only);
    }

    /// <summary>Reloads the page excluding the specified prop.</summary>
    public Task<AssertableInertia> ReloadExceptAsync(
        string except, Action<AssertableInertia>? callback = null)
        => ReloadExceptAsync([except], callback);

    /// <summary>Reloads the page excluding the specified props.</summary>
    public async Task<AssertableInertia> ReloadExceptAsync(
        string[] except, Action<AssertableInertia>? callback = null)
    {
        return await ReloadAsync(
            callback: assertable =>
            {
                assertable.MissingAll(except);
                callback?.Invoke(assertable);
            },
            except: except);
    }

    /// <summary>Loads deferred props for all groups.</summary>
    public async Task<AssertableInertia> LoadDeferredPropsAsync(
        Action<AssertableInertia> callback)
    {
        var allProps = _deferredProps.Values
            .SelectMany(props => props)
            .ToArray();

        return await ReloadOnlyAsync(allProps, callback);
    }

    /// <summary>Loads deferred props for the specified group.</summary>
    public Task<AssertableInertia> LoadDeferredPropsAsync(
        string group, Action<AssertableInertia>? callback = null)
        => LoadDeferredPropsAsync([group], callback);

    /// <summary>Loads deferred props for the specified groups.</summary>
    public async Task<AssertableInertia> LoadDeferredPropsAsync(
        string[] groups, Action<AssertableInertia>? callback = null)
    {
        var props = groups
            .Where(_deferredProps.ContainsKey)
            .SelectMany(g => _deferredProps[g])
            .ToArray();

        return await ReloadOnlyAsync(props, callback);
    }

    // ---- Data access ----

    /// <summary>Returns the component name.</summary>
    public string GetComponent() => _component;

    /// <summary>Returns the page URL.</summary>
    public string GetUrl() => _url;

    /// <summary>Returns the asset version.</summary>
    public string GetVersion() => _version;

    /// <summary>Returns the deferred props mapping.</summary>
    public IDictionary<string, IReadOnlyList<string>> GetDeferredProps() => _deferredProps;

    /// <summary>Returns the raw props as a <see cref="JsonElement"/>.</summary>
    public JsonElement GetProps() => _props;

    /// <summary>Gets the prop value at the given dot-notation path as a <see cref="JsonElement"/>.</summary>
    public JsonElement Prop(string path)
    {
        Assert.True(
            TryGetNestedProperty(_props, path, out var element),
            $"Property [{path}] does not exist.");
        TrackInteraction(path);
        return element;
    }

    /// <summary>Gets the prop value at the given path, deserialized to the specified type.</summary>
    public T? Prop<T>(string path)
    {
        var element = Prop(path);
        return element.Deserialize<T>();
    }

    /// <summary>Returns the full page data as a dictionary.</summary>
    public IDictionary<string, object?> ToPage()
    {
        var page = new Dictionary<string, object?>
        {
            ["component"] = _component,
            ["props"] = JsonSerializer.Deserialize<Dictionary<string, object?>>(_props.GetRawText()),
            ["url"] = _url,
            ["version"] = _version,
        };

        if (_flash.HasValue)
            page["flash"] = JsonSerializer.Deserialize<Dictionary<string, object?>>(_flash.Value.GetRawText());

        if (_encryptHistory)
            page["encryptHistory"] = true;

        if (_clearHistory)
            page["clearHistory"] = true;

        return page;
    }

    // ---- Private helpers ----

    private static void ValidateComponentFileExists(string component)
    {
        var config = _pageExistenceConfig;
        if (config is null || config.PagePaths.Length == 0)
        {
            Assert.Fail(
                $"Inertia page component file [{component}] cannot be validated: no PagePaths configured. " +
                "Call AssertableInertia.Configure() with PagePaths to enable file existence checks.");
            return;
        }

        foreach (var basePath in config.PagePaths)
            foreach (var ext in config.PageExtensions)
                if (File.Exists(Path.Combine(basePath, $"{component}.{ext}")))
                    return;

        Assert.Fail($"Inertia page component file [{component}] does not exist.");
    }

    private void TrackInteraction(string path)
    {
        _interactedKeys?.Add(path.Split('.')[0]);
    }

    private void VerifyInteracted()
    {
        if (!_isScope || _etcCalled || _interactedKeys is null)
            return;

        if (_props.ValueKind != JsonValueKind.Object)
            return;

        var allKeys = new HashSet<string>();
        foreach (var prop in _props.EnumerateObject())
            allKeys.Add(prop.Name);

        var uninteracted = allKeys.Except(_interactedKeys).ToList();
        if (uninteracted.Count > 0)
        {
            Assert.Fail(
                $"Unexpected properties were found: [{string.Join(", ", uninteracted.Order())}]. " +
                "Use Etc() to allow additional properties or add assertions for them.");
        }
    }

    private static void ValidatePageStructure(JsonElement root)
    {
        Assert.True(root.TryGetProperty("component", out _), "Not a valid Inertia response: missing 'component'.");
        Assert.True(root.TryGetProperty("props", out _), "Not a valid Inertia response: missing 'props'.");
        Assert.True(root.TryGetProperty("url", out _), "Not a valid Inertia response: missing 'url'.");
        Assert.True(root.TryGetProperty("version", out _), "Not a valid Inertia response: missing 'version'.");
    }

    private static bool TryGetNestedProperty(JsonElement root, string path, out JsonElement result)
    {
        result = root;
        var segments = path.Split('.');

        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out var index) && result.ValueKind == JsonValueKind.Array)
            {
                if (index < 0 || index >= result.GetArrayLength())
                    return false;

                result = result[index];
            }
            else if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty(segment, out var next))
            {
                result = next;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static bool JsonElementEquals(JsonElement element, object? expected)
    {
        if (expected is null)
            return element.ValueKind == JsonValueKind.Null;

        return expected switch
        {
            string s => element.ValueKind == JsonValueKind.String && element.GetString() == s,
            bool b => (element.ValueKind is JsonValueKind.True or JsonValueKind.False) && element.GetBoolean() == b,
            int or long or short or byte or sbyte =>
                element.ValueKind == JsonValueKind.Number && element.GetInt64() == Convert.ToInt64(expected),
            float or double or decimal =>
                element.ValueKind == JsonValueKind.Number && element.GetDecimal() == Convert.ToDecimal(expected),
            JsonElement je => element.GetRawText() == je.GetRawText(),
            _ => CompareAsSerialized(element, expected),
        };
    }

    private static bool CompareAsSerialized(JsonElement element, object expected)
    {
        var expectedJson = JsonSerializer.Serialize(expected, CamelCaseOptions);
        return element.GetRawText() == expectedJson;
    }

    private static IDictionary<string, IReadOnlyList<string>> ParseDeferredProps(JsonElement root)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>();

        if (!root.TryGetProperty("deferredProps", out var dp) || dp.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var group in dp.EnumerateObject())
        {
            var props = new List<string>();
            foreach (var prop in group.Value.EnumerateArray())
            {
                if (prop.GetString() is { } name)
                    props.Add(name);
            }
            result[group.Name] = props;
        }

        return result;
    }

    private static string? ExtractDataPageFromHtml(string html)
    {
        // v3 format: <script ... type="application/json">...JSON...</script>
        var scriptMarker = "<script";
        var scriptStart = html.IndexOf(scriptMarker, StringComparison.OrdinalIgnoreCase);
        if (scriptStart >= 0)
        {
            var typeMarker = "type=\"application/json\"";
            var tagEnd = html.IndexOf('>', scriptStart);
            if (tagEnd > scriptStart)
            {
                var tagContent = html[scriptStart..tagEnd];
                if (tagContent.Contains(typeMarker, StringComparison.OrdinalIgnoreCase))
                {
                    var jsonStart = tagEnd + 1;
                    var scriptClose = html.IndexOf("</script>", jsonStart, StringComparison.OrdinalIgnoreCase);
                    if (scriptClose > jsonStart)
                    {
                        var json = html[jsonStart..scriptClose];
                        if (!string.IsNullOrEmpty(json))
                            return json;
                    }
                }
            }
        }

        // v1/v2 fallback: data-page="..." (double-quoted, HTML-attribute-encoded)
        // Only match values that look like JSON (start with '{') to avoid matching
        // the v3 <script data-page="app"> attribute which contains an element ID.
        var marker = "data-page=\"";
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        if (start >= 0)
        {
            start += marker.Length;
            var end = html.IndexOf('"', start);
            if (end > start)
            {
                var encoded = html[start..end];
                var decoded = WebUtility.HtmlDecode(encoded);
                if (decoded.StartsWith('{'))
                    return decoded;
            }
        }

        // v1/v2 fallback: data-page='...' (single-quoted, HTML-attribute-encoded)
        marker = "data-page='";
        start = html.IndexOf(marker, StringComparison.Ordinal);
        if (start >= 0)
        {
            start += marker.Length;
            var end = html.IndexOf('\'', start);
            if (end > start)
            {
                var encoded = html[start..end];
                var decoded = WebUtility.HtmlDecode(encoded);
                if (decoded.StartsWith('{'))
                    return decoded;
            }
        }

        return null;
    }

    private static int CountObjectProperties(JsonElement element)
    {
        var count = 0;
        foreach (var _ in element.EnumerateObject())
            count++;
        return count;
    }

    private static string FormatValue(object? value) =>
        value is null ? "null" : JsonSerializer.Serialize(value, CamelCaseOptions);
}
