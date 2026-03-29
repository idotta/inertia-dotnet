using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Inertia.Tests.Props;

public class ScrollPropTests
{
    private static HttpRequest CreateRequest(string? mergeIntent = null)
    {
        var context = new DefaultHttpContext();
        if (mergeIntent is not null)
            context.Request.Headers[InertiaHeaderNames.InfiniteScrollMergeIntent] = mergeIntent;
        return context.Request;
    }

    public class Constructor
    {
        [Fact]
        public void Constructor_SetsMergeTrue()
        {
            var prop = new ScrollProp<string>("value");

            var mergeable = (IMergeable)prop;
            mergeable.ShouldMerge.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithDefaultWrapper_UsesData()
        {
            var prop = new ScrollProp<int[]>(() => [1, 2, 3]);

            // ConfigureMergeIntent without header appends at wrapper path.
            // Default wrapper is "data", so AppendsAtPaths should contain "data".
            prop.ConfigureMergeIntent();

            var mergeable = (IMergeable)prop;
            mergeable.AppendsAtPaths.Should().Contain("data");
        }

        [Fact]
        public void Constructor_WithCustomWrapper_UsesCustom()
        {
            var prop = new ScrollProp<int[]>(() => [1, 2, 3], wrapper: "items");

            prop.ConfigureMergeIntent();

            var mergeable = (IMergeable)prop;
            mergeable.AppendsAtPaths.Should().Contain("items");
        }
    }

    public class ResolveAsync
    {
        [Fact]
        public async Task ResolveAsync_WithScalarValue_ReturnsValue()
        {
            var prop = new ScrollProp<string>("hello");

            var result = await prop.ResolveAsync();

            result.Should().Be("hello");
        }

        [Fact]
        public async Task ResolveAsync_WithCallback_InvokesAndReturns()
        {
            var invoked = false;
            var prop = new ScrollProp<int>(() =>
            {
                invoked = true;
                return 42;
            });

            var result = await prop.ResolveAsync();

            invoked.Should().BeTrue();
            result.Should().Be(42);
        }

        [Fact]
        public async Task ResolveAsync_CalledMultipleTimes_InvokesCallbackOnce()
        {
            var invokeCount = 0;
            var prop = new ScrollProp<string>(() =>
            {
                invokeCount++;
                return "cached";
            });

            await prop.ResolveAsync();
            await prop.ResolveAsync();
            await prop.ResolveAsync();

            invokeCount.Should().Be(1);
        }

        [Fact]
        public async Task ResolveAsync_WithAsyncCallback_AwaitsAndReturns()
        {
            var prop = new ScrollProp<int>(async () =>
            {
                await Task.Delay(1);
                return 99;
            });

            var result = await prop.ResolveAsync();

            result.Should().Be(99);
        }
    }

    public class DeferBehavior
    {
        [Fact]
        public void ShouldDefer_DefaultsFalse()
        {
            var prop = new ScrollProp<string>("value");
            var deferrable = (IDeferrable)prop;

            deferrable.ShouldDefer.Should().BeFalse();
        }

        [Fact]
        public void Defer_SetsShouldDeferTrue()
        {
            var prop = new ScrollProp<string>("value");

            prop.Defer();

            var deferrable = (IDeferrable)prop;
            deferrable.ShouldDefer.Should().BeTrue();
        }

        [Fact]
        public void Defer_WithGroup_SetsCustomGroup()
        {
            var prop = new ScrollProp<string>("value");

            prop.Defer("scroll-group");

            var deferrable = (IDeferrable)prop;
            deferrable.Group.Should().Be("scroll-group");
        }
    }

    public class MetadataMethod
    {
        [Fact]
        public void Metadata_WithProvider_ReturnsExpectedDictionary()
        {
            var scrollMeta = new ScrollMetadata("page", previousPage: 1, nextPage: 3, currentPage: 2);
            var prop = new ScrollProp<string>("value", metadata: scrollMeta);

            var dict = prop.Metadata();

            dict["pageName"].Should().Be("page");
            dict["previousPage"].Should().Be(1);
            dict["nextPage"].Should().Be(3);
            dict["currentPage"].Should().Be(2);
        }

        [Fact]
        public void Metadata_WithFactory_InvokesFactoryWithResolvedValue()
        {
            object? capturedValue = null;
            var prop = new ScrollProp<string>(
                () => "resolved-data",
                "data",
                resolved =>
                {
                    capturedValue = resolved;
                    return new ScrollMetadata("page", previousPage: 0, nextPage: 2, currentPage: 1);
                });

            var dict = prop.Metadata();

            capturedValue.Should().Be("resolved-data");
            dict["pageName"].Should().Be("page");
            dict["currentPage"].Should().Be(1);
        }

        [Fact]
        public void Metadata_WithoutProvider_ThrowsInvalidOperationException()
        {
            var prop = new ScrollProp<string>("value");

            var act = () => prop.Metadata();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No scroll metadata provider configured*");
        }
    }

    public class ConfigureMergeIntentMethod
    {
        [Fact]
        public void ConfigureMergeIntent_WithoutHeader_AppendsAtWrapper()
        {
            var prop = new ScrollProp<string>("value", wrapper: "data");
            var request = CreateRequest();

            prop.ConfigureMergeIntent(request);

            var mergeable = (IMergeable)prop;
            mergeable.AppendsAtPaths.Should().Contain("data");
            mergeable.PrependsAtPaths.Should().BeEmpty();
        }

        [Fact]
        public void ConfigureMergeIntent_WithAppendHeader_AppendsAtWrapper()
        {
            var prop = new ScrollProp<string>("value", wrapper: "data");
            var request = CreateRequest("append");

            prop.ConfigureMergeIntent(request);

            var mergeable = (IMergeable)prop;
            mergeable.AppendsAtPaths.Should().Contain("data");
            mergeable.PrependsAtPaths.Should().BeEmpty();
        }

        [Fact]
        public void ConfigureMergeIntent_WithPrependHeader_PrependsAtWrapper()
        {
            var prop = new ScrollProp<string>("value", wrapper: "items");
            var request = CreateRequest("prepend");

            prop.ConfigureMergeIntent(request);

            var mergeable = (IMergeable)prop;
            mergeable.PrependsAtPaths.Should().Contain("items");
            mergeable.AppendsAtPaths.Should().BeEmpty();
        }
    }

    public class Interfaces
    {
        [Fact]
        public void ImplementsIDeferrable()
        {
            var prop = new ScrollProp<string>("value");

            prop.Should().BeAssignableTo<IDeferrable>();
        }

        [Fact]
        public void ImplementsIMergeable()
        {
            var prop = new ScrollProp<string>("value");

            prop.Should().BeAssignableTo<IMergeable>();
        }

        [Fact]
        public void DoesNotImplementIOnceable()
        {
            var prop = new ScrollProp<string>("value");

            prop.Should().NotBeAssignableTo<IOnceable>();
        }

        [Fact]
        public void DoesNotImplementIIgnoreFirstLoad()
        {
            var prop = new ScrollProp<string>("value");

            prop.Should().NotBeAssignableTo<IIgnoreFirstLoad>();
        }
    }
}
