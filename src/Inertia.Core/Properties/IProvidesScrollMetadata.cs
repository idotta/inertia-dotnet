namespace Inertia.Core.Properties;

/// <summary>
/// Interface for objects that provide pagination metadata for infinite scroll functionality.
/// </summary>
/// <remarks>
/// This interface is used with ScrollProp to provide pagination information
/// for infinite scroll implementations. The metadata includes information about the current page,
/// next page, and previous page, which helps the client determine whether to load more data
/// and what page to request next.
/// 
/// This is typically implemented by pagination helpers or custom pagination result objects.
/// </remarks>
/// <example>
/// <code>
/// public class PagedListMetadata : IProvidesScrollMetadata
/// {
///     private readonly IPagedList _pagedList;
///     
///     public PagedListMetadata(IPagedList pagedList)
///     {
///         _pagedList = pagedList;
///     }
///     
///     public string GetPageName() => "page";
///     public object? GetPreviousPage() => _pagedList.HasPreviousPage ? _pagedList.PageNumber - 1 : null;
///     public object? GetNextPage() => _pagedList.HasNextPage ? _pagedList.PageNumber + 1 : null;
///     public object? GetCurrentPage() => _pagedList.PageNumber;
/// }
/// </code>
/// </example>
public interface IProvidesScrollMetadata
{
    /// <summary>
    /// Gets the parameter name used for pagination (e.g., "page", "cursor", "offset").
    /// </summary>
    /// <returns>The pagination parameter name.</returns>
    string GetPageName();

    /// <summary>
    /// Gets the previous page identifier, or <c>null</c> if there is no previous page.
    /// </summary>
    /// <returns>The previous page identifier (number, cursor, etc.), or <c>null</c>.</returns>
    object? GetPreviousPage();

    /// <summary>
    /// Gets the next page identifier, or <c>null</c> if there is no next page.
    /// </summary>
    /// <returns>The next page identifier (number, cursor, etc.), or <c>null</c>.</returns>
    object? GetNextPage();

    /// <summary>
    /// Gets the current page identifier.
    /// </summary>
    /// <returns>The current page identifier (number, cursor, etc.).</returns>
    object? GetCurrentPage();
}
