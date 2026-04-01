using System.Text.Json;
using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Inertia.Tests;

public class HistoryIntegrationTests
{
    private static (InertiaFactory Factory, DefaultHttpContext HttpContext, ITempDataDictionary TempData) CreateFactory(
        Action<InertiaOptions>? configure = null)
    {
        var options = new InertiaOptions();
        configure?.Invoke(options);
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var tempData = Substitute.For<ITempDataDictionary>();
        var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
        tempDataFactory.GetTempData(httpContext).Returns(tempData);
        var factory = new InertiaFactory(Options.Create(options), accessor, tempDataFactory);
        return (factory, httpContext, tempData);
    }

    private static async Task<JsonElement> RenderAndGetPageJson(
        InertiaFactory factory, DefaultHttpContext httpContext)
    {
        httpContext.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        httpContext.Request.Path = "/test";
        var body = new MemoryStream();
        httpContext.Response.Body = body;

        var response = factory.Render("Test/Component");
        await response.ExecuteAsync(httpContext);

        body.Position = 0;
        using var reader = new StreamReader(body);
        var json = await reader.ReadToEndAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    public class DefaultState
    {
        [Fact]
        public async Task Render_Default_OmitsEncryptHistoryFromJson()
        {
            var (factory, ctx, _) = CreateFactory();

            var page = await RenderAndGetPageJson(factory, ctx);

            page.TryGetProperty("encryptHistory", out _).Should().BeFalse();
        }

        [Fact]
        public async Task Render_Default_OmitsClearHistoryFromJson()
        {
            var (factory, ctx, _) = CreateFactory();

            var page = await RenderAndGetPageJson(factory, ctx);

            page.TryGetProperty("clearHistory", out _).Should().BeFalse();
        }

        [Fact]
        public async Task Render_Default_OmitsPreserveFragmentFromJson()
        {
            var (factory, ctx, _) = CreateFactory();

            var page = await RenderAndGetPageJson(factory, ctx);

            page.TryGetProperty("preserveFragment", out _).Should().BeFalse();
        }
    }

    public class EncryptHistoryTests
    {
        [Fact]
        public async Task EncryptHistory_IncludesInRenderedJson()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.EncryptHistory();

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task EncryptHistory_GlobalConfig_IncludesInJson()
        {
            var (factory, ctx, _) = CreateFactory(o => o.EncryptHistory = true);

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task EncryptHistory_GlobalTrue_PerRequestFalse_OmitsFromJson()
        {
            var (factory, ctx, _) = CreateFactory(o => o.EncryptHistory = true);
            factory.EncryptHistory(false);

            var page = await RenderAndGetPageJson(factory, ctx);

            page.TryGetProperty("encryptHistory", out _).Should().BeFalse();
        }
    }

    public class ClearHistoryTests
    {
        [Fact]
        public async Task ClearHistory_IncludesInRenderedJson()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.ClearHistory();

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("clearHistory").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public void ClearHistory_StoresFlagInTempData()
        {
            var (factory, _, tempData) = CreateFactory();

            factory.ClearHistory();

            tempData.Received()[InertiaSessionKeys.ClearHistory] = "true";
        }
    }

    public class PreserveFragmentTests
    {
        [Fact]
        public async Task PreserveFragment_IncludesInRenderedJson()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.PreserveFragment();

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("preserveFragment").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task PreserveFragment_Default_OmitsFromJson()
        {
            var (factory, ctx, _) = CreateFactory();

            var page = await RenderAndGetPageJson(factory, ctx);

            page.TryGetProperty("preserveFragment", out _).Should().BeFalse();
        }
    }
}
