using System.Text.Json;
using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Inertia.Tests;

public class InertiaResponseIntegrationTests
{
    private static InertiaResponse CreateResponse(
        string component = "Test/Component",
        IDictionary<string, object?>? props = null,
        IDictionary<string, object?>? sharedProps = null,
        IReadOnlyList<IInertiaPropertyProvider>? sharedProviders = null,
        string rootView = "~/Views/App.cshtml",
        string version = "1.0",
        bool encryptHistory = false,
        bool clearHistory = false,
        bool preserveFragment = false,
        IDictionary<string, object?>? flash = null,
        bool exposeSharedPropKeys = true)
    {
        return new InertiaResponse(
            component: component,
            props: props ?? new Dictionary<string, object?>(),
            sharedProps: sharedProps ?? new Dictionary<string, object?>(),
            sharedProviders: sharedProviders ?? [],
            rootView: rootView,
            version: version,
            encryptHistory: encryptHistory,
            clearHistory: clearHistory,
            preserveFragment: preserveFragment,
            flash: flash,
            exposeSharedPropKeys: exposeSharedPropKeys,
            jsonOptions: null);
    }

    private static (DefaultHttpContext Context, MemoryStream Body) CreateInertiaHttpContext(
        string path = "/test",
        string? queryString = null,
        string? pathBase = null,
        string? partialComponent = null,
        string[]? only = null,
        string[]? except = null,
        string[]? exceptOnceProps = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        context.Request.Path = path;
        if (queryString is not null)
            context.Request.QueryString = new QueryString(queryString);
        if (pathBase is not null)
            context.Request.PathBase = pathBase;
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

    public class DeferredProps
    {
        [Fact]
        public async Task Render_DeferProp_InitialLoad_ExcludesFromPropsAndSetsMetadata()
        {
            var response = CreateResponse(props: new Dictionary<string, object?>
            {
                ["user"] = new Dictionary<string, object?> { ["name"] = "John" },
                ["foo"] = Prop.Defer(() => "bar"),
            });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").TryGetProperty("foo", out _).Should().BeFalse();
            page.GetProperty("props").GetProperty("user").GetProperty("name").GetString().Should().Be("John");
            var deferred = page.GetProperty("deferredProps").GetProperty("default");
            deferred.EnumerateArray().Select(e => e.GetString()).Should().Contain("foo");
        }

        [Fact]
        public async Task Render_MultipleDeferGroups_GroupsInMetadata()
        {
            var response = CreateResponse(props: new Dictionary<string, object?>
            {
                ["foo"] = Prop.Defer(() => "a"),
                ["bar"] = Prop.Defer(() => "b"),
                ["baz"] = Prop.Defer(() => "c", group: "custom"),
            });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            var deferred = page.GetProperty("deferredProps");
            deferred.GetProperty("default").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("foo").And.Contain("bar");
            deferred.GetProperty("custom").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("baz");
        }

        [Fact]
        public async Task Render_DeferMergeProp_BothMetadataPresent()
        {
            var response = CreateResponse(props: new Dictionary<string, object?>
            {
                ["foo"] = Prop.Defer(() => "bar").Merge(),
            });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("deferredProps").GetProperty("default")
                .EnumerateArray().Select(e => e.GetString()).Should().Contain("foo");
            page.GetProperty("mergeProps").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("foo");
        }
    }

    public class MergeProps
    {
        [Fact]
        public async Task Render_MergeProps_SetsMetadata()
        {
            var response = CreateResponse(props: new Dictionary<string, object?>
            {
                ["items"] = Prop.Merge(new[] { 1, 2, 3 }),
            });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("mergeProps").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("items");
        }

        [Fact]
        public async Task Render_PrependMergeProps_SetsPrependMetadata()
        {
            var response = CreateResponse(props: new Dictionary<string, object?>
            {
                ["items"] = Prop.Merge(new[] { 1, 2, 3 }).Prepend(),
            });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("prependProps").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("items");
        }

        [Fact]
        public async Task Render_DeepMergeProps_SetsDeepMergeMetadata()
        {
            var response = CreateResponse(props: new Dictionary<string, object?>
            {
                ["settings"] = Prop.DeepMerge(new Dictionary<string, object?> { ["theme"] = "dark" }),
            });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("deepMergeProps").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("settings");
        }
    }

    public class OnceProps
    {
        [Fact]
        public async Task Render_OnceProp_InitialLoad_IncludesValueAndMetadata()
        {
            var response = CreateResponse(props: new Dictionary<string, object?>
            {
                ["settings"] = Prop.Once(() => new Dictionary<string, object?> { ["theme"] = "dark" }),
            });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").GetProperty("settings").GetProperty("theme").GetString().Should().Be("dark");
            page.GetProperty("onceProps").TryGetProperty("settings", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Render_OncePropFresh_IncludedEvenWhenAlreadyLoaded()
        {
            var response = CreateResponse(props: new Dictionary<string, object?>
            {
                ["settings"] = Prop.Once(() => "value").Fresh(),
            });
            var (context, body) = CreateInertiaHttpContext(exceptOnceProps: ["settings"]);

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").GetProperty("settings").GetString().Should().Be("value");
        }

        [Fact]
        public async Task Render_OnceProp_AlreadyLoaded_ExcludedFromProps()
        {
            var response = CreateResponse(props: new Dictionary<string, object?>
            {
                ["settings"] = Prop.Once(() => "value"),
                ["name"] = "John",
            });
            var (context, body) = CreateInertiaHttpContext(exceptOnceProps: ["settings"]);

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").TryGetProperty("settings", out _).Should().BeFalse();
            page.GetProperty("props").GetProperty("name").GetString().Should().Be("John");
        }
    }

    public class UrlHandling
    {
        [Fact]
        public async Task Url_WithTrailingSlash_Preserved()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext(path: "/users/");

            var page = await GetPageJson(response, context, body);

            page.GetProperty("url").GetString().Should().Be("/users/");
        }

        [Fact]
        public async Task Url_WithQueryParameters_Preserved()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext(path: "/users", queryString: "?page=2&sort=name");

            var page = await GetPageJson(response, context, body);

            page.GetProperty("url").GetString().Should().Be("/users?page=2&sort=name");
        }

        [Fact]
        public async Task Url_WithoutTrailingSlash_NotModified()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext(path: "/users");

            var page = await GetPageJson(response, context, body);

            page.GetProperty("url").GetString().Should().Be("/users");
        }

        [Fact]
        public async Task Url_PathBase_IncludedInUrl()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext(path: "/users", pathBase: "/app");

            var page = await GetPageJson(response, context, body);

            page.GetProperty("url").GetString().Should().Be("/app/users");
        }
    }

    public class HistoryFlags
    {
        [Fact]
        public async Task Render_Default_OmitsEncryptAndClearHistory()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.TryGetProperty("encryptHistory", out _).Should().BeFalse();
            page.TryGetProperty("clearHistory", out _).Should().BeFalse();
            page.TryGetProperty("preserveFragment", out _).Should().BeFalse();
        }

        [Fact]
        public async Task Render_WithEncryptHistory_IncludesInJson()
        {
            var response = CreateResponse(encryptHistory: true);
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Render_WithClearHistory_IncludesInJson()
        {
            var response = CreateResponse(clearHistory: true);
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("clearHistory").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Render_WithPreserveFragment_IncludesInJson()
        {
            var response = CreateResponse(preserveFragment: true);
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("preserveFragment").GetBoolean().Should().BeTrue();
        }
    }

    public class SharedPropsMerging
    {
        [Fact]
        public async Task SharedProps_PageOverridesShared()
        {
            var response = CreateResponse(
                props: new Dictionary<string, object?> { ["key"] = "page" },
                sharedProps: new Dictionary<string, object?> { ["key"] = "shared" });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").GetProperty("key").GetString().Should().Be("page");
        }

        [Fact]
        public async Task SharedProps_CallbacksResolved()
        {
            var response = CreateResponse(
                sharedProps: new Dictionary<string, object?>
                {
                    ["auth"] = new Func<Dictionary<string, object?>>(() =>
                        new Dictionary<string, object?> { ["user"] = "Jonathan" }),
                });
            var (context, body) = CreateInertiaHttpContext();

            var page = await GetPageJson(response, context, body);

            page.GetProperty("props").GetProperty("auth").GetProperty("user").GetString().Should().Be("Jonathan");
        }
    }
}
