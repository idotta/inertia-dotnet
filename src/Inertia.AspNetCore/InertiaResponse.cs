using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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
    private string _rootView;
    private readonly string _version;
    private readonly bool _encryptHistory;
    private readonly bool _clearHistory;
    private readonly bool _preserveFragment;
    private readonly IDictionary<string, object?>? _flash;
    private readonly bool _exposeSharedPropKeys;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Action<string, object?>? _flashAction;
    private readonly Func<HttpContext, string>? _urlResolver;
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
        bool exposeSharedPropKeys,
        JsonSerializerOptions? jsonOptions,
        Func<HttpContext, string>? urlResolver = null,
        Action<string, object?>? flashAction = null)
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
        _exposeSharedPropKeys = exposeSharedPropKeys;
        _jsonOptions = jsonOptions ?? InertiaPage.DefaultJsonOptions;
        _flashAction = flashAction;
        _urlResolver = urlResolver;
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

    /// <summary>Adds a prop to this response.</summary>
    /// <param name="key">The prop key.</param>
    /// <param name="value">The prop value.</param>
    /// <returns>This response for fluent chaining.</returns>
    public InertiaResponse With(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _props[key] = value;
        return this;
    }

    /// <summary>Merges multiple props into this response.</summary>
    /// <param name="props">A dictionary of props to merge.</param>
    /// <returns>This response for fluent chaining.</returns>
    public InertiaResponse With(IDictionary<string, object?> props)
    {
        ArgumentNullException.ThrowIfNull(props);
        foreach (var (key, value) in props)
            _props[key] = value;
        return this;
    }

    /// <summary>Adds a property provider to this response.</summary>
    /// <param name="provider">The property provider.</param>
    /// <returns>This response for fluent chaining.</returns>
    public InertiaResponse With(IInertiaPropertyProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _props[_props.Count.ToString()] = provider;
        return this;
    }

    /// <summary>Overrides the root view for this response only.</summary>
    /// <param name="rootView">The Razor view path.</param>
    /// <returns>This response for fluent chaining.</returns>
    public InertiaResponse WithRootView(string rootView)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootView);
        _rootView = rootView;
        return this;
    }

    /// <summary>Adds flash data to the current request.</summary>
    /// <param name="key">The flash data key.</param>
    /// <param name="value">The flash data value.</param>
    /// <returns>This response for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">When the response was not created through <see cref="IInertia.Render(string, object?)"/>.</exception>
    public InertiaResponse Flash(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_flashAction is null)
            throw new InvalidOperationException(
                "Flash support requires the response to be created through IInertia.Render().");
        _flashAction(key, value);
        return this;
    }

    /// <summary>Adds multiple flash data entries to the current request.</summary>
    /// <param name="data">A dictionary of flash data.</param>
    /// <returns>This response for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">When the response was not created through <see cref="IInertia.Render(string, object?)"/>.</exception>
    public InertiaResponse Flash(IDictionary<string, object?> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (_flashAction is null)
            throw new InvalidOperationException(
                "Flash support requires the response to be created through IInertia.Render().");
        foreach (var (key, value) in data)
            _flashAction(key, value);
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
        var page = await BuildPageAsync(httpContext);

        if (httpContext.Request.Headers.ContainsKey(InertiaHeaderNames.Inertia))
        {
            // Inertia request -- return JSON
            if (httpContext.Response.StatusCode is 0 or StatusCodes.Status200OK)
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

            // Store page on SsrState for SSR dispatch (Tag Helpers call DispatchAsync)
            if (httpContext.RequestServices?.GetService<SsrState>() is { } ssrState)
                ssrState.SetPage(page);

            if (httpContext.Response.StatusCode is 0 or StatusCodes.Status200OK)
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "text/html; charset=utf-8";

            // Store view data for Razor view
            if (_viewData is not null)
            {
                foreach (var (key, value) in _viewData)
                    httpContext.Items[$"InertiaViewData:{key}"] = value;
            }

            // Render the root Razor view (contains <inertia-app> and <inertia-head> Tag Helpers)
            var viewRenderer = httpContext.RequestServices?.GetService<InertiaViewRenderer>();
            if (viewRenderer is not null)
            {
                await viewRenderer.RenderAsync(httpContext, _rootView, _viewData);
            }
            else
            {
                // Fallback when DI is not configured (e.g., unit tests without AddInertia)
                var pageJson = page.ToJson(_jsonOptions);
                await httpContext.Response.WriteAsync(
                    $"<script data-page=\"app\" type=\"application/json\">{pageJson}</script><div id=\"app\"></div>");
            }
        }
    }

    private async Task<InertiaPage> BuildPageAsync(HttpContext httpContext)
    {
        var resolver = new PropsResolver(httpContext, _component, _exposeSharedPropKeys);
        var (resolvedProps, metadata) = await resolver.ResolveAsync(_sharedProps, _sharedProviders, _props);

        return new InertiaPage
        {
            Component = _component,
            Props = resolvedProps,
            Url = _urlResolver?.Invoke(httpContext) ?? GetUrl(httpContext.Request),
            Version = _version,
            ClearHistory = _clearHistory,
            EncryptHistory = _encryptHistory,
            PreserveFragment = _preserveFragment,
            Flash = _flash,
            DeferredProps = metadata.DeferredProps,
            MergeProps = metadata.MergeProps,
            PrependProps = metadata.PrependProps,
            DeepMergeProps = metadata.DeepMergeProps,
            MatchPropsOn = metadata.MatchPropsOn,
            ScrollProps = metadata.ScrollProps,
            OnceProps = metadata.OnceProps,
            SharedProps = metadata.SharedProps,
        };
    }

    private static string GetUrl(HttpRequest request)
    {
        var path = (request.PathBase.Value ?? "") + (request.Path.Value ?? "/");
        var query = request.QueryString.Value ?? "";
        return path + query;
    }
}
