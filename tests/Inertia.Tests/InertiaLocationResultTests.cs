using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Inertia.Tests;

public class InertiaLocationResultTests
{
    private static HttpContext CreateInertiaHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        return context;
    }

    private static ActionContext CreateActionContext(HttpContext? httpContext = null)
    {
        return new ActionContext(
            httpContext ?? new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
    }

    public class Construction
    {
        [Fact]
        public void Constructor_SetsUrl()
        {
            var result = new InertiaLocationResult("https://example.com");

            result.Url.Should().Be("https://example.com");
        }

        [Fact]
        public void Constructor_NullUrl_ThrowsArgumentException()
        {
            var act = () => new InertiaLocationResult(null!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_EmptyUrl_ThrowsArgumentException()
        {
            var act = () => new InertiaLocationResult(string.Empty);

            act.Should().Throw<ArgumentException>();
        }
    }

    public class ExecuteResultAsyncTests
    {
        [Fact]
        public async Task ExecuteResultAsync_InertiaRequest_Returns409WithLocationHeader()
        {
            var httpContext = CreateInertiaHttpContext();
            var actionContext = CreateActionContext(httpContext);
            var result = new InertiaLocationResult("https://example.com/login");

            await result.ExecuteResultAsync(actionContext);

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            httpContext.Response.Headers[InertiaHeaderNames.Location].ToString()
                .Should().Be("https://example.com/login");
        }

        [Fact]
        public async Task ExecuteResultAsync_NonInertiaRequest_Returns302Redirect()
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = CreateActionContext(httpContext);
            var result = new InertiaLocationResult("https://example.com/login");

            await result.ExecuteResultAsync(actionContext);

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
            httpContext.Response.Headers.Location.ToString()
                .Should().Be("https://example.com/login");
        }
    }

    public class ExecuteAsyncTests
    {
        [Fact]
        public async Task ExecuteAsync_InertiaRequest_Returns409WithLocationHeader()
        {
            var httpContext = CreateInertiaHttpContext();
            var result = new InertiaLocationResult("https://example.com/login");

            await result.ExecuteAsync(httpContext);

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            httpContext.Response.Headers[InertiaHeaderNames.Location].ToString()
                .Should().Be("https://example.com/login");
        }

        [Fact]
        public async Task ExecuteAsync_NonInertiaRequest_Returns302Redirect()
        {
            var httpContext = new DefaultHttpContext();
            var result = new InertiaLocationResult("https://example.com/login");

            await result.ExecuteAsync(httpContext);

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
            httpContext.Response.Headers.Location.ToString()
                .Should().Be("https://example.com/login");
        }
    }

    public class Interfaces
    {
        [Fact]
        public void ImplementsIActionResult()
        {
            var result = new InertiaLocationResult("/test");

            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public void ImplementsIResult()
        {
            var result = new InertiaLocationResult("/test");

            result.Should().BeAssignableTo<IResult>();
        }
    }
}
