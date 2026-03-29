using System.Net;
using System.Text;
using FluentAssertions;
using Inertia.AspNetCore;
using Inertia.Testing;

namespace Inertia.Testing.Tests;

public class ReloadRequestTests
{
    private static string BuildPageJson() =>
        """{"component":"Test","url":"/test","version":"1.0","props":{}}""";

    private static (HttpClient Client, MockHttpMessageHandler Handler) CreateMockClient()
    {
        var handler = new MockHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(BuildPageJson(), Encoding.UTF8, "application/json");
            response.Headers.Add("X-Inertia", "true");
            return response;
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return (client, handler);
    }

    [Fact]
    public async Task ExecuteAsync_SetsInertiaHeader()
    {
        var (client, handler) = CreateMockClient();
        var request = new ReloadRequest(client, "/test", "Test", "1.0");

        await request.ExecuteAsync();

        handler.LastRequest!.Headers.GetValues(InertiaHeaderNames.Inertia)
            .Should().Contain("true");
    }

    [Fact]
    public async Task ExecuteAsync_SetsVersionHeader()
    {
        var (client, handler) = CreateMockClient();
        var request = new ReloadRequest(client, "/test", "Test", "abc123");

        await request.ExecuteAsync();

        handler.LastRequest!.Headers.GetValues(InertiaHeaderNames.Version)
            .Should().Contain("abc123");
    }

    [Fact]
    public async Task ExecuteAsync_WithOnly_SetsPartialHeaders()
    {
        var (client, handler) = CreateMockClient();
        var request = new ReloadRequest(client, "/test", "TestComponent", "1.0", only: "name,age");

        await request.ExecuteAsync();

        handler.LastRequest!.Headers.GetValues(InertiaHeaderNames.PartialComponent)
            .Should().Contain("TestComponent");
        handler.LastRequest.Headers.GetValues(InertiaHeaderNames.PartialOnly)
            .Should().Contain("name,age");
    }

    [Fact]
    public async Task ExecuteAsync_WithExcept_SetsExceptHeaders()
    {
        var (client, handler) = CreateMockClient();
        var request = new ReloadRequest(client, "/test", "TestComponent", "1.0", except: "secret");

        await request.ExecuteAsync();

        handler.LastRequest!.Headers.GetValues(InertiaHeaderNames.PartialComponent)
            .Should().Contain("TestComponent");
        handler.LastRequest.Headers.GetValues(InertiaHeaderNames.PartialExcept)
            .Should().Contain("secret");
    }

    [Fact]
    public async Task ExecuteAsync_WithoutPartials_NoPartialHeaders()
    {
        var (client, handler) = CreateMockClient();
        var request = new ReloadRequest(client, "/test", "Test", "1.0");

        await request.ExecuteAsync();

        handler.LastRequest!.Headers.Contains(InertiaHeaderNames.PartialComponent).Should().BeFalse();
        handler.LastRequest.Headers.Contains(InertiaHeaderNames.PartialOnly).Should().BeFalse();
        handler.LastRequest.Headers.Contains(InertiaHeaderNames.PartialExcept).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_UsesGetMethod()
    {
        var (client, handler) = CreateMockClient();
        var request = new ReloadRequest(client, "/test", "Test", "1.0");

        await request.ExecuteAsync();

        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public async Task ExecuteAsync_UsesCorrectUrl()
    {
        var (client, handler) = CreateMockClient();
        var request = new ReloadRequest(client, "/users?page=2", "Test", "1.0");

        await request.ExecuteAsync();

        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/users?page=2");
    }
}

/// <summary>Mock HTTP handler for capturing outgoing requests in tests.</summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public HttpRequestMessage? LastRequest { get; private set; }

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_handler(request));
    }
}
