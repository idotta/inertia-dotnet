using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Inertia.AspNetCore;

/// <summary>
/// Base class for handling Inertia requests in your application.
/// Extend this class to customize version detection, shared data, and validation error handling.
/// </summary>
public abstract class HandleInertiaRequests
{
    /// <summary>
    /// Determine the current asset version.
    /// This is used to detect when the client needs to reload due to asset changes.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>A version string, or null if no versioning is used.</returns>
    public virtual string? Version(HttpRequest request)
    {
        return null;
    }

    /// <summary>
    /// Define the props that are shared by default across all Inertia responses.
    /// These props are merged with page-specific props.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>A dictionary of props to share.</returns>
    public virtual IDictionary<string, object?> Share(HttpRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["errors"] = ResolveValidationErrors(request.HttpContext)
        };
    }

    /// <summary>
    /// Define the props that are shared once and remembered across navigations.
    /// These are typically used for data that doesn't change during a session.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>A dictionary of once props to share.</returns>
    public virtual IDictionary<string, object?> ShareOnce(HttpRequest request)
    {
        return new Dictionary<string, object?>();
    }

    /// <summary>
    /// Set the root template that is loaded on the first page visit.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The name of the root view.</returns>
    public virtual string RootView(HttpRequest request)
    {
        return "app";
    }

    /// <summary>
    /// Define a callback that returns the relative URL.
    /// This is useful for applications that are not hosted at the domain root.
    /// </summary>
    /// <returns>A function that resolves the URL, or null to use the default.</returns>
    public virtual Func<string>? UrlResolver()
    {
        return null;
    }

    /// <summary>
    /// Resolve validation errors for client-side use.
    /// By default, this retrieves errors from TempData.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>An object containing validation errors, or an empty object if none.</returns>
    public virtual object ResolveValidationErrors(HttpContext context)
    {
        // Get validation errors from TempData (set by ModelState or validation filters)
        if (context.Items.TryGetValue("InertiaValidationErrors", out var errors))
        {
            return errors ?? new { };
        }

        return new { };
    }

    /// <summary>
    /// Handle empty responses.
    /// By default, this redirects back to the previous page.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task OnEmptyResponse(HttpContext context)
    {
        // Redirect back to referer or root
        var referer = context.Request.Headers.Referer.ToString();
        var redirectUrl = !string.IsNullOrEmpty(referer) ? referer : "/";

        context.Response.StatusCode = 302;
        context.Response.Headers.Location = redirectUrl;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handle version changes.
    /// By default, this forces a full page reload by returning a 409 with X-Inertia-Location header.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task OnVersionChange(HttpContext context)
    {
        // Force a client-side full page reload
        context.Response.StatusCode = 409;
        context.Response.Headers[Core.InertiaHeaders.Location] = context.Request.GetEncodedUrl();

        return Task.CompletedTask;
    }
}
