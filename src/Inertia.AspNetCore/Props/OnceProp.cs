namespace Inertia.AspNetCore;

/// <summary>
/// A property that is resolved only once and cached on the client.
/// </summary>
public sealed class OnceProp<T> : IOnceable, IResolvableProp<T>, IServiceResolvableProp
{
    private readonly Func<T>? _syncCallback;
    private readonly Func<Task<T>>? _asyncCallback;
    private readonly Func<IServiceProvider, T>? _serviceCallback;
    private readonly Func<IServiceProvider, Task<T>>? _asyncServiceCallback;
    private readonly OnceInfo _once = new();

    /// <summary>Initializes a new instance with a synchronous callback. Once-resolution is enabled by default.</summary>
    /// <param name="callback">A function that produces the value.</param>
    public OnceProp(Func<T> callback) { _syncCallback = callback; _once.Once(); }

    /// <summary>Initializes a new instance with an asynchronous callback. Once-resolution is enabled by default.</summary>
    /// <param name="asyncCallback">An async function that produces the value.</param>
    public OnceProp(Func<Task<T>> asyncCallback) { _asyncCallback = asyncCallback; _once.Once(); }

    /// <summary>Initializes a new instance with a synchronous service-provider callback. Once-resolution is enabled by default.</summary>
    /// <param name="serviceCallback">A function that receives an <see cref="IServiceProvider"/> and produces the value.</param>
    public OnceProp(Func<IServiceProvider, T> serviceCallback)
    {
        ArgumentNullException.ThrowIfNull(serviceCallback);
        _serviceCallback = serviceCallback;
        _once.Once();
    }

    /// <summary>Initializes a new instance with an asynchronous service-provider callback. Once-resolution is enabled by default.</summary>
    /// <param name="asyncServiceCallback">An async function that receives an <see cref="IServiceProvider"/> and produces the value.</param>
    public OnceProp(Func<IServiceProvider, Task<T>> asyncServiceCallback)
    {
        ArgumentNullException.ThrowIfNull(asyncServiceCallback);
        _asyncServiceCallback = asyncServiceCallback;
        _once.Once();
    }

    /// <summary>Resolves the property value, awaiting async callbacks if present.</summary>
    public async Task<T> ResolveAsync()
    {
        if (_asyncCallback is not null) return await _asyncCallback();
        if (_syncCallback is not null) return _syncCallback();
        return default!;
    }

    /// <inheritdoc />
    async Task<object?> IResolvableProp.ResolveAsObjectAsync() => await ResolveAsync();

    // IServiceResolvableProp (explicit interface implementation)
    bool IServiceResolvableProp.HasServiceCallback => _serviceCallback is not null || _asyncServiceCallback is not null;

    async Task<object?> IServiceResolvableProp.ResolveWithServiceAsync(IServiceProvider serviceProvider)
    {
        if (_asyncServiceCallback is not null) return await _asyncServiceCallback(serviceProvider);
        if (_serviceCallback is not null) return _serviceCallback(serviceProvider);
        return await ResolveAsync();
    }

    // IOnceable (explicit interface implementation)
    bool IOnceable.ShouldResolveOnce => _once.ShouldResolveOnce;
    bool IOnceable.ShouldBeRefreshed => _once.ShouldBeRefreshed;
    string? IOnceable.Key => _once.Key;
    long? IOnceable.ExpiresAt => _once.ExpiresAt;

    /// <summary>Sets whether this property should be resolved only once.</summary>
    public OnceProp<T> Once(bool value = true) { _once.Once(value); return this; }

    /// <summary>Sets a custom cache key using a string.</summary>
    public OnceProp<T> As(string key) { _once.As(key); return this; }

    /// <summary>Sets a custom cache key using an enum value.</summary>
    public OnceProp<T> As(Enum key) { _once.As(key); return this; }

    /// <summary>Sets whether the cached value should be forcefully refreshed.</summary>
    public OnceProp<T> Fresh(bool value = true) { _once.Fresh(value); return this; }

    /// <summary>Sets the time-to-live for the cached value.</summary>
    public OnceProp<T> Until(TimeSpan delay) { _once.Until(delay); return this; }

    /// <summary>Sets the time-to-live for the cached value in seconds.</summary>
    public OnceProp<T> Until(int seconds) { _once.Until(seconds); return this; }
    /// <summary>Sets the expiration time as an absolute UTC timestamp.</summary>
    public OnceProp<T> Until(DateTimeOffset expiresAt) { _once.Until(expiresAt); return this; }
}
