using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Extension methods for <see cref="HttpRequest"/> to detect Inertia requests.
/// </summary>
public static class InertiaHttpRequestExtensions
{
    /// <summary>Returns whether the current request is an Inertia request (has the X-Inertia header).</summary>
    /// <param name="request">The HTTP request.</param>
    public static bool IsInertia(this HttpRequest request)
        => request.Headers.ContainsKey(InertiaHeaderNames.Inertia);
}
