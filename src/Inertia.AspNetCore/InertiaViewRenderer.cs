using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Inertia.AspNetCore;

/// <summary>
/// Renders Razor views for initial Inertia page loads. Resolved from DI by
/// <see cref="InertiaResponse"/> to render the root view containing Tag Helpers.
/// </summary>
internal sealed class InertiaViewRenderer
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;

    public InertiaViewRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
    }

    /// <summary>Renders the specified Razor view to the HTTP response body.</summary>
    public async Task RenderAsync(
        HttpContext httpContext,
        string viewName,
        IDictionary<string, object?>? viewData = null)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        // Try absolute path first (~/Views/App.cshtml), then search by name
        var viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);
        if (!viewResult.Success)
            viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: true);

        if (!viewResult.Success)
        {
            throw new InvalidOperationException(
                $"Inertia root view '{viewName}' not found. Searched locations: {string.Join(", ", viewResult.SearchedLocations ?? [])}");
        }

        var viewDataDict = new ViewDataDictionary(
            new EmptyModelMetadataProvider(), new ModelStateDictionary());

        if (viewData is not null)
        {
            foreach (var (key, value) in viewData)
                viewDataDict[key] = value;
        }

        var tempData = new TempDataDictionary(httpContext, _tempDataProvider);

        await using var writer = new StreamWriter(httpContext.Response.Body, leaveOpen: true);
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDataDict,
            tempData,
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        await writer.FlushAsync();
    }
}
