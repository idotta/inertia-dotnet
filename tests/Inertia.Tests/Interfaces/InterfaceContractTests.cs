using System.Reflection;
using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Interfaces;

public class InterfaceContractTests
{
    [Theory]
    [MemberData(nameof(AllInterfaceTypes))]
    public void AllInterfaces_ArePublic(Type interfaceType)
    {
        interfaceType.IsInterface.Should().BeTrue(
            because: $"{interfaceType.Name} should be an interface");
        interfaceType.IsPublic.Should().BeTrue(
            because: $"{interfaceType.Name} should be public");
    }

    public static TheoryData<Type> AllInterfaceTypes =>
    [
        typeof(IIgnoreFirstLoad),
        typeof(IDeferrable),
        typeof(IMergeable),
        typeof(IOnceable),
        typeof(IInertiaPropertyProvider),
        typeof(IInertiaPropertyValueProvider),
        typeof(IScrollMetadataProvider),
    ];

    [Fact]
    public void IIgnoreFirstLoad_IsMarkerInterface()
    {
        typeof(IIgnoreFirstLoad)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Should().BeEmpty("marker interface should have no members");
    }

    [Fact]
    public void IDeferrable_HasShouldDeferAndGroup()
    {
        var type = typeof(IDeferrable);

        type.GetProperty("ShouldDefer").Should().NotBeNull();
        type.GetProperty("ShouldDefer")!.PropertyType.Should().Be(typeof(bool));

        type.GetProperty("Group").Should().NotBeNull();
        type.GetProperty("Group")!.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void IMergeable_HasExpectedMembers()
    {
        var type = typeof(IMergeable);
        var memberNames = type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToList();

        memberNames.Should().Contain("ShouldMerge");
        memberNames.Should().Contain("ShouldDeepMerge");
        memberNames.Should().Contain("AppendsAtRoot");
    }

    [Fact]
    public void IOnceable_HasExpectedMembers()
    {
        var type = typeof(IOnceable);
        var memberNames = type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToList();

        memberNames.Should().Contain("ShouldResolveOnce");
        memberNames.Should().Contain("ShouldBeRefreshed");
        memberNames.Should().Contain("ExpiresAt");
    }

    [Fact]
    public void IInertiaPropertyProvider_HasToInertiaProperties()
    {
        var method = typeof(IInertiaPropertyProvider)
            .GetMethod("ToInertiaProperties");

        method.Should().NotBeNull();
        method!.GetParameters()
            .Should().ContainSingle()
            .Which.ParameterType.Should().Be(typeof(RenderContext));
    }

    [Fact]
    public void IInertiaPropertyValueProvider_HasToInertiaProperty()
    {
        var method = typeof(IInertiaPropertyValueProvider)
            .GetMethod("ToInertiaProperty");

        method.Should().NotBeNull();
        method!.GetParameters()
            .Should().ContainSingle()
            .Which.ParameterType.Should().Be(typeof(PropertyContext));
    }

    [Fact]
    public void IScrollMetadataProvider_HasExpectedProperties()
    {
        var type = typeof(IScrollMetadataProvider);

        type.GetProperty("PageName").Should().NotBeNull();
        type.GetProperty("PreviousPage").Should().NotBeNull();
        type.GetProperty("NextPage").Should().NotBeNull();
        type.GetProperty("CurrentPage").Should().NotBeNull();
    }
}
