using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inertia.AspNetCore;

/// <summary>
/// Represents an Inertia page response. Implements both IActionResult (MVC) and IResult (minimal APIs).
/// </summary>
public sealed class InertiaResponse : IActionResult, IResult
{
    private readonly string _component;
    private readonly IDictionary<string, object?> _props;
    private readonly IDictionary<string, object?> _sharedProps;
    private readonly IReadOnlyList<IInertiaPropertyProvider> _sharedProviders;
    private readonly string _rootView;
    private readonly string _version;
    private readonly bool _encryptHistory;
    private readonly bool _clearHistory;
    private readonly bool _preserveFragment;
    private readonly IDictionary<string, object?>? _flash;
    private readonly JsonSerializerOptions _jsonOptions;
    private Dictionary<string, object?>? _viewData;

    internal InertiaResponse(
        string component,
        IDictionary<string, object?> props,
        IDictionary<string, object?> sharedProps,
        IReadOnlyList<IInertiaPropertyProvider> sharedProviders,
        string rootView,
        string version,
        bool encryptHistory,
        bool clearHistory,
        bool preserveFragment,
        IDictionary<string, object?>? flash,
        JsonSerializerOptions? jsonOptions)
    {
        _component = component;
        _props = props;
        _sharedProps = sharedProps;
        _sharedProviders = sharedProviders;
        _rootView = rootView;
        _version = version;
        _encryptHistory = encryptHistory;
        _clearHistory = clearHistory;
        _preserveFragment = preserveFragment;
        _flash = flash;
        _jsonOptions = jsonOptions ?? InertiaPage.DefaultJsonOptions;
    }

    /// <summary>Adds view data for the Razor view (initial page load only).</summary>
    public InertiaResponse WithViewData(string key, object? value)
    {
        _viewData ??= [];
        _viewData[key] = value;
        return this;
    }

    /// <summary>Adds view data for the Razor view (initial page load only).</summary>
    public InertiaResponse WithViewData(IDictionary<string, object?> data)
    {
        _viewData ??= [];
        foreach (var (key, value) in data)
            _viewData[key] = value;
        return this;
    }

    /// <summary>The component name for this response.</summary>
    internal string Component => _component;

    /// <summary>The page props for this response.</summary>
    internal IDictionary<string, object?> Props => _props;

    /// <summary>The root view for this response.</summary>
    internal string RootView => _rootView;

    /// <summary>The asset version for this response.</summary>
    internal string Version => _version;

    /// <inheritdoc />
    public Task ExecuteResultAsync(ActionContext context)
        => Execute(context.HttpContext);

    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
        => Execute(httpContext);

    private async Task Execute(HttpContext httpContext)
    {
        var page = BuildPage(httpContext);

        if (httpContext.Request.Headers.ContainsKey(InertiaHeaderNames.Inertia))
        {
            // Inertia request -- return JSON
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.Headers[InertiaHeaderNames.Inertia] = "true";
            httpContext.Response.ContentType = "application/json";
            var json = page.ToJson(_jsonOptions);
            await httpContext.Response.WriteAsync(json);
        }
        else
        {
            // Initial page load -- store page in HttpContext.Items for Tag Helpers
            httpContext.Items["InertiaPage"] = page;
            httpContext.Items["InertiaPageJson"] = page.ToJson(_jsonOptions);

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "text/html; charset=utf-8";

            // Store view data for when view rendering is set up
            if (_viewData is not null)
            {
                foreach (var (key, value) in _viewData)
                    httpContext.Items[$"InertiaViewData:{key}"] = value;
            }

            // Write a minimal response that includes the page data.
            // This will be replaced by proper Razor view rendering in Phase 7 (Tag Helpers).
            var pageJson = page.ToJson(_jsonOptions);
            await httpContext.Response.WriteAsync(
                $"<div id=\"app\" data-page='{System.Web.HttpUtility.HtmlAttributeEncode(pageJson)}'></div>");
        }
    }

    private InertiaPage BuildPage(HttpContext httpContext)
    {
        // Merge shared props with page props (shared first, page overrides)
        var mergedProps = new Dictionary<string, object?>(_sharedProps);
        foreach (var (key, value) in _props)
            mergedProps[key] = value;

        // NOTE: Full prop resolution (partial filtering, deferred exclusion, metadata collection)
        // is deferred to Phase 4 (PropsResolver). For now, pass props through directly.

        return new InertiaPage
        {
            Component = _component,
            Props = mergedProps,
            Url = GetUrl(httpContext.Request),
            Version = _version,
            ClearHistory = _clearHistory,
            EncryptHistory = _encryptHistory,
            PreserveFragment = _preserveFragment,
            Flash = _flash,
        };
    }

    private static string GetUrl(HttpRequest request)
    {
        var path = request.Path.Value ?? "/";
        var query = request.QueryString.Value ?? "";
        return path + query;
    }
}
