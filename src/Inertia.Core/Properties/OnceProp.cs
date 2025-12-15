namespace Inertia.Core.Properties;

/// <summary>
/// Represents a property that is resolved once and cached across navigations.
/// </summary>
/// <remarks>
/// Once props are evaluated on the first request and then reused for subsequent navigations
/// within the same session. This is particularly useful for expensive operations or data
/// that doesn't change frequently, such as:
/// - Translations and localization data
/// - Configuration settings
/// - User permissions
/// - Dropdown options or reference data
/// 
/// The cached value persists until:
/// - The user visits a page that doesn't include this prop
/// - An explicit refresh is triggered
/// - The session expires
/// </remarks>
public class OnceProp : IOnceable
{
    private readonly Func<object?>? _callback;
    private readonly Func<Task<object?>>? _asyncCallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnceProp"/> class with a synchronous callback.
    /// </summary>
    /// <param name="callback">The callback function that returns the value.</param>
    public OnceProp(Func<object?> callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OnceProp"/> class with an asynchronous callback.
    /// </summary>
    /// <param name="asyncCallback">The async callback function that returns the value.</param>
    public OnceProp(Func<Task<object?>> asyncCallback)
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
    /// Gets a value indicating whether this property should use once resolution semantics.
    /// Always returns <c>true</c> for <see cref="OnceProp"/>.
    /// </summary>
    /// <returns>Always <c>true</c>.</returns>
    public bool IsOnce() => true;
}
