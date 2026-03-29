namespace Inertia.AspNetCore;

/// <summary>
/// Exception thrown when an Inertia page component cannot be found.
/// </summary>
public sealed class ComponentNotFoundException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="ComponentNotFoundException"/> class.</summary>
    public ComponentNotFoundException()
    {
    }

    /// <summary>Initializes a new instance with a specified error message.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ComponentNotFoundException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with a specified error message and inner exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ComponentNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
