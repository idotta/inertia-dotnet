using System.Text.Json;
using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests;

public class InertiaPageTests
{
    private static InertiaPage CreateMinimalPage(
        string component = "Users/Index",
        string url = "/users",
        string version = "abc123") => new()
        {
            Component = component,
            Props = new Dictionary<string, object?>(),
            Url = url,
            Version = version,
        };

    public class Construction
    {
        [Fact]
        public void Constructor_SetsComponent()
        {
            var page = CreateMinimalPage(component: "Dashboard");

            page.Component.Should().Be("Dashboard");
        }

        [Fact]
        public void Constructor_SetsProps()
        {
            var props = new Dictionary<string, object?> { ["key"] = "value" };
            var page = new InertiaPage
            {
                Component = "Test",
                Props = props,
                Url = "/test",
                Version = "1",
            };

            page.Props.Should().BeSameAs(props);
        }

        [Fact]
        public void Constructor_SetsUrl()
        {
            var page = CreateMinimalPage(url: "/dashboard");

            page.Url.Should().Be("/dashboard");
        }

        [Fact]
        public void Constructor_SetsVersion()
        {
            var page = CreateMinimalPage(version: "v2");

            page.Version.Should().Be("v2");
        }

        [Fact]
        public void ClearHistory_DefaultsFalse()
        {
            var page = CreateMinimalPage();

            page.ClearHistory.Should().BeFalse();
        }

        [Fact]
        public void EncryptHistory_DefaultsFalse()
        {
            var page = CreateMinimalPage();

            page.EncryptHistory.Should().BeFalse();
        }

        [Fact]
        public void PreserveFragment_DefaultsFalse()
        {
            var page = CreateMinimalPage();

            page.PreserveFragment.Should().BeFalse();
        }
    }

    public class OptionalMetadata
    {
        [Fact]
        public void DeferredProps_DefaultsNull()
        {
            var page = CreateMinimalPage();

            page.DeferredProps.Should().BeNull();
        }

        [Fact]
        public void MergeProps_DefaultsNull()
        {
            var page = CreateMinimalPage();

            page.MergeProps.Should().BeNull();
        }

        [Fact]
        public void DeepMergeProps_DefaultsNull()
        {
            var page = CreateMinimalPage();

            page.DeepMergeProps.Should().BeNull();
        }

        [Fact]
        public void OnceProps_DefaultsNull()
        {
            var page = CreateMinimalPage();

            page.OnceProps.Should().BeNull();
        }

        [Fact]
        public void ScrollProps_DefaultsNull()
        {
            var page = CreateMinimalPage();

            page.ScrollProps.Should().BeNull();
        }

        [Fact]
        public void SharedProps_DefaultsNull()
        {
            var page = CreateMinimalPage();

            page.SharedProps.Should().BeNull();
        }

        [Fact]
        public void Flash_DefaultsNull()
        {
            var page = CreateMinimalPage();

            page.Flash.Should().BeNull();
        }
    }

    public class Serialization
    {
        [Fact]
        public void ToJson_MinimalPage_ContainsRequiredFields()
        {
            var page = CreateMinimalPage();

            var json = page.ToJson();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("component").GetString().Should().Be("Users/Index");
            root.GetProperty("props").ValueKind.Should().Be(JsonValueKind.Object);
            root.GetProperty("url").GetString().Should().Be("/users");
            root.GetProperty("version").GetString().Should().Be("abc123");
        }

        [Fact]
        public void ToJson_UsesCamelCaseNaming()
        {
            var page = new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?>(),
                Url = "/test",
                Version = "1",
                ClearHistory = true,
                EncryptHistory = true,
                PreserveFragment = true,
            };

            var json = page.ToJson();

            json.Should().Contain("\"clearHistory\"");
            json.Should().Contain("\"encryptHistory\"");
            json.Should().Contain("\"preserveFragment\"");
            json.Should().NotContain("\"ClearHistory\"");
            json.Should().NotContain("\"EncryptHistory\"");
            json.Should().NotContain("\"PreserveFragment\"");
        }

        [Fact]
        public void ToJson_ClearHistoryFalse_IsOmitted()
        {
            var page = CreateMinimalPage();

            var json = page.ToJson();

            json.Should().NotContain("clearHistory");
        }

        [Fact]
        public void ToJson_ClearHistoryTrue_IsIncluded()
        {
            var page = new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?>(),
                Url = "/test",
                Version = "1",
                ClearHistory = true,
            };

            var json = page.ToJson();
            using var doc = JsonDocument.Parse(json);

            doc.RootElement.GetProperty("clearHistory").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public void ToJson_EncryptHistoryTrue_IsIncluded()
        {
            var page = new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?>(),
                Url = "/test",
                Version = "1",
                EncryptHistory = true,
            };

            var json = page.ToJson();
            using var doc = JsonDocument.Parse(json);

            doc.RootElement.GetProperty("encryptHistory").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public void ToJson_PreserveFragmentTrue_IsIncluded()
        {
            var page = new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?>(),
                Url = "/test",
                Version = "1",
                PreserveFragment = true,
            };

            var json = page.ToJson();
            using var doc = JsonDocument.Parse(json);

            doc.RootElement.GetProperty("preserveFragment").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public void ToJson_NullMetadata_OmittedFromJson()
        {
            var page = CreateMinimalPage();

            var json = page.ToJson();

            json.Should().NotContain("deferredProps");
            json.Should().NotContain("mergeProps");
            json.Should().NotContain("deepMergeProps");
            json.Should().NotContain("onceProps");
            json.Should().NotContain("scrollProps");
            json.Should().NotContain("sharedProps");
            json.Should().NotContain("flash");
        }

        [Fact]
        public void ToJson_PropsWithAnonymousType_SerializesCorrectly()
        {
            var page = new InertiaPage
            {
                Component = "Test",
                Props = new Dictionary<string, object?> { ["user"] = new { Name = "John", Age = 30 } },
                Url = "/test",
                Version = "1",
            };

            var json = page.ToJson();
            using var doc = JsonDocument.Parse(json);
            var user = doc.RootElement.GetProperty("props").GetProperty("user");

            user.GetProperty("name").GetString().Should().Be("John");
            user.GetProperty("age").GetInt32().Should().Be(30);
        }
    }
}
