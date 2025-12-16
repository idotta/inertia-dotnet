using FluentAssertions;
using Inertia.AspNetCore;
using Inertia.Core;
using Inertia.Core.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.AspNetCore.Tests;

/// <summary>
/// Integration tests for session-based once prop caching.
/// These tests verify that once props are cached in session and reused across requests.
/// </summary>
public class SessionIntegrationTests
{
    /// <summary>
    /// Helper method to create a session-enabled HTTP context.
    /// </summary>
    private DefaultHttpContext CreateHttpContextWithSession()
    {
        var httpContext = new DefaultHttpContext();

        // Create an in-memory session for testing
        var sessionFeature = new TestSessionFeature();
        httpContext.Features.Set<ISessionFeature>(sessionFeature);

        return httpContext;
    }

    [Fact]
    public async Task OnceProp_WithSession_CachesValueAcrossRequests()
    {
        // Arrange
        var callCount = 0;
        var services = new ServiceCollection();
        services.AddInertia();
        services.AddDistributedMemoryCache();
        services.AddSession();

        var httpContext = CreateHttpContextWithSession();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();

        // Act - First request
        var response1 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["translations"] = new OnceProp(() =>
            {
                callCount++;
                return new { hello = "Hello", goodbye = "Goodbye" };
            })
        });

        // Act - Second request (should use cached value)
        var response2 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["translations"] = new OnceProp(() =>
            {
                callCount++;
                return new { hello = "Hello", goodbye = "Goodbye" };
            })
        });

        // Assert
        callCount.Should().Be(1, "the once prop callback should only be called once");
        response1.Props.Should().ContainKey("translations");
        response2.Props.Should().ContainKey("translations");
    }

    [Fact]
    public async Task OnceProp_WithoutSession_ResolvesEachTime()
    {
        // Arrange
        var callCount = 0;
        var services = new ServiceCollection();
        services.AddInertia();

        var httpContext = new DefaultHttpContext();
        // No session configured
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();

        // Act - First request
        var response1 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["translations"] = new OnceProp(() =>
            {
                callCount++;
                return new { hello = "Hello" };
            })
        });

        // Act - Second request (should resolve again since no session)
        var response2 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["translations"] = new OnceProp(() =>
            {
                callCount++;
                return new { hello = "Hello" };
            })
        });

        // Assert
        callCount.Should().Be(2, "without session, the once prop should be resolved each time");
        response1.Props.Should().ContainKey("translations");
        response2.Props.Should().ContainKey("translations");
    }

    [Fact]
    public async Task OnceProp_WithResetHeader_ForcesReResolution()
    {
        // Arrange
        var callCount = 0;
        var services = new ServiceCollection();
        services.AddInertia();
        services.AddDistributedMemoryCache();
        services.AddSession();

        var httpContext = CreateHttpContextWithSession();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();

        // Act - First request
        var response1 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["translations"] = new OnceProp(() =>
            {
                callCount++;
                return new { count = callCount };
            })
        });

        // Act - Second request with X-Inertia-Reset header
        httpContext.Request.Headers["X-Inertia-Reset"] = "translations";
        var response2 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["translations"] = new OnceProp(() =>
            {
                callCount++;
                return new { count = callCount };
            })
        });

        // Assert
        callCount.Should().Be(2, "the reset header should force re-resolution");
        response1.Props.Should().ContainKey("translations");
        response2.Props.Should().ContainKey("translations");
    }

    [Fact]
    public async Task OnceProp_WithResetAllHeader_ForcesReResolutionOfAllProps()
    {
        // Arrange
        var translationsCallCount = 0;
        var permissionsCallCount = 0;
        var services = new ServiceCollection();
        services.AddInertia();
        services.AddDistributedMemoryCache();
        services.AddSession();

        var httpContext = CreateHttpContextWithSession();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();

        // Act - First request
        var response1 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["translations"] = new OnceProp(() =>
            {
                translationsCallCount++;
                return new { hello = "Hello" };
            }),
            ["permissions"] = new OnceProp(() =>
            {
                permissionsCallCount++;
                return new[] { "read", "write" };
            })
        });

        // Act - Second request with X-Inertia-Reset: all
        httpContext.Request.Headers["X-Inertia-Reset"] = "all";
        var response2 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["translations"] = new OnceProp(() =>
            {
                translationsCallCount++;
                return new { hello = "Hello" };
            }),
            ["permissions"] = new OnceProp(() =>
            {
                permissionsCallCount++;
                return new[] { "read", "write" };
            })
        });

        // Assert
        translationsCallCount.Should().Be(2, "reset all should force re-resolution of translations");
        permissionsCallCount.Should().Be(2, "reset all should force re-resolution of permissions");
    }

    [Fact]
    public async Task OnceProp_WithNestedProperty_CachesCorrectly()
    {
        // Arrange
        var callCount = 0;
        var services = new ServiceCollection();
        services.AddInertia();
        services.AddDistributedMemoryCache();
        services.AddSession();

        var httpContext = CreateHttpContextWithSession();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();

        // Act - First request
        var response1 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = "John",
                ["permissions"] = new OnceProp(() =>
                {
                    callCount++;
                    return new[] { "read", "write" };
                })
            }
        });

        // Act - Second request
        var response2 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = "John",
                ["permissions"] = new OnceProp(() =>
                {
                    callCount++;
                    return new[] { "read", "write" };
                })
            }
        });

        // Assert
        callCount.Should().Be(1, "nested once prop should be cached with proper key scoping");
    }

    [Fact]
    public async Task OnceProp_WithComplexValue_CachesCorrectly()
    {
        // Arrange
        var callCount = 0;
        var services = new ServiceCollection();
        services.AddInertia();
        services.AddDistributedMemoryCache();
        services.AddSession();

        var httpContext = CreateHttpContextWithSession();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();

        // Act - First request
        var response1 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["config"] = new OnceProp(() =>
            {
                callCount++;
                return new
                {
                    appName = "TestApp",
                    version = "1.0.0",
                    features = new[] { "feature1", "feature2" }
                };
            })
        });

        // Act - Second request
        var response2 = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["config"] = new OnceProp(() =>
            {
                callCount++;
                return new
                {
                    appName = "TestApp",
                    version = "1.0.0",
                    features = new[] { "feature1", "feature2" }
                };
            })
        });

        // Assert
        callCount.Should().Be(1, "complex objects should be cached correctly");
        response1.Props.Should().ContainKey("config");
        response2.Props.Should().ContainKey("config");
    }
}

/// <summary>
/// Test implementation of ISessionFeature for in-memory session testing.
/// </summary>
internal class TestSessionFeature : ISessionFeature
{
    public ISession Session { get; set; } = new TestSession();
}

/// <summary>
/// Test implementation of ISession for in-memory session testing.
/// </summary>
internal class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id => "test-session-id";
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _store.Remove(key);

    public void Set(string key, byte[] value) => _store[key] = value;

    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}
