using Inertia.Core.Properties;
using Xunit;

namespace Inertia.Tests.Properties;

public class ScrollMetadataTests
{
    [Fact]
    public void Constructor_StoresAllValues()
    {
        // Arrange
        var pageName = "page";
        var currentPage = 2;
        var previousPage = 1;
        var nextPage = 3;

        // Act
        var metadata = new ScrollMetadata(pageName, currentPage, previousPage, nextPage);

        // Assert
        Assert.Equal(pageName, metadata.GetPageName());
        Assert.Equal(currentPage, metadata.GetCurrentPage());
        Assert.Equal(previousPage, metadata.GetPreviousPage());
        Assert.Equal(nextPage, metadata.GetNextPage());
    }

    [Fact]
    public void Constructor_WithNullPageName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ScrollMetadata(null!, 1));
    }

    [Fact]
    public void FromPageNumbers_FirstPage_HasNoPreviousPage()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.FromPageNumbers(1, 10);

        // Assert
        Assert.Equal(1, metadata.GetCurrentPage());
        Assert.Null(metadata.GetPreviousPage());
        Assert.Equal(2, metadata.GetNextPage());
    }

    [Fact]
    public void FromPageNumbers_MiddlePage_HasBothPreviousAndNext()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.FromPageNumbers(5, 10);

        // Assert
        Assert.Equal(5, metadata.GetCurrentPage());
        Assert.Equal(4, metadata.GetPreviousPage());
        Assert.Equal(6, metadata.GetNextPage());
    }

    [Fact]
    public void FromPageNumbers_LastPage_HasNoNextPage()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.FromPageNumbers(10, 10);

        // Assert
        Assert.Equal(10, metadata.GetCurrentPage());
        Assert.Equal(9, metadata.GetPreviousPage());
        Assert.Null(metadata.GetNextPage());
    }

    [Fact]
    public void FromPageNumbers_CustomPageName()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.FromPageNumbers(1, 10, "custom");

        // Assert
        Assert.Equal("custom", metadata.GetPageName());
    }

    [Fact]
    public void FromCursors_StoresAllCursors()
    {
        // Arrange
        var currentCursor = "current123";
        var previousCursor = "prev123";
        var nextCursor = "next123";

        // Act
        var metadata = ScrollMetadata.FromCursors(currentCursor, previousCursor, nextCursor);

        // Assert
        Assert.Equal(currentCursor, metadata.GetCurrentPage());
        Assert.Equal(previousCursor, metadata.GetPreviousPage());
        Assert.Equal(nextCursor, metadata.GetNextPage());
        Assert.Equal("cursor", metadata.GetPageName());
    }

    [Fact]
    public void FromCursors_WithNullPrevious_HasNoPreviousCursor()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.FromCursors("current", null, "next");

        // Assert
        Assert.Null(metadata.GetPreviousPage());
    }

    [Fact]
    public void FromCursors_WithNullNext_HasNoNextCursor()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.FromCursors("current", "prev", null);

        // Assert
        Assert.Null(metadata.GetNextPage());
    }

    [Fact]
    public void FromCursors_CustomCursorName()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.FromCursors("current", cursorName: "token");

        // Assert
        Assert.Equal("token", metadata.GetPageName());
    }

    [Fact]
    public void Final_FirstPage_HasNoPreviousOrNext()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.Final(1);

        // Assert
        Assert.Equal(1, metadata.GetCurrentPage());
        Assert.Null(metadata.GetPreviousPage());
        Assert.Null(metadata.GetNextPage());
    }

    [Fact]
    public void Final_NotFirstPage_HasPreviousButNoNext()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.Final(5);

        // Assert
        Assert.Equal(5, metadata.GetCurrentPage());
        Assert.Equal(4, metadata.GetPreviousPage());
        Assert.Null(metadata.GetNextPage());
    }

    [Fact]
    public void Final_CustomPageName()
    {
        // Arrange & Act
        var metadata = ScrollMetadata.Final(1, "custom");

        // Assert
        Assert.Equal("custom", metadata.GetPageName());
    }

    [Fact]
    public void ImplementsIProvidesScrollMetadata()
    {
        // Arrange
        var metadata = new ScrollMetadata("page", 1);

        // Assert
        Assert.IsAssignableFrom<IProvidesScrollMetadata>(metadata);
    }
}
