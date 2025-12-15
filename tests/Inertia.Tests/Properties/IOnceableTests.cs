using FluentAssertions;
using Inertia.Core.Properties;

namespace Inertia.Tests.Properties;

/// <summary>
/// Tests for the IOnceable interface.
/// </summary>
public class IOnceableTests
{
    [Fact]
    public void IOnceable_ShouldHaveIsOnceMethod()
    {
        // Arrange & Act
        var methods = typeof(IOnceable).GetMethods();

        // Assert
        methods.Should().ContainSingle(m => m.Name == nameof(IOnceable.IsOnce));
    }

    [Fact]
    public void IOnceable_IsOnceMethod_ShouldReturnBoolean()
    {
        // Arrange & Act
        var method = typeof(IOnceable).GetMethod(nameof(IOnceable.IsOnce));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }

    [Fact]
    public void IOnceable_IsOnceMethod_ShouldHaveNoParameters()
    {
        // Arrange & Act
        var method = typeof(IOnceable).GetMethod(nameof(IOnceable.IsOnce));

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().BeEmpty();
    }

    [Fact]
    public void IOnceable_Implementation_CanReturnTrue()
    {
        // Arrange
        var implementor = new TestOnceableTrue();

        // Act
        var result = implementor.IsOnce();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IOnceable_Implementation_CanReturnFalse()
    {
        // Arrange
        var implementor = new TestOnceableFalse();

        // Act
        var result = implementor.IsOnce();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IOnceable_ShouldBeAssignableFromImplementingTypes()
    {
        // Arrange
        var implementor = new TestOnceableTrue();

        // Act
        var isAssignable = implementor is IOnceable;

        // Assert
        isAssignable.Should().BeTrue();
    }

    // Test implementations
    private class TestOnceableTrue : IOnceable
    {
        public bool IsOnce() => true;
    }

    private class TestOnceableFalse : IOnceable
    {
        public bool IsOnce() => false;
    }
}
