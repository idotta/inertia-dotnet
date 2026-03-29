using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Inertia.Tests.Contexts;

public class PropertyContextTests
{
    private static readonly Dictionary<string, object?> SampleProps = new()
    {
        ["name"] = "John",
        ["age"] = 30
    };

    [Fact]
    public void Constructor_SetsKey()
    {
        var context = new PropertyContext("users", SampleProps, new DefaultHttpContext());

        context.Key.Should().Be("users");
    }

    [Fact]
    public void Constructor_SetsProps()
    {
        var context = new PropertyContext("users", SampleProps, new DefaultHttpContext());

        context.Props.Should().BeSameAs(SampleProps);
    }

    [Fact]
    public void Constructor_SetsHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        var context = new PropertyContext("users", SampleProps, httpContext);

        context.HttpContext.Should().BeSameAs(httpContext);
    }

    [Fact]
    public void Constructor_NullKey_ThrowsArgumentException()
    {
        var act = () => new PropertyContext(null!, SampleProps, new DefaultHttpContext());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullProps_ThrowsArgumentNullException()
    {
        var act = () => new PropertyContext("users", null!, new DefaultHttpContext());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullHttpContext_ThrowsArgumentNullException()
    {
        var act = () => new PropertyContext("users", SampleProps, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
