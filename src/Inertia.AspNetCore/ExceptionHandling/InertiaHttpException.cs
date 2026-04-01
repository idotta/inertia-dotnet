namespace Inertia.AspNetCore;

/// <summary>
/// An exception that represents an HTTP error with a specific status code.
/// This is the C# equivalent of PHP's <c>abort(403)</c> — throw it to trigger
/// the Inertia exception handler with a specific HTTP status code.
/// </summary>
public sealed class InertiaHttpException : Exception
{
    /// <summary>The HTTP status code associated with this exception.</summary>
    public int StatusCode { get; }

    /// <summary>Creates a new <see cref="InertiaHttpException"/> with the specified status code.</summary>
    /// <param name="statusCode">The HTTP status code (e.g., 403, 404, 500).</param>
    public InertiaHttpException(int statusCode)
        : base($"HTTP {statusCode}") => StatusCode = statusCode;

    /// <summary>Creates a new <see cref="InertiaHttpException"/> with the specified status code and message.</summary>
    /// <param name="statusCode">The HTTP status code (e.g., 403, 404, 500).</param>
    /// <param name="message">The error message.</param>
    public InertiaHttpException(int statusCode, string message)
        : base(message) => StatusCode = statusCode;

    /// <summary>Creates a new <see cref="InertiaHttpException"/> with the specified status code, message, and inner exception.</summary>
    /// <param name="statusCode">The HTTP status code (e.g., 403, 404, 500).</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public InertiaHttpException(int statusCode, string message, Exception? innerException)
        : base(message, innerException) => StatusCode = statusCode;
}
