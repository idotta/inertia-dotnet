using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Inertia.Tests;

/// <summary>
/// Port of inertia-laravel PropsResolverTest.php (~65 tests).
/// Tests call PropsResolver directly without going through InertiaResponse.
/// </summary>
public class PropsResolverTests
{
    private const string TestComponent = "TestComponent";

    // -- Test Helpers --

    private static DefaultHttpContext MakeHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/";
        return ctx;
    }

    private static DefaultHttpContext MakePartialRequest(string only)
    {
        var ctx = MakeHttpContext();
        ctx.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        ctx.Request.Headers[InertiaHeaderNames.PartialComponent] = TestComponent;
        ctx.Request.Headers[InertiaHeaderNames.PartialOnly] = only;
        return ctx;
    }

    private static DefaultHttpContext MakeInertiaRequest()
    {
        var ctx = MakeHttpContext();
        ctx.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        return ctx;
    }

    private static async Task<PageResult> ResolvePageAsync(
        HttpContext ctx,
        Dictionary<string, object?> props,
        Dictionary<string, object?>? shared = null)
    {
        var resolver = new PropsResolver(ctx, TestComponent, exposeSharedPropKeys: true);
        var (resolvedProps, metadata) = await resolver.ResolveAsync(
            shared ?? new Dictionary<string, object?>(),
            Array.Empty<IInertiaPropertyProvider>(),
            props);
        return new PageResult(resolvedProps, metadata);
    }

    private static IScrollMetadataProvider MakeScrollMetadata() =>
        new ScrollMetadata("page", previousPage: null, nextPage: 2, currentPage: 1);

    private static ScrollProp<Dictionary<string, object?>> MakeScrollProp() =>
        Prop.Scroll(
            (Dictionary<string, object?>)new Dictionary<string, object?>
            {
                ["data"] = new List<Dictionary<string, object?>> { new() { ["id"] = 1 } },
            },
            "data",
            MakeScrollMetadata());

    // Helper record for test assertions
    private record PageResult(Dictionary<string, object?> Props, PageMetadata Metadata)
    {
        public Dictionary<string, object?>? DeferredProps =>
            Metadata.DeferredProps?.ToDictionary(
                kvp => kvp.Key,
                kvp => (object?)kvp.Value.ToList());
        public List<string>? MergeProps => Metadata.MergeProps?.ToList();
        public List<string>? PrependProps => Metadata.PrependProps?.ToList();
        public List<string>? DeepMergeProps => Metadata.DeepMergeProps?.ToList();
        public List<string>? MatchPropsOn => Metadata.MatchPropsOn?.ToList();
        public Dictionary<string, object?>? OnceProps =>
            Metadata.OnceProps?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        public Dictionary<string, object?>? ScrollProps =>
            Metadata.ScrollProps?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    // ========================================================================
    // Group A: Basic Resolution
    // ========================================================================

    [Fact]
    public async Task NestedClosure_IsResolved()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = (Func<Dictionary<string, object?>>)(() => new() { ["user"] = "Jonathan" }),
        });

        ((Dictionary<string, object?>)page.Props["auth"]!)["user"].Should().Be("Jonathan");
    }

    [Fact]
    public async Task NestedClosureInsideArray_IsResolved()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = (Func<string>)(() => "Jonathan"),
            },
        });

        ((Dictionary<string, object?>)page.Props["auth"]!)["user"].Should().Be("Jonathan");
    }

    [Fact]
    public async Task NestedProvidesInertiaProperties_IsResolved()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["0"] = new TestPropertyProvider(new() { ["user"] = "Jonathan", ["role"] = "admin" }),
                ["team"] = "Inertia",
            },
        });

        // IInertiaPropertyProvider resolves with numeric keys
        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["user"].Should().Be("Jonathan");
        auth["role"].Should().Be("admin");
        auth["team"].Should().Be("Inertia");
    }

    [Fact]
    public async Task NestedProvidesInertiaProperties_WithPropTypes()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["0"] = new TestPropertyProvider(new()
                {
                    ["user"] = "Jonathan",
                    ["permissions"] = Prop.Optional<string[]>(() => ["manage-users"]),
                }),
            },
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["user"].Should().Be("Jonathan");
        auth.Should().NotContainKey("permissions");
    }

    [Fact]
    public async Task NestedProvidesInertiaProperties_WithPropTypes_OnPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("auth.permissions"), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["0"] = new TestPropertyProvider(new()
                {
                    ["user"] = "Jonathan",
                    ["permissions"] = Prop.Optional<string[]>(() => ["manage-users"]),
                }),
            },
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth.Should().NotContainKey("user");
        auth["permissions"].Should().BeEquivalentTo(new[] { "manage-users" });
    }

    [Fact]
    public async Task NestedAlwaysProp_IsResolved()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = Prop.Always<string>(() => "Jonathan"),
            },
        });

        ((Dictionary<string, object?>)page.Props["auth"]!)["user"].Should().Be("Jonathan");
    }

    [Fact]
    public async Task NestedMergeProp_IsResolved()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
            },
        });

        ((Dictionary<string, object?>)page.Props["feed"]!)["posts"]
            .Should().BeAssignableTo<IEnumerable<Dictionary<string, object?>>>()
            .Subject.First()["id"].Should().Be(1);
    }

    [Fact]
    public async Task NestedOnceProp_IsResolved_OnInitialLoad()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["config"] = new Dictionary<string, object?>
            {
                ["locale"] = Prop.Once<string>(() => "en"),
            },
        });

        ((Dictionary<string, object?>)page.Props["config"]!)["locale"].Should().Be("en");
    }

    [Fact]
    public async Task NestedOptionalProp_IsExcluded_FromInitialLoad()
    {
        var resolved = false;
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = "Jonathan",
                ["permissions"] = Prop.Optional<string[]>(() =>
                {
                    resolved = true;
                    return ["admin"];
                }),
            },
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["user"].Should().Be("Jonathan");
        auth.Should().NotContainKey("permissions");
        resolved.Should().BeFalse("OptionalProp closure should not be resolved on initial load");
    }

    // ========================================================================
    // Group B: Initial Load Exclusions
    // ========================================================================

    [Fact]
    public async Task NestedDeferProp_IsExcluded_FromInitialLoad()
    {
        var resolved = false;
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = "Jonathan",
                ["notifications"] = Prop.Defer<List<string>>(() =>
                {
                    resolved = true;
                    return [];
                }),
            },
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["user"].Should().Be("Jonathan");
        auth.Should().NotContainKey("notifications");
        resolved.Should().BeFalse("DeferProp closure should not be resolved on initial load");
    }

    [Fact]
    public async Task ExcludedProps_AreNotResolved_OnInitialLoad()
    {
        var optionalResolved = false;
        var deferResolved = false;

        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["name"] = "Jonathan",
            ["permissions"] = Prop.Optional<string[]>(() =>
            {
                optionalResolved = true;
                return ["admin"];
            }),
            ["notifications"] = Prop.Defer<string[]>(() =>
            {
                deferResolved = true;
                return ["You have a new follower"];
            }),
        });

        page.Props["name"].Should().Be("Jonathan");
        page.Props.Should().NotContainKey("permissions");
        page.Props.Should().NotContainKey("notifications");
        optionalResolved.Should().BeFalse();
        deferResolved.Should().BeFalse();
    }

    [Fact]
    public async Task ClosureReturningOptionalProp_IsExcluded_FromInitialLoad()
    {
        var resolved = false;
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = (Func<Dictionary<string, object?>>)(() => new()
            {
                ["user"] = "Jonathan",
                ["permissions"] = Prop.Optional<string[]>(() =>
                {
                    resolved = true;
                    return ["admin"];
                }),
            }),
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["user"].Should().Be("Jonathan");
        auth.Should().NotContainKey("permissions");
        resolved.Should().BeFalse();
    }

    [Fact]
    public async Task ClosureReturningDeferProp_IsExcluded_FromInitialLoad()
    {
        var resolved = false;
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = (Func<Dictionary<string, object?>>)(() => new()
            {
                ["user"] = "Jonathan",
                ["notifications"] = Prop.Defer<List<string>>(() =>
                {
                    resolved = true;
                    return [];
                }),
            }),
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["user"].Should().Be("Jonathan");
        auth.Should().NotContainKey("notifications");
        resolved.Should().BeFalse();
    }

    [Fact]
    public async Task ClosureReturningMergeProp_ResolvesWithMetadata()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["posts"] = (Func<MergeProp<List<Dictionary<string, object?>>>>)(
                () => Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } })),
        });

        page.Props["posts"].Should().BeAssignableTo<IEnumerable<Dictionary<string, object?>>>()
            .Subject.First()["id"].Should().Be(1);
        page.MergeProps.Should().BeEquivalentTo(["posts"]);
    }

    [Fact]
    public async Task ClosureReturningOnceProp_ResolvesWithMetadata()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["locale"] = (Func<OnceProp<string>>)(() => Prop.Once<string>(() => "en")),
        });

        page.Props["locale"].Should().Be("en");
        page.OnceProps.Should().ContainKey("locale");
        var onceData = (Dictionary<string, object?>)page.OnceProps!["locale"]!;
        onceData["prop"].Should().Be("locale");
        onceData["expiresAt"].Should().BeNull();
    }

    [Fact]
    public async Task ClosureReturningDeferProp_CollectsDeferredAndMergeMetadata()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["posts"] = (Func<DeferProp<List<Dictionary<string, object?>>>>)(
                () => Prop.Defer<List<Dictionary<string, object?>>>(() => [new() { ["id"] = 1 }]).Merge()),
        });

        page.Props.Should().NotContainKey("posts");
        page.DeferredProps.Should().ContainKey("default");
        ((List<string>)page.DeferredProps!["default"]!).Should().Contain("posts");
        page.MergeProps.Should().Contain("posts");
    }

    // ========================================================================
    // Group C: Partial Request Filtering
    // ========================================================================

    [Fact]
    public async Task NestedOptionalProp_IsIncluded_OnPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("auth.permissions"), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = "Jonathan",
                ["permissions"] = Prop.Optional<string[]>(() => ["admin"]),
            },
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["permissions"].Should().BeEquivalentTo(new[] { "admin" });
    }

    [Fact]
    public async Task NestedDeferProp_IsIncluded_OnPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("auth.notifications"), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = "Jonathan",
                ["notifications"] = Prop.Defer<string[]>(() => ["new message"]),
            },
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["notifications"].Should().BeEquivalentTo(new[] { "new message" });
    }

    [Fact]
    public async Task NestedAlwaysProp_IsIncluded_OnPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("auth.user"), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = "Jonathan",
                ["errors"] = Prop.Always<Dictionary<string, string>>(() => new() { ["name"] = "required" }),
            },
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["user"].Should().Be("Jonathan");
        ((Dictionary<string, string>)auth["errors"]!)["name"].Should().Be("required");
    }

    [Fact]
    public async Task TopLevelAlwaysProp_IsIncluded_WhenNotRequested()
    {
        var page = await ResolvePageAsync(MakePartialRequest("other"), new()
        {
            ["other"] = "value",
            ["errors"] = Prop.Always<Dictionary<string, string>>(() => new() { ["name"] = "required" }),
        });

        page.Props["other"].Should().Be("value");
        ((Dictionary<string, string>)page.Props["errors"]!)["name"].Should().Be("required");
    }

    [Fact]
    public async Task NestedProp_IsExcluded_ViaExceptHeader()
    {
        var ctx = MakeHttpContext();
        ctx.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        ctx.Request.Headers[InertiaHeaderNames.PartialComponent] = TestComponent;
        ctx.Request.Headers[InertiaHeaderNames.PartialOnly] = "auth";
        ctx.Request.Headers[InertiaHeaderNames.PartialExcept] = "auth.token";

        var page = await ResolvePageAsync(ctx, new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = "Jonathan",
                ["token"] = "secret",
            },
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["user"].Should().Be("Jonathan");
        auth.Should().NotContainKey("token");
    }

    [Fact]
    public async Task PartialRequestForParent_ResolvesAllNestedPropTypes()
    {
        var page = await ResolvePageAsync(MakePartialRequest("dashboard"), new()
        {
            ["dashboard"] = new Dictionary<string, object?>
            {
                ["stats"] = "visible",
                ["feed"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
                ["notifications"] = Prop.Defer<string[]>(() => ["msg"]),
                ["settings"] = Prop.Optional<Dictionary<string, string>>(() => new() { ["theme"] = "dark" }),
                ["locale"] = Prop.Once<string>(() => "en"),
            },
        });

        var dash = (Dictionary<string, object?>)page.Props["dashboard"]!;
        dash["stats"].Should().Be("visible");
        dash["feed"].Should().BeAssignableTo<IEnumerable<Dictionary<string, object?>>>()
            .Subject.First()["id"].Should().Be(1);
        dash["notifications"].Should().BeEquivalentTo(new[] { "msg" });
        ((Dictionary<string, string>)dash["settings"]!)["theme"].Should().Be("dark");
        dash["locale"].Should().Be("en");
        page.MergeProps.Should().BeEquivalentTo(["dashboard.feed"]);
        page.OnceProps.Should().ContainKey("dashboard.locale");
        page.DeferredProps.Should().BeNull();
    }

    // ========================================================================
    // Group D: Metadata Collection
    // ========================================================================

    [Fact]
    public async Task NestedDeferProp_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = "Jonathan",
                ["notifications"] = Prop.Defer<List<string>>(() => []),
            },
        });

        page.DeferredProps.Should().ContainKey("default");
        ((List<string>)page.DeferredProps!["default"]!).Should().Contain("auth.notifications");
    }

    [Fact]
    public async Task NestedDeferProp_MetadataPreservesGroup()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["notifications"] = Prop.Defer<List<string>>(() => [], "sidebar"),
                ["messages"] = Prop.Defer<List<string>>(() => [], "sidebar"),
            },
        });

        page.DeferredProps.Should().ContainKey("sidebar");
        ((List<string>)page.DeferredProps!["sidebar"]!).Should().BeEquivalentTo(
            ["auth.notifications", "auth.messages"]);
    }

    [Fact]
    public async Task ClosureReturningDeferProp_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = (Func<Dictionary<string, object?>>)(() => new()
            {
                ["user"] = "Jonathan",
                ["notifications"] = Prop.Defer<List<string>>(() => [], "alerts"),
            }),
        });

        page.DeferredProps.Should().ContainKey("alerts");
        ((List<string>)page.DeferredProps!["alerts"]!).Should().Contain("auth.notifications");
    }

    [Fact]
    public async Task NestedMergeProp_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
            },
        });

        page.MergeProps.Should().BeEquivalentTo(["feed.posts"]);
    }

    [Fact]
    public async Task NestedPrependMergeProp_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }).Prepend(),
            },
        });

        page.PrependProps.Should().BeEquivalentTo(["feed.posts"]);
    }

    [Fact]
    public async Task NestedDeepMergeProp_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["settings"] = new Dictionary<string, object?>
            {
                ["preferences"] = Prop.DeepMerge(new Dictionary<string, object?> { ["theme"] = "dark" }),
            },
        });

        page.DeepMergeProps.Should().BeEquivalentTo(["settings.preferences"]);
    }

    [Fact]
    public async Task NestedMergeProp_WithNestedPath_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new Dictionary<string, object?> { ["data"] = new List<Dictionary<string, object?>> { new() { ["id"] = 1 } } }).Append("data"),
            },
        });

        page.MergeProps.Should().BeEquivalentTo(["feed.posts.data"]);
    }

    [Fact]
    public async Task NestedMergeProp_WithMatchOn_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }).MatchOn("id").DeepMerge(),
            },
        });

        page.DeepMergeProps.Should().BeEquivalentTo(["feed.posts"]);
        page.MatchPropsOn.Should().BeEquivalentTo(["feed.posts.id"]);
    }

    [Fact]
    public async Task NestedDeferWithMerge_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Defer<List<Dictionary<string, object?>>>(() => [new() { ["id"] = 1 }]).Merge(),
            },
        });

        page.DeferredProps.Should().ContainKey("default");
        ((List<string>)page.DeferredProps!["default"]!).Should().Contain("feed.posts");
        page.MergeProps.Should().Contain("feed.posts");
    }

    [Fact]
    public async Task NestedMergeMetadata_IsCollected_OnExactPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("feed.posts"), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
            },
        });

        var feed = (Dictionary<string, object?>)page.Props["feed"]!;
        feed["posts"].Should().BeAssignableTo<IEnumerable<Dictionary<string, object?>>>()
            .Subject.First()["id"].Should().Be(1);
        page.MergeProps.Should().BeEquivalentTo(["feed.posts"]);
    }

    [Fact]
    public async Task NestedMergeMetadata_IsCollected_WhenParentIsRequested()
    {
        var page = await ResolvePageAsync(MakePartialRequest("feed"), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
            },
        });

        page.MergeProps.Should().BeEquivalentTo(["feed.posts"]);
    }

    [Fact]
    public async Task NestedMergeProp_MetadataIsSuppressed_ByResetHeader()
    {
        var ctx = MakePartialRequest("feed.posts");
        ctx.Request.Headers[InertiaHeaderNames.Reset] = "feed.posts";

        var page = await ResolvePageAsync(ctx, new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
            },
        });

        page.MergeProps.Should().BeNull();
    }

    [Fact]
    public async Task NestedOnceProp_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["config"] = new Dictionary<string, object?>
            {
                ["locale"] = Prop.Once<string>(() => "en"),
            },
        });

        page.OnceProps.Should().ContainKey("config.locale");
        var data = (Dictionary<string, object?>)page.OnceProps!["config.locale"]!;
        data["prop"].Should().Be("config.locale");
        data["expiresAt"].Should().BeNull();
    }

    [Fact]
    public async Task NestedOnceProp_WithCustomKey_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["config"] = new Dictionary<string, object?>
            {
                ["locale"] = Prop.Once<string>(() => "en").As("app-locale"),
            },
        });

        page.OnceProps.Should().ContainKey("app-locale");
        var data = (Dictionary<string, object?>)page.OnceProps!["app-locale"]!;
        data["prop"].Should().Be("config.locale");
    }

    [Fact]
    public async Task NestedOnceProp_IsExcluded_WhenAlreadyLoaded()
    {
        var ctx = MakeInertiaRequest();
        ctx.Request.Headers[InertiaHeaderNames.ExceptOnceProps] = "config.locale";

        var page = await ResolvePageAsync(ctx, new()
        {
            ["config"] = new Dictionary<string, object?>
            {
                ["locale"] = Prop.Once<string>(() => "en"),
                ["timezone"] = "UTC",
            },
        });

        var config = (Dictionary<string, object?>)page.Props["config"]!;
        config["timezone"].Should().Be("UTC");
        config.Should().NotContainKey("locale");
        page.OnceProps.Should().ContainKey("config.locale");
    }

    [Fact]
    public async Task NestedOnceMetadata_IsCollected_OnExactPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("config.locale"), new()
        {
            ["config"] = new Dictionary<string, object?>
            {
                ["locale"] = Prop.Once<string>(() => "en"),
            },
        });

        var config = (Dictionary<string, object?>)page.Props["config"]!;
        config["locale"].Should().Be("en");
        page.OnceProps.Should().ContainKey("config.locale");
    }

    [Fact]
    public async Task NestedOnceMetadata_IsCollected_WhenParentIsRequested()
    {
        var page = await ResolvePageAsync(MakePartialRequest("config"), new()
        {
            ["config"] = new Dictionary<string, object?>
            {
                ["locale"] = Prop.Once<string>(() => "en"),
            },
        });

        page.OnceProps.Should().ContainKey("config.locale");
    }

    [Fact]
    public async Task NestedScrollProp_MetadataIsCollected()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = MakeScrollProp(),
            },
        });

        page.ScrollProps.Should().ContainKey("feed.posts");
        var meta = (Dictionary<string, object?>)page.ScrollProps!["feed.posts"]!;
        meta["pageName"].Should().Be("page");
        meta["previousPage"].Should().BeNull();
        meta["nextPage"].Should().Be(2);
        meta["currentPage"].Should().Be(1);
        meta["reset"].Should().Be(false);
    }

    [Fact]
    public async Task NestedDeferredScrollProp_IsExcluded_FromInitialLoad()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = MakeScrollProp().Defer(),
            },
        });

        var feed = page.Props.ContainsKey("feed") ? (Dictionary<string, object?>)page.Props["feed"]! : null;
        feed?.Should().NotContainKey("posts");
        page.DeferredProps.Should().ContainKey("default");
        ((List<string>)page.DeferredProps!["default"]!).Should().Contain("feed.posts");
    }

    [Fact]
    public async Task NestedScrollProp_IsIncluded_OnPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("feed.posts"), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = MakeScrollProp(),
            },
        });

        var feed = (Dictionary<string, object?>)page.Props["feed"]!;
        feed.Should().ContainKey("posts");
        page.ScrollProps.Should().ContainKey("feed.posts");
    }

    [Fact]
    public async Task NestedScrollProp_ResetFlag_IsSetByResetHeader()
    {
        var ctx = MakeHttpContext();
        ctx.Request.Headers[InertiaHeaderNames.Reset] = "feed.posts";

        var page = await ResolvePageAsync(ctx, new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = MakeScrollProp(),
            },
        });

        var meta = (Dictionary<string, object?>)page.ScrollProps!["feed.posts"]!;
        meta["reset"].Should().Be(true);
    }

    [Fact]
    public async Task NestedDeferOnceProp_SuppressesDeferredMetadata_WhenAlreadyLoaded()
    {
        var ctx = MakeHttpContext();
        ctx.Request.Headers[InertiaHeaderNames.ExceptOnceProps] = "feed.posts";

        var page = await ResolvePageAsync(ctx, new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Defer<List<string>>(() => []).Once(),
            },
        });

        page.DeferredProps.Should().BeNull();
    }

    [Fact]
    public async Task NestedDeferOnceProp_IncludesDeferredMetadata_OnFirstLoad()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Defer<List<string>>(() => []).Once(),
            },
        });

        page.DeferredProps.Should().ContainKey("default");
        page.OnceProps.Should().ContainKey("feed.posts");
    }

    // ========================================================================
    // Group E: Complex Scenarios
    // ========================================================================

    [Fact]
    public async Task NestedPropsOnNonPartialInertiaRequest_BehaveLikeInitialLoad()
    {
        var page = await ResolvePageAsync(MakeInertiaRequest(), new()
        {
            ["dashboard"] = new Dictionary<string, object?>
            {
                ["stats"] = "visible",
                ["feed"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
                ["notifications"] = Prop.Defer<List<string>>(() => []),
                ["settings"] = Prop.Optional<List<string>>(() => []),
            },
        });

        var dash = (Dictionary<string, object?>)page.Props["dashboard"]!;
        dash["stats"].Should().Be("visible");
        dash["feed"].Should().BeAssignableTo<IEnumerable<Dictionary<string, object?>>>()
            .Subject.First()["id"].Should().Be(1);
        dash.Should().NotContainKey("notifications");
        dash.Should().NotContainKey("settings");
        page.MergeProps.Should().BeEquivalentTo(["dashboard.feed"]);
        page.DeferredProps.Should().ContainKey("default");
    }

    [Fact]
    public async Task ExceptHeader_SuppressesNestedMergeMetadata()
    {
        var ctx = MakeHttpContext();
        ctx.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        ctx.Request.Headers[InertiaHeaderNames.PartialComponent] = TestComponent;
        ctx.Request.Headers[InertiaHeaderNames.PartialOnly] = "feed.posts,feed.comments";
        ctx.Request.Headers[InertiaHeaderNames.PartialExcept] = "feed.posts";

        var page = await ResolvePageAsync(ctx, new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
                ["comments"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 2 } }),
            },
        });

        var feed = (Dictionary<string, object?>)page.Props["feed"]!;
        feed.Should().NotContainKey("posts");
        feed["comments"].Should().BeAssignableTo<IEnumerable<Dictionary<string, object?>>>()
            .Subject.First()["id"].Should().Be(2);
        page.MergeProps.Should().BeEquivalentTo(["feed.comments"]);
    }

    [Fact]
    public async Task ExceptHeaderForParent_SuppressesAllNestedMetadata()
    {
        var ctx = MakeHttpContext();
        ctx.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        ctx.Request.Headers[InertiaHeaderNames.PartialComponent] = TestComponent;
        ctx.Request.Headers[InertiaHeaderNames.PartialOnly] = "feed,other";
        ctx.Request.Headers[InertiaHeaderNames.PartialExcept] = "feed";

        var page = await ResolvePageAsync(ctx, new()
        {
            ["feed"] = new Dictionary<string, object?>
            {
                ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
            },
            ["other"] = "value",
        });

        page.Props.Should().NotContainKey("feed");
        page.Props["other"].Should().Be("value");
        page.MergeProps.Should().BeNull();
    }

    [Fact]
    public async Task DeeplyNestedDeferProp_IsExcluded_WithMetadata()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["app"] = new Dictionary<string, object?>
            {
                ["auth"] = new Dictionary<string, object?>
                {
                    ["notifications"] = Prop.Defer<List<string>>(() => [], "alerts"),
                },
            },
        });

        var auth = (Dictionary<string, object?>)((Dictionary<string, object?>)page.Props["app"]!)["auth"]!;
        auth.Should().NotContainKey("notifications");
        page.DeferredProps.Should().ContainKey("alerts");
        ((List<string>)page.DeferredProps!["alerts"]!).Should().Contain("app.auth.notifications");
    }

    [Fact]
    public async Task DeeplyNestedMergeProp_MetadataUsesFullPath()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["app"] = new Dictionary<string, object?>
            {
                ["feed"] = new Dictionary<string, object?>
                {
                    ["posts"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
                },
            },
        });

        page.MergeProps.Should().BeEquivalentTo(["app.feed.posts"]);
    }

    [Fact]
    public async Task DeeplyNestedOptionalProp_IsIncluded_OnPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("app.auth.permissions"), new()
        {
            ["app"] = new Dictionary<string, object?>
            {
                ["auth"] = new Dictionary<string, object?>
                {
                    ["permissions"] = Prop.Optional<string[]>(() => ["admin"]),
                },
            },
        });

        var auth = (Dictionary<string, object?>)((Dictionary<string, object?>)page.Props["app"]!)["auth"]!;
        auth["permissions"].Should().BeEquivalentTo(new[] { "admin" });
    }

    [Fact]
    public async Task MultipleNestedPropTypes_AreHandledTogether()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["dashboard"] = new Dictionary<string, object?>
            {
                ["stats"] = "visible",
                ["feed"] = Prop.Merge(new List<Dictionary<string, object?>> { new() { ["id"] = 1 } }),
                ["notifications"] = Prop.Defer<List<string>>(() => []),
                ["settings"] = Prop.Optional<List<string>>(() => []),
                ["locale"] = Prop.Once<string>(() => "en"),
            },
        });

        var dash = (Dictionary<string, object?>)page.Props["dashboard"]!;
        dash["stats"].Should().Be("visible");
        dash["feed"].Should().BeAssignableTo<IEnumerable<Dictionary<string, object?>>>()
            .Subject.First()["id"].Should().Be(1);
        dash["locale"].Should().Be("en");
        dash.Should().NotContainKey("notifications");
        dash.Should().NotContainKey("settings");
        page.MergeProps.Should().BeEquivalentTo(["dashboard.feed"]);
        page.DeferredProps.Should().ContainKey("default");
        page.OnceProps.Should().ContainKey("dashboard.locale");
    }

    [Fact]
    public async Task DeferredPropsAtMixedDepths_CollectCorrectMetadata()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["foo"] = Prop.Defer<string>(() => "bar"),
            ["nested"] = new Dictionary<string, object?>
            {
                ["a"] = "b",
                ["c"] = Prop.Defer<string>(() => "d"),
            },
        });

        var nested = (Dictionary<string, object?>)page.Props["nested"]!;
        nested["a"].Should().Be("b");
        page.Props.Should().NotContainKey("foo");
        nested.Should().NotContainKey("c");
        page.DeferredProps.Should().ContainKey("default");
        ((List<string>)page.DeferredProps!["default"]!).Should().BeEquivalentTo(["foo", "nested.c"]);
    }

    [Fact]
    public async Task DeferredPropsAtMixedDepths_ResolveOnPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("foo,nested.c"), new()
        {
            ["foo"] = Prop.Defer<string>(() => "bar"),
            ["nested"] = new Dictionary<string, object?>
            {
                ["a"] = "b",
                ["c"] = Prop.Defer<string>(() => "d"),
            },
        });

        page.Props["foo"].Should().Be("bar");
        var nested = (Dictionary<string, object?>)page.Props["nested"]!;
        nested["c"].Should().Be("d");
        nested.Should().NotContainKey("a");
        page.DeferredProps.Should().BeNull();
    }

    [Fact]
    public async Task DeferredPropsInsideClosure_AreExcluded_FromInitialLoad()
    {
        var notificationsResolved = false;
        var rolesResolved = false;

        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = (Func<Dictionary<string, object?>>)(() => new()
            {
                ["user"] = new Dictionary<string, object?> { ["name"] = "Jonathan", ["email"] = "jonathan@example.com" },
                ["notifications"] = Prop.Defer<string[]>(() =>
                {
                    notificationsResolved = true;
                    return ["You have a new follower"];
                }),
                ["roles"] = Prop.Defer<string[]>(() =>
                {
                    rolesResolved = true;
                    return ["admin"];
                }),
            }),
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        ((Dictionary<string, object?>)auth["user"]!)["name"].Should().Be("Jonathan");
        auth.Should().NotContainKey("notifications");
        auth.Should().NotContainKey("roles");
        page.DeferredProps.Should().ContainKey("default");
        ((List<string>)page.DeferredProps!["default"]!).Should().BeEquivalentTo(
            ["auth.notifications", "auth.roles"]);
        notificationsResolved.Should().BeFalse();
        rolesResolved.Should().BeFalse();
    }

    [Fact]
    public async Task DeferredPropsInsideClosure_AreResolved_OnPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("auth.notifications,auth.roles"), new()
        {
            ["auth"] = (Func<Dictionary<string, object?>>)(() => new()
            {
                ["user"] = new Dictionary<string, object?> { ["name"] = "Jonathan" },
                ["notifications"] = Prop.Defer<string[]>(() => ["You have a new follower"]),
                ["roles"] = Prop.Defer<string[]>(() => ["admin"]),
            }),
        });

        var auth = (Dictionary<string, object?>)page.Props["auth"]!;
        auth["notifications"].Should().BeEquivalentTo(new[] { "You have a new follower" });
        auth["roles"].Should().BeEquivalentTo(new[] { "admin" });
    }

    // ========================================================================
    // Group F: Dot-notation Props
    // ========================================================================

    [Fact]
    public async Task DotNotationProp_MergesIntoExistingNestedStructure()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = new Dictionary<string, object?>
                {
                    ["name"] = "Jonathan Reinink",
                    ["email"] = "jonathan@example.com",
                },
            },
            ["auth.user.permissions"] = (Func<string[]>)(() => ["edit-posts", "delete-posts"]),
        });

        var user = (Dictionary<string, object?>)((Dictionary<string, object?>)page.Props["auth"]!)["user"]!;
        user["name"].Should().Be("Jonathan Reinink");
        user["email"].Should().Be("jonathan@example.com");
        user["permissions"].Should().BeEquivalentTo(new[] { "edit-posts", "delete-posts" });
        page.Props.Should().NotContainKey("auth.user.permissions");
    }

    [Fact]
    public async Task DotNotationProp_MergesWhenParentIsAClosure()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = (Func<Dictionary<string, object?>>)(() => new()
            {
                ["user"] = new Dictionary<string, object?>
                {
                    ["name"] = "Jonathan Reinink",
                    ["email"] = "jonathan@example.com",
                },
            }),
            ["auth.user.permissions"] = (Func<string[]>)(() => ["edit-posts", "delete-posts"]),
        });

        var user = (Dictionary<string, object?>)((Dictionary<string, object?>)page.Props["auth"]!)["user"]!;
        user["name"].Should().Be("Jonathan Reinink");
        user["email"].Should().Be("jonathan@example.com");
        user["permissions"].Should().BeEquivalentTo(new[] { "edit-posts", "delete-posts" });
    }

    [Fact]
    public async Task DotNotationOptionalProp_IsExcluded_FromInitialLoad()
    {
        var page = await ResolvePageAsync(MakeHttpContext(), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = new Dictionary<string, object?>
                {
                    ["name"] = "Jonathan Reinink",
                    ["email"] = "jonathan@example.com",
                },
            },
            ["auth.user.permissions"] = Prop.Optional<string[]>(() => ["edit-posts", "delete-posts"]),
        });

        var user = (Dictionary<string, object?>)((Dictionary<string, object?>)page.Props["auth"]!)["user"]!;
        user["name"].Should().Be("Jonathan Reinink");
        user["email"].Should().Be("jonathan@example.com");
        user.Should().NotContainKey("permissions");
        page.Props.Should().NotContainKey("auth.user.permissions");
    }

    [Fact]
    public async Task DotNotationOptionalProp_IsIncluded_OnPartialRequest()
    {
        var page = await ResolvePageAsync(MakePartialRequest("auth.user.permissions"), new()
        {
            ["auth"] = new Dictionary<string, object?>
            {
                ["user"] = new Dictionary<string, object?>
                {
                    ["name"] = "Jonathan Reinink",
                    ["email"] = "jonathan@example.com",
                },
            },
            ["auth.user.permissions"] = Prop.Optional<string[]>(() => ["edit-posts", "delete-posts"]),
        });

        var user = (Dictionary<string, object?>)((Dictionary<string, object?>)page.Props["auth"]!)["user"]!;
        user["permissions"].Should().BeEquivalentTo(new[] { "edit-posts", "delete-posts" });
        page.Props.Should().NotContainKey("auth.user.permissions");
    }

    // ========================================================================
    // Test helpers
    // ========================================================================

    // Asserts a value is a list of dicts where the first item has the given id
    private static void AssertListWithId(object? value, int id)
    {
        var list = value.Should().BeAssignableTo<IEnumerable<Dictionary<string, object?>>>().Subject.ToList();
        list.Should().HaveCount(1);
        list[0]["id"].Should().Be(id);
    }

    private class TestPropertyProvider(Dictionary<string, object?> props) : IInertiaPropertyProvider
    {
        public IEnumerable<KeyValuePair<string, object?>> ToInertiaProperties(RenderContext context) => props;
    }
}
