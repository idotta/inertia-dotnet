namespace Inertia.AspNetCore;

/// <summary>
/// Represents the result of handling an exception in an Inertia context.
/// Use static factory methods to create instances.
/// </summary>
public sealed class InertiaExceptionResult
{
    /// <summary>The component to render for the error page.</summary>
    internal string? Component { get; private set; }

    /// <summary>The props to pass to the error component.</summary>
    internal IDictionary<string, object?>? Props { get; private set; }

    /// <summary>The URL to redirect to instead of rendering an error page.</summary>
    internal string? RedirectUrl { get; private set; }

    /// <summary>Whether to include shared data from the Inertia middleware.</summary>
    internal bool IncludeSharedData { get; private set; }

    /// <summary>A custom root view to use for the error page.</summary>
    internal string? CustomRootView { get; private set; }

    private InertiaExceptionResult() { }

    /// <summary>Renders an Inertia component as the error page.</summary>
    /// <param name="component">The JavaScript page component name.</param>
    /// <param name="props">An optional object whose public properties become page props.</param>
    public static InertiaExceptionResult Render(string component, object? props = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);

        var propsDict = props switch
        {
            null => new Dictionary<string, object?>(),
            IDictionary<string, object?> d => new Dictionary<string, object?>(d),
            IInertiaPropertyProvider provider => new Dictionary<string, object?> { ["0"] = provider },
            _ => PropHelpers.ObjectToDictionary(props),
        };
        return new InertiaExceptionResult { Component = component, Props = propsDict };
    }

    /// <summary>Renders an Inertia component as the error page with a props dictionary.</summary>
    /// <param name="component">The JavaScript page component name.</param>
    /// <param name="props">A dictionary of page props.</param>
    public static InertiaExceptionResult Render(string component, IDictionary<string, object?> props)
        => new() { Component = component, Props = new Dictionary<string, object?>(props) };

    /// <summary>Redirects instead of rendering an error page.</summary>
    /// <param name="url">The target URL.</param>
    public static InertiaExceptionResult Redirect(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new() { RedirectUrl = url };
    }

    /// <summary>Include shared data from the Inertia middleware in the error page props.</summary>
    public InertiaExceptionResult WithSharedData()
    {
        IncludeSharedData = true;
        return this;
    }

    /// <summary>Use a custom root view for the error page.</summary>
    /// <param name="rootView">The Razor view path.</param>
    public InertiaExceptionResult RootView(string rootView)
    {
        CustomRootView = rootView;
        return this;
    }

}
