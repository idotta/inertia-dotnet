using Inertia.Core.Ssr;
using Xunit;

namespace Inertia.Tests.Ssr;

public class BundleDetectorTests : IDisposable
{
    private readonly string _tempDirectory;

    public BundleDetectorTests()
    {
        // Create a temporary directory for testing
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"InertiaTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        // Clean up temporary directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Detect_WithNoBundle_ReturnsNull()
    {
        // Act
        var result = BundleDetector.Detect(_tempDirectory);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Detect_WithWwwrootSsrMjs_FindsBundle()
    {
        // Arrange
        var bundlePath = Path.Combine(_tempDirectory, "wwwroot", "ssr", "ssr.mjs");
        Directory.CreateDirectory(Path.GetDirectoryName(bundlePath)!);
        File.WriteAllText(bundlePath, "// SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(bundlePath), result);
    }

    [Fact]
    public void Detect_WithWwwrootSsrJs_FindsBundle()
    {
        // Arrange
        var bundlePath = Path.Combine(_tempDirectory, "wwwroot", "ssr", "ssr.js");
        Directory.CreateDirectory(Path.GetDirectoryName(bundlePath)!);
        File.WriteAllText(bundlePath, "// SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(bundlePath), result);
    }

    [Fact]
    public void Detect_WithBootstrapSsrMjs_FindsBundle()
    {
        // Arrange
        var bundlePath = Path.Combine(_tempDirectory, "bootstrap", "ssr", "ssr.mjs");
        Directory.CreateDirectory(Path.GetDirectoryName(bundlePath)!);
        File.WriteAllText(bundlePath, "// SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(bundlePath), result);
    }

    [Fact]
    public void Detect_WithPublicSsrMjs_FindsBundle()
    {
        // Arrange
        var bundlePath = Path.Combine(_tempDirectory, "public", "ssr", "ssr.mjs");
        Directory.CreateDirectory(Path.GetDirectoryName(bundlePath)!);
        File.WriteAllText(bundlePath, "// SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(bundlePath), result);
    }

    [Fact]
    public void Detect_WithCustomPath_FindsBundle()
    {
        // Arrange
        var customPath = Path.Combine(_tempDirectory, "custom", "ssr.mjs");
        Directory.CreateDirectory(Path.GetDirectoryName(customPath)!);
        File.WriteAllText(customPath, "// SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory, "custom/ssr.mjs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(customPath), result);
    }

    [Fact]
    public void Detect_WithCustomPathAndDefaultPath_PrefersCustomPath()
    {
        // Arrange
        var customPath = Path.Combine(_tempDirectory, "custom", "ssr.mjs");
        var defaultPath = Path.Combine(_tempDirectory, "wwwroot", "ssr", "ssr.mjs");

        Directory.CreateDirectory(Path.GetDirectoryName(customPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(defaultPath)!);
        File.WriteAllText(customPath, "// Custom SSR bundle");
        File.WriteAllText(defaultPath, "// Default SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory, "custom/ssr.mjs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(customPath), result);
    }

    [Fact]
    public void Detect_WithAbsoluteCustomPath_FindsBundle()
    {
        // Arrange
        var absolutePath = Path.Combine(_tempDirectory, "absolute", "ssr.mjs");
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        File.WriteAllText(absolutePath, "// SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory, absolutePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(absolutePath), result);
    }

    [Fact]
    public void Detect_WithNullBasePath_UsesCurrentDirectory()
    {
        // Act
        var result = BundleDetector.Detect(basePath: null);

        // Assert - should not throw and may return null if no bundle exists
        // This test just verifies the method doesn't crash with null basePath
        Assert.True(result == null || File.Exists(result));
    }

    [Fact]
    public void Detect_WithMultipleCustomPaths_FindsFirstExisting()
    {
        // Arrange
        var customPath1 = Path.Combine(_tempDirectory, "custom1", "ssr.mjs");
        var customPath2 = Path.Combine(_tempDirectory, "custom2", "ssr.mjs");

        // Only create the second one
        Directory.CreateDirectory(Path.GetDirectoryName(customPath2)!);
        File.WriteAllText(customPath2, "// Custom SSR bundle 2");

        // Act
        var result = BundleDetector.Detect(_tempDirectory, "custom1/ssr.mjs", "custom2/ssr.mjs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(customPath2), result);
    }

    [Fact]
    public void Detect_WithEmptyCustomPath_IgnoresAndSearchesDefaults()
    {
        // Arrange
        var defaultPath = Path.Combine(_tempDirectory, "wwwroot", "ssr", "ssr.mjs");
        Directory.CreateDirectory(Path.GetDirectoryName(defaultPath)!);
        File.WriteAllText(defaultPath, "// Default SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory, "");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(defaultPath), result);
    }

    [Fact]
    public void Detect_WithNullInCustomPaths_IgnoresNullAndContinues()
    {
        // Arrange
        var customPath = Path.Combine(_tempDirectory, "custom", "ssr.mjs");
        Directory.CreateDirectory(Path.GetDirectoryName(customPath)!);
        File.WriteAllText(customPath, "// Custom SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory, null!, "custom/ssr.mjs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(customPath), result);
    }

    [Fact]
    public void Detect_ReturnsFullPath_NotRelativePath()
    {
        // Arrange
        var bundlePath = Path.Combine(_tempDirectory, "wwwroot", "ssr", "ssr.mjs");
        Directory.CreateDirectory(Path.GetDirectoryName(bundlePath)!);
        File.WriteAllText(bundlePath, "// SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory);

        // Assert
        Assert.NotNull(result);
        Assert.True(Path.IsPathRooted(result));
    }

    [Fact]
    public void Detect_WithMultipleDefaultBundles_ReturnsFirstFound()
    {
        // Arrange
        var wwwrootPath = Path.Combine(_tempDirectory, "wwwroot", "ssr", "ssr.mjs");
        var bootstrapPath = Path.Combine(_tempDirectory, "bootstrap", "ssr", "ssr.mjs");

        Directory.CreateDirectory(Path.GetDirectoryName(wwwrootPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(bootstrapPath)!);
        File.WriteAllText(wwwrootPath, "// Wwwroot SSR bundle");
        File.WriteAllText(bootstrapPath, "// Bootstrap SSR bundle");

        // Act
        var result = BundleDetector.Detect(_tempDirectory);

        // Assert - should return wwwroot path as it's first in the search order
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(wwwrootPath), result);
    }
}
