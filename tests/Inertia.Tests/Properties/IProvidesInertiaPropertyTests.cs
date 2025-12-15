using FluentAssertions;
using Inertia.Core;
using Inertia.Core.Properties;

namespace Inertia.Tests.Properties;

/// <summary>
/// Tests for the IProvidesInertiaProperty interface.
/// </summary>
public class IProvidesInertiaPropertyTests
{
    [Fact]
    public void IProvidesInertiaProperty_ShouldHaveToInertiaPropertyMethod()
    {
        // Arrange & Act
        var methods = typeof(IProvidesInertiaProperty).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == nameof(IProvidesInertiaProperty.ToInertiaProperty));
    }

    [Fact]
    public void IProvidesInertiaProperty_ToInertiaProperty_ShouldAcceptContextParameter()
    {
        // Arrange & Act
        var method = typeof(IProvidesInertiaProperty).GetMethod(nameof(IProvidesInertiaProperty.ToInertiaProperty));

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(PropertyContext));
    }

    [Fact]
    public void IProvidesInertiaProperty_ToInertiaProperty_ShouldReturnNullableObject()
    {
        // Arrange & Act
        var method = typeof(IProvidesInertiaProperty).GetMethod(nameof(IProvidesInertiaProperty.ToInertiaProperty));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(object));
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_CanProvideValue()
    {
        // Arrange
        var implementor = new TestPropertyProvider();
        var context = new PropertyContext("test", new Dictionary<string, object?>(), new object());

        // Act
        var value = implementor.ToInertiaProperty(context);

        // Assert
        value.Should().Be("testValue");
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_CanReturnNullValue()
    {
        // Arrange
        var implementor = new TestPropertyProviderWithNull();
        var context = new PropertyContext("test", new Dictionary<string, object?>(), new object());

        // Act
        var value = implementor.ToInertiaProperty(context);

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_CanReturnDifferentTypes()
    {
        // Arrange
        var context = new PropertyContext("test", new Dictionary<string, object?>(), new object());
        var stringProvider = new TestPropertyProviderString();
        var numberProvider = new TestPropertyProviderNumber();
        var boolProvider = new TestPropertyProviderBool();
        var arrayProvider = new TestPropertyProviderArray();

        // Act & Assert
        stringProvider.ToInertiaProperty(context).Should().BeOfType<string>();
        numberProvider.ToInertiaProperty(context).Should().BeOfType<int>();
        boolProvider.ToInertiaProperty(context).Should().BeOfType<bool>();
        arrayProvider.ToInertiaProperty(context).Should().BeOfType<int[]>();
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_RealWorldExample_CurrentDate()
    {
        // Arrange
        var context = new PropertyContext("test", new Dictionary<string, object?>(), new object());
        var implementor = new CurrentDateProvider();

        // Act
        var value = implementor.ToInertiaProperty(context);

        // Assert
        value.Should().NotBeNull();
        value.Should().BeOfType<string>();
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_RealWorldExample_AppVersion()
    {
        // Arrange
        var context = new PropertyContext("test", new Dictionary<string, object?>(), new object());
        var implementor = new AppVersionProvider("1.2.3");

        // Act
        var value = implementor.ToInertiaProperty(context);

        // Assert
        value.Should().Be("1.2.3");
    }

    // Test implementations
    private class TestPropertyProvider : IProvidesInertiaProperty
    {
        public object? ToInertiaProperty(PropertyContext context) => "testValue";
    }

    private class TestPropertyProviderWithNull : IProvidesInertiaProperty
    {
        public object? ToInertiaProperty(PropertyContext context) => null;
    }

    private class TestPropertyProviderString : IProvidesInertiaProperty
    {
        public object? ToInertiaProperty(PropertyContext context) => "hello";
    }

    private class TestPropertyProviderNumber : IProvidesInertiaProperty
    {
        public object? ToInertiaProperty(PropertyContext context) => 42;
    }

    private class TestPropertyProviderBool : IProvidesInertiaProperty
    {
        public object? ToInertiaProperty(PropertyContext context) => true;
    }

    private class TestPropertyProviderArray : IProvidesInertiaProperty
    {
        public object? ToInertiaProperty(PropertyContext context) => new[] { 1, 2, 3 };
    }

    private class CurrentDateProvider : IProvidesInertiaProperty
    {
        public object? ToInertiaProperty(PropertyContext context) => DateTime.Now.ToString("yyyy-MM-dd");
    }

    private class AppVersionProvider : IProvidesInertiaProperty
    {
        private readonly string _version;

        public AppVersionProvider(string version)
        {
            _version = version;
        }

        public object? ToInertiaProperty(PropertyContext context) => _version;
    }
}
