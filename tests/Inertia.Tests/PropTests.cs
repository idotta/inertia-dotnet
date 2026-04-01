using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests;

public class PropTests
{
    public class Always
    {
        [Fact]
        public void Always_WithValue_ReturnsAlwaysProp()
        {
            var result = Prop.Always("test");

            result.Should().BeOfType<AlwaysProp<string>>();
        }

        [Fact]
        public void Always_WithCallback_ReturnsAlwaysProp()
        {
            var result = Prop.Always(() => 42);

            result.Should().BeOfType<AlwaysProp<int>>();
        }

        [Fact]
        public void Always_WithAsyncCallback_ReturnsAlwaysProp()
        {
            var result = Prop.Always(async () =>
            {
                await Task.Delay(1);
                return "async";
            });

            result.Should().BeOfType<AlwaysProp<string>>();
        }
    }

    public class Optional
    {
        [Fact]
        public void Optional_WithCallback_ReturnsOptionalProp()
        {
            var result = Prop.Optional(() => "value");

            result.Should().BeOfType<OptionalProp<string>>();
        }

        [Fact]
        public void Optional_WithAsyncCallback_ReturnsOptionalProp()
        {
            var result = Prop.Optional(async () =>
            {
                await Task.Delay(1);
                return 99;
            });

            result.Should().BeOfType<OptionalProp<int>>();
        }
    }

    public class Defer
    {
        [Fact]
        public void Defer_WithCallback_ReturnsDeferProp()
        {
            var result = Prop.Defer(() => "deferred");

            result.Should().BeOfType<DeferProp<string>>();
        }

        [Fact]
        public void Defer_WithAsyncCallback_ReturnsDeferProp()
        {
            var result = Prop.Defer(async () =>
            {
                await Task.Delay(1);
                return "deferred-async";
            });

            result.Should().BeOfType<DeferProp<string>>();
        }

        [Fact]
        public void Defer_WithGroup_SetsGroup()
        {
            var result = Prop.Defer<string>(() => "test", "myGroup");

            ((IDeferrable)result).Group.Should().Be("myGroup");
        }

        [Fact]
        public void Defer_DefaultGroup_IsDefault()
        {
            var result = Prop.Defer<string>(() => "test");

            ((IDeferrable)result).Group.Should().Be("default");
        }
    }

    public class Merge
    {
        [Fact]
        public void Merge_WithValue_ReturnsMergeProp()
        {
            var result = Prop.Merge("merged");

            result.Should().BeOfType<MergeProp<string>>();
        }

        [Fact]
        public void Merge_WithCallback_ReturnsMergeProp()
        {
            var result = Prop.Merge(() => 42);

            result.Should().BeOfType<MergeProp<int>>();
        }

        [Fact]
        public void Merge_WithAsyncCallback_ReturnsMergeProp()
        {
            var result = Prop.Merge(async () =>
            {
                await Task.Delay(1);
                return "async-merge";
            });

            result.Should().BeOfType<MergeProp<string>>();
        }
    }

    public class DeepMergeFactory
    {
        [Fact]
        public void DeepMerge_WithValue_ReturnsMergePropWithDeepMerge()
        {
            var result = Prop.DeepMerge("test");

            result.Should().BeOfType<MergeProp<string>>();
            ((IMergeable)result).ShouldDeepMerge.Should().BeTrue();
        }

        [Fact]
        public void DeepMerge_WithCallback_SetsShouldDeepMergeTrue()
        {
            var result = Prop.DeepMerge(() => 42);

            result.Should().BeOfType<MergeProp<int>>();
            ((IMergeable)result).ShouldDeepMerge.Should().BeTrue();
        }
    }

    public class Once
    {
        [Fact]
        public void Once_WithCallback_ReturnsOnceProp()
        {
            var result = Prop.Once(() => "once");

            result.Should().BeOfType<OnceProp<string>>();
            ((IOnceable)result).ShouldResolveOnce.Should().BeTrue();
        }

        [Fact]
        public void Once_WithAsyncCallback_ReturnsOnceProp()
        {
            var result = Prop.Once(async () =>
            {
                await Task.Delay(1);
                return "once-async";
            });

            result.Should().BeOfType<OnceProp<string>>();
            ((IOnceable)result).ShouldResolveOnce.Should().BeTrue();
        }
    }

    public class Scroll
    {
        [Fact]
        public void Scroll_WithValue_ReturnsScrollProp()
        {
            var result = Prop.Scroll("scroll-data");

            result.Should().BeOfType<ScrollProp<string>>();
        }

        [Fact]
        public void Scroll_WithCallback_ReturnsScrollProp()
        {
            var result = Prop.Scroll(() => new[] { 1, 2, 3 });

            result.Should().BeOfType<ScrollProp<int[]>>();
        }

        [Fact]
        public void Scroll_WithValueAndWrapper_SetsWrapper()
        {
            var result = Prop.Scroll(new[] { 1, 2, 3 }, wrapper: "items");

            // ConfigureMergeIntent uses the wrapper path for append/prepend.
            // Default intent (no header) appends at the wrapper path.
            result.ConfigureMergeIntent();

            var mergeable = (IMergeable)result;
            mergeable.AppendsAtPaths.Should().Contain("items");
        }

        [Fact]
        public void Scroll_WithSyncCallbackAndMetadataFactory_CreatesScrollProp()
        {
            var result = Prop.Scroll<string>(
                () => "data",
                "items",
                resolved => new ScrollMetadata("page", previousPage: 0, nextPage: 2, currentPage: 1));

            result.Should().BeOfType<ScrollProp<string>>();
            var mergeable = (IMergeable)result;
            mergeable.ShouldMerge.Should().BeTrue();
        }

        [Fact]
        public void Scroll_WithAsyncCallbackAndMetadataFactory_CreatesScrollProp()
        {
            var result = Prop.Scroll<string>(
                () => Task.FromResult("data"),
                "items",
                resolved => new ScrollMetadata("page", previousPage: 0, nextPage: 2, currentPage: 1));

            result.Should().BeOfType<ScrollProp<string>>();
            var mergeable = (IMergeable)result;
            mergeable.ShouldMerge.Should().BeTrue();
        }
    }
}
