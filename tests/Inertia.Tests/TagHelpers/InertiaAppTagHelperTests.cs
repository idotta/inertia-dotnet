using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Inertia.Tests.TagHelpers;

public class InertiaAppTagHelperTests
{
    private static TagHelperContext CreateTagHelperContext() =>
        new("inertia-app", new TagHelperAttributeList(), new Dictionary<object, object>(), "test");

    private static TagHelperOutput CreateTagHelperOutput() =>
        new("inertia-app", new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

    private static (InertiaAppTagHelper TagHelper, HttpContext HttpContext) CreateTagHelper(
        SsrResponse? ssrResponse = null,
        string? pageJson = null,
        string? requestPath = null)
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>())
            .Returns(ssrResponse);

        var ssrState = new SsrState(gateway);
        if (ssrResponse is not null)
        {
            // Set a page so dispatch will call the gateway
            ssrState.SetPage(new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?>(),
                Url = "/test",
                Version = "1.0",
            });
        }

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = requestPath ?? "/test";
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(ssrState)
            .BuildServiceProvider();

        if (pageJson is not null)
            httpContext.Items["InertiaPageJson"] = pageJson;

        var tagHelper = new InertiaAppTagHelper
        {
            ViewContext = new ViewContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                Substitute.For<IView>(),
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                Substitute.For<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions()),
        };

        return (tagHelper, httpContext);
    }

    // ---- Group 1: SSR Enabled ----
    public class SsrEnabled
    {
        [Fact]
        public async Task ProcessAsync_WithSsrResponse_OutputsSsrBody()
        {
            var (tagHelper, _) = CreateTagHelper(
                ssrResponse: new SsrResponse("<title>SSR</title>", "<div>SSR App</div>"),
                pageJson: """{"component":"Test"}""");

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output);

            output.Content.GetContent().Should().Be("<div>SSR App</div>");
        }

        [Fact]
        public async Task ProcessAsync_WithSsrResponse_SuppressesTag()
        {
            var (tagHelper, _) = CreateTagHelper(
                ssrResponse: new SsrResponse("head", "body"),
                pageJson: """{"component":"Test"}""");

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output);

            output.TagName.Should().BeNull();
        }
    }

    // ---- Group 2: CSR Fallback ----
    public class CsrFallback
    {
        [Fact]
        public async Task ProcessAsync_NoSsrResponse_OutputsScriptAndDiv()
        {
            var pageJson = """{"component":"Test","url":"/test"}""";
            var (tagHelper, _) = CreateTagHelper(pageJson: pageJson);

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output);

            var content = output.Content.GetContent();
            content.Should().Contain("""<script data-page="app" type="application/json">""");
            content.Should().Contain(pageJson);
            content.Should().Contain("</script>");
            content.Should().Contain("""<div id="app"></div>""");
        }

        [Fact]
        public async Task ProcessAsync_NoSsrResponse_IncludesPageJson()
        {
            var pageJson = """{"component":"Users/Index","url":"/users"}""";
            var (tagHelper, _) = CreateTagHelper(pageJson: pageJson);

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output);

            var content = output.Content.GetContent();
            content.Should().Contain("""<script data-page="app" type="application/json">""");
            content.Should().Contain("Users/Index");
        }

        [Fact]
        public async Task ProcessAsync_CustomId_UsesCustomId()
        {
            var pageJson = """{"component":"Test"}""";
            var (tagHelper, _) = CreateTagHelper(pageJson: pageJson);
            tagHelper.Id = "my-app";

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output);

            var content = output.Content.GetContent();
            content.Should().Contain("""<script data-page="my-app" type="application/json">""");
            content.Should().Contain("""<div id="my-app"></div>""");
        }

        [Fact]
        public async Task ProcessAsync_NullOrWhitespaceId_Throws()
        {
            var (tagHelper, _) = CreateTagHelper(
                pageJson: """{"component":"Test"}""");

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            tagHelper.Id = null!;
            var actNull = () => tagHelper.ProcessAsync(context, output);
            await actNull.Should().ThrowAsync<ArgumentException>();

            tagHelper.Id = "";
            var actEmpty = () => tagHelper.ProcessAsync(context, output);
            await actEmpty.Should().ThrowAsync<ArgumentException>();

            tagHelper.Id = "   ";
            var actWhitespace = () => tagHelper.ProcessAsync(context, output);
            await actWhitespace.Should().ThrowAsync<ArgumentException>();
        }
    }

    // ---- Group 3: No Page ----
    public class NoPage
    {
        [Fact]
        public async Task ProcessAsync_NoPage_SuppressesOutput()
        {
            var (tagHelper, _) = CreateTagHelper(pageJson: null);

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output);

            output.Content.GetContent().Should().BeEmpty();
            output.TagName.Should().BeNull();
        }
    }

    // ---- Group 4: Path Exclusion ----
    public class PathExclusion
    {
        [Fact]
        public async Task ProcessAsync_ExcludedPath_SkipsSsr()
        {
            var gateway = Substitute.For<ISsrGateway>();
            gateway.DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>())
                .Returns(new SsrResponse("head", "body"));

            var ssrState = new SsrState(gateway);
            ssrState.SetPage(new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?>(),
                Url = "/admin/dashboard",
                Version = "1.0",
            });
            ssrState.ExcludePaths("/admin/*");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/admin/dashboard";
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(ssrState)
                .BuildServiceProvider();
            httpContext.Items["InertiaPageJson"] = """{"component":"Test"}""";

            var tagHelper = new InertiaAppTagHelper
            {
                ViewContext = new ViewContext(
                    new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                    Substitute.For<IView>(),
                    new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                    Substitute.For<ITempDataDictionary>(),
                    TextWriter.Null,
                    new HtmlHelperOptions()),
            };

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output);

            // Should fall back to CSR (script + div), not SSR body
            var content = output.Content.GetContent();
            content.Should().Contain("""<script data-page="app" type="application/json">""");
            content.Should().Contain("""<div id="app"></div>""");
            content.Should().NotBe("body");
            await gateway.DidNotReceive().DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessAsync_SsrDispatched_CachesResult()
        {
            var gateway = Substitute.For<ISsrGateway>();
            gateway.DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>())
                .Returns(new SsrResponse("head", "<div>Cached</div>"));

            var ssrState = new SsrState(gateway);
            ssrState.SetPage(new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?>(),
                Url = "/test",
                Version = "1.0",
            });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/test";
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(ssrState)
                .BuildServiceProvider();
            httpContext.Items["InertiaPageJson"] = """{"component":"Test"}""";

            var tagHelper = new InertiaAppTagHelper
            {
                ViewContext = new ViewContext(
                    new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                    Substitute.For<IView>(),
                    new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                    Substitute.For<ITempDataDictionary>(),
                    TextWriter.Null,
                    new HtmlHelperOptions()),
            };

            var context = CreateTagHelperContext();
            var output1 = CreateTagHelperOutput();
            var output2 = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output1);
            await tagHelper.ProcessAsync(context, output2);

            // Gateway should only be called once (cached by SsrState)
            await gateway.Received(1).DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>());
            output1.Content.GetContent().Should().Be("<div>Cached</div>");
            output2.Content.GetContent().Should().Be("<div>Cached</div>");
        }
    }
}
