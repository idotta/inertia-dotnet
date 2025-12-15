using FluentAssertions;
using Inertia.Core;

namespace Inertia.Tests;

public class InertiaOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new InertiaOptions();

        // Assert
        options.RootView.Should().Be("app");
        options.EnsurePagesExist.Should().BeFalse();
        options.PagePaths.Should().BeEmpty();
        options.PageExtensions.Should().BeEmpty();
        options.UseScriptElement.Should().BeFalse();
        options.Ssr.Should().NotBeNull();
        options.Testing.Should().NotBeNull();
        options.History.Should().NotBeNull();
    }

    [Fact]
    public void RootView_CanBeSet()
    {
        // Arrange
        var options = new InertiaOptions();

        // Act
        options.RootView = "custom";

        // Assert
        options.RootView.Should().Be("custom");
    }

    [Fact]
    public void EnsurePagesExist_CanBeEnabled()
    {
        // Arrange
        var options = new InertiaOptions();

        // Act
        options.EnsurePagesExist = true;

        // Assert
        options.EnsurePagesExist.Should().BeTrue();
    }

    [Fact]
    public void PagePaths_CanBeConfigured()
    {
        // Arrange
        var options = new InertiaOptions();

        // Act
        options.PagePaths.Add("Pages");
        options.PagePaths.Add("Views");

        // Assert
        options.PagePaths.Should().HaveCount(2);
        options.PagePaths.Should().Contain("Pages");
        options.PagePaths.Should().Contain("Views");
    }
}

public class SsrOptionsTests
{
    [Fact]
    public void DefaultSsrOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new SsrOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.Url.Should().Be("http://127.0.0.1:13714");
        options.Bundle.Should().BeNull();
        options.EnsureBundleExists.Should().BeTrue();
    }

    [Fact]
    public void SsrEnabled_CanBeDisabled()
    {
        // Arrange
        var options = new SsrOptions();

        // Act
        options.Enabled = false;

        // Assert
        options.Enabled.Should().BeFalse();
    }

    [Fact]
    public void SsrUrl_CanBeSet()
    {
        // Arrange
        var options = new SsrOptions();

        // Act
        options.Url = "http://localhost:3000";

        // Assert
        options.Url.Should().Be("http://localhost:3000");
    }

    [Fact]
    public void SsrBundle_CanBeSet()
    {
        // Arrange
        var options = new SsrOptions();

        // Act
        options.Bundle = "wwwroot/ssr/ssr.mjs";

        // Assert
        options.Bundle.Should().Be("wwwroot/ssr/ssr.mjs");
    }
}

public class TestingOptionsTests
{
    [Fact]
    public void DefaultTestingOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new TestingOptions();

        // Assert
        options.EnsurePagesExist.Should().BeTrue();
        options.PagePaths.Should().BeEmpty();
        options.PageExtensions.Should().BeEmpty();
    }
}

public class HistoryOptionsTests
{
    [Fact]
    public void DefaultHistoryOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new HistoryOptions();

        // Assert
        options.Encrypt.Should().BeFalse();
    }

    [Fact]
    public void HistoryEncrypt_CanBeEnabled()
    {
        // Arrange
        var options = new HistoryOptions();

        // Act
        options.Encrypt = true;

        // Assert
        options.Encrypt.Should().BeTrue();
    }
}
