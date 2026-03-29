using System.Net;
using System.Text;
using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Inertia.Tests.Ssr;

public class HttpSsrGatewayTests
{
    /// <summary>Captures log entries for assertion in tests.</summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    /// <summary>Test HTTP handler that captures requests and returns a configurable response.</summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public int CallCount { get; private set; }

        public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public MockHttpMessageHandler(HttpResponseMessage response)
            : this(_ => Task.FromResult(response))
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return await _handler(request);
        }
    }

    private static InertiaPage CreatePage(string component = "Users/Index") => new()
    {
        Component = component,
        Props = new Dictionary<string, object?> { ["name"] = "Test" },
        Url = "/users",
        Version = "1.0",
    };

    private static (HttpSsrGateway Gateway, MockHttpMessageHandler Handler) CreateGateway(
        Action<InertiaOptions>? configure = null,
        MockHttpMessageHandler? handler = null,
        SsrBundleDetector? bundleDetector = null,
        ILogger<HttpSsrGateway>? logger = null)
    {
        var options = new InertiaOptions();
        configure?.Invoke(options);

        handler ??= new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"head\":[],\"body\":\"\"}", Encoding.UTF8, "application/json"),
        });

        var httpClient = new HttpClient(handler);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(HttpSsrGateway.HttpClientName).Returns(httpClient);

        bundleDetector ??= new SsrBundleDetector(options, _ => true);
        logger ??= NullLogger<HttpSsrGateway>.Instance;

        var gateway = new HttpSsrGateway(
            httpClientFactory, Options.Create(options), bundleDetector, logger);

        return (gateway, handler);
    }

    // ---- Group 1: Dispatch Success ----
    public class DispatchSuccess
    {
        [Fact]
        public async Task DispatchAsync_PostsToRenderEndpoint()
        {
            var (gateway, handler) = CreateGateway();
            var page = CreatePage();

            await gateway.DispatchAsync(page);

            handler.LastRequest!.RequestUri!.ToString().Should().Be("http://127.0.0.1:13714/render");
            handler.LastRequest.Method.Should().Be(HttpMethod.Post);
        }

        [Fact]
        public async Task DispatchAsync_Success_ReturnsSsrResponse()
        {
            var responseJson = """{"head":["<title>Test</title>","<meta name='desc'>"],"body":"<div>App</div>"}""";
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
            var (gateway, _) = CreateGateway(handler: handler);

            var result = await gateway.DispatchAsync(CreatePage());

            result.Should().NotBeNull();
            result!.Head.Should().Be("<title>Test</title>\n<meta name='desc'>");
            result.Body.Should().Be("<div>App</div>");
        }

        [Fact]
        public async Task DispatchAsync_Success_EmptyHead_ReturnsEmptyHeadString()
        {
            var responseJson = """{"head":[],"body":"<div>App</div>"}""";
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
            var (gateway, _) = CreateGateway(handler: handler);

            var result = await gateway.DispatchAsync(CreatePage());

            result.Should().NotBeNull();
            result!.Head.Should().BeEmpty();
        }

        [Fact]
        public async Task DispatchAsync_PostsPageJson()
        {
            var (gateway, handler) = CreateGateway();
            var page = CreatePage("Dashboard");

            await gateway.DispatchAsync(page);

            handler.LastRequestBody.Should().Contain("\"component\":\"Dashboard\"");
            handler.LastRequestBody.Should().Contain("\"url\":\"/users\"");
        }
    }

    // ---- Group 2: Dispatch Skipped ----
    public class DispatchSkipped
    {
        [Fact]
        public async Task DispatchAsync_SsrDisabled_ReturnsNull()
        {
            var (gateway, handler) = CreateGateway(o => o.SsrEnabled = false);

            var result = await gateway.DispatchAsync(CreatePage());

            result.Should().BeNull();
            handler.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task DispatchAsync_BundleNotFound_ReturnsNull()
        {
            var bundleDetector = new SsrBundleDetector(new InertiaOptions(), _ => false);
            var (gateway, handler) = CreateGateway(bundleDetector: bundleDetector);

            var result = await gateway.DispatchAsync(CreatePage());

            result.Should().BeNull();
            handler.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task DispatchAsync_BundleNotFound_EnsureDisabled_Dispatches()
        {
            var bundleDetector = new SsrBundleDetector(new InertiaOptions(), _ => false);
            var (gateway, handler) = CreateGateway(
                o => o.SsrEnsureBundleExists = false,
                bundleDetector: bundleDetector);

            var result = await gateway.DispatchAsync(CreatePage());

            result.Should().NotBeNull();
            handler.CallCount.Should().Be(1);
        }
    }

    // ---- Group 3: Dispatch Failure ----
    public class DispatchFailure
    {
        [Fact]
        public async Task DispatchAsync_HttpError_ReturnsNull()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"error":"server error","type":"render"}"""),
                });
            var (gateway, _) = CreateGateway(handler: handler);

            var result = await gateway.DispatchAsync(CreatePage());

            result.Should().BeNull();
        }

        [Fact]
        public async Task DispatchAsync_HttpError_LogsWarning()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"error":"server error","type":"render"}"""),
                });
            var logger = new CapturingLogger<HttpSsrGateway>();
            var (gateway, _) = CreateGateway(handler: handler, logger: logger);

            await gateway.DispatchAsync(CreatePage());

            logger.Entries.Should().ContainSingle()
                .Which.Level.Should().Be(LogLevel.Warning);
        }

        [Fact]
        public async Task DispatchAsync_HttpError_ThrowOnError_ThrowsSsrException()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"error":"render failed","type":"render"}"""),
                });
            var (gateway, _) = CreateGateway(o => o.SsrThrowOnError = true, handler: handler);

            var act = () => gateway.DispatchAsync(CreatePage());

            var ex = await act.Should().ThrowAsync<SsrException>();
            ex.Which.Component.Should().Be("Users/Index");
            ex.Which.ErrorType.Should().Be(SsrErrorType.Render);
        }

        [Fact]
        public async Task DispatchAsync_ConnectionError_ReturnsNull()
        {
            var handler = new MockHttpMessageHandler(
                _ => throw new HttpRequestException("Connection refused"));
            var (gateway, _) = CreateGateway(handler: handler);

            var result = await gateway.DispatchAsync(CreatePage());

            result.Should().BeNull();
        }

        [Fact]
        public async Task DispatchAsync_ConnectionError_ThrowOnError_ThrowsSsrException()
        {
            var handler = new MockHttpMessageHandler(
                _ => throw new HttpRequestException("Connection refused"));
            var (gateway, _) = CreateGateway(o => o.SsrThrowOnError = true, handler: handler);

            var act = () => gateway.DispatchAsync(CreatePage());

            var ex = await act.Should().ThrowAsync<SsrException>();
            ex.Which.ErrorType.Should().Be(SsrErrorType.Connection);
        }

        [Fact]
        public async Task DispatchAsync_InvalidJson_ReturnsNull()
        {
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json", Encoding.UTF8, "application/json"),
            });
            var (gateway, _) = CreateGateway(handler: handler);

            var result = await gateway.DispatchAsync(CreatePage());

            result.Should().BeNull();
        }
    }

    // ---- Group 4: Error Parsing ----
    public class ErrorParsing
    {
        [Fact]
        public async Task DispatchAsync_ParsesErrorType_FromResponse()
        {
            var handler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        """{"error":"window is not defined","type":"browser-api","hint":"Use typeof window check"}"""),
                });
            var (gateway, _) = CreateGateway(o => o.SsrThrowOnError = true, handler: handler);

            var act = () => gateway.DispatchAsync(CreatePage());

            var ex = await act.Should().ThrowAsync<SsrException>();
            ex.Which.ErrorType.Should().Be(SsrErrorType.BrowserApi);
            ex.Which.Hint.Should().Be("Use typeof window check");
        }

        [Fact]
        public async Task DispatchAsync_ConnectionError_SetsConnectionType()
        {
            var handler = new MockHttpMessageHandler(
                _ => throw new HttpRequestException("Connection refused"));
            var (gateway, _) = CreateGateway(o => o.SsrThrowOnError = true, handler: handler);

            var act = () => gateway.DispatchAsync(CreatePage());

            var ex = await act.Should().ThrowAsync<SsrException>();
            ex.Which.ErrorType.Should().Be(SsrErrorType.Connection);
        }
    }

    // ---- Group 5: Health Check ----
    public class HealthCheck
    {
        [Fact]
        public async Task IsHealthyAsync_Success_ReturnsTrue()
        {
            var handler = new MockHttpMessageHandler(req =>
            {
                if (req.RequestUri!.PathAndQuery == "/health")
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });
            var (gateway, _) = CreateGateway(handler: handler);

            var result = await gateway.IsHealthyAsync();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsHealthyAsync_Error_ReturnsFalse()
        {
            var handler = new MockHttpMessageHandler(
                _ => throw new HttpRequestException("Connection refused"));
            var (gateway, _) = CreateGateway(handler: handler);

            var result = await gateway.IsHealthyAsync();

            result.Should().BeFalse();
        }
    }
}
