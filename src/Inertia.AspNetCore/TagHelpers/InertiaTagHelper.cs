using System.Text.Encodings.Web;
using System.Text.Json;
using Inertia.Core;
using Inertia.Core.Ssr;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore.TagHelpers;

/// <summary>
/// Tag helper for rendering the Inertia root element.
/// Converts &lt;inertia /&gt; to a div with the page data,
/// handling both client-side rendering and server-side rendering.
/// </summary>
[HtmlTargetElement("inertia")]
public class InertiaTagHelper : TagHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IGateway? _gateway;
    private readonly InertiaOptions _options;

    /// <summary>
    /// Gets or sets the ViewContext.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ID for the root element. Default is "app".
    /// </summary>
    [HtmlAttributeName("id")]
    public string Id { get; set; } = "app";

    /// <summary>
    /// Initializes a new instance of the <see cref="InertiaTagHelper"/> class.
    /// </summary>
    /// <param name="gateway">The SSR gateway (optional).</param>
    /// <param name="options">The Inertia options.</param>
    public InertiaTagHelper(IOptions<InertiaOptions> options, IGateway? gateway = null)
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
            // If no page data, render an empty div
            output.TagName = "div";
            output.Attributes.SetAttribute("id", Id);
            output.Content.Clear();
            return;
        }

        // Try SSR first if enabled
        var ssrResponse = await TryGetSsrResponseAsync(page);

        if (ssrResponse != null)
        {
            // SSR is available - render the SSR body content
            output.TagName = null; // Remove the tag wrapper
            output.Content.SetHtmlContent(ssrResponse.Body);
        }
        else
        {
            // CSR fallback - render the page data for client-side hydration
            var pageJson = JsonSerializer.Serialize(page, JsonOptions);

            if (_options.UseScriptElement)
            {
                // Render using script element approach
                output.TagName = null; // Remove the tag wrapper
                output.Content.SetHtmlContent(
                    $"<script data-page=\"{Id}\" type=\"application/json\">{pageJson}</script><div id=\"{Id}\"></div>");
            }
            else
            {
                // Default: render using data-page attribute
                output.TagName = "div";
                output.Attributes.SetAttribute("id", Id);
                output.Attributes.SetAttribute("data-page", pageJson);
                output.Content.Clear();
            }
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
            // Silently fail and fallback to CSR
            return null;
        }
    }
}
