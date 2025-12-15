namespace Inertia.Core;

/// <summary>
/// Factory interface for creating Inertia responses.
/// Equivalent to the ResponseFactory in the Laravel adapter.
/// </summary>
public interface IInertia
{
    /// <summary>
    /// Renders an Inertia response with the specified component and props.
    /// </summary>
    /// <param name="component">The name of the frontend component to render.</param>
    /// <param name="props">The properties/data to pass to the component.</param>
    /// <returns>An InertiaResponse that can be returned from a controller action.</returns>
    Task<InertiaResponse> RenderAsync(string component, IDictionary<string, object?>? props = null);

    /// <summary>
    /// Creates an Inertia location response that forces a client-side redirect.
    /// Returns a 409 status code with X-Inertia-Location header for Inertia requests,
    /// or a standard redirect for non-Inertia requests.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <returns>An action result representing the redirect.</returns>
    object Location(string url);

    /// <summary>
    /// Shares data across all Inertia responses. This data is automatically included
    /// with every response, making it ideal for user authentication state, flash messages, etc.
    /// </summary>
    /// <param name="key">The key for the shared data.</param>
    /// <param name="value">The value to share.</param>
    void Share(string key, object? value);

    /// <summary>
    /// Shares multiple properties at once by merging them with existing shared props.
    /// </summary>
    /// <param name="props">A dictionary containing properties to share.</param>
    void Share(IDictionary<string, object?> props);

    /// <summary>
    /// Gets the value of a shared property.
    /// </summary>
    /// <param name="key">The key of the shared property to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the key doesn't exist.</param>
    /// <returns>The value of the shared property or the default value.</returns>
    object? GetShared(string? key = null, object? defaultValue = null);

    /// <summary>
    /// Removes all shared data.
    /// </summary>
    void FlushShared();

    /// <summary>
    /// Sets the asset version for cache busting. Can be a static string or a callback.
    /// </summary>
    /// <param name="version">The version string or a callback that returns a version string.</param>
    void SetVersion(string version);

    /// <summary>
    /// Sets the asset version using a provider callback.
    /// </summary>
    /// <param name="versionProvider">A function that returns the version string.</param>
    void SetVersion(Func<string> versionProvider);

    /// <summary>
    /// Gets the current asset version.
    /// </summary>
    /// <returns>The current asset version string.</returns>
    string GetVersion();

    /// <summary>
    /// Sets the name of the root view template.
    /// </summary>
    /// <param name="viewName">The name of the root view.</param>
    void SetRootView(string viewName);

    /// <summary>
    /// Clears the browser history on the next visit.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Encrypts the browser history.
    /// </summary>
    /// <param name="encrypt">Whether to encrypt the history.</param>
    void EncryptHistory(bool encrypt = true);

    /// <summary>
    /// Sets a custom URL resolver callback.
    /// </summary>
    /// <param name="urlResolver">A function that resolves the URL for the current request.</param>
    void ResolveUrlUsing(Func<string>? urlResolver);
}
