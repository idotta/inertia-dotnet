using FluentAssertions;
using Inertia.Core;

namespace Inertia.Tests;

public class InertiaHeadersTests
{
    [Fact]
    public void InertiaHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.Inertia.Should().Be("X-Inertia");
    }

    [Fact]
    public void VersionHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.Version.Should().Be("X-Inertia-Version");
    }

    [Fact]
    public void PartialDataHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.PartialData.Should().Be("X-Inertia-Partial-Data");
    }

    [Fact]
    public void PartialComponentHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.PartialComponent.Should().Be("X-Inertia-Partial-Component");
    }

    [Fact]
    public void PartialExceptHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.PartialExcept.Should().Be("X-Inertia-Partial-Except");
    }

    [Fact]
    public void ErrorBagHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.ErrorBag.Should().Be("X-Inertia-Error-Bag");
    }

    [Fact]
    public void LocationHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.Location.Should().Be("X-Inertia-Location");
    }

    [Fact]
    public void ResetHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.Reset.Should().Be("X-Inertia-Reset");
    }

    [Fact]
    public void InfiniteScrollMergeIntentHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.InfiniteScrollMergeIntent.Should().Be("X-Inertia-Infinite-Scroll-Merge-Intent");
    }

    [Fact]
    public void ExceptOncePropsHeader_ShouldHaveCorrectValue()
    {
        InertiaHeaders.ExceptOnceProps.Should().Be("X-Inertia-Except-Once-Props");
    }
}
