using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Props;

public class DeferInfoTests
{
    public class Defaults
    {
        [Fact]
        public void Default_ShouldDefer_IsFalse()
        {
            var info = new DeferInfo();

            info.ShouldDefer.Should().BeFalse();
        }

        [Fact]
        public void Default_Group_IsDefault()
        {
            var info = new DeferInfo();

            info.Group.Should().Be("default");
        }
    }

    public class Defer
    {
        [Fact]
        public void Defer_WithoutGroup_SetsShouldDeferTrue()
        {
            var info = new DeferInfo();

            info.Defer();

            info.ShouldDefer.Should().BeTrue();
        }

        [Fact]
        public void Defer_WithoutGroup_KeepsDefaultGroup()
        {
            var info = new DeferInfo();

            info.Defer();

            info.Group.Should().Be("default");
        }

        [Fact]
        public void Defer_WithGroup_SetsCustomGroup()
        {
            var info = new DeferInfo();

            info.Defer("analytics");

            info.ShouldDefer.Should().BeTrue();
            info.Group.Should().Be("analytics");
        }

        [Fact]
        public void Defer_WithNullGroup_UsesDefaultGroup()
        {
            var info = new DeferInfo();

            info.Defer(null);

            info.Group.Should().Be("default");
        }

        [Fact]
        public void Defer_WithEmptyGroup_SetsEmptyGroup()
        {
            var info = new DeferInfo();

            info.Defer("");

            info.Group.Should().BeEmpty();
        }
    }
}
