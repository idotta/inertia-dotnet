using System.Text;
using System.Text.Json;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Inertia.AspNetCore.Tests.TagHelpers;

public class InertiaTagHelperTests
{
    private readonly InertiaOptions _options;
    private readonly Mock<IGateway> _mockGateway;

    public InertiaTagHelperTests()
    {
        _options = new InertiaOptions
        {
            Ssr = new SsrOptions { Enabled = false }
        };
        _mockGateway = new Mock<IGateway>();
    }

    private (InertiaTagHelper tagHelper, TagHelperContext context, TagHelperOutput output) CreateTagHelper(
        Dictionary<string, object?>? pageData = null,
        string id = "app",
        IGateway? gateway = null)
    {
        var tagHelper = new InertiaTagHelper(Options.Create(_options), gateway)
        {
            Id = id,
            ViewContext = CreateViewContext(pageData)
        };

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());

        var output = new TagHelperOutput(
            "inertia",
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
    public async Task ProcessAsync_WithNoPageData_RendersEmptyDiv()
    {
        // Arrange
        var (tagHelper, context, output) = CreateTagHelper();

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("div", output.TagName);
        Assert.Equal("app", output.Attributes["id"]?.Value);
        Assert.Empty(output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_WithPageData_RendersDataPageAttribute()
    {
        // Arrange
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Users/Index",
            ["props"] = new Dictionary<string, object?> { ["users"] = new[] { "John", "Jane" } },
            ["url"] = "/users",
            ["version"] = "abc123"
        };

        var (tagHelper, context, output) = CreateTagHelper(pageData);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("div", output.TagName);
        Assert.Equal("app", output.Attributes["id"]?.Value);

        var dataPageAttr = output.Attributes["data-page"];
        Assert.NotNull(dataPageAttr);

        var json = dataPageAttr.Value as string;
        Assert.NotNull(json);

        // Verify it's valid JSON
        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json!);
        Assert.NotNull(parsed);
        Assert.Equal("Users/Index", parsed!["component"].GetString());
    }

    [Fact]
    public async Task ProcessAsync_WithCustomId_UsesCustomId()
    {
        // Arrange
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Dashboard",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/dashboard",
            ["version"] = "abc123"
        };

        var (tagHelper, context, output) = CreateTagHelper(pageData, id: "custom-app");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("custom-app", output.Attributes["id"]?.Value);
    }

    [Fact]
    public async Task ProcessAsync_WithUseScriptElement_RendersScriptTag()
    {
        // Arrange
        _options.UseScriptElement = true;
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Dashboard",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/dashboard",
            ["version"] = "abc123"
        };

        var (tagHelper, context, output) = CreateTagHelper(pageData);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Null(output.TagName); // Tag wrapper should be removed

        var content = output.Content.GetContent();
        Assert.Contains("<script data-page=\"app\" type=\"application/json\">", content);
        Assert.Contains("</script>", content);
        Assert.Contains("<div id=\"app\"></div>", content);
    }

    [Fact]
    public async Task ProcessAsync_WithSsr_RendersSsrBody()
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

        var ssrResponse = new SsrResponse(
            "<title>Dashboard</title>",
            "<div id=\"app\" data-server-rendered=\"true\"><h1>Dashboard</h1></div>");

        _mockGateway.Setup(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(ssrResponse);

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: _mockGateway.Object);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Null(output.TagName); // Tag wrapper should be removed for SSR
        var content = output.Content.GetContent();
        Assert.Contains("<h1>Dashboard</h1>", content);
        Assert.Contains("data-server-rendered=\"true\"", content);
    }

    [Fact]
    public async Task ProcessAsync_WithSsrDisabled_FallbacksToDataPageAttribute()
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
        Assert.Equal("div", output.TagName);
        Assert.NotNull(output.Attributes["data-page"]);
        _mockGateway.Verify(g => g.DispatchAsync(It.IsAny<Dictionary<string, object?>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenSsrFails_FallbacksToDataPageAttribute()
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
            .ReturnsAsync((SsrResponse?)null); // SSR fails

        var (tagHelper, context, output) = CreateTagHelper(pageData, gateway: _mockGateway.Object);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("div", output.TagName);
        Assert.NotNull(output.Attributes["data-page"]);
    }

    [Fact]
    public async Task ProcessAsync_WhenSsrThrows_FallbacksToDataPageAttribute()
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
        Assert.Equal("div", output.TagName);
        Assert.NotNull(output.Attributes["data-page"]);
    }

    [Fact]
    public async Task ProcessAsync_WithNoGateway_RendersWithoutSsr()
    {
        // Arrange
        _options.Ssr.Enabled = true; // Even if enabled, no gateway means no SSR
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
        Assert.Equal("div", output.TagName);
        Assert.NotNull(output.Attributes["data-page"]);
    }

    [Fact]
    public async Task ProcessAsync_SerializesComplexProps()
    {
        // Arrange
        var pageData = new Dictionary<string, object?>
        {
            ["component"] = "Users/Index",
            ["props"] = new Dictionary<string, object?>
            {
                ["users"] = new[]
                {
                    new { id = 1, name = "John", roles = new[] { "admin", "user" } },
                    new { id = 2, name = "Jane", roles = new[] { "user" } }
                },
                ["pagination"] = new { current = 1, total = 10 }
            },
            ["url"] = "/users",
            ["version"] = "abc123"
        };

        var (tagHelper, context, output) = CreateTagHelper(pageData);

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var dataPageAttr = output.Attributes["data-page"];
        Assert.NotNull(dataPageAttr);

        var json = dataPageAttr.Value as string;
        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json!);

        Assert.NotNull(parsed);
        var props = parsed!["props"];
        Assert.True(props.TryGetProperty("users", out var users));
        Assert.Equal(2, users.GetArrayLength());
    }
}
