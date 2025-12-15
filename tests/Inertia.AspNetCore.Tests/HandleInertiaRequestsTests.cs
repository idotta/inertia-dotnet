using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore.Tests;

public class HandleInertiaRequestsTests
{
    private class TestHandler : HandleInertiaRequests
    {
        public string? CustomVersion { get; set; }
        public IDictionary<string, object?>? CustomShared { get; set; }
        public IDictionary<string, object?>? CustomOnce { get; set; }
        public string? CustomRootView { get; set; }
        public Func<string>? CustomUrlResolver { get; set; }
        public object? CustomValidationErrors { get; set; }
        public bool EmptyResponseCalled { get; private set; }
        public bool VersionChangeCalled { get; private set; }

        public override string? Version(HttpRequest request)
        {
            return CustomVersion ?? base.Version(request);
        }

        public override IDictionary<string, object?> Share(HttpRequest request)
        {
            return CustomShared ?? base.Share(request);
        }

        public override IDictionary<string, object?> ShareOnce(HttpRequest request)
        {
            return CustomOnce ?? base.ShareOnce(request);
        }

        public override string RootView(HttpRequest request)
        {
            return CustomRootView ?? base.RootView(request);
        }

        public override Func<string>? UrlResolver()
        {
            return CustomUrlResolver ?? base.UrlResolver();
        }

        public override object ResolveValidationErrors(HttpContext context)
        {
            return CustomValidationErrors ?? base.ResolveValidationErrors(context);
        }

        public override Task OnEmptyResponse(HttpContext context)
        {
            EmptyResponseCalled = true;
            return base.OnEmptyResponse(context);
        }

        public override Task OnVersionChange(HttpContext context)
        {
            VersionChangeCalled = true;
            return base.OnVersionChange(context);
        }
    }

    private readonly TestHandler _handler;
    private readonly DefaultHttpContext _context;

    public HandleInertiaRequestsTests()
    {
        _handler = new TestHandler();
        _context = new DefaultHttpContext();
    }

    [Fact]
    public void Version_ByDefault_ReturnsNull()
    {
        // Act
        var result = _handler.Version(_context.Request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Version_WhenOverridden_ReturnsCustomValue()
    {
        // Arrange
        _handler.CustomVersion = "v1.0.0";

        // Act
        var result = _handler.Version(_context.Request);

        // Assert
        result.Should().Be("v1.0.0");
    }

    [Fact]
    public void Share_ByDefault_IncludesErrorsProperty()
    {
        // Act
        var result = _handler.Share(_context.Request);

        // Assert
        result.Should().ContainKey("errors");
    }

    [Fact]
    public void Share_WhenOverridden_ReturnsCustomProps()
    {
        // Arrange
        _handler.CustomShared = new Dictionary<string, object?>
        {
            ["user"] = new { Name = "John" },
            ["flash"] = "Success!"
        };

        // Act
        var result = _handler.Share(_context.Request);

        // Assert
        result.Should().ContainKey("user");
        result.Should().ContainKey("flash");
    }

    [Fact]
    public void ShareOnce_ByDefault_ReturnsEmptyDictionary()
    {
        // Act
        var result = _handler.ShareOnce(_context.Request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ShareOnce_WhenOverridden_ReturnsCustomProps()
    {
        // Arrange
        _handler.CustomOnce = new Dictionary<string, object?>
        {
            ["config"] = new { Api = "https://api.example.com" }
        };

        // Act
        var result = _handler.ShareOnce(_context.Request);

        // Assert
        result.Should().ContainKey("config");
    }

    [Fact]
    public void RootView_ByDefault_ReturnsApp()
    {
        // Act
        var result = _handler.RootView(_context.Request);

        // Assert
        result.Should().Be("app");
    }

    [Fact]
    public void RootView_WhenOverridden_ReturnsCustomView()
    {
        // Arrange
        _handler.CustomRootView = "main";

        // Act
        var result = _handler.RootView(_context.Request);

        // Assert
        result.Should().Be("main");
    }

    [Fact]
    public void UrlResolver_ByDefault_ReturnsNull()
    {
        // Act
        var result = _handler.UrlResolver();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UrlResolver_WhenOverridden_ReturnsCustomResolver()
    {
        // Arrange
        _handler.CustomUrlResolver = () => "/custom/path";

        // Act
        var result = _handler.UrlResolver();

        // Assert
        result.Should().NotBeNull();
        result!.Invoke().Should().Be("/custom/path");
    }

    [Fact]
    public void ResolveValidationErrors_ByDefault_ReturnsEmptyObject()
    {
        // Act
        var result = _handler.ResolveValidationErrors(_context);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ResolveValidationErrors_WithValidationErrorsInItems_ReturnsErrors()
    {
        // Arrange
        var errors = new { email = "Email is required", password = "Password is required" };
        _context.Items["InertiaValidationErrors"] = errors;

        // Act
        var result = _handler.ResolveValidationErrors(_context);

        // Assert
        result.Should().Be(errors);
    }

    [Fact]
    public void ResolveValidationErrors_WhenOverridden_ReturnsCustomErrors()
    {
        // Arrange
        _handler.CustomValidationErrors = new { custom = "error" };

        // Act
        var result = _handler.ResolveValidationErrors(_context);

        // Assert
        result.Should().Be(_handler.CustomValidationErrors);
    }

    [Fact]
    public async Task OnEmptyResponse_ByDefault_SetsRedirectToReferer()
    {
        // Arrange
        _context.Request.Headers.Referer = "https://example.com/previous";

        // Act
        await _handler.OnEmptyResponse(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(302);
        _context.Response.Headers.Location.ToString().Should().Be("https://example.com/previous");
    }

    [Fact]
    public async Task OnEmptyResponse_WithoutReferer_RedirectsToRoot()
    {
        // Act
        await _handler.OnEmptyResponse(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(302);
        _context.Response.Headers.Location.ToString().Should().Be("/");
    }

    [Fact]
    public async Task OnEmptyResponse_WhenOverridden_CallsBaseAndCustomLogic()
    {
        // Act
        await _handler.OnEmptyResponse(_context);

        // Assert
        _handler.EmptyResponseCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnVersionChange_ByDefault_Returns409WithLocation()
    {
        // Arrange
        _context.Request.Scheme = "https";
        _context.Request.Host = new HostString("example.com");
        _context.Request.Path = "/users";

        // Act
        await _handler.OnVersionChange(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(409);
        _context.Response.Headers[Core.InertiaHeaders.Location].ToString().Should().Contain("/users");
    }

    [Fact]
    public async Task OnVersionChange_WhenOverridden_CallsBaseAndCustomLogic()
    {
        // Act
        await _handler.OnVersionChange(_context);

        // Assert
        _handler.VersionChangeCalled.Should().BeTrue();
    }
}
