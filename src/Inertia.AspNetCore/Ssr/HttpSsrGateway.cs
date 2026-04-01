using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore;

/// <summary>
/// HTTP-based SSR gateway. Posts page data to an external SSR server (typically Node.js)
/// and parses the rendered HTML response.
/// </summary>
internal sealed class HttpSsrGateway : ISsrGateway
{
    private readonly HttpClient _httpClient;
    private readonly InertiaOptions _options;
    private readonly SsrBundleDetector _bundleDetector;
    private readonly ILogger<HttpSsrGateway> _logger;

    /// <summary>The named HttpClient identifier for DI registration.</summary>
    internal const string HttpClientName = "InertiaSSR";

    public HttpSsrGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<InertiaOptions> options,
        SsrBundleDetector bundleDetector,
        ILogger<HttpSsrGateway> logger)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientName);
        _options = options.Value;
        _bundleDetector = bundleDetector;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SsrResponse?> DispatchAsync(InertiaPage page, CancellationToken cancellationToken = default)
    {
        if (!_options.SsrEnabled)
            return null;

        var hotUrl = _options.HotFileResolver?.Invoke();
        var isHot = hotUrl is not null;

        if (!isHot && _options.SsrEnsureBundleExists && _bundleDetector.Detect() is null)
            return null;

        var url = isHot
            ? $"{hotUrl!.TrimEnd('/')}/__inertia_ssr"
            : $"{_options.SsrUrl.TrimEnd('/')}/render";
        var json = page.ToJson(_options.JsonSerializerOptions);

        try
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                HandleFailure(page, errorJson, null);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ParseResponse(responseJson);
        }
        catch (Exception ex) when (ex is not SsrException)
        {
            HandleFailure(page, null, ex);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.SsrUrl.TrimEnd('/')}/health";
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static SsrResponse? ParseResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var body = root.TryGetProperty("body", out var bodyEl)
                ? bodyEl.GetString() ?? ""
                : "";

            var head = "";
            if (root.TryGetProperty("head", out var headEl) && headEl.ValueKind == JsonValueKind.Array)
            {
                head = string.Join("\n", headEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()));
            }

            return new SsrResponse(head, body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void HandleFailure(InertiaPage page, string? errorJson, Exception? exception)
    {
        var (error, errorType, hint, browserApi, stack, sourceLocation) = ParseError(errorJson, exception);

        if (_options.OnSsrRenderFailed is { } callback)
        {
            try
            {
                callback(new SsrRenderFailedContext
                {
                    Page = page,
                    Error = error,
                    ErrorType = errorType,
                    Hint = hint,
                    BrowserApi = browserApi,
                    Stack = stack,
                    SourceLocation = sourceLocation,
                    Exception = exception,
                });
            }
            catch (Exception callbackEx)
            {
                _logger.LogError(callbackEx, "OnSsrRenderFailed callback threw an exception");
            }
        }

        _logger.LogWarning(
            "SSR render failed for component [{Component}]: {Error} (type: {ErrorType})",
            page.Component, error, errorType);

        if (_options.SsrThrowOnError)
        {
            throw SsrException.Create(
                page.Component, error, errorType, hint, browserApi, stack, sourceLocation, exception);
        }
    }

    private static (string Error, SsrErrorType Type, string? Hint, string? BrowserApi, string? Stack, string? SourceLocation)
        ParseError(string? errorJson, Exception? exception)
    {
        if (errorJson is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(errorJson);
                var root = doc.RootElement;
                return (
                    root.TryGetProperty("error", out var e) ? e.GetString() ?? "Unknown SSR error" : "Unknown SSR error",
                    SsrErrorTypeParser.FromString(root.TryGetProperty("type", out var t) ? t.GetString() : null),
                    root.TryGetProperty("hint", out var h) ? h.GetString() : null,
                    root.TryGetProperty("browserApi", out var b) ? b.GetString() : null,
                    root.TryGetProperty("stack", out var st) ? st.GetString() : null,
                    root.TryGetProperty("sourceLocation", out var s) ? s.GetString() : null
                );
            }
            catch (JsonException) { }
        }

        return (
            exception?.Message ?? "Unknown SSR error",
            exception is HttpRequestException ? SsrErrorType.Connection : SsrErrorType.Unknown,
            null,
            null,
            null,
            null
        );
    }
}
