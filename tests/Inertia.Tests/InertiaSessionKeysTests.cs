using System.Reflection;
using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests;

public class InertiaSessionKeysTests
{
    [Fact]
    public void ClearHistory_HasCorrectValue()
    {
        InertiaSessionKeys.ClearHistory.Should().Be("inertia.clear_history");
    }

    [Fact]
    public void FlashData_HasCorrectValue()
    {
        InertiaSessionKeys.FlashData.Should().Be("inertia.flash_data");
    }

    [Fact]
    public void PreserveFragment_HasCorrectValue()
    {
        InertiaSessionKeys.PreserveFragment.Should().Be("inertia.preserve_fragment");
    }

    [Fact]
    public void AllKeys_AreDistinct()
    {
        GetAllConstantValues().Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllKeys_StartWithInertiaPrefix()
    {
        GetAllConstantValues().Should().AllSatisfy(
            value => value.Should().StartWith("inertia."));
    }

    [Fact]
    public void Class_HasExactly3Constants()
    {
        GetAllConstantFields().Should().HaveCount(3);
    }

    private static IEnumerable<string> GetAllConstantValues() =>
        GetAllConstantFields().Select(f => (string)f.GetRawConstantValue()!);

    private static FieldInfo[] GetAllConstantFields() =>
        typeof(InertiaSessionKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, FieldType.Name: "String" })
            .ToArray();
}
