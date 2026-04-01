using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Inertia.Tests.Props;

/// <summary>
/// Tests for <see cref="IServiceResolvableProp"/> implementation across all prop types.
/// Validates that service-provider callbacks are invoked with the provided IServiceProvider.
/// </summary>
public class ServiceResolvablePropTests
{
    private readonly IServiceProvider _mockServiceProvider = Substitute.For<IServiceProvider>();

    public class AlwaysPropServiceCallback : ServiceResolvablePropTests
    {
        [Fact]
        public void HasServiceCallback_WithServiceCallback_ReturnsTrue()
        {
            var prop = new AlwaysProp<string>(sp => "service-value");

            ((IServiceResolvableProp)prop).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void HasServiceCallback_WithAsyncServiceCallback_ReturnsTrue()
        {
            var prop = new AlwaysProp<string>(sp => Task.FromResult("async-service-value"));

            ((IServiceResolvableProp)prop).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void HasServiceCallback_WithRegularCallback_ReturnsFalse()
        {
            var prop = new AlwaysProp<string>(() => "regular-value");

            ((IServiceResolvableProp)prop).HasServiceCallback.Should().BeFalse();
        }

        [Fact]
        public void HasServiceCallback_WithValue_ReturnsFalse()
        {
            var prop = new AlwaysProp<string>("static-value");

            ((IServiceResolvableProp)prop).HasServiceCallback.Should().BeFalse();
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithSyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new AlwaysProp<string>(sp =>
            {
                captured = sp;
                return "resolved";
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("resolved");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithAsyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new AlwaysProp<string>(sp =>
            {
                captured = sp;
                return Task.FromResult("async-resolved");
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("async-resolved");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithoutServiceCallback_FallsBackToResolveAsync()
        {
            var prop = new AlwaysProp<string>("fallback-value");

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("fallback-value");
        }

        [Fact]
        public void Constructor_WithNullServiceCallback_ThrowsArgumentNullException()
        {
            var act = () => new AlwaysProp<string>((Func<IServiceProvider, string>)null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithNullAsyncServiceCallback_ThrowsArgumentNullException()
        {
            var act = () => new AlwaysProp<string>((Func<IServiceProvider, Task<string>>)null!);

            act.Should().Throw<ArgumentNullException>();
        }
    }

    public class OptionalPropServiceCallback : ServiceResolvablePropTests
    {
        [Fact]
        public void HasServiceCallback_WithServiceCallback_ReturnsTrue()
        {
            var prop = new OptionalProp<int>(sp => 42);

            ((IServiceResolvableProp)prop).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithSyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new OptionalProp<int>(sp =>
            {
                captured = sp;
                return 42;
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be(42);
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithAsyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new OptionalProp<string>(sp =>
            {
                captured = sp;
                return Task.FromResult("optional-service");
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("optional-service");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithoutServiceCallback_FallsBackToResolveAsync()
        {
            var prop = new OptionalProp<string>(() => "regular");

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("regular");
        }
    }

    public class OncePropServiceCallback : ServiceResolvablePropTests
    {
        [Fact]
        public void HasServiceCallback_WithServiceCallback_ReturnsTrue()
        {
            var prop = new OnceProp<string>(sp => "once-service");

            ((IServiceResolvableProp)prop).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithAsyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new OnceProp<string>(sp =>
            {
                captured = sp;
                return Task.FromResult("once-async-service");
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("once-async-service");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public void Constructor_WithServiceCallback_EnablesOnceByDefault()
        {
            var prop = new OnceProp<string>(sp => "value");

            ((IOnceable)prop).ShouldResolveOnce.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithAsyncServiceCallback_EnablesOnceByDefault()
        {
            var prop = new OnceProp<string>(sp => Task.FromResult("value"));

            ((IOnceable)prop).ShouldResolveOnce.Should().BeTrue();
        }
    }

    public class DeferPropServiceCallback : ServiceResolvablePropTests
    {
        [Fact]
        public void HasServiceCallback_WithServiceCallback_ReturnsTrue()
        {
            var prop = new DeferProp<string>(sp => "deferred-service");

            ((IServiceResolvableProp)prop).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithSyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new DeferProp<string>(sp =>
            {
                captured = sp;
                return "deferred-resolved";
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("deferred-resolved");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithAsyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new DeferProp<string>(sp =>
            {
                captured = sp;
                return Task.FromResult("deferred-async-resolved");
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("deferred-async-resolved");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public void Constructor_WithServiceCallback_AutoSetsShouldDeferTrue()
        {
            var prop = new DeferProp<string>(sp => "value");

            ((IDeferrable)prop).ShouldDefer.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithServiceCallback_DefaultGroupIsDefault()
        {
            var prop = new DeferProp<string>(sp => "value");

            ((IDeferrable)prop).Group.Should().Be("default");
        }

        [Fact]
        public void Constructor_WithServiceCallback_CustomGroup_SetsGroup()
        {
            var prop = new DeferProp<string>(sp => "value", group: "my-group");

            ((IDeferrable)prop).Group.Should().Be("my-group");
        }

        [Fact]
        public void Constructor_WithAsyncServiceCallback_CustomGroup_SetsGroup()
        {
            var prop = new DeferProp<string>(sp => Task.FromResult("value"), group: "my-group");

            ((IDeferrable)prop).Group.Should().Be("my-group");
        }
    }

    public class MergePropServiceCallback : ServiceResolvablePropTests
    {
        [Fact]
        public void HasServiceCallback_WithServiceCallback_ReturnsTrue()
        {
            var prop = new MergeProp<string>(sp => "merge-service");

            ((IServiceResolvableProp)prop).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithSyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new MergeProp<string>(sp =>
            {
                captured = sp;
                return "merge-resolved";
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("merge-resolved");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithAsyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new MergeProp<string>(sp =>
            {
                captured = sp;
                return Task.FromResult("merge-async-resolved");
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("merge-async-resolved");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public void Constructor_WithServiceCallback_AutoSetsShouldMergeTrue()
        {
            var prop = new MergeProp<string>(sp => "value");

            ((IMergeable)prop).ShouldMerge.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithAsyncServiceCallback_AutoSetsShouldMergeTrue()
        {
            var prop = new MergeProp<string>(sp => Task.FromResult("value"));

            ((IMergeable)prop).ShouldMerge.Should().BeTrue();
        }
    }

    public class ScrollPropServiceCallback : ServiceResolvablePropTests
    {
        [Fact]
        public void HasServiceCallback_WithServiceCallback_ReturnsTrue()
        {
            var prop = new ScrollProp<string>(sp => "scroll-service");

            ((IServiceResolvableProp)prop).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithSyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new ScrollProp<string>(sp =>
            {
                captured = sp;
                return "scroll-resolved";
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("scroll-resolved");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public async Task ResolveWithServiceAsync_WithAsyncServiceCallback_InvokesWithServiceProvider()
        {
            IServiceProvider? captured = null;
            var prop = new ScrollProp<string>(sp =>
            {
                captured = sp;
                return Task.FromResult("scroll-async-resolved");
            });

            var result = await ((IServiceResolvableProp)prop).ResolveWithServiceAsync(_mockServiceProvider);

            result.Should().Be("scroll-async-resolved");
            captured.Should().BeSameAs(_mockServiceProvider);
        }

        [Fact]
        public async Task ResolveWithServiceAsync_CachesResult_OnSubsequentCalls()
        {
            var callCount = 0;
            var prop = new ScrollProp<string>(sp =>
            {
                callCount++;
                return "cached-scroll";
            });

            var srp = (IServiceResolvableProp)prop;
            var result1 = await srp.ResolveWithServiceAsync(_mockServiceProvider);
            var result2 = await srp.ResolveWithServiceAsync(_mockServiceProvider);

            result1.Should().Be("cached-scroll");
            result2.Should().Be("cached-scroll");
            callCount.Should().Be(1, because: "ScrollProp caches the resolved value after first call");
        }

        [Fact]
        public void Constructor_WithServiceCallback_AutoSetsShouldMergeTrue()
        {
            var prop = new ScrollProp<string>(sp => "value");

            ((IMergeable)prop).ShouldMerge.Should().BeTrue();
        }
    }

    public class PropFactoryServiceCallbacks
    {
        [Fact]
        public void Always_WithServiceCallback_CreatesAlwaysProp()
        {
            var result = Prop.Always<string>(sp => "test");

            result.Should().BeOfType<AlwaysProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void Always_WithAsyncServiceCallback_CreatesAlwaysProp()
        {
            var result = Prop.Always<string>(sp => Task.FromResult("test"));

            result.Should().BeOfType<AlwaysProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void Optional_WithServiceCallback_CreatesOptionalProp()
        {
            var result = Prop.Optional<string>(sp => "test");

            result.Should().BeOfType<OptionalProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void Optional_WithAsyncServiceCallback_CreatesOptionalProp()
        {
            var result = Prop.Optional<string>(sp => Task.FromResult("test"));

            result.Should().BeOfType<OptionalProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void Defer_WithServiceCallback_CreatesDeferProp()
        {
            var result = Prop.Defer<string>(sp => "test");

            result.Should().BeOfType<DeferProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void Defer_WithAsyncServiceCallback_CreatesDeferProp()
        {
            var result = Prop.Defer<string>(sp => Task.FromResult("test"));

            result.Should().BeOfType<DeferProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void Defer_WithServiceCallback_AndGroup_SetsGroup()
        {
            var result = Prop.Defer<string>(sp => "test", group: "custom");

            ((IDeferrable)result).Group.Should().Be("custom");
        }

        [Fact]
        public void Merge_WithServiceCallback_CreatesMergeProp()
        {
            var result = Prop.Merge<string>(sp => "test");

            result.Should().BeOfType<MergeProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
            ((IMergeable)result).ShouldMerge.Should().BeTrue();
        }

        [Fact]
        public void Merge_WithAsyncServiceCallback_CreatesMergeProp()
        {
            var result = Prop.Merge<string>(sp => Task.FromResult("test"));

            result.Should().BeOfType<MergeProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void DeepMerge_WithServiceCallback_CreatesMergePropWithDeepMerge()
        {
            var result = Prop.DeepMerge<string>(sp => "test");

            result.Should().BeOfType<MergeProp<string>>();
            ((IMergeable)result).ShouldDeepMerge.Should().BeTrue();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void DeepMerge_WithAsyncServiceCallback_CreatesMergePropWithDeepMerge()
        {
            var result = Prop.DeepMerge<string>(sp => Task.FromResult("test"));

            result.Should().BeOfType<MergeProp<string>>();
            ((IMergeable)result).ShouldDeepMerge.Should().BeTrue();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
        }

        [Fact]
        public void Once_WithServiceCallback_CreatesOnceProp()
        {
            var result = Prop.Once<string>(sp => "test");

            result.Should().BeOfType<OnceProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
            ((IOnceable)result).ShouldResolveOnce.Should().BeTrue();
        }

        [Fact]
        public void Once_WithAsyncServiceCallback_CreatesOnceProp()
        {
            var result = Prop.Once<string>(sp => Task.FromResult("test"));

            result.Should().BeOfType<OnceProp<string>>();
            ((IServiceResolvableProp)result).HasServiceCallback.Should().BeTrue();
            ((IOnceable)result).ShouldResolveOnce.Should().BeTrue();
        }
    }

    public class PropsResolverIntegration
    {
        private const string TestComponent = "TestComponent";

        [Fact]
        public async Task PropsResolver_ResolvesServiceCallback_ViaHttpContextRequestServices()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/";
            var mockSp = Substitute.For<IServiceProvider>();
            ctx.RequestServices = mockSp;

            IServiceProvider? captured = null;
            var props = new Dictionary<string, object?>
            {
                ["data"] = new AlwaysProp<string>(sp =>
                {
                    captured = sp;
                    return "from-service";
                }),
            };

            var resolver = new PropsResolver(ctx, TestComponent, exposeSharedPropKeys: false);
            var (resolved, _) = await resolver.ResolveAsync(
                new Dictionary<string, object?>(),
                Array.Empty<IInertiaPropertyProvider>(),
                props);

            resolved["data"].Should().Be("from-service");
            captured.Should().BeSameAs(mockSp);
        }

        [Fact]
        public async Task PropsResolver_ResolvesAsyncServiceCallback_ViaHttpContextRequestServices()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/";
            var mockSp = Substitute.For<IServiceProvider>();
            ctx.RequestServices = mockSp;

            IServiceProvider? captured = null;
            var props = new Dictionary<string, object?>
            {
                ["data"] = new AlwaysProp<string>(sp =>
                {
                    captured = sp;
                    return Task.FromResult("async-from-service");
                }),
            };

            var resolver = new PropsResolver(ctx, TestComponent, exposeSharedPropKeys: false);
            var (resolved, _) = await resolver.ResolveAsync(
                new Dictionary<string, object?>(),
                Array.Empty<IInertiaPropertyProvider>(),
                props);

            resolved["data"].Should().Be("async-from-service");
            captured.Should().BeSameAs(mockSp);
        }

        [Fact]
        public async Task PropsResolver_RegularCallback_BypassesServiceResolution()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/";

            var props = new Dictionary<string, object?>
            {
                ["data"] = new AlwaysProp<string>(() => "regular-value"),
            };

            var resolver = new PropsResolver(ctx, TestComponent, exposeSharedPropKeys: false);
            var (resolved, _) = await resolver.ResolveAsync(
                new Dictionary<string, object?>(),
                Array.Empty<IInertiaPropertyProvider>(),
                props);

            resolved["data"].Should().Be("regular-value");
        }

        [Fact]
        public async Task PropsResolver_MergePropWithServiceCallback_ResolvesAndRetainsMergeMetadata()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/";
            var mockSp = Substitute.For<IServiceProvider>();
            ctx.RequestServices = mockSp;

            var props = new Dictionary<string, object?>
            {
                ["items"] = new MergeProp<List<int>>(sp => new List<int> { 1, 2, 3 }),
            };

            var resolver = new PropsResolver(ctx, TestComponent, exposeSharedPropKeys: false);
            var (resolved, metadata) = await resolver.ResolveAsync(
                new Dictionary<string, object?>(),
                Array.Empty<IInertiaPropertyProvider>(),
                props);

            resolved["items"].Should().BeEquivalentTo(new List<int> { 1, 2, 3 });
            metadata.MergeProps.Should().Contain("items");
        }

        [Fact]
        public async Task PropsResolver_ServiceCallbackCanResolveFromServiceProvider()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/";
            var mockSp = Substitute.For<IServiceProvider>();
            var fakeService = new FakeUserService { Name = "Alice" };
            mockSp.GetService(typeof(FakeUserService)).Returns(fakeService);
            ctx.RequestServices = mockSp;

            var props = new Dictionary<string, object?>
            {
                ["userName"] = new AlwaysProp<string>(sp =>
                {
                    var svc = (FakeUserService)sp.GetService(typeof(FakeUserService))!;
                    return svc.Name;
                }),
            };

            var resolver = new PropsResolver(ctx, TestComponent, exposeSharedPropKeys: false);
            var (resolved, _) = await resolver.ResolveAsync(
                new Dictionary<string, object?>(),
                Array.Empty<IInertiaPropertyProvider>(),
                props);

            resolved["userName"].Should().Be("Alice");
        }

        private class FakeUserService
        {
            public string Name { get; set; } = "";
        }
    }
}
