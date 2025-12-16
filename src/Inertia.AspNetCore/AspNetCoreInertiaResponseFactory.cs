using Inertia.Core;
using Inertia.Core.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Text.Json;

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
    private const string SessionKeyPrefix = "inertia.once.";
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
            var (resolvedProps, metadata) = await ResolvePropertiesWithMetadataAsync(response.Props, httpContext.Request, component);

            // Update the response props with resolved values
            response.Props.Clear();
            foreach (var kvp in resolvedProps)
            {
                response.Props[kvp.Key] = kvp.Value;
            }

            // Update metadata
            response.MergeProps.AddRange(metadata.MergeProps);
            response.DeferredProps.AddRange(metadata.DeferredProps);
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
    /// Holds metadata about properties collected during resolution.
    /// </summary>
    private class PropertyMetadata
    {
        public List<string> MergeProps { get; } = new();
        public List<string> DeferredProps { get; } = new();
    }

    /// <summary>
    /// Resolves properties for the response with metadata tracking.
    /// </summary>
    /// <param name="props">The properties to resolve.</param>
    /// <param name="request">The HTTP request.</param>
    /// <param name="component">The component being rendered.</param>
    /// <returns>A tuple containing the resolved properties and metadata.</returns>
    private async Task<(Dictionary<string, object?>, PropertyMetadata)> ResolvePropertiesWithMetadataAsync(
        IDictionary<string, object?> props,
        HttpRequest request,
        string component)
    {
        var result = new Dictionary<string, object?>(props);
        var metadata = new PropertyMetadata();

        // Step 1: Collect metadata about merge and deferred props before filtering
        CollectPropertyMetadata(result, metadata);

        // Step 2: Resolve property providers (IProvidesInertiaProperties)
        result = await ResolveInertiaPropsProvidersAsync(result, request, component);

        // Step 3: Handle partial reloads - filter based on X-Inertia-Partial-Data/Except headers
        result = ResolvePartialProperties(result, request, component);

        // Step 4: Resolve property instances (callbacks, prop types, providers)
        result = await ResolvePropertyInstancesAsync(result, request);

        // Step 5: Filter metadata to only include props that are still in the result
        FilterMetadataByResolvedProps(metadata, result);

        return (result, metadata);
    }

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
        var httpContext = request.HttpContext;

        foreach (var kvp in props)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            var currentKey = parentKey != null ? $"{parentKey}.{key}" : key;

            // Handle once props with session caching
            if (value is OnceProp onceProp)
            {
                // Check if we should reset this prop
                if (ShouldResetOnceProp(request, currentKey))
                {
                    RemoveCachedOnceProp(httpContext, currentKey);
                }

                // Try to get from cache first
                if (TryGetCachedOnceProp(httpContext, currentKey, out var cachedValue))
                {
                    value = cachedValue;
                }
                else
                {
                    // Not in cache, resolve it
                    value = await onceProp.ResolveAsync();

                    // Cache the resolved value
                    CacheOnceProp(httpContext, currentKey, value);
                }
            }
            else
            {
                // Resolve other property types normally
                value = await ResolvePropertyTypeAsync(value);
            }

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
    /// Resolves a property type (OptionalProp, DeferProp, AlwaysProp, MergeProp, or ScrollProp) 
    /// by calling its ResolveAsync method.
    /// Note: OnceProp is handled separately in ResolvePropertyInstancesAsync with session caching.
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
            OnceProp once => await once.ResolveAsync(), // Fallback, should not reach here
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

    /// <summary>
    /// Gets the session key for a once prop.
    /// </summary>
    /// <param name="propKey">The property key.</param>
    /// <returns>The session key.</returns>
    private string GetSessionKey(string propKey)
    {
        return $"{SessionKeyPrefix}{propKey}";
    }

    /// <summary>
    /// Checks if a once prop should be reset based on the X-Inertia-Reset header.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="propKey">The property key.</param>
    /// <returns>True if the prop should be reset; otherwise, false.</returns>
    private bool ShouldResetOnceProp(HttpRequest request, string propKey)
    {
        var resetProps = request.GetReset();

        // Check if "all" is requested
        if (resetProps.Contains("all", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if this specific prop is requested to be reset
        return resetProps.Contains(propKey, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tries to get a cached once prop value from the session.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="propKey">The property key.</param>
    /// <param name="value">The cached value, if found.</param>
    /// <returns>True if the value was found in cache; otherwise, false.</returns>
    private bool TryGetCachedOnceProp(HttpContext httpContext, string propKey, out object? value)
    {
        value = null;

        // Session might not be available if not configured
        // Check the session feature first to avoid exceptions
        var sessionFeature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>();
        if (sessionFeature?.Session == null)
        {
            return false;
        }

        var sessionKey = GetSessionKey(propKey);
        var cachedJson = sessionFeature.Session.GetString(sessionKey);

        if (string.IsNullOrEmpty(cachedJson))
        {
            return false;
        }

        try
        {
            // Deserialize to JsonElement which preserves the JSON structure
            using var document = JsonDocument.Parse(cachedJson);
            value = DeserializeJsonElement(document.RootElement);
            return true;
        }
        catch (JsonException)
        {
            // If deserialization fails (e.g., invalid JSON), treat as cache miss
            return false;
        }
    }

    /// <summary>
    /// Deserializes a JsonElement to a .NET object.
    /// </summary>
    private object? DeserializeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => DeserializeJsonElement(p.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(DeserializeJsonElement)
                .ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    /// <summary>
    /// Caches a once prop value in the session.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="propKey">The property key.</param>
    /// <param name="value">The value to cache.</param>
    private void CacheOnceProp(HttpContext httpContext, string propKey, object? value)
    {
        // Session might not be available if not configured
        var sessionFeature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>();
        if (sessionFeature?.Session == null)
        {
            return;
        }

        var sessionKey = GetSessionKey(propKey);

        try
        {
            // Use JsonSerializer.Serialize for both null and non-null values for consistency
            var json = JsonSerializer.Serialize(value);
            sessionFeature.Session.SetString(sessionKey, json);
        }
        catch (JsonException)
        {
            // If serialization fails, don't cache
            // This is graceful degradation - the prop will still work, just won't be cached
        }
        catch (NotSupportedException)
        {
            // Some types cannot be serialized (e.g., types with circular references)
            // This is graceful degradation - the prop will still work, just won't be cached
        }
    }

    /// <summary>
    /// Removes a once prop from the session cache.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="propKey">The property key.</param>
    private void RemoveCachedOnceProp(HttpContext httpContext, string propKey)
    {
        // Session might not be available if not configured
        var sessionFeature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>();
        if (sessionFeature?.Session == null)
        {
            return;
        }

        var sessionKey = GetSessionKey(propKey);
        sessionFeature.Session.Remove(sessionKey);
    }

    /// <summary>
    /// Collects metadata about properties (merge props, deferred props) recursively.
    /// </summary>
    /// <param name="props">The properties to analyze.</param>
    /// <param name="metadata">The metadata object to populate.</param>
    /// <param name="parentKey">The parent key for nested properties.</param>
    private void CollectPropertyMetadata(
        IDictionary<string, object?> props,
        PropertyMetadata metadata,
        string? parentKey = null)
    {
        foreach (var kvp in props)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            var currentKey = parentKey != null ? $"{parentKey}.{key}" : key;

            // Check if this is a merge prop
            if (value is MergeProp)
            {
                metadata.MergeProps.Add(currentKey);
            }

            // Check if this is a deferred prop
            if (value is DeferProp)
            {
                metadata.DeferredProps.Add(currentKey);
            }

            // Check if this is a scroll prop (which is also a merge prop)
            if (value is ScrollProp)
            {
                metadata.MergeProps.Add(currentKey);
            }

            // Recursively check nested dictionaries
            if (value is Dictionary<string, object?> nestedDict)
            {
                CollectPropertyMetadata(nestedDict, metadata, currentKey);
            }
            else if (value is IDictionary<string, object?> nestedIDict && value is not string)
            {
                var dictCopy = new Dictionary<string, object?>(nestedIDict);
                CollectPropertyMetadata(dictCopy, metadata, currentKey);
            }
        }
    }

    /// <summary>
    /// Filters metadata to only include properties that are in the resolved props.
    /// </summary>
    /// <param name="metadata">The metadata to filter.</param>
    /// <param name="resolvedProps">The resolved properties.</param>
    private void FilterMetadataByResolvedProps(PropertyMetadata metadata, Dictionary<string, object?> resolvedProps)
    {
        // Get all resolved prop keys (including nested keys)
        var resolvedKeys = GetAllPropertyKeys(resolvedProps);

        // Filter merge props
        metadata.MergeProps.RemoveAll(key => !resolvedKeys.Contains(key));

        // Filter deferred props
        metadata.DeferredProps.RemoveAll(key => !resolvedKeys.Contains(key));
    }

    /// <summary>
    /// Gets all property keys from a dictionary, including nested keys with dot notation.
    /// </summary>
    /// <param name="props">The properties.</param>
    /// <param name="parentKey">The parent key for nested properties.</param>
    /// <returns>A set of all property keys.</returns>
    private HashSet<string> GetAllPropertyKeys(Dictionary<string, object?> props, string? parentKey = null)
    {
        var keys = new HashSet<string>();

        foreach (var kvp in props)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            var currentKey = parentKey != null ? $"{parentKey}.{key}" : key;

            keys.Add(currentKey);

            // Recursively get nested keys
            if (value is Dictionary<string, object?> nestedDict)
            {
                var nestedKeys = GetAllPropertyKeys(nestedDict, currentKey);
                keys.UnionWith(nestedKeys);
            }
        }

        return keys;
    }
}
