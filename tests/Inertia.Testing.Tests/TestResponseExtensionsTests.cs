using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Inertia.Testing.Tests;

public class TestResponseExtensionsTests
{
    [Fact]
    public void AssertInertia_ShouldReturnAssertableInertia()
    {
        // Arrange
        var response = CreateInertiaResponse();

        // Act
        var result = response.AssertInertia();

        // Assert
        result.Should().BeSameAs(response);
    }

    [Fact]
    public void AssertInertia_WithCallback_ShouldInvokeCallback()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var callbackInvoked = false;

        // Act
        response.AssertInertia(assertable =>
        {
            callbackInvoked = true;
            assertable.Should().NotBeNull();
        });

        // Assert
        callbackInvoked.Should().BeTrue();
    }

    [Fact]
    public void InertiaPage_ShouldReturnPageDictionary()
    {
        // Arrange
        var response = CreateInertiaResponse();

        // Act
        var page = response.InertiaPage();

        // Assert
        page.Should().ContainKey("component");
        page.Should().ContainKey("props");
        page.Should().ContainKey("url");
        page.Should().ContainKey("version");
        page["component"].Should().Be("Users/Index");
    }

    [Fact]
    public void InertiaProps_WithoutKey_ShouldReturnAllProps()
    {
        // Arrange
        var response = CreateInertiaResponse();

        // Act
        var props = response.InertiaProps();

        // Assert
        props.Should().NotBeNull();
        props.Should().BeOfType<Dictionary<string, object?>>();
    }

    [Fact]
    public void InertiaProps_WithKey_ShouldReturnSpecificProp()
    {
        // Arrange
        var response = CreateInertiaResponse();

        // Act
        var value = response.InertiaProps("user");

        // Assert
        value.Should().NotBeNull();
    }

    [Fact]
    public void InertiaProps_WithDotNotation_ShouldReturnNestedProp()
    {
        // Arrange
        var response = CreateInertiaResponse(new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com"
            }
        });

        // Act
        var value = response.InertiaProps("user.name");

        // Assert
        value.Should().Be("John Doe");
    }

    [Fact]
    public void InertiaProps_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var response = CreateInertiaResponse();

        // Act
        var value = response.InertiaProps("nonexistent");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void AssertInertia_WithoutPageData_ShouldThrowException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var response = context.Response;

        // Act & Assert
        var act = () => response.AssertInertia();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Response does not contain Inertia page data*");
    }

    private static HttpResponse CreateInertiaResponse(Dictionary<string, object?>? customProps = null)
    {
        var context = new DefaultHttpContext();
        var props = customProps ?? new Dictionary<string, object?>
        {
            ["user"] = new { name = "John Doe", id = 1 }
        };

        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Users/Index",
            ["props"] = props,
            ["url"] = "/users",
            ["version"] = "v1",
            ["encryptHistory"] = false,
            ["clearHistory"] = false
        };

        context.Items["InertiaPageData"] = pageData;

        return context.Response;
    }
}
