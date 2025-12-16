namespace Inertia.Core.Ssr;

/// <summary>
/// Exception thrown when an error occurs during server-side rendering.
/// </summary>
public class SsrException : Exception
{
    /// <summary>
    /// Gets the SSR server URL that was attempted.
    /// </summary>
    public string? SsrUrl { get; }

    /// <summary>
    /// Gets additional diagnostic information about the error.
    /// </summary>
    public string? DiagnosticInfo { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SsrException"/> class.
    /// </summary>
    public SsrException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SsrException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SsrException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SsrException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SsrException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SsrException"/> class with a specified error message, SSR URL, and diagnostic info.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="ssrUrl">The SSR server URL that was attempted.</param>
    /// <param name="diagnosticInfo">Additional diagnostic information about the error.</param>
    public SsrException(string message, string? ssrUrl, string? diagnosticInfo = null)
        : base(message)
    {
        SsrUrl = ssrUrl;
        DiagnosticInfo = diagnosticInfo;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SsrException"/> class with a specified error message, SSR URL, diagnostic info, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="ssrUrl">The SSR server URL that was attempted.</param>
    /// <param name="diagnosticInfo">Additional diagnostic information about the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SsrException(string message, string? ssrUrl, string? diagnosticInfo, Exception innerException)
        : base(message, innerException)
    {
        SsrUrl = ssrUrl;
        DiagnosticInfo = diagnosticInfo;
    }
}
