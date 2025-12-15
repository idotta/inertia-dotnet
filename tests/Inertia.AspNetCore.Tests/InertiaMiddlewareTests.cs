using FluentAssertions;
using Inertia.AspNetCore;
using Inertia.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore.Tests;

public class InertiaMiddlewareTests
{
    private class TestHandler : HandleInertiaRequests
    {
        public string? TestVersion { get; set; }
        public Dictionary<string, object?>? TestSharedProps { get; set; }
        public bool OnVersionChangeCalled { get; set; }
        public bool OnEmptyResponseCalled { get; set; }

        public override string? Version(HttpRequest request)
        {
            return TestVersion;
        }

        public override IDictionary<string, object?> Share(HttpRequest request)
        {
            return TestSharedProps ?? base.Share(request);
        }

        public override Task OnVersionChange(HttpContext context)
        {
            OnVersionChangeCalled = true;
            return base.OnVersionChange(context);
        }

        public override Task OnEmptyResponse(HttpContext context)
        {
            OnEmptyResponseCalled = true;
            return base.OnEmptyResponse(context);
        }
    }

    private readonly DefaultHttpContext _context;
    private readonly TestHandler _handler;
    private readonly InertiaMiddleware _middleware;
    private bool _nextCalled;

    public InertiaMiddlewareTests()
    {
        _context = new DefaultHttpContext();
        _handler = new TestHandler();
        _middleware = new InertiaMiddleware(_handler);
        _nextCalled = false;

        // Setup DI
        var services = new ServiceCollection();
        services.AddInertia();
        var serviceProvider = services.BuildServiceProvider();
        _context.RequestServices = serviceProvider.CreateScope().ServiceProvider;
    }

    private Task Next(HttpContext context)
    {
        _nextCalled = true;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task InvokeAsync_Always_CallsNextMiddleware()
    {
        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_Always_AddsVaryHeader()
    {
        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _context.Response.Headers.Should().ContainKey("Vary");
        _context.Response.Headers["Vary"].ToString().Should().Contain(InertiaHeaders.Inertia);
    }

    [Fact]
    public async Task InvokeAsync_WithVersion_SetsVersionOnInertia()
    {
        // Arrange
        _handler.TestVersion = "v1.2.3";
        var inertia = _context.RequestServices.GetRequiredService<IInertia>();

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        inertia.GetVersion().Should().Be("v1.2.3");
    }

    [Fact]
    public async Task InvokeAsync_WithSharedProps_SharesPropsOnInertia()
    {
        // Arrange
        _handler.TestSharedProps = new Dictionary<string, object?>
        {
            ["user"] = new { Name = "John" },
            ["flash"] = "Success"
        };
        var inertia = _context.RequestServices.GetRequiredService<IInertia>();

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        var shared = inertia.GetShared() as Dictionary<string, object?>;
        shared.Should().ContainKey("user");
        shared.Should().ContainKey("flash");
    }

    [Fact]
    public async Task InvokeAsync_NonInertiaRequest_DoesNotProcessHeaders()
    {
        // Arrange - no X-Inertia header
        _handler.TestVersion = "v1.0.0";
        _context.Response.StatusCode = 200;

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _nextCalled.Should().BeTrue();
        _handler.OnVersionChangeCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_WithVersionMismatch_CallsOnVersionChange()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Request.Headers[InertiaHeaders.Version] = "old-version";
        _context.Request.Method = HttpMethods.Get;
        _handler.TestVersion = "new-version";

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _handler.OnVersionChangeCalled.Should().BeTrue();
        _context.Response.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_WithSameVersion_DoesNotCallOnVersionChange()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Request.Headers[InertiaHeaders.Version] = "v1.0.0";
        _context.Request.Method = HttpMethods.Get;
        _handler.TestVersion = "v1.0.0";

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _handler.OnVersionChangeCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_PostMethod_DoesNotCheckVersion()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Request.Headers[InertiaHeaders.Version] = "old-version";
        _context.Request.Method = HttpMethods.Post;
        _handler.TestVersion = "new-version";

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _handler.OnVersionChangeCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_EmptyResponse_CallsOnEmptyResponse()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Response.StatusCode = 200;
        _context.Response.ContentLength = 0;

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _handler.OnEmptyResponseCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_ResponseWithContent_DoesNotCallOnEmptyResponse()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Response.StatusCode = 200;
        _context.Response.ContentLength = 100;

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _handler.OnEmptyResponseCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_302Redirect_PutMethod_ChangesTo303()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Request.Method = HttpMethods.Put;
        _context.Response.StatusCode = 302;

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _context.Response.StatusCode.Should().Be(303);
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_302Redirect_PatchMethod_ChangesTo303()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Request.Method = HttpMethods.Patch;
        _context.Response.StatusCode = 302;

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _context.Response.StatusCode.Should().Be(303);
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_302Redirect_DeleteMethod_ChangesTo303()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Request.Method = HttpMethods.Delete;
        _context.Response.StatusCode = 302;

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _context.Response.StatusCode.Should().Be(303);
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_302Redirect_GetMethod_DoesNotChange()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Request.Method = HttpMethods.Get;
        _context.Response.StatusCode = 302;

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _context.Response.StatusCode.Should().Be(302);
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_302Redirect_PostMethod_DoesNotChange()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Request.Method = HttpMethods.Post;
        _context.Response.StatusCode = 302;

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _context.Response.StatusCode.Should().Be(302);
    }

    [Fact]
    public async Task InvokeAsync_InertiaRequest_200Response_DoesNotChangeStatusCode()
    {
        // Arrange
        _context.Request.Headers[InertiaHeaders.Inertia] = "true";
        _context.Request.Method = HttpMethods.Put;
        _context.Response.StatusCode = 200;
        _context.Response.ContentLength = 100; // Has content

        // Act
        await _middleware.InvokeAsync(_context, Next);

        // Assert
        _context.Response.StatusCode.Should().Be(200);
    }
}
