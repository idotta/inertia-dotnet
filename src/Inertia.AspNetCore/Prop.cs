namespace Inertia.AspNetCore;

/// <summary>
/// Static factory for creating Inertia property types.
/// Provides a clean API for constructing props without DI injection.
/// </summary>
/// <example>
/// <code>
/// inertia.Render("Users/Index", new {
///     users = Prop.Defer&lt;List&lt;User&gt;&gt;(() => GetUsersAsync()),
///     auth = Prop.Always(GetAuth()),
///     filters = Prop.Optional&lt;Filters&gt;(() => GetFilters())
/// });
/// </code>
/// </example>
public static class Prop
{
    // Always — included in every response, even during partial reloads.

    /// <summary>Creates an <see cref="AlwaysProp{T}"/> with a static value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to always include.</param>
    /// <returns>A new <see cref="AlwaysProp{T}"/> instance.</returns>
    public static AlwaysProp<T> Always<T>(T value) => new(value);

    /// <summary>Creates an <see cref="AlwaysProp{T}"/> with a synchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The callback to invoke when the property is resolved.</param>
    /// <returns>A new <see cref="AlwaysProp{T}"/> instance.</returns>
    public static AlwaysProp<T> Always<T>(Func<T> callback) => new(callback);

    /// <summary>Creates an <see cref="AlwaysProp{T}"/> with an asynchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The async callback to invoke when the property is resolved.</param>
    /// <returns>A new <see cref="AlwaysProp{T}"/> instance.</returns>
    public static AlwaysProp<T> Always<T>(Func<Task<T>> callback) => new(callback);

    // Optional — only included in partial reloads when explicitly requested.

    /// <summary>Creates an <see cref="OptionalProp{T}"/> with a synchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The callback to invoke when requested via partial reload.</param>
    /// <returns>A new <see cref="OptionalProp{T}"/> instance.</returns>
    public static OptionalProp<T> Optional<T>(Func<T> callback) => new(callback);

    /// <summary>Creates an <see cref="OptionalProp{T}"/> with an asynchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The async callback to invoke when requested via partial reload.</param>
    /// <returns>A new <see cref="OptionalProp{T}"/> instance.</returns>
    public static OptionalProp<T> Optional<T>(Func<Task<T>> callback) => new(callback);

    // Defer — excluded from initial page load, fetched on demand by the client.

    /// <summary>Creates a <see cref="DeferProp{T}"/> with a synchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The callback to invoke when the deferred property is fetched.</param>
    /// <param name="group">The defer group name. Props in the same group are fetched together. Defaults to "default".</param>
    /// <returns>A new <see cref="DeferProp{T}"/> instance.</returns>
    public static DeferProp<T> Defer<T>(Func<T> callback, string? group = null) => new(callback, group);

    /// <summary>Creates a <see cref="DeferProp{T}"/> with an asynchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The async callback to invoke when the deferred property is fetched.</param>
    /// <param name="group">The defer group name. Props in the same group are fetched together. Defaults to "default".</param>
    /// <returns>A new <see cref="DeferProp{T}"/> instance.</returns>
    public static DeferProp<T> Defer<T>(Func<Task<T>> callback, string? group = null) => new(callback, group);

    // Merge — merged with existing client-side data during partial reloads.

    /// <summary>Creates a <see cref="MergeProp{T}"/> with a static value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to merge with existing client-side data.</param>
    /// <returns>A new <see cref="MergeProp{T}"/> instance with merging enabled.</returns>
    public static MergeProp<T> Merge<T>(T value) => new(value);

    /// <summary>Creates a <see cref="MergeProp{T}"/> with a synchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The callback to invoke when the property is resolved.</param>
    /// <returns>A new <see cref="MergeProp{T}"/> instance with merging enabled.</returns>
    public static MergeProp<T> Merge<T>(Func<T> callback) => new(callback);

    /// <summary>Creates a <see cref="MergeProp{T}"/> with an asynchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The async callback to invoke when the property is resolved.</param>
    /// <returns>A new <see cref="MergeProp{T}"/> instance with merging enabled.</returns>
    public static MergeProp<T> Merge<T>(Func<Task<T>> callback) => new(callback);

    // DeepMerge — convenience for Merge + DeepMerge in a single call.

    /// <summary>Creates a <see cref="MergeProp{T}"/> with a static value and deep merging enabled.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to deep-merge with existing client-side data.</param>
    /// <returns>A new <see cref="MergeProp{T}"/> instance with deep merging enabled.</returns>
    public static MergeProp<T> DeepMerge<T>(T value) => new MergeProp<T>(value).DeepMerge();

    /// <summary>Creates a <see cref="MergeProp{T}"/> with a synchronous callback and deep merging enabled.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The callback to invoke when the property is resolved.</param>
    /// <returns>A new <see cref="MergeProp{T}"/> instance with deep merging enabled.</returns>
    public static MergeProp<T> DeepMerge<T>(Func<T> callback) => new MergeProp<T>(callback).DeepMerge();

    /// <summary>Creates a <see cref="MergeProp{T}"/> with an asynchronous callback and deep merging enabled.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The async callback to invoke when the property is resolved.</param>
    /// <returns>A new <see cref="MergeProp{T}"/> instance with deep merging enabled.</returns>
    public static MergeProp<T> DeepMerge<T>(Func<Task<T>> callback) => new MergeProp<T>(callback).DeepMerge();

    // Once — resolved once, cached on the client.

    /// <summary>Creates an <see cref="OnceProp{T}"/> with a synchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The callback to invoke on first resolution.</param>
    /// <returns>A new <see cref="OnceProp{T}"/> instance with once-resolution enabled.</returns>
    public static OnceProp<T> Once<T>(Func<T> callback) => new(callback);

    /// <summary>Creates an <see cref="OnceProp{T}"/> with an asynchronous callback.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="callback">The async callback to invoke on first resolution.</param>
    /// <returns>A new <see cref="OnceProp{T}"/> instance with once-resolution enabled.</returns>
    public static OnceProp<T> Once<T>(Func<Task<T>> callback) => new(callback);

    // Scroll — paginated/infinite scroll data with merge capabilities.

    /// <summary>Creates a <see cref="ScrollProp{T}"/> with a static value.</summary>
    /// <typeparam name="T">The type of the scroll data.</typeparam>
    /// <param name="value">The scroll data value.</param>
    /// <param name="wrapper">The wrapper path used for merge append/prepend operations. Defaults to "data".</param>
    /// <param name="metadata">Optional scroll metadata provider.</param>
    /// <returns>A new <see cref="ScrollProp{T}"/> instance with merging enabled.</returns>
    public static ScrollProp<T> Scroll<T>(T value, string wrapper = "data", IScrollMetadataProvider? metadata = null)
        => new(value, wrapper, metadata);

    /// <summary>Creates a <see cref="ScrollProp{T}"/> with a synchronous callback.</summary>
    /// <typeparam name="T">The type of the scroll data produced by the callback.</typeparam>
    /// <param name="callback">The callback to invoke when the property is resolved.</param>
    /// <param name="wrapper">The wrapper path used for merge append/prepend operations. Defaults to "data".</param>
    /// <param name="metadata">Optional scroll metadata provider.</param>
    /// <returns>A new <see cref="ScrollProp{T}"/> instance with merging enabled.</returns>
    public static ScrollProp<T> Scroll<T>(Func<T> callback, string wrapper = "data", IScrollMetadataProvider? metadata = null)
        => new(callback, wrapper, metadata);

    /// <summary>Creates a <see cref="ScrollProp{T}"/> with an asynchronous callback.</summary>
    /// <typeparam name="T">The type of the scroll data produced by the callback.</typeparam>
    /// <param name="callback">The async callback to invoke when the property is resolved.</param>
    /// <param name="wrapper">The wrapper path used for merge append/prepend operations. Defaults to "data".</param>
    /// <param name="metadata">Optional scroll metadata provider.</param>
    /// <returns>A new <see cref="ScrollProp{T}"/> instance with merging enabled.</returns>
    public static ScrollProp<T> Scroll<T>(Func<Task<T>> callback, string wrapper = "data", IScrollMetadataProvider? metadata = null)
        => new(callback, wrapper, metadata);
}
