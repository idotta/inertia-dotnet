namespace Inertia.AspNetCore;

/// <summary>
/// Internal interface for props that accept an <see cref="IServiceProvider"/> for callback resolution.
/// Used to support dependency injection in prop callbacks, analogous to PHP's <c>App::call($callback)</c>.
/// </summary>
internal interface IServiceResolvableProp
{
    /// <summary>Gets whether this prop has a service-provider-dependent callback configured.</summary>
    bool HasServiceCallback { get; }

    /// <summary>Resolves the prop value using the provided service provider.</summary>
    /// <param name="serviceProvider">The service provider from <c>HttpContext.RequestServices</c>.</param>
    Task<object?> ResolveWithServiceAsync(IServiceProvider serviceProvider);
}
