namespace Inertia.AspNetCore;

/// <summary>
/// Composition value object for deferred prop behavior. Used by DeferProp and ScrollProp.
/// Replaces PHP DefersProps trait.
/// </summary>
internal record struct DeferInfo
{
    /// <summary>Gets a value indicating whether the prop should be deferred from the initial page load.</summary>
    public bool ShouldDefer { get; private set; }

    /// <summary>Gets the defer group name. Deferred props in the same group are fetched together.</summary>
    public string Group { get; private set; }

    /// <summary>Initializes a new instance of <see cref="DeferInfo"/> with default values.</summary>
    public DeferInfo()
    {
        ShouldDefer = false;
        Group = "default";
    }

    /// <summary>Marks this prop as deferred, optionally in a specific group.</summary>
    /// <param name="group">The defer group name, or null to use "default".</param>
    public void Defer(string? group = null)
    {
        ShouldDefer = true;
        Group = group ?? "default";
    }
}
