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
    /// Asserts that the response is a valid Inertia response and runs the async assertion callback.
    /// Use this overload when assertions involve async operations like <see cref="AssertableInertia.ReloadAsync"/>.
    /// </summary>
    public static async Task<HttpResponseMessage> AssertInertia(
        this HttpResponseMessage response,
        Func<AssertableInertia, Task> asyncCallback,
        HttpClient? httpClient = null)
    {
        var assertable = await AssertableInertia.FromResponseAsync(response, httpClient);
        await asyncCallback(assertable);
        return response;
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
    /// Asserts that the response is a valid Inertia response and runs the async assertion callback.
    /// Task overload for chaining with HttpClient calls.
    /// </summary>
    public static async Task<HttpResponseMessage> AssertInertia(
        this Task<HttpResponseMessage> responseTask,
        Func<AssertableInertia, Task> asyncCallback,
        HttpClient? httpClient = null)
    {
        var response = await responseTask;
        return await response.AssertInertia(asyncCallback, httpClient);
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

    // ---- Flash assertions on redirect responses ----

    /// <summary>
    /// Follows a redirect response and asserts the resulting Inertia page has the given flash key.
    /// </summary>
    /// <param name="response">A redirect response (3xx) with a Location header.</param>
    /// <param name="key">The flash data key to assert.</param>
    /// <param name="httpClient">The HttpClient used to follow the redirect.</param>
    public static async Task<HttpResponseMessage> AssertInertiaFlash(
        this HttpResponseMessage response,
        string key,
        HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(httpClient);

        var location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException(
                "Cannot follow redirect: response has no Location header.");

        var followedResponse = await httpClient.GetAsync(location);
        var assertable = await AssertableInertia.FromResponseAsync(followedResponse, httpClient);
        assertable.HasFlash(key);
        return response;
    }

    /// <summary>
    /// Follows a redirect response and asserts the resulting Inertia page has the given flash key with the expected value.
    /// </summary>
    /// <param name="response">A redirect response (3xx) with a Location header.</param>
    /// <param name="key">The flash data key to assert.</param>
    /// <param name="expected">The expected flash value.</param>
    /// <param name="httpClient">The HttpClient used to follow the redirect.</param>
    public static async Task<HttpResponseMessage> AssertInertiaFlash(
        this HttpResponseMessage response,
        string key,
        object? expected,
        HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(httpClient);

        var location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException(
                "Cannot follow redirect: response has no Location header.");

        var followedResponse = await httpClient.GetAsync(location);
        var assertable = await AssertableInertia.FromResponseAsync(followedResponse, httpClient);
        assertable.HasFlash(key, expected);
        return response;
    }

    /// <summary>
    /// Follows a redirect response and asserts the resulting Inertia page has the given flash key.
    /// Task overload for chaining with HttpClient calls.
    /// </summary>
    /// <param name="responseTask">A task returning a redirect response (3xx) with a Location header.</param>
    /// <param name="key">The flash data key to assert.</param>
    /// <param name="httpClient">The HttpClient used to follow the redirect.</param>
    public static async Task<HttpResponseMessage> AssertInertiaFlash(
        this Task<HttpResponseMessage> responseTask,
        string key,
        HttpClient httpClient)
    {
        var response = await responseTask;
        return await response.AssertInertiaFlash(key, httpClient);
    }

    /// <summary>
    /// Follows a redirect response and asserts the resulting Inertia page has the given flash key with the expected value.
    /// Task overload for chaining with HttpClient calls.
    /// </summary>
    /// <param name="responseTask">A task returning a redirect response (3xx) with a Location header.</param>
    /// <param name="key">The flash data key to assert.</param>
    /// <param name="expected">The expected flash value.</param>
    /// <param name="httpClient">The HttpClient used to follow the redirect.</param>
    public static async Task<HttpResponseMessage> AssertInertiaFlash(
        this Task<HttpResponseMessage> responseTask,
        string key,
        object? expected,
        HttpClient httpClient)
    {
        var response = await responseTask;
        return await response.AssertInertiaFlash(key, expected, httpClient);
    }
}
