namespace Inertia.AspNetCore;

/// <summary>
/// HTTP header name constants used by the Inertia.js protocol.
/// </summary>
public static class InertiaHeaderNames
{
    /// <summary>The main Inertia request/response header. Present on all Inertia requests and JSON responses.</summary>
    public const string Inertia = "X-Inertia";

    /// <summary>Specifies which error bag to use for validation errors.</summary>
    public const string ErrorBag = "X-Inertia-Error-Bag";

    /// <summary>Used for external redirects that force a full page visit.</summary>
    public const string Location = "X-Inertia-Location";

    /// <summary>Used for hash fragment redirects.</summary>
    public const string Redirect = "X-Inertia-Redirect";

    /// <summary>The current asset version for cache busting.</summary>
    public const string Version = "X-Inertia-Version";

    /// <summary>Specifies the component for partial reloads.</summary>
    public const string PartialComponent = "X-Inertia-Partial-Component";

    /// <summary>Specifies which props to include in partial reloads.</summary>
    public const string PartialOnly = "X-Inertia-Partial-Data";

    /// <summary>Specifies which props to exclude from partial reloads.</summary>
    public const string PartialExcept = "X-Inertia-Partial-Except";

    /// <summary>Signals that the page state should be reset.</summary>
    public const string Reset = "X-Inertia-Reset";

    /// <summary>Specifies the merge intent when paginating with infinite scroll.</summary>
    public const string InfiniteScrollMergeIntent = "X-Inertia-Infinite-Scroll-Merge-Intent";

    /// <summary>Specifies which once-resolved props to exclude from the response.</summary>
    public const string ExceptOnceProps = "X-Inertia-Except-Once-Props";
}
