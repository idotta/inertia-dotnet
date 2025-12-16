using Inertia.Core;
using Inertia.Core.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Collections;

namespace Inertia.AspNetCore;

/// <summary>
/// ASP.NET Core-aware implementation of IInertia that adds property resolution capabilities.
/// This factory extends the core InertiaResponseFactory with HTTP context awareness for:
/// - Partial reload filtering based on request headers
/// - Property callback resolution  
/// - Property provider resolution
/// - Once props session caching
/// - Merge/defer/scroll metadata handling
/// </summary>
public class AspNetCoreInertiaResponseFactory : IInertia
{
    private readonly InertiaResponseFactory _coreFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetCoreInertiaResponseFactory"/> class.
    /// </summary>
    /// <param name="options">The Inertia configuration options.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public AspNetCoreInertiaResponseFactory(
        IOptions<InertiaOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _coreFactory = new InertiaResponseFactory(options);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public async Task<InertiaResponse> RenderAsync(string component, IDictionary<string, object?>? props = null)
    {
        // First, get the base response from the core factory
        var response = await _coreFactory.RenderAsync(component, props);

        // Then, resolve properties with HTTP context awareness
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var resolvedProps = await ResolvePropertiesAsync(response.Props, httpContext.Request, component);

            // Update the response props with resolved values
            response.Props.Clear();
            foreach (var kvp in resolvedProps)
            {
                response.Props[kvp.Key] = kvp.Value;
            }
        }

        return response;
    }

    /// <inheritdoc/>
    public object Location(string url) => _coreFactory.Location(url);

    /// <inheritdoc/>
    public void Share(string key, object? value) => _coreFactory.Share(key, value);

    /// <inheritdoc/>
    public void Share(IDictionary<string, object?> props) => _coreFactory.Share(props);

    /// <inheritdoc/>
    public object? GetShared(string? key = null, object? defaultValue = null) =>
        _coreFactory.GetShared(key, defaultValue);

    /// <inheritdoc/>
    public void FlushShared() => _coreFactory.FlushShared();

    /// <inheritdoc/>
    public void SetVersion(string version) => _coreFactory.SetVersion(version);

    /// <inheritdoc/>
    public void SetVersion(Func<string> versionProvider) => _coreFactory.SetVersion(versionProvider);

    /// <inheritdoc/>
    public string GetVersion() => _coreFactory.GetVersion();

    /// <inheritdoc/>
    public void SetRootView(string viewName) => _coreFactory.SetRootView(viewName);

    /// <inheritdoc/>
    public void ClearHistory() => _coreFactory.ClearHistory();

    /// <inheritdoc/>
    public void EncryptHistory(bool encrypt = true) => _coreFactory.EncryptHistory(encrypt);

    /// <inheritdoc/>
    public void ResolveUrlUsing(Func<string>? urlResolver) => _coreFactory.ResolveUrlUsing(urlResolver);

    /// <summary>
    /// Resolves properties for the response, handling partial reloads, callbacks, and property providers.
    /// This is the main property resolution pipeline that mimics Laravel's Response::resolveProperties().
    /// </summary>
    /// <param name="props">The properties to resolve.</param>
    /// <param name="request">The HTTP request.</param>
    /// <param name="component">The component being rendered.</param>
    /// <returns>The resolved properties.</returns>
    private async Task<Dictionary<string, object?>> ResolvePropertiesAsync(
        IDictionary<string, object?> props,
        HttpRequest request,
        string component)
    {
        var result = new Dictionary<string, object?>(props);

        // Step 1: Resolve property providers (IProvidesInertiaProperties)
        result = await ResolveInertiaPropsProvidersAsync(result, request, component);

        // Step 2: Handle partial reloads - filter based on X-Inertia-Partial-Data/Except headers
        result = ResolvePartialProperties(result, request, component);

        // Step 3: Resolve property instances (callbacks, prop types, providers)
        result = await ResolvePropertyInstancesAsync(result, request);

        return result;
    }

    /// <summary>
    /// Resolves objects implementing IProvidesInertiaProperties.
    /// These objects can provide multiple properties at once.
    /// </summary>
    private async Task<Dictionary<string, object?>> ResolveInertiaPropsProvidersAsync(
        Dictionary<string, object?> props,
        HttpRequest request,
        string component)
    {
        var newProps = new Dictionary<string, object?>();
        var renderContext = new RenderContext<HttpRequest>(component, request);

        foreach (var kvp in props)
        {
            // Check if the value (not the key) is a provider
            if (kvp.Value is IProvidesInertiaProperties<HttpRequest> provider)
            {
                // Get all properties from the provider
                var providedProps = await Task.Run(() => provider.ToInertiaProperties(renderContext));
                foreach (var providedKvp in providedProps)
                {
                    newProps[providedKvp.Key] = providedKvp.Value;
                }
            }
            else
            {
                newProps[kvp.Key] = kvp.Value;
            }
        }

        return newProps;
    }

    /// <summary>
    /// Resolves properties for partial requests, filtering based on 'only' and 'except' headers.
    /// Also removes properties marked with IIgnoreFirstLoad on initial (non-partial) loads.
    /// </summary>
    private Dictionary<string, object?> ResolvePartialProperties(
        Dictionary<string, object?> props,
        HttpRequest request,
        string component)
    {
        var isPartial = IsPartialReload(request, component);

        if (!isPartial)
        {
            // On initial load, filter out properties that implement IIgnoreFirstLoad
            return props.Where(kvp => kvp.Value is not IIgnoreFirstLoad)
                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        // For partial reloads, handle 'only' and 'except' filters
        var only = request.GetPartialData();
        var except = request.GetPartialExcept();

        if (only.Length > 0)
        {
            // Include only the requested properties
            var filtered = new Dictionary<string, object?>();
            foreach (var key in only)
            {
                if (props.TryGetValue(key, out var value))
                {
                    filtered[key] = value;
                }
            }
            return filtered;
        }

        if (except.Length > 0)
        {
            // Exclude the specified properties
            return props.Where(kvp => !except.Contains(kvp.Key))
                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return props;
    }

    /// <summary>
    /// Resolves all property instances recursively, handling closures, callbacks,
    /// property types, property providers, and nested dictionaries.
    /// </summary>
    private async Task<Dictionary<string, object?>> ResolvePropertyInstancesAsync(
        Dictionary<string, object?> props,
        HttpRequest request,
        string? parentKey = null)
    {
        var resolved = new Dictionary<string, object?>();

        foreach (var kvp in props)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            var currentKey = parentKey != null ? $"{parentKey}.{key}" : key;

            // Resolve property types
            value = await ResolvePropertyTypeAsync(value);

            // Resolve IProvidesInertiaProperty
            if (value is IProvidesInertiaProperty<HttpRequest> propertyProvider)
            {
                var propertyContext = new PropertyContext<HttpRequest>(currentKey, props, request);
                value = await Task.Run(() => propertyProvider.ToInertiaProperty(propertyContext));
            }

            // Resolve callbacks
            value = await ResolveCallbackAsync(value);

            // Recursively resolve nested dictionaries
            if (value is Dictionary<string, object?> nestedDict)
            {
                value = await ResolvePropertyInstancesAsync(nestedDict, request, currentKey);
            }
            else if (value is IDictionary<string, object?> nestedIDict)
            {
                // Convert IDictionary to Dictionary for recursive resolution
                var dictCopy = new Dictionary<string, object?>(nestedIDict);
                value = await ResolvePropertyInstancesAsync(dictCopy, request, currentKey);
            }
            else if (value is IDictionary dictionary && value is not string)
            {
                // Handle non-generic dictionaries
                var typedDict = new Dictionary<string, object?>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    typedDict[entry.Key.ToString() ?? string.Empty] = entry.Value;
                }
                value = await ResolvePropertyInstancesAsync(typedDict, request, currentKey);
            }

            resolved[key] = value;
        }

        return resolved;
    }

    /// <summary>
    /// Resolves a property type (OptionalProp, DeferProp, AlwaysProp, MergeProp, ScrollProp, or OnceProp) 
    /// by calling its ResolveAsync method.
    /// </summary>
    private async Task<object?> ResolvePropertyTypeAsync(object? value)
    {
        return value switch
        {
            OptionalProp optional => await optional.ResolveAsync(),
            DeferProp defer => await defer.ResolveAsync(),
            AlwaysProp always => await always.ResolveAsync(),
            MergeProp merge => await merge.ResolveAsync(),
            ScrollProp scroll => await scroll.ResolveAsync(),
            OnceProp once => await once.ResolveAsync(),
            _ => value
        };
    }

    /// <summary>
    /// Resolves a callback (Func of T or Func of Task of T) by invoking it.
    /// </summary>
    private async Task<object?> ResolveCallbackAsync(object? value)
    {
        if (value is Func<Task<object?>> asyncFunc)
        {
            return await asyncFunc();
        }

        if (value is Func<object?> syncFunc)
        {
            return syncFunc();
        }

        // Handle generic Func of Task of T
        if (value?.GetType().IsGenericType == true)
        {
            var type = value.GetType();
            if (type.GetGenericTypeDefinition() == typeof(Func<>))
            {
                var returnType = type.GetGenericArguments()[0];
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    // It's a Func of Task of T
                    var invokeMethod = type.GetMethod("Invoke");
                    if (invokeMethod != null)
                    {
                        var task = invokeMethod.Invoke(value, null);
                        if (task is Task taskResult)
                        {
                            await taskResult;
                            var resultProperty = taskResult.GetType().GetProperty("Result");
                            return resultProperty?.GetValue(taskResult);
                        }
                    }
                }
                else
                {
                    // It's a Func of T
                    var invokeMethod = type.GetMethod("Invoke");
                    if (invokeMethod != null)
                    {
                        return invokeMethod.Invoke(value, null);
                    }
                }
            }
        }

        return value;
    }

    /// <summary>
    /// Determines if the current request is a partial reload for the given component.
    /// </summary>
    private bool IsPartialReload(HttpRequest request, string component)
    {
        if (!request.IsInertia())
        {
            return false;
        }

        var partialComponent = request.GetPartialComponent();
        if (string.IsNullOrEmpty(partialComponent))
        {
            return false;
        }

        return partialComponent == component;
    }
}
