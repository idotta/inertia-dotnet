using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inertia.Tests;

public class InertiaOptionsTests
{
    private static InertiaOptions CreateOptions(Action<InertiaOptions>? configure = null)
    {
        var options = new InertiaOptions { RootView = "~/Views/App.cshtml" };
        configure?.Invoke(options);
        return options;
    }

    private static (bool IsValid, List<ValidationResult> Results) Validate(InertiaOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        return (isValid, results);
    }

    public class SectionConstant
    {
        [Fact]
        public void Section_IsInertia()
        {
            InertiaOptions.Section.Should().Be("Inertia");
        }
    }

    public class Defaults
    {
        private readonly InertiaOptions _options = CreateOptions();

        [Fact]
        public void RootView_DefaultsToAppCshtml()
        {
            _options.RootView.Should().Be("~/Views/App.cshtml");
        }

        [Fact]
        public void EncryptHistory_DefaultsToFalse()
        {
            _options.EncryptHistory.Should().BeFalse();
        }

        [Fact]
        public void SsrEnabled_DefaultsToTrue()
        {
            _options.SsrEnabled.Should().BeTrue();
        }

        [Fact]
        public void SsrUrl_DefaultsToLocalhost()
        {
            _options.SsrUrl.Should().Be("http://127.0.0.1:13714");
        }

        [Fact]
        public void SsrEnsureBundleExists_DefaultsToTrue()
        {
            _options.SsrEnsureBundleExists.Should().BeTrue();
        }

        [Fact]
        public void SsrBundle_DefaultsToNull()
        {
            _options.SsrBundle.Should().BeNull();
        }

        [Fact]
        public void SsrThrowOnError_DefaultsToFalse()
        {
            _options.SsrThrowOnError.Should().BeFalse();
        }

        [Fact]
        public void EnsurePagesExist_DefaultsToFalse()
        {
            _options.EnsurePagesExist.Should().BeFalse();
        }

        [Fact]
        public void PagePaths_DefaultsToEmpty()
        {
            _options.PagePaths.Should().BeEmpty();
        }

        [Fact]
        public void PageExtensions_DefaultsToSixEntries()
        {
            _options.PageExtensions.Should().BeEquivalentTo(
                ["js", "jsx", "svelte", "ts", "tsx", "vue"]);
        }

        [Fact]
        public void TestingEnsurePagesExist_DefaultsToTrue()
        {
            _options.TestingEnsurePagesExist.Should().BeTrue();
        }

        [Fact]
        public void ExposeSharedPropKeys_DefaultsToTrue()
        {
            _options.ExposeSharedPropKeys.Should().BeTrue();
        }

        [Fact]
        public void JsonSerializerOptions_DefaultsToNull()
        {
            _options.JsonSerializerOptions.Should().BeNull();
        }

        [Fact]
        public void VersionProvider_DefaultsToNull()
        {
            _options.VersionProvider.Should().BeNull();
        }

        [Fact]
        public void RootViewProvider_DefaultsToNull()
        {
            _options.RootViewProvider.Should().BeNull();
        }

        [Fact]
        public void SharedPropsProvider_DefaultsToNull()
        {
            _options.SharedPropsProvider.Should().BeNull();
        }

        [Fact]
        public void OnVersionChange_DefaultsToNull()
        {
            _options.OnVersionChange.Should().BeNull();
        }

        [Fact]
        public void OnEmptyResponse_DefaultsToNull()
        {
            _options.OnEmptyResponse.Should().BeNull();
        }
    }

    public class Validation
    {
        [Fact]
        public void ValidOptions_PassesValidation()
        {
            var options = CreateOptions();

            var (isValid, results) = Validate(options);

            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Fact]
        public void RootView_HasRequiredAttribute()
        {
            typeof(InertiaOptions)
                .GetProperty(nameof(InertiaOptions.RootView))!
                .GetCustomAttributes(typeof(RequiredAttribute), inherit: false)
                .Should().ContainSingle();
        }

        [Fact]
        public void SsrUrl_InvalidUrl_FailsValidation()
        {
            var options = CreateOptions(o => o.SsrUrl = "not-a-url");

            var (isValid, results) = Validate(options);

            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains(nameof(InertiaOptions.SsrUrl)));
        }

        [Fact]
        public void SsrUrl_EmptyString_FailsValidation()
        {
            var options = CreateOptions(o => o.SsrUrl = "");

            var (isValid, results) = Validate(options);

            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains(nameof(InertiaOptions.SsrUrl)));
        }

        [Fact]
        public void SsrUrl_ValidHttpUrl_PassesValidation()
        {
            var options = CreateOptions(o => o.SsrUrl = "http://localhost:3000");

            var (isValid, _) = Validate(options);

            isValid.Should().BeTrue();
        }

        [Fact]
        public void SsrUrl_ValidHttpsUrl_PassesValidation()
        {
            var options = CreateOptions(o => o.SsrUrl = "https://ssr.example.com");

            var (isValid, _) = Validate(options);

            isValid.Should().BeTrue();
        }

        [Fact]
        public void SsrUrl_RelativePath_FailsValidation()
        {
            var options = CreateOptions(o => o.SsrUrl = "/render");

            var (isValid, results) = Validate(options);

            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains(nameof(InertiaOptions.SsrUrl)));
        }
    }

    public class Delegates
    {
        [Fact]
        public void VersionProvider_CanBeAssignedAndInvoked()
        {
            var options = CreateOptions(o =>
                o.VersionProvider = _ => "abc123");

            var result = options.VersionProvider!(new DefaultHttpContext());

            result.Should().Be("abc123");
        }

        [Fact]
        public void RootViewProvider_CanBeAssignedAndInvoked()
        {
            var options = CreateOptions(o =>
                o.RootViewProvider = _ => "~/Views/Custom.cshtml");

            var result = options.RootViewProvider!(new DefaultHttpContext());

            result.Should().Be("~/Views/Custom.cshtml");
        }

        [Fact]
        public void SharedPropsProvider_CanBeAssignedAndInvoked()
        {
            var expected = new Dictionary<string, object?> { ["auth"] = "user1" };
            var options = CreateOptions(o =>
                o.SharedPropsProvider = (_, _) => expected);

            var result = options.SharedPropsProvider!(
                new DefaultHttpContext(),
                new ServiceCollection().BuildServiceProvider());

            result.Should().BeSameAs(expected);
        }

        [Fact]
        public void OnVersionChange_CanBeAssignedAndInvoked()
        {
            var options = CreateOptions(o =>
                o.OnVersionChange = _ => TypedResults.Conflict());

            var result = options.OnVersionChange!(new DefaultHttpContext());

            result.Should().BeOfType<Conflict>();
        }

        [Fact]
        public void OnEmptyResponse_CanBeAssignedAndInvoked()
        {
            var options = CreateOptions(o =>
                o.OnEmptyResponse = _ => TypedResults.NoContent());

            var result = options.OnEmptyResponse!(new DefaultHttpContext());

            result.Should().BeOfType<NoContent>();
        }
    }

    public class OptionsValidationIntegration
    {
        [Fact]
        public void ValidateOnStart_WithValidDefaults_DoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddOptions<InertiaOptions>()
                .Configure(o => o.RootView = "~/Views/App.cshtml")
                .ValidateDataAnnotations()
                .ValidateOnStart();
            var serviceProvider = services.BuildServiceProvider();

            var act = () => serviceProvider.GetRequiredService<IOptions<InertiaOptions>>().Value;

            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateOnStart_WithInvalidSsrUrl_ThrowsOptionsValidationException()
        {
            var services = new ServiceCollection();
            services.AddOptions<InertiaOptions>()
                .Configure(o =>
                {
                    o.RootView = "~/Views/App.cshtml";
                    o.SsrUrl = "not-a-url";
                })
                .ValidateDataAnnotations()
                .ValidateOnStart();
            var serviceProvider = services.BuildServiceProvider();

            var act = () => serviceProvider.GetRequiredService<IOptions<InertiaOptions>>().Value;

            act.Should().Throw<OptionsValidationException>();
        }
    }
}
