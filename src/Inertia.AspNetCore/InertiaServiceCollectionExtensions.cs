using Inertia.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inertia.AspNetCore;

/// <summary>
/// Extension methods for setting up Inertia services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class InertiaServiceCollectionExtensions
{
    /// <summary>
    /// Adds Inertia services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddInertia(this IServiceCollection services)
    {
        return services.AddInertia(_ => { });
    }

    /// <summary>
    /// Adds Inertia services to the specified <see cref="IServiceCollection"/> with configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An <see cref="Action{InertiaOptions}"/> to configure the provided <see cref="InertiaOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddInertia(
        this IServiceCollection services,
        Action<InertiaOptions> configure)
    {
        // Configure options
        services.Configure(configure);

        // Register IInertia as scoped (per-request)
        // This ensures each request gets its own instance with isolated shared props
        services.TryAddScoped<IInertia, InertiaResponseFactory>();

        return services;
    }

    /// <summary>
    /// Adds Inertia services and registers a custom HandleInertiaRequests implementation.
    /// </summary>
    /// <typeparam name="THandler">The type of the HandleInertiaRequests implementation.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddInertia<THandler>(this IServiceCollection services)
        where THandler : HandleInertiaRequests
    {
        return services.AddInertia<THandler>(_ => { });
    }

    /// <summary>
    /// Adds Inertia services with configuration and registers a custom HandleInertiaRequests implementation.
    /// </summary>
    /// <typeparam name="THandler">The type of the HandleInertiaRequests implementation.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An <see cref="Action{InertiaOptions}"/> to configure the provided <see cref="InertiaOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddInertia<THandler>(
        this IServiceCollection services,
        Action<InertiaOptions> configure)
        where THandler : HandleInertiaRequests
    {
        // Add base Inertia services
        services.AddInertia(configure);

        // Register the custom handler as scoped
        services.TryAddScoped<HandleInertiaRequests, THandler>();

        // Register the middleware itself
        services.TryAddScoped<InertiaMiddleware>();

        // Register optional middleware
        services.TryAddScoped<EncryptHistoryMiddleware>();

        return services;
    }

    /// <summary>
    /// Adds Inertia validation filter to MVC options.
    /// This enables automatic validation error handling for Inertia requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddInertiaValidation(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<InertiaValidationFilter>();
        });

        return services;
    }
}
