namespace Inertia.AspNetCore;

/// <summary>Dispatches Inertia page data to an SSR server for server-side rendering.</summary>
public interface ISsrGateway
{
    /// <summary>
    /// Dispatches the page to the SSR server for rendering.
    /// Returns null on failure, indicating the client should fall back to client-side rendering.
    /// </summary>
    /// <param name="page">The Inertia page object to render.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The SSR response containing rendered HTML, or null on failure.</returns>
    Task<SsrResponse?> DispatchAsync(InertiaPage page, CancellationToken cancellationToken = default);

    /// <summary>Checks if the SSR server is healthy and responsive.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the SSR server responded successfully.</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
