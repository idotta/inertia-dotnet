using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Inertia.Tests;

public class InertiaMiddlewareTests
{
    private static readonly RequestDelegate NoOpNext = _ => Task.CompletedTask;

    private static RequestDelegate RedirectNext(int statusCode = 302, string? location = null) => ctx =>
    {
        ctx.Response.StatusCode = statusCode;
        if (location is not null)
            ctx.Response.Headers.Location = location;
        return Task.CompletedTask;
    };

    private static (InertiaMiddleware Middleware, InertiaFactory Factory, DefaultHttpContext HttpContext) CreateMiddleware(
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

        var services = new ServiceCollection();
        services.AddSingleton<IInertia>(factory);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        httpContext.RequestServices = services.BuildServiceProvider();

        var middleware = new InertiaMiddleware(Options.Create(options));
        return (middleware, factory, httpContext);
    }

    private static void SetInertiaHeaders(HttpContext ctx, string? version = null)
    {
        ctx.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        if (version is not null)
            ctx.Request.Headers[InertiaHeaderNames.Version] = version;
    }

    // ---- Group 1: Vary Header ----
    public class VaryHeader
    {
        [Fact]
        public async Task InvokeAsync_SetsVaryHeader_OnAllResponses()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            // Non-Inertia request, no body written -> explicit Vary set after next()
            await middleware.InvokeAsync(ctx, NoOpNext);
            ctx.Response.Headers["Vary"].ToString().Should().Be(InertiaHeaderNames.Inertia);
        }

        [Fact]
        public async Task InvokeAsync_SetsVaryHeader_OnInertiaResponses()
        {
            var (middleware, factory, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx, "v1");
            factory.SetVersion("v1");
            // Inertia request with matching version, no body -> Vary set after next()
            await middleware.InvokeAsync(ctx, NoOpNext);
            ctx.Response.Headers["Vary"].ToString().Should().Be(InertiaHeaderNames.Inertia);
        }

        [Fact]
        public async Task InvokeAsync_SetsVaryHeader_OnVersionMismatch()
        {
            var (middleware, factory, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx, "old");
            factory.SetVersion("new");
            ctx.Request.Method = HttpMethods.Get;

            await middleware.InvokeAsync(ctx, NoOpNext);

            ctx.Response.Headers["Vary"].ToString().Should().Be(InertiaHeaderNames.Inertia);
        }
    }

    // ---- Group 2: Non-Inertia Passthrough ----
    public class NonInertiaPassthrough
    {
        [Fact]
        public async Task InvokeAsync_NonInertiaRequest_CallsNextAndReturns()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            await middleware.InvokeAsync(ctx, next);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_NonInertiaRequest_DoesNotConvert302To303ForPut()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            ctx.Request.Method = HttpMethods.Put;

            await middleware.InvokeAsync(ctx, RedirectNext(302));

            ctx.Response.StatusCode.Should().Be(302);
        }
    }

    // ---- Group 3: Setup Delegates ----
    public class SetupDelegates
    {
        [Fact]
        public async Task InvokeAsync_CallsVersionProvider_AndSetsVersion()
        {
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.VersionProvider = _ => "v42");

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetVersion().Should().Be("v42");
        }

        [Fact]
        public async Task InvokeAsync_NoVersionProvider_VersionRemainsEmpty()
        {
            var (middleware, factory, ctx) = CreateMiddleware();

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetVersion().Should().BeEmpty();
        }

        [Fact]
        public async Task InvokeAsync_CallsSharedPropsProvider_AndSharesProps()
        {
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.SharedPropsProvider = (_, _) => new Dictionary<string, object?>
                {
                    ["auth"] = "user1",
                    ["flash"] = "msg"
                });

            await middleware.InvokeAsync(ctx, NoOpNext);

            var shared = factory.GetShared();
            shared.Should().ContainKey("auth").WhoseValue.Should().Be("user1");
            shared.Should().ContainKey("flash").WhoseValue.Should().Be("msg");
        }

        [Fact]
        public async Task InvokeAsync_NoSharedPropsProvider_NoSharedProps()
        {
            var (middleware, factory, ctx) = CreateMiddleware();

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetShared().Should().BeEmpty();
        }

        [Fact]
        public async Task InvokeAsync_CallsRootViewProvider_AndSetsRootView()
        {
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.RootViewProvider = _ => "~/Views/Custom.cshtml");

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetRootView().Should().Be("~/Views/Custom.cshtml");
        }

        [Fact]
        public async Task InvokeAsync_NoRootViewProvider_UsesDefaultRootView()
        {
            var (middleware, factory, ctx) = CreateMiddleware();

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetRootView().Should().Be("~/Views/App.cshtml");
        }
    }

    // ---- Group 4: Version Mismatch ----
    public class VersionMismatch
    {
        [Fact]
        public async Task InvokeAsync_VersionMatch_CallsNext()
        {
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.VersionProvider = _ => "v1");
            SetInertiaHeaders(ctx, "v1");
            ctx.Request.Method = HttpMethods.Get;
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            await middleware.InvokeAsync(ctx, next);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_VersionMismatch_OnGet_Returns409WithLocation()
        {
            var (middleware, _, ctx) = CreateMiddleware(o =>
                o.VersionProvider = _ => "v2");
            SetInertiaHeaders(ctx, "v1");
            ctx.Request.Method = HttpMethods.Get;
            ctx.Request.Path = "/users";
            ctx.Request.QueryString = new QueryString("?page=1");

            await middleware.InvokeAsync(ctx, NoOpNext);

            ctx.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            ctx.Response.Headers[InertiaHeaderNames.Location].ToString().Should().Be("/users?page=1");
        }

        [Fact]
        public async Task InvokeAsync_VersionMismatch_OnGet_DoesNotCallNext()
        {
            var (middleware, _, ctx) = CreateMiddleware(o =>
                o.VersionProvider = _ => "v2");
            SetInertiaHeaders(ctx, "v1");
            ctx.Request.Method = HttpMethods.Get;
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            await middleware.InvokeAsync(ctx, next);

            nextCalled.Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_VersionMismatch_OnPost_DoesNotCheck()
        {
            var (middleware, _, ctx) = CreateMiddleware(o =>
                o.VersionProvider = _ => "v2");
            SetInertiaHeaders(ctx, "v1");
            ctx.Request.Method = HttpMethods.Post;
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            await middleware.InvokeAsync(ctx, next);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_VersionMismatch_CustomDelegate_CallsDelegate()
        {
            var delegateCalled = false;
            var (middleware, _, ctx) = CreateMiddleware(o =>
            {
                o.VersionProvider = _ => "v2";
                o.OnVersionChange = _ =>
                {
                    delegateCalled = true;
                    return Microsoft.AspNetCore.Http.Results.StatusCode(418);
                };
            });
            SetInertiaHeaders(ctx, "v1");
            ctx.Request.Method = HttpMethods.Get;

            await middleware.InvokeAsync(ctx, NoOpNext);

            delegateCalled.Should().BeTrue();
            ctx.Response.StatusCode.Should().Be(418);
        }
    }

    // ---- Group 5: Empty Response ----
    public class EmptyResponse
    {
        [Fact]
        public async Task InvokeAsync_EmptyInertiaResponse_DefaultRedirectsBack()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Headers.Referer = "https://example.com/previous";

            await middleware.InvokeAsync(ctx, NoOpNext);

            ctx.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
            ctx.Response.Headers.Location.ToString().Should().Be("https://example.com/previous");
        }

        [Fact]
        public async Task InvokeAsync_EmptyInertiaResponse_NoReferer_Returns204()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);

            await middleware.InvokeAsync(ctx, NoOpNext);

            ctx.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        }

        [Fact]
        public async Task InvokeAsync_EmptyInertiaResponse_CustomDelegate_CallsDelegate()
        {
            var delegateCalled = false;
            var (middleware, _, ctx) = CreateMiddleware(o =>
                o.OnEmptyResponse = _ =>
                {
                    delegateCalled = true;
                    return Microsoft.AspNetCore.Http.Results.StatusCode(418);
                });
            SetInertiaHeaders(ctx);

            await middleware.InvokeAsync(ctx, NoOpNext);

            delegateCalled.Should().BeTrue();
            ctx.Response.StatusCode.Should().Be(418);
        }

        [Fact]
        public async Task InvokeAsync_EmptyNonInertiaResponse_DoesNotRedirect()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            // Non-Inertia request, empty response
            await middleware.InvokeAsync(ctx, NoOpNext);

            ctx.Response.StatusCode.Should().Be(200); // Default, unchanged
        }
    }

    // ---- Group 6: 302->303 Conversion ----
    public class RedirectConversion
    {
        [Fact]
        public async Task InvokeAsync_302FromPut_ConvertedTo303()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Method = HttpMethods.Put;

            await middleware.InvokeAsync(ctx, RedirectNext(302, "/target"));

            ctx.Response.StatusCode.Should().Be(303);
        }

        [Fact]
        public async Task InvokeAsync_302FromPatch_ConvertedTo303()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Method = HttpMethods.Patch;

            await middleware.InvokeAsync(ctx, RedirectNext(302, "/target"));

            ctx.Response.StatusCode.Should().Be(303);
        }

        [Fact]
        public async Task InvokeAsync_302FromDelete_ConvertedTo303()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Method = HttpMethods.Delete;

            await middleware.InvokeAsync(ctx, RedirectNext(302, "/target"));

            ctx.Response.StatusCode.Should().Be(303);
        }

        [Fact]
        public async Task InvokeAsync_302FromPost_StaysAs302()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Method = HttpMethods.Post;

            await middleware.InvokeAsync(ctx, RedirectNext(302, "/target"));

            ctx.Response.StatusCode.Should().Be(302);
        }

        [Fact]
        public async Task InvokeAsync_301FromPut_StaysAs301()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Method = HttpMethods.Put;

            await middleware.InvokeAsync(ctx, RedirectNext(301, "/target"));

            ctx.Response.StatusCode.Should().Be(301);
        }
    }

    // ---- Group 7: Fragment Redirect ----
    public class FragmentRedirect
    {
        [Fact]
        public async Task InvokeAsync_InertiaRedirectWithFragment_Returns409WithRedirectHeader()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Method = HttpMethods.Get;

            await middleware.InvokeAsync(ctx, RedirectNext(302, "/article#section"));

            ctx.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            ctx.Response.Headers[InertiaHeaderNames.Redirect].ToString().Should().Be("/article#section");
            ctx.Response.Headers.Location.Count.Should().Be(0);
        }

        [Fact]
        public async Task InvokeAsync_InertiaRedirectWithoutFragment_NormalRedirect()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Method = HttpMethods.Post;

            await middleware.InvokeAsync(ctx, RedirectNext(302, "/article"));

            ctx.Response.StatusCode.Should().Be(302);
            ctx.Response.Headers.Location.ToString().Should().Be("/article");
        }

        [Fact]
        public async Task InvokeAsync_NonInertiaRedirectWithFragment_NormalRedirect()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            // No Inertia headers
            await middleware.InvokeAsync(ctx, RedirectNext(302, "/article#section"));

            ctx.Response.StatusCode.Should().Be(302);
            ctx.Response.Headers.Location.ToString().Should().Be("/article#section");
        }

        [Fact]
        public async Task InvokeAsync_PrefetchRedirectWithFragment_NormalRedirect()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Headers["Purpose"] = "prefetch";

            await middleware.InvokeAsync(ctx, RedirectNext(302, "/article#section"));

            ctx.Response.StatusCode.Should().Be(302);
        }

        [Fact]
        public async Task InvokeAsync_SecPurposePrefetchRedirectWithFragment_NormalRedirect()
        {
            var (middleware, _, ctx) = CreateMiddleware();
            SetInertiaHeaders(ctx);
            ctx.Request.Headers["Sec-Purpose"] = "prefetch";

            await middleware.InvokeAsync(ctx, RedirectNext(302, "/article#section"));

            ctx.Response.StatusCode.Should().Be(302);
        }
    }

    // ---- Group 7b: Validation Error Sharing ----
    public class ValidationErrorSharing
    {
        [Fact]
        public async Task InvokeAsync_ValidationErrorProvider_SharesErrorsAsAlwaysProp()
        {
            var errors = new Dictionary<string, object?> { ["email"] = "Required" };
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.ValidationErrorProvider = (_, _) => errors);

            await middleware.InvokeAsync(ctx, NoOpNext);

            var shared = factory.GetShared();
            shared.Should().ContainKey("errors");
            shared["errors"].Should().BeAssignableTo<AlwaysProp<IDictionary<string, object?>>>();
        }

        [Fact]
        public async Task InvokeAsync_NoValidationErrorProvider_DoesNotShareErrors()
        {
            var (middleware, factory, ctx) = CreateMiddleware();

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetShared().Should().NotContainKey("errors");
        }

        [Fact]
        public async Task InvokeAsync_ValidationErrorProvider_PassesErrorBagHeader()
        {
            string? receivedBag = null;
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.ValidationErrorProvider = (_, bag) =>
                {
                    receivedBag = bag;
                    return new Dictionary<string, object?>();
                });
            ctx.Request.Headers[InertiaHeaderNames.ErrorBag] = "updateProfile";

            await middleware.InvokeAsync(ctx, NoOpNext);

            // Resolve the AlwaysProp to trigger the lazy delegate
            var alwaysProp = (AlwaysProp<IDictionary<string, object?>>)factory.GetShared()["errors"]!;
            await alwaysProp.ResolveAsync();
            receivedBag.Should().Be("updateProfile");
        }

        [Fact]
        public async Task InvokeAsync_ValidationErrorProvider_NullErrorBag_WhenHeaderMissing()
        {
            string? receivedBag = "not-null";
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.ValidationErrorProvider = (_, bag) =>
                {
                    receivedBag = bag;
                    return new Dictionary<string, object?>();
                });

            await middleware.InvokeAsync(ctx, NoOpNext);

            var alwaysProp = (AlwaysProp<IDictionary<string, object?>>)factory.GetShared()["errors"]!;
            await alwaysProp.ResolveAsync();
            receivedBag.Should().BeNull();
        }
    }

    // ---- Group 7c: SharedOnce Props ----
    public class SharedOnceProps
    {
        [Fact]
        public async Task InvokeAsync_SharedOncePropsProvider_WrapsPlainDelegateInOnceProp()
        {
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.SharedOncePropsProvider = (_, _) => new Dictionary<string, object?>
                {
                    ["token"] = (Func<string>)(() => "secret")
                });

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetShared()["token"].Should().BeAssignableTo<IOnceable>();
        }

        [Fact]
        public async Task InvokeAsync_SharedOncePropsProvider_PreservesExistingOnceProp()
        {
            var onceProp = new OnceProp<string>(() => "existing");
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.SharedOncePropsProvider = (_, _) => new Dictionary<string, object?>
                {
                    ["token"] = onceProp
                });

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetShared()["token"].Should().BeSameAs(onceProp);
        }

        [Fact]
        public async Task InvokeAsync_SharedOncePropsProvider_WrapsStaticValueInOnceProp()
        {
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.SharedOncePropsProvider = (_, _) => new Dictionary<string, object?>
                {
                    ["permissions"] = new[] { "read", "write" }
                });

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetShared()["permissions"].Should().BeAssignableTo<IOnceable>();
        }

        [Fact]
        public async Task InvokeAsync_NoSharedOncePropsProvider_NoOnceProps()
        {
            var (middleware, factory, ctx) = CreateMiddleware();

            await middleware.InvokeAsync(ctx, NoOpNext);

            factory.GetShared().Should().BeEmpty();
        }
    }

    // ---- Group 7d: SSR Path Exclusion ----
    public class SsrPathExclusion
    {
        [Fact]
        public async Task InvokeAsync_SsrExcludePaths_AppliesExclusionsToSsrState()
        {
            var (middleware, factory, ctx) = CreateMiddleware(o =>
                o.SsrExcludePaths = ["/admin", "/api/*"]);
            var ssrState = new SsrState(Substitute.For<ISsrGateway>());
            var services = new ServiceCollection();
            services.AddSingleton<IInertia>(factory);
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(ssrState);
            ctx.RequestServices = services.BuildServiceProvider();

            await middleware.InvokeAsync(ctx, NoOpNext);

            ssrState.IsPathExcluded("/admin").Should().BeTrue();
            ssrState.IsPathExcluded("/api/users").Should().BeTrue();
            ssrState.IsPathExcluded("/home").Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_NullSsrExcludePaths_DoesNotThrow()
        {
            var (middleware, _, ctx) = CreateMiddleware(o =>
                o.SsrExcludePaths = null);

            await middleware.InvokeAsync(ctx, NoOpNext);
        }

        [Fact]
        public async Task InvokeAsync_EmptySsrExcludePaths_DoesNotThrow()
        {
            var (middleware, _, ctx) = CreateMiddleware(o =>
                o.SsrExcludePaths = []);

            await middleware.InvokeAsync(ctx, NoOpNext);
        }

        [Fact]
        public async Task InvokeAsync_SsrExcludePaths_NoSsrState_DoesNotThrow()
        {
            // Default CreateMiddleware doesn't register SsrState
            var (middleware, _, ctx) = CreateMiddleware(o =>
                o.SsrExcludePaths = ["/admin"]);

            await middleware.InvokeAsync(ctx, NoOpNext);
        }
    }

    // ---- Group 8: Flash Data Reflashing ----
    public class FlashDataReflashing
    {
        [Fact]
        public async Task InvokeAsync_OnRedirect_ReflashesFlashData()
        {
            var options = new InertiaOptions();
            var httpContext = new DefaultHttpContext();
            var accessor = Substitute.For<IHttpContextAccessor>();
            accessor.HttpContext.Returns(httpContext);

            // Use a real-ish TempData that stores data
            var tempDataStore = new Dictionary<string, object?>();
            var tempData = Substitute.For<ITempDataDictionary>();
            tempData.ContainsKey(Arg.Any<string>()).Returns(call => tempDataStore.ContainsKey((string)call[0]));
            tempData.TryGetValue(Arg.Any<string>(), out Arg.Any<object?>()!)
                .Returns(call =>
                {
                    var key = (string)call[0];
                    if (tempDataStore.TryGetValue(key, out var val))
                    {
                        call[1] = val;
                        return true;
                    }
                    return false;
                });
            tempData[Arg.Any<string>()] = Arg.Do<object?>(val =>
            {
                // capture the key from the indexer
            });
            // Track writes
            tempData.When(t => t[Arg.Any<string>()] = Arg.Any<object?>())
                .Do(call => tempDataStore[(string)call[0]] = call[1]);

            var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
            tempDataFactory.GetTempData(httpContext).Returns(tempData);

            var factory = new InertiaFactory(Options.Create(options), accessor, tempDataFactory);
            factory.Flash("message", "Success!");

            var services = new ServiceCollection();
            services.AddSingleton<IInertia>(factory);
            httpContext.RequestServices = services.BuildServiceProvider();

            var middleware = new InertiaMiddleware(Options.Create(options));

            await middleware.InvokeAsync(httpContext, RedirectNext(302, "/target"));

            // Verify flash data was re-written (reflashed)
            tempDataStore.Should().ContainKey(InertiaSessionKeys.FlashData);
        }

        [Fact]
        public async Task InvokeAsync_VersionMismatch_ReflashesFlashData()
        {
            var options = new InertiaOptions { VersionProvider = _ => "v2" };
            var httpContext = new DefaultHttpContext();
            var accessor = Substitute.For<IHttpContextAccessor>();
            accessor.HttpContext.Returns(httpContext);

            var tempDataStore = new Dictionary<string, object?>();
            var tempData = Substitute.For<ITempDataDictionary>();
            tempData.ContainsKey(Arg.Any<string>()).Returns(call => tempDataStore.ContainsKey((string)call[0]));
            tempData.TryGetValue(Arg.Any<string>(), out Arg.Any<object?>()!)
                .Returns(call =>
                {
                    var key = (string)call[0];
                    if (tempDataStore.TryGetValue(key, out var val))
                    {
                        call[1] = val;
                        return true;
                    }
                    return false;
                });
            tempData.When(t => t[Arg.Any<string>()] = Arg.Any<object?>())
                .Do(call => tempDataStore[(string)call[0]] = call[1]);

            var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
            tempDataFactory.GetTempData(httpContext).Returns(tempData);

            var factory = new InertiaFactory(Options.Create(options), accessor, tempDataFactory);
            factory.Flash("alert", "Version changed!");

            var services = new ServiceCollection();
            services.AddSingleton<IInertia>(factory);
            httpContext.RequestServices = services.BuildServiceProvider();

            SetInertiaHeaders(httpContext, "v1");
            httpContext.Request.Method = HttpMethods.Get;

            var middleware = new InertiaMiddleware(Options.Create(options));

            await middleware.InvokeAsync(httpContext, NoOpNext);

            tempDataStore.Should().ContainKey(InertiaSessionKeys.FlashData);
        }

        private static void SetInertiaHeaders(HttpContext ctx, string? version = null)
        {
            ctx.Request.Headers[InertiaHeaderNames.Inertia] = "true";
            if (version is not null)
                ctx.Request.Headers[InertiaHeaderNames.Version] = version;
        }
    }
}
