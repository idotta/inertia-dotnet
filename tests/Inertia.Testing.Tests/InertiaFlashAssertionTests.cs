using System.Net;
using System.Text;
using FluentAssertions;
using Inertia.Testing;

namespace Inertia.Testing.Tests;

public class InertiaFlashAssertionTests
{
    private static string BuildPageJson(string? flashJson = null)
    {
        var flash = flashJson is not null ? $""","flash":{flashJson}""" : "";
        return $$"""{"component":"Test","url":"/test","version":"1.0","props":{}{{flash}}}""";
    }

    private static HttpResponseMessage CreateRedirectResponse(string location)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Redirect);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    }

    private static (HttpClient Client, MockHttpMessageHandler Handler) CreateMockClient(
        Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        var client = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost") };
        return (client, mockHandler);
    }

    private static HttpResponseMessage CreateInertiaResponse(string json)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        response.Headers.Add("X-Inertia", "true");
        return response;
    }

    public class WithKeyOnly
    {
        [Fact]
        public async Task AssertInertiaFlash_FollowsRedirectAndAsserts()
        {
            var pageJson = BuildPageJson(flashJson: """{"message":"Hello"}""");
            var (client, _) = CreateMockClient(_ => CreateInertiaResponse(pageJson));
            var redirect = CreateRedirectResponse("/dashboard");

            var result = await redirect.AssertInertiaFlash("message", client);

            result.Should().BeSameAs(redirect);
        }

        [Fact]
        public async Task AssertInertiaFlash_MissingFlashKey_Fails()
        {
            var pageJson = BuildPageJson(flashJson: """{"message":"Hello"}""");
            var (client, _) = CreateMockClient(_ => CreateInertiaResponse(pageJson));
            var redirect = CreateRedirectResponse("/dashboard");

            var act = async () => await redirect.AssertInertiaFlash("other", client);

            await act.Should().ThrowAsync<Exception>().WithMessage("*missing key*");
        }

        [Fact]
        public async Task AssertInertiaFlash_TaskOverload_FollowsRedirectAndAsserts()
        {
            var pageJson = BuildPageJson(flashJson: """{"status":"ok"}""");
            var (client, _) = CreateMockClient(_ => CreateInertiaResponse(pageJson));
            var redirect = CreateRedirectResponse("/dashboard");
            var task = Task.FromResult(redirect);

            var result = await task.AssertInertiaFlash("status", client);

            result.Should().BeSameAs(redirect);
        }
    }

    public class WithKeyAndValue
    {
        [Fact]
        public async Task AssertInertiaFlash_FollowsRedirectAndAsserts()
        {
            var pageJson = BuildPageJson(flashJson: """{"message":"Hello"}""");
            var (client, _) = CreateMockClient(_ => CreateInertiaResponse(pageJson));
            var redirect = CreateRedirectResponse("/dashboard");

            var result = await redirect.AssertInertiaFlash("message", "Hello", client);

            result.Should().BeSameAs(redirect);
        }

        [Fact]
        public async Task AssertInertiaFlash_WrongValue_Fails()
        {
            var pageJson = BuildPageJson(flashJson: """{"message":"Hello"}""");
            var (client, _) = CreateMockClient(_ => CreateInertiaResponse(pageJson));
            var redirect = CreateRedirectResponse("/dashboard");

            var act = async () => await redirect.AssertInertiaFlash("message", "Wrong", client);

            await act.Should().ThrowAsync<Exception>().WithMessage("*does not match*");
        }

        [Fact]
        public async Task AssertInertiaFlash_TaskOverload_FollowsRedirectAndAsserts()
        {
            var pageJson = BuildPageJson(flashJson: """{"status":"ok"}""");
            var (client, _) = CreateMockClient(_ => CreateInertiaResponse(pageJson));
            var redirect = CreateRedirectResponse("/dashboard");
            var task = Task.FromResult(redirect);

            var result = await task.AssertInertiaFlash("status", "ok", client);

            result.Should().BeSameAs(redirect);
        }
    }

    public class NoLocationHeader
    {
        [Fact]
        public async Task AssertInertiaFlash_WithKey_ThrowsInvalidOperationException()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            var (client, _) = CreateMockClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

            var act = async () => await response.AssertInertiaFlash("key", client);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*no Location header*");
        }

        [Fact]
        public async Task AssertInertiaFlash_WithKeyAndValue_ThrowsInvalidOperationException()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            var (client, _) = CreateMockClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

            var act = async () => await response.AssertInertiaFlash("key", "value", client);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*no Location header*");
        }
    }

    public class FollowsCorrectUrl
    {
        [Fact]
        public async Task AssertInertiaFlash_UsesLocationHeaderUrl()
        {
            var pageJson = BuildPageJson(flashJson: """{"flash_key":"flash_value"}""");
            var (client, handler) = CreateMockClient(_ => CreateInertiaResponse(pageJson));
            var redirect = CreateRedirectResponse("/expected-url");

            await redirect.AssertInertiaFlash("flash_key", client);

            handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/expected-url");
        }
    }
}
