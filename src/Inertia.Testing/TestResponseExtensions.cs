using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Inertia.Testing;

/// <summary>
/// Extension methods for HttpResponse to support Inertia testing.
/// </summary>
public static class TestResponseExtensions
{
    /// <summary>
    /// Assert that the response is an Inertia response and optionally perform assertions on it.
    /// </summary>
    /// <param name="response">The HTTP response to assert.</param>
    /// <param name="callback">Optional callback to perform assertions on the Inertia response.</param>
    /// <returns>The HttpResponse for method chaining.</returns>
    public static HttpResponse AssertInertia(this HttpResponse response, Action<AssertableInertia>? callback = null)
    {
        var assertable = AssertableInertia.FromResponse(response);

        callback?.Invoke(assertable);

        return response;
    }

    /// <summary>
    /// Get the Inertia page object from the response.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <returns>A dictionary containing the page data (component, props, url, version, etc.).</returns>
    public static Dictionary<string, object?> InertiaPage(this HttpResponse response)
    {
        return AssertableInertia.FromResponse(response).ToArray();
    }

    /// <summary>
    /// Get Inertia props from the response, optionally retrieving a specific prop by key.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="propName">Optional property name to retrieve using dot notation (e.g., "user.name").</param>
    /// <returns>The props dictionary or a specific property value.</returns>
    public static object? InertiaProps(this HttpResponse response, string? propName = null)
    {
        var page = AssertableInertia.FromResponse(response).ToArray();

        if (!page.TryGetValue("props", out var propsObj) || propsObj is not Dictionary<string, object?> props)
        {
            return null;
        }

        if (string.IsNullOrEmpty(propName))
        {
            return props;
        }

        return GetNestedValue(props, propName);
    }

    private static object? GetNestedValue(Dictionary<string, object?> dict, string key)
    {
        var parts = key.Split('.');
        object? current = dict;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object?> currentDict)
            {
                if (!currentDict.TryGetValue(part, out current))
                {
                    return null;
                }
            }
            else if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current;
    }
}
