namespace Inertia.AspNetCore;

/// <summary>
/// Exception thrown when SSR rendering fails and <see cref="InertiaOptions.SsrThrowOnError"/> is enabled.
/// </summary>
public sealed class SsrException : Exception
{
    /// <summary>The component that failed to render.</summary>
    public string? Component { get; }

    /// <summary>The category of the SSR failure.</summary>
    public SsrErrorType ErrorType { get; }

    /// <summary>A hint on how to fix the error, if provided by the SSR server.</summary>
    public string? Hint { get; }

    /// <summary>The browser API that was accessed during SSR, if the error type is <see cref="SsrErrorType.BrowserApi"/>.</summary>
    public string? BrowserApi { get; }

    /// <summary>The JavaScript stack trace from the SSR server, if available.</summary>
    public string? Stack { get; }

    /// <summary>The source location (file:line:column) where the error occurred.</summary>
    public string? SourceLocation { get; }

    internal SsrException(string message, string? component, SsrErrorType errorType,
        string? hint = null, string? browserApi = null, string? stack = null,
        string? sourceLocation = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Component = component;
        ErrorType = errorType;
        Hint = hint;
        BrowserApi = browserApi;
        Stack = stack;
        SourceLocation = sourceLocation;
    }

    /// <summary>Creates an <see cref="SsrException"/> with a formatted message matching the PHP implementation.</summary>
    internal static SsrException Create(string component, string error,
        SsrErrorType errorType, string? hint = null, string? browserApi = null,
        string? stack = null, string? sourceLocation = null, Exception? innerException = null)
    {
        var message = $"SSR render failed for component [{component}]: {error}";
        if (sourceLocation is not null)
            message += $" at {sourceLocation}";
        return new SsrException(message, component, errorType, hint, browserApi, stack, sourceLocation, innerException);
    }
}
