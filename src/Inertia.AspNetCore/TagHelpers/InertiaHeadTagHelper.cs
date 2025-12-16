using System.Text.Json;
using Inertia.Core;
using Inertia.Core.Ssr;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore.TagHelpers;

/// <summary>
/// Tag helper for rendering the Inertia head content from SSR.
/// Converts &lt;inertia-head /&gt; to the SSR head content if available.
/// </summary>
[HtmlTargetElement("inertia-head")]
public class InertiaHeadTagHelper : TagHelper
{
    private readonly IGateway? _gateway;
    private readonly InertiaOptions _options;

    /// <summary>
    /// Gets or sets the ViewContext.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="InertiaHeadTagHelper"/> class.
    /// </summary>
    /// <param name="gateway">The SSR gateway (optional).</param>
    /// <param name="options">The Inertia options.</param>
    public InertiaHeadTagHelper(IOptions<InertiaOptions> options, IGateway? gateway = null)
    {
        _gateway = gateway;
        _options = options.Value;
    }

    /// <summary>
    /// Processes the tag helper asynchronously.
    /// </summary>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Get the page data from ViewData
        if (!ViewContext.ViewData.TryGetValue("page", out var pageObj) ||
            pageObj is not Dictionary<string, object?> page)
        {
            // No page data - render nothing
            output.SuppressOutput();
            return;
        }

        // Try to get SSR response
        var ssrResponse = await TryGetSsrResponseAsync(page);

        if (ssrResponse != null && !string.IsNullOrWhiteSpace(ssrResponse.Head))
        {
            // SSR head content is available
            output.TagName = null; // Remove the tag wrapper
            output.Content.SetHtmlContent(ssrResponse.Head);
        }
        else
        {
            // No SSR or no head content - render nothing
            output.SuppressOutput();
        }
    }

    /// <summary>
    /// Attempts to get an SSR response from the gateway.
    /// </summary>
    private async Task<SsrResponse?> TryGetSsrResponseAsync(Dictionary<string, object?> page)
    {
        if (_gateway == null || !_options.Ssr.Enabled)
        {
            return null;
        }

        try
        {
            return await _gateway.DispatchAsync(page);
        }
        catch
        {
            // Silently fail - no head content
            return null;
        }
    }
}
