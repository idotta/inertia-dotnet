namespace Inertia.AspNetCore;

/// <summary>Contains the HTML output from server-side rendering.</summary>
/// <param name="Head">The rendered HTML head content (meta tags, title, etc.).</param>
/// <param name="Body">The rendered HTML body content.</param>
public sealed record SsrResponse(string Head, string Body);
