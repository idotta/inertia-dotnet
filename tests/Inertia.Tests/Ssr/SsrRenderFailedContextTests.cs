using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Ssr;

public class SsrRenderFailedContextTests
{
    private static InertiaPage CreatePage(string component = "Users/Index") => new()
    {
        Component = component,
        Props = new Dictionary<string, object?> { ["name"] = "Test" },
        Url = "/users",
        Version = "1.0",
    };

    public class Properties
    {
        [Fact]
        public void RequiredProperties_SetCorrectly()
        {
            var page = CreatePage();
            var context = new SsrRenderFailedContext
            {
                Page = page,
                Error = "render error",
                ErrorType = SsrErrorType.Render,
            };

            context.Page.Should().BeSameAs(page);
            context.Error.Should().Be("render error");
            context.ErrorType.Should().Be(SsrErrorType.Render);
        }

        [Fact]
        public void OptionalProperties_DefaultToNull()
        {
            var context = new SsrRenderFailedContext
            {
                Page = CreatePage(),
                Error = "error",
                ErrorType = SsrErrorType.Unknown,
            };

            context.Hint.Should().BeNull();
            context.BrowserApi.Should().BeNull();
            context.Stack.Should().BeNull();
            context.SourceLocation.Should().BeNull();
            context.Exception.Should().BeNull();
        }

        [Fact]
        public void AllProperties_SetCorrectly()
        {
            var page = CreatePage();
            var exception = new HttpRequestException("timeout");
            var context = new SsrRenderFailedContext
            {
                Page = page,
                Error = "window is not defined",
                ErrorType = SsrErrorType.BrowserApi,
                Hint = "Use typeof window check",
                BrowserApi = "window",
                Stack = "Error\n  at render",
                SourceLocation = "app.tsx:10:5",
                Exception = exception,
            };

            context.Hint.Should().Be("Use typeof window check");
            context.BrowserApi.Should().Be("window");
            context.Stack.Should().Be("Error\n  at render");
            context.SourceLocation.Should().Be("app.tsx:10:5");
            context.Exception.Should().BeSameAs(exception);
        }
    }
}
