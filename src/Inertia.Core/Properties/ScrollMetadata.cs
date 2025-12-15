namespace Inertia.Core.Properties;

/// <summary>
/// Provides metadata for paginated scroll properties to support infinite scroll functionality.
/// </summary>
/// <remarks>
/// ScrollMetadata encapsulates pagination information that helps the client determine
/// whether there are more pages to load and how to request them. This is used in
/// conjunction with <see cref="ScrollProp"/> for infinite scroll implementations.
/// </remarks>
public class ScrollMetadata : IProvidesScrollMetadata
{
    private readonly string _pageName;
    private readonly object? _previousPage;
    private readonly object? _nextPage;
    private readonly object? _currentPage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollMetadata"/> class.
    /// </summary>
    /// <param name="pageName">The name of the page parameter (e.g., "page").</param>
    /// <param name="currentPage">The current page number or cursor.</param>
    /// <param name="previousPage">The previous page number or cursor, or null if on first page.</param>
    /// <param name="nextPage">The next page number or cursor, or null if on last page.</param>
    public ScrollMetadata(
        string pageName,
        object? currentPage,
        object? previousPage = null,
        object? nextPage = null)
    {
        _pageName = pageName ?? throw new ArgumentNullException(nameof(pageName));
        _currentPage = currentPage;
        _previousPage = previousPage;
        _nextPage = nextPage;
    }

    /// <summary>
    /// Gets the name of the page parameter.
    /// </summary>
    /// <returns>The page parameter name (e.g., "page", "cursor").</returns>
    public string GetPageName() => _pageName;

    /// <summary>
    /// Gets the previous page number or cursor.
    /// </summary>
    /// <returns>The previous page identifier, or null if on the first page.</returns>
    public object? GetPreviousPage() => _previousPage;

    /// <summary>
    /// Gets the next page number or cursor.
    /// </summary>
    /// <returns>The next page identifier, or null if on the last page.</returns>
    public object? GetNextPage() => _nextPage;

    /// <summary>
    /// Gets the current page number or cursor.
    /// </summary>
    /// <returns>The current page identifier.</returns>
    public object? GetCurrentPage() => _currentPage;

    /// <summary>
    /// Creates a ScrollMetadata instance from standard pagination information.
    /// </summary>
    /// <param name="currentPage">The current page number (1-based).</param>
    /// <param name="totalPages">The total number of pages.</param>
    /// <param name="pageName">The name of the page parameter. Default is "page".</param>
    /// <returns>A new <see cref="ScrollMetadata"/> instance.</returns>
    public static ScrollMetadata FromPageNumbers(
        int currentPage,
        int totalPages,
        string pageName = "page")
    {
        var previousPage = currentPage > 1 ? (object)(currentPage - 1) : null;
        var nextPage = currentPage < totalPages ? (object)(currentPage + 1) : null;

        return new ScrollMetadata(pageName, currentPage, previousPage, nextPage);
    }

    /// <summary>
    /// Creates a ScrollMetadata instance from cursor-based pagination.
    /// </summary>
    /// <param name="currentCursor">The current cursor.</param>
    /// <param name="previousCursor">The previous cursor, or null if at the start.</param>
    /// <param name="nextCursor">The next cursor, or null if at the end.</param>
    /// <param name="cursorName">The name of the cursor parameter. Default is "cursor".</param>
    /// <returns>A new <see cref="ScrollMetadata"/> instance.</returns>
    public static ScrollMetadata FromCursors(
        string? currentCursor,
        string? previousCursor = null,
        string? nextCursor = null,
        string cursorName = "cursor")
    {
        return new ScrollMetadata(cursorName, currentCursor, previousCursor, nextCursor);
    }

    /// <summary>
    /// Creates a ScrollMetadata instance indicating there are no more pages.
    /// </summary>
    /// <param name="currentPage">The current (and final) page number.</param>
    /// <param name="pageName">The name of the page parameter. Default is "page".</param>
    /// <returns>A new <see cref="ScrollMetadata"/> instance with no next page.</returns>
    public static ScrollMetadata Final(int currentPage, string pageName = "page")
    {
        var previousPage = currentPage > 1 ? (object)(currentPage - 1) : null;
        return new ScrollMetadata(pageName, currentPage, previousPage, null);
    }
}
