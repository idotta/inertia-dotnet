namespace Inertia.Core.Properties;

/// <summary>
/// Represents a property that is always included in Inertia responses,
/// bypassing partial reload filtering.
/// </summary>
/// <remarks>
/// Always props are included in every response, regardless of whether a partial reload
/// is requested. This is useful for props that should always be fresh, such as
/// authentication state, flash messages, or frequently changing data.
/// 
/// The value can be static or provided via a callback that is evaluated on each request.
/// </remarks>
public class AlwaysProp
{
    private readonly object? _value;
    private readonly Func<object?>? _callback;
    private readonly Func<Task<object?>>? _asyncCallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlwaysProp"/> class with a static value.
    /// </summary>
    /// <param name="value">The static value to include.</param>
    public AlwaysProp(object? value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlwaysProp"/> class with a synchronous callback.
    /// </summary>
    /// <param name="callback">The callback function that returns the value.</param>
    public AlwaysProp(Func<object?> callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlwaysProp"/> class with an asynchronous callback.
    /// </summary>
    /// <param name="asyncCallback">The async callback function that returns the value.</param>
    public AlwaysProp(Func<Task<object?>> asyncCallback)
    {
        _asyncCallback = asyncCallback ?? throw new ArgumentNullException(nameof(asyncCallback));
    }

    /// <summary>
    /// Resolves the property value by evaluating the callback or returning the static value.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the resolved value.</returns>
    public async Task<object?> ResolveAsync()
    {
        if (_asyncCallback != null)
        {
            return await _asyncCallback();
        }

        if (_callback != null)
        {
            return _callback();
        }

        return _value;
    }

    /// <summary>
    /// Gets a value indicating whether this property uses a callback.
    /// </summary>
    public bool IsCallable => _callback != null || _asyncCallback != null;
}
