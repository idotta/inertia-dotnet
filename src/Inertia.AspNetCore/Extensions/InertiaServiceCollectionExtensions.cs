using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inertia.AspNetCore;

/// <summary>
/// Extension methods for registering Inertia.js services with the DI container.
/// </summary>
public static class InertiaServiceCollectionExtensions
{
    /// <summary>
    /// Adds Inertia.js services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">An optional action to configure <see cref="InertiaOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInertia(
        this IServiceCollection services,
        Action<InertiaOptions>? configure = null)
    {
        // Options with validation
        var optionsBuilder = services
            .AddOptions<InertiaOptions>()
            .BindConfiguration(InertiaOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
            optionsBuilder.Configure(configure);

        // Core services
        services.AddHttpContextAccessor();
        services.TryAddScoped<IInertia, InertiaFactory>();
        services.TryAddTransient<InertiaMiddleware>();
        services.TryAddTransient<EncryptHistoryMiddleware>();

        // SSR services
        services.TryAddSingleton<SsrBundleDetector>();
        services.TryAddSingleton<ISsrGateway, HttpSsrGateway>();
        services.TryAddScoped<SsrState>();

        // Named HttpClient for SSR
        services.AddHttpClient(HttpSsrGateway.HttpClientName);

        // View rendering
        services.TryAddScoped<InertiaViewRenderer>();

        return services;
    }
}
