namespace Inertia.AspNetCore;

/// <summary>
/// Represents a property that should be resolved only once and cached on the client.
/// </summary>
public interface IOnceable
{
    /// <summary>Gets a value indicating whether this property should be resolved only once and cached.</summary>
    bool ShouldResolveOnce { get; }

    /// <summary>Gets a value indicating whether the cached value should be forcefully refreshed.</summary>
    bool ShouldBeRefreshed { get; }

    /// <summary>Gets the custom cache key for the once-resolved property, or null to use the default key.</summary>
    string? Key { get; }

    /// <summary>Gets the expiration timestamp in milliseconds (Unix epoch) for the cached value, or null for no expiration.</summary>
    long? ExpiresAt { get; }
}
