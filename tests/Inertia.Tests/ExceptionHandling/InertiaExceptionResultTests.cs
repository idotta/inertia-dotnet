using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests;

public class InertiaExceptionResultTests
{
    private sealed class TestPropertyProvider : IInertiaPropertyProvider
    {
        private readonly Dictionary<string, object?> _props;

        public TestPropertyProvider(Dictionary<string, object?> props) => _props = props;

        public IEnumerable<KeyValuePair<string, object?>> ToInertiaProperties(RenderContext context)
            => _props;
    }

    public class Render
    {
        [Fact]
        public void Render_SetsComponentAndProps()
        {
            var result = InertiaExceptionResult.Render("Error", new { status = 500 });

            result.Component.Should().Be("Error");
            result.Props.Should().ContainKey("status");
        }

        [Fact]
        public void Render_WithDictionary_SetsProps()
        {
            var props = new Dictionary<string, object?> { ["status"] = 404, ["message"] = "Not Found" };

            var result = InertiaExceptionResult.Render("Error", props);

            result.Component.Should().Be("Error");
            result.Props!["status"].Should().Be(404);
            result.Props!["message"].Should().Be("Not Found");
        }

        [Fact]
        public void Render_NullComponent_ThrowsArgumentException()
        {
            var act = () => InertiaExceptionResult.Render(null!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Render_EmptyComponent_ThrowsArgumentException()
        {
            var act = () => InertiaExceptionResult.Render("");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Render_WhitespaceComponent_ThrowsArgumentException()
        {
            var act = () => InertiaExceptionResult.Render("   ");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Render_WithPropertyProvider_WrapsAsNumericKey()
        {
            var provider = new TestPropertyProvider(new Dictionary<string, object?>
            {
                ["auth"] = "data",
                ["errors"] = new Dictionary<string, object?>(),
            });

            var result = InertiaExceptionResult.Render("Error", (object)provider);

            result.Props.Should().ContainKey("0");
            result.Props!["0"].Should().BeSameAs(provider);
        }
    }

    public class RedirectTests
    {
        [Fact]
        public void Redirect_SetsRedirectUrl()
        {
            var result = InertiaExceptionResult.Redirect("/login");

            result.RedirectUrl.Should().Be("/login");
            result.Component.Should().BeNull();
        }

        [Fact]
        public void Redirect_NullUrl_ThrowsArgumentException()
        {
            var act = () => InertiaExceptionResult.Redirect(null!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Redirect_EmptyUrl_ThrowsArgumentException()
        {
            var act = () => InertiaExceptionResult.Redirect("");

            act.Should().Throw<ArgumentException>();
        }
    }

    public class Fluent
    {
        [Fact]
        public void WithSharedData_SetsFlag()
        {
            var result = InertiaExceptionResult.Render("Error").WithSharedData();

            result.IncludeSharedData.Should().BeTrue();
        }

        [Fact]
        public void RootView_SetsCustomRootView()
        {
            var result = InertiaExceptionResult.Render("Error").RootView("~/Views/Custom.cshtml");

            result.CustomRootView.Should().Be("~/Views/Custom.cshtml");
        }
    }
}
