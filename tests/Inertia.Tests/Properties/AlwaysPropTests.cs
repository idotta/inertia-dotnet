using Inertia.Core.Properties;
using Xunit;

namespace Inertia.Tests.Properties;

public class AlwaysPropTests
{
    [Fact]
    public async Task ResolveAsync_WithStaticValue_ReturnsValue()
    {
        // Arrange
        var value = "test value";
        var prop = new AlwaysProp(value);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task ResolveAsync_WithSyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "callback result";
        var prop = new AlwaysProp(() => expectedValue);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task ResolveAsync_WithAsyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "async callback result";
        var prop = new AlwaysProp(async () =>
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
    public async Task ResolveAsync_WithNullValue_ReturnsNull()
    {
        // Arrange
        var prop = new AlwaysProp((object?)null);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_WithNullSyncCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AlwaysProp((Func<object?>)null!));
    }

    [Fact]
    public void Constructor_WithNullAsyncCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AlwaysProp((Func<Task<object?>>)null!));
    }

    [Fact]
    public void IsCallable_WithStaticValue_ReturnsFalse()
    {
        // Arrange
        var prop = new AlwaysProp("value");

        // Act
        var isCallable = prop.IsCallable;

        // Assert
        Assert.False(isCallable);
    }

    [Fact]
    public void IsCallable_WithSyncCallback_ReturnsTrue()
    {
        // Arrange
        var prop = new AlwaysProp(() => "value");

        // Act
        var isCallable = prop.IsCallable;

        // Assert
        Assert.True(isCallable);
    }

    [Fact]
    public void IsCallable_WithAsyncCallback_ReturnsTrue()
    {
        // Arrange
        var prop = new AlwaysProp(async () => await Task.FromResult("value"));

        // Act
        var isCallable = prop.IsCallable;

        // Assert
        Assert.True(isCallable);
    }

    [Fact]
    public async Task ResolveAsync_WithComplexObject_ReturnsObject()
    {
        // Arrange
        var complexObject = new { Name = "Test", Count = 42 };
        var prop = new AlwaysProp(complexObject);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(complexObject, result);
    }
}
