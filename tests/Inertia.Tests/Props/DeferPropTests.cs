using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Props;

public class DeferPropTests
{
    public class Constructor
    {
        [Fact]
        public void Constructor_AutoSetsShouldDeferTrue()
        {
            var prop = new DeferProp<string>(() => "value");

            ((IDeferrable)prop).ShouldDefer.Should().BeTrue();
        }

        [Fact]
        public void Constructor_DefaultGroup_IsDefault()
        {
            var prop = new DeferProp<string>(() => "value");

            ((IDeferrable)prop).Group.Should().Be("default");
        }

        [Fact]
        public void Constructor_WithGroup_SetsCustomGroup()
        {
            var prop = new DeferProp<string>(() => "value", group: "my-group");

            ((IDeferrable)prop).Group.Should().Be("my-group");
        }

        [Fact]
        public void Constructor_WithNullGroup_UsesDefault()
        {
            var prop = new DeferProp<string>(() => "value", group: null);

            ((IDeferrable)prop).Group.Should().Be("default");
        }
    }

    public class ResolveAsync
    {
        [Fact]
        public async Task ResolveAsync_InvokesCallbackAndReturnsResult()
        {
            var called = false;
            var prop = new DeferProp<string>(() =>
            {
                called = true;
                return "deferred-value";
            });

            var result = await prop.ResolveAsync();

            called.Should().BeTrue();
            result.Should().Be("deferred-value");
        }

        [Fact]
        public async Task ResolveAsync_AwaitsAndReturnsResult()
        {
            var prop = new DeferProp<string>(() => Task.FromResult("async-deferred"));

            var result = await prop.ResolveAsync();

            result.Should().Be("async-deferred");
        }
    }

    public class MergeBehavior
    {
        [Fact]
        public void ShouldMerge_DefaultsFalse()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.ShouldMerge.Should().BeFalse(
                because: "DeferProp does not auto-merge unlike MergeProp");
        }

        [Fact]
        public void Merge_SetsShouldMergeTrue()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.Merge();

            prop.ShouldMerge.Should().BeTrue();
        }

        [Fact]
        public void DeepMerge_SetsBothFlags()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.DeepMerge();

            prop.ShouldDeepMerge.Should().BeTrue();
            prop.ShouldMerge.Should().BeTrue();
        }
    }

    public class OnceCapability
    {
        [Fact]
        public void ShouldResolveOnce_DefaultsFalse()
        {
            var prop = new DeferProp<string>(() => "value");

            ((IOnceable)prop).ShouldResolveOnce.Should().BeFalse();
        }

        [Fact]
        public void Once_SetsShouldResolveOnce()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.Once();

            ((IOnceable)prop).ShouldResolveOnce.Should().BeTrue();
        }

        [Fact]
        public void As_SetsKey()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.As("cache-key");

            ((IOnceable)prop).Key.Should().Be("cache-key");
        }

        [Fact]
        public void Until_SetsExpiration()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.Until(60);

            ((IOnceable)prop).ExpiresAt.Should().NotBeNull();
        }

        [Fact]
        public void Until_WithDateTimeOffset_SetsExpiration()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.Until(DateTimeOffset.UtcNow.AddMinutes(10));

            ((IOnceable)prop).ExpiresAt.Should().NotBeNull();
        }
    }

    public class FluentChaining
    {
        [Fact]
        public void Merge_ReturnsDeferPropT()
        {
            var prop = new DeferProp<string>(() => "value");

            var result = prop.Merge();

            result.Should().BeOfType<DeferProp<string>>();
            result.Should().BeSameAs(prop);
        }

        [Fact]
        public void Once_ReturnsDeferPropT()
        {
            var prop = new DeferProp<string>(() => "value");

            var result = prop.Once();

            result.Should().BeOfType<DeferProp<string>>();
            result.Should().BeSameAs(prop);
        }
    }

    public class Interfaces
    {
        [Fact]
        public void ImplementsIDeferrable()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.Should().BeAssignableTo<IDeferrable>();
        }

        [Fact]
        public void ImplementsIIgnoreFirstLoad()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.Should().BeAssignableTo<IIgnoreFirstLoad>();
        }

        [Fact]
        public void ImplementsIMergeable()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.Should().BeAssignableTo<IMergeable>();
        }

        [Fact]
        public void ImplementsIOnceable()
        {
            var prop = new DeferProp<string>(() => "value");

            prop.Should().BeAssignableTo<IOnceable>();
        }
    }
}
