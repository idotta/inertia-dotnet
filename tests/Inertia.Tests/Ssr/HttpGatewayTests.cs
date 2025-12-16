using System.Net;
using System.Text;
using System.Text.Json;
using Inertia.Core;
using Inertia.Core.Ssr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Inertia.Tests.Ssr;

public class HttpGatewayTests
{
    private readonly InertiaOptions _options;
    private readonly Mock<ILogger<HttpGateway>> _mockLogger;

    public HttpGatewayTests()
    {
        _options = new InertiaOptions
        {
            Ssr = new SsrOptions
            {
                Enabled = true,
                Url = "http://localhost:13714"
            }
        };
        _mockLogger = new Mock<ILogger<HttpGateway>>();
    }

    private IHttpClientFactory CreateMockHttpClientFactory(HttpMessageHandler handler)
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient(handler);
        mockFactory.Setup(f => f.CreateClient("InertiaSSR")).Returns(httpClient);
        return mockFactory.Object;
    }

    [Fact]
    public async Task DispatchAsync_WithSsrDisabled_ReturnsNull()
    {
        // Arrange
        _options.Ssr.Enabled = false;
        var mockHandler = new Mock<HttpMessageHandler>();
        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

        // Act
        var result = await gateway.DispatchAsync(pageData);

        // Assert
        Assert.Null(result);
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithSuccessfulResponse_ReturnsSsrResponse()
    {
        // Arrange
        var responseContent = new
        {
            head = new[] { "<title>Test Page</title>", "<meta name=\"test\" content=\"value\">" },
            body = "<div id=\"app\" data-server-rendered=\"true\"><h1>Test</h1></div>"
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(responseContent),
                    Encoding.UTF8,
                    "application/json")
            });

        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

        // Act
        var result = await gateway.DispatchAsync(pageData);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("<title>Test Page</title>", result!.Head);
        Assert.Contains("<meta name=\"test\"", result.Head);
        Assert.Contains("<h1>Test</h1>", result.Body);
    }

    [Fact]
    public async Task DispatchAsync_WithNonSuccessStatusCode_ReturnsNull()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

        // Act
        var result = await gateway.DispatchAsync(pageData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DispatchAsync_WithInvalidResponse_ReturnsNull()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "{ invalid json }",
                    Encoding.UTF8,
                    "application/json")
            });

        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

        // Act
        var result = await gateway.DispatchAsync(pageData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DispatchAsync_WithMissingHeadInResponse_ReturnsNull()
    {
        // Arrange
        var responseContent = new
        {
            body = "<div>Test</div>"
            // head is missing
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(responseContent),
                    Encoding.UTF8,
                    "application/json")
            });

        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

        // Act
        var result = await gateway.DispatchAsync(pageData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DispatchAsync_WithHttpRequestException_ReturnsNull()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

        // Act
        var result = await gateway.DispatchAsync(pageData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DispatchAsync_WithTimeout_ReturnsNull()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

        // Act
        var result = await gateway.DispatchAsync(pageData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DispatchAsync_SendsCorrectUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        head = new[] { "<title>Test</title>" },
                        body = "<div>Test</div>"
                    }),
                    Encoding.UTF8,
                    "application/json")
            });

        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

        // Act
        await gateway.DispatchAsync(pageData);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("http://localhost:13714/render", capturedRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
    }

    [Fact]
    public async Task DispatchAsync_SendsPageDataAsJson()
    {
        // Arrange
        string? capturedContent = null;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                capturedContent = await req.Content!.ReadAsStringAsync(ct);
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        head = new[] { "<title>Test</title>" },
                        body = "<div>Test</div>"
                    }),
                    Encoding.UTF8,
                    "application/json")
            });

        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Users/Index",
            ["props"] = new Dictionary<string, object?> { ["userId"] = 42 },
            ["url"] = "/users",
            ["version"] = "abc123"
        };

        // Act
        await gateway.DispatchAsync(pageData);

        // Assert
        Assert.NotNull(capturedContent);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(capturedContent!);
        Assert.NotNull(parsed);
        Assert.Equal("Users/Index", parsed!["component"].GetString());
    }

    [Fact]
    public async Task DispatchAsync_WithBundlePathAndFileExists_DispatchesRequest()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            _options.Ssr.Bundle = tempFile;
            _options.Ssr.EnsureBundleExists = true;

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            head = new[] { "<title>Test</title>" },
                            body = "<div>Test</div>"
                        }),
                        Encoding.UTF8,
                        "application/json")
                });

            var factory = CreateMockHttpClientFactory(mockHandler.Object);
            var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

            var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

            // Act
            var result = await gateway.DispatchAsync(pageData);

            // Assert
            Assert.NotNull(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DispatchAsync_WithBundlePathAndFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        _options.Ssr.Bundle = "/non/existent/path/ssr.mjs";
        _options.Ssr.EnsureBundleExists = true;

        var mockHandler = new Mock<HttpMessageHandler>();
        var factory = CreateMockHttpClientFactory(mockHandler.Object);
        var gateway = new HttpGateway(factory, Options.Create(_options), _mockLogger.Object);

        var pageData = new Dictionary<string, object?> { ["component"] = "Test" };

        // Act
        var result = await gateway.DispatchAsync(pageData);

        // Assert
        Assert.Null(result);
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}
