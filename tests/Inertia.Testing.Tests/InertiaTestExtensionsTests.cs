using System.Net;
using System.Text;
using FluentAssertions;
using Inertia.Testing;

namespace Inertia.Testing.Tests;

public class InertiaTestExtensionsTests
{
    private static HttpResponseMessage CreateInertiaResponse(string json)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        response.Headers.Add("X-Inertia", "true");
        return response;
    }

    private static string BuildPageJson(
        string component = "TestComponent",
        string url = "/test",
        string version = "1.0",
        string? propsJson = null)
    {
        return $$"""{"component":"{{component}}","url":"{{url}}","version":"{{version}}","props":{{propsJson ?? "{}"}}}""";
    }

    [Fact]
    public async Task AssertInertia_ValidInertiaJsonResponse_RunsCallback()
    {
        var response = CreateInertiaResponse(BuildPageJson(component: "Foo"));
        var called = false;

        await response.AssertInertia(page =>
        {
            page.Component("Foo");
            called = true;
        });

        called.Should().BeTrue();
    }

    [Fact]
    public async Task AssertInertia_NoCallback_DoesNotThrow()
    {
        var response = CreateInertiaResponse(BuildPageJson());

        var act = async () => await response.AssertInertia();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AssertInertia_NonJsonResponse_FailsWithMessage()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("<html>Not Inertia</html>", Encoding.UTF8, "text/html");

        var act = async () => await response.AssertInertia(page => { });

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AssertInertia_ReturnsOriginalResponse()
    {
        var response = CreateInertiaResponse(BuildPageJson());

        var result = await response.AssertInertia(page => { });

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task InertiaPage_ReturnsPageDictionary()
    {
        var response = CreateInertiaResponse(
            BuildPageJson(component: "Foo", propsJson: """{"bar":"baz"}"""));

        var page = await response.InertiaPage();

        page["component"].Should().Be("Foo");
        page.Should().ContainKey("props");
    }

    [Fact]
    public async Task InertiaPage_ContainsComponentAndProps()
    {
        var response = CreateInertiaResponse(
            BuildPageJson(component: "Users/Index", url: "/users", version: "v1"));

        var page = await response.InertiaPage();

        page["component"].Should().Be("Users/Index");
        page["url"].Should().Be("/users");
        page["version"].Should().Be("v1");
    }

    [Fact]
    public async Task InertiaProps_ReturnsAllProps()
    {
        var response = CreateInertiaResponse(
            BuildPageJson(propsJson: """{"name":"John","age":30}"""));

        var props = await response.InertiaProps();

        props.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
        props.GetProperty("name").GetString().Should().Be("John");
        props.GetProperty("age").GetInt32().Should().Be(30);
    }

    [Fact]
    public async Task InertiaProps_WithName_ReturnsSpecificProp()
    {
        var response = CreateInertiaResponse(
            BuildPageJson(propsJson: """{"name":"John"}"""));

        var prop = await response.InertiaProps("name");

        prop.GetString().Should().Be("John");
    }

    [Fact]
    public async Task InertiaProps_WithDotNotation_ReturnsNestedProp()
    {
        var response = CreateInertiaResponse(
            BuildPageJson(propsJson: """{"user":{"name":"John"}}"""));

        var prop = await response.InertiaProps("user.name");

        prop.GetString().Should().Be("John");
    }

    [Fact]
    public async Task AssertInertia_TaskOverload_Works()
    {
        var response = CreateInertiaResponse(BuildPageJson(component: "Test"));
        var task = Task.FromResult(response);
        var called = false;

        await task.AssertInertia(page =>
        {
            page.Component("Test");
            called = true;
        });

        called.Should().BeTrue();
    }
}
