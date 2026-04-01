namespace Inertia.AspNetCore;

/// <summary>
/// Composition object for once-resolved prop behavior.
/// Used by OptionalProp, OnceProp, MergeProp, DeferProp.
/// Replaces PHP ResolvesOnce trait.
/// </summary>
internal sealed class OnceInfo
{
    private TimeSpan? _ttl;

    /// <summary>Gets a value indicating whether this property should be resolved only once and cached.</summary>
    public bool ShouldResolveOnce { get; private set; }

    /// <summary>Gets a value indicating whether the cached value should be forcefully refreshed.</summary>
    public bool ShouldBeRefreshed { get; private set; }

    /// <summary>Gets the custom cache key for the once-resolved property, or null to use the default key.</summary>
    public string? Key { get; private set; }

    /// <summary>Gets the expiration timestamp in milliseconds (Unix epoch) for the cached value, or null for no expiration.</summary>
    public long? ExpiresAt =>
        _ttl is null ? null : DateTimeOffset.UtcNow.Add(_ttl.Value).ToUnixTimeMilliseconds();

    /// <summary>Sets whether this property should be resolved only once.</summary>
    /// <param name="value">True to enable once-resolution; false to disable.</param>
    public void Once(bool value = true) => ShouldResolveOnce = value;

    /// <summary>Sets a custom cache key using a string.</summary>
    /// <param name="key">The cache key.</param>
    public void As(string key) => Key = key;

    /// <summary>Sets a custom cache key using an enum value (its name).</summary>
    /// <param name="key">The enum value whose name becomes the cache key.</param>
    public void As(Enum key) => Key = key.ToString();

    /// <summary>Sets whether the cached value should be forcefully refreshed.</summary>
    /// <param name="value">True to force refresh; false to use cached value.</param>
    public void Fresh(bool value = true) => ShouldBeRefreshed = value;

    /// <summary>Sets the time-to-live for the cached value.</summary>
    /// <param name="delay">The duration after which the cached value expires.</param>
    public void Until(TimeSpan delay) => _ttl = delay;

    /// <summary>Sets the time-to-live for the cached value in seconds.</summary>
    /// <param name="seconds">The number of seconds after which the cached value expires.</param>
    public void Until(int seconds) => _ttl = TimeSpan.FromSeconds(seconds);

    /// <summary>Sets the expiration time as an absolute UTC timestamp.</summary>
    /// <param name="expiresAt">The absolute point in time when the cached value expires.</param>
    public void Until(DateTimeOffset expiresAt) => _ttl = expiresAt - DateTimeOffset.UtcNow;
}
