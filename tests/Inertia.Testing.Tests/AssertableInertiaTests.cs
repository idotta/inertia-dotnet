using System.Text.Json;
using FluentAssertions;
using Inertia.Testing;

namespace Inertia.Testing.Tests;

public class AssertableInertiaTests
{
    private static string BuildPageJson(
        string component = "TestComponent",
        string url = "/test",
        string version = "1.0",
        string? propsJson = null,
        string? flashJson = null,
        string? deferredPropsJson = null,
        bool encryptHistory = false,
        bool clearHistory = false)
    {
        var parts = new List<string>
        {
            $"\"component\":\"{component}\"",
            $"\"url\":\"{url}\"",
            $"\"version\":\"{version}\"",
            $"\"props\":{propsJson ?? "{}"}",
        };

        if (flashJson is not null)
            parts.Add($"\"flash\":{flashJson}");

        if (deferredPropsJson is not null)
            parts.Add($"\"deferredProps\":{deferredPropsJson}");

        if (encryptHistory)
            parts.Add("\"encryptHistory\":true");

        if (clearHistory)
            parts.Add("\"clearHistory\":true");

        return "{" + string.Join(",", parts) + "}";
    }

    // ---- Group 1: Construction ----
    public class Construction
    {
        [Fact]
        public void FromJson_ValidJson_SetsComponent()
        {
            var json = BuildPageJson(component: "Users/Index");

            var assertable = AssertableInertia.FromJson(json);

            assertable.GetComponent().Should().Be("Users/Index");
        }

        [Fact]
        public void FromJson_ValidJson_SetsUrl()
        {
            var json = BuildPageJson(url: "/users");

            var assertable = AssertableInertia.FromJson(json);

            assertable.GetUrl().Should().Be("/users");
        }

        [Fact]
        public void FromJson_ValidJson_SetsVersion()
        {
            var json = BuildPageJson(version: "abc123");

            var assertable = AssertableInertia.FromJson(json);

            assertable.GetVersion().Should().Be("abc123");
        }

        [Fact]
        public void FromJson_ValidJson_SetsProps()
        {
            var json = BuildPageJson(propsJson: """{"name":"John"}""");

            var assertable = AssertableInertia.FromJson(json);

            assertable.Prop("name").GetString().Should().Be("John");
        }

        [Fact]
        public void FromJson_MissingComponent_Fails()
        {
            var json = """{"props":{},"url":"/test","version":"1.0"}""";

            var act = () => AssertableInertia.FromJson(json);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void FromJson_MissingProps_Fails()
        {
            var json = """{"component":"Test","url":"/test","version":"1.0"}""";

            var act = () => AssertableInertia.FromJson(json);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void FromJson_MissingUrl_Fails()
        {
            var json = """{"component":"Test","props":{},"version":"1.0"}""";

            var act = () => AssertableInertia.FromJson(json);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void FromJson_MissingVersion_Fails()
        {
            var json = """{"component":"Test","props":{},"url":"/test"}""";

            var act = () => AssertableInertia.FromJson(json);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void FromJson_WithDeferredProps_ParsesDeferredProps()
        {
            var json = BuildPageJson(
                deferredPropsJson: """{"default":["deferred1","deferred2"],"custom":["deferred3"]}""");

            var assertable = AssertableInertia.FromJson(json);

            assertable.GetDeferredProps().Should().ContainKey("default");
            assertable.GetDeferredProps()["default"].Should().BeEquivalentTo(["deferred1", "deferred2"]);
            assertable.GetDeferredProps()["custom"].Should().BeEquivalentTo(["deferred3"]);
        }

        [Fact]
        public void FromJson_WithFlash_ParsesFlash()
        {
            var json = BuildPageJson(flashJson: """{"message":"Hello"}""");

            var assertable = AssertableInertia.FromJson(json);

            assertable.HasFlash("message");
        }

        [Fact]
        public void FromJson_WithEncryptHistory_SetsFlag()
        {
            var json = BuildPageJson(encryptHistory: true);

            var assertable = AssertableInertia.FromJson(json);

            var page = assertable.ToPage();
            page.Should().ContainKey("encryptHistory");
        }

        [Fact]
        public void FromJson_WithClearHistory_SetsFlag()
        {
            var json = BuildPageJson(clearHistory: true);

            var assertable = AssertableInertia.FromJson(json);

            var page = assertable.ToPage();
            page.Should().ContainKey("clearHistory");
        }

        [Fact]
        public void FromJson_WithoutOptionalFields_DefaultsToEmpty()
        {
            var json = BuildPageJson();

            var assertable = AssertableInertia.FromJson(json);

            assertable.GetDeferredProps().Should().BeEmpty();
        }
    }

    // ---- Group 2: Component, Url, Version ----
    public class ComponentUrlVersion
    {
        [Fact]
        public void Component_MatchingValue_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson(component: "Users/Index"));

            var result = assertable.Component("Users/Index");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void Component_NonMatchingValue_Fails()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson(component: "Users/Index"));

            var act = () => assertable.Component("Users/Edit");

            act.Should().Throw<Exception>().WithMessage("*Unexpected Inertia page component*");
        }

        [Fact]
        public void Url_MatchingValue_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson(url: "/users"));

            var result = assertable.Url("/users");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void Url_NonMatchingValue_Fails()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson(url: "/users"));

            var act = () => assertable.Url("/other");

            act.Should().Throw<Exception>().WithMessage("*Unexpected Inertia page url*");
        }

        [Fact]
        public void Version_MatchingValue_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson(version: "abc"));

            var result = assertable.Version("abc");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void Version_NonMatchingValue_Fails()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson(version: "abc"));

            var act = () => assertable.Version("xyz");

            act.Should().Throw<Exception>().WithMessage("*Unexpected Inertia asset version*");
        }
    }

    // ---- Group 3: Has / Missing ----
    public class HasMissing
    {
        [Fact]
        public void Has_ExistingTopLevelProp_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var result = assertable.Has("name");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void Has_NestedDotNotation_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"user":{"name":"John"}}"""));

            assertable.Has("user.name");
        }

        [Fact]
        public void Has_ArrayIndex_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"users":[{"name":"John"}]}"""));

            assertable.Has("users.0.name");
        }

        [Fact]
        public void Has_NonExistingProp_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var act = () => assertable.Has("missing");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Has_WithCount_MatchingArrayLength_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"users":["a","b","c"]}"""));

            var result = assertable.Has("users", 3);

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void Has_WithCount_NonMatchingLength_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"users":["a","b"]}"""));

            var act = () => assertable.Has("users", 5);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Has_WithCount_ObjectPropertyCount_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"user":{"name":"John","age":30}}"""));

            assertable.Has("user", 2);
        }

        [Fact]
        public void Missing_NonExistingProp_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var result = assertable.Missing("other");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void Missing_ExistingProp_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var act = () => assertable.Missing("name");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void HasAll_AllExist_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John","age":30}"""));

            var result = assertable.HasAll("name", "age");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void HasAll_SomeMissing_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var act = () => assertable.HasAll("name", "missing");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void MissingAll_NoneExist_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var result = assertable.MissingAll("foo", "bar");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void MissingAll_SomeExist_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John","age":30}"""));

            var act = () => assertable.MissingAll("other", "name");

            act.Should().Throw<Exception>();
        }
    }

    // ---- Group 4: Where ----
    public class WhereAssertions
    {
        [Fact]
        public void Where_StringValue_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var result = assertable.Where("name", "John");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void Where_IntValue_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"age":30}"""));

            assertable.Where("age", 30);
        }

        [Fact]
        public void Where_BoolValue_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"active":true}"""));

            assertable.Where("active", true);
        }

        [Fact]
        public void Where_NullValue_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"value":null}"""));

            assertable.Where("value", (object?)null);
        }

        [Fact]
        public void Where_NestedDotNotation_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"user":{"name":"John"}}"""));

            assertable.Where("user.name", "John");
        }

        [Fact]
        public void Where_ArrayIndex_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"users":[{"name":"John"},{"name":"Jane"}]}"""));

            assertable.Where("users.0.name", "John");
            assertable.Where("users.1.name", "Jane");
        }

        [Fact]
        public void Where_NonMatchingValue_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var act = () => assertable.Where("name", "Jane");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Where_NonExistingPath_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var act = () => assertable.Where("missing", "value");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Where_WithCallback_PassesElement()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            string? captured = null;
            assertable.Where("name", el => captured = el.GetString());

            captured.Should().Be("John");
        }

        [Fact]
        public void Where_DoubleValue_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"price":19.99}"""));

            assertable.Where("price", 19.99);
        }

        [Fact]
        public void Where_AnonymousObject_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"user":{"name":"John","age":30}}"""));

            assertable.Where("user", new { name = "John", age = 30 });
        }
    }

    // ---- Group 5: Flash ----
    public class FlashAssertions
    {
        [Fact]
        public void HasFlash_ExistingKey_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(flashJson: """{"message":"Hello"}"""));

            var result = assertable.HasFlash("message");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void HasFlash_ExistingKeyWithValue_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(flashJson: """{"message":"Hello"}"""));

            var result = assertable.HasFlash("message", "Hello");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void HasFlash_MissingKey_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(flashJson: """{"message":"Hello"}"""));

            var act = () => assertable.HasFlash("other");

            act.Should().Throw<Exception>().WithMessage("*missing key*");
        }

        [Fact]
        public void HasFlash_WrongValue_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(flashJson: """{"message":"Hello"}"""));

            var act = () => assertable.HasFlash("message", "Wrong");

            act.Should().Throw<Exception>().WithMessage("*does not match*");
        }

        [Fact]
        public void HasFlash_NestedDotNotation_Works()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(flashJson: """{"notification":{"type":"success"}}"""));

            assertable.HasFlash("notification.type", "success");
        }

        [Fact]
        public void MissingFlash_MissingKey_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(flashJson: """{"message":"Hello"}"""));

            var result = assertable.MissingFlash("other");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void MissingFlash_ExistingKey_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(flashJson: """{"message":"Hello"}"""));

            var act = () => assertable.MissingFlash("message");

            act.Should().Throw<Exception>().WithMessage("*unexpected key*");
        }
    }

    // ---- Group 6: Data Access ----
    public class DataAccess
    {
        [Fact]
        public void Prop_ExistingPath_ReturnsElement()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var element = assertable.Prop("name");

            element.GetString().Should().Be("John");
        }

        [Fact]
        public void Prop_NonExistingPath_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"name":"John"}"""));

            var act = () => assertable.Prop("missing");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void PropGeneric_DeserializesToType()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(propsJson: """{"count":42}"""));

            var value = assertable.Prop<int>("count");

            value.Should().Be(42);
        }

        [Fact]
        public void ToPage_ReturnsDictionary()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(component: "Foo", url: "/foo", version: "v1",
                    propsJson: """{"bar":"baz"}"""));

            var page = assertable.ToPage();

            page["component"].Should().Be("Foo");
            page["url"].Should().Be("/foo");
            page["version"].Should().Be("v1");
            page.Should().ContainKey("props");
        }
    }

    // ---- Group 7: FromResponseAsync HTML Parsing ----
    public class FromResponseAsyncHtmlParsing
    {
        private static HttpResponseMessage CreateHtmlResponse(string body)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(body, System.Text.Encoding.UTF8, "text/html");
            return response;
        }

        [Fact]
        public async Task FromResponseAsync_V3ScriptFormat_ExtractsJson()
        {
            var pageJson = BuildPageJson(component: "Users/Index", url: "/users");
            var html = $"""<script data-page="app" type="application/json">{pageJson}</script><div id="app"></div>""";

            var assertable = await AssertableInertia.FromResponseAsync(CreateHtmlResponse(html));

            assertable.GetComponent().Should().Be("Users/Index");
            assertable.GetUrl().Should().Be("/users");
        }

        [Fact]
        public async Task FromResponseAsync_V3ScriptFormatWithCustomId_ExtractsJson()
        {
            var pageJson = BuildPageJson(component: "Dashboard", url: "/dashboard", version: "v2");
            var html = $"""<script data-page="my-app" type="application/json">{pageJson}</script><div id="my-app"></div>""";

            var assertable = await AssertableInertia.FromResponseAsync(CreateHtmlResponse(html));

            assertable.GetComponent().Should().Be("Dashboard");
            assertable.GetVersion().Should().Be("v2");
        }

        [Fact]
        public async Task FromResponseAsync_V3ScriptFormatWithSurroundingHtml_ExtractsJson()
        {
            var pageJson = BuildPageJson(component: "Home", url: "/");
            var html = $"""
                <!DOCTYPE html>
                <html><head><title>Test</title></head><body>
                <script data-page="app" type="application/json">{pageJson}</script><div id="app"></div>
                </body></html>
                """;

            var assertable = await AssertableInertia.FromResponseAsync(CreateHtmlResponse(html));

            assertable.GetComponent().Should().Be("Home");
        }

        [Fact]
        public async Task FromResponseAsync_EmptyHtml_Fails()
        {
            var act = () => AssertableInertia.FromResponseAsync(CreateHtmlResponse(""));

            await act.Should().ThrowAsync<Exception>().WithMessage("*Not a valid Inertia response*");
        }

        [Fact]
        public async Task FromResponseAsync_MalformedHtml_NoScriptOrDataPage_Fails()
        {
            var act = () => AssertableInertia.FromResponseAsync(
                CreateHtmlResponse("<html><body><div>Nothing here</div></body></html>"));

            await act.Should().ThrowAsync<Exception>().WithMessage("*Not a valid Inertia response*");
        }

        [Fact]
        public async Task FromResponseAsync_ScriptTagWithEmptyContent_Fails()
        {
            var html = """<script data-page="app" type="application/json"></script><div id="app"></div>""";

            var act = () => AssertableInertia.FromResponseAsync(CreateHtmlResponse(html));

            // Empty script content is not valid JSON; the parser rejects it
            await act.Should().ThrowAsync<Exception>();
        }
    }

    // ---- Group 8: Fluent Chaining ----
    public class FluentChaining
    {
        [Fact]
        public void MultipleAssertions_AllChained_AllPass()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson(
                    component: "Users/Index",
                    url: "/users",
                    version: "1.0",
                    propsJson: """{"name":"John","age":30,"active":true}""",
                    flashJson: """{"message":"Hello"}"""));

            assertable
                .Component("Users/Index")
                .Url("/users")
                .Version("1.0")
                .Has("name")
                .Has("age")
                .Where("name", "John")
                .Where("age", 30)
                .Where("active", true)
                .HasFlash("message", "Hello")
                .MissingFlash("other")
                .Missing("nonexistent");
        }
    }
}
