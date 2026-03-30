using System.Text.Json;
using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Inertia.Tests;

public class InertiaFactoryIntegrationTests
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
        InertiaFactory factory, DefaultHttpContext httpContext,
        string component = "Test/Component",
        IDictionary<string, object?>? props = null)
    {
        httpContext.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        httpContext.Request.Path = "/test";
        var body = new MemoryStream();
        httpContext.Response.Body = body;

        var response = factory.Render(component, props ?? new Dictionary<string, object?>());
        await response.ExecuteAsync(httpContext);

        body.Position = 0;
        using var reader = new StreamReader(body);
        var json = await reader.ReadToEndAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    private sealed class TestPropertyProvider : IInertiaPropertyProvider
    {
        private readonly Dictionary<string, object?> _props;

        public TestPropertyProvider(Dictionary<string, object?> props) => _props = props;

        public IEnumerable<KeyValuePair<string, object?>> ToInertiaProperties(RenderContext context)
            => _props;
    }

    public class DotNotation
    {
        [Fact]
        public async Task Share_DotNotation_UnpacksToNestedObject()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("auth.user", new Dictionary<string, object?> { ["name"] = "Jonathan" });

            var page = await RenderAndGetPageJson(factory, ctx, props: new Dictionary<string, object?>
            {
                ["auth.user.can"] = new Dictionary<string, object?> { ["create_group"] = false },
            });

            var auth = page.GetProperty("props").GetProperty("auth");
            auth.GetProperty("user").GetProperty("name").GetString().Should().Be("Jonathan");
            auth.GetProperty("user").GetProperty("can").GetProperty("create_group").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task Share_DotNotationWithCallback_UnpacksToNestedObject()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("auth.user", new Func<Dictionary<string, object?>>(() =>
                new Dictionary<string, object?> { ["name"] = "Jonathan" }));

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("props").GetProperty("auth").GetProperty("user")
                .GetProperty("name").GetString().Should().Be("Jonathan");
        }

        [Fact]
        public async Task Share_MultipleDotNotation_MergesCorrectly()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("auth.user", new Dictionary<string, object?> { ["name"] = "Jonathan" });
            factory.Share("auth.role", "admin");

            var page = await RenderAndGetPageJson(factory, ctx);

            var auth = page.GetProperty("props").GetProperty("auth");
            auth.GetProperty("user").GetProperty("name").GetString().Should().Be("Jonathan");
            auth.GetProperty("role").GetString().Should().Be("admin");
        }

        [Fact]
        public async Task SharedPropsMetadata_DotNotation_TracksTopLevelKey()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("auth.user", new Dictionary<string, object?> { ["name"] = "Jonathan" });

            var page = await RenderAndGetPageJson(factory, ctx);

            var shared = page.GetProperty("sharedProps");
            shared.EnumerateArray().Select(e => e.GetString()).Should().Contain("auth");
        }
    }

    public class OncePropSharing
    {
        [Fact]
        public async Task Share_OnceProp_IncludesValueAndOnceMetadata()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("settings", Prop.Once(() =>
                new Dictionary<string, object?> { ["theme"] = "dark" }));

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("props").GetProperty("settings").GetProperty("theme").GetString().Should().Be("dark");
            page.GetProperty("onceProps").TryGetProperty("settings", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Share_OncePropWithCustomKey_UsesCustomKeyInOnceMetadata()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("settings", Prop.Once(() => "value").As("app-settings").Until(60));

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("props").GetProperty("settings").GetString().Should().Be("value");
            var onceProps = page.GetProperty("onceProps");
            onceProps.TryGetProperty("app-settings", out var entry).Should().BeTrue();
            entry.GetProperty("prop").GetString().Should().Be("settings");
        }

        [Fact]
        public async Task Share_OncePropFresh_IncludedEvenWhenAlreadyLoaded()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("settings", Prop.Once(() => "value").Fresh());
            ctx.Request.Headers[InertiaHeaderNames.ExceptOnceProps] = "settings";

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("props").GetProperty("settings").GetString().Should().Be("value");
        }
    }

    public class SharedPropsMetadata
    {
        [Fact]
        public async Task SharedPropsMetadata_IncludesKeysFromShare()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("app_name", "My App");

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("sharedProps").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("app_name");
        }

        [Fact]
        public async Task SharedPropsMetadata_IncludesKeysFromProvider()
        {
            var (factory, ctx, _) = CreateFactory();
            var provider = new TestPropertyProvider(new Dictionary<string, object?>
            {
                ["auth"] = "data",
                ["errors"] = new Dictionary<string, object?>(),
            });
            factory.Share(provider);

            var page = await RenderAndGetPageJson(factory, ctx);

            var shared = page.GetProperty("sharedProps").EnumerateArray().Select(e => e.GetString()).ToList();
            shared.Should().Contain("auth");
            shared.Should().Contain("errors");
        }

        [Fact]
        public async Task SharedPropsMetadata_IncludesPageSpecificOverrideKeys()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("auth", (object?)null);

            var page = await RenderAndGetPageJson(factory, ctx, props: new Dictionary<string, object?>
            {
                ["auth"] = new Dictionary<string, object?> { ["user"] = "Jonathan" },
            });

            page.GetProperty("sharedProps").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("auth");
        }

        [Fact]
        public async Task SharedPropsMetadata_DisabledWhenOptionFalse()
        {
            var (factory, ctx, _) = CreateFactory(o => o.ExposeSharedPropKeys = false);
            factory.Share("app_name", "My App");

            var page = await RenderAndGetPageJson(factory, ctx);

            page.TryGetProperty("sharedProps", out _).Should().BeFalse();
        }

        [Fact]
        public async Task SharedPropsMetadata_MultipleShareCalls_AllTracked()
        {
            var (factory, ctx, _) = CreateFactory();
            factory.Share("a", 1);
            factory.Share("b", 2);
            factory.Share("c", 3);

            var page = await RenderAndGetPageJson(factory, ctx);

            var shared = page.GetProperty("sharedProps").EnumerateArray().Select(e => e.GetString()).ToList();
            shared.Should().Contain("a").And.Contain("b").And.Contain("c");
        }
    }

    public class Flash
    {
        [Fact]
        public async Task Flash_IncludedInPageResponse()
        {
            var (factory, ctx, tempData) = CreateFactory();
            var flashJson = JsonSerializer.Serialize(new Dictionary<string, object?> { ["message"] = "Success!" });
            tempData.TryGetValue(InertiaSessionKeys.FlashData, out Arg.Any<object?>()!)
                .Returns(x => { x[1] = flashJson; return true; });
            tempData.ContainsKey(InertiaSessionKeys.FlashData).Returns(true);

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("flash").GetProperty("message").GetString().Should().Be("Success!");
        }

        [Fact]
        public async Task Flash_NotIncludedWhenNoFlashData()
        {
            var (factory, ctx, tempData) = CreateFactory();
            tempData.ContainsKey(InertiaSessionKeys.FlashData).Returns(false);

            var page = await RenderAndGetPageJson(factory, ctx);

            page.TryGetProperty("flash", out _).Should().BeFalse();
        }
    }

    public class PropertyProvider
    {
        [Fact]
        public async Task Render_WithProviderAsObjectProps_ExpandsProperties()
        {
            var (factory, ctx, _) = CreateFactory();
            var provider = new TestPropertyProvider(new Dictionary<string, object?>
            {
                ["auth"] = new Dictionary<string, object?> { ["user"] = "Jonathan" },
                ["settings"] = "dark",
            });

            // Cast to object to exercise the Render(string, object?) overload
            var response = factory.Render("Test/Component", (object)provider);

            ctx.Request.Headers[InertiaHeaderNames.Inertia] = "true";
            ctx.Request.Path = "/test";
            var body = new MemoryStream();
            ctx.Response.Body = body;
            await response.ExecuteAsync(ctx);

            body.Position = 0;
            using var reader = new StreamReader(body);
            var json = await reader.ReadToEndAsync();
            var page = JsonDocument.Parse(json).RootElement;

            page.GetProperty("props").GetProperty("auth").GetProperty("user").GetString().Should().Be("Jonathan");
            page.GetProperty("props").GetProperty("settings").GetString().Should().Be("dark");
        }

        [Fact]
        public async Task Render_WithProviderAsProps_ExpandsProperties()
        {
            var (factory, ctx, _) = CreateFactory();
            var provider = new TestPropertyProvider(new Dictionary<string, object?>
            {
                ["auth"] = new Dictionary<string, object?> { ["user"] = "Jonathan" },
            });

            var page = await RenderAndGetPageJson(factory, ctx, props: new Dictionary<string, object?>
            {
                ["0"] = provider,
                ["extra"] = "value",
            });

            page.GetProperty("props").GetProperty("auth").GetProperty("user").GetString().Should().Be("Jonathan");
            page.GetProperty("props").GetProperty("extra").GetString().Should().Be("value");
        }

        [Fact]
        public async Task Share_WithProvider_ExpandsOnRender()
        {
            var (factory, ctx, _) = CreateFactory();
            var provider = new TestPropertyProvider(new Dictionary<string, object?>
            {
                ["auth"] = new Dictionary<string, object?> { ["user"] = "Jonathan" },
                ["errors"] = new Dictionary<string, object?>(),
            });
            factory.Share(provider);

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("props").GetProperty("auth").GetProperty("user").GetString().Should().Be("Jonathan");
        }

        [Fact]
        public async Task Render_ProviderWithOptionalProp_ExcludedOnInitialLoad()
        {
            var (factory, ctx, _) = CreateFactory();
            var provider = new TestPropertyProvider(new Dictionary<string, object?>
            {
                ["optional_data"] = Prop.Optional(() => "lazy-value"),
                ["always_data"] = "always",
            });
            factory.Share(provider);

            var page = await RenderAndGetPageJson(factory, ctx);

            page.GetProperty("props").TryGetProperty("optional_data", out _).Should().BeFalse();
            page.GetProperty("props").GetProperty("always_data").GetString().Should().Be("always");
        }
    }

    public class ComponentValidation
    {
        [Fact]
        public void Render_EnsurePagesExist_ThrowsComponentNotFoundException()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"inertia-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var (factory, ctx, _) = CreateFactory(o =>
                {
                    o.EnsurePagesExist = true;
                    o.PagePaths = [tempDir];
                });

                var act = () => factory.Render("NonExistent/Component");
                act.Should().Throw<ComponentNotFoundException>();
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Render_EnsurePagesExist_DoesNotThrowForExistingComponent()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"inertia-test-{Guid.NewGuid():N}");
            var componentDir = Path.Combine(tempDir, "Users");
            Directory.CreateDirectory(componentDir);
            File.WriteAllText(Path.Combine(componentDir, "Index.vue"), "");
            try
            {
                var (factory, ctx, _) = CreateFactory(o =>
                {
                    o.EnsurePagesExist = true;
                    o.PagePaths = [tempDir];
                });

                var act = () => factory.Render("Users/Index");
                act.Should().NotThrow();
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Render_EnsurePagesExistDisabled_DoesNotThrow()
        {
            var (factory, _, _) = CreateFactory(o => o.EnsurePagesExist = false);

            var act = () => factory.Render("Anything/At/All");
            act.Should().NotThrow();
        }
    }

    public class PropTypeIntegration
    {
        [Fact]
        public async Task Render_DeferProp_ExcludedFromInitialLoad_InDeferredMetadata()
        {
            var (factory, ctx, _) = CreateFactory();

            var page = await RenderAndGetPageJson(factory, ctx, props: new Dictionary<string, object?>
            {
                ["foo"] = Prop.Defer(() => "bar"),
                ["name"] = "John",
            });

            page.GetProperty("props").TryGetProperty("foo", out _).Should().BeFalse();
            page.GetProperty("props").GetProperty("name").GetString().Should().Be("John");
            page.GetProperty("deferredProps").GetProperty("default")
                .EnumerateArray().Select(e => e.GetString()).Should().Contain("foo");
        }

        [Fact]
        public async Task Render_MergeProp_IncludedWithMergeMetadata()
        {
            var (factory, ctx, _) = CreateFactory();

            var page = await RenderAndGetPageJson(factory, ctx, props: new Dictionary<string, object?>
            {
                ["items"] = Prop.Merge(new[] { 1, 2, 3 }),
            });

            page.GetProperty("props").TryGetProperty("items", out _).Should().BeTrue();
            page.GetProperty("mergeProps").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("items");
        }

        [Fact]
        public async Task Render_DeferMergeProp_BothMetadataPresent()
        {
            var (factory, ctx, _) = CreateFactory();

            var page = await RenderAndGetPageJson(factory, ctx, props: new Dictionary<string, object?>
            {
                ["items"] = Prop.Defer(() => new[] { 1, 2 }).Merge(),
            });

            page.GetProperty("deferredProps").GetProperty("default")
                .EnumerateArray().Select(e => e.GetString()).Should().Contain("items");
            page.GetProperty("mergeProps").EnumerateArray().Select(e => e.GetString())
                .Should().Contain("items");
        }

        [Fact]
        public async Task Render_ScrollProp_IncludesScrollMetadata()
        {
            var (factory, ctx, _) = CreateFactory();

            var page = await RenderAndGetPageJson(factory, ctx, props: new Dictionary<string, object?>
            {
                ["users"] = Prop.Scroll(new[] { "a", "b" }, "data",
                    new ScrollMetadata("page", previousPage: null, nextPage: 2, currentPage: 1)),
            });

            page.GetProperty("props").TryGetProperty("users", out _).Should().BeTrue();
            page.GetProperty("scrollProps").TryGetProperty("users", out _).Should().BeTrue();
        }
    }
}
