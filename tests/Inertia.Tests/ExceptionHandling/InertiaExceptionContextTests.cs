using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Inertia.Tests;

public class InertiaExceptionContextTests
{
    public class Construction
    {
        [Fact]
        public void Properties_CanBeSetViaInit()
        {
            var exception = new InvalidOperationException("test");
            var httpContext = new DefaultHttpContext();

            var context = new InertiaExceptionContext
            {
                Exception = exception,
                HttpContext = httpContext,
                StatusCode = 500,
            };

            context.Exception.Should().BeSameAs(exception);
            context.HttpContext.Should().BeSameAs(httpContext);
            context.StatusCode.Should().Be(500);
        }

        [Fact]
        public void StatusCode_ReflectsAssignedValue()
        {
            var context = new InertiaExceptionContext
            {
                Exception = new Exception(),
                HttpContext = new DefaultHttpContext(),
                StatusCode = 404,
            };

            context.StatusCode.Should().Be(404);
        }
    }
}
