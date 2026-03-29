using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.AspNetCore;

/// <summary>
/// Renders the Inertia application container. When SSR is available, outputs the
/// pre-rendered HTML body. Otherwise, renders a div with page data for client-side rendering.
/// </summary>
/// <remarks>
/// Usage in Razor view: <c>&lt;inertia-app&gt;&lt;/inertia-app&gt;</c>
/// or <c>&lt;inertia-app id="my-app"&gt;&lt;/inertia-app&gt;</c>.
/// </remarks>
[HtmlTargetElement("inertia-app")]
public sealed class InertiaAppTagHelper : TagHelper
{
    /// <summary>The HTML element ID for the app container. Defaults to "app".</summary>
    public string Id { get; set; } = "app";

    /// <summary>The current Razor view context.</summary>
    [HtmlAttributeNotBound]
    [ViewContextAttribute]
    public ViewContext ViewContext { get; set; } = null!;

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // Suppress the <inertia-app> wrapper tag

        var httpContext = ViewContext.HttpContext;

        if (httpContext.Items["InertiaPageJson"] is not string pageJson)
        {
            output.SuppressOutput();
            return;
        }

        // Resolve scoped SsrState from RequestServices (internal type, not constructor-injectable)
        var ssrState = httpContext.RequestServices.GetService<SsrState>();

        // Attempt SSR dispatch (respects path exclusions)
        SsrResponse? ssrResponse = null;
        if (ssrState is not null && !ssrState.IsPathExcluded(httpContext.Request.Path))
            ssrResponse = await ssrState.DispatchAsync();

        if (ssrResponse is not null)
        {
            // SSR: render the pre-rendered body HTML
            output.Content.SetHtmlContent(ssrResponse.Body);
        }
        else
        {
            // CSR fallback: div with page data attribute
            output.Content.SetHtmlContent(
                $"""<div id="{Id}" data-page="{System.Web.HttpUtility.HtmlAttributeEncode(pageJson)}"></div>""");
        }
    }
}
