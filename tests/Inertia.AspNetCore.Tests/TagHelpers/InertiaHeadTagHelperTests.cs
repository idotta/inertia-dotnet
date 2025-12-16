using System.Text;
using Inertia.AspNetCore.TagHelpers;
using Inertia.Core;
using Inertia.Core.Ssr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Inertia.AspNetCore.Tests.TagHelpers;

public class InertiaHeadTagHelperTests
{
    private readonly InertiaOptions _options;
    private readonly Mock<IGateway> _mockGateway;

    public InertiaHeadTagHelperTests()
    {
        _options = new InertiaOptions
        {
            Ssr = new SsrOptions { Enabled = false }
        };
        _mockGateway = new Mock<IGateway>();
    }

    private (InertiaHeadTagHelper tagHelper, TagHelperContext context, TagHelperOutput output) CreateTagHelper(
        Dictionary<string, object?>? pageData = null,
        IGateway? gateway = null)
    {
        var tagHelper = new InertiaHeadTagHelper(Options.Create(_options), gateway)
        {
            ViewContext = CreateViewContext(pageData)
        };

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());

        var output = new TagHelperOutput(
            "inertia-head",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        return (tagHelper, context, output);
    }

    private ViewContext CreateViewContext(Dictionary<string, object?>? pageData = null)
    {
        var httpContext = new DefaultHttpContext();
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        
        if (pageData != null)
        {
            viewData["page"] = pageData;
        }

        return new ViewContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            Mock.Of<IView>(),
            viewData,
            Mock.Of<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());
    }

    [Fact]
    public async Task ProcessAsync_WithNoPageData_SuppressesOutput()
    {
        // Arrange
        var (tagHelper, context, output) = CreateTagHelper();

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.True(output.IsContentModified);
        Assert.Empty(output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_WithSsrDisabled_SuppressesOutput()
    {
        // Arrange
        _options.Ssr.Enabled = false;
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Dashboard",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/dashboard",
            ["version"] = "abc123"
        };

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: _mockGateway.Object);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Empty(output.Content.GetContent());
        _mockGateway.Verify(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WithNoGateway_SuppressesOutput()
    {
        // Arrange
        _options.Ssr.Enabled = true;
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Dashboard",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/dashboard",
            ["version"] = "abc123"
        };

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: null);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Empty(output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_WithSsrResponse_RendersHeadContent()
    {
        // Arrange
        _options.Ssr.Enabled = true;
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Dashboard",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/dashboard",
            ["version"] = "abc123"
        };

        var headContent = "<title>Dashboard - My App</title>\n<meta name=\"description\" content=\"Dashboard page\">";
        var ssrResponse = new SsrResponse(headContent, "<div>Body content</div>");

        _mockGateway.Setup(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(ssrResponse);

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: _mockGateway.Object);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Null(output.TagName); // Tag wrapper should be removed
        var content = output.Content.GetContent();
        Assert.Contains("<title>Dashboard - My App</title>", content);
        Assert.Contains("<meta name=\"description\"", content);
    }

    [Fact]
    public async Task ProcessAsync_WhenSsrFails_SuppressesOutput()
    {
        // Arrange
        _options.Ssr.Enabled = true;
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Dashboard",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/dashboard",
            ["version"] = "abc123"
        };

        _mockGateway.Setup(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync((SsrResponse?)null);

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: _mockGateway.Object);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Empty(output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_WhenSsrThrows_SuppressesOutput()
    {
        // Arrange
        _options.Ssr.Enabled = true;
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Dashboard",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/dashboard",
            ["version"] = "abc123"
        };

        _mockGateway.Setup(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()))
            .ThrowsAsync(new Exception("SSR error"));

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: _mockGateway.Object);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Empty(output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyHeadContent_SuppressesOutput()
    {
        // Arrange
        _options.Ssr.Enabled = true;
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Dashboard",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/dashboard",
            ["version"] = "abc123"
        };

        var ssrResponse = new SsrResponse("", "<div>Body content</div>");

        _mockGateway.Setup(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(ssrResponse);

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: _mockGateway.Object);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Empty(output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_WithWhitespaceHeadContent_SuppressesOutput()
    {
        // Arrange
        _options.Ssr.Enabled = true;
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Dashboard",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/dashboard",
            ["version"] = "abc123"
        };

        var ssrResponse = new SsrResponse("   \n  ", "<div>Body content</div>");

        _mockGateway.Setup(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(ssrResponse);

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: _mockGateway.Object);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Empty(output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_PassesCorrectPageDataToGateway()
    {
        // Arrange
        _options.Ssr.Enabled = true;
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Users/Show",
            ["props"] = new Dictionary<string, object?> { ["userId"] = 42 },
            ["url"] = "/users/42",
            ["version"] = "xyz789"
        };

        var ssrResponse = new SsrResponse("<title>User 42</title>", "<div>User content</div>");

        Dictionary<string, object?>? capturedPageData = null;
        _mockGateway.Setup(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()))
            .Callback<Dictionary<string, object?>>(data => capturedPageData = data)
            .ReturnsAsync(ssrResponse);

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: _mockGateway.Object);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.NotNull(capturedPageData);
        Assert.Equal("Users/Show", capturedPageData!["component"]);
        Assert.Equal("xyz789", capturedPageData["version"]);
        _mockGateway.Verify(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()), Times.Once);
    }
}
