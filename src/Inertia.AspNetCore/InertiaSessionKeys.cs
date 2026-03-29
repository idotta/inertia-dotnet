namespace Inertia.AspNetCore;

/// <summary>
/// TempData key constants used by the Inertia middleware for cross-request state.
/// </summary>
public static class InertiaSessionKeys
{
    /// <summary>TempData key for signaling that the browser history should be cleared.</summary>
    public const string ClearHistory = "inertia.clear_history";

    /// <summary>TempData key for flash data that should be shared with the next Inertia response.</summary>
    public const string FlashData = "inertia.flash_data";

    /// <summary>TempData key for preserving the URL fragment across redirects.</summary>
    public const string PreserveFragment = "inertia.preserve_fragment";
}
