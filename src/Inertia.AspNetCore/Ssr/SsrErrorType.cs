namespace Inertia.AspNetCore;

/// <summary>Categorizes SSR rendering failures.</summary>
public enum SsrErrorType
{
    /// <summary>Unknown or unclassified error.</summary>
    Unknown,

    /// <summary>A browser-only API was accessed during server-side rendering.</summary>
    BrowserApi,

    /// <summary>The page component could not be resolved.</summary>
    ComponentResolution,

    /// <summary>An error occurred during component rendering.</summary>
    Render,

    /// <summary>The SSR server could not be reached.</summary>
    Connection,
}

/// <summary>Parses SSR error type strings from the SSR server JSON response.</summary>
internal static class SsrErrorTypeParser
{
    /// <summary>Converts a wire-format string to an <see cref="SsrErrorType"/>, defaulting to <see cref="SsrErrorType.Unknown"/>.</summary>
    internal static SsrErrorType FromString(string? value) => value switch
    {
        "browser-api" => SsrErrorType.BrowserApi,
        "component-resolution" => SsrErrorType.ComponentResolution,
        "render" => SsrErrorType.Render,
        "connection" => SsrErrorType.Connection,
        _ => SsrErrorType.Unknown,
    };
}
