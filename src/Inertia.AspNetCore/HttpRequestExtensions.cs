using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Extension methods for HttpRequest to work with Inertia.js headers.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Determines whether the current request is an Inertia request.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>True if the request has the X-Inertia header set to true; otherwise, false.</returns>
    public static bool IsInertia(this HttpRequest request)
    {
        return request.Headers.ContainsKey(Core.InertiaHeaders.Inertia) &&
               request.Headers[Core.InertiaHeaders.Inertia].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the Inertia version from the request headers.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The version string, or null if not present.</returns>
    public static string? GetInertiaVersion(this HttpRequest request)
    {
        return request.Headers.TryGetValue(Core.InertiaHeaders.Version, out var version)
            ? version.ToString()
            : null;
    }

    /// <summary>
    /// Gets the partial component name from the request headers.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The component name for partial reloads, or null if not present.</returns>
    public static string? GetPartialComponent(this HttpRequest request)
    {
        return request.Headers.TryGetValue(Core.InertiaHeaders.PartialComponent, out var component)
            ? component.ToString()
            : null;
    }

    /// <summary>
    /// Gets the list of properties to include in a partial reload.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>An array of property names, or an empty array if not present.</returns>
    public static string[] GetPartialData(this HttpRequest request)
    {
        if (request.Headers.TryGetValue(Core.InertiaHeaders.PartialData, out var data))
        {
            var dataStr = data.ToString();
            return string.IsNullOrEmpty(dataStr)
                ? Array.Empty<string>()
                : dataStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Gets the list of properties to exclude in a partial reload.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>An array of property names to exclude, or an empty array if not present.</returns>
    public static string[] GetPartialExcept(this HttpRequest request)
    {
        if (request.Headers.TryGetValue(Core.InertiaHeaders.PartialExcept, out var except))
        {
            var exceptStr = except.ToString();
            return string.IsNullOrEmpty(exceptStr)
                ? Array.Empty<string>()
                : exceptStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Gets the error bag name from the request headers.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The error bag name, or null if not present.</returns>
    public static string? GetErrorBag(this HttpRequest request)
    {
        return request.Headers.TryGetValue(Core.InertiaHeaders.ErrorBag, out var errorBag)
            ? errorBag.ToString()
            : null;
    }

    /// <summary>
    /// Gets the reset properties list from the request headers.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>An array of property names to reset, or an empty array if not present.</returns>
    public static string[] GetReset(this HttpRequest request)
    {
        if (request.Headers.TryGetValue(Core.InertiaHeaders.Reset, out var reset))
        {
            var resetStr = reset.ToString();
            if (resetStr == "all")
            {
                return new[] { "all" };
            }
            return string.IsNullOrEmpty(resetStr)
                ? Array.Empty<string>()
                : resetStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        return Array.Empty<string>();
    }
}
