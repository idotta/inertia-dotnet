using FluentAssertions;
using Inertia.Core.Properties;

namespace Inertia.Tests.Properties;

/// <summary>
/// Tests for the IIgnoreFirstLoad marker interface.
/// </summary>
public class IIgnoreFirstLoadTests
{
    [Fact]
    public void IIgnoreFirstLoad_ShouldBeAssignableFromImplementingTypes()
    {
        // Arrange
        var implementor = new TestIgnoreFirstLoad();

        // Act
        var isAssignable = implementor is IIgnoreFirstLoad;

        // Assert
        isAssignable.Should().BeTrue();
    }

    [Fact]
    public void IIgnoreFirstLoad_ShouldHaveNoMethods()
    {
        // Arrange & Act
        var methods = typeof(IIgnoreFirstLoad).GetMethods();

        // Assert - Should only have methods inherited from object
        methods.Should().BeEmpty("IIgnoreFirstLoad is a marker interface with no methods");
    }

    [Fact]
    public void IIgnoreFirstLoad_ShouldBeAnInterface()
    {
        // Arrange & Act
        var type = typeof(IIgnoreFirstLoad);

        // Assert
        type.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IIgnoreFirstLoad_CanBeUsedForTypeChecking()
    {
        // Arrange
        var implementor = new TestIgnoreFirstLoad();
        var nonImplementor = new object();

        // Act
        var implementorCheck = implementor is IIgnoreFirstLoad;
        var nonImplementorCheck = nonImplementor is IIgnoreFirstLoad;

        // Assert
        implementorCheck.Should().BeTrue();
        nonImplementorCheck.Should().BeFalse();
    }

    // Test implementation
    private class TestIgnoreFirstLoad : IIgnoreFirstLoad
    {
    }
}
