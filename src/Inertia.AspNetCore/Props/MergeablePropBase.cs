namespace Inertia.AspNetCore;

/// <summary>
/// Abstract base class implementing <see cref="IMergeable"/>. Provides fluent merge/append/prepend configuration.
/// Subclassed by MergeProp, DeferProp, and ScrollProp.
/// </summary>
public abstract class MergeablePropBase : IMergeable
{
    private bool _merge;
    private bool _deepMerge;
    private List<string> _matchOn = [];
    private bool _append = true;
    private readonly List<string> _appendsAtPaths = [];
    private readonly List<string> _prependsAtPaths = [];

    /// <inheritdoc />
    public bool ShouldMerge => _merge;

    /// <inheritdoc />
    public bool ShouldDeepMerge => _deepMerge;

    /// <inheritdoc />
    public IReadOnlyList<string> MatchesOn => _matchOn;

    /// <inheritdoc />
    public bool AppendsAtRoot => _append && _appendsAtPaths.Count == 0 && _prependsAtPaths.Count == 0;

    /// <inheritdoc />
    public bool PrependsAtRoot => !_append && _appendsAtPaths.Count == 0 && _prependsAtPaths.Count == 0;

    /// <inheritdoc />
    public IReadOnlyList<string> AppendsAtPaths => _appendsAtPaths;

    /// <inheritdoc />
    public IReadOnlyList<string> PrependsAtPaths => _prependsAtPaths;

    /// <summary>Enables merging with existing client-side data.</summary>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase Merge()
    {
        _merge = true;
        return this;
    }

    /// <summary>Enables deep merging, which also enables regular merging.</summary>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase DeepMerge()
    {
        _deepMerge = true;
        return Merge();
    }

    /// <summary>Sets the match-on path used for merge identity matching.</summary>
    /// <param name="matchOn">The property path to match on.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase MatchOn(string matchOn)
    {
        _matchOn = [matchOn];
        return this;
    }

    /// <summary>Sets the match-on paths used for merge identity matching.</summary>
    /// <param name="matchOn">The property paths to match on.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase MatchOn(IEnumerable<string> matchOn)
    {
        _matchOn = [.. matchOn];
        return this;
    }

    /// <summary>Sets the append flag. When true, values are appended; when false, values are prepended at root.</summary>
    /// <param name="value">Whether to append. Defaults to true.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase Append(bool value = true)
    {
        _append = value;
        return this;
    }

    /// <summary>Adds a specific path where values should be appended during merging.</summary>
    /// <param name="path">The path at which to append.</param>
    /// <param name="matchOn">Optional match-on key. When provided, adds "path.matchOn" to the match-on list.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase Append(string path, string? matchOn = null)
    {
        _appendsAtPaths.Add(path);
        if (matchOn is not null)
            _matchOn = [.. _matchOn, $"{path}.{matchOn}"];
        return this;
    }

    /// <summary>Adds multiple paths where values should be appended during merging.</summary>
    /// <param name="paths">The paths at which to append.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase Append(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            _appendsAtPaths.Add(path);
        return this;
    }

    /// <summary>Adds paths with associated match-on keys for appending during merging.</summary>
    /// <param name="pathsWithMatchOn">Dictionary mapping paths to their match-on keys.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase Append(IDictionary<string, string> pathsWithMatchOn)
    {
        foreach (var (path, matchOn) in pathsWithMatchOn)
            Append(path, matchOn);
        return this;
    }

    /// <summary>Sets the prepend flag by inverting the value (matching PHP behavior where prepend sets append=!value).</summary>
    /// <param name="value">Whether to prepend. Defaults to true.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase Prepend(bool value = true)
    {
        _append = !value;
        return this;
    }

    /// <summary>Adds a specific path where values should be prepended during merging.</summary>
    /// <param name="path">The path at which to prepend.</param>
    /// <param name="matchOn">Optional match-on key. When provided, adds "path.matchOn" to the match-on list.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase Prepend(string path, string? matchOn = null)
    {
        _prependsAtPaths.Add(path);
        if (matchOn is not null)
            _matchOn = [.. _matchOn, $"{path}.{matchOn}"];
        return this;
    }

    /// <summary>Adds multiple paths where values should be prepended during merging.</summary>
    /// <param name="paths">The paths at which to prepend.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase Prepend(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            _prependsAtPaths.Add(path);
        return this;
    }

    /// <summary>Adds paths with associated match-on keys for prepending during merging.</summary>
    /// <param name="pathsWithMatchOn">Dictionary mapping paths to their match-on keys.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public MergeablePropBase Prepend(IDictionary<string, string> pathsWithMatchOn)
    {
        foreach (var (path, matchOn) in pathsWithMatchOn)
            Prepend(path, matchOn);
        return this;
    }
}
