using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// A property for paginated/infinite scroll data with merge capabilities.
/// Extends <see cref="MergeablePropBase"/> and implements <see cref="IDeferrable"/>.
/// Constructor auto-sets merge to true. Resolve caches result after first call.
/// </summary>
/// <typeparam name="T">The type of the scroll data value.</typeparam>
public sealed class ScrollProp<T> : MergeablePropBase, IDeferrable, IResolvableProp<T>, IScrollPropInternal
{
    private readonly T? _value;
    private readonly Func<T>? _syncCallback;
    private readonly Func<Task<T>>? _asyncCallback;
    private T? _resolved;
    private bool _hasResolved;
    private readonly string _wrapper;
    private readonly IScrollMetadataProvider? _metadata;
    private readonly Func<object?, IScrollMetadataProvider>? _metadataFactory;
    private DeferInfo _defer;

    /// <summary>Initializes a new instance with a direct value.</summary>
    public ScrollProp(T value, string wrapper = "data", IScrollMetadataProvider? metadata = null)
    {
        _value = value;
        _wrapper = wrapper;
        _metadata = metadata;
        _defer = new DeferInfo();
        Merge();
    }

    /// <summary>Initializes a new instance with a synchronous callback.</summary>
    public ScrollProp(Func<T> callback, string wrapper = "data", IScrollMetadataProvider? metadata = null)
    {
        _syncCallback = callback;
        _wrapper = wrapper;
        _metadata = metadata;
        _defer = new DeferInfo();
        Merge();
    }

    /// <summary>Initializes a new instance with a synchronous callback and metadata factory.</summary>
    public ScrollProp(Func<T> callback, string wrapper, Func<object?, IScrollMetadataProvider> metadataFactory)
    {
        _syncCallback = callback;
        _wrapper = wrapper;
        _metadataFactory = metadataFactory;
        _defer = new DeferInfo();
        Merge();
    }

    /// <summary>Initializes a new instance with an asynchronous callback.</summary>
    public ScrollProp(Func<Task<T>> asyncCallback, string wrapper = "data", IScrollMetadataProvider? metadata = null)
    {
        _asyncCallback = asyncCallback;
        _wrapper = wrapper;
        _metadata = metadata;
        _defer = new DeferInfo();
        Merge();
    }

    /// <summary>Resolves the value, caching after first call.</summary>
    public async Task<T> ResolveAsync()
    {
        if (!_hasResolved)
        {
            if (_asyncCallback is not null)
                _resolved = await _asyncCallback();
            else if (_syncCallback is not null)
                _resolved = _syncCallback();
            else
                _resolved = _value;
            _hasResolved = true;
        }
        return _resolved!;
    }

    /// <inheritdoc />
    async Task<object?> IResolvableProp.ResolveAsObjectAsync() => await ResolveAsync();

    // IDeferrable (explicit)
    bool IDeferrable.ShouldDefer => _defer.ShouldDefer;
    string IDeferrable.Group => _defer.Group;

    // IScrollPropInternal (explicit — return type differs from public fluent API)
    void IScrollPropInternal.ConfigureMergeIntent(HttpRequest? request) => ConfigureMergeIntent(request);
    IDictionary<string, object?> IScrollPropInternal.Metadata() => Metadata();

    /// <summary>Mark as deferred, optionally in a specific group.</summary>
    public ScrollProp<T> Defer(string? group = null) { _defer.Defer(group); return this; }

    /// <summary>
    /// Configures merge intent based on the <c>X-Inertia-Infinite-Scroll-Merge-Intent</c> header.
    /// If header is "prepend", prepends at wrapper path. Otherwise appends at wrapper path.
    /// </summary>
    public ScrollProp<T> ConfigureMergeIntent(HttpRequest? request = null)
    {
        var intent = request?.Headers[InertiaHeaderNames.InfiniteScrollMergeIntent].FirstOrDefault();
        if (intent == "prepend")
            Prepend(_wrapper);
        else
            Append(_wrapper);
        return this;
    }

    /// <summary>Returns scroll metadata as a dictionary.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no metadata provider or factory is configured.</exception>
    public IDictionary<string, object?> Metadata()
    {
        var provider = ResolveMetadataProvider();
        return new Dictionary<string, object?>
        {
            ["pageName"] = provider.PageName,
            ["previousPage"] = provider.PreviousPage,
            ["nextPage"] = provider.NextPage,
            ["currentPage"] = provider.CurrentPage,
        };
    }

    private IScrollMetadataProvider ResolveMetadataProvider()
    {
        if (_metadata is not null) return _metadata;
        if (_metadataFactory is not null)
        {
            // Resolve synchronously for metadata — the value should already be cached
            if (!_hasResolved)
            {
                if (_syncCallback is not null)
                    _resolved = _syncCallback();
                else
                    _resolved = _value;
                _hasResolved = true;
            }
            return _metadataFactory(_resolved);
        }
        throw new InvalidOperationException("No scroll metadata provider configured. Provide an IScrollMetadataProvider or a metadata factory.");
    }

    /// <summary>Enables merging with existing client-side data.</summary>
    public new ScrollProp<T> Merge() { base.Merge(); return this; }
    /// <summary>Enables deep merging, which also enables regular merging.</summary>
    public new ScrollProp<T> DeepMerge() { base.DeepMerge(); return this; }
    /// <summary>Sets the match-on path used for merge identity matching.</summary>
    public new ScrollProp<T> MatchOn(string matchOn) { base.MatchOn(matchOn); return this; }
    /// <summary>Sets the match-on paths used for merge identity matching.</summary>
    public new ScrollProp<T> MatchOn(IEnumerable<string> matchOn) { base.MatchOn(matchOn); return this; }
    /// <summary>Sets the append flag.</summary>
    public new ScrollProp<T> Append(bool value = true) { base.Append(value); return this; }
    /// <summary>Adds a specific path where values should be appended during merging.</summary>
    public new ScrollProp<T> Append(string path, string? matchOn = null) { base.Append(path, matchOn); return this; }
    /// <summary>Adds multiple paths where values should be appended during merging.</summary>
    public new ScrollProp<T> Append(IEnumerable<string> paths) { base.Append(paths); return this; }
    /// <summary>Sets the prepend flag by inverting the value.</summary>
    public new ScrollProp<T> Prepend(bool value = true) { base.Prepend(value); return this; }
    /// <summary>Adds a specific path where values should be prepended during merging.</summary>
    public new ScrollProp<T> Prepend(string path, string? matchOn = null) { base.Prepend(path, matchOn); return this; }
    /// <summary>Adds multiple paths where values should be prepended during merging.</summary>
    public new ScrollProp<T> Prepend(IEnumerable<string> paths) { base.Prepend(paths); return this; }
}
