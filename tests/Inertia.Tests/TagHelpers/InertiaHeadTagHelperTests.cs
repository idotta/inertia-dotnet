using System.Text.Encodings.Web;
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

public class InertiaHeadTagHelperTests
{
    private static TagHelperContext CreateTagHelperContext() =>
        new("inertia-head", new TagHelperAttributeList(), new Dictionary<object, object>(), "test");

    private static TagHelperOutput CreateTagHelperOutput(Func<bool, HtmlEncoder?, Task<TagHelperContent>>? getChildContentAsync = null) =>
        new("inertia-head", new TagHelperAttributeList(),
            getChildContentAsync ?? ((useCachedResult, encoder) =>
                Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())));

    private static InertiaHeadTagHelper CreateTagHelper(
        SsrResponse? ssrResponse = null)
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>())
            .Returns(ssrResponse);

        var ssrState = new SsrState(gateway);
        if (ssrResponse is not null)
        {
            ssrState.SetPage(new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?>(),
                Url = "/test",
                Version = "1.0",
            });
        }

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(ssrState)
            .BuildServiceProvider();

        return new InertiaHeadTagHelper
        {
            ViewContext = new ViewContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                Substitute.For<IView>(),
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                Substitute.For<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions()),
        };
    }

    // ---- Group 1: SSR Enabled ----
    public class SsrEnabled
    {
        [Fact]
        public async Task ProcessAsync_WithSsrResponse_OutputsSsrHead()
        {
            var tagHelper = CreateTagHelper(
                ssrResponse: new SsrResponse("<title>SSR Title</title>\n<meta name=\"desc\">", "body"));

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output);

            output.Content.GetContent().Should().Be("<title>SSR Title</title>\n<meta name=\"desc\">");
        }

        [Fact]
        public async Task ProcessAsync_WithSsrResponse_SuppressesTag()
        {
            var tagHelper = CreateTagHelper(
                ssrResponse: new SsrResponse("head", "body"));

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
        public async Task ProcessAsync_NoSsrResponse_OutputsChildContent()
        {
            var tagHelper = CreateTagHelper(ssrResponse: null);

            var context = CreateTagHelperContext();
            var childContent = new DefaultTagHelperContent();
            childContent.SetHtmlContent("<title>Default Title</title>");

            var output = CreateTagHelperOutput((_, _) => Task.FromResult<TagHelperContent>(childContent));

            await tagHelper.ProcessAsync(context, output);

            output.Content.GetContent().Should().Be("<title>Default Title</title>");
        }

        [Fact]
        public async Task ProcessAsync_NoSsrResponse_NoChildren_OutputsNothing()
        {
            var tagHelper = CreateTagHelper(ssrResponse: null);

            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput();

            await tagHelper.ProcessAsync(context, output);

            output.Content.GetContent().Should().BeEmpty();
        }
    }

    // ---- Group 3: Null SSR ----
    public class NullSsr
    {
        [Fact]
        public async Task ProcessAsync_SsrNull_OutputsChildContent()
        {
            // SSR state exists but dispatch returns null
            var gateway = Substitute.For<ISsrGateway>();
            gateway.DispatchAsync(Arg.Any<InertiaPage>(), Arg.Any<CancellationToken>())
                .Returns((SsrResponse?)null);

            var ssrState = new SsrState(gateway);
            ssrState.SetPage(new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?>(),
                Url = "/test",
                Version = "1.0",
            });

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(ssrState)
                .BuildServiceProvider();

            var tagHelper = new InertiaHeadTagHelper
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
            var childContent = new DefaultTagHelperContent();
            childContent.SetHtmlContent("<title>Fallback</title>");
            var output = CreateTagHelperOutput((_, _) => Task.FromResult<TagHelperContent>(childContent));

            await tagHelper.ProcessAsync(context, output);

            output.Content.GetContent().Should().Be("<title>Fallback</title>");
        }
    }
}
