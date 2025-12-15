using FluentAssertions;
using Inertia.Core.Properties;

namespace Inertia.Tests.Properties;

/// <summary>
/// Tests for the IProvidesScrollMetadata interface.
/// </summary>
public class IProvidesScrollMetadataTests
{
    [Fact]
    public void IProvidesScrollMetadata_ShouldHaveAllRequiredMethods()
    {
        // Arrange & Act
        var methods = typeof(IProvidesScrollMetadata).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == nameof(IProvidesScrollMetadata.GetPageName));
        methods.Should().Contain(m => m.Name == nameof(IProvidesScrollMetadata.GetPreviousPage));
        methods.Should().Contain(m => m.Name == nameof(IProvidesScrollMetadata.GetNextPage));
        methods.Should().Contain(m => m.Name == nameof(IProvidesScrollMetadata.GetCurrentPage));
    }

    [Fact]
    public void IProvidesScrollMetadata_GetPageName_ShouldReturnString()
    {
        // Arrange & Act
        var method = typeof(IProvidesScrollMetadata).GetMethod(nameof(IProvidesScrollMetadata.GetPageName));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void IProvidesScrollMetadata_GetPreviousPage_ShouldReturnNullableObject()
    {
        // Arrange & Act
        var method = typeof(IProvidesScrollMetadata).GetMethod(nameof(IProvidesScrollMetadata.GetPreviousPage));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(object));
    }

    [Fact]
    public void IProvidesScrollMetadata_GetNextPage_ShouldReturnNullableObject()
    {
        // Arrange & Act
        var method = typeof(IProvidesScrollMetadata).GetMethod(nameof(IProvidesScrollMetadata.GetNextPage));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(object));
    }

    [Fact]
    public void IProvidesScrollMetadata_GetCurrentPage_ShouldReturnNullableObject()
    {
        // Arrange & Act
        var method = typeof(IProvidesScrollMetadata).GetMethod(nameof(IProvidesScrollMetadata.GetCurrentPage));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(object));
    }

    [Fact]
    public void IProvidesScrollMetadata_Implementation_CanProvidePageMetadata()
    {
        // Arrange
        var implementor = new TestScrollMetadata();

        // Act & Assert
        implementor.GetPageName().Should().Be("page");
        implementor.GetCurrentPage().Should().Be(2);
        implementor.GetPreviousPage().Should().Be(1);
        implementor.GetNextPage().Should().Be(3);
    }

    [Fact]
    public void IProvidesScrollMetadata_Implementation_CanReturnNullForPreviousPage()
    {
        // Arrange - First page
        var implementor = new TestScrollMetadataFirstPage();

        // Act & Assert
        implementor.GetPreviousPage().Should().BeNull();
        implementor.GetNextPage().Should().NotBeNull();
    }

    [Fact]
    public void IProvidesScrollMetadata_Implementation_CanReturnNullForNextPage()
    {
        // Arrange - Last page
        var implementor = new TestScrollMetadataLastPage();

        // Act & Assert
        implementor.GetNextPage().Should().BeNull();
        implementor.GetPreviousPage().Should().NotBeNull();
    }

    [Fact]
    public void IProvidesScrollMetadata_Implementation_CanUseCursorBasedPagination()
    {
        // Arrange
        var implementor = new TestScrollMetadataCursor();

        // Act & Assert
        implementor.GetPageName().Should().Be("cursor");
        implementor.GetCurrentPage().Should().Be("abc123");
        implementor.GetPreviousPage().Should().Be("xyz789");
        implementor.GetNextPage().Should().Be("def456");
    }

    [Fact]
    public void IProvidesScrollMetadata_Implementation_CanUseOffsetBasedPagination()
    {
        // Arrange
        var implementor = new TestScrollMetadataOffset();

        // Act & Assert
        implementor.GetPageName().Should().Be("offset");
        implementor.GetCurrentPage().Should().Be(20);
        implementor.GetPreviousPage().Should().Be(10);
        implementor.GetNextPage().Should().Be(30);
    }

    [Fact]
    public void IProvidesScrollMetadata_Implementation_RealWorldExample_PagedList()
    {
        // Arrange - Simulate a paged list result
        var implementor = new PagedListMetadata(
            currentPage: 3,
            totalPages: 10,
            pageSize: 15
        );

        // Act & Assert
        implementor.GetPageName().Should().Be("page");
        implementor.GetCurrentPage().Should().Be(3);
        implementor.GetPreviousPage().Should().Be(2);
        implementor.GetNextPage().Should().Be(4);
    }

    [Fact]
    public void IProvidesScrollMetadata_Implementation_RealWorldExample_EdgeCases()
    {
        // Arrange - Single page result
        var singlePage = new PagedListMetadata(
            currentPage: 1,
            totalPages: 1,
            pageSize: 15
        );

        // Act & Assert
        singlePage.GetPreviousPage().Should().BeNull();
        singlePage.GetNextPage().Should().BeNull();
    }

    // Test implementations
    private class TestScrollMetadata : IProvidesScrollMetadata
    {
        public string GetPageName() => "page";
        public object? GetPreviousPage() => 1;
        public object? GetNextPage() => 3;
        public object? GetCurrentPage() => 2;
    }

    private class TestScrollMetadataFirstPage : IProvidesScrollMetadata
    {
        public string GetPageName() => "page";
        public object? GetPreviousPage() => null;
        public object? GetNextPage() => 2;
        public object? GetCurrentPage() => 1;
    }

    private class TestScrollMetadataLastPage : IProvidesScrollMetadata
    {
        public string GetPageName() => "page";
        public object? GetPreviousPage() => 9;
        public object? GetNextPage() => null;
        public object? GetCurrentPage() => 10;
    }

    private class TestScrollMetadataCursor : IProvidesScrollMetadata
    {
        public string GetPageName() => "cursor";
        public object? GetPreviousPage() => "xyz789";
        public object? GetNextPage() => "def456";
        public object? GetCurrentPage() => "abc123";
    }

    private class TestScrollMetadataOffset : IProvidesScrollMetadata
    {
        public string GetPageName() => "offset";
        public object? GetPreviousPage() => 10;
        public object? GetNextPage() => 30;
        public object? GetCurrentPage() => 20;
    }

    private class PagedListMetadata : IProvidesScrollMetadata
    {
        private readonly int _currentPage;
        private readonly int _totalPages;

        public PagedListMetadata(int currentPage, int totalPages, int pageSize)
        {
            _currentPage = currentPage;
            _totalPages = totalPages;
        }

        public string GetPageName() => "page";

        public object? GetPreviousPage()
        {
            return _currentPage > 1 ? _currentPage - 1 : null;
        }

        public object? GetNextPage()
        {
            return _currentPage < _totalPages ? _currentPage + 1 : null;
        }

        public object? GetCurrentPage() => _currentPage;
    }
}
