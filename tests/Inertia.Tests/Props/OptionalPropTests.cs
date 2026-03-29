using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Props;

public class OptionalPropTests
{
    private enum CacheKey { Users, Posts }

    public class ResolveAsync
    {
        [Fact]
        public async Task ResolveAsync_InvokesCallbackAndReturnsResult()
        {
            var invoked = false;
            var prop = new OptionalProp<string>(() =>
            {
                invoked = true;
                return "value";
            });

            var result = await prop.ResolveAsync();

            invoked.Should().BeTrue();
            result.Should().Be("value");
        }

        [Fact]
        public async Task ResolveAsync_AwaitsAndReturnsResult()
        {
            var prop = new OptionalProp<int>(async () =>
            {
                await Task.Delay(1);
                return 42;
            });

            var result = await prop.ResolveAsync();

            result.Should().Be(42);
        }
    }

    public class OnceCapability
    {
        [Fact]
        public void ShouldResolveOnce_DefaultsFalse()
        {
            var prop = new OptionalProp<string>(() => "value");
            var onceable = (IOnceable)prop;

            onceable.ShouldResolveOnce.Should().BeFalse();
        }

        [Fact]
        public void ShouldBeRefreshed_DefaultsFalse()
        {
            var prop = new OptionalProp<string>(() => "value");
            var onceable = (IOnceable)prop;

            onceable.ShouldBeRefreshed.Should().BeFalse();
        }

        [Fact]
        public void Key_DefaultsToNull()
        {
            var prop = new OptionalProp<string>(() => "value");
            var onceable = (IOnceable)prop;

            onceable.Key.Should().BeNull();
        }

        [Fact]
        public void ExpiresAt_DefaultsToNull()
        {
            var prop = new OptionalProp<string>(() => "value");
            var onceable = (IOnceable)prop;

            onceable.ExpiresAt.Should().BeNull();
        }

        [Fact]
        public void Once_SetsShouldResolveOnceTrue()
        {
            var prop = new OptionalProp<string>(() => "value");

            prop.Once();

            var onceable = (IOnceable)prop;
            onceable.ShouldResolveOnce.Should().BeTrue();
        }

        [Fact]
        public void As_WithString_SetsKey()
        {
            var prop = new OptionalProp<string>(() => "value");

            prop.As("my-key");

            var onceable = (IOnceable)prop;
            onceable.Key.Should().Be("my-key");
        }

        [Fact]
        public void As_WithEnum_SetsKey()
        {
            var prop = new OptionalProp<string>(() => "value");

            prop.As(CacheKey.Users);

            var onceable = (IOnceable)prop;
            onceable.Key.Should().Be("Users");
        }

        [Fact]
        public void Fresh_SetsShouldBeRefreshedTrue()
        {
            var prop = new OptionalProp<string>(() => "value");

            prop.Fresh();

            var onceable = (IOnceable)prop;
            onceable.ShouldBeRefreshed.Should().BeTrue();
        }

        [Fact]
        public void Until_WithTimeSpan_SetsExpiration()
        {
            var prop = new OptionalProp<string>(() => "value");

            prop.Until(TimeSpan.FromMinutes(5));

            var onceable = (IOnceable)prop;
            onceable.ExpiresAt.Should().NotBeNull();
        }
    }

    public class FluentChaining
    {
        [Fact]
        public void Once_ReturnsSelf()
        {
            var prop = new OptionalProp<string>(() => "value");

            var result = prop.Once();

            result.Should().BeSameAs(prop);
        }

        [Fact]
        public void As_ReturnsSelf()
        {
            var prop = new OptionalProp<string>(() => "value");

            var result = prop.As("key");

            result.Should().BeSameAs(prop);
        }

        [Fact]
        public void Fresh_ReturnsSelf()
        {
            var prop = new OptionalProp<string>(() => "value");

            var result = prop.Fresh();

            result.Should().BeSameAs(prop);
        }

        [Fact]
        public void Until_ReturnsSelf()
        {
            var prop = new OptionalProp<string>(() => "value");

            var result = prop.Until(30);

            result.Should().BeSameAs(prop);
        }

        [Fact]
        public void FullChain_OnceThenAsThenUntil()
        {
            var prop = new OptionalProp<string>(() => "value");

            var result = prop.Once().As("my-key").Until(60);

            result.Should().BeSameAs(prop);
            var onceable = (IOnceable)prop;
            onceable.ShouldResolveOnce.Should().BeTrue();
            onceable.Key.Should().Be("my-key");
            onceable.ExpiresAt.Should().NotBeNull();
        }
    }

    public class Interfaces
    {
        [Fact]
        public void ImplementsIIgnoreFirstLoad()
        {
            var prop = new OptionalProp<string>(() => "value");

            prop.Should().BeAssignableTo<IIgnoreFirstLoad>();
        }

        [Fact]
        public void ImplementsIOnceable()
        {
            var prop = new OptionalProp<string>(() => "value");

            prop.Should().BeAssignableTo<IOnceable>();
        }

        [Fact]
        public void DoesNotImplementIDeferrable()
        {
            var prop = new OptionalProp<string>(() => "value");

            prop.Should().NotBeAssignableTo<IDeferrable>();
        }

        [Fact]
        public void DoesNotImplementIMergeable()
        {
            var prop = new OptionalProp<string>(() => "value");

            prop.Should().NotBeAssignableTo<IMergeable>();
        }
    }
}
