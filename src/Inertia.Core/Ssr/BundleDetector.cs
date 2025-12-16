namespace Inertia.Core.Ssr;

/// <summary>
/// Detects the location of the SSR bundle by searching common paths.
/// </summary>
public class BundleDetector
{
    /// <summary>
    /// Gets the default search paths for SSR bundles.
    /// </summary>
    private static readonly string[] DefaultSearchPaths = new[]
    {
        "wwwroot/ssr/ssr.mjs",
        "wwwroot/ssr/ssr.js",
        "bootstrap/ssr/ssr.mjs",
        "bootstrap/ssr/ssr.js",
        "public/ssr/ssr.mjs",
        "public/ssr/ssr.js"
    };

    /// <summary>
    /// Detects the SSR bundle location by searching common paths.
    /// </summary>
    /// <param name="basePath">The base path to search from. Defaults to the current directory.</param>
    /// <param name="customPath">An optional custom path to check first.</param>
    /// <returns>The full path to the SSR bundle if found, otherwise null.</returns>
    public static string? Detect(string? basePath = null, string? customPath = null)
    {
        basePath ??= Directory.GetCurrentDirectory();

        // First check custom path if provided
        if (!string.IsNullOrEmpty(customPath))
        {
            var customFullPath = Path.IsPathRooted(customPath)
                ? customPath
                : Path.Combine(basePath, customPath);

            if (File.Exists(customFullPath))
            {
                return Path.GetFullPath(customFullPath);
            }
        }

        // Then search default paths
        foreach (var searchPath in DefaultSearchPaths)
        {
            var fullPath = Path.Combine(basePath, searchPath);
            if (File.Exists(fullPath))
            {
                return Path.GetFullPath(fullPath);
            }
        }

        return null;
    }

    /// <summary>
    /// Detects the SSR bundle location by searching common paths with a set of custom paths.
    /// </summary>
    /// <param name="basePath">The base path to search from. Defaults to the current directory.</param>
    /// <param name="customPaths">An optional array of custom paths to check first.</param>
    /// <returns>The full path to the SSR bundle if found, otherwise null.</returns>
    public static string? Detect(string? basePath, params string[]? customPaths)
    {
        basePath ??= Directory.GetCurrentDirectory();

        // First check custom paths if provided
        if (customPaths != null)
        {
            foreach (var customPath in customPaths)
            {
                if (string.IsNullOrEmpty(customPath))
                {
                    continue;
                }

                var customFullPath = Path.IsPathRooted(customPath)
                    ? customPath
                    : Path.Combine(basePath, customPath);

                if (File.Exists(customFullPath))
                {
                    return Path.GetFullPath(customFullPath);
                }
            }
        }

        // Then search default paths
        foreach (var searchPath in DefaultSearchPaths)
        {
            var fullPath = Path.Combine(basePath, searchPath);
            if (File.Exists(fullPath))
            {
                return Path.GetFullPath(fullPath);
            }
        }

        return null;
    }
}
