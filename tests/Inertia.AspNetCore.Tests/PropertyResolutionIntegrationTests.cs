using FluentAssertions;
using Inertia.AspNetCore;
using Inertia.Core;
using Inertia.Core.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore.Tests;

/// <summary>
/// Integration tests for Phase 3.3 Property Resolution Integration.
/// These tests verify that the AspNetCoreInertiaResponseFactory correctly resolves
/// properties with HTTP context awareness, matching the Laravel adapter's behavior.
/// </summary>
public class PropertyResolutionIntegrationTests
{
    [Fact]
    public async Task PropertyResolution_WithPartialReload_FiltersPropsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInertia();
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Inertia"] = "true";
        httpContext.Request.Headers["X-Inertia-Partial-Data"] = "users";
        httpContext.Request.Headers["X-Inertia-Partial-Component"] = "Users/Index";
        
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        
        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();
        
        // Act
        var response = await inertia.RenderAsync("Users/Index", new Dictionary<string, object?>
        {
            ["users"] = new[] { "User1", "User2" },
            ["filters"] = new { search = "test" },
            ["stats"] = 100
        });
        
        // Assert
        response.Props.Should().ContainKey("users");
        response.Props.Should().NotContainKey("filters");
        response.Props.Should().NotContainKey("stats");
    }
    
    [Fact]
    public async Task PropertyResolution_WithOptionalProp_ExcludesOnInitialLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInertia();
        
        var httpContext = new DefaultHttpContext();
        // Not an Inertia request (initial load)
        
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        
        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();
        
        // Act
        var response = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["user"] = new { name = "John" },
            ["stats"] = new OptionalProp(() => new { count = 100 })
        });
        
        // Assert
        response.Props.Should().ContainKey("user");
        response.Props.Should().NotContainKey("stats"); // Excluded on initial load
    }
    
    [Fact]
    public async Task PropertyResolution_WithOptionalProp_IncludesOnPartialReload()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInertia();
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Inertia"] = "true";
        httpContext.Request.Headers["X-Inertia-Partial-Data"] = "stats";
        httpContext.Request.Headers["X-Inertia-Partial-Component"] = "Dashboard";
        
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        
        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();
        
        // Act
        var response = await inertia.RenderAsync("Dashboard", new Dictionary<string, object?>
        {
            ["user"] = new { name = "John" },
            ["stats"] = new OptionalProp(() => new { count = 100 })
        });
        
        // Assert
        response.Props.Should().ContainKey("stats"); // Included when explicitly requested
        response.Props["stats"].Should().BeEquivalentTo(new { count = 100 });
    }
    
    [Fact]
    public async Task PropertyResolution_WithCallback_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInertia();
        
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        
        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();
        
        var callbackCalled = false;
        
        // Act
        var response = await inertia.RenderAsync("Test", new Dictionary<string, object?>
        {
            ["data"] = new Func<object>(() =>
            {
                callbackCalled = true;
                return "resolved value";
            })
        });
        
        // Assert
        callbackCalled.Should().BeTrue();
        response.Props["data"].Should().Be("resolved value");
    }
    
    [Fact]
    public async Task PropertyResolution_WithAsyncCallback_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInertia();
        
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        
        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();
        
        // Act
        var response = await inertia.RenderAsync("Test", new Dictionary<string, object?>
        {
            ["data"] = new Func<Task<object>>(async () =>
            {
                await Task.Delay(10);
                return "async resolved value";
            })
        });
        
        // Assert
        response.Props["data"].Should().Be("async resolved value");
    }
    
    [Fact]
    public async Task PropertyResolution_WithPropertyProvider_ResolvesWithContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInertia();
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/test-path";
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        
        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();
        
        // Act
        var response = await inertia.RenderAsync("Test", new Dictionary<string, object?>
        {
            ["contextData"] = new TestPropertyProvider()
        });
        
        // Assert
        response.Props["contextData"].Should().Be("provided value");
    }
    
    [Fact]
    public async Task PropertyResolution_WithPropertiesProvider_ExpandsMultipleProps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInertia();
        
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        
        var provider = services.BuildServiceProvider();
        var inertia = provider.GetRequiredService<IInertia>();
        
        // Act
        var response = await inertia.RenderAsync("Test", new Dictionary<string, object?>
        {
            ["provider"] = new TestPropertiesProvider()
        });
        
        // Assert - The provider should have been expanded into multiple props
        response.Props.Should().ContainKey("name");
        response.Props.Should().ContainKey("email");
        response.Props["name"].Should().Be("John Doe");
        response.Props["email"].Should().Be("john@example.com");
        response.Props.Should().NotContainKey("provider"); // Original key should be replaced
    }
    
    // Test helper classes
    private class TestPropertyProvider : IProvidesInertiaProperty
    {
        public object? ToInertiaProperty(object context)
        {
            return "provided value";
        }
    }
    
    private class TestPropertiesProvider : IProvidesInertiaProperties
    {
        public Dictionary<string, object?> ToInertiaProperties(object context)
        {
            return new Dictionary<string, object?>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com"
            };
        }
    }
}
