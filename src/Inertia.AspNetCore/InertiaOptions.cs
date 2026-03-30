using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>
/// Configuration options for the Inertia.js ASP.NET Core adapter.
/// </summary>
public sealed class InertiaOptions
{
    /// <summary>The configuration section name for binding from appsettings.json.</summary>
    public const string Section = "Inertia";

    /// <summary>The default Razor view used as the root template for Inertia responses.</summary>
    [Required]
    public string RootView { get; set; } = "~/Views/App.cshtml";

    /// <summary>When enabled, page data is encrypted before being stored in the browser's history state.</summary>
    public bool EncryptHistory { get; set; }

    /// <summary>When enabled, each page response includes a sharedProps metadata key listing the top-level shared prop keys.</summary>
    public bool ExposeSharedPropKeys { get; set; } = true;

    // -- SSR --

    /// <summary>Enable server-side rendering.</summary>
    public bool SsrEnabled { get; set; } = true;

    /// <summary>The base URL of the SSR server. The /render path is appended at call time.</summary>
    [Url]
    public string SsrUrl { get; set; } = "http://127.0.0.1:13714";

    /// <summary>When enabled, the SSR gateway verifies the SSR bundle file exists before dispatching.</summary>
    public bool SsrEnsureBundleExists { get; set; } = true;

    /// <summary>The absolute path to the SSR bundle file, or null to use auto-detection.</summary>
    public string? SsrBundle { get; set; }

    /// <summary>When enabled, SSR rendering failures throw an exception instead of falling back to client-side rendering.</summary>
    public bool SsrThrowOnError { get; set; }

    // -- Pages --

    /// <summary>When enabled, component names are validated against the file system during rendering.</summary>
    public bool EnsurePagesExist { get; set; }

    /// <summary>Directories to search for page component files when EnsurePagesExist is enabled.</summary>
    public string[] PagePaths { get; set; } = [];

    /// <summary>File extensions to consider when searching for page components.</summary>
    public string[] PageExtensions { get; set; } = ["js", "jsx", "svelte", "ts", "tsx", "vue"];

    // -- Testing --

    /// <summary>When enabled in test environments, component names are validated against the file system.</summary>
    public bool TestingEnsurePagesExist { get; set; } = true;

    // -- Serialization --

    /// <summary>Custom JSON serializer options. When null, a default instance with CamelCase naming policy is used.</summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    // -- Middleware delegates --

    /// <summary>Delegate to resolve the current asset version. When null, an empty string is used.</summary>
    public Func<HttpContext, string>? VersionProvider { get; set; }

    /// <summary>Delegate to resolve the root view per-request. When null, <see cref="RootView"/> is used.</summary>
    public Func<HttpContext, string>? RootViewProvider { get; set; }

    /// <summary>Delegate to provide shared props for every Inertia response. Called by the middleware.</summary>
    public Func<HttpContext, IServiceProvider, IDictionary<string, object?>>? SharedPropsProvider { get; set; }

    /// <summary>Delegate to resolve the page URL for Inertia responses. When null, the URL is built from PathBase + Path + QueryString.</summary>
    public Func<HttpContext, string>? UrlResolver { get; set; }

    /// <summary>
    /// Delegate to resolve validation errors for the current request. The returned dictionary
    /// is shared as an <see cref="AlwaysProp{T}"/> under the <c>"errors"</c> key, ensuring it is
    /// always included even during partial reloads.
    /// <para>The <c>string?</c> parameter is the value of the <c>X-Inertia-Error-Bag</c> request header, or null when absent.</para>
    /// <para>When null, no <c>"errors"</c> prop is shared automatically.</para>
    /// </summary>
    public Func<HttpContext, string?, IDictionary<string, object?>>? ValidationErrorProvider { get; set; }

    /// <summary>
    /// Delegate to provide props that are shared once and cached on the client across navigations.
    /// Values that already implement <see cref="IOnceable"/> are shared directly;
    /// other values are wrapped in <see cref="OnceProp{T}"/>.
    /// </summary>
    public Func<HttpContext, IServiceProvider, IDictionary<string, object?>>? SharedOncePropsProvider { get; set; }

    /// <summary>Delegate invoked when the client's asset version does not match the server's. Default behavior returns 409 Conflict with X-Inertia-Location.</summary>
    public Func<HttpContext, IResult>? OnVersionChange { get; set; }

    /// <summary>Delegate invoked when the response body is empty. Default behavior returns 204 No Content.</summary>
    public Func<HttpContext, IResult>? OnEmptyResponse { get; set; }

    // -- Exception handling --

    /// <summary>Delegate invoked when an exception occurs. Return an <see cref="InertiaExceptionResult"/> to render
    /// an Inertia error page or redirect, or null to fall through to the default exception handler.</summary>
    public Func<InertiaExceptionContext, InertiaExceptionResult?>? ExceptionHandler { get; set; }
}
