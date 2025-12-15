using Inertia.Core.Properties;
using Xunit;

namespace Inertia.Tests.Properties;

public class ScrollPropTests
{
    [Fact]
    public void ScrollProp_ImplementsIMergeable()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Assert
        Assert.IsAssignableFrom<IMergeable>(prop);
    }

    [Fact]
    public async Task ResolveAsync_WithStaticValue_ReturnsValue()
    {
        // Arrange
        var value = new[] { 1, 2, 3 };
        var prop = new ScrollProp(value);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task ResolveAsync_WithSyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = new[] { 4, 5, 6 };
        var prop = new ScrollProp(() => expectedValue);

        // Act
        var result = await prop.ResolveAsync();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task ResolveAsync_WithAsyncCallback_ReturnsCallbackResult()
    {
        // Arrange
        var expectedValue = new[] { 7, 8, 9 };
        var prop = new ScrollProp(async () =>
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
        Assert.Throws<ArgumentNullException>(() => new ScrollProp((Func<object?>)null!));
    }

    [Fact]
    public void Constructor_WithNullAsyncCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ScrollProp((Func<Task<object?>>)null!));
    }

    [Fact]
    public void ShouldMerge_AlwaysReturnsTrue()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        var shouldMerge = prop.ShouldMerge();

        // Assert
        Assert.True(shouldMerge);
    }

    [Fact]
    public void IsDeepMerge_AlwaysReturnsFalse()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        var isDeepMerge = prop.IsDeepMerge();

        // Assert
        Assert.False(isDeepMerge);
    }

    [Fact]
    public void OnlyOnPartial_AlwaysReturnsTrue()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        var onlyOnPartial = prop.OnlyOnPartial();

        // Assert
        Assert.True(onlyOnPartial);
    }

    [Fact]
    public void IsPrepend_DefaultValue_ReturnsFalse()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Assert
        Assert.False(prop.IsPrepend);
    }

    [Fact]
    public void Append_SetsMergePathAndDirection()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        prop.Append();

        // Assert
        Assert.False(prop.IsPrepend);
        Assert.Null(prop.GetMergePath());
    }

    [Fact]
    public void Append_WithPath_SetsMergePath()
    {
        // Arrange
        var path = "data";
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        prop.Append(path);

        // Assert
        Assert.False(prop.IsPrepend);
        Assert.Equal(path, prop.GetMergePath());
    }

    [Fact]
    public void Append_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        var result = prop.Append();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void Prepend_SetsMergePathAndDirection()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        prop.Prepend();

        // Assert
        Assert.True(prop.IsPrepend);
        Assert.Null(prop.GetMergePath());
    }

    [Fact]
    public void Prepend_WithPath_SetsMergePath()
    {
        // Arrange
        var path = "data";
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        prop.Prepend(path);

        // Assert
        Assert.True(prop.IsPrepend);
        Assert.Equal(path, prop.GetMergePath());
    }

    [Fact]
    public void Prepend_ReturnsThis_ForMethodChaining()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        var result = prop.Prepend();

        // Assert
        Assert.Same(prop, result);
    }

    [Fact]
    public void Wrapper_ReturnsWrapperValue()
    {
        // Arrange
        var wrapper = "data";
        var prop = new ScrollProp(new[] { 1, 2, 3 }, wrapper);

        // Assert
        Assert.Equal(wrapper, prop.Wrapper);
    }

    [Fact]
    public void ConfigureMergeIntent_WithAppend_SetsDirection()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        prop.ConfigureMergeIntent("append");

        // Assert
        Assert.False(prop.IsPrepend);
    }

    [Fact]
    public void ConfigureMergeIntent_WithPrepend_SetsDirection()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        prop.ConfigureMergeIntent("prepend");

        // Assert
        Assert.True(prop.IsPrepend);
    }

    [Fact]
    public void ConfigureMergeIntent_WithNull_DoesNotThrow()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act & Assert
        var exception = Record.Exception(() => prop.ConfigureMergeIntent(null));
        Assert.Null(exception);
    }

    [Fact]
    public void ConfigureMergeIntent_WithEmptyString_DoesNotThrow()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act & Assert
        var exception = Record.Exception(() => prop.ConfigureMergeIntent(""));
        Assert.Null(exception);
    }

    [Fact]
    public void ConfigureMergeIntent_CaseInsensitive()
    {
        // Arrange
        var prop1 = new ScrollProp(new[] { 1, 2, 3 });
        var prop2 = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        prop1.ConfigureMergeIntent("APPEND");
        prop2.ConfigureMergeIntent("PREPEND");

        // Assert
        Assert.False(prop1.IsPrepend);
        Assert.True(prop2.IsPrepend);
    }

    [Fact]
    public async Task GetMetadataAsync_WithoutProvider_ReturnsNull()
    {
        // Arrange
        var prop = new ScrollProp(new[] { 1, 2, 3 });

        // Act
        var metadata = await prop.GetMetadataAsync();

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public async Task GetMetadataAsync_WithProvider_ReturnsMetadata()
    {
        // Arrange
        var expectedMetadata = new ScrollMetadata("page", 1, null, 2);
        var prop = new ScrollProp(
            new[] { 1, 2, 3 },
            metadataProvider: _ => expectedMetadata);

        // Act
        var metadata = await prop.GetMetadataAsync();

        // Assert
        Assert.Same(expectedMetadata, metadata);
    }
}
