using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Provides context about the exception being handled for an Inertia response.
/// </summary>
public sealed class InertiaExceptionContext
{
    /// <summary>The exception that was thrown.</summary>
    public required Exception Exception { get; init; }

    /// <summary>The HTTP context for the current request.</summary>
    public required HttpContext HttpContext { get; init; }

    /// <summary>The HTTP status code derived from the exception.</summary>
    public required int StatusCode { get; init; }
}
