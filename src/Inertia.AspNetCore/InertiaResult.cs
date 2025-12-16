using Inertia.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text;
using System.Text.Json;

namespace Inertia.AspNetCore;

/// <summary>
/// Represents an action result that renders an Inertia response.
/// Handles both Inertia requests (JSON) and initial page loads (HTML).
/// </summary>
public class InertiaResult : IActionResult
{
    private readonly InertiaResponse _response;

    /// <summary>
    /// Initializes a new instance of the <see cref="InertiaResult"/> class.
    /// </summary>
    /// <param name="response">The Inertia response to render.</param>
    public InertiaResult(InertiaResponse response)
    {
        _response = response;
    }

    /// <summary>
    /// Executes the result operation of the action method asynchronously.
    /// </summary>
    /// <param name="context">The context in which the result is executed.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var request = context.HttpContext.Request;
        var response = context.HttpContext.Response;

        // Set the URL based on the current request
        if (string.IsNullOrEmpty(_response.Url))
        {
            _response.Url = request.GetEncodedUrl();
        }

        // Check if this is an Inertia request
        if (request.IsInertia())
        {
            // Return JSON for Inertia requests
            response.StatusCode = 200;
            response.ContentType = "application/json";

            var json = await _response.ToJsonAsync();
            await response.WriteAsync(json, Encoding.UTF8);
        }
        else
        {
            // Return HTML for initial page loads
            // Prepare the page data for the view
            var pageData = new Dictionary<string, object?>
            {
                ["component"] = _response.Component,
                ["props"] = _response.Props,
                ["url"] = _response.Url,
                ["version"] = _response.Version
            };

            // Create a ViewResult to render the root view
            var viewResult = new ViewResult
            {
                ViewName = _response.RootView,
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                    new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                    context.ModelState)
                {
                    ["page"] = pageData
                }
            };

            // Copy any additional view data from the response
            foreach (var kvp in _response.ViewData)
            {
                viewResult.ViewData[kvp.Key] = kvp.Value;
            }

            await viewResult.ExecuteResultAsync(context);
        }
    }
}

/// <summary>
/// Extension methods for converting InertiaResponse to IActionResult.
/// </summary>
public static class InertiaResponseExtensions
{
    /// <summary>
    /// Converts an InertiaResponse to an IActionResult.
    /// </summary>
    /// <param name="response">The Inertia response.</param>
    /// <returns>An IActionResult that can be returned from a controller action.</returns>
    public static IActionResult ToActionResult(this InertiaResponse response)
    {
        return new InertiaResult(response);
    }
}
