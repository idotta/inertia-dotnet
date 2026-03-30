using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Inertia.Tests;

public class InertiaFactoryTests
{
    private static (InertiaFactory Factory, DefaultHttpContext HttpContext, ITempDataDictionary TempData) CreateFactory(
        Action<InertiaOptions>? configure = null)
    {
        var options = new InertiaOptions();
        configure?.Invoke(options);
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var tempData = Substitute.For<ITempDataDictionary>();
        var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
        tempDataFactory.GetTempData(httpContext).Returns(tempData);
        var factory = new InertiaFactory(Options.Create(options), accessor, tempDataFactory);
        return (factory, httpContext, tempData);
    }

    public class Share
    {
        [Fact]
        public void Share_StringKey_StoresValue()
        {
            var (factory, _, _) = CreateFactory();

            factory.Share("key", "value");

            factory.GetShared().Should().ContainKey("key")
                .WhoseValue.Should().Be("value");
        }

        [Fact]
        public void Share_StringKey_OverwritesExistingKey()
        {
            var (factory, _, _) = CreateFactory();

            factory.Share("key", "first");
            factory.Share("key", "second");

            factory.GetShared()["key"].Should().Be("second");
        }

        [Fact]
        public void Share_Dictionary_MergesAll()
        {
            var (factory, _, _) = CreateFactory();

            factory.Share(new Dictionary<string, object?>
            {
                ["a"] = 1,
                ["b"] = 2,
            });

            var shared = factory.GetShared();
            shared.Should().ContainKey("a").WhoseValue.Should().Be(1);
            shared.Should().ContainKey("b").WhoseValue.Should().Be(2);
        }

        [Fact]
        public void Share_Provider_StoresProviderReference()
        {
            var (factory, _, _) = CreateFactory();
            var provider = Substitute.For<IInertiaPropertyProvider>();

            factory.Share(provider);

            factory.GetSharedProviders().Should().ContainSingle()
                .Which.Should().BeSameAs(provider);
        }

        [Fact]
        public void GetShared_ReturnsAllSharedProps()
        {
            var (factory, _, _) = CreateFactory();
            factory.Share("x", 10);
            factory.Share("y", 20);

            var shared = factory.GetShared();

            shared.Should().HaveCount(2);
            shared["x"].Should().Be(10);
            shared["y"].Should().Be(20);
        }

        [Fact]
        public void FlushShared_ClearsAll()
        {
            var (factory, _, _) = CreateFactory();
            factory.Share("key", "value");
            factory.Share(Substitute.For<IInertiaPropertyProvider>());

            factory.FlushShared();

            factory.GetShared().Should().BeEmpty();
            factory.GetSharedProviders().Should().BeEmpty();
        }
    }

    public class ClearHistoryTests
    {
        [Fact]
        public void ClearHistory_SetsFlag()
        {
            var (factory, _, _) = CreateFactory();

            factory.ClearHistory();

            factory.GetClearHistory().Should().BeTrue();
        }

        [Fact]
        public void GetClearHistory_DefaultsFalse()
        {
            var (factory, _, _) = CreateFactory();

            factory.GetClearHistory().Should().BeFalse();
        }
    }

    public class PreserveFragmentTests
    {
        [Fact]
        public void PreserveFragment_SetsFlag()
        {
            var (factory, _, _) = CreateFactory();

            factory.PreserveFragment();

            factory.GetPreserveFragment().Should().BeTrue();
        }
    }

    public class EncryptHistoryTests
    {
        [Fact]
        public void EncryptHistory_DefaultTrue_SetsFlag()
        {
            var (factory, _, _) = CreateFactory();

            factory.EncryptHistory();

            factory.GetEncryptHistory().Should().BeTrue();
        }

        [Fact]
        public void EncryptHistory_WithFalse_UnsetsFlag()
        {
            var (factory, _, _) = CreateFactory();

            factory.EncryptHistory(false);

            factory.GetEncryptHistory().Should().BeFalse();
        }
    }

    public class Render
    {
        [Fact]
        public void Render_WithNullProps_ReturnsResponse()
        {
            var (factory, _, _) = CreateFactory();

            var response = factory.Render("Test/Page");

            response.Should().NotBeNull();
            response.Should().BeOfType<InertiaResponse>();
        }

        [Fact]
        public void Render_WithDictionaryProps_ReturnsResponse()
        {
            var (factory, _, _) = CreateFactory();
            var props = new Dictionary<string, object?> { ["name"] = "test" };

            var response = factory.Render("Test/Page", props);

            response.Should().NotBeNull();
            response.Should().BeOfType<InertiaResponse>();
        }

        [Fact]
        public void Render_WithAnonymousObject_ReflectsToDict()
        {
            var (factory, _, _) = CreateFactory();

            var response = factory.Render("Test/Page", new { Name = "test", Age = 30 });

            response.Should().NotBeNull();
            response.Props.Should().ContainKey("Name").WhoseValue.Should().Be("test");
            response.Props.Should().ContainKey("Age").WhoseValue.Should().Be(30);
        }

        [Fact]
        public void Render_SetsComponentOnResponse()
        {
            var (factory, _, _) = CreateFactory();

            var response = factory.Render("Users/Index");

            response.Component.Should().Be("Users/Index");
        }

        [Fact]
        public void Render_NullComponent_ThrowsArgumentException()
        {
            var (factory, _, _) = CreateFactory();

            var act = () => factory.Render(null!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Render_WithIInertiaPropertyProvider_WrapsAsNumericKey()
        {
            var (factory, _, _) = CreateFactory();
            var provider = Substitute.For<IInertiaPropertyProvider>();

            var response = factory.Render("Test/Page", (object)provider);

            response.Props.Should().ContainKey("0").WhoseValue.Should().BeSameAs(provider);
        }
    }

    public class LocationTests
    {
        [Fact]
        public void Location_ReturnsInertiaLocationResult()
        {
            var (factory, _, _) = CreateFactory();

            var result = factory.Location("https://example.com");

            result.Should().BeOfType<InertiaLocationResult>();
        }

        [Fact]
        public void Location_SetsUrlOnResult()
        {
            var (factory, _, _) = CreateFactory();

            var result = factory.Location("https://example.com/login");

            result.Url.Should().Be("https://example.com/login");
        }
    }

    public class Internal
    {
        [Fact]
        public void SetVersion_OverridesOptionsVersion()
        {
            var (factory, _, _) = CreateFactory(o => o.VersionProvider = _ => "from-options");

            factory.SetVersion("override-version");

            factory.GetVersion().Should().Be("override-version");
        }

        [Fact]
        public void SetRootView_OverridesOptionsRootView()
        {
            var (factory, _, _) = CreateFactory(o => o.RootView = "~/Views/Default.cshtml");

            factory.SetRootView("~/Views/Custom.cshtml");

            factory.GetRootView().Should().Be("~/Views/Custom.cshtml");
        }

        [Fact]
        public void GetVersion_DefaultsToEmpty()
        {
            var (factory, _, _) = CreateFactory();

            factory.GetVersion().Should().BeEmpty();
        }

        [Fact]
        public void GetRootView_DefaultsToOptionsValue()
        {
            var (factory, _, _) = CreateFactory(o => o.RootView = "~/Views/MyApp.cshtml");

            factory.GetRootView().Should().Be("~/Views/MyApp.cshtml");
        }
    }
}
