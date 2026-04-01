using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Inertia.Tests;

public class InertiaHttpRequestExtensionsTests
{
    [Fact]
    public void IsInertia_WithInertiaHeader_ReturnsTrue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaderNames.Inertia] = "true";

        context.Request.IsInertia().Should().BeTrue();
    }

    [Fact]
    public void IsInertia_WithoutInertiaHeader_ReturnsFalse()
    {
        var context = new DefaultHttpContext();

        context.Request.IsInertia().Should().BeFalse();
    }

    [Fact]
    public void IsInertia_WithEmptyInertiaHeader_ReturnsTrue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaderNames.Inertia] = "";

        // ContainsKey returns true even for empty value
        context.Request.IsInertia().Should().BeTrue();
    }
}
