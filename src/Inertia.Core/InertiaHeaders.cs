namespace Inertia.Core;

/// <summary>
/// HTTP header constants used by Inertia.js for request and response handling.
/// </summary>
public static class InertiaHeaders
{
    /// <summary>
    /// The main Inertia request header. Used to identify an Inertia request.
    /// </summary>
    public const string Inertia = "X-Inertia";

    /// <summary>
    /// Header for the current asset version. Used for cache busting.
    /// </summary>
    public const string Version = "X-Inertia-Version";

    /// <summary>
    /// Header specifying which props to include in partial reloads.
    /// </summary>
    public const string PartialData = "X-Inertia-Partial-Data";

    /// <summary>
    /// Header specifying the component for partial reloads.
    /// Used to validate that the partial reload is for the correct component.
    /// </summary>
    public const string PartialComponent = "X-Inertia-Partial-Component";

    /// <summary>
    /// Header specifying which props to exclude from partial reloads.
    /// </summary>
    public const string PartialExcept = "X-Inertia-Partial-Except";

    /// <summary>
    /// Header for specifying which error bag to use for validation errors.
    /// </summary>
    public const string ErrorBag = "X-Inertia-Error-Bag";

    /// <summary>
    /// Header for external redirects. Used with 409 status code to force a client-side redirect.
    /// </summary>
    public const string Location = "X-Inertia-Location";

    /// <summary>
    /// Header for resetting the page state.
    /// </summary>
    public const string Reset = "X-Inertia-Reset";

    /// <summary>
    /// Header for specifying the merge intent when paginating on infinite scroll.
    /// Values can be 'append' or 'prepend'.
    /// </summary>
    public const string InfiniteScrollMergeIntent = "X-Inertia-Infinite-Scroll-Merge-Intent";

    /// <summary>
    /// Header specifying which once props to exclude from the response.
    /// Used to prevent re-sending props that have already been cached.
    /// </summary>
    public const string ExceptOnceProps = "X-Inertia-Except-Once-Props";
}
