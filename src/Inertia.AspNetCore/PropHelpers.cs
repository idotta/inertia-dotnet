using System.Reflection;
using System.Text.Json;

namespace Inertia.AspNetCore;

/// <summary>
/// Internal helper for converting objects to prop dictionaries.
/// </summary>
internal static class PropHelpers
{
    /// <summary>
    /// Converts an object's public instance properties to a dictionary with camelCase keys.
    /// </summary>
    internal static Dictionary<string, object?> ObjectToDictionary(object obj)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead)
                dict[JsonNamingPolicy.CamelCase.ConvertName(prop.Name)] = prop.GetValue(obj);
        }
        return dict;
    }
}
