using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inertia.Tests.Extensions;

public class InertiaServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(Action<InertiaOptions>? configure = null)
    {
        var services = new ServiceCollection();
        // Add configuration (required by BindConfiguration in AddInertia)
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        // Add MVC services needed by InertiaViewRenderer and other internal types
        services.AddLogging();
        services.AddMvc();
        services.AddInertia(configure);
        return services.BuildServiceProvider();
    }

    // ---- Group 1: Registration ----
    public class Registration
    {
        [Fact]
        public void AddInertia_RegistersIInertia_AsScoped()
        {
            using var provider = BuildProvider();
            using var scope = provider.CreateScope();

            var inertia = scope.ServiceProvider.GetService<IInertia>();

            inertia.Should().NotBeNull();
            inertia.Should().BeOfType<InertiaFactory>();
        }

        [Fact]
        public void AddInertia_RegistersSsrState_AsScoped()
        {
            using var provider = BuildProvider();
            using var scope = provider.CreateScope();

            var state = scope.ServiceProvider.GetService<SsrState>();

            state.Should().NotBeNull();
        }

        [Fact]
        public void AddInertia_RegistersISsrGateway_AsSingleton()
        {
            using var provider = BuildProvider();

            var gateway1 = provider.GetService<ISsrGateway>();
            var gateway2 = provider.GetService<ISsrGateway>();

            gateway1.Should().NotBeNull();
            gateway1.Should().BeOfType<HttpSsrGateway>();
            gateway1.Should().BeSameAs(gateway2);
        }

        [Fact]
        public void AddInertia_RegistersSsrBundleDetector_AsSingleton()
        {
            using var provider = BuildProvider();

            var detector1 = provider.GetService<SsrBundleDetector>();
            var detector2 = provider.GetService<SsrBundleDetector>();

            detector1.Should().NotBeNull();
            detector1.Should().BeSameAs(detector2);
        }

        [Fact]
        public void AddInertia_RegistersInertiaMiddleware()
        {
            using var provider = BuildProvider();
            using var scope = provider.CreateScope();

            var middleware = scope.ServiceProvider.GetService<InertiaMiddleware>();

            middleware.Should().NotBeNull();
        }

        [Fact]
        public void AddInertia_RegistersEncryptHistoryMiddleware()
        {
            using var provider = BuildProvider();
            using var scope = provider.CreateScope();

            var middleware = scope.ServiceProvider.GetService<EncryptHistoryMiddleware>();

            middleware.Should().NotBeNull();
        }

        [Fact]
        public void AddInertia_RegistersHttpContextAccessor()
        {
            using var provider = BuildProvider();

            var accessor = provider.GetService<IHttpContextAccessor>();

            accessor.Should().NotBeNull();
        }

        [Fact]
        public void AddInertia_RegistersInertiaViewRenderer()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddInertia();

            // Verify registration exists (full resolution requires a complete host)
            services.Should().Contain(sd =>
                sd.ServiceType == typeof(InertiaViewRenderer) &&
                sd.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInertia_RegistersNamedHttpClient()
        {
            using var provider = BuildProvider();

            var factory = provider.GetService<IHttpClientFactory>();

            factory.Should().NotBeNull();
            var client = factory!.CreateClient(HttpSsrGateway.HttpClientName);
            client.Should().NotBeNull();
        }
    }

    // ---- Group 2: Configuration ----
    public class Configuration
    {
        [Fact]
        public void AddInertia_ConfigureAction_AppliesOptions()
        {
            using var provider = BuildProvider(o =>
            {
                o.RootView = "~/Views/Custom.cshtml";
                o.SsrEnabled = false;
            });

            var options = provider.GetRequiredService<IOptions<InertiaOptions>>().Value;

            options.RootView.Should().Be("~/Views/Custom.cshtml");
            options.SsrEnabled.Should().BeFalse();
        }

        [Fact]
        public void AddInertia_NoConfiguration_UsesDefaults()
        {
            using var provider = BuildProvider();

            var options = provider.GetRequiredService<IOptions<InertiaOptions>>().Value;

            options.RootView.Should().Be("~/Views/App.cshtml");
            options.SsrEnabled.Should().BeTrue();
            options.SsrUrl.Should().Be("http://127.0.0.1:13714");
        }

        [Fact]
        public void AddInertia_ReturnsSameServiceCollection()
        {
            var services = new ServiceCollection();

            var result = services.AddInertia();

            result.Should().BeSameAs(services);
        }
    }
}
