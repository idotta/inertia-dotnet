namespace Inertia.AspNetCore;

/// <summary>
/// A property that is always included in Inertia responses, even during partial reloads.
/// </summary>
public sealed class AlwaysProp<T> : IAlwaysProp, IResolvableProp<T>, IServiceResolvableProp
{
    private readonly T? _value;
    private readonly Func<T>? _syncCallback;
    private readonly Func<Task<T>>? _asyncCallback;
    private readonly Func<IServiceProvider, T>? _serviceCallback;
    private readonly Func<IServiceProvider, Task<T>>? _asyncServiceCallback;

    /// <summary>Initializes a new instance with a static value.</summary>
    /// <param name="value">The value to include in the response.</param>
    public AlwaysProp(T value) => _value = value;

    /// <summary>Initializes a new instance with a synchronous callback.</summary>
    /// <param name="callback">A function that produces the value.</param>
    public AlwaysProp(Func<T> callback) => _syncCallback = callback;

    /// <summary>Initializes a new instance with an asynchronous callback.</summary>
    /// <param name="asyncCallback">An async function that produces the value.</param>
    public AlwaysProp(Func<Task<T>> asyncCallback) => _asyncCallback = asyncCallback;

    /// <summary>Initializes a new instance with a synchronous service-provider callback.</summary>
    /// <param name="serviceCallback">A function that receives an <see cref="IServiceProvider"/> and produces the value.</param>
    public AlwaysProp(Func<IServiceProvider, T> serviceCallback)
    {
        ArgumentNullException.ThrowIfNull(serviceCallback);
        _serviceCallback = serviceCallback;
    }

    /// <summary>Initializes a new instance with an asynchronous service-provider callback.</summary>
    /// <param name="asyncServiceCallback">An async function that receives an <see cref="IServiceProvider"/> and produces the value.</param>
    public AlwaysProp(Func<IServiceProvider, Task<T>> asyncServiceCallback)
    {
        ArgumentNullException.ThrowIfNull(asyncServiceCallback);
        _asyncServiceCallback = asyncServiceCallback;
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
}
