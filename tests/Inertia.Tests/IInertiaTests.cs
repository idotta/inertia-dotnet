using System.Reflection;
using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests;

public class IInertiaTests
{
    private static readonly Type InterfaceType = typeof(IInertia);

    public class InterfaceContract
    {
        [Fact]
        public void IsPublicInterface()
        {
            InterfaceType.IsInterface.Should().BeTrue();
            InterfaceType.IsPublic.Should().BeTrue();
        }

        [Fact]
        public void HasRenderWithObjectOverload()
        {
            var method = InterfaceType.GetMethod("Render", [typeof(string), typeof(object)]);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(InertiaResponse));
        }

        [Fact]
        public void HasRenderWithDictionaryOverload()
        {
            var method = InterfaceType.GetMethod("Render", [typeof(string), typeof(IDictionary<string, object?>)]);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(InertiaResponse));
        }

        [Fact]
        public void HasLocationMethod()
        {
            var method = InterfaceType.GetMethod("Location", [typeof(string)]);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(InertiaLocationResult));
        }

        [Fact]
        public void HasShareStringKeyOverload()
        {
            var method = InterfaceType.GetMethod("Share", [typeof(string), typeof(object)]);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(void));
        }

        [Fact]
        public void HasShareDictionaryOverload()
        {
            var method = InterfaceType.GetMethod("Share", [typeof(IDictionary<string, object?>)]);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(void));
        }

        [Fact]
        public void HasShareProviderOverload()
        {
            var method = InterfaceType.GetMethod("Share", [typeof(IInertiaPropertyProvider)]);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(void));
        }

        [Fact]
        public void HasFlashStringKeyOverload()
        {
            var method = InterfaceType.GetMethod("Flash", [typeof(string), typeof(object)]);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(void));
        }

        [Fact]
        public void HasFlashDictionaryOverload()
        {
            var method = InterfaceType.GetMethod("Flash", [typeof(IDictionary<string, object?>)]);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(void));
        }

        [Fact]
        public void HasGetFlashedMethod()
        {
            var method = InterfaceType.GetMethod("GetFlashed", Type.EmptyTypes);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(IDictionary<string, object?>));
        }

        [Fact]
        public void HasClearHistoryMethod()
        {
            var method = InterfaceType.GetMethod("ClearHistory", Type.EmptyTypes);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(void));
        }

        [Fact]
        public void HasPreserveFragmentMethod()
        {
            var method = InterfaceType.GetMethod("PreserveFragment", Type.EmptyTypes);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(void));
        }

        [Fact]
        public void HasEncryptHistoryMethod()
        {
            var method = InterfaceType.GetMethod("EncryptHistory", [typeof(bool)]);

            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(void));
        }
    }
}
