namespace Inertia.AspNetCore;

/// <summary>
/// A property that is always included in Inertia responses, even during partial reloads.
/// </summary>
public sealed class AlwaysProp<T>
{
    private readonly T? _value;
    private readonly Func<T>? _syncCallback;
    private readonly Func<Task<T>>? _asyncCallback;

    /// <summary>Initializes a new instance with a static value.</summary>
    /// <param name="value">The value to include in the response.</param>
    public AlwaysProp(T value) => _value = value;

    /// <summary>Initializes a new instance with a synchronous callback.</summary>
    /// <param name="callback">A function that produces the value.</param>
    public AlwaysProp(Func<T> callback) => _syncCallback = callback;

    /// <summary>Initializes a new instance with an asynchronous callback.</summary>
    /// <param name="asyncCallback">An async function that produces the value.</param>
    public AlwaysProp(Func<Task<T>> asyncCallback) => _asyncCallback = asyncCallback;

    /// <summary>Resolves the property value, awaiting async callbacks if present.</summary>
    public async Task<object?> ResolveAsync()
    {
        if (_asyncCallback is not null) return await _asyncCallback();
        if (_syncCallback is not null) return _syncCallback();
        return _value;
    }
}
