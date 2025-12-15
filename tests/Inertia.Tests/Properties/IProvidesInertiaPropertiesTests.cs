using FluentAssertions;
using Inertia.Core;
using Inertia.Core.Properties;

namespace Inertia.Tests.Properties;

/// <summary>
/// Tests for the IProvidesInertiaProperties interface.
/// </summary>
public class IProvidesInertiaPropertiesTests
{
    [Fact]
    public void IProvidesInertiaProperties_ShouldHaveToInertiaPropertiesMethod()
    {
        // Arrange & Act
        var methods = typeof(IProvidesInertiaProperties).GetMethods();

        // Assert
        methods.Should().ContainSingle(m => m.Name == nameof(IProvidesInertiaProperties.ToInertiaProperties));
    }

    [Fact]
    public void IProvidesInertiaProperties_ToInertiaPropertiesMethod_ShouldReturnDictionary()
    {
        // Arrange & Act
        var method = typeof(IProvidesInertiaProperties).GetMethod(nameof(IProvidesInertiaProperties.ToInertiaProperties));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Dictionary<string, object?>));
    }

    [Fact]
    public void IProvidesInertiaProperties_Implementation_CanReturnEmptyDictionary()
    {
        // Arrange
        var context = new RenderContext("TestComponent", new object());
        var implementor = new TestPropertiesProviderEmpty();

        // Act
        var result = implementor.ToInertiaProperties(context);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void IProvidesInertiaProperties_Implementation_CanReturnMultipleProperties()
    {
        // Arrange
        var context = new RenderContext("TestComponent", new object());
        var implementor = new TestPropertiesProviderMultiple();

        // Act
        var result = implementor.ToInertiaProperties(context);

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainKey("name");
        result.Should().ContainKey("email");
        result.Should().ContainKey("age");
    }

    [Fact]
    public void IProvidesInertiaProperties_Implementation_CanReturnDifferentTypes()
    {
        // Arrange
        var context = new RenderContext("TestComponent", new object());
        var implementor = new TestPropertiesProviderVariousTypes();

        // Act
        var result = implementor.ToInertiaProperties(context);

        // Assert
        result["string"].Should().BeOfType<string>();
        result["number"].Should().BeOfType<int>();
        result["boolean"].Should().BeOfType<bool>();
        result["array"].Should().BeOfType<int[]>();
    }

    [Fact]
    public void IProvidesInertiaProperties_Implementation_CanReturnNullValues()
    {
        // Arrange
        var context = new RenderContext("TestComponent", new object());
        var implementor = new TestPropertiesProviderWithNulls();

        // Act
        var result = implementor.ToInertiaProperties(context);

        // Assert
        result.Should().ContainKey("nullValue");
        result["nullValue"].Should().BeNull();
    }

    [Fact]
    public void IProvidesInertiaProperties_Implementation_RealWorldExample()
    {
        // Arrange - Simulate a view model
        var context = new RenderContext("TestComponent", new object());
        var implementor = new UserViewModel
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsAdmin = true
        };

        // Act
        var result = implementor.ToInertiaProperties(context);

        // Assert
        result.Should().HaveCount(4);
        result["firstName"].Should().Be("John");
        result["lastName"].Should().Be("Doe");
        result["email"].Should().Be("john@example.com");
        result["isAdmin"].Should().Be(true);
    }

    // Test implementations
    private class TestPropertiesProviderEmpty : IProvidesInertiaProperties
    {
        public Dictionary<string, object?> ToInertiaProperties(RenderContext context)
        {
            return new Dictionary<string, object?>();
        }
    }

    private class TestPropertiesProviderMultiple : IProvidesInertiaProperties
    {
        public Dictionary<string, object?> ToInertiaProperties(RenderContext context)
        {
            return new Dictionary<string, object?>
            {
                ["name"] = "Test User",
                ["email"] = "test@example.com",
                ["age"] = 30
            };
        }
    }

    private class TestPropertiesProviderVariousTypes : IProvidesInertiaProperties
    {
        public Dictionary<string, object?> ToInertiaProperties(RenderContext context)
        {
            return new Dictionary<string, object?>
            {
                ["string"] = "hello",
                ["number"] = 42,
                ["boolean"] = true,
                ["array"] = new[] { 1, 2, 3 }
            };
        }
    }

    private class TestPropertiesProviderWithNulls : IProvidesInertiaProperties
    {
        public Dictionary<string, object?> ToInertiaProperties(RenderContext context)
        {
            return new Dictionary<string, object?>
            {
                ["nullValue"] = null
            };
        }
    }

    private class UserViewModel : IProvidesInertiaProperties
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }

        public Dictionary<string, object?> ToInertiaProperties(RenderContext context)
        {
            return new Dictionary<string, object?>
            {
                ["firstName"] = FirstName,
                ["lastName"] = LastName,
                ["email"] = Email,
                ["isAdmin"] = IsAdmin
            };
        }
    }
}
