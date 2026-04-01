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

    // ---- Group 8: Additional Assertions (WhereNot, WhereType, WhereContains, HasAny) ----
    public class AdditionalAssertions
    {
        private static string BuildPageJson(string propsJson = "{}")
        {
            return $$"""{"component":"Test","url":"/test","version":"1.0","props":{{propsJson}}}""";
        }

        [Fact]
        public void WhereNot_DifferentValue_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"name":"John"}"""));

            var result = assertable.WhereNot("name", "Jane");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void WhereNot_MatchingValue_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"name":"John"}"""));

            var act = () => assertable.WhereNot("name", "John");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void WhereNot_NonExistingPath_Fails()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson());

            var act = () => assertable.WhereNot("missing", "value");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void WhereType_String_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"name":"John"}"""));

            assertable.WhereType("name", "string");
        }

        [Fact]
        public void WhereType_Integer_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"age":30}"""));

            assertable.WhereType("age", "integer");
        }

        [Fact]
        public void WhereType_Number_MatchesDecimal()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"score":9.5}"""));

            assertable.WhereType("score", "number");
        }

        [Fact]
        public void WhereType_Boolean_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"active":true}"""));

            assertable.WhereType("active", "boolean");
        }

        [Fact]
        public void WhereType_Array_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"items":[1,2,3]}"""));

            assertable.WhereType("items", "array");
        }

        [Fact]
        public void WhereType_Object_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"user":{"name":"John"}}"""));

            assertable.WhereType("user", "object");
        }

        [Fact]
        public void WhereType_Null_Matches()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"value":null}"""));

            assertable.WhereType("value", "null");
        }

        [Fact]
        public void WhereType_Mismatch_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"name":"John"}"""));

            var act = () => assertable.WhereType("name", "integer");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void WhereType_UnknownType_ThrowsArgumentException()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"name":"John"}"""));

            var act = () => assertable.WhereType("name", "custom");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WhereContains_ArrayContainsValue_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"tags":["a","b","c"]}"""));

            var result = assertable.WhereContains("tags", "b");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void WhereContains_ArrayMissing_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"tags":["a","b","c"]}"""));

            var act = () => assertable.WhereContains("tags", "z");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void WhereContains_StringContains_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"message":"hello world"}"""));

            var result = assertable.WhereContains("message", "world");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void WhereContains_StringMissing_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"message":"hello world"}"""));

            var act = () => assertable.WhereContains("message", "xyz");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void WhereContains_NonArrayOrString_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"count":42}"""));

            var act = () => assertable.WhereContains("count", 42);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void HasAny_OneExists_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"name":"John"}"""));

            var result = assertable.HasAny("missing", "name");

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void HasAny_NoneExist_Fails()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson());

            var act = () => assertable.HasAny("a", "b", "c");

            act.Should().Throw<Exception>();
        }
    }

    // ---- Group 9: Scoping Assertions ----
    public class ScopingAssertions
    {
        private static string BuildPageJson(string propsJson = "{}")
        {
            return $$"""{"component":"Test","url":"/test","version":"1.0","props":{{propsJson}}}""";
        }

        [Fact]
        public void Scope_ValidPath_ScopesToNestedObject()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"user":{"name":"John","age":30}}"""));

            assertable.Scope("user", scoped =>
            {
                scoped.Where("name", "John");
                scoped.Where("age", 30);
            });
        }

        [Fact]
        public void Scope_NonExistingPath_Fails()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson());

            var act = () => assertable.Scope("missing", _ => { });

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Scope_InteractionTracking_AllKeysInteracted_Passes()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"user":{"name":"John","age":30}}"""));

            var act = () => assertable.Scope("user", scoped =>
            {
                scoped.Has("name");
                scoped.Has("age");
            });

            act.Should().NotThrow();
        }

        [Fact]
        public void Scope_InteractionTracking_MissingKeys_FailsWithUninteractedList()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"user":{"name":"John","age":30,"email":"j@x.com"}}"""));

            var act = () => assertable.Scope("user", scoped =>
            {
                scoped.Has("name");
                // age and email are not interacted
            });

            act.Should().Throw<Exception>().WithMessage("*Unexpected properties*age*email*");
        }

        [Fact]
        public void Scope_Etc_DisablesInteractionTracking()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"user":{"name":"John","age":30,"email":"j@x.com"}}"""));

            var act = () => assertable.Scope("user", scoped =>
            {
                scoped.Has("name");
                scoped.Etc();
            });

            act.Should().NotThrow();
        }

        [Fact]
        public void Scope_NestedScopes_Work()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"user":{"profile":{"bio":"hello"}}}"""));

            assertable.Scope("user", userScope =>
            {
                userScope.Scope("profile", profileScope =>
                {
                    profileScope.Where("bio", "hello");
                });
            });
        }

        [Fact]
        public void Scope_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"user":{"name":"John"}}"""));

            var result = assertable.Scope("user", scoped =>
            {
                scoped.Has("name");
            });

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void First_ArrayPath_ScopesToFirstElement()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"users":[{"name":"Alice"},{"name":"Bob"}]}"""));

            assertable.First("users", scoped =>
            {
                scoped.Where("name", "Alice");
            });
        }

        [Fact]
        public void First_EmptyArray_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"users":[]}"""));

            var act = () => assertable.First("users", _ => { });

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void First_NonArrayPath_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"user":{"name":"John"}}"""));

            var act = () => assertable.First("user", _ => { });

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void First_NoPath_ScopesToCurrentArray()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"users":[{"id":1,"name":"Alice"},{"id":2,"name":"Bob"}]}"""));

            assertable.Scope("users", arrayScope =>
            {
                arrayScope.First(firstScope =>
                {
                    firstScope.Where("id", 1);
                    firstScope.Where("name", "Alice");
                });
                arrayScope.Etc();
            });
        }

        [Fact]
        public void Each_ArrayPath_IteratesAllElements()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"users":[{"active":true},{"active":true}]}"""));

            var count = 0;
            assertable.Each("users", scoped =>
            {
                scoped.Where("active", true);
                count++;
            });

            count.Should().Be(2);
        }

        [Fact]
        public void Each_EmptyArray_NoOp()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"users":[]}"""));

            var count = 0;
            assertable.Each("users", _ => count++);

            count.Should().Be(0);
        }

        [Fact]
        public void Each_NonArrayPath_Fails()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"user":{"name":"John"}}"""));

            var act = () => assertable.Each("user", _ => { });

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Each_NoPath_IteratesCurrentArray()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"items":[{"type":"a"},{"type":"b"}]}"""));

            var types = new List<string>();
            assertable.Scope("items", arrayScope =>
            {
                arrayScope.Each(itemScope =>
                {
                    types.Add(itemScope.Prop("type").GetString()!);
                    itemScope.Etc();
                });
                arrayScope.Etc();
            });

            types.Should().BeEquivalentTo(["a", "b"]);
        }

        [Fact]
        public void Each_InteractionTracking_PerElement()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"users":[{"id":1,"name":"a"},{"id":2,"name":"b"}]}"""));

            var act = () => assertable.Each("users", scoped =>
            {
                scoped.Has("id");
                // name not interacted — should fail
            });

            act.Should().Throw<Exception>().WithMessage("*name*");
        }

        [Fact]
        public void Etc_ReturnsSelf()
        {
            var assertable = AssertableInertia.FromJson(BuildPageJson());

            var result = assertable.Etc();

            result.Should().BeSameAs(assertable);
        }

        [Fact]
        public void TopLevel_NoInteractionTracking()
        {
            var assertable = AssertableInertia.FromJson(
                BuildPageJson("""{"name":"John","age":30,"extra":"data"}"""));

            // Only asserting on "name" — should not fail because top-level has no interaction tracking
            var act = () => assertable.Has("name");

            act.Should().NotThrow();
        }
    }

    // ---- Group 10: Component File Existence ----
    [Collection("PageExistence")]
    public class ComponentFileExistence : IDisposable
    {
        private readonly string _tempDir;

        public ComponentFileExistence()
        {
            AssertableInertia.Configure((AssertableInertia.PageExistenceConfig?)null);
            _tempDir = Path.Combine(Path.GetTempPath(), $"inertia_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            AssertableInertia.Configure((AssertableInertia.PageExistenceConfig?)null);
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        private static string BuildPageJson(string component = "Test")
        {
            return $$$"""{"component":"{{{component}}}","url":"/test","version":"1.0","props":{}}""";
        }

        [Fact]
        public void Component_ShouldExistTrue_FileExists_Passes()
        {
            File.WriteAllText(Path.Combine(_tempDir, "Dashboard.vue"), "");
            AssertableInertia.Configure(new AssertableInertia.PageExistenceConfig(
                true, [_tempDir], ["vue"]));

            var assertable = AssertableInertia.FromJson(BuildPageJson("Dashboard"));

            var act = () => assertable.Component("Dashboard", shouldExist: true);

            act.Should().NotThrow();
        }

        [Fact]
        public void Component_ShouldExistTrue_FileNotFound_Fails()
        {
            AssertableInertia.Configure(new AssertableInertia.PageExistenceConfig(
                true, [_tempDir], ["vue"]));

            var assertable = AssertableInertia.FromJson(BuildPageJson("Missing"));

            var act = () => assertable.Component("Missing", shouldExist: true);

            act.Should().Throw<Exception>().WithMessage("*does not exist*");
        }

        [Fact]
        public void Component_ShouldExistFalse_SkipsFileCheck()
        {
            AssertableInertia.Configure(new AssertableInertia.PageExistenceConfig(
                true, [_tempDir], ["vue"]));

            var assertable = AssertableInertia.FromJson(BuildPageJson("Missing"));

            var act = () => assertable.Component("Missing", shouldExist: false);

            act.Should().NotThrow();
        }

        [Fact]
        public void Component_ShouldExistNull_ConfigEnabled_ChecksFile()
        {
            AssertableInertia.Configure(new AssertableInertia.PageExistenceConfig(
                true, [_tempDir], ["vue"]));

            var assertable = AssertableInertia.FromJson(BuildPageJson("Missing"));

            var act = () => assertable.Component("Missing");

            act.Should().Throw<Exception>().WithMessage("*does not exist*");
        }

        [Fact]
        public void Component_ShouldExistNull_ConfigDisabled_SkipsCheck()
        {
            AssertableInertia.Configure(new AssertableInertia.PageExistenceConfig(
                false, [_tempDir], ["vue"]));

            var assertable = AssertableInertia.FromJson(BuildPageJson("Missing"));

            var act = () => assertable.Component("Missing");

            act.Should().NotThrow();
        }

        [Fact]
        public void Component_ShouldExistNull_NoConfig_SkipsCheck()
        {
            // No Configure() call — defaults to no check
            var assertable = AssertableInertia.FromJson(BuildPageJson("Missing"));

            var act = () => assertable.Component("Missing");

            act.Should().NotThrow();
        }

        [Fact]
        public void Component_ShouldExistTrue_NoPagePaths_FailsWithMessage()
        {
            AssertableInertia.Configure(new AssertableInertia.PageExistenceConfig(
                true, [], ["vue"]));

            var assertable = AssertableInertia.FromJson(BuildPageJson("Test"));

            var act = () => assertable.Component("Test", shouldExist: true);

            act.Should().Throw<Exception>().WithMessage("*PagePaths*");
        }
    }

    // ---- Group 11: Fluent Chaining ----
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
