using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.AspNetCore;

/// <summary>
/// Renders SSR head content (meta tags, title, etc.) when server-side rendering is available.
/// Falls back to rendering child content when SSR is not available.
/// </summary>
/// <remarks>
/// Usage in Razor view: <c>&lt;inertia-head&gt;&lt;/inertia-head&gt;</c>
/// or with fallback: <c>&lt;inertia-head&gt;&lt;title&gt;Default&lt;/title&gt;&lt;/inertia-head&gt;</c>.
/// </remarks>
[HtmlTargetElement("inertia-head")]
public sealed class InertiaHeadTagHelper : TagHelper
{
    /// <summary>The current Razor view context.</summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // Suppress the <inertia-head> wrapper tag

        // Resolve scoped SsrState from RequestServices (internal type, not constructor-injectable)
        var ssrState = ViewContext.HttpContext.RequestServices.GetService<SsrState>();
        var ssrResponse = ssrState is not null ? await ssrState.DispatchAsync().ConfigureAwait(false) : null;

        if (ssrResponse is not null)
        {
            // SSR: render head content (meta tags, title, etc.)
            output.Content.SetHtmlContent(ssrResponse.Head);
        }
        else
        {
            // CSR fallback: render child content (slot)
            var childContent = await output.GetChildContentAsync().ConfigureAwait(false);
            output.Content.SetHtmlContent(childContent);
        }
    }
}
