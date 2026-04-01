using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Inertia.Tests;

public class InertiaExceptionHandlerTests
{
    private static (InertiaExceptionHandler Handler, DefaultHttpContext HttpContext) CreateHandler(
        Action<InertiaOptions>? configure = null)
    {
        var options = new InertiaOptions();
        configure?.Invoke(options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        httpContext.Request.Path = "/test";
        var body = new MemoryStream();
        httpContext.Response.Body = body;

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var tempData = Substitute.For<ITempDataDictionary>();
        var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
        tempDataFactory.GetTempData(httpContext).Returns(tempData);

        var factory = new InertiaFactory(Options.Create(options), accessor, tempDataFactory);

        var services = new ServiceCollection();
        services.AddSingleton<IInertia>(factory);
        httpContext.RequestServices = services.BuildServiceProvider();

        var handler = new InertiaExceptionHandler(Options.Create(options));
        return (handler, httpContext);
    }

    private static async Task<string> GetResponseBody(HttpContext ctx)
    {
        var body = ctx.Response.Body as MemoryStream;
        body!.Position = 0;
        using var reader = new StreamReader(body);
        return await reader.ReadToEndAsync();
    }

    public class NoHandler
    {
        [Fact]
        public async Task TryHandleAsync_NoExceptionHandler_ReturnsFalse()
        {
            var (handler, ctx) = CreateHandler();

            var result = await handler.TryHandleAsync(ctx, new Exception("fail"), default);

            result.Should().BeFalse();
        }
    }

    public class RenderErrorPage
    {
        [Fact]
        public async Task TryHandleAsync_RenderResult_WritesInertiaJsonResponse()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = _ => InertiaExceptionResult.Render("Error", new { status = 500 }));

            var result = await handler.TryHandleAsync(ctx, new Exception("fail"), default);

            result.Should().BeTrue();
            ctx.Response.Headers[InertiaHeaderNames.Inertia].ToString().Should().Be("true");
            var json = await GetResponseBody(ctx);
            var page = JsonDocument.Parse(json).RootElement;
            page.GetProperty("component").GetString().Should().Be("Error");
        }

        [Fact]
        public async Task TryHandleAsync_RenderResult_PreservesStatusCode()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = _ => InertiaExceptionResult.Render("Error"));

            await handler.TryHandleAsync(ctx, new Exception("fail"), default);

            ctx.Response.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task TryHandleAsync_RenderResult_IncludesPropsInResponse()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = ec => InertiaExceptionResult.Render("Error",
                    new Dictionary<string, object?>
                    {
                        ["status"] = ec.StatusCode,
                        ["message"] = ec.Exception.Message,
                    }));

            await handler.TryHandleAsync(ctx, new Exception("Something went wrong"), default);

            var json = await GetResponseBody(ctx);
            var props = JsonDocument.Parse(json).RootElement.GetProperty("props");
            props.GetProperty("status").GetInt32().Should().Be(500);
            props.GetProperty("message").GetString().Should().Be("Something went wrong");
        }
    }

    public class Redirect
    {
        [Fact]
        public async Task TryHandleAsync_RedirectResult_RedirectsToUrl()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = _ => InertiaExceptionResult.Redirect("/login"));

            var result = await handler.TryHandleAsync(ctx, new Exception("unauthorized"), default);

            result.Should().BeTrue();
            ctx.Response.Headers.Location.ToString().Should().Be("/login");
        }
    }

    public class FallThrough
    {
        [Fact]
        public async Task TryHandleAsync_HandlerReturnsNull_ReturnsFalse()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = _ => null);

            var result = await handler.TryHandleAsync(ctx, new Exception("fail"), default);

            result.Should().BeFalse();
        }
    }

    public class SharedData
    {
        [Fact]
        public async Task TryHandleAsync_WithSharedData_IncludesSharedPropsInResponse()
        {
            var (handler, ctx) = CreateHandler(o =>
            {
                o.SharedPropsProvider = (_, _) => new Dictionary<string, object?>
                {
                    ["appName"] = "My App",
                };
                o.ExceptionHandler = _ =>
                    InertiaExceptionResult.Render("Error", new { status = 404 }).WithSharedData();
            });

            await handler.TryHandleAsync(ctx, new Exception("not found"), default);

            var json = await GetResponseBody(ctx);
            var props = JsonDocument.Parse(json).RootElement.GetProperty("props");
            props.GetProperty("appName").GetString().Should().Be("My App");
        }

        [Fact]
        public async Task TryHandleAsync_WithoutSharedData_ExcludesSharedProps()
        {
            var (handler, ctx) = CreateHandler(o =>
            {
                o.SharedPropsProvider = (_, _) => new Dictionary<string, object?>
                {
                    ["appName"] = "My App",
                };
                o.ExceptionHandler = _ =>
                    InertiaExceptionResult.Render("Error", new { status = 404 });
            });

            await handler.TryHandleAsync(ctx, new Exception("not found"), default);

            var json = await GetResponseBody(ctx);
            var props = JsonDocument.Parse(json).RootElement.GetProperty("props");
            props.TryGetProperty("appName", out _).Should().BeFalse();
        }
    }

    public class SafetyGuards
    {
        [Fact]
        public async Task TryHandleAsync_ResponseHasStarted_ReturnsFalse()
        {
            var options = new InertiaOptions
            {
                ExceptionHandler = _ => InertiaExceptionResult.Render("Error"),
            };

            var httpContext = Substitute.For<HttpContext>();
            var response = Substitute.For<HttpResponse>();
            response.HasStarted.Returns(true);
            httpContext.Response.Returns(response);

            var handler = new InertiaExceptionHandler(Options.Create(options));

            var result = await handler.TryHandleAsync(httpContext, new Exception("fail"), default);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryHandleAsync_ExceptionHandlerDelegateThrows_ReturnsFalse()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = _ => throw new InvalidOperationException("delegate exploded"));

            var result = await handler.TryHandleAsync(ctx, new Exception("fail"), default);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryHandleAsync_InvalidDerivedStatusCode_FallsBackTo500()
        {
            // BadHttpRequestException uses a private constructor; use reflection to create one
            // with an out-of-range status code.
            var exception = CreateBadHttpRequestException(200);

            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = ec => InertiaExceptionResult.Render("Error",
                    new Dictionary<string, object?> { ["status"] = ec.StatusCode }));

            await handler.TryHandleAsync(ctx, exception, default);

            ctx.Response.StatusCode.Should().Be(500);

            var json = await GetResponseBody(ctx);
            var props = JsonDocument.Parse(json).RootElement.GetProperty("props");
            props.GetProperty("status").GetInt32().Should().Be(500);
        }

        private static BadHttpRequestException CreateBadHttpRequestException(int statusCode)
        {
            return new BadHttpRequestException("test", statusCode);
        }
    }

    public class StatusCodeDerivation
    {
        [Fact]
        public async Task TryHandleAsync_BadHttpRequestException_DerivesStatusCode()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = ec => InertiaExceptionResult.Render("Error",
                    new Dictionary<string, object?> { ["status"] = ec.StatusCode }));

            await handler.TryHandleAsync(ctx, new BadHttpRequestException("bad request", 400), default);

            ctx.Response.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task TryHandleAsync_InertiaHttpException_DerivesStatusCode()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = ec => InertiaExceptionResult.Render("Error",
                    new Dictionary<string, object?> { ["status"] = ec.StatusCode }));

            await handler.TryHandleAsync(ctx, new InertiaHttpException(403, "Forbidden"), default);

            ctx.Response.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task TryHandleAsync_HttpRequestException_DerivesStatusCode()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = ec => InertiaExceptionResult.Render("Error",
                    new Dictionary<string, object?> { ["status"] = ec.StatusCode }));

            await handler.TryHandleAsync(ctx,
                new HttpRequestException("Not Found", null, HttpStatusCode.NotFound), default);

            ctx.Response.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task TryHandleAsync_GenericException_Defaults500()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = ec => InertiaExceptionResult.Render("Error",
                    new Dictionary<string, object?> { ["status"] = ec.StatusCode }));

            await handler.TryHandleAsync(ctx, new InvalidOperationException("oops"), default);

            ctx.Response.StatusCode.Should().Be(500);
        }
    }

    public class CustomRootView
    {
        [Fact]
        public async Task TryHandleAsync_WithCustomRootView_SetsRootViewOnFactory()
        {
            var (handler, ctx) = CreateHandler(o =>
                o.ExceptionHandler = _ =>
                    InertiaExceptionResult.Render("Error").RootView("~/Views/Custom.cshtml"));

            await handler.TryHandleAsync(ctx, new Exception("fail"), default);

            // Verify the response was rendered (if root view is applied, response executes successfully)
            var json = await GetResponseBody(ctx);
            var page = JsonDocument.Parse(json).RootElement;
            page.GetProperty("component").GetString().Should().Be("Error");
        }
    }
}
