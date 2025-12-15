namespace Inertia.Core.Properties;

/// <summary>
/// Represents a property for paginated data that supports infinite scroll with merge functionality.
/// </summary>
/// <remarks>
/// ScrollProp is specifically designed for infinite scroll implementations. It combines
/// merge functionality with pagination metadata to enable:
/// - Appending new items to the end of a list (default)
/// - Prepending new items to the beginning of a list
/// - Tracking pagination state (current, previous, next pages)
/// 
/// The property supports both standard pagination (page numbers) and cursor-based pagination.
/// Data can be wrapped in a structure (e.g., { data: [...], meta: {...} }) or provided as a flat array.
/// </remarks>
public class ScrollProp : IMergeable
{
    private readonly object? _value;
    private readonly Func<object?>? _callback;
    private readonly Func<Task<object?>>? _asyncCallback;
    private readonly string? _wrapper;
    private readonly Func<object?, IProvidesScrollMetadata>? _metadataProvider;
    private string? _mergePath;
    private bool _isPrepend;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollProp"/> class with a static value.
    /// </summary>
    /// <param name="value">The paginated data.</param>
    /// <param name="wrapper">Optional property name that wraps the data (e.g., "data").</param>
    /// <param name="metadataProvider">Optional function to extract pagination metadata from the value.</param>
    public ScrollProp(
        object? value,
        string? wrapper = null,
        Func<object?, IProvidesScrollMetadata>? metadataProvider = null)
    {
        _value = value;
        _wrapper = wrapper;
        _metadataProvider = metadataProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollProp"/> class with a synchronous callback.
    /// </summary>
    /// <param name="callback">The callback function that returns the paginated data.</param>
    /// <param name="wrapper">Optional property name that wraps the data (e.g., "data").</param>
    /// <param name="metadataProvider">Optional function to extract pagination metadata from the value.</param>
    public ScrollProp(
        Func<object?> callback,
        string? wrapper = null,
        Func<object?, IProvidesScrollMetadata>? metadataProvider = null)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _wrapper = wrapper;
        _metadataProvider = metadataProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollProp"/> class with an asynchronous callback.
    /// </summary>
    /// <param name="asyncCallback">The async callback function that returns the paginated data.</param>
    /// <param name="wrapper">Optional property name that wraps the data (e.g., "data").</param>
    /// <param name="metadataProvider">Optional function to extract pagination metadata from the value.</param>
    public ScrollProp(
        Func<Task<object?>> asyncCallback,
        string? wrapper = null,
        Func<object?, IProvidesScrollMetadata>? metadataProvider = null)
    {
        _asyncCallback = asyncCallback ?? throw new ArgumentNullException(nameof(asyncCallback));
        _wrapper = wrapper;
        _metadataProvider = metadataProvider;
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
    /// Configures this property to append new items to the end of the existing list.
    /// </summary>
    /// <param name="path">Optional path within the data structure where items should be appended.</param>
    /// <returns>The current instance for method chaining.</returns>
    public ScrollProp Append(string? path = null)
    {
        _isPrepend = false;
        _mergePath = path ?? _wrapper;
        return this;
    }

    /// <summary>
    /// Configures this property to prepend new items to the beginning of the existing list.
    /// </summary>
    /// <param name="path">Optional path within the data structure where items should be prepended.</param>
    /// <returns>The current instance for method chaining.</returns>
    public ScrollProp Prepend(string? path = null)
    {
        _isPrepend = true;
        _mergePath = path ?? _wrapper;
        return this;
    }

    /// <summary>
    /// Configures the merge intent based on the provided intent string.
    /// This should be called during request processing to determine whether to append or prepend.
    /// </summary>
    /// <param name="mergeIntent">The merge intent string ("append" or "prepend").</param>
    /// <returns>The current instance for method chaining.</returns>
    public ScrollProp ConfigureMergeIntent(string? mergeIntent)
    {
        if (string.IsNullOrEmpty(mergeIntent))
        {
            return this;
        }

        var intentValue = mergeIntent.ToLowerInvariant();
        if (intentValue == "prepend")
        {
            _isPrepend = true;
        }
        else if (intentValue == "append")
        {
            _isPrepend = false;
        }

        return this;
    }

    /// <summary>
    /// Gets the pagination metadata for this scroll property.
    /// </summary>
    /// <returns>The scroll metadata, or null if no metadata provider was specified.</returns>
    public async Task<IProvidesScrollMetadata?> GetMetadataAsync()
    {
        if (_metadataProvider == null)
        {
            return null;
        }

        var value = await ResolveAsync();
        return _metadataProvider(value);
    }

    /// <summary>
    /// Gets the wrapper property name, if any.
    /// </summary>
    public string? Wrapper => _wrapper;

    /// <summary>
    /// Gets a value indicating whether new items should be prepended (true) or appended (false).
    /// </summary>
    public bool IsPrepend => _isPrepend;

    /// <summary>
    /// Gets a value indicating whether this property should be merged with existing client data.
    /// Always returns <c>true</c> for <see cref="ScrollProp"/>.
    /// </summary>
    /// <returns>Always <c>true</c>.</returns>
    public bool ShouldMerge() => true;

    /// <summary>
    /// Gets the path within the property data where merging should occur.
    /// </summary>
    /// <returns>The dot-notation path to the merge location, or <c>null</c> to merge at the root level.</returns>
    public string? GetMergePath() => _mergePath;

    /// <summary>
    /// Gets a value indicating whether deep merging should be used.
    /// ScrollProp uses shallow merge by default (items are appended/prepended to arrays).
    /// </summary>
    /// <returns>Always <c>false</c> for <see cref="ScrollProp"/>.</returns>
    public bool IsDeepMerge() => false;

    /// <summary>
    /// Gets a value indicating whether merging should only occur during partial reloads.
    /// ScrollProp always uses merge behavior only on partial reloads.
    /// </summary>
    /// <returns>Always <c>true</c> for <see cref="ScrollProp"/>.</returns>
    public bool OnlyOnPartial() => true;
}
