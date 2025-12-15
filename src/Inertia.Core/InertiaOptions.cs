namespace Inertia.Core;

/// <summary>
/// Configuration options for Inertia.js integration.
/// </summary>
public class InertiaOptions
{
    /// <summary>
    /// Gets or sets the name of the root view template that serves as the HTML wrapper
    /// for the Inertia root element. Default is "app".
    /// </summary>
    public string RootView { get; set; } = "app";

    /// <summary>
    /// Gets or sets a value indicating whether to validate that page components exist
    /// before rendering. Default is false.
    /// </summary>
    public bool EnsurePagesExist { get; set; } = false;

    /// <summary>
    /// Gets or sets the paths where page components are located.
    /// Used for component validation when EnsurePagesExist is true.
    /// </summary>
    public List<string> PagePaths { get; set; } = new();

    /// <summary>
    /// Gets or sets the file extensions to search for when validating page components.
    /// </summary>
    public List<string> PageExtensions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to use a script element for initial page data
    /// instead of a JSON data attribute. Default is false.
    /// </summary>
    public bool UseScriptElement { get; set; } = false;

    /// <summary>
    /// Gets or sets the server-side rendering configuration.
    /// </summary>
    public SsrOptions Ssr { get; set; } = new();

    /// <summary>
    /// Gets or sets the testing-specific configuration.
    /// </summary>
    public TestingOptions Testing { get; set; } = new();

    /// <summary>
    /// Gets or sets the history management configuration.
    /// </summary>
    public HistoryOptions History { get; set; } = new();
}

/// <summary>
/// Server-side rendering configuration options.
/// </summary>
public class SsrOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether server-side rendering is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the URL of the SSR server.
    /// Default is "http://127.0.0.1:13714".
    /// </summary>
    public string Url { get; set; } = "http://127.0.0.1:13714";

    /// <summary>
    /// Gets or sets the path to the SSR bundle.
    /// If not specified, the bundle will be auto-detected.
    /// </summary>
    public string? Bundle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ensure the SSR bundle exists.
    /// Default is true.
    /// </summary>
    public bool EnsureBundleExists { get; set; } = true;
}

/// <summary>
/// Testing-specific configuration options.
/// </summary>
public class TestingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to validate that page components exist
    /// during testing. Default is true.
    /// </summary>
    public bool EnsurePagesExist { get; set; } = true;

    /// <summary>
    /// Gets or sets the paths where page components are located for testing.
    /// </summary>
    public List<string> PagePaths { get; set; } = new();

    /// <summary>
    /// Gets or sets the file extensions to search for when validating page components
    /// during testing.
    /// </summary>
    public List<string> PageExtensions { get; set; } = new();
}

/// <summary>
/// History management configuration options.
/// </summary>
public class HistoryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to encrypt browser history.
    /// Default is false.
    /// </summary>
    public bool Encrypt { get; set; } = false;
}
