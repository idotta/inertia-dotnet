using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Props;

public class ScrollMetadataTests
{
    public class Constructor
    {
        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var metadata = new ScrollMetadata("page", previousPage: 1, nextPage: 3, currentPage: 2);

            metadata.PageName.Should().Be("page");
            metadata.PreviousPage.Should().Be(1);
            metadata.NextPage.Should().Be(3);
            metadata.CurrentPage.Should().Be(2);
        }

        [Fact]
        public void Constructor_WithNullPages_SetsNull()
        {
            var metadata = new ScrollMetadata("page");

            metadata.PageName.Should().Be("page");
            metadata.PreviousPage.Should().BeNull();
            metadata.NextPage.Should().BeNull();
            metadata.CurrentPage.Should().BeNull();
        }

        [Fact]
        public void Constructor_NullPageName_Throws()
        {
            var act = () => new ScrollMetadata(null!);

            act.Should().Throw<ArgumentException>();
        }
    }

    public class ToDictionaryMethod
    {
        [Fact]
        public void ToDictionary_ReturnsExpectedKeys()
        {
            var metadata = new ScrollMetadata("page", previousPage: 1, nextPage: 3, currentPage: 2);

            var dict = metadata.ToDictionary();

            dict.Should().ContainKeys("pageName", "previousPage", "nextPage", "currentPage");
            dict["pageName"].Should().Be("page");
            dict["previousPage"].Should().Be(1);
            dict["nextPage"].Should().Be(3);
            dict["currentPage"].Should().Be(2);
        }

        [Fact]
        public void ToDictionary_WithIntPages_PreservesIntegers()
        {
            var metadata = new ScrollMetadata("page", previousPage: 5, nextPage: 7, currentPage: 6);

            var dict = metadata.ToDictionary();

            dict["previousPage"].Should().BeOfType<int>().And.Be(5);
            dict["nextPage"].Should().BeOfType<int>().And.Be(7);
            dict["currentPage"].Should().BeOfType<int>().And.Be(6);
        }
    }
}
