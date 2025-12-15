using FluentAssertions;
using Inertia.Core;
using Microsoft.Extensions.Options;

namespace Inertia.Tests;

public class InertiaResponseFactoryTests
{
    private readonly InertiaResponseFactory _factory;
    private readonly InertiaOptions _options;

    public InertiaResponseFactoryTests()
    {
        _options = new InertiaOptions();
        _factory = new InertiaResponseFactory(Options.Create(_options));
    }

    [Fact]
    public async Task RenderAsync_ShouldCreateResponseWithComponent()
    {
        // Arrange
        var component = "Users/Index";

        // Act
        var response = await _factory.RenderAsync(component);

        // Assert
        response.Component.Should().Be(component);
    }

    [Fact]
    public async Task RenderAsync_WithProps_ShouldIncludeProps()
    {
        // Arrange
        var component = "Users/Index";
        var props = new Dictionary<string, object?> 
        { 
            ["users"] = new[] { "user1", "user2" }, 
            ["count"] = 2 
        };

        // Act
        var response = await _factory.RenderAsync(component, props);

        // Assert
        response.Props.Should().ContainKey("users");
        response.Props.Should().ContainKey("count");
        response.Props["count"].Should().Be(2);
    }

    [Fact]
    public async Task RenderAsync_ShouldMergeSharedProps()
    {
        // Arrange
        var component = "Users/Index";
        _factory.Share("auth", new { user = "John" });
        _factory.Share("flash", "Message");

        // Act
        var response = await _factory.RenderAsync(component, new Dictionary<string, object?> { ["data"] = "value" });

        // Assert
        response.Props.Should().ContainKey("auth");
        response.Props.Should().ContainKey("flash");
        response.Props.Should().ContainKey("data");
    }

    [Fact]
    public async Task RenderAsync_PropsOverrideSharedProps()
    {
        // Arrange
        var component = "Users/Index";
        _factory.Share("key", "shared");

        // Act
        var response = await _factory.RenderAsync(component, new Dictionary<string, object?> { ["key"] = "override" });

        // Assert
        response.Props["key"].Should().Be("override");
    }

    [Fact]
    public void Share_WithKeyValue_ShouldAddSharedProp()
    {
        // Arrange & Act
        _factory.Share("key", "value");

        // Assert
        _factory.GetShared("key").Should().Be("value");
    }

    [Fact]
    public void Share_WithDictionary_ShouldAddMultipleSharedProps()
    {
        // Arrange & Act
        _factory.Share(new Dictionary<string, object?> { ["prop1"] = "value1", ["prop2"] = "value2" });

        // Assert
        _factory.GetShared("prop1").Should().Be("value1");
        _factory.GetShared("prop2").Should().Be("value2");
    }

    [Fact]
    public void GetShared_WithoutKey_ShouldReturnAllSharedProps()
    {
        // Arrange
        _factory.Share("key1", "value1");
        _factory.Share("key2", "value2");

        // Act
        var shared = _factory.GetShared() as Dictionary<string, object?>;

        // Assert
        shared.Should().NotBeNull();
        shared.Should().ContainKey("key1");
        shared.Should().ContainKey("key2");
    }

    [Fact]
    public void GetShared_WithNonExistentKey_ShouldReturnDefault()
    {
        // Act
        var value = _factory.GetShared("nonexistent", "default");

        // Assert
        value.Should().Be("default");
    }

    [Fact]
    public void FlushShared_ShouldRemoveAllSharedProps()
    {
        // Arrange
        _factory.Share("key1", "value1");
        _factory.Share("key2", "value2");

        // Act
        _factory.FlushShared();

        // Assert
        var shared = _factory.GetShared() as Dictionary<string, object?>;
        shared.Should().NotBeNull();
        shared.Should().BeEmpty();
    }

    [Fact]
    public void SetVersion_WithString_ShouldSetVersion()
    {
        // Arrange & Act
        _factory.SetVersion("1.0.0");

        // Assert
        _factory.GetVersion().Should().Be("1.0.0");
    }

    [Fact]
    public void SetVersion_WithProvider_ShouldCallProvider()
    {
        // Arrange
        var callCount = 0;
        _factory.SetVersion(() =>
        {
            callCount++;
            return "2.0.0";
        });

        // Act
        var version1 = _factory.GetVersion();
        var version2 = _factory.GetVersion();

        // Assert
        version1.Should().Be("2.0.0");
        version2.Should().Be("2.0.0");
        callCount.Should().Be(2); // Provider should be called each time
    }

    [Fact]
    public void GetVersion_WithoutSetting_ShouldReturnEmptyString()
    {
        // Act
        var version = _factory.GetVersion();

        // Assert
        version.Should().BeEmpty();
    }

    [Fact]
    public async Task SetRootView_ShouldOverrideDefaultRootView()
    {
        // Arrange
        _factory.SetRootView("custom");

        // Act
        var response = await _factory.RenderAsync("Test");

        // Assert
        response.RootView.Should().Be("custom");
    }

    [Fact]
    public async Task RenderAsync_ShouldUseDefaultRootViewFromOptions()
    {
        // Arrange
        var options = new InertiaOptions { RootView = "custom" };
        var factory = new InertiaResponseFactory(Options.Create(options));

        // Act
        var response = await factory.RenderAsync("Test");

        // Assert
        response.RootView.Should().Be("custom");
    }

    [Fact]
    public async Task EncryptHistory_ShouldSetEncryptionFlag()
    {
        // Arrange
        _factory.EncryptHistory(true);

        // Act
        var response = await _factory.RenderAsync("Test");

        // Assert
        response.EncryptHistory.Should().BeTrue();
    }

    [Fact]
    public async Task EncryptHistory_DefaultValue_ShouldBeTrue()
    {
        // Arrange & Act
        _factory.EncryptHistory();
        var response = await _factory.RenderAsync("Test");

        // Assert
        response.EncryptHistory.Should().BeTrue();
    }

    [Fact]
    public void Location_ShouldReturnLocationResult()
    {
        // Arrange
        var url = "/users";

        // Act
        var result = _factory.Location(url);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<InertiaLocationResult>();
    }

    [Fact]
    public async Task ResolveUrlUsing_ShouldSetCustomUrlResolver()
    {
        // Arrange
        var customUrl = "/custom/url";
        _factory.ResolveUrlUsing(() => customUrl);

        // Act
        var response = await _factory.RenderAsync("Test");

        // Assert
        response.UrlResolver.Should().NotBeNull();
        response.UrlResolver().Should().Be(customUrl);
    }

    [Fact]
    public async Task RenderAsync_WithComponentValidation_ShouldThrowIfComponentNotFound()
    {
        // Arrange
        var options = new InertiaOptions
        {
            EnsurePagesExist = true,
            PagePaths = new List<string> { "/nonexistent" },
            PageExtensions = new List<string> { ".cshtml" }
        };
        var factory = new InertiaResponseFactory(Options.Create(options));

        // Act
        Func<Task> act = async () => await factory.RenderAsync("NonExistent");

        // Assert
        await act.Should().ThrowAsync<ComponentNotFoundException>()
            .WithMessage("*NonExistent*");
    }

    [Fact]
    public async Task RenderAsync_WithoutComponentValidation_ShouldNotThrow()
    {
        // Arrange
        var options = new InertiaOptions
        {
            EnsurePagesExist = false
        };
        var factory = new InertiaResponseFactory(Options.Create(options));

        // Act
        Func<Task> act = async () => await factory.RenderAsync("NonExistent");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void ClearHistory_ShouldThrowNotImplemented()
    {
        // Act
        Action act = () => _factory.ClearHistory();

        // Assert
        act.Should().Throw<NotImplementedException>()
            .WithMessage("*Phase 3*");
    }
}
