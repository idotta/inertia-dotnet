namespace Inertia.AspNetCore;

/// <summary>
/// The main Inertia service for creating responses and managing per-request state.
/// </summary>
public interface IInertia
{
    /// <summary>Renders an Inertia page response with the given component and optional props object.</summary>
    /// <param name="component">The JavaScript page component name.</param>
    /// <param name="props">An optional object whose public properties become page props.</param>
    InertiaResponse Render(string component, object? props = null);

    /// <summary>Renders an Inertia page response with the given component and props dictionary.</summary>
    /// <param name="component">The JavaScript page component name.</param>
    /// <param name="props">A dictionary of page props.</param>
    InertiaResponse Render(string component, IDictionary<string, object?> props);

    /// <summary>Creates an external redirect (409/302) response for the given URL.</summary>
    /// <param name="url">The target URL.</param>
    InertiaLocationResult Location(string url);

    /// <summary>Shares a single prop with all subsequent Inertia responses in this request.</summary>
    /// <param name="key">The prop key.</param>
    /// <param name="value">The prop value.</param>
    void Share(string key, object? value);

    /// <summary>Shares multiple props with all subsequent Inertia responses in this request.</summary>
    /// <param name="props">A dictionary of props to share.</param>
    void Share(IDictionary<string, object?> props);

    /// <summary>Shares a property provider with all subsequent Inertia responses in this request.</summary>
    /// <param name="provider">The property provider.</param>
    void Share(IInertiaPropertyProvider provider);

    /// <summary>Returns a single shared prop by key, with optional dot-notation traversal for nested values.</summary>
    /// <param name="key">The prop key. Supports dot-notation (e.g., "user.profile.name") for nested lookups.</param>
    /// <param name="defaultValue">The value to return if the key is not found.</param>
    object? GetShared(string key, object? defaultValue = null);

    /// <summary>Creates an <see cref="OnceProp{T}"/> from the callback and shares it under the given key.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="key">The prop key.</param>
    /// <param name="callback">A function that produces the value. Resolved once and cached on the client.</param>
    void ShareOnce<T>(string key, Func<T> callback);

    /// <summary>Creates an <see cref="OnceProp{T}"/> from the async callback and shares it under the given key.</summary>
    /// <typeparam name="T">The type of the value produced by the callback.</typeparam>
    /// <param name="key">The prop key.</param>
    /// <param name="callback">An async function that produces the value. Resolved once and cached on the client.</param>
    void ShareOnce<T>(string key, Func<Task<T>> callback);

    /// <summary>Stores a flash data entry for the current request.</summary>
    /// <param name="key">The flash data key.</param>
    /// <param name="value">The flash data value.</param>
    void Flash(string key, object? value);

    /// <summary>Stores multiple flash data entries for the current request.</summary>
    /// <param name="data">A dictionary of flash data.</param>
    void Flash(IDictionary<string, object?> data);

    /// <summary>Returns all flash data for the current request.</summary>
    IDictionary<string, object?> GetFlashed();

    /// <summary>Signals the client to clear the browser history state.</summary>
    void ClearHistory();

    /// <summary>Signals the client to preserve the URL fragment across navigations.</summary>
    void PreserveFragment();

    /// <summary>Sets whether the browser history state should be encrypted.</summary>
    /// <param name="encrypt">True to encrypt, false to disable encryption.</param>
    void EncryptHistory(bool encrypt = true);

    /// <summary>Excludes the given paths from server-side rendering for this request.</summary>
    /// <param name="paths">Paths to exclude. Supports exact match and trailing wildcard (e.g., "/api/*").</param>
    void WithoutSsr(params string[] paths);
}
