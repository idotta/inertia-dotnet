using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Inertia.Tests.Contexts;

public class RenderContextTests
{
    [Fact]
    public void Constructor_SetsComponent()
    {
        var context = new RenderContext("Users/Index", new DefaultHttpContext());

        context.Component.Should().Be("Users/Index");
    }

    [Fact]
    public void Constructor_SetsHttpContext()
    {
        var httpContext = new DefaultHttpContext();

        var context = new RenderContext("Users/Index", httpContext);

        context.HttpContext.Should().BeSameAs(httpContext);
    }

    [Fact]
    public void Constructor_NullComponent_ThrowsArgumentException()
    {
        var act = () => new RenderContext(null!, new DefaultHttpContext());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyComponent_ThrowsArgumentException()
    {
        var act = () => new RenderContext("", new DefaultHttpContext());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullHttpContext_ThrowsArgumentNullException()
    {
        var act = () => new RenderContext("Users/Index", null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
