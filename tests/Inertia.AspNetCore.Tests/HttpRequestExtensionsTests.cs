using FluentAssertions;
using Inertia.AspNetCore;
using Inertia.Core;
using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore.Tests;

public class HttpRequestExtensionsTests
{
    private readonly DefaultHttpContext _context;
    private HttpRequest Request => _context.Request;

    public HttpRequestExtensionsTests()
    {
        _context = new DefaultHttpContext();
    }

    [Fact]
    public void IsInertia_WithInertiaHeader_ReturnsTrue()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";

        // Act
        var result = Request.IsInertia();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInertia_WithoutInertiaHeader_ReturnsFalse()
    {
        // Act
        var result = Request.IsInertia();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInertia_WithFalseInertiaHeader_ReturnsFalse()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "false";

        // Act
        var result = Request.IsInertia();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetInertiaVersion_WithVersionHeader_ReturnsVersion()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Version] = "abc123";

        // Act
        var result = Request.GetInertiaVersion();

        // Assert
        result.Should().Be("abc123");
    }

    [Fact]
    public void GetInertiaVersion_WithoutVersionHeader_ReturnsNull()
    {
        // Act
        var result = Request.GetInertiaVersion();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPartialComponent_WithComponentHeader_ReturnsComponent()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.PartialComponent] = "Users/Index";

        // Act
        var result = Request.GetPartialComponent();

        // Assert
        result.Should().Be("Users/Index");
    }

    [Fact]
    public void GetPartialComponent_WithoutComponentHeader_ReturnsNull()
    {
        // Act
        var result = Request.GetPartialComponent();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPartialData_WithPartialDataHeader_ReturnsArray()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.PartialData] = "users,posts,comments";

        // Act
        var result = Request.GetPartialData();

        // Assert
        result.Should().BeEquivalentTo(new[] { "users", "posts", "comments" });
    }

    [Fact]
    public void GetPartialData_WithPartialDataHeaderWithSpaces_ReturnsTrimmedArray()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.PartialData] = "users, posts , comments";

        // Act
        var result = Request.GetPartialData();

        // Assert
        result.Should().BeEquivalentTo(new[] { "users", "posts", "comments" });
    }

    [Fact]
    public void GetPartialData_WithoutPartialDataHeader_ReturnsEmptyArray()
    {
        // Act
        var result = Request.GetPartialData();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPartialExcept_WithPartialExceptHeader_ReturnsArray()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.PartialExcept] = "auth,flash";

        // Act
        var result = Request.GetPartialExcept();

        // Assert
        result.Should().BeEquivalentTo(new[] { "auth", "flash" });
    }

    [Fact]
    public void GetPartialExcept_WithoutPartialExceptHeader_ReturnsEmptyArray()
    {
        // Act
        var result = Request.GetPartialExcept();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetErrorBag_WithErrorBagHeader_ReturnsErrorBag()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.ErrorBag] = "createUser";

        // Act
        var result = Request.GetErrorBag();

        // Assert
        result.Should().Be("createUser");
    }

    [Fact]
    public void GetErrorBag_WithoutErrorBagHeader_ReturnsNull()
    {
        // Act
        var result = Request.GetErrorBag();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetReset_WithResetHeader_ReturnsArray()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Reset] = "errors,form";

        // Act
        var result = Request.GetReset();

        // Assert
        result.Should().BeEquivalentTo(new[] { "errors", "form" });
    }

    [Fact]
    public void GetReset_WithResetAllHeader_ReturnsAllArray()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Reset] = "all";

        // Act
        var result = Request.GetReset();

        // Assert
        result.Should().BeEquivalentTo(new[] { "all" });
    }

    [Fact]
    public void GetReset_WithoutResetHeader_ReturnsEmptyArray()
    {
        // Act
        var result = Request.GetReset();

        // Assert
        result.Should().BeEmpty();
    }
}
