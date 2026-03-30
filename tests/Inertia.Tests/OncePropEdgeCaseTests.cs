using System.Text.Json;
using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Inertia.Tests;

public class OncePropEdgeCaseTests
{
    private static InertiaResponse CreateResponse(
        IDictionary<string, object?> props,
        IDictionary<string, object?>? sharedProps = null,
        IReadOnlyList<IInertiaPropertyProvider>? sharedProviders = null)
    {
        return new InertiaResponse(
            component: "Test/Component",
            props: props,
            sharedProps: sharedProps ?? new Dictionary<string, object?>(),
            sharedProviders: sharedProviders ?? [],
            rootView: "~/Views/App.cshtml",
            version: "1.0",
            encryptHistory: false,
            clearHistory: false,
            preserveFragment: false,
            flash: null,
            exposeSharedPropKeys: true,
            jsonOptions: null);
    }

    private static (DefaultHttpContext Context, MemoryStream Body) CreateInertiaHttpContext(
        string? partialComponent = null,
        string[]? only = null,
        string[]? except = null,
        string[]? exceptOnceProps = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        context.Request.Path = "/test";
        if (partialComponent is not null)
        {
            context.Request.Headers[InertiaHeaderNames.PartialComponent] = partialComponent;
            if (only is not null)
                context.Request.Headers[InertiaHeaderNames.PartialOnly] = string.Join(",", only);
            if (except is not null)
                context.Request.Headers[InertiaHeaderNames.PartialExcept] = string.Join(",", except);
        }
        if (exceptOnceProps is not null)
            context.Request.Headers[InertiaHeaderNames.ExceptOnceProps] = string.Join(",", exceptOnceProps);
        var body = new MemoryStream();
        context.Response.Body = body;
        return (context, body);
    }

    private static async Task<JsonElement> GetPageJson(InertiaResponse response, DefaultHttpContext context, MemoryStream body)
    {
        await response.ExecuteAsync(context);
        body.Position = 0;
        using var reader = new StreamReader(body);
        var json = await reader.ReadToEndAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    public class FreshPropOverride
    {
        [Fact]
        public async Task OncePropFresh_OverridesExceptOncePropsHeader()
        {
            var response = CreateResponse(new Dictionary<string, object?>
            {
                ["settings"] = Prop.Once(() => "fresh-value").Fresh(),
                ["other"] = "constant",
            });
            // Request says "settings" was already loaded, but Fresh() overrides
            var (context, body) = CreateInertiaHttpContext(exceptOnceProps: ["settings"]);

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").GetProperty("settings").GetString().Should().Be("fresh-value");
        }

        [Fact]
        public async Task OncePropFresh_OnInitialLoad_ResolvedAndInMetadata()
        {
            var response = CreateResponse(new Dictionary<string, object?>
            {
                ["settings"] = Prop.Once(() => "fresh-value").Fresh(),
            });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").GetProperty("settings").GetString().Should().Be("fresh-value");
            page.GetProperty("onceProps").TryGetProperty("settings", out _).Should().BeTrue();
        }
    }

    public class OncePropsOnPartial
    {
        [Fact]
        public async Task OnceProp_NotResolvedOnFullVisit_WhenInExceptOnceHeaders()
        {
            var response = CreateResponse(new Dictionary<string, object?>
            {
                ["settings"] = Prop.Once(() => "value"),
                ["name"] = "John",
            });
            // Non-partial Inertia request — settings already loaded by client
            var (context, body) = CreateInertiaHttpContext(
                exceptOnceProps: ["settings"]);

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").TryGetProperty("settings", out _).Should().BeFalse();
            page.GetProperty("props").GetProperty("name").GetString().Should().Be("John");
        }

        [Fact]
        public async Task OnceProp_ResolvedOnPartial_WhenInOnlyHeaders()
        {
            var response = CreateResponse(new Dictionary<string, object?>
            {
                ["settings"] = Prop.Once(() => "once-value"),
                ["name"] = "John",
            });
            // Partial request that specifically asks for "settings"
            var (context, body) = CreateInertiaHttpContext(
                partialComponent: "Test/Component",
                only: ["settings"]);

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").GetProperty("settings").GetString().Should().Be("once-value");
        }
    }
}
