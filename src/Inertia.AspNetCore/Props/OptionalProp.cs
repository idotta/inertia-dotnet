namespace Inertia.AspNetCore;

/// <summary>
/// A property only included when explicitly requested via partial reloads.
/// </summary>
public sealed class OptionalProp<T> : IIgnoreFirstLoad, IOnceable
{
    private readonly Func<T>? _syncCallback;
    private readonly Func<Task<T>>? _asyncCallback;
    private readonly OnceInfo _once = new();

    /// <summary>Initializes a new instance with a synchronous callback.</summary>
    /// <param name="callback">A function that produces the value.</param>
    public OptionalProp(Func<T> callback) => _syncCallback = callback;

    /// <summary>Initializes a new instance with an asynchronous callback.</summary>
    /// <param name="asyncCallback">An async function that produces the value.</param>
    public OptionalProp(Func<Task<T>> asyncCallback) => _asyncCallback = asyncCallback;

    /// <summary>Resolves the property value, awaiting async callbacks if present.</summary>
    public async Task<object?> ResolveAsync()
    {
        if (_asyncCallback is not null) return await _asyncCallback();
        if (_syncCallback is not null) return _syncCallback();
        return default(T);
    }

    // IOnceable (explicit interface implementation)
    bool IOnceable.ShouldResolveOnce => _once.ShouldResolveOnce;
    bool IOnceable.ShouldBeRefreshed => _once.ShouldBeRefreshed;
    string? IOnceable.Key => _once.Key;
    long? IOnceable.ExpiresAt => _once.ExpiresAt;

    /// <summary>Sets whether this property should be resolved only once.</summary>
    public OptionalProp<T> Once(bool value = true) { _once.Once(value); return this; }

    /// <summary>Sets a custom cache key using a string.</summary>
    public OptionalProp<T> As(string key) { _once.As(key); return this; }

    /// <summary>Sets a custom cache key using an enum value.</summary>
    public OptionalProp<T> As(Enum key) { _once.As(key); return this; }

    /// <summary>Sets whether the cached value should be forcefully refreshed.</summary>
    public OptionalProp<T> Fresh(bool value = true) { _once.Fresh(value); return this; }

    /// <summary>Sets the time-to-live for the cached value.</summary>
    public OptionalProp<T> Until(TimeSpan delay) { _once.Until(delay); return this; }

    /// <summary>Sets the time-to-live for the cached value in seconds.</summary>
    public OptionalProp<T> Until(int seconds) { _once.Until(seconds); return this; }
}
