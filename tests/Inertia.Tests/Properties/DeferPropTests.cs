using Inertia.Core.Properties;
using Xunit;

namespace Inertia.Tests.Properties;

public class DeferPropTests
{
    [Fact]
    public void DeferProp_ImplementsIIgnoreFirstLoad()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Assert
        Assert.IsAssignableFrom<IIgnoreFirstLoad>(prop);
    }

    [Fact]
    public void DeferProp_ImplementsIMergeable()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Assert
        Assert.IsAssignableFrom<IMergeable>(prop);
    }

    [Fact]
    public void DeferProp_ImplementsIOnceable()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Assert
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    [Fact]
    public async Task ResolveAsync_WithSyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "deferred value";
        var prop = new DeferProp(() => expectedValue);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task ResolveAsync_WithAsyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = "async deferred value";
        var prop = new DeferProp(async () =>
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
    public void Constructor_WithGroup_StoresGroup()
    {
        // Arrange
        var groupName = "analytics";
        var prop = new DeferProp(() => "value", groupName);

        // Assert
        Assert.Equal(groupName, prop.Group);
    }

    [Fact]
    public void Constructor_WithoutGroup_GroupIsNull()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Assert
        Assert.Null(prop.Group);
    }

    [Fact]
    public void Constructor_WithNullSyncCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DeferProp((Func<object?>)null!));
    }

    [Fact]
    public void Constructor_WithNullAsyncCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DeferProp((Func<Task<object?>>)null!));
    }

    [Fact]
    public void IsOnce_DefaultValue_ReturnsFalse()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        var isOnce = prop.IsOnce();

        // Assert
        Assert.False(isOnce);
    }

    [Fact]
    public void Once_MarksPropertyAsOnce()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        prop.Once();

        // Assert
        Assert.True(prop.IsOnce());
    }

    [Fact]
    public void Once_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        var result = prop.Once();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void ShouldMerge_DefaultValue_ReturnsFalse()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        var shouldMerge = prop.ShouldMerge();

        // Assert
        Assert.False(shouldMerge);
    }

    [Fact]
    public void Merge_EnablesMerging()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        prop.Merge();

        // Assert
        Assert.True(prop.ShouldMerge());
    }

    [Fact]
    public void Merge_WithPath_SetsMergePath()
    {
        // Arrange
        var path = "data.items";
        var prop = new DeferProp(() => "value");

        // Act
        prop.Merge(path);

        // Assert
        Assert.Equal(path, prop.GetMergePath());
    }

    [Fact]
    public void Merge_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        var result = prop.Merge();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void IsDeepMerge_DefaultValue_ReturnsFalse()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        var isDeepMerge = prop.IsDeepMerge();

        // Assert
        Assert.False(isDeepMerge);
    }

    [Fact]
    public void DeepMerge_EnablesDeepMerging()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        prop.DeepMerge();

        // Assert
        Assert.True(prop.IsDeepMerge());
        Assert.True(prop.ShouldMerge());
    }

    [Fact]
    public void DeepMerge_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        var result = prop.DeepMerge();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void OnlyOnPartial_InterfaceMethod_DefaultValue_ReturnsFalse()
    {
        // Arrange
        IMergeable prop = new DeferProp(() => "value");

        // Act
        var onlyOnPartial = prop.OnlyOnPartial();

        // Assert
        Assert.False(onlyOnPartial);
    }

    [Fact]
    public void OnlyOnPartial_SetsFlag()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        prop.OnlyOnPartial();

        // Assert
        Assert.True(((IMergeable)prop).OnlyOnPartial());
    }

    [Fact]
    public void OnlyOnPartial_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        var result = prop.OnlyOnPartial();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void ChainedMethods_AllReturnThis()
    {
        // Arrange
        var prop = new DeferProp(() => "value");

        // Act
        var result = prop.Once().Merge("data").DeepMerge().OnlyOnPartial();

        // Assert
        Assert.Same(prop, result);
        Assert.True(prop.IsOnce());
        Assert.True(prop.ShouldMerge());
        Assert.True(prop.IsDeepMerge());
        Assert.True(((IMergeable)prop).OnlyOnPartial());
    }
}
