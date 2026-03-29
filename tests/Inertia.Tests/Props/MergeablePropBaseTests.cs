using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Props;

public class MergeablePropBaseTests
{
    private sealed class TestMergeableProp : MergeablePropBase { }

    public class Defaults
    {
        [Fact]
        public void ShouldMerge_DefaultsFalse()
        {
            var prop = new TestMergeableProp();

            prop.ShouldMerge.Should().BeFalse();
        }

        [Fact]
        public void ShouldDeepMerge_DefaultsFalse()
        {
            var prop = new TestMergeableProp();

            prop.ShouldDeepMerge.Should().BeFalse();
        }

        [Fact]
        public void MatchesOn_DefaultsEmpty()
        {
            var prop = new TestMergeableProp();

            prop.MatchesOn.Should().BeEmpty();
        }

        [Fact]
        public void AppendsAtRoot_DefaultsTrue()
        {
            var prop = new TestMergeableProp();

            prop.AppendsAtRoot.Should().BeTrue(
                because: "append defaults to true and no paths are configured");
        }

        [Fact]
        public void PrependsAtRoot_DefaultsFalse()
        {
            var prop = new TestMergeableProp();

            prop.PrependsAtRoot.Should().BeFalse();
        }

        [Fact]
        public void AppendsAtPaths_DefaultsEmpty()
        {
            var prop = new TestMergeableProp();

            prop.AppendsAtPaths.Should().BeEmpty();
        }

        [Fact]
        public void PrependsAtPaths_DefaultsEmpty()
        {
            var prop = new TestMergeableProp();

            prop.PrependsAtPaths.Should().BeEmpty();
        }
    }

    public class Merge
    {
        [Fact]
        public void Merge_SetsShouldMergeTrue()
        {
            var prop = new TestMergeableProp();

            prop.Merge();

            prop.ShouldMerge.Should().BeTrue();
        }

        [Fact]
        public void Merge_ReturnsSelf()
        {
            var prop = new TestMergeableProp();

            var result = prop.Merge();

            result.Should().BeSameAs(prop);
        }
    }

    public class DeepMerge
    {
        [Fact]
        public void DeepMerge_SetsShouldDeepMergeTrue()
        {
            var prop = new TestMergeableProp();

            prop.DeepMerge();

            prop.ShouldDeepMerge.Should().BeTrue();
        }

        [Fact]
        public void DeepMerge_AlsoSetsMergeTrue()
        {
            var prop = new TestMergeableProp();

            prop.DeepMerge();

            prop.ShouldMerge.Should().BeTrue(
                because: "DeepMerge internally calls Merge()");
        }
    }

    public class MatchOn
    {
        [Fact]
        public void MatchOn_WithString_SetsMatchesOn()
        {
            var prop = new TestMergeableProp();

            prop.MatchOn("id");

            prop.MatchesOn.Should().ContainSingle().Which.Should().Be("id");
        }

        [Fact]
        public void MatchOn_WithEnumerable_SetsMatchesOn()
        {
            var prop = new TestMergeableProp();

            prop.MatchOn(new[] { "id", "slug" });

            prop.MatchesOn.Should().Equal("id", "slug");
        }

        [Fact]
        public void MatchOn_ReplacesExistingValues()
        {
            var prop = new TestMergeableProp();

            prop.MatchOn("old");
            prop.MatchOn("new");

            prop.MatchesOn.Should().ContainSingle().Which.Should().Be("new");
        }
    }

    public class Append
    {
        [Fact]
        public void Append_WithBoolTrue_SetsAppendFlag()
        {
            var prop = new TestMergeableProp();

            // First set to prepend, then back to append
            prop.Prepend();
            prop.Append(true);

            prop.AppendsAtRoot.Should().BeTrue();
            prop.PrependsAtRoot.Should().BeFalse();
        }

        [Fact]
        public void Append_WithString_AddsToAppendsAtPaths()
        {
            var prop = new TestMergeableProp();

            prop.Append("items");

            prop.AppendsAtPaths.Should().ContainSingle().Which.Should().Be("items");
        }

        [Fact]
        public void Append_WithStringAndMatchOn_AddsPathAndUpdatesMatchOn()
        {
            var prop = new TestMergeableProp();

            prop.Append("items", "id");

            prop.AppendsAtPaths.Should().ContainSingle().Which.Should().Be("items");
            prop.MatchesOn.Should().ContainSingle().Which.Should().Be("items.id");
        }

        [Fact]
        public void Append_WithEnumerable_AddsAllPaths()
        {
            var prop = new TestMergeableProp();

            prop.Append(new[] { "items", "tags" });

            prop.AppendsAtPaths.Should().Equal("items", "tags");
        }

        [Fact]
        public void Append_WithDictionary_AddsPathsAndMatchOns()
        {
            var prop = new TestMergeableProp();

            prop.Append(new Dictionary<string, string>
            {
                ["items"] = "id",
                ["tags"] = "slug",
            });

            prop.AppendsAtPaths.Should().Equal("items", "tags");
            prop.MatchesOn.Should().Equal("items.id", "tags.slug");
        }

        [Fact]
        public void Append_WithPath_DisablesRootAppend()
        {
            var prop = new TestMergeableProp();

            prop.Append("items");

            prop.AppendsAtRoot.Should().BeFalse(
                because: "root append is disabled when specific paths are configured");
        }
    }

    public class Prepend
    {
        [Fact]
        public void Prepend_WithBoolTrue_SetsPrependFlag()
        {
            var prop = new TestMergeableProp();

            prop.Prepend();

            prop.PrependsAtRoot.Should().BeTrue();
            prop.AppendsAtRoot.Should().BeFalse(
                because: "Prepend(true) sets _append = false");
        }

        [Fact]
        public void Prepend_WithString_AddsToPrependsAtPaths()
        {
            var prop = new TestMergeableProp();

            prop.Prepend("items");

            prop.PrependsAtPaths.Should().ContainSingle().Which.Should().Be("items");
        }

        [Fact]
        public void Prepend_WithStringAndMatchOn_AddsPathAndUpdatesMatchOn()
        {
            var prop = new TestMergeableProp();

            prop.Prepend("items", "id");

            prop.PrependsAtPaths.Should().ContainSingle().Which.Should().Be("items");
            prop.MatchesOn.Should().ContainSingle().Which.Should().Be("items.id");
        }

        [Fact]
        public void Prepend_WithPath_DisablesRootPrepend()
        {
            var prop = new TestMergeableProp();

            prop.Prepend();
            prop.Prepend("items");

            prop.PrependsAtRoot.Should().BeFalse(
                because: "root prepend is disabled when specific paths are configured");
        }
    }
}
