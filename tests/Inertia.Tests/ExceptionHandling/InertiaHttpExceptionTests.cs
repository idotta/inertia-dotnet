using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.ExceptionHandling;

public class InertiaHttpExceptionTests
{
    public class Constructor
    {
        [Fact]
        public void StatusCodeOnly_SetsStatusCode()
        {
            var ex = new InertiaHttpException(403);

            ex.StatusCode.Should().Be(403);
            ex.Message.Should().Be("HTTP 403");
        }

        [Fact]
        public void WithMessage_SetsMessageAndStatusCode()
        {
            var ex = new InertiaHttpException(404, "Page not found");

            ex.StatusCode.Should().Be(404);
            ex.Message.Should().Be("Page not found");
        }

        [Fact]
        public void WithInnerException_SetsAll()
        {
            var inner = new InvalidOperationException("inner");
            var ex = new InertiaHttpException(503, "Service unavailable", inner);

            ex.StatusCode.Should().Be(503);
            ex.Message.Should().Be("Service unavailable");
            ex.InnerException.Should().BeSameAs(inner);
        }
    }
}
