using FluentAssertions;
using Inertia.Core.Properties;

namespace Inertia.Tests.Properties;

/// <summary>
/// Tests for the IProvidesInertiaProperty interface.
/// </summary>
public class IProvidesInertiaPropertyTests
{
    [Fact]
    public void IProvidesInertiaProperty_ShouldHaveGetKeyMethod()
    {
        // Arrange & Act
        var methods = typeof(IProvidesInertiaProperty).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == nameof(IProvidesInertiaProperty.GetKey));
    }

    [Fact]
    public void IProvidesInertiaProperty_ShouldHaveGetValueMethod()
    {
        // Arrange & Act
        var methods = typeof(IProvidesInertiaProperty).GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == nameof(IProvidesInertiaProperty.GetValue));
    }

    [Fact]
    public void IProvidesInertiaProperty_GetKey_ShouldReturnString()
    {
        // Arrange & Act
        var method = typeof(IProvidesInertiaProperty).GetMethod(nameof(IProvidesInertiaProperty.GetKey));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void IProvidesInertiaProperty_GetValue_ShouldReturnNullableObject()
    {
        // Arrange & Act
        var method = typeof(IProvidesInertiaProperty).GetMethod(nameof(IProvidesInertiaProperty.GetValue));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(object));
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_CanProvideKeyAndValue()
    {
        // Arrange
        var implementor = new TestPropertyProvider();

        // Act
        var key = implementor.GetKey();
        var value = implementor.GetValue();

        // Assert
        key.Should().Be("testKey");
        value.Should().Be("testValue");
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_CanReturnNullValue()
    {
        // Arrange
        var implementor = new TestPropertyProviderWithNull();

        // Act
        var key = implementor.GetKey();
        var value = implementor.GetValue();

        // Assert
        key.Should().Be("nullProperty");
        value.Should().BeNull();
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_CanReturnDifferentTypes()
    {
        // Arrange
        var stringProvider = new TestPropertyProviderString();
        var numberProvider = new TestPropertyProviderNumber();
        var boolProvider = new TestPropertyProviderBool();
        var arrayProvider = new TestPropertyProviderArray();

        // Act & Assert
        stringProvider.GetValue().Should().BeOfType<string>();
        numberProvider.GetValue().Should().BeOfType<int>();
        boolProvider.GetValue().Should().BeOfType<bool>();
        arrayProvider.GetValue().Should().BeOfType<int[]>();
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_RealWorldExample_CurrentDate()
    {
        // Arrange
        var implementor = new CurrentDateProvider();

        // Act
        var key = implementor.GetKey();
        var value = implementor.GetValue();

        // Assert
        key.Should().Be("currentDate");
        value.Should().NotBeNull();
        value.Should().BeOfType<string>();
    }

    [Fact]
    public void IProvidesInertiaProperty_Implementation_RealWorldExample_AppVersion()
    {
        // Arrange
        var implementor = new AppVersionProvider("1.2.3");

        // Act
        var key = implementor.GetKey();
        var value = implementor.GetValue();

        // Assert
        key.Should().Be("appVersion");
        value.Should().Be("1.2.3");
    }

    // Test implementations
    private class TestPropertyProvider : IProvidesInertiaProperty
    {
        public string GetKey() => "testKey";
        public object? GetValue() => "testValue";
    }

    private class TestPropertyProviderWithNull : IProvidesInertiaProperty
    {
        public string GetKey() => "nullProperty";
        public object? GetValue() => null;
    }

    private class TestPropertyProviderString : IProvidesInertiaProperty
    {
        public string GetKey() => "stringProp";
        public object? GetValue() => "hello";
    }

    private class TestPropertyProviderNumber : IProvidesInertiaProperty
    {
        public string GetKey() => "numberProp";
        public object? GetValue() => 42;
    }

    private class TestPropertyProviderBool : IProvidesInertiaProperty
    {
        public string GetKey() => "boolProp";
        public object? GetValue() => true;
    }

    private class TestPropertyProviderArray : IProvidesInertiaProperty
    {
        public string GetKey() => "arrayProp";
        public object? GetValue() => new[] { 1, 2, 3 };
    }

    private class CurrentDateProvider : IProvidesInertiaProperty
    {
        public string GetKey() => "currentDate";
        public object? GetValue() => DateTime.Now.ToString("yyyy-MM-dd");
    }

    private class AppVersionProvider : IProvidesInertiaProperty
    {
        private readonly string _version;

        public AppVersionProvider(string version)
        {
            _version = version;
        }

        public string GetKey() => "appVersion";
        public object? GetValue() => _version;
    }
}
