using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Props;

public class AlwaysPropTests
{
    public class Constructor
    {
        [Fact]
        public async Task Constructor_WithValue_StoresValue()
        {
            var prop = new AlwaysProp<string>("hello");

            var result = await prop.ResolveAsync();
            result.Should().Be("hello");
        }

        [Fact]
        public async Task Constructor_WithCallback_StoresCallback()
        {
            var prop = new AlwaysProp<int>(() => 42);

            var result = await prop.ResolveAsync();
            result.Should().Be(42);
        }

        [Fact]
        public async Task Constructor_WithAsyncCallback_StoresCallback()
        {
            var prop = new AlwaysProp<int>(async () =>
            {
                await Task.Delay(1);
                return 99;
            });

            var result = await prop.ResolveAsync();
            result.Should().Be(99);
        }
    }

    public class ResolveAsync
    {
        [Fact]
        public async Task ResolveAsync_WithScalarValue_ReturnsValue()
        {
            var prop = new AlwaysProp<int>(42);

            var result = await prop.ResolveAsync();

            result.Should().Be(42);
        }

        [Fact]
        public async Task ResolveAsync_WithCallback_InvokesAndReturnsResult()
        {
            var invoked = false;
            var prop = new AlwaysProp<string>(() =>
            {
                invoked = true;
                return "result";
            });

            var result = await prop.ResolveAsync();

            invoked.Should().BeTrue();
            result.Should().Be("result");
        }

        [Fact]
        public async Task ResolveAsync_WithNullValue_ReturnsNull()
        {
            var prop = new AlwaysProp<string?>((string?)null);

            var result = await prop.ResolveAsync();
            result.Should().BeNull();
        }

        [Fact]
        public async Task ResolveAsync_WithAsyncCallback_AwaitsAndReturnsResult()
        {
            var prop = new AlwaysProp<string>(async () =>
            {
                await Task.Delay(1);
                return "async-result";
            });

            var result = await prop.ResolveAsync();

            result.Should().Be("async-result");
        }
    }

    public class TypeChecks
    {
        [Fact]
        public void DoesNotImplementIIgnoreFirstLoad()
        {
            var prop = new AlwaysProp<string>("test");

            prop.Should().NotBeAssignableTo<IIgnoreFirstLoad>();
        }

        [Fact]
        public void DoesNotImplementIDeferrable()
        {
            var prop = new AlwaysProp<string>("test");

            prop.Should().NotBeAssignableTo<IDeferrable>();
        }

        [Fact]
        public void DoesNotImplementIMergeable()
        {
            var prop = new AlwaysProp<string>("test");

            prop.Should().NotBeAssignableTo<IMergeable>();
        }

        [Fact]
        public void DoesNotImplementIOnceable()
        {
            var prop = new AlwaysProp<string>("test");

            prop.Should().NotBeAssignableTo<IOnceable>();
        }
    }
}
