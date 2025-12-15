namespace Inertia.Core.Properties;

/// <summary>
/// Represents a property that is loaded only when explicitly requested via partial reloads.
/// </summary>
/// <remarks>
/// Optional props (formerly known as lazy props) implement the <see cref="IIgnoreFirstLoad"/> interface,
/// which means they are not included in the initial page load but are loaded when the client
/// explicitly requests them via the X-Inertia-Partial-Data header.
/// 
/// This is useful for reducing the initial payload size by deferring non-critical data
/// until it's actually needed.
/// 
/// Optional props can also be marked as "once" props, which means they are resolved once
/// and cached across navigations.
/// </remarks>
public class OptionalProp : IIgnoreFirstLoad, IOnceable
{
    private readonly Func<object?>? _callback;
    private readonly Func<Task<object?>>? _asyncCallback;
    private bool _isOnce;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionalProp"/> class with a synchronous callback.
    /// </summary>
    /// <param name="callback">The callback function that returns the value.</param>
    public OptionalProp(Func<object?> callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionalProp"/> class with an asynchronous callback.
    /// </summary>
    /// <param name="asyncCallback">The async callback function that returns the value.</param>
    public OptionalProp(Func<Task<object?>> asyncCallback)
    {
        _asyncCallback = asyncCallback ?? throw new ArgumentNullException(nameof(asyncCallback));
    }

    /// <summary>
    /// Resolves the property value by evaluating the callback.
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

        return null;
    }

    /// <summary>
    /// Marks this property as a "once" prop, which means it will be resolved once
    /// and cached across navigations.
    /// </summary>
    /// <returns>The current instance for method chaining.</returns>
    public OptionalProp Once()
    {
        _isOnce = true;
        return this;
    }

    /// <summary>
    /// Gets a value indicating whether this property should use once resolution semantics.
    /// </summary>
    /// <returns><c>true</c> if this property should be resolved once and cached; otherwise, <c>false</c>.</returns>
    public bool IsOnce() => _isOnce;
}
