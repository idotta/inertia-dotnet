using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Props;

public class OncePropTests
{
    private enum CacheKey { Users, Posts }

    public class Constructor
    {
        [Fact]
        public void Constructor_AutoSetsShouldResolveOnceTrue()
        {
            var prop = new OnceProp<string>(() => "value");
            var onceable = (IOnceable)prop;

            onceable.ShouldResolveOnce.Should().BeTrue();
        }
    }

    public class ResolveAsync
    {
        [Fact]
        public async Task ResolveAsync_InvokesCallbackAndReturnsResult()
        {
            var invoked = false;
            var prop = new OnceProp<string>(() =>
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
            var prop = new OnceProp<int>(async () =>
            {
                await Task.Delay(1);
                return 42;
            });

            var result = await prop.ResolveAsync();

            result.Should().Be(42);
        }
    }

    public class OnceDefaults
    {
        [Fact]
        public void ShouldResolveOnce_IsTrue()
        {
            var prop = new OnceProp<string>(() => "value");
            var onceable = (IOnceable)prop;

            onceable.ShouldResolveOnce.Should().BeTrue();
        }

        [Fact]
        public void ShouldBeRefreshed_DefaultsFalse()
        {
            var prop = new OnceProp<string>(() => "value");
            var onceable = (IOnceable)prop;

            onceable.ShouldBeRefreshed.Should().BeFalse();
        }

        [Fact]
        public void Key_DefaultsToNull()
        {
            var prop = new OnceProp<string>(() => "value");
            var onceable = (IOnceable)prop;

            onceable.Key.Should().BeNull();
        }

        [Fact]
        public void ExpiresAt_DefaultsToNull()
        {
            var prop = new OnceProp<string>(() => "value");
            var onceable = (IOnceable)prop;

            onceable.ExpiresAt.Should().BeNull();
        }
    }

    public class FluentApi
    {
        [Fact]
        public void As_WithString_SetsKey()
        {
            var prop = new OnceProp<string>(() => "value");

            prop.As("my-key");

            var onceable = (IOnceable)prop;
            onceable.Key.Should().Be("my-key");
        }

        [Fact]
        public void As_WithEnum_SetsKey()
        {
            var prop = new OnceProp<string>(() => "value");

            prop.As(CacheKey.Posts);

            var onceable = (IOnceable)prop;
            onceable.Key.Should().Be("Posts");
        }

        [Fact]
        public void Fresh_SetsShouldBeRefreshedTrue()
        {
            var prop = new OnceProp<string>(() => "value");

            prop.Fresh();

            var onceable = (IOnceable)prop;
            onceable.ShouldBeRefreshed.Should().BeTrue();
        }

        [Fact]
        public void Fresh_WithFalse_ClearsShouldBeRefreshed()
        {
            var prop = new OnceProp<string>(() => "value");
            prop.Fresh();

            prop.Fresh(false);

            var onceable = (IOnceable)prop;
            onceable.ShouldBeRefreshed.Should().BeFalse();
        }

        [Fact]
        public void Until_WithSeconds_SetsExpiration()
        {
            var prop = new OnceProp<string>(() => "value");

            prop.Until(300);

            var onceable = (IOnceable)prop;
            onceable.ExpiresAt.Should().NotBeNull();
        }

        [Fact]
        public void Until_WithDateTimeOffset_SetsExpiration()
        {
            var prop = new OnceProp<string>(() => "value");

            prop.Until(DateTimeOffset.UtcNow.AddMinutes(10));

            var onceable = (IOnceable)prop;
            onceable.ExpiresAt.Should().NotBeNull();
        }

        [Fact]
        public void Once_WithFalse_DisablesOnce()
        {
            var prop = new OnceProp<string>(() => "value");
            var onceable = (IOnceable)prop;
            onceable.ShouldResolveOnce.Should().BeTrue(
                because: "constructor auto-enables Once");

            prop.Once(false);

            onceable.ShouldResolveOnce.Should().BeFalse();
        }
    }

    public class Interfaces
    {
        [Fact]
        public void ImplementsIOnceable()
        {
            var prop = new OnceProp<string>(() => "value");

            prop.Should().BeAssignableTo<IOnceable>();
        }

        [Fact]
        public void DoesNotImplementIIgnoreFirstLoad()
        {
            var prop = new OnceProp<string>(() => "value");

            prop.Should().NotBeAssignableTo<IIgnoreFirstLoad>();
        }

        [Fact]
        public void DoesNotImplementIDeferrable()
        {
            var prop = new OnceProp<string>(() => "value");

            prop.Should().NotBeAssignableTo<IDeferrable>();
        }
    }
}
