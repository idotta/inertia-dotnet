namespace Inertia.Core.Properties;

/// <summary>
/// Represents a property that should be merged with existing client-side data
/// instead of replacing it entirely.
/// </summary>
/// <remarks>
/// Merge props allow you to update or append to existing data on the client side
/// without replacing the entire dataset. This is particularly useful for:
/// - Infinite scroll / load more functionality
/// - Updating specific items in a list
/// - Incrementally loading data
/// 
/// Merge props support both shallow (top-level only) and deep (nested objects) merging.
/// You can also specify a path within the property to target specific nested data.
/// 
/// By default, merge behavior applies to all requests, but you can restrict it to
/// partial reloads only using the <see cref="OnlyOnPartial"/> method.
/// </remarks>
public class MergeProp : IMergeable, IOnceable
{
    private readonly object? _value;
    private readonly Func<object?>? _callback;
    private readonly Func<Task<object?>>? _asyncCallback;
    private bool _isOnce;
    private string? _mergePath;
    private bool _isDeepMerge;
    private bool _onlyOnPartial;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeProp"/> class with a static value.
    /// </summary>
    /// <param name="value">The static value to merge.</param>
    public MergeProp(object? value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeProp"/> class with a synchronous callback.
    /// </summary>
    /// <param name="callback">The callback function that returns the value to merge.</param>
    public MergeProp(Func<object?> callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeProp"/> class with an asynchronous callback.
    /// </summary>
    /// <param name="asyncCallback">The async callback function that returns the value to merge.</param>
    public MergeProp(Func<Task<object?>> asyncCallback)
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
    /// Specifies the path within the property data where merging should occur.
    /// </summary>
    /// <param name="path">The dot-notation path to the merge location (e.g., "data.items").</param>
    /// <returns>The current instance for method chaining.</returns>
    public MergeProp WithPath(string path)
    {
        _mergePath = path ?? throw new ArgumentNullException(nameof(path));
        return this;
    }

    /// <summary>
    /// Configures this property to use deep merging, recursively merging nested objects.
    /// </summary>
    /// <returns>The current instance for method chaining.</returns>
    public MergeProp DeepMerge()
    {
        _isDeepMerge = true;
        return this;
    }

    /// <summary>
    /// Configures this property to only merge during partial reloads.
    /// On full page loads, the data will replace rather than merge.
    /// </summary>
    /// <returns>The current instance for method chaining.</returns>
    public MergeProp OnlyOnPartial()
    {
        _onlyOnPartial = true;
        return this;
    }

    /// <summary>
    /// Marks this property as a "once" prop, which means it will be resolved once
    /// and cached across navigations.
    /// </summary>
    /// <returns>The current instance for method chaining.</returns>
    public MergeProp Once()
    {
        _isOnce = true;
        return this;
    }

    /// <summary>
    /// Gets a value indicating whether this property should use once resolution semantics.
    /// </summary>
    /// <returns><c>true</c> if this property should be resolved once and cached; otherwise, <c>false</c>.</returns>
    public bool IsOnce() => _isOnce;

    /// <summary>
    /// Gets a value indicating whether this property should be merged with existing client data.
    /// Always returns <c>true</c> for <see cref="MergeProp"/>.
    /// </summary>
    /// <returns>Always <c>true</c>.</returns>
    public bool ShouldMerge() => true;

    /// <summary>
    /// Gets the path within the property data where merging should occur.
    /// </summary>
    /// <returns>
    /// The dot-notation path to the merge location (e.g., "data.items"), 
    /// or <c>null</c> to merge at the root level.
    /// </returns>
    public string? GetMergePath() => _mergePath;

    /// <summary>
    /// Gets a value indicating whether deep merging should be used.
    /// </summary>
    /// <returns>
    /// <c>true</c> for deep merge (recursively merge nested objects); 
    /// <c>false</c> for shallow merge (top-level only).
    /// </returns>
    public bool IsDeepMerge() => _isDeepMerge;

    /// <summary>
    /// Gets a value indicating whether merging should only occur during partial reloads.
    /// </summary>
    /// <returns>
    /// <c>true</c> if merge behavior should only apply to partial reloads; 
    /// <c>false</c> to merge on all requests.
    /// </returns>
    bool IMergeable.OnlyOnPartial() => _onlyOnPartial;
}
