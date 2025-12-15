using FluentAssertions;
using Inertia.AspNetCore;
using Inertia.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore.Tests;

public class ServiceRegistrationTests
{
    private class TestHandler : HandleInertiaRequests
    {
    }

    [Fact]
    public void AddInertia_RegistersIInertiaAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInertia();
        var provider = services.BuildServiceProvider();

        // Assert
        var inertia = provider.GetService<IInertia>();
        inertia.Should().NotBeNull();
        inertia.Should().BeOfType<InertiaResponseFactory>();
    }

    [Fact]
    public void AddInertia_WithConfiguration_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInertia(options =>
        {
            options.RootView = "custom";
            options.Ssr.Enabled = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<InertiaOptions>>();
        options.Value.RootView.Should().Be("custom");
        options.Value.Ssr.Enabled.Should().BeFalse();
    }

    [Fact]
    public void AddInertia_IsScopedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInertia();
        var provider = services.BuildServiceProvider();

        // Act
        var scope1 = provider.CreateScope();
        var scope2 = provider.CreateScope();
        var inertia1a = scope1.ServiceProvider.GetRequiredService<IInertia>();
        var inertia1b = scope1.ServiceProvider.GetRequiredService<IInertia>();
        var inertia2 = scope2.ServiceProvider.GetRequiredService<IInertia>();

        // Assert
        inertia1a.Should().BeSameAs(inertia1b, "same scope should return same instance");
        inertia1a.Should().NotBeSameAs(inertia2, "different scopes should have different instances");
    }

    [Fact]
    public void AddInertiaWithHandler_RegistersHandlerAndMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInertia<TestHandler>();
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<HandleInertiaRequests>();
        handler.Should().NotBeNull();
        handler.Should().BeOfType<TestHandler>();

        var middleware = provider.GetService<InertiaMiddleware>();
        middleware.Should().NotBeNull();
    }

    [Fact]
    public void AddInertiaWithHandler_WithConfiguration_ConfiguresOptionsAndRegistersHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInertia<TestHandler>(options =>
        {
            options.RootView = "main";
            options.Ssr.Url = "http://localhost:13714";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<InertiaOptions>>();
        options.Value.RootView.Should().Be("main");
        options.Value.Ssr.Url.Should().Be("http://localhost:13714");

        var handler = provider.GetRequiredService<HandleInertiaRequests>();
        handler.Should().BeOfType<TestHandler>();
    }

    [Fact]
    public void AddInertia_MultipleCallsDoNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInertia();
        services.AddInertia(); // Call twice
        var provider = services.BuildServiceProvider();

        // Assert
        var allInertiaServices = provider.GetServices<IInertia>();
        allInertiaServices.Should().HaveCount(1);
    }

    [Fact]
    public void AddInertiaWithHandler_HandlerIsScopedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInertia<TestHandler>();
        var provider = services.BuildServiceProvider();

        // Act
        var scope1 = provider.CreateScope();
        var scope2 = provider.CreateScope();
        var handler1a = scope1.ServiceProvider.GetRequiredService<HandleInertiaRequests>();
        var handler1b = scope1.ServiceProvider.GetRequiredService<HandleInertiaRequests>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<HandleInertiaRequests>();

        // Assert
        handler1a.Should().BeSameAs(handler1b, "same scope should return same instance");
        handler1a.Should().NotBeSameAs(handler2, "different scopes should have different instances");
    }

    [Fact]
    public void AddInertia_WithoutHandler_DoesNotRegisterMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInertia();
        var provider = services.BuildServiceProvider();

        // Assert
        var middleware = provider.GetService<InertiaMiddleware>();
        middleware.Should().BeNull();
    }

    [Fact]
    public void AddInertia_RegistersOptionsWithDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInertia();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<InertiaOptions>>();
        options.Value.RootView.Should().Be("app");
        options.Value.Ssr.Enabled.Should().BeTrue();
        options.Value.Ssr.Url.Should().Be("http://127.0.0.1:13714");
    }
}
