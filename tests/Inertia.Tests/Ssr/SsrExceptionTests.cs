using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Ssr;

public class SsrExceptionTests
{
    public class Create
    {
        [Fact]
        public void Create_SetsMessage()
        {
            var ex = SsrException.Create("Users/Index", "render error", SsrErrorType.Render);

            ex.Message.Should().Be("SSR render failed for component [Users/Index]: render error");
        }

        [Fact]
        public void Create_WithSourceLocation_AppendsToMessage()
        {
            var ex = SsrException.Create("Users/Index", "render error", SsrErrorType.Render,
                sourceLocation: "file.js:10:5");

            ex.Message.Should().Be("SSR render failed for component [Users/Index]: render error at file.js:10:5");
        }

        [Fact]
        public void Create_SetsProperties()
        {
            var ex = SsrException.Create("Users/Index", "render error", SsrErrorType.BrowserApi,
                hint: "Use window check", sourceLocation: "app.tsx:42:1");

            ex.Component.Should().Be("Users/Index");
            ex.ErrorType.Should().Be(SsrErrorType.BrowserApi);
            ex.Hint.Should().Be("Use window check");
            ex.SourceLocation.Should().Be("app.tsx:42:1");
        }

        [Fact]
        public void Create_WithInnerException_SetsInnerException()
        {
            var inner = new InvalidOperationException("inner");
            var ex = SsrException.Create("App", "connection failed", SsrErrorType.Connection,
                innerException: inner);

            ex.InnerException.Should().BeSameAs(inner);
        }

        [Fact]
        public void Create_WithBrowserApiAndStack_SetsProperties()
        {
            var ex = SsrException.Create("Users/Index", "window is not defined", SsrErrorType.BrowserApi,
                hint: "Use typeof window check", browserApi: "window",
                stack: "Error: window is not defined\n    at render (app.tsx:10:5)");

            ex.BrowserApi.Should().Be("window");
            ex.Stack.Should().Be("Error: window is not defined\n    at render (app.tsx:10:5)");
        }

        [Fact]
        public void Create_WithAllDetails()
        {
            var inner = new HttpRequestException("timeout");
            var ex = SsrException.Create("Dashboard", "timeout", SsrErrorType.Connection,
                hint: "Check SSR server", sourceLocation: "main.ts:1:1", innerException: inner);

            ex.Message.Should().Contain("Dashboard");
            ex.Message.Should().Contain("timeout");
            ex.Message.Should().Contain("main.ts:1:1");
            ex.Component.Should().Be("Dashboard");
            ex.ErrorType.Should().Be(SsrErrorType.Connection);
            ex.Hint.Should().Be("Check SSR server");
            ex.BrowserApi.Should().BeNull();
            ex.Stack.Should().BeNull();
            ex.SourceLocation.Should().Be("main.ts:1:1");
            ex.InnerException.Should().BeSameAs(inner);
        }
    }
}
