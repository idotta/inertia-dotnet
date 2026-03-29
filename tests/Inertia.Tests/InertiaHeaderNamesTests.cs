using System.Reflection;
using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests;

public class InertiaHeaderNamesTests
{
    [Fact]
    public void Inertia_HasCorrectValue()
    {
        InertiaHeaderNames.Inertia.Should().Be("X-Inertia");
    }

    [Fact]
    public void ErrorBag_HasCorrectValue()
    {
        InertiaHeaderNames.ErrorBag.Should().Be("X-Inertia-Error-Bag");
    }

    [Fact]
    public void Location_HasCorrectValue()
    {
        InertiaHeaderNames.Location.Should().Be("X-Inertia-Location");
    }

    [Fact]
    public void Redirect_HasCorrectValue()
    {
        InertiaHeaderNames.Redirect.Should().Be("X-Inertia-Redirect");
    }

    [Fact]
    public void Version_HasCorrectValue()
    {
        InertiaHeaderNames.Version.Should().Be("X-Inertia-Version");
    }

    [Fact]
    public void PartialComponent_HasCorrectValue()
    {
        InertiaHeaderNames.PartialComponent.Should().Be("X-Inertia-Partial-Component");
    }

    [Fact]
    public void PartialOnly_HasCorrectValue()
    {
        InertiaHeaderNames.PartialOnly.Should().Be("X-Inertia-Partial-Data");
    }

    [Fact]
    public void PartialExcept_HasCorrectValue()
    {
        InertiaHeaderNames.PartialExcept.Should().Be("X-Inertia-Partial-Except");
    }

    [Fact]
    public void Reset_HasCorrectValue()
    {
        InertiaHeaderNames.Reset.Should().Be("X-Inertia-Reset");
    }

    [Fact]
    public void InfiniteScrollMergeIntent_HasCorrectValue()
    {
        InertiaHeaderNames.InfiniteScrollMergeIntent.Should().Be("X-Inertia-Infinite-Scroll-Merge-Intent");
    }

    [Fact]
    public void ExceptOnceProps_HasCorrectValue()
    {
        InertiaHeaderNames.ExceptOnceProps.Should().Be("X-Inertia-Except-Once-Props");
    }

    [Fact]
    public void AllHeaders_AreDistinct()
    {
        GetAllConstantValues().Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllHeaders_StartWithXInertia()
    {
        GetAllConstantValues().Should().AllSatisfy(
            value => value.Should().StartWith("X-Inertia"));
    }

    [Fact]
    public void Class_HasExactly11Constants()
    {
        GetAllConstantFields().Should().HaveCount(11);
    }

    private static IEnumerable<string> GetAllConstantValues() =>
        GetAllConstantFields().Select(f => (string)f.GetRawConstantValue()!);

    private static FieldInfo[] GetAllConstantFields() =>
        typeof(InertiaHeaderNames).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, FieldType.Name: "String" })
            .ToArray();
}
