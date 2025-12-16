using FluentAssertions;
using Inertia.Core;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Inertia.Testing.Tests;

public class ReloadRequestTests
{
    [Fact]
    public void Constructor_ShouldSetBasicProperties()
    {
        // Act
        var request = new ReloadRequest("/users", "Users/Index", "v1");

        // Assert
        request.Url.Should().Be("/users");
        request.Component.Should().Be("Users/Index");
        request.Version.Should().Be("v1");
    }

    [Fact]
    public void Constructor_WithoutVersion_ShouldAllowNullVersion()
    {
        // Act
        var request = new ReloadRequest("/users", "Users/Index");

        // Assert
        request.Version.Should().BeNull();
    }

    [Fact]
    public void ReloadOnly_WithSingleProp_ShouldConfigureHeaders()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index", "v1");

        // Act
        request.ReloadOnly("user");
        var headers = request.GetHeaders();

        // Assert
        headers.Should().ContainKey(InertiaHeaders.PartialComponent);
        headers.Should().ContainKey(InertiaHeaders.PartialData);
        headers[InertiaHeaders.PartialComponent].Should().Be("Users/Index");
        headers[InertiaHeaders.PartialData].Should().Be("user");
    }

    [Fact]
    public void ReloadOnly_WithMultipleProps_ShouldJoinWithCommas()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index", "v1");

        // Act
        request.ReloadOnly("user", "posts", "comments");
        var headers = request.GetHeaders();

        // Assert
        headers[InertiaHeaders.PartialData].Should().Be("user,posts,comments");
    }

    [Fact]
    public void ReloadOnly_WithEnumerable_ShouldConfigureHeaders()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index", "v1");
        var props = new List<string> { "user", "posts" };

        // Act
        request.ReloadOnly(props);
        var headers = request.GetHeaders();

        // Assert
        headers[InertiaHeaders.PartialData].Should().Be("user,posts");
    }

    [Fact]
    public void ReloadExcept_WithSingleProp_ShouldConfigureHeaders()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index", "v1");

        // Act
        request.ReloadExcept("stats");
        var headers = request.GetHeaders();

        // Assert
        headers.Should().ContainKey(InertiaHeaders.PartialComponent);
        headers.Should().ContainKey(InertiaHeaders.PartialExcept);
        headers[InertiaHeaders.PartialComponent].Should().Be("Users/Index");
        headers[InertiaHeaders.PartialExcept].Should().Be("stats");
    }

    [Fact]
    public void ReloadExcept_WithMultipleProps_ShouldJoinWithCommas()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index", "v1");

        // Act
        request.ReloadExcept("stats", "notifications");
        var headers = request.GetHeaders();

        // Assert
        headers[InertiaHeaders.PartialExcept].Should().Be("stats,notifications");
    }

    [Fact]
    public void ReloadExcept_WithEnumerable_ShouldConfigureHeaders()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index", "v1");
        var props = new List<string> { "stats", "notifications" };

        // Act
        request.ReloadExcept(props);
        var headers = request.GetHeaders();

        // Assert
        headers[InertiaHeaders.PartialExcept].Should().Be("stats,notifications");
    }

    [Fact]
    public void GetHeaders_WithVersion_ShouldIncludeVersionHeader()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index", "v1");

        // Act
        var headers = request.GetHeaders();

        // Assert
        headers.Should().ContainKey(InertiaHeaders.Version);
        headers[InertiaHeaders.Version].Should().Be("v1");
    }

    [Fact]
    public void GetHeaders_WithoutVersion_ShouldNotIncludeVersionHeader()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index");

        // Act
        var headers = request.GetHeaders();

        // Assert
        headers.Should().NotContainKey(InertiaHeaders.Version);
    }

    [Fact]
    public void ApplyToRequest_ShouldSetInertiaHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = new ReloadRequest("/users", "Users/Index", "v1");

        // Act
        request.ApplyToRequest(context.Request);

        // Assert
        context.Request.Headers.Should().ContainKey(InertiaHeaders.Inertia);
        context.Request.Headers[InertiaHeaders.Inertia].ToString().Should().Be("true");
    }

    [Fact]
    public void ApplyToRequest_WithReloadOnly_ShouldSetAllHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = new ReloadRequest("/users", "Users/Index", "v1")
            .ReloadOnly("user", "posts");

        // Act
        request.ApplyToRequest(context.Request);

        // Assert
        context.Request.Headers.Should().ContainKey(InertiaHeaders.Inertia);
        context.Request.Headers.Should().ContainKey(InertiaHeaders.Version);
        context.Request.Headers.Should().ContainKey(InertiaHeaders.PartialComponent);
        context.Request.Headers.Should().ContainKey(InertiaHeaders.PartialData);
        context.Request.Headers[InertiaHeaders.Version].ToString().Should().Be("v1");
        context.Request.Headers[InertiaHeaders.PartialData].ToString().Should().Be("user,posts");
    }

    [Fact]
    public void ReloadOnly_ShouldReturnSelfForChaining()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index", "v1");

        // Act
        var result = request.ReloadOnly("user");

        // Assert
        result.Should().BeSameAs(request);
    }

    [Fact]
    public void ReloadExcept_ShouldReturnSelfForChaining()
    {
        // Arrange
        var request = new ReloadRequest("/users", "Users/Index", "v1");

        // Act
        var result = request.ReloadExcept("stats");

        // Assert
        result.Should().BeSameAs(request);
    }
}
