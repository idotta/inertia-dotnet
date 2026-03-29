using System.Text.Json;

namespace Inertia.Testing;

/// <summary>
/// Extension methods for asserting on Inertia responses in integration tests.
/// </summary>
public static class InertiaTestExtensions
{
    /// <summary>
    /// Asserts that the response is a valid Inertia response and runs the assertion callback.
    /// </summary>
    public static async Task<HttpResponseMessage> AssertInertia(
        this HttpResponseMessage response,
        Action<AssertableInertia> callback,
        HttpClient? httpClient = null)
    {
        var assertable = await AssertableInertia.FromResponseAsync(response, httpClient);
        callback(assertable);
        return response;
    }

    /// <summary>
    /// Asserts that the response is a valid Inertia response (no additional assertions).
    /// </summary>
    public static async Task<HttpResponseMessage> AssertInertia(
        this HttpResponseMessage response)
    {
        await AssertableInertia.FromResponseAsync(response);
        return response;
    }

    /// <summary>
    /// Extracts the Inertia page data as a dictionary.
    /// </summary>
    public static async Task<IDictionary<string, object?>> InertiaPage(
        this HttpResponseMessage response)
    {
        var assertable = await AssertableInertia.FromResponseAsync(response);
        return assertable.ToPage();
    }

    /// <summary>
    /// Extracts Inertia props, optionally navigating to a specific prop via dot notation.
    /// </summary>
    public static async Task<JsonElement> InertiaProps(
        this HttpResponseMessage response, string? propName = null)
    {
        var assertable = await AssertableInertia.FromResponseAsync(response);

        if (propName is not null)
            return assertable.Prop(propName);

        return assertable.GetProps();
    }

    /// <summary>
    /// Asserts that the response is a valid Inertia response and runs the assertion callback.
    /// Task overload for chaining with HttpClient calls.
    /// </summary>
    public static async Task<HttpResponseMessage> AssertInertia(
        this Task<HttpResponseMessage> responseTask,
        Action<AssertableInertia> callback,
        HttpClient? httpClient = null)
    {
        var response = await responseTask;
        return await response.AssertInertia(callback, httpClient);
    }

    /// <summary>
    /// Asserts that the response is a valid Inertia response.
    /// Task overload for chaining with HttpClient calls.
    /// </summary>
    public static async Task<HttpResponseMessage> AssertInertia(
        this Task<HttpResponseMessage> responseTask)
    {
        var response = await responseTask;
        return await response.AssertInertia();
    }
}
