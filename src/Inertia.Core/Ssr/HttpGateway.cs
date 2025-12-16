using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inertia.Core.Ssr;

/// <summary>
/// HTTP-based gateway for server-side rendering.
/// </summary>
public class HttpGateway : IGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InertiaOptions _options;
    private readonly ILogger<HttpGateway> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpGateway"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="options">The Inertia options.</param>
    /// <param name="logger">The logger.</param>
    public HttpGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<InertiaOptions> options,
        ILogger<HttpGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SsrResponse?> DispatchAsync(Dictionary<string, object?> pageData)
    {
        if (!ShouldDispatch())
        {
            return null;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("InertiaSSR");
            
            // Configure a reasonable timeout
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.PostAsJsonAsync(
                $"{_options.Ssr.Url.TrimEnd('/')}/render",
                pageData);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "SSR server returned non-success status code: {StatusCode}",
                    response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<SsrResponseDto>();
            
            if (result?.Head == null || result.Body == null)
            {
                _logger.LogWarning("SSR server returned invalid response format");
                return null;
            }

            return new SsrResponse(
                string.Join("\n", result.Head),
                result.Body);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to SSR server at {Url}", _options.Ssr.Url);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "SSR request timed out");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during SSR dispatch");
            return null;
        }
    }

    /// <summary>
    /// Determines whether SSR dispatch should be attempted.
    /// </summary>
    private bool ShouldDispatch()
    {
        if (!_options.Ssr.Enabled)
        {
            return false;
        }

        // Check if bundle exists if required
        if (_options.Ssr.EnsureBundleExists && !string.IsNullOrEmpty(_options.Ssr.Bundle))
        {
            if (!File.Exists(_options.Ssr.Bundle))
            {
                _logger.LogWarning("SSR bundle not found at {Path}", _options.Ssr.Bundle);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// DTO for deserializing SSR response from the server.
    /// </summary>
    private class SsrResponseDto
    {
        public string[]? Head { get; set; }
        public string? Body { get; set; }
    }
}
