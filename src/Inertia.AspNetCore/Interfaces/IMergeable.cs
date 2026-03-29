namespace Inertia.AspNetCore;

/// <summary>
/// Represents a property whose value can be merged with existing client-side data during partial reloads.
/// </summary>
public interface IMergeable
{
    /// <summary>Gets a value indicating whether the property should be merged with existing client-side data.</summary>
    bool ShouldMerge { get; }

    /// <summary>Gets a value indicating whether the property should be deep merged.</summary>
    bool ShouldDeepMerge { get; }

    /// <summary>Gets the property paths used for matching during merge operations.</summary>
    IReadOnlyList<string> MatchesOn { get; }

    /// <summary>Gets a value indicating whether values should be appended at the root level.</summary>
    bool AppendsAtRoot { get; }

    /// <summary>Gets a value indicating whether values should be prepended at the root level.</summary>
    bool PrependsAtRoot { get; }

    /// <summary>Gets the paths at which values should be appended during merging.</summary>
    IReadOnlyList<string> AppendsAtPaths { get; }

    /// <summary>Gets the paths at which values should be prepended during merging.</summary>
    IReadOnlyList<string> PrependsAtPaths { get; }
}
