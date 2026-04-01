using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Props;

public class OnceInfoTests
{
    private enum TestStatus { Active, Inactive }

    public class Defaults
    {
        [Fact]
        public void Default_ShouldResolveOnce_IsFalse()
        {
            var info = new OnceInfo();

            info.ShouldResolveOnce.Should().BeFalse();
        }

        [Fact]
        public void Default_ShouldBeRefreshed_IsFalse()
        {
            var info = new OnceInfo();

            info.ShouldBeRefreshed.Should().BeFalse();
        }

        [Fact]
        public void Default_Key_IsNull()
        {
            var info = new OnceInfo();

            info.Key.Should().BeNull();
        }

        [Fact]
        public void Default_ExpiresAt_IsNull()
        {
            var info = new OnceInfo();

            info.ExpiresAt.Should().BeNull();
        }
    }

    public class Once
    {
        [Fact]
        public void Once_SetsResolveOnceTrue()
        {
            var info = new OnceInfo();

            info.Once();

            info.ShouldResolveOnce.Should().BeTrue();
        }

        [Fact]
        public void Once_WithFalse_SetsResolveOnceFalse()
        {
            var info = new OnceInfo();
            info.Once();

            info.Once(false);

            info.ShouldResolveOnce.Should().BeFalse();
        }
    }

    public class AsMethod
    {
        [Fact]
        public void As_WithString_SetsKey()
        {
            var info = new OnceInfo();

            info.As("my-cache-key");

            info.Key.Should().Be("my-cache-key");
        }

        [Fact]
        public void As_WithEnum_SetsEnumName()
        {
            var info = new OnceInfo();

            info.As(TestStatus.Active);

            info.Key.Should().Be("Active");
        }
    }

    public class Fresh
    {
        [Fact]
        public void Fresh_SetsShouldBeRefreshedTrue()
        {
            var info = new OnceInfo();

            info.Fresh();

            info.ShouldBeRefreshed.Should().BeTrue();
        }

        [Fact]
        public void Fresh_WithFalse_ClearsShouldBeRefreshed()
        {
            var info = new OnceInfo();
            info.Fresh();

            info.Fresh(false);

            info.ShouldBeRefreshed.Should().BeFalse();
        }
    }

    public class UntilMethod
    {
        [Fact]
        public void Until_WithTimeSpan_SetsExpiration()
        {
            var info = new OnceInfo();

            info.Until(TimeSpan.FromMinutes(5));

            info.ExpiresAt.Should().NotBeNull();
        }

        [Fact]
        public void Until_WithSeconds_SetsExpiration()
        {
            var info = new OnceInfo();

            info.Until(300);

            info.ExpiresAt.Should().NotBeNull();
        }

        [Fact]
        public void Until_WithDateTimeOffset_SetsExpiration()
        {
            var info = new OnceInfo();
            var futureTime = DateTimeOffset.UtcNow.AddMinutes(10);

            info.Until(futureTime);

            info.ExpiresAt.Should().NotBeNull();
            var expectedMs = futureTime.ToUnixTimeMilliseconds();
            info.ExpiresAt!.Value.Should().BeCloseTo(expectedMs, 1000);
        }
    }

    public class ExpiresAtProperty
    {
        [Fact]
        public void ExpiresAt_WhenNoTtl_ReturnsNull()
        {
            var info = new OnceInfo();

            info.ExpiresAt.Should().BeNull();
        }

        [Fact]
        public void ExpiresAt_WhenTtlSet_ReturnsMillisecondsTimestamp()
        {
            var info = new OnceInfo();
            var ttl = TimeSpan.FromMinutes(10);
            var nowMs = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeMilliseconds();

            info.Until(ttl);

            var expiresAt = info.ExpiresAt;
            expiresAt.Should().NotBeNull();
            expiresAt!.Value.Should().BeGreaterThanOrEqualTo(nowMs);
            expiresAt.Value.Should().BeLessThan(nowMs + 1000); // 1 second tolerance
        }
    }
}
