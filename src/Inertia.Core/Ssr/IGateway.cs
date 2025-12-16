namespace Inertia.Core.Ssr;

/// <summary>
/// Gateway interface for server-side rendering.
/// </summary>
public interface IGateway
{
    /// <summary>
    /// Dispatches a request to the SSR server to render the page.
    /// </summary>
    /// <param name="pageData">The page data to render.</param>
    /// <returns>The SSR response, or null if SSR is unavailable or fails.</returns>
    Task<SsrResponse?> DispatchAsync(Dictionary<string, object?> pageData);
}
