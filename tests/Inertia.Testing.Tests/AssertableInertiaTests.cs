using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Inertia.Testing.Tests;

public class AssertableInertiaTests
{
    [Fact]
    public void FromResponse_WithValidResponse_ShouldCreateInstance()
    {
        // Arrange
        var response = CreateInertiaResponse();

        // Act
        var assertable = AssertableInertia.FromResponse(response);

        // Assert
        assertable.Should().NotBeNull();
    }

    [Fact]
    public void FromResponse_WithoutPageData_ShouldThrowException()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act & Assert
        var act = () => AssertableInertia.FromResponse(context.Response);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Response does not contain Inertia page data*");
    }

    [Fact]
    public void WithComponent_WithMatchingComponent_ShouldSucceed()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var result = assertable.WithComponent("Users/Index");
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void WithComponent_WithNonMatchingComponent_ShouldThrowException()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var act = () => assertable.WithComponent("Posts/Index");
        act.Should().Throw<Xunit.Sdk.EqualException>();
    }

    [Fact]
    public void WithUrl_WithMatchingUrl_ShouldSucceed()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var result = assertable.WithUrl("/users");
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void WithUrl_WithNonMatchingUrl_ShouldThrowException()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var act = () => assertable.WithUrl("/posts");
        act.Should().Throw<Xunit.Sdk.EqualException>();
    }

    [Fact]
    public void WithVersion_WithMatchingVersion_ShouldSucceed()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var result = assertable.WithVersion("v1");
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void Has_WithExistingProperty_ShouldSucceed()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var result = assertable.Has("user");
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void Has_WithNonExistingProperty_ShouldThrowException()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var act = () => assertable.Has("nonexistent");
        act.Should().Throw<Xunit.Sdk.NotNullException>();
    }

    [Fact]
    public void Missing_WithNonExistingProperty_ShouldSucceed()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var result = assertable.Missing("nonexistent");
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void Missing_WithExistingProperty_ShouldThrowException()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var act = () => assertable.Missing("user");
        act.Should().Throw<Xunit.Sdk.NullException>();
    }

    [Fact]
    public void Where_WithMatchingValue_ShouldSucceed()
    {
        // Arrange
        var response = CreateInertiaResponse(new Dictionary<string, object?>
        {
            ["count"] = 42
        });
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var result = assertable.Where("count", 42);
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void Where_WithNonMatchingValue_ShouldThrowException()
    {
        // Arrange
        var response = CreateInertiaResponse(new Dictionary<string, object?>
        {
            ["count"] = 42
        });
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var act = () => assertable.Where("count", 100);
        act.Should().Throw<Xunit.Sdk.EqualException>();
    }

    [Fact]
    public void Where_WithPredicate_ShouldEvaluatePredicate()
    {
        // Arrange
        var response = CreateInertiaResponse(new Dictionary<string, object?>
        {
            ["count"] = 42
        });
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var result = assertable.Where("count", value => Convert.ToInt32(value) > 40);
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void Where_WithFailingPredicate_ShouldThrowException()
    {
        // Arrange
        var response = CreateInertiaResponse(new Dictionary<string, object?>
        {
            ["count"] = 42
        });
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var act = () => assertable.Where("count", value => Convert.ToInt32(value) > 50);
        act.Should().Throw<Xunit.Sdk.TrueException>();
    }

    [Fact]
    public void WhereType_WithCorrectType_ShouldSucceed()
    {
        // Arrange
        var response = CreateInertiaResponse(new Dictionary<string, object?>
        {
            ["name"] = "John Doe"
        });
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var result = assertable.WhereType("name", typeof(string));
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void WithCount_WithCorrectCount_ShouldSucceed()
    {
        // Arrange
        var response = CreateInertiaResponse(new Dictionary<string, object?>
        {
            ["items"] = new List<int> { 1, 2, 3 }
        });
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var result = assertable.WithCount("items", 3);
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void WithCount_WithIncorrectCount_ShouldThrowException()
    {
        // Arrange
        var response = CreateInertiaResponse(new Dictionary<string, object?>
        {
            ["items"] = new List<int> { 1, 2, 3 }
        });
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var act = () => assertable.WithCount("items", 5);
        act.Should().Throw<Xunit.Sdk.EqualException>();
    }

    [Fact]
    public void ToArray_ShouldReturnPageData()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act
        var array = assertable.ToArray();

        // Assert
        array.Should().ContainKey("component");
        array.Should().ContainKey("props");
        array.Should().ContainKey("url");
        array.Should().ContainKey("version");
        array.Should().ContainKey("encryptHistory");
        array.Should().ContainKey("clearHistory");
        array["component"].Should().Be("Users/Index");
        array["url"].Should().Be("/users");
        array["version"].Should().Be("v1");
    }

    [Fact]
    public void Dump_ShouldReturnSelf()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act
        var result = assertable.Dump();

        // Assert
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void Dd_ShouldThrowException()
    {
        // Arrange
        var response = CreateInertiaResponse();
        var assertable = AssertableInertia.FromResponse(response);

        // Act & Assert
        var act = () => assertable.Dd();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Dd() was called*");
    }

    [Fact]
    public void LoadDeferredProps_WithGroups_ShouldReturnSelf()
    {
        // Arrange
        var response = CreateInertiaResponseWithDeferredProps();
        var assertable = AssertableInertia.FromResponse(response);

        // Act
        var result = assertable.LoadDeferredProps(new[] { "default" });

        // Assert
        result.Should().BeSameAs(assertable);
    }

    [Fact]
    public void LoadDeferredProps_WithCallback_ShouldInvokeCallback()
    {
        // Arrange
        var response = CreateInertiaResponseWithDeferredProps();
        var assertable = AssertableInertia.FromResponse(response);
        var callbackInvoked = false;

        // Act
        assertable.LoadDeferredProps(new[] { "default" }, a =>
        {
            callbackInvoked = true;
            a.Should().NotBeNull();
        });

        // Assert
        callbackInvoked.Should().BeTrue();
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

    private static HttpResponse CreateInertiaResponseWithDeferredProps()
    {
        var context = new DefaultHttpContext();
        var props = new Dictionary<string, object?>
        {
            ["user"] = new { name = "John Doe", id = 1 }
        };

        var deferredProps = new Dictionary<string, List<string>>
        {
            ["default"] = new List<string> { "stats", "notifications" }
        };

        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Users/Index",
            ["props"] = props,
            ["url"] = "/users",
            ["version"] = "v1",
            ["encryptHistory"] = false,
            ["clearHistory"] = false,
            ["deferredProps"] = deferredProps
        };

        context.Items["InertiaPageData"] = pageData;

        return context.Response;
    }
}
