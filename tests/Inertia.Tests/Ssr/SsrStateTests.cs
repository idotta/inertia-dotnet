using FluentAssertions;
using Inertia.AspNetCore;
using NSubstitute;

namespace Inertia.Tests.Ssr;

public class SsrStateTests
{
    private static InertiaPage CreatePage(string component = "Users/Index") => new()
    {
        Component = component,
        Props = new Dictionary<string, object?>(),
        Url = "/users",
        Version = "1.0",
    };

    public class SetPageTests
    {
        [Fact]
        public void SetPage_StoresPage()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);
            var page = CreatePage();

            state.SetPage(page);

            state.Page.Should().BeSameAs(page);
        }

        [Fact]
        public void Page_BeforeSet_ReturnsNull()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);

            state.Page.Should().BeNull();
        }
    }

    public class DispatchAsyncTests
    {
        [Fact]
        public async Task DispatchAsync_CallsGateway()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var ssrResponse = new SsrResponse("<title>Test</title>", "<div>App</div>");
            var page = CreatePage();
            gateway.DispatchAsync(page, Arg.Any<CancellationToken>()).Returns(ssrResponse);
            var state = new SsrState(gateway);
            state.SetPage(page);

            var result = await state.DispatchAsync();

            result.Should().BeSameAs(ssrResponse);
            await gateway.Received(1).DispatchAsync(page, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DispatchAsync_CachesResult()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var ssrResponse = new SsrResponse("head", "body");
            var page = CreatePage();
            gateway.DispatchAsync(page, Arg.Any<CancellationToken>()).Returns(ssrResponse);
            var state = new SsrState(gateway);
            state.SetPage(page);

            var first = await state.DispatchAsync();
            var second = await state.DispatchAsync();

            first.Should().BeSameAs(second);
            await gateway.Received(1).DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DispatchAsync_NullResult_Cached()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var page = CreatePage();
            gateway.DispatchAsync(page, Arg.Any<CancellationToken>()).Returns((SsrResponse?)null);
            var state = new SsrState(gateway);
            state.SetPage(page);

            var first = await state.DispatchAsync();
            var second = await state.DispatchAsync();

            first.Should().BeNull();
            second.Should().BeNull();
            await gateway.Received(1).DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DispatchAsync_BeforeSetPage_ReturnsNull()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);

            var result = await state.DispatchAsync();

            result.Should().BeNull();
            await gateway.DidNotReceive().DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>());
        }
    }

    public class ExcludePathsTests
    {
        [Fact]
        public void ExcludePaths_ExactMatch()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);

            state.ExcludePaths("/admin");

            state.IsPathExcluded("/admin").Should().BeTrue();
        }

        [Fact]
        public void ExcludePaths_WildcardMatch()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);

            state.ExcludePaths("/api/*");

            state.IsPathExcluded("/api/users").Should().BeTrue();
        }

        [Fact]
        public void ExcludePaths_WildcardNoMatch()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);

            state.ExcludePaths("/api/*");

            state.IsPathExcluded("/admin").Should().BeFalse();
        }

        [Fact]
        public void ExcludePaths_CaseInsensitive()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);

            state.ExcludePaths("/Admin");

            state.IsPathExcluded("/admin").Should().BeTrue();
        }

        [Fact]
        public void IsPathExcluded_NoPaths_ReturnsFalse()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);

            state.IsPathExcluded("/anything").Should().BeFalse();
        }

        [Fact]
        public void IsPathExcluded_FullUrlPattern_MatchesPathPortion()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);

            state.ExcludePaths("https://example.com/admin");

            state.IsPathExcluded("/admin").Should().BeTrue();
        }

        [Fact]
        public void IsPathExcluded_FullUrlWithWildcard_MatchesPathPortion()
        {
            var gateway = Substitute.For<ISsrGateway>();
            var state = new SsrState(gateway);

            state.ExcludePaths("https://example.com/api/*");

            state.IsPathExcluded("/api/users").Should().BeTrue();
            state.IsPathExcluded("/api/posts").Should().BeTrue();
            state.IsPathExcluded("/other").Should().BeFalse();
        }
    }
}
