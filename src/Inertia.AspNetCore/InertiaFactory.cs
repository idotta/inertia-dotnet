using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore;

/// <summary>
/// Scoped implementation of <see cref="IInertia"/>. Manages per-request shared state.
/// </summary>
internal sealed class InertiaFactory : IInertia
{
    private readonly InertiaOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly Dictionary<string, object?> _sharedProps = [];
    private readonly List<IInertiaPropertyProvider> _sharedProviders = [];
    private string? _version;
    private string? _rootView;
    private bool _clearHistory;
    private bool _preserveFragment;
    private bool? _encryptHistory;

    public InertiaFactory(
        IOptions<InertiaOptions> options,
        IHttpContextAccessor httpContextAccessor,
        ITempDataDictionaryFactory tempDataFactory)
    {
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _tempDataFactory = tempDataFactory;
    }

    /// <inheritdoc />
    public InertiaResponse Render(string component, object? props = null)
    {
        var propsDict = props switch
        {
            null => [],
            IDictionary<string, object?> d => new Dictionary<string, object?>(d),
            _ => ObjectToDictionary(props),
        };
        return Render(component, propsDict);
    }

    /// <inheritdoc />
    public InertiaResponse Render(string component, IDictionary<string, object?> props)
    {
        ArgumentException.ThrowIfNullOrEmpty(component);
        ArgumentNullException.ThrowIfNull(props);

        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available.");

        var version = _version ?? _options.VersionProvider?.Invoke(httpContext) ?? "";
        var rootView = _rootView ?? _options.RootViewProvider?.Invoke(httpContext) ?? _options.RootView;
        var encryptHistory = _encryptHistory ?? _options.EncryptHistory;

        return new InertiaResponse(
            component: component,
            props: props,
            sharedProps: new Dictionary<string, object?>(_sharedProps),
            sharedProviders: _sharedProviders.ToList(),
            rootView: rootView,
            version: version,
            encryptHistory: encryptHistory,
            clearHistory: _clearHistory,
            preserveFragment: _preserveFragment,
            flash: GetFlashedInternal(httpContext),
            exposeSharedPropKeys: _options.ExposeSharedPropKeys,
            jsonOptions: _options.JsonSerializerOptions);
    }

    /// <inheritdoc />
    public InertiaLocationResult Location(string url) => new(url);

    /// <inheritdoc />
    public void Share(string key, object? value) => _sharedProps[key] = value;

    /// <inheritdoc />
    public void Share(IDictionary<string, object?> props)
    {
        foreach (var (key, value) in props)
            _sharedProps[key] = value;
    }

    /// <inheritdoc />
    public void Share(IInertiaPropertyProvider provider) => _sharedProviders.Add(provider);

    /// <inheritdoc />
    public void Flash(string key, object? value)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available.");
        var tempData = _tempDataFactory.GetTempData(httpContext);
        var flashData = GetFlashDictFromTempData(tempData);
        flashData[key] = value;
        SetFlashDictToTempData(tempData, flashData);
    }

    /// <inheritdoc />
    public void Flash(IDictionary<string, object?> data)
    {
        foreach (var (key, value) in data)
            Flash(key, value);
    }

    /// <inheritdoc />
    public IDictionary<string, object?> GetFlashed()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return new Dictionary<string, object?>();
        return GetFlashedInternal(httpContext) ?? new Dictionary<string, object?>();
    }

    /// <inheritdoc />
    public void ClearHistory() => _clearHistory = true;

    /// <inheritdoc />
    public void PreserveFragment() => _preserveFragment = true;

    /// <inheritdoc />
    public void EncryptHistory(bool encrypt = true) => _encryptHistory = encrypt;

    // -- Internal methods for middleware --

    internal IDictionary<string, object?> GetShared() => new Dictionary<string, object?>(_sharedProps);

    internal IReadOnlyList<IInertiaPropertyProvider> GetSharedProviders() => _sharedProviders;

    internal void FlushShared()
    {
        _sharedProps.Clear();
        _sharedProviders.Clear();
    }

    internal void SetVersion(string version) => _version = version;

    internal void SetRootView(string rootView) => _rootView = rootView;

    internal string GetVersion() => _version ?? "";

    internal string GetRootView() => _rootView ?? _options.RootView;

    internal bool GetClearHistory() => _clearHistory;

    internal bool GetPreserveFragment() => _preserveFragment;

    internal bool GetEncryptHistory() => _encryptHistory ?? _options.EncryptHistory;

    private IDictionary<string, object?>? GetFlashedInternal(HttpContext httpContext)
    {
        var tempData = _tempDataFactory.GetTempData(httpContext);
        if (!tempData.ContainsKey(InertiaSessionKeys.FlashData)) return null;
        return GetFlashDictFromTempData(tempData);
    }

    private static Dictionary<string, object?> GetFlashDictFromTempData(ITempDataDictionary tempData)
    {
        if (tempData.TryGetValue(InertiaSessionKeys.FlashData, out var existing) && existing is string json)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? [];
        }
        return [];
    }

    private static void SetFlashDictToTempData(ITempDataDictionary tempData, Dictionary<string, object?> data)
    {
        tempData[InertiaSessionKeys.FlashData] = JsonSerializer.Serialize(data);
    }

    private static Dictionary<string, object?> ObjectToDictionary(object obj)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead)
                dict[prop.Name] = prop.GetValue(obj);
        }
        return dict;
    }
}
