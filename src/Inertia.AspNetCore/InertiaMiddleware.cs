using Inertia.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.AspNetCore;

/// <summary>
/// Core Inertia middleware that handles Inertia requests and responses.
/// This middleware must be registered after routing and before endpoint execution.
/// </summary>
public class InertiaMiddleware : IMiddleware
{
    private readonly HandleInertiaRequests _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="InertiaMiddleware"/> class.
    /// </summary>
    /// <param name="handler">The Inertia request handler.</param>
    public InertiaMiddleware(HandleInertiaRequests handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Process an individual request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;
        
        // Get the IInertia service from DI
        var inertia = context.RequestServices.GetRequiredService<IInertia>();

        // Configure version from handler
        var version = _handler.Version(request);
        if (version != null)
        {
            inertia.SetVersion(() => version);
        }

        // Share props from handler
        var sharedProps = _handler.Share(request);
        if (sharedProps.Count > 0)
        {
            inertia.Share(sharedProps);
        }

        // Share once props from handler
        var onceProps = _handler.ShareOnce(request);
        foreach (var kvp in onceProps)
        {
            // TODO: Implement ShareOnce when we add RenderContext in Phase 3.2
            // For now, just share them as regular props
            inertia.Share(kvp.Key, kvp.Value);
        }

        // Set root view from handler
        var rootView = _handler.RootView(request);
        if (!string.IsNullOrEmpty(rootView))
        {
            inertia.SetRootView(rootView);
        }

        // Set URL resolver from handler
        var urlResolver = _handler.UrlResolver();
        if (urlResolver != null)
        {
            inertia.ResolveUrlUsing(urlResolver);
        }

        // Call next middleware
        await next(context);

        // Post-processing after the response has been generated
        var response = context.Response;

        // Always add Vary header for proper HTTP caching
        response.Headers.Append("Vary", InertiaHeaders.Inertia);

        // Only process if this is an Inertia request
        if (!request.IsInertia())
        {
            return;
        }

        // Check for version mismatch on GET requests
        if (request.Method == HttpMethods.Get)
        {
            var requestVersion = request.GetInertiaVersion();
            var currentVersion = inertia.GetVersion();
            
            if (!string.IsNullOrEmpty(requestVersion) && 
                !string.IsNullOrEmpty(currentVersion) && 
                requestVersion != currentVersion)
            {
                await _handler.OnVersionChange(context);
                return;
            }
        }

        // Handle empty responses (200 OK with no content)
        if (response.StatusCode == 200 && response.ContentLength.GetValueOrDefault(0) == 0)
        {
            await _handler.OnEmptyResponse(context);
            return;
        }

        // Change 302 redirects to 303 for PUT/PATCH/DELETE requests
        // This ensures the browser follows the redirect with a GET request
        if (response.StatusCode == 302 && 
            (request.Method == HttpMethods.Put || 
             request.Method == HttpMethods.Patch || 
             request.Method == HttpMethods.Delete))
        {
            response.StatusCode = 303;
        }
    }
}
