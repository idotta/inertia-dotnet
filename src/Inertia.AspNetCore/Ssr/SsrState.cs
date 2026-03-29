namespace Inertia.AspNetCore;

/// <summary>
/// Scoped per-request SSR state. Stores the page for SSR dispatch, caches the dispatch result,
/// and holds per-request path exclusions. Tag Helpers (Phase 7) call <see cref="DispatchAsync"/>
/// to get the SSR-rendered HTML.
/// </summary>
internal sealed class SsrState
{
    private readonly ISsrGateway _gateway;
    private readonly HashSet<string> _excludedPaths = new(StringComparer.OrdinalIgnoreCase);
    private InertiaPage? _page;
    private SsrResponse? _response;
    private bool _dispatched;

    public SsrState(ISsrGateway gateway)
    {
        _gateway = gateway;
    }

    /// <summary>Stores the page data for SSR dispatch.</summary>
    public void SetPage(InertiaPage page) => _page = page;

    /// <summary>The stored page, or null if not set.</summary>
    public InertiaPage? Page => _page;

    /// <summary>Excludes paths from SSR. Supports exact match and trailing wildcard (e.g., "/api/*").</summary>
    public void ExcludePaths(params string[] paths)
    {
        foreach (var path in paths)
            _excludedPaths.Add(path);
    }

    /// <summary>Returns whether the given request path is excluded from SSR.</summary>
    public bool IsPathExcluded(string requestPath)
    {
        foreach (var pattern in _excludedPaths)
        {
            if (pattern.EndsWith("/*"))
            {
                var prefix = pattern[..^2];
                if (requestPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (string.Equals(pattern, requestPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Dispatches to the SSR gateway. The result is cached — the gateway is called at most once per request.
    /// Returns null if no page has been set or if the gateway returns null.
    /// </summary>
    public async Task<SsrResponse?> DispatchAsync(CancellationToken cancellationToken = default)
    {
        if (!_dispatched)
        {
            _dispatched = true;
            if (_page is not null)
                _response = await _gateway.DispatchAsync(_page, cancellationToken);
        }

        return _response;
    }
}
