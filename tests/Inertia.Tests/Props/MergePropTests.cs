using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Props;

public class MergePropTests
{
    public class Constructor
    {
        [Fact]
        public void Constructor_AutoSetsShouldMergeTrue()
        {
            var prop = new MergeProp<string>("hello");

            prop.ShouldMerge.Should().BeTrue(
                because: "the constructor calls Merge() automatically");
        }

        [Fact]
        public async Task Constructor_WithValue_StoresValue()
        {
            var prop = new MergeProp<int>(42);

            (await prop.ResolveAsync()).Should().Be(42);
        }

        [Fact]
        public async Task Constructor_WithCallback_StoresCallback()
        {
            var prop = new MergeProp<string>(() => "computed");

            (await prop.ResolveAsync()).Should().Be("computed");
        }

        [Fact]
        public async Task Constructor_WithAsyncCallback_StoresCallback()
        {
            var prop = new MergeProp<int>(() => Task.FromResult(99));

            (await prop.ResolveAsync()).Should().Be(99);
        }
    }

    public class ResolveAsync
    {
        [Fact]
        public async Task ResolveAsync_WithCallback_InvokesAndReturns()
        {
            var called = false;
            var prop = new MergeProp<string>(() =>
            {
                called = true;
                return "result";
            });

            var result = await prop.ResolveAsync();

            called.Should().BeTrue();
            result.Should().Be("result");
        }

        [Fact]
        public async Task ResolveAsync_WithScalarValue_ReturnsValue()
        {
            var prop = new MergeProp<int>(123);

            (await prop.ResolveAsync()).Should().Be(123);
        }

        [Fact]
        public async Task ResolveAsync_AwaitsAndReturnsResult()
        {
            var prop = new MergeProp<string>(() => Task.FromResult("async-result"));

            var result = await prop.ResolveAsync();

            result.Should().Be("async-result");
        }
    }

    public class MergeDefaults
    {
        [Fact]
        public void AppendsAtRoot_DefaultsTrue()
        {
            var prop = new MergeProp<string>("value");

            prop.AppendsAtRoot.Should().BeTrue();
        }

        [Fact]
        public void PrependsAtRoot_DefaultsFalse()
        {
            var prop = new MergeProp<string>("value");

            prop.PrependsAtRoot.Should().BeFalse();
        }

        [Fact]
        public void AppendsAtPaths_DefaultsEmpty()
        {
            var prop = new MergeProp<string>("value");

            prop.AppendsAtPaths.Should().BeEmpty();
        }

        [Fact]
        public void PrependsAtPaths_DefaultsEmpty()
        {
            var prop = new MergeProp<string>("value");

            prop.PrependsAtPaths.Should().BeEmpty();
        }

        [Fact]
        public void MatchesOn_DefaultsEmpty()
        {
            var prop = new MergeProp<string>("value");

            prop.MatchesOn.Should().BeEmpty();
        }
    }

    public class MergeBehavior
    {
        [Fact]
        public void DeepMerge_SetsBothFlags()
        {
            var prop = new MergeProp<string>("value");

            prop.DeepMerge();

            prop.ShouldDeepMerge.Should().BeTrue();
            prop.ShouldMerge.Should().BeTrue();
        }

        [Fact]
        public void Append_WithString_AddsPath()
        {
            var prop = new MergeProp<string>("value");

            prop.Append("items");

            prop.AppendsAtPaths.Should().ContainSingle().Which.Should().Be("items");
        }

        [Fact]
        public void Append_WithStringAndMatchOn_AddsPathAndMatchOn()
        {
            var prop = new MergeProp<string>("value");

            prop.Append("items", "id");

            prop.AppendsAtPaths.Should().ContainSingle().Which.Should().Be("items");
            prop.MatchesOn.Should().ContainSingle().Which.Should().Be("items.id");
        }

        [Fact]
        public void Prepend_WithString_AddsPath()
        {
            var prop = new MergeProp<string>("value");

            prop.Prepend("items");

            prop.PrependsAtPaths.Should().ContainSingle().Which.Should().Be("items");
        }

        [Fact]
        public void Prepend_WithBoolTrue_SetsPrependFlag()
        {
            var prop = new MergeProp<string>("value");

            prop.Prepend();

            prop.PrependsAtRoot.Should().BeTrue();
            prop.AppendsAtRoot.Should().BeFalse();
        }
    }

    public class OnceCapability
    {
        [Fact]
        public void ShouldResolveOnce_DefaultsFalse()
        {
            var prop = new MergeProp<string>("value");

            ((IOnceable)prop).ShouldResolveOnce.Should().BeFalse();
        }

        [Fact]
        public void Once_SetsShouldResolveOnce()
        {
            var prop = new MergeProp<string>("value");

            prop.Once();

            ((IOnceable)prop).ShouldResolveOnce.Should().BeTrue();
        }

        [Fact]
        public void As_WithString_SetsKey()
        {
            var prop = new MergeProp<string>("value");

            prop.As("my-key");

            ((IOnceable)prop).Key.Should().Be("my-key");
        }

        [Fact]
        public void Fresh_SetsShouldBeRefreshed()
        {
            var prop = new MergeProp<string>("value");

            prop.Fresh();

            ((IOnceable)prop).ShouldBeRefreshed.Should().BeTrue();
        }

        [Fact]
        public void Until_SetsExpiration()
        {
            var prop = new MergeProp<string>("value");

            prop.Until(TimeSpan.FromMinutes(5));

            ((IOnceable)prop).ExpiresAt.Should().NotBeNull();
        }
    }

    public class FluentChaining
    {
        [Fact]
        public void Merge_ReturnsMergePropT()
        {
            var prop = new MergeProp<string>("value");

            var result = prop.Merge();

            result.Should().BeOfType<MergeProp<string>>();
            result.Should().BeSameAs(prop);
        }

        [Fact]
        public void Once_ReturnsMergePropT()
        {
            var prop = new MergeProp<string>("value");

            var result = prop.Once();

            result.Should().BeOfType<MergeProp<string>>();
            result.Should().BeSameAs(prop);
        }

        [Fact]
        public void Append_ReturnsMergePropT()
        {
            var prop = new MergeProp<string>("value");

            var result = prop.Append("items");

            result.Should().BeOfType<MergeProp<string>>();
            result.Should().BeSameAs(prop);
        }
    }

    public class Interfaces
    {
        [Fact]
        public void ImplementsIMergeable()
        {
            var prop = new MergeProp<string>("value");

            prop.Should().BeAssignableTo<IMergeable>();
        }

        [Fact]
        public void ImplementsIOnceable()
        {
            var prop = new MergeProp<string>("value");

            prop.Should().BeAssignableTo<IOnceable>();
        }

        [Fact]
        public void DoesNotImplementIIgnoreFirstLoad()
        {
            var prop = new MergeProp<string>("value");

            prop.Should().NotBeAssignableTo<IIgnoreFirstLoad>();
        }

        [Fact]
        public void DoesNotImplementIDeferrable()
        {
            var prop = new MergeProp<string>("value");

            prop.Should().NotBeAssignableTo<IDeferrable>();
        }
    }
}
