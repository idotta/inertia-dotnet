using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Ssr;

public class SsrResponseTests
{
    public class RecordBehavior
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            var response = new SsrResponse("<title>Test</title>", "<div>Hello</div>");

            response.Head.Should().Be("<title>Test</title>");
            response.Body.Should().Be("<div>Hello</div>");
        }

        [Fact]
        public void Equality_SameValues_AreEqual()
        {
            var a = new SsrResponse("head", "body");
            var b = new SsrResponse("head", "body");

            a.Should().Be(b);
        }

        [Fact]
        public void Equality_DifferentValues_AreNotEqual()
        {
            var a = new SsrResponse("head1", "body");
            var b = new SsrResponse("head2", "body");

            a.Should().NotBe(b);
        }
    }
}
