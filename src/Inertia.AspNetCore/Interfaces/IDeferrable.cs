namespace Inertia.AspNetCore;

/// <summary>
/// Represents a property that can be deferred from the initial page load and fetched later by the client.
/// </summary>
public interface IDeferrable
{
    /// <summary>Gets a value indicating whether this property should be deferred from the initial page load.</summary>
    bool ShouldDefer { get; }

    /// <summary>Gets the defer group name. Deferred props in the same group are fetched together. Defaults to "default".</summary>
    string Group { get; }
}
