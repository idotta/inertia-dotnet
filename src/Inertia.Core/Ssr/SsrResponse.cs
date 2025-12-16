namespace Inertia.Core.Ssr;

/// <summary>
/// Represents a server-side rendered response from the SSR gateway.
/// </summary>
public class SsrResponse
{
    /// <summary>
    /// Gets the HTML content to be placed in the head element.
    /// </summary>
    public string Head { get; }

    /// <summary>
    /// Gets the HTML content to be placed in the body (the rendered component).
    /// </summary>
    public string Body { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SsrResponse"/> class.
    /// </summary>
    /// <param name="head">The head content.</param>
    /// <param name="body">The body content.</param>
    public SsrResponse(string head, string body)
    {
        Head = head;
        Body = body;
    }
}
