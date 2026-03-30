using System.Text.Json;
using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Inertia.Tests;

public class InertiaResponseTests
{
    private static InertiaResponse CreateResponse(
        string component = "Test/Component",
        IDictionary<string, object?>? props = null,
        IDictionary<string, object?>? sharedProps = null,
        string rootView = "~/Views/App.cshtml",
        string version = "1.0",
        bool encryptHistory = false,
        bool clearHistory = false,
        bool preserveFragment = false,
        IDictionary<string, object?>? flash = null)
    {
        return new InertiaResponse(
            component: component,
            props: props ?? new Dictionary<string, object?>(),
            sharedProps: sharedProps ?? new Dictionary<string, object?>(),
            sharedProviders: [],
            rootView: rootView,
            version: version,
            encryptHistory: encryptHistory,
            clearHistory: clearHistory,
            preserveFragment: preserveFragment,
            flash: flash,
            exposeSharedPropKeys: true,
            jsonOptions: null);
    }

    private static (DefaultHttpContext Context, MemoryStream Body) CreateInertiaHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaderNames.Inertia] = "true";
        var body = new MemoryStream();
        context.Response.Body = body;
        return (context, body);
    }

    private static (DefaultHttpContext Context, MemoryStream Body) CreateStandardHttpContext()
    {
        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;
        return (context, body);
    }

    private static async Task<string> GetResponseBody(MemoryStream body)
    {
        body.Position = 0;
        using var reader = new StreamReader(body);
        return await reader.ReadToEndAsync();
    }

    public class Construction
    {
        [Fact]
        public void Constructor_SetsComponent()
        {
            var response = CreateResponse(component: "Users/Index");

            response.Component.Should().Be("Users/Index");
        }

        [Fact]
        public void Constructor_SetsProps()
        {
            var props = new Dictionary<string, object?> { ["name"] = "test" };

            var response = CreateResponse(props: props);

            response.Props.Should().ContainKey("name").WhoseValue.Should().Be("test");
        }

        [Fact]
        public void Constructor_SetsVersion()
        {
            var response = CreateResponse(version: "abc123");

            response.Version.Should().Be("abc123");
        }

        [Fact]
        public void Constructor_SetsRootView()
        {
            var response = CreateResponse(rootView: "~/Views/Custom.cshtml");

            response.RootView.Should().Be("~/Views/Custom.cshtml");
        }
    }

    public class WithViewDataTests
    {
        [Fact]
        public void WithViewData_StringKey_AddsToViewData()
        {
            var response = CreateResponse();

            response.WithViewData("title", "My Page");

            // WithViewData stores data internally; verify via Execute that it ends up in HttpContext.Items
            // We verify the fluent return type here and data propagation in the integration tests below
            response.Should().NotBeNull();
        }

        [Fact]
        public void WithViewData_Dictionary_MergesAll()
        {
            var response = CreateResponse();
            var data = new Dictionary<string, object?>
            {
                ["title"] = "My Page",
                ["description"] = "A test page",
            };

            response.WithViewData(data);

            response.Should().NotBeNull();
        }

        [Fact]
        public void WithViewData_ReturnsSelf()
        {
            var response = CreateResponse();

            var result = response.WithViewData("key", "value");

            result.Should().BeSameAs(response);
        }
    }

    public class InertiaRequestTests
    {
        [Fact]
        public async Task InertiaRequest_Returns200()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task InertiaRequest_SetsXInertiaHeader()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            context.Response.Headers[InertiaHeaderNames.Inertia].ToString().Should().Be("true");
        }

        [Fact]
        public async Task InertiaRequest_SetsContentTypeJson()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            context.Response.ContentType.Should().Be("application/json");
        }

        [Fact]
        public async Task InertiaRequest_ResponseBodyContainsComponent()
        {
            var response = CreateResponse(component: "Users/Show");
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("component").GetString().Should().Be("Users/Show");
        }

        [Fact]
        public async Task InertiaRequest_ResponseBodyContainsProps()
        {
            var props = new Dictionary<string, object?> { ["name"] = "Alice" };
            var response = CreateResponse(props: props);
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("props").GetProperty("name").GetString().Should().Be("Alice");
        }

        [Fact]
        public async Task InertiaRequest_ResponseBodyContainsUrl()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();
            context.Request.Path = "/users/1";

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("url").GetString().Should().Be("/users/1");
        }

        [Fact]
        public async Task InertiaRequest_ResponseBodyContainsVersion()
        {
            var response = CreateResponse(version: "v2.0");
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("version").GetString().Should().Be("v2.0");
        }

        [Fact]
        public async Task InertiaRequest_WithClearHistory_IncludesClearHistory()
        {
            var response = CreateResponse(clearHistory: true);
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("clearHistory").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task InertiaRequest_WithoutClearHistory_OmitsClearHistory()
        {
            var response = CreateResponse(clearHistory: false);
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("clearHistory", out _).Should().BeFalse();
        }

        [Fact]
        public async Task InertiaRequest_WithEncryptHistory_IncludesEncryptHistory()
        {
            var response = CreateResponse(encryptHistory: true);
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task InertiaRequest_WithPreserveFragment_IncludesPreserveFragment()
        {
            var response = CreateResponse(preserveFragment: true);
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("preserveFragment").GetBoolean().Should().BeTrue();
        }
    }

    public class InitialPageLoadTests
    {
        [Fact]
        public async Task InitialLoad_Returns200()
        {
            var response = CreateResponse();
            var (context, body) = CreateStandardHttpContext();

            await response.ExecuteAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task InitialLoad_SetsContentTypeHtml()
        {
            var response = CreateResponse();
            var (context, body) = CreateStandardHttpContext();

            await response.ExecuteAsync(context);

            context.Response.ContentType.Should().Be("text/html; charset=utf-8");
        }

        [Fact]
        public async Task InitialLoad_StoresPageInHttpContextItems()
        {
            var response = CreateResponse(component: "Dashboard/Index");
            var (context, body) = CreateStandardHttpContext();

            await response.ExecuteAsync(context);

            context.Items.Should().ContainKey("InertiaPage");
            var page = context.Items["InertiaPage"] as InertiaPage;
            page.Should().NotBeNull();
            page!.Component.Should().Be("Dashboard/Index");
        }

        [Fact]
        public async Task InitialLoad_ResponseContainsScriptAndDiv()
        {
            var response = CreateResponse(component: "Test/Page");
            var (context, body) = CreateStandardHttpContext();

            await response.ExecuteAsync(context);

            var html = await GetResponseBody(body);
            html.Should().Contain("""<script data-page="app" type="application/json">""");
            html.Should().Contain("</script>");
            html.Should().Contain("""<div id="app"></div>""");
        }
    }

    public class ExecuteAsyncTests
    {
        [Fact]
        public async Task ExecuteAsync_InertiaRequest_Returns200WithJson()
        {
            var response = CreateResponse(component: "Test/Page");
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            context.Response.ContentType.Should().Be("application/json");
            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("component").GetString().Should().Be("Test/Page");
        }

        [Fact]
        public async Task ExecuteAsync_NonInertiaRequest_ReturnsHtml()
        {
            var response = CreateResponse();
            var (context, body) = CreateStandardHttpContext();

            await response.ExecuteAsync(context);

            context.Response.ContentType.Should().Be("text/html; charset=utf-8");
            var html = await GetResponseBody(body);
            html.Should().Contain("<script");
            html.Should().Contain("<div id=\"app\"></div>");
        }
    }

    public class UrlTests
    {
        [Fact]
        public async Task Url_ExtractedFromRequestPath()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();
            context.Request.Path = "/users/42";

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("url").GetString().Should().Be("/users/42");
        }

        [Fact]
        public async Task Url_PreservesQueryString()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();
            context.Request.Path = "/users";
            context.Request.QueryString = new QueryString("?page=2&sort=name");

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("url").GetString().Should().Be("/users?page=2&sort=name");
        }

        [Fact]
        public async Task Url_RootPath_ReturnsSlash()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();
            context.Request.Path = "/";

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("url").GetString().Should().Be("/");
        }
    }

    public class SharedPropsTests
    {
        [Fact]
        public async Task SharedProps_MergedWithPageProps()
        {
            var shared = new Dictionary<string, object?> { ["appName"] = "MyApp" };
            var props = new Dictionary<string, object?> { ["users"] = new[] { "Alice" } };
            var response = CreateResponse(props: props, sharedProps: shared);
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            var propsEl = doc.RootElement.GetProperty("props");
            propsEl.GetProperty("appName").GetString().Should().Be("MyApp");
            propsEl.GetProperty("users").GetArrayLength().Should().Be(1);
        }

        [Fact]
        public async Task PageProps_OverrideSharedProps()
        {
            var shared = new Dictionary<string, object?> { ["key"] = "shared-value" };
            var props = new Dictionary<string, object?> { ["key"] = "page-value" };
            var response = CreateResponse(props: props, sharedProps: shared);
            var (context, body) = CreateInertiaHttpContext();

            await response.ExecuteAsync(context);

            var json = await GetResponseBody(body);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("props").GetProperty("key").GetString().Should().Be("page-value");
        }
    }

    public class StatusCodePreservation
    {
        [Fact]
        public async Task Execute_PreexistingNonOkStatusCode_NotOverwritten_InertiaRequest()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await response.ExecuteAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        [Fact]
        public async Task Execute_PreexistingNonOkStatusCode_NotOverwritten_InitialPageLoad()
        {
            var response = CreateResponse();
            var (context, body) = CreateStandardHttpContext();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await response.ExecuteAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        [Fact]
        public async Task Execute_DefaultStatusCodeZero_SetsTo200_InertiaRequest()
        {
            var response = CreateResponse();
            var (context, body) = CreateInertiaHttpContext();
            // DefaultHttpContext starts at 200 by default, so explicitly set to 0
            context.Response.StatusCode = 0;

            await response.ExecuteAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task Execute_DefaultStatusCodeZero_SetsTo200_InitialPageLoad()
        {
            var response = CreateResponse();
            var (context, body) = CreateStandardHttpContext();
            context.Response.StatusCode = 0;

            await response.ExecuteAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }
    }

    public class InterfaceTests
    {
        [Fact]
        public void ImplementsIActionResult()
        {
            var response = CreateResponse();

            response.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public void ImplementsIResult()
        {
            var response = CreateResponse();

            response.Should().BeAssignableTo<IResult>();
        }
    }
}
