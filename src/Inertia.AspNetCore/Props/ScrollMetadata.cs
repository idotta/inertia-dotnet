namespace Inertia.AspNetCore;

/// <summary>
/// Default implementation of <see cref="IScrollMetadataProvider"/> for manual pagination metadata.
/// </summary>
public sealed class ScrollMetadata : IScrollMetadataProvider
{
    /// <inheritdoc />
    public string PageName { get; }

    /// <inheritdoc />
    public object? PreviousPage { get; }

    /// <inheritdoc />
    public object? NextPage { get; }

    /// <inheritdoc />
    public object? CurrentPage { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ScrollMetadata"/> with the specified pagination values.
    /// </summary>
    /// <param name="pageName">The query parameter name used for pagination (e.g., "page"). Must not be null or empty.</param>
    /// <param name="previousPage">The identifier for the previous page, or null if there is no previous page.</param>
    /// <param name="nextPage">The identifier for the next page, or null if there is no next page.</param>
    /// <param name="currentPage">The identifier for the current page, or null if not applicable.</param>
    public ScrollMetadata(string pageName, object? previousPage = null, object? nextPage = null, object? currentPage = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(pageName);
        PageName = pageName;
        PreviousPage = previousPage;
        NextPage = nextPage;
        CurrentPage = currentPage;
    }

    /// <summary>Converts this metadata to a dictionary representation.</summary>
    /// <returns>A dictionary with keys "pageName", "previousPage", "nextPage", and "currentPage".</returns>
    public IReadOnlyDictionary<string, object?> ToDictionary() => new Dictionary<string, object?>
    {
        ["pageName"] = PageName,
        ["previousPage"] = PreviousPage,
        ["nextPage"] = NextPage,
        ["currentPage"] = CurrentPage,
    };
}
