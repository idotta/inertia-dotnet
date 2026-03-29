namespace Inertia.AspNetCore;

/// <summary>
/// Provides pagination metadata for scroll props used in infinite scroll scenarios.
/// </summary>
public interface IScrollMetadataProvider
{
    /// <summary>Gets the query parameter name used for pagination (e.g., "page").</summary>
    string PageName { get; }

    /// <summary>Gets the identifier for the previous page, or null if there is no previous page.</summary>
    object? PreviousPage { get; }

    /// <summary>Gets the identifier for the next page, or null if there is no next page.</summary>
    object? NextPage { get; }

    /// <summary>Gets the identifier for the current page, or null if not applicable.</summary>
    object? CurrentPage { get; }
}
