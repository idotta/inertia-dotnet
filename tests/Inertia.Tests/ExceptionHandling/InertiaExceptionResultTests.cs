using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests;

public class InertiaExceptionResultTests
{
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
