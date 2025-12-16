namespace Inertia.Core.Ssr;

/// <summary>
/// Interface for gateways that support health check functionality.
/// </summary>
public interface IHasHealthCheck
{
    /// <summary>
    /// Checks if the SSR server is healthy and ready to process requests.
    /// </summary>
    /// <returns>True if the SSR server is healthy, false otherwise.</returns>
    Task<bool> IsHealthyAsync();
}
