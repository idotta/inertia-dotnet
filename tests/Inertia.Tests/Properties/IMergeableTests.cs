using FluentAssertions;
using Inertia.Core.Properties;

namespace Inertia.Tests.Properties;

/// <summary>
/// Tests for the IMergeable interface.
/// </summary>
public class IMergeableTests
{
    [Fact]
    public void IMergeable_ShouldHaveAllRequiredMethods()
    {
        // Arrange & Act
        var methods = typeof(IMergeable).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == nameof(IMergeable.ShouldMerge));
        methods.Should().Contain(m => m.Name == nameof(IMergeable.GetMergePath));
        methods.Should().Contain(m => m.Name == nameof(IMergeable.IsDeepMerge));
        methods.Should().Contain(m => m.Name == nameof(IMergeable.OnlyOnPartial));
    }

    [Fact]
    public void IMergeable_ShouldMerge_ShouldReturnBoolean()
    {
        // Arrange & Act
        var method = typeof(IMergeable).GetMethod(nameof(IMergeable.ShouldMerge));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }

    [Fact]
    public void IMergeable_GetMergePath_ShouldReturnNullableString()
    {
        // Arrange & Act
        var method = typeof(IMergeable).GetMethod(nameof(IMergeable.GetMergePath));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void IMergeable_IsDeepMerge_ShouldReturnBoolean()
    {
        // Arrange & Act
        var method = typeof(IMergeable).GetMethod(nameof(IMergeable.IsDeepMerge));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }

    [Fact]
    public void IMergeable_OnlyOnPartial_ShouldReturnBoolean()
    {
        // Arrange & Act
        var method = typeof(IMergeable).GetMethod(nameof(IMergeable.OnlyOnPartial));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }

    [Fact]
    public void IMergeable_Implementation_CanConfigureShallowMerge()
    {
        // Arrange
        var implementor = new TestMergeableShallow();

        // Act & Assert
        implementor.ShouldMerge().Should().BeTrue();
        implementor.IsDeepMerge().Should().BeFalse();
        implementor.GetMergePath().Should().BeNull();
        implementor.OnlyOnPartial().Should().BeFalse();
    }

    [Fact]
    public void IMergeable_Implementation_CanConfigureDeepMerge()
    {
        // Arrange
        var implementor = new TestMergeableDeep();

        // Act & Assert
        implementor.ShouldMerge().Should().BeTrue();
        implementor.IsDeepMerge().Should().BeTrue();
    }

    [Fact]
    public void IMergeable_Implementation_CanSpecifyMergePath()
    {
        // Arrange
        var implementor = new TestMergeableWithPath();

        // Act & Assert
        implementor.GetMergePath().Should().Be("data.items");
    }

    [Fact]
    public void IMergeable_Implementation_CanRestrictToPartialOnly()
    {
        // Arrange
        var implementor = new TestMergeablePartialOnly();

        // Act & Assert
        implementor.OnlyOnPartial().Should().BeTrue();
    }

    [Fact]
    public void IMergeable_Implementation_CanDisableMerge()
    {
        // Arrange
        var implementor = new TestMergeableDisabled();

        // Act & Assert
        implementor.ShouldMerge().Should().BeFalse();
    }

    // Test implementations
    private class TestMergeableShallow : IMergeable
    {
        public bool ShouldMerge() => true;
        public string? GetMergePath() => null;
        public bool IsDeepMerge() => false;
        public bool OnlyOnPartial() => false;
    }

    private class TestMergeableDeep : IMergeable
    {
        public bool ShouldMerge() => true;
        public string? GetMergePath() => null;
        public bool IsDeepMerge() => true;
        public bool OnlyOnPartial() => false;
    }

    private class TestMergeableWithPath : IMergeable
    {
        public bool ShouldMerge() => true;
        public string? GetMergePath() => "data.items";
        public bool IsDeepMerge() => false;
        public bool OnlyOnPartial() => false;
    }

    private class TestMergeablePartialOnly : IMergeable
    {
        public bool ShouldMerge() => true;
        public string? GetMergePath() => null;
        public bool IsDeepMerge() => false;
        public bool OnlyOnPartial() => true;
    }

    private class TestMergeableDisabled : IMergeable
    {
        public bool ShouldMerge() => false;
        public string? GetMergePath() => null;
        public bool IsDeepMerge() => false;
        public bool OnlyOnPartial() => false;
    }
}
