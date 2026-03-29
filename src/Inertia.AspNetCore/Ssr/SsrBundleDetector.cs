using Microsoft.Extensions.Options;

namespace Inertia.AspNetCore;

/// <summary>Detects the SSR bundle file path by checking configured and default locations.</summary>
internal sealed class SsrBundleDetector
{
    private readonly InertiaOptions _options;
    private readonly Func<string, bool> _fileExists;

    /// <summary>Default search paths (ASP.NET Core equivalents of Laravel's bootstrap/ssr paths).</summary>
    internal static readonly string[] DefaultPaths =
    [
        "wwwroot/js/ssr.js",
        "wwwroot/js/ssr.mjs",
        "wwwroot/js/app.js",
        "wwwroot/js/app.mjs",
    ];

    public SsrBundleDetector(IOptions<InertiaOptions> options)
        : this(options.Value, File.Exists)
    {
    }

    /// <summary>Internal constructor for testability (no file system dependency).</summary>
    internal SsrBundleDetector(InertiaOptions options, Func<string, bool> fileExists)
    {
        _options = options;
        _fileExists = fileExists;
    }

    /// <summary>Returns the first existing bundle path, or null if no bundle is found.</summary>
    public string? Detect()
    {
        if (_options.SsrBundle is { } custom)
            return _fileExists(custom) ? custom : null;

        foreach (var path in DefaultPaths)
        {
            if (_fileExists(path))
                return path;
        }

        return null;
    }
}
