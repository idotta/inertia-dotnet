using Inertia.Core.Properties;
using Xunit;

namespace Inertia.Tests.Properties;

public class OncePropTests
{
    [Fact]
    public void OnceProp_ImplementsIOnceable()
    {
        // Arrange
        var prop = new OnceProp(() => "value");

        // Assert
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    [Fact]
    public async Task ResolveAsync_WithSyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "once value";
        var prop = new OnceProp(() => expectedValue);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task ResolveAsync_WithAsyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "async once value";
        var prop = new OnceProp(async () =>
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
        Assert.Throws<ArgumentNullException>(() => new OnceProp((Func<object?>)null!));
    }

    [Fact]
    public void Constructor_WithNullAsyncCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OnceProp((Func<Task<object?>>)null!));
    }

    [Fact]
    public void IsOnce_AlwaysReturnsTrue()
    {
        // Arrange
        var prop = new OnceProp(() => "value");

        // Act
        var isOnce = prop.IsOnce();

        // Assert
        Assert.True(isOnce);
    }

    [Fact]
    public async Task ResolveAsync_WithExpensiveOperation_ExecutesCallback()
    {
        // Arrange
        var executed = false;
        var prop = new OnceProp(async () =>
        {
            await Task.Delay(10);
            executed = true;
            return "result";
        });

        // Act
        await prop.ResolveAsync();

        // Assert
        Assert.True(executed);
    }
}
