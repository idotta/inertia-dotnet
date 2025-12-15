using Inertia.Core.Properties;
using Xunit;

namespace Inertia.Tests.Properties;

public class MergePropTests
{
    [Fact]
    public void MergeProp_ImplementsIMergeable()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Assert
        Assert.IsAssignableFrom<IMergeable>(prop);
    }

    [Fact]
    public void MergeProp_ImplementsIOnceable()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Assert
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    [Fact]
    public async Task ResolveAsync_WithStaticValue_ReturnsValue()
    {
        // Arrange
        var value = "merge value";
        var prop = new MergeProp(value);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task ResolveAsync_WithSyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "callback merge value";
        var prop = new MergeProp(() => expectedValue);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task ResolveAsync_WithAsyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "async merge value";
        var prop = new MergeProp(async () =>
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
        Assert.Throws<ArgumentNullException>(() => new MergeProp((Func<object?>)null!));
    }

    [Fact]
    public void Constructor_WithNullAsyncCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MergeProp((Func<Task<object?>>)null!));
    }

    [Fact]
    public void ShouldMerge_AlwaysReturnsTrue()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        var shouldMerge = prop.ShouldMerge();

        // Assert
        Assert.True(shouldMerge);
    }

    [Fact]
    public void GetMergePath_DefaultValue_ReturnsNull()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        var path = prop.GetMergePath();

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void WithPath_SetsMergePath()
    {
        // Arrange
        var path = "data.items";
        var prop = new MergeProp("value");

        // Act
        prop.WithPath(path);

        // Assert
        Assert.Equal(path, prop.GetMergePath());
    }

    [Fact]
    public void WithPath_WithNullPath_ThrowsArgumentNullException()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => prop.WithPath(null!));
    }

    [Fact]
    public void WithPath_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        var result = prop.WithPath("data");

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void IsDeepMerge_DefaultValue_ReturnsFalse()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        var isDeepMerge = prop.IsDeepMerge();

        // Assert
        Assert.False(isDeepMerge);
    }

    [Fact]
    public void DeepMerge_EnablesDeepMerging()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        prop.DeepMerge();

        // Assert
        Assert.True(prop.IsDeepMerge());
    }

    [Fact]
    public void DeepMerge_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        var result = prop.DeepMerge();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void OnlyOnPartial_InterfaceMethod_DefaultValue_ReturnsFalse()
    {
        // Arrange
        IMergeable prop = new MergeProp("value");

        // Act
        var onlyOnPartial = prop.OnlyOnPartial();

        // Assert
        Assert.False(onlyOnPartial);
    }

    [Fact]
    public void OnlyOnPartial_SetsFlag()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        prop.OnlyOnPartial();

        // Assert
        Assert.True(((IMergeable)prop).OnlyOnPartial());
    }

    [Fact]
    public void OnlyOnPartial_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        var result = prop.OnlyOnPartial();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void IsOnce_DefaultValue_ReturnsFalse()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        var isOnce = prop.IsOnce();

        // Assert
        Assert.False(isOnce);
    }

    [Fact]
    public void Once_MarksPropertyAsOnce()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        prop.Once();

        // Assert
        Assert.True(prop.IsOnce());
    }

    [Fact]
    public void Once_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        var result = prop.Once();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void ChainedMethods_AllReturnThis()
    {
        // Arrange
        var prop = new MergeProp("value");

        // Act
        var result = prop.WithPath("data").DeepMerge().OnlyOnPartial().Once();

        // Assert
        Assert.Same(prop, result);
        Assert.Equal("data", prop.GetMergePath());
        Assert.True(prop.IsDeepMerge());
        Assert.True(((IMergeable)prop).OnlyOnPartial());
        Assert.True(prop.IsOnce());
    }
}
