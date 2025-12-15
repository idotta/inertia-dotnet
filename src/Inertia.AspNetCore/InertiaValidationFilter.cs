using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Inertia.AspNetCore;

/// <summary>
/// Action filter that automatically handles validation errors for Inertia requests.
/// When ModelState is invalid, this filter makes errors available to the Inertia response.
/// </summary>
/// <remarks>
/// This filter integrates ASP.NET Core ModelState validation with Inertia's error handling.
/// Validation errors are stored in HttpContext.Items and can be accessed by HandleInertiaRequests.
/// Supports multiple errors per field and error bags via the X-Inertia-Error-Bag header.
/// </remarks>
public class InertiaValidationFilter : IActionFilter
{
    /// <summary>
    /// Called before the action method executes.
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // No pre-action logic needed
    }

    /// <summary>
    /// Called after the action method executes.
    /// Checks for validation errors and makes them available to Inertia.
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Only process if ModelState is invalid
        if (context.ModelState.IsValid)
        {
            return;
        }

        // Get error bag from header if specified
        var errorBag = context.HttpContext.Request.Headers[Core.InertiaHeaders.ErrorBag].ToString();

        // Convert ModelState errors to dictionary format
        var errors = ConvertModelStateErrors(context.ModelState, string.IsNullOrEmpty(errorBag) ? null : errorBag);

        // Store errors in HttpContext.Items for access by HandleInertiaRequests
        context.HttpContext.Items["InertiaValidationErrors"] = errors;
    }

    /// <summary>
    /// Converts ModelState errors to a dictionary format suitable for Inertia.
    /// </summary>
    /// <param name="modelState">The ModelState dictionary containing validation errors.</param>
    /// <param name="errorBag">Optional error bag name for organizing errors.</param>
    /// <returns>A dictionary of validation errors.</returns>
    private static object ConvertModelStateErrors(
        Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState,
        string? errorBag)
    {
        var errors = new Dictionary<string, string[]>();

        foreach (var key in modelState.Keys)
        {
            var state = modelState[key];
            if (state != null && state.Errors.Count > 0)
            {
                // Get all error messages for this field
                var fieldErrors = state.Errors
                    .Select(e => !string.IsNullOrEmpty(e.ErrorMessage)
                        ? e.ErrorMessage
                        : e.Exception?.Message ?? "Validation error")
                    .ToArray();

                errors[key] = fieldErrors;
            }
        }

        // If error bag is specified, wrap errors in a nested dictionary
        if (!string.IsNullOrEmpty(errorBag))
        {
            return new Dictionary<string, Dictionary<string, string[]>>
            {
                [errorBag] = errors
            };
        }

        return errors;
    }
}
