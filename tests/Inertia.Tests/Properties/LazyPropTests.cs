using Inertia.Core.Properties;
using Xunit;

namespace Inertia.Tests.Properties;

public class LazyPropTests
{
    [Fact]
    public void LazyProp_InheritsFromOptionalProp()
    {
        // Arrange
        var prop = new LazyProp(() => "value");

        // Assert
        Assert.IsAssignableFrom<OptionalProp>(prop);
    }

    [Fact]
    public void LazyProp_ImplementsIIgnoreFirstLoad()
    {
        // Arrange
        var prop = new LazyProp(() => "value");

        // Assert
        Assert.IsAssignableFrom<IIgnoreFirstLoad>(prop);
    }

    [Fact]
    public void LazyProp_ImplementsIOnceable()
    {
        // Arrange
        var prop = new LazyProp(() => "value");

        // Assert
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    [Fact]
    public async Task ResolveAsync_WithSyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "lazy value";
#pragma warning disable CS0618 // Type or member is obsolete
        var prop = new LazyProp(() => expectedValue);
#pragma warning restore CS0618

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task ResolveAsync_WithAsyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "async lazy value";
#pragma warning disable CS0618 // Type or member is obsolete
        var prop = new LazyProp(async () =>
        {
            await Task.Delay(1);
            return expectedValue;
        });
#pragma warning restore CS0618

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void LazyProp_IsMarkedObsolete()
    {
        // Arrange
        var type = typeof(LazyProp);

        // Act
        var obsoleteAttribute = type.GetCustomAttributes(typeof(ObsoleteAttribute), false)
            .Cast<ObsoleteAttribute>()
            .FirstOrDefault();

        // Assert
        Assert.NotNull(obsoleteAttribute);
        Assert.Contains("OptionalProp", obsoleteAttribute.Message);
    }

    [Fact]
    public void Once_WorksWithLazyProp()
    {
        // Arrange
#pragma warning disable CS0618 // Type or member is obsolete
        var prop = new LazyProp(() => "value");
#pragma warning restore CS0618

        // Act
        prop.Once();

        // Assert
        Assert.True(prop.IsOnce());
    }
}
