using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Ssr;

public class SsrErrorTypeTests
{
    public class EnumValues
    {
        [Fact]
        public void HasExpectedValues()
        {
            Enum.GetNames<SsrErrorType>().Should().BeEquivalentTo(
                "Unknown", "BrowserApi", "ComponentResolution", "Render", "Connection");
        }
    }

    public class FromString
    {
        [Fact]
        public void FromString_BrowserApi_ReturnsBrowserApi()
        {
            SsrErrorTypeParser.FromString("browser-api").Should().Be(SsrErrorType.BrowserApi);
        }

        [Fact]
        public void FromString_ComponentResolution_ReturnsComponentResolution()
        {
            SsrErrorTypeParser.FromString("component-resolution").Should().Be(SsrErrorType.ComponentResolution);
        }

        [Fact]
        public void FromString_Render_ReturnsRender()
        {
            SsrErrorTypeParser.FromString("render").Should().Be(SsrErrorType.Render);
        }

        [Fact]
        public void FromString_Connection_ReturnsConnection()
        {
            SsrErrorTypeParser.FromString("connection").Should().Be(SsrErrorType.Connection);
        }

        [Fact]
        public void FromString_Null_ReturnsUnknown()
        {
            SsrErrorTypeParser.FromString(null).Should().Be(SsrErrorType.Unknown);
        }

        [Fact]
        public void FromString_InvalidString_ReturnsUnknown()
        {
            SsrErrorTypeParser.FromString("foo").Should().Be(SsrErrorType.Unknown);
        }
    }
}
