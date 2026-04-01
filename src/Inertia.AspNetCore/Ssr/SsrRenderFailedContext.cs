namespace Inertia.AspNetCore;

/// <summary>
/// Provides context about an SSR rendering failure. Passed to the
/// <see cref="InertiaOptions.OnSsrRenderFailed"/> callback.
/// </summary>
public sealed class SsrRenderFailedContext
{
    /// <summary>The page data that was being rendered.</summary>
    public required InertiaPage Page { get; init; }

    /// <summary>The error message from the SSR server or exception.</summary>
    public required string Error { get; init; }

    /// <summary>The category of the SSR failure.</summary>
    public required SsrErrorType ErrorType { get; init; }

    /// <summary>A hint on how to fix the error, if provided by the SSR server.</summary>
    public string? Hint { get; init; }

    /// <summary>The browser API that was accessed during SSR, if the error type is <see cref="SsrErrorType.BrowserApi"/>.</summary>
    public string? BrowserApi { get; init; }

    /// <summary>The JavaScript stack trace from the SSR server, if available.</summary>
    public string? Stack { get; init; }

    /// <summary>The source location (file:line:column) where the error occurred.</summary>
    public string? SourceLocation { get; init; }

    /// <summary>The underlying .NET exception, if the failure was caused by one (e.g., connection error).</summary>
    public Exception? Exception { get; init; }
}
