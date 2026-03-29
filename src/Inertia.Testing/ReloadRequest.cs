using Inertia.AspNetCore;

namespace Inertia.Testing;

/// <summary>
/// Builds and executes an Inertia partial reload request with appropriate headers.
/// </summary>
internal sealed class ReloadRequest
{
    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly string _component;
    private readonly string _version;
    private readonly string? _only;
    private readonly string? _except;

    internal ReloadRequest(
        HttpClient httpClient,
        string url,
        string component,
        string version,
        string? only = null,
        string? except = null)
    {
        _httpClient = httpClient;
        _url = url;
        _component = component;
        _version = version;
        _only = only;
        _except = except;
    }

    /// <summary>Executes the reload request and returns the HTTP response.</summary>
    internal async Task<HttpResponseMessage> ExecuteAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _url);
        request.Headers.Add(InertiaHeaderNames.Inertia, "true");
        request.Headers.Add(InertiaHeaderNames.Version, _version);

        if (!string.IsNullOrEmpty(_only))
        {
            request.Headers.Add(InertiaHeaderNames.PartialComponent, _component);
            request.Headers.Add(InertiaHeaderNames.PartialOnly, _only);
        }

        if (!string.IsNullOrEmpty(_except))
        {
            request.Headers.Add(InertiaHeaderNames.PartialComponent, _component);
            request.Headers.Add(InertiaHeaderNames.PartialExcept, _except);
        }

        return await _httpClient.SendAsync(request);
    }
}
