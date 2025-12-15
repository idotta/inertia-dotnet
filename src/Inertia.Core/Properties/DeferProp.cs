namespace Inertia.Core.Properties;

/// <summary>
/// Represents a property that is loaded asynchronously after the initial page render.
/// </summary>
/// <remarks>
/// Deferred props allow you to postpone loading of non-critical data until after the initial
/// page render, improving perceived performance. The client will make a follow-up request
/// to load deferred props after the initial page is displayed.
/// 
/// Deferred props can be organized into named groups, allowing you to control which props
/// are loaded together. They also support merging with existing client-side data.
/// 
/// Typical use cases:
/// - Analytics data or statistics
/// - Non-critical user activity feeds
/// - Secondary content that doesn't affect the initial view
/// - Heavy computations that can be delayed
/// </remarks>
public class DeferProp : IIgnoreFirstLoad, IMergeable, IOnceable
{
    private readonly Func<object?>? _callback;
    private readonly Func<Task<object?>>? _asyncCallback;
    private readonly string? _group;
    private bool _isOnce;
    private bool _shouldMerge;
    private string? _mergePath;
    private bool _isDeepMerge;
    private bool _onlyOnPartial;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeferProp"/> class with a synchronous callback.
    /// </summary>
    /// <param name="callback">The callback function that returns the value.</param>
    /// <param name="group">Optional group name for organizing deferred props.</param>
    public DeferProp(Func<object?> callback, string? group = null)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _group = group;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeferProp"/> class with an asynchronous callback.
    /// </summary>
    /// <param name="asyncCallback">The async callback function that returns the value.</param>
    /// <param name="group">Optional group name for organizing deferred props.</param>
    public DeferProp(Func<Task<object?>> asyncCallback, string? group = null)
    {
        _asyncCallback = asyncCallback ?? throw new ArgumentNullException(nameof(asyncCallback));
        _group = group;
    }

    /// <summary>
    /// Gets the group name for this deferred prop, if any.
    /// </summary>
    public string? Group => _group;

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
    public DeferProp Once()
    {
        _isOnce = true;
        return this;
    }

    /// <summary>
    /// Configures this property to be merged with existing client-side data.
    /// </summary>
    /// <param name="path">Optional dot-notation path to the merge location within the property.</param>
    /// <returns>The current instance for method chaining.</returns>
    public DeferProp Merge(string? path = null)
    {
        _shouldMerge = true;
        _mergePath = path;
        return this;
    }

    /// <summary>
    /// Configures this property to use deep merging, recursively merging nested objects.
    /// </summary>
    /// <returns>The current instance for method chaining.</returns>
    public DeferProp DeepMerge()
    {
        _shouldMerge = true;
        _isDeepMerge = true;
        return this;
    }

    /// <summary>
    /// Configures this property to only merge during partial reloads.
    /// </summary>
    /// <returns>The current instance for method chaining.</returns>
    public DeferProp OnlyOnPartial()
    {
        _onlyOnPartial = true;
        return this;
    }

    /// <summary>
    /// Gets a value indicating whether this property should use once resolution semantics.
    /// </summary>
    /// <returns><c>true</c> if this property should be resolved once and cached; otherwise, <c>false</c>.</returns>
    public bool IsOnce() => _isOnce;

    /// <summary>
    /// Gets a value indicating whether this property should be merged with existing client data.
    /// </summary>
    /// <returns><c>true</c> if the property should be merged; otherwise, <c>false</c>.</returns>
    public bool ShouldMerge() => _shouldMerge;

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
