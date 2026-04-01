namespace Inertia.AspNetCore;

/// <summary>
/// A property that is merged with existing client-side data during partial reloads.
/// </summary>
public sealed class MergeProp<T> : MergeablePropBase, IOnceable, IResolvableProp<T>, IServiceResolvableProp
{
    private readonly T? _value;
    private readonly Func<T>? _syncCallback;
    private readonly Func<Task<T>>? _asyncCallback;
    private readonly Func<IServiceProvider, T>? _serviceCallback;
    private readonly Func<IServiceProvider, Task<T>>? _asyncServiceCallback;
    private readonly OnceInfo _once = new();

    /// <summary>Initializes a new <see cref="MergeProp{T}"/> with a scalar value. Merge is enabled by default.</summary>
    public MergeProp(T value) { _value = value; Merge(); }

    /// <summary>Initializes a new <see cref="MergeProp{T}"/> with a synchronous callback. Merge is enabled by default.</summary>
    public MergeProp(Func<T> callback) { _syncCallback = callback; Merge(); }

    /// <summary>Initializes a new <see cref="MergeProp{T}"/> with an asynchronous callback. Merge is enabled by default.</summary>
    public MergeProp(Func<Task<T>> asyncCallback) { _asyncCallback = asyncCallback; Merge(); }

    /// <summary>Initializes a new <see cref="MergeProp{T}"/> with a synchronous service-provider callback. Merge is enabled by default.</summary>
    /// <param name="serviceCallback">A function that receives an <see cref="IServiceProvider"/> and produces the value.</param>
    public MergeProp(Func<IServiceProvider, T> serviceCallback)
    {
        ArgumentNullException.ThrowIfNull(serviceCallback);
        _serviceCallback = serviceCallback;
        Merge();
    }

    /// <summary>Initializes a new <see cref="MergeProp{T}"/> with an asynchronous service-provider callback. Merge is enabled by default.</summary>
    /// <param name="asyncServiceCallback">An async function that receives an <see cref="IServiceProvider"/> and produces the value.</param>
    public MergeProp(Func<IServiceProvider, Task<T>> asyncServiceCallback)
    {
        ArgumentNullException.ThrowIfNull(asyncServiceCallback);
        _asyncServiceCallback = asyncServiceCallback;
        Merge();
    }

    /// <summary>Resolves the property value, awaiting async callbacks if present.</summary>
    public async Task<T> ResolveAsync()
    {
        if (_asyncCallback is not null) return await _asyncCallback().ConfigureAwait(false);
        if (_syncCallback is not null) return _syncCallback();
        return _value!;
    }

    /// <inheritdoc />
    async Task<object?> IResolvableProp.ResolveAsObjectAsync() => await ResolveAsync().ConfigureAwait(false);

    // IServiceResolvableProp (explicit interface implementation)
    bool IServiceResolvableProp.HasServiceCallback => _serviceCallback is not null || _asyncServiceCallback is not null;

    async Task<object?> IServiceResolvableProp.ResolveWithServiceAsync(IServiceProvider serviceProvider)
    {
        if (_asyncServiceCallback is not null) return await _asyncServiceCallback(serviceProvider).ConfigureAwait(false);
        if (_serviceCallback is not null) return _serviceCallback(serviceProvider);
        return await ResolveAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    bool IOnceable.ShouldResolveOnce => _once.ShouldResolveOnce;
    /// <inheritdoc />
    bool IOnceable.ShouldBeRefreshed => _once.ShouldBeRefreshed;
    /// <inheritdoc />
    string? IOnceable.Key => _once.Key;
    /// <inheritdoc />
    long? IOnceable.ExpiresAt => _once.ExpiresAt;

    /// <summary>Enables merging with existing client-side data.</summary>
    public new MergeProp<T> Merge() { base.Merge(); return this; }
    /// <summary>Enables deep merging, which also enables regular merging.</summary>
    public new MergeProp<T> DeepMerge() { base.DeepMerge(); return this; }
    /// <summary>Sets the match-on path used for merge identity matching.</summary>
    public new MergeProp<T> MatchOn(string matchOn) { base.MatchOn(matchOn); return this; }
    /// <summary>Sets the match-on paths used for merge identity matching.</summary>
    public new MergeProp<T> MatchOn(IEnumerable<string> matchOn) { base.MatchOn(matchOn); return this; }
    /// <summary>Sets the append flag.</summary>
    public new MergeProp<T> Append(bool value = true) { base.Append(value); return this; }
    /// <summary>Adds a specific path where values should be appended during merging.</summary>
    public new MergeProp<T> Append(string path, string? matchOn = null) { base.Append(path, matchOn); return this; }
    /// <summary>Adds multiple paths where values should be appended during merging.</summary>
    public new MergeProp<T> Append(IEnumerable<string> paths) { base.Append(paths); return this; }
    /// <summary>Adds paths with associated match-on keys for appending during merging.</summary>
    public new MergeProp<T> Append(IDictionary<string, string> pathsWithMatchOn) { base.Append(pathsWithMatchOn); return this; }
    /// <summary>Sets the prepend flag by inverting the value.</summary>
    public new MergeProp<T> Prepend(bool value = true) { base.Prepend(value); return this; }
    /// <summary>Adds a specific path where values should be prepended during merging.</summary>
    public new MergeProp<T> Prepend(string path, string? matchOn = null) { base.Prepend(path, matchOn); return this; }
    /// <summary>Adds multiple paths where values should be prepended during merging.</summary>
    public new MergeProp<T> Prepend(IEnumerable<string> paths) { base.Prepend(paths); return this; }
    /// <summary>Adds paths with associated match-on keys for prepending during merging.</summary>
    public new MergeProp<T> Prepend(IDictionary<string, string> pathsWithMatchOn) { base.Prepend(pathsWithMatchOn); return this; }

    /// <summary>Marks the property to be resolved only once and cached.</summary>
    public MergeProp<T> Once(bool value = true) { _once.Once(value); return this; }
    /// <summary>Sets a custom cache key using a string.</summary>
    public MergeProp<T> As(string key) { _once.As(key); return this; }
    /// <summary>Sets a custom cache key using an enum value.</summary>
    public MergeProp<T> As(Enum key) { _once.As(key); return this; }
    /// <summary>Marks the cached value to be forcefully refreshed.</summary>
    public MergeProp<T> Fresh(bool value = true) { _once.Fresh(value); return this; }
    /// <summary>Sets the time-to-live for the cached value.</summary>
    public MergeProp<T> Until(TimeSpan delay) { _once.Until(delay); return this; }
    /// <summary>Sets the time-to-live for the cached value in seconds.</summary>
    public MergeProp<T> Until(int seconds) { _once.Until(seconds); return this; }
    /// <summary>Sets the expiration time as an absolute UTC timestamp.</summary>
    public MergeProp<T> Until(DateTimeOffset expiresAt) { _once.Until(expiresAt); return this; }
}
