using Inertia.Core.Properties;
using Xunit;

namespace Inertia.Tests.Properties;

public class OptionalPropTests
{
    [Fact]
    public void OptionalProp_ImplementsIIgnoreFirstLoad()
    {
        // Arrange
        var prop = new OptionalProp(() => "value");

        // Assert
        Assert.IsAssignableFrom<IIgnoreFirstLoad>(prop);
    }

    [Fact]
    public void OptionalProp_ImplementsIOnceable()
    {
        // Arrange
        var prop = new OptionalProp(() => "value");

        // Assert
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    [Fact]
    public async Task ResolveAsync_WithSyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "optional value";
        var prop = new OptionalProp(() => expectedValue);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task ResolveAsync_WithAsyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "async optional value";
        var prop = new OptionalProp(async () =>
        {
            await Task.Delay(1);
            return expectedValue;
        });

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void Constructor_WithNullSyncCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OptionalProp((Func<object?>)null!));
    }

    [Fact]
    public void Constructor_WithNullAsyncCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OptionalProp((Func<Task<object?>>)null!));
    }

    [Fact]
    public void IsOnce_DefaultValue_ReturnsFalse()
    {
        // Arrange
        var prop = new OptionalProp(() => "value");

        // Act
        var isOnce = prop.IsOnce();

        // Assert
        Assert.False(isOnce);
    }

    [Fact]
    public void Once_MarksPropertyAsOnce()
    {
        // Arrange
        var prop = new OptionalProp(() => "value");

        // Act
        prop.Once();

        // Assert
        Assert.True(prop.IsOnce());
    }

    [Fact]
    public void Once_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new OptionalProp(() => "value");

        // Act
        var result = prop.Once();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public async Task ResolveAsync_CalledMultipleTimes_ExecutesCallbackEachTime()
    {
        // Arrange
        var callCount = 0;
        var prop = new OptionalProp(() =>
        {
            callCount++;
            return callCount;
        });

        // Act
        var result1 = await prop.ResolveAsync();
        var result2 = await prop.ResolveAsync();

        // Assert
        Assert.Equal(1, result1);
        Assert.Equal(2, result2);
        Assert.Equal(2, callCount);
    }
}
