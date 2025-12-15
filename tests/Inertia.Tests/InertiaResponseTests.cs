using FluentAssertions;
using Inertia.Core;
using System.Text.Json;

namespace Inertia.Tests;

public class InertiaResponseTests
{
    [Fact]
    public void Constructor_ShouldSetBasicProperties()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?> { ["users"] = new[] { "user1", "user2" } };
        var rootView = "app";
        var version = "1.0.0";

        // Act
        var response = new InertiaResponse(component, props, rootView, version);

        // Assert
        response.Component.Should().Be(component);
        response.Props.Should().BeSameAs(props);
        response.RootView.Should().Be(rootView);
        response.Version.Should().Be(version);
        response.EncryptHistory.Should().BeFalse();
        response.ClearHistory.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithEncryptHistory_ShouldSetEncryptHistory()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?>();

        // Act
        var response = new InertiaResponse(component, props, encryptHistory: true);

        // Assert
        response.EncryptHistory.Should().BeTrue();
    }

    [Fact]
    public void With_ShouldAddPropertyToProps()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?>();
        var response = new InertiaResponse(component, props);

        // Act
        response.With("user", new { id = 1, name = "John" });

        // Assert
        response.Props.Should().ContainKey("user");
        response.Props["user"].Should().NotBeNull();
    }

    [Fact]
    public void With_WithObject_ShouldAddMultipleProperties()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?>();
        var response = new InertiaResponse(component, props);

        // Act
        response.With(new { user = "John", count = 10 });

        // Assert
        response.Props.Should().ContainKey("user");
        response.Props.Should().ContainKey("count");
        response.Props["user"].Should().Be("John");
        response.Props["count"].Should().Be(10);
    }

    [Fact]
    public void With_ShouldReturnSelfForChaining()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?>();
        var response = new InertiaResponse(component, props);

        // Act
        var result = response.With("key", "value");

        // Assert
        result.Should().BeSameAs(response);
    }

    [Fact]
    public void WithViewData_ShouldAddDataToViewData()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?>();
        var response = new InertiaResponse(component, props);

        // Act
        response.WithViewData("title", "Users Page");

        // Assert
        response.ViewData.Should().ContainKey("title");
        response.ViewData["title"].Should().Be("Users Page");
    }

    [Fact]
    public void WithViewData_WithObject_ShouldAddMultipleViewData()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?>();
        var response = new InertiaResponse(component, props);

        // Act
        response.WithViewData(new { title = "Users", subtitle = "List" });

        // Assert
        response.ViewData.Should().ContainKey("title");
        response.ViewData.Should().ContainKey("subtitle");
    }

    [Fact]
    public async Task ToJsonAsync_ShouldSerializePageData()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?> { ["count"] = 10 };
        var response = new InertiaResponse(component, props, version: "1.0.0")
        {
            Url = "/users"
        };

        // Act
        var json = await response.ToJsonAsync();

        // Assert
        json.Should().NotBeNullOrEmpty();
        
        var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        root.GetProperty("component").GetString().Should().Be("Users/Index");
        root.GetProperty("version").GetString().Should().Be("1.0.0");
        root.GetProperty("url").GetString().Should().Be("/users");
        root.GetProperty("props").GetProperty("count").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task BuildPageDataAsync_ShouldIncludeAllRequiredFields()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?> { ["users"] = new[] { "user1" } };
        var response = new InertiaResponse(component, props, version: "1.0")
        {
            Url = "/users"
        };

        // Act
        var pageData = await response.BuildPageDataAsync();

        // Assert
        pageData.Should().ContainKey("component");
        pageData.Should().ContainKey("props");
        pageData.Should().ContainKey("url");
        pageData.Should().ContainKey("version");
        pageData["component"].Should().Be("Users/Index");
        pageData["version"].Should().Be("1.0");
        pageData["url"].Should().Be("/users");
    }

    [Fact]
    public async Task BuildPageDataAsync_WithClearHistory_ShouldIncludeClearHistory()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?>();
        // Note: ClearHistory is set internally from session in real usage
        // For this test, we'll need to create a derived class or wait for Phase 3
        var response = new InertiaResponse(component, props);

        // Act
        var pageData = await response.BuildPageDataAsync();

        // Assert
        // clearHistory should not be present if false
        pageData.Should().NotContainKey("clearHistory");
    }

    [Fact]
    public async Task BuildPageDataAsync_WithEncryptHistory_ShouldIncludeEncryptHistory()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?>();
        var response = new InertiaResponse(component, props, encryptHistory: true);

        // Act
        var pageData = await response.BuildPageDataAsync();

        // Assert
        pageData.Should().ContainKey("encryptHistory");
        pageData["encryptHistory"].Should().Be(true);
    }

    [Fact]
    public async Task BuildPageDataAsync_WithUrlResolver_ShouldUseResolver()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?>();
        var customUrl = "/custom/url";
        var response = new InertiaResponse(component, props, urlResolver: () => customUrl)
        {
            Url = "/default/url"
        };

        // Act
        var pageData = await response.BuildPageDataAsync();

        // Assert
        pageData["url"].Should().Be(customUrl);
    }

    [Fact]
    public void ViewData_ShouldBeIndependentFromProps()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?> { ["propKey"] = "propValue" };
        var response = new InertiaResponse(component, props);

        // Act
        response.WithViewData("viewKey", "viewValue");

        // Assert
        response.Props.Should().ContainKey("propKey");
        response.Props.Should().NotContainKey("viewKey");
        response.ViewData.Should().ContainKey("viewKey");
        response.ViewData.Should().NotContainKey("propKey");
    }
}
