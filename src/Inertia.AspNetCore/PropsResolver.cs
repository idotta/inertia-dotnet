using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Resolves and filters Inertia props, collecting metadata for the page response.
/// Created per-response inside InertiaResponse. Not registered in DI.
/// Port of inertia-laravel PropsResolver.php.
/// </summary>
internal sealed class PropsResolver
{
    private readonly HttpContext _httpContext;
    private readonly string _component;
    private readonly bool _isPartial;
    private readonly bool _isInertia;
    private readonly string[]? _only;
    private readonly string[]? _except;
    private readonly string[] _resetProps;
    private readonly string[] _loadedOnceProps;
    private readonly bool _exposeSharedPropKeys;

    // Metadata collections (populated during resolution)
    private readonly Dictionary<string, List<string>> _deferredProps = new();
    private readonly List<string> _mergeProps = [];
    private readonly List<string> _prependProps = [];
    private readonly List<string> _deepMergeProps = [];
    private readonly List<string> _matchPropsOn = [];
    private readonly Dictionary<string, Dictionary<string, object?>> _scrollProps = new();
    private readonly Dictionary<string, Dictionary<string, object?>> _onceProps = new();
    private readonly List<string> _sharedPropKeys = [];

    internal PropsResolver(HttpContext httpContext, string component, bool exposeSharedPropKeys)
    {
        _httpContext = httpContext;
        _component = component;
        _exposeSharedPropKeys = exposeSharedPropKeys;

        var headers = httpContext.Request.Headers;
        _isPartial = headers[InertiaHeaderNames.PartialComponent].FirstOrDefault() == component;
        _isInertia = headers.ContainsKey(InertiaHeaderNames.Inertia);
        _only = ParseHeader(headers, InertiaHeaderNames.PartialOnly);
        _except = ParseHeader(headers, InertiaHeaderNames.PartialExcept);
        _resetProps = ParseHeader(headers, InertiaHeaderNames.Reset) ?? [];
        _loadedOnceProps = ParseHeader(headers, InertiaHeaderNames.ExceptOnceProps) ?? [];
    }

    /// <summary>
    /// Resolves shared and page props, collecting metadata.
    /// Returns (resolvedProps, pageMetadata).
    /// </summary>
    internal async Task<(Dictionary<string, object?> Props, PageMetadata Metadata)> ResolveAsync(
        IDictionary<string, object?> shared,
        IReadOnlyList<IInertiaPropertyProvider> sharedProviders,
        IDictionary<string, object?> props)
    {
        var resolvedShared = ResolveSharedProps(shared, sharedProviders);
        var merged = new Dictionary<string, object?>(resolvedShared);
        foreach (var (key, value) in props)
            merged[key] = value;

        var resolved = await ResolvePropsAsync(UnpackDotProps(merged));
        return (resolved, BuildMetadata());
    }

    /// <summary>
    /// Resolves shared property providers and collects shared prop keys.
    /// </summary>
    private Dictionary<string, object?> ResolveSharedProps(
        IDictionary<string, object?> shared,
        IReadOnlyList<IInertiaPropertyProvider> sharedProviders)
    {
        // Merge providers into the shared dict (providers are expanded by ResolvePropertyProviders)
        var combined = new Dictionary<string, object?>(shared);
        for (var i = 0; i < sharedProviders.Count; i++)
            combined[i.ToString()] = sharedProviders[i];

        var resolved = ResolvePropertyProviders(combined);

        if (!_exposeSharedPropKeys)
            return resolved;

        var keys = new HashSet<string>();
        foreach (var key in resolved.Keys)
        {
            var dotIndex = key.IndexOf('.');
            keys.Add(dotIndex >= 0 ? key[..dotIndex] : key);
        }
        _sharedPropKeys.AddRange(keys);

        return resolved;
    }

    /// <summary>
    /// Resolves IInertiaPropertyProvider instances in the props dictionary.
    /// Numeric-keyed providers are expanded; string-keyed entries pass through.
    /// </summary>
    private Dictionary<string, object?> ResolvePropertyProviders(IDictionary<string, object?> props)
    {
        RenderContext? context = null;
        var result = new Dictionary<string, object?>();

        foreach (var (key, value) in props)
        {
            if (int.TryParse(key, out _) && value is IInertiaPropertyProvider provider)
            {
                context ??= new RenderContext(_component, _httpContext);
                foreach (var kvp in provider.ToInertiaProperties(context))
                    result[kvp.Key] = kvp.Value;
            }
            else
            {
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Recursively resolves the props tree, collecting metadata along the way.
    /// </summary>
    private async Task<Dictionary<string, object?>> ResolvePropsAsync(
        IDictionary<string, object?> props, string prefix = "", bool parentWasResolved = false)
    {
        props = ResolvePropertyProviders(props);
        var result = new Dictionary<string, object?>();

        foreach (var (key, value) in props)
        {
            var path = prefix == "" ? key : $"{prefix}.{key}";
            var prop = value;

            // On partial requests, only include matching paths. AlwaysProp and
            // children of resolved values bypass this filter.
            if (!ShouldIncludeInPartialResponse(prop, path, parentWasResolved))
                continue;

            // On initial loads, certain prop types are excluded before resolution.
            if (!_isPartial && ExcludeFromInitialResponse(prop, path))
                continue;

            var resolved = await ResolveValueAsync(prop, path, props);

            // A closure may return a prop type. When this happens, unwrap one
            // level so the prop type can participate in filtering and metadata.
            if (!ReferenceEquals(resolved, prop) && IsPropType(resolved))
            {
                prop = resolved;

                if (!_isPartial && ExcludeFromInitialResponse(prop, path))
                    continue;

                resolved = await ResolveValueAsync(prop, path, props);
            }

            CollectMetadata(prop, path);

            // When resolved value is a dictionary, recurse into it.
            // PHP's is_array matches both indexed and associative arrays;
            // in C# only IDictionary<string, object?> is a prop tree node.
            if (resolved is IDictionary<string, object?> dict)
            {
                result[key] = await ResolvePropsAsync(dict, path,
                    parentWasResolved || prop is not IDictionary<string, object?>);
            }
            else
            {
                result[key] = resolved;
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves a single prop value through the resolution pipeline.
    /// </summary>
    private async Task<object?> ResolveValueAsync(object? value, string path, IDictionary<string, object?> siblings)
    {
        if (value is IScrollPropInternal scrollProp)
            scrollProp.ConfigureMergeIntent(_httpContext.Request);

        value = await ResolveCallableAsync(value);

        if (value is IInertiaPropertyValueProvider pvp)
        {
            var roSiblings = siblings is IReadOnlyDictionary<string, object?> ro
                ? ro
                : new Dictionary<string, object?>(siblings);
            value = pvp.ToInertiaProperty(new PropertyContext(path, (IReadOnlyDictionary<string, object?>)roSiblings, _httpContext));
        }

        return value;
    }

    /// <summary>
    /// Resolves a callable value — either an IResolvableProp or a raw Delegate.
    /// </summary>
    private static async Task<object?> ResolveCallableAsync(object? value)
    {
        if (value is null or string)
            return value;

        if (value is IResolvableProp prop)
            return await prop.ResolveAsObjectAsync();

        if (value is Delegate d
            && d.Method.GetParameters().Length == 0
            && d.Method.ReturnType != typeof(void))
        {
            var result = d.DynamicInvoke();
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var taskType = task.GetType();
                return taskType.IsGenericType
                    ? taskType.GetProperty("Result")!.GetValue(task)
                    : null;
            }
            return result;
        }

        if (value is Delegate unresolvable)
        {
            var method = unresolvable.Method;
            throw new InvalidOperationException(
                $"Inertia props do not support delegates with parameters or void return types. " +
                $"Found: {method.DeclaringType?.Name ?? "?"}.{method.Name} " +
                $"with {method.GetParameters().Length} parameter(s) and return type {method.ReturnType.Name}.");
        }

        return value;
    }

    /// <summary>
    /// Determines if the value is a prop type requiring filtering or metadata collection.
    /// </summary>
    private static bool IsPropType(object? value) =>
        value is IAlwaysProp or IDeferrable or IIgnoreFirstLoad or IMergeable or IOnceable;

    /// <summary>
    /// Determines if a prop should be included in a partial response.
    /// AlwaysProp and children of resolved values bypass partial filtering.
    /// </summary>
    private bool ShouldIncludeInPartialResponse(object? prop, string path, bool parentWasResolved)
    {
        if (!_isPartial || prop is IAlwaysProp || parentWasResolved)
            return true;

        return PathMatchesPartialRequest(path);
    }

    /// <summary>
    /// Determines if a prop should be excluded from the initial page response.
    /// Each exclusion type collects its metadata before the prop is removed.
    /// </summary>
    private bool ExcludeFromInitialResponse(object? prop, string path)
    {
        if (prop is IIgnoreFirstLoad)
            return ExcludeIgnoredProp(prop, path);

        if (prop is IDeferrable { ShouldDefer: true } deferrable)
            return ExcludeDeferredProp(deferrable, prop, path);

        if (_isInertia && WasAlreadyLoadedByClient(prop, path))
            return ExcludeAlreadyLoadedProp(prop, path);

        return false;
    }

    private bool ExcludeIgnoredProp(object? prop, string path)
    {
        if (prop is IDeferrable { ShouldDefer: true } deferrable
            && !WasAlreadyLoadedByClient(prop, path))
        {
            CollectDeferredPropMetadata(path, deferrable);
        }

        if (prop is IMergeable { ShouldMerge: true } mergeable)
            CollectMergeableMetadata(path, mergeable);

        if (prop is IOnceable { ShouldResolveOnce: true })
            CollectOnceMetadata(path, prop);

        return true;
    }

    private bool ExcludeDeferredProp(IDeferrable deferrable, object? prop, string path)
    {
        CollectDeferredPropMetadata(path, deferrable);

        if (prop is IMergeable { ShouldMerge: true } mergeable)
            CollectMergeableMetadata(path, mergeable);

        return true;
    }

    private bool ExcludeAlreadyLoadedProp(object? prop, string path)
    {
        CollectOnceMetadata(path, prop);
        return true;
    }

    private bool WasAlreadyLoadedByClient(object? prop, string path)
    {
        return prop is IOnceable onceable
            && onceable.ShouldResolveOnce
            && !onceable.ShouldBeRefreshed
            && _loadedOnceProps.Contains(onceable.Key ?? path);
    }

    /// <summary>
    /// Collects metadata for a prop that will be included in the response.
    /// </summary>
    private void CollectMetadata(object? prop, string path)
    {
        if (prop is IMergeable { ShouldMerge: true } mergeable)
            CollectMergeableMetadata(path, mergeable);

        if (prop is IScrollPropInternal scrollProp)
            CollectScrollMetadata(path, scrollProp);

        if (prop is IOnceable { ShouldResolveOnce: true })
            CollectOnceMetadata(path, prop);
    }

    private void CollectDeferredPropMetadata(string path, IDeferrable prop)
    {
        if (!_deferredProps.TryGetValue(prop.Group, out var list))
        {
            list = [];
            _deferredProps[prop.Group] = list;
        }
        list.Add(path);
    }

    private void CollectMergeableMetadata(string path, IMergeable prop)
    {
        if (_resetProps.Contains(path))
            return;

        if (_isPartial && !IsIncludedInPartialMetadata(path))
            return;

        if (prop.ShouldDeepMerge)
        {
            _deepMergeProps.Add(path);
        }
        else if (prop.AppendsAtRoot)
        {
            _mergeProps.Add(path);
        }
        else if (prop.PrependsAtRoot)
        {
            _prependProps.Add(path);
        }
        else
        {
            foreach (var appendPath in prop.AppendsAtPaths)
                _mergeProps.Add($"{path}.{appendPath}");
            foreach (var prependPath in prop.PrependsAtPaths)
                _prependProps.Add($"{path}.{prependPath}");
        }

        foreach (var strategy in prop.MatchesOn)
            _matchPropsOn.Add($"{path}.{strategy}");
    }

    private void CollectScrollMetadata(string path, IScrollPropInternal prop)
    {
        var metadata = new Dictionary<string, object?>(prop.Metadata())
        {
            ["reset"] = _resetProps.Contains(path),
        };
        _scrollProps[path] = metadata;
    }

    private void CollectOnceMetadata(string path, object? prop)
    {
        if (prop is not IOnceable { ShouldResolveOnce: true } onceable)
            return;

        if (_isPartial && !IsIncludedInPartialMetadata(path))
            return;

        _onceProps[onceable.Key ?? path] = new Dictionary<string, object?>
        {
            ["prop"] = path,
            ["expiresAt"] = onceable.ExpiresAt,
        };
    }

    private bool IsIncludedInPartialMetadata(string path)
    {
        if (_only is not null && !MatchesOnly(path))
            return false;

        if (_except is not null && MatchesExcept(path))
            return false;

        return true;
    }

    /// <summary>
    /// Determines if the path matches the current partial request using
    /// bidirectional prefix matching on the only/except headers.
    /// </summary>
    private bool PathMatchesPartialRequest(string path)
    {
        if (_only is not null && !MatchesOnly(path) && !LeadsToOnly(path))
            return false;

        if (_except is not null && MatchesExcept(path))
            return false;

        return true;
    }

    private bool MatchesOnly(string path)
    {
        foreach (var onlyPath in _only!)
        {
            if (path == onlyPath || path.StartsWith($"{onlyPath}."))
                return true;
        }
        return false;
    }

    private bool LeadsToOnly(string path)
    {
        foreach (var onlyPath in _only!)
        {
            if (onlyPath.StartsWith($"{path}."))
                return true;
        }
        return false;
    }

    private bool MatchesExcept(string path)
    {
        foreach (var exceptPath in _except!)
        {
            if (path == exceptPath || path.StartsWith($"{exceptPath}."))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Unpacks top-level dot-notation keys into nested dictionaries.
    /// </summary>
    private Dictionary<string, object?> UnpackDotProps(IDictionary<string, object?> props)
    {
        var result = new Dictionary<string, object?>(props);

        foreach (var key in props.Keys.ToList())
        {
            if (!key.Contains('.'))
                continue;

            var value = result[key];

            // Resolve closures on dotted keys before nesting
            if (value is Delegate d
                && d.Method.GetParameters().Length == 0
                && d.Method.ReturnType != typeof(void))
            {
                value = d.DynamicInvoke();
            }
            else if (value is Delegate)
            {
                throw new InvalidOperationException(
                    $"Inertia props do not support delegates with parameters or void return types for dotted key '{key}'.");
            }

            EnsurePathIsTraversable(result, key);
            SetNestedValue(result, key, value);
            result.Remove(key);
        }

        return result;
    }

    /// <summary>
    /// Resolves closures along intermediate segments of a dot-notation path
    /// so nested set can traverse into them.
    /// </summary>
    private static void EnsurePathIsTraversable(Dictionary<string, object?> props, string dotKey)
    {
        var segments = dotKey.Split('.');
        IDictionary<string, object?> current = props;

        // Walk all segments except the last (which is the target key)
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (!current.TryGetValue(segment, out var existing))
                return;

            // Resolve closures along the path
            if (existing is Delegate d
                && d.Method.GetParameters().Length == 0
                && d.Method.ReturnType != typeof(void))
            {
                existing = d.DynamicInvoke();
                current[segment] = existing;
            }
            else if (existing is Delegate)
            {
                throw new InvalidOperationException(
                    $"Inertia props do not support delegates with parameters or void return types at path segment '{segment}'.");
            }

            if (existing is not IDictionary<string, object?> dict)
                return;

            current = dict;
        }
    }

    /// <summary>
    /// Sets a value at a dot-notation path in a nested dictionary.
    /// Creates intermediate dictionaries as needed.
    /// </summary>
    private static void SetNestedValue(Dictionary<string, object?> root, string dotKey, object? value)
    {
        var segments = dotKey.Split('.');
        IDictionary<string, object?> current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (!current.TryGetValue(segment, out var existing) || existing is not IDictionary<string, object?> dict)
            {
                dict = new Dictionary<string, object?>();
                current[segment] = dict;
            }
            current = dict;
        }

        current[segments[^1]] = value;
    }

    /// <summary>
    /// Builds the non-empty metadata for the page response.
    /// </summary>
    private PageMetadata BuildMetadata()
    {
        return new PageMetadata
        {
            SharedProps = _sharedPropKeys.Count > 0 ? _sharedPropKeys.ToList() : null,
            MergeProps = _mergeProps.Count > 0 ? _mergeProps.ToList() : null,
            PrependProps = _prependProps.Count > 0 ? _prependProps.ToList() : null,
            DeepMergeProps = _deepMergeProps.Count > 0 ? _deepMergeProps.ToList() : null,
            MatchPropsOn = _matchPropsOn.Count > 0 ? _matchPropsOn.ToList() : null,
            DeferredProps = _deferredProps.Count > 0
                ? _deferredProps.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyList<string>)kvp.Value.ToList())
                : null,
            ScrollProps = _scrollProps.Count > 0
                ? _scrollProps.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object?)kvp.Value)
                : null,
            OnceProps = _onceProps.Count > 0
                ? _onceProps.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object?)kvp.Value)
                : null,
        };
    }

    /// <summary>
    /// Parses a comma-separated header value into an array.
    /// </summary>
    private static string[]? ParseHeader(IHeaderDictionary headers, string key)
    {
        var value = headers[key].FirstOrDefault();
        if (string.IsNullOrEmpty(value))
            return null;

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts : null;
    }
}

/// <summary>
/// Holds the resolved page metadata from PropsResolver.
/// </summary>
internal sealed class PageMetadata
{
    public IReadOnlyList<string>? SharedProps { get; init; }
    public IReadOnlyList<string>? MergeProps { get; init; }
    public IReadOnlyList<string>? PrependProps { get; init; }
    public IReadOnlyList<string>? DeepMergeProps { get; init; }
    public IReadOnlyList<string>? MatchPropsOn { get; init; }
    public IDictionary<string, IReadOnlyList<string>>? DeferredProps { get; init; }
    public IDictionary<string, object?>? ScrollProps { get; init; }
    public IDictionary<string, object?>? OnceProps { get; init; }
}
