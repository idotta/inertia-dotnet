using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inertia.AspNetCore;

/// <summary>
/// Represents the Inertia page object serialized to JSON for Inertia requests
/// or embedded in the Razor view for initial page loads.
/// </summary>
public sealed class InertiaPage
{
    /// <summary>The name of the JavaScript page component.</summary>
    public required string Component { get; init; }

    /// <summary>The page props (data) to pass to the component.</summary>
    public required IDictionary<string, object?> Props { get; init; }

    /// <summary>The current request URL.</summary>
    public required string Url { get; init; }

    /// <summary>The current asset version for cache busting.</summary>
    public required string Version { get; init; }

    /// <summary>When true, instructs the client to clear the history state.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ClearHistory { get; init; }

    /// <summary>When true, instructs the client to encrypt the history state.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool EncryptHistory { get; init; }

    /// <summary>When true, instructs the client to preserve the URL fragment.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool PreserveFragment { get; init; }

    /// <summary>Maps deferred prop groups to the list of prop names they contain.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, IReadOnlyList<string>>? DeferredProps { get; init; }

    /// <summary>Props that should be merged (shallow) with existing client-side data.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? MergeProps { get; init; }

    /// <summary>Props that should be deep-merged with existing client-side data.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? DeepMergeProps { get; init; }

    /// <summary>Props resolved only once per session.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, object?>? OnceProps { get; init; }

    /// <summary>Props controlling scroll position behavior.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, object?>? ScrollProps { get; init; }

    /// <summary>Names of props that are shared across all pages.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? SharedProps { get; init; }

    /// <summary>Flash data to include in the page response.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, object?>? Flash { get; init; }

    /// <summary>Default JSON serializer options with camelCase naming policy.</summary>
    internal static JsonSerializerOptions DefaultJsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new RuntimeTypeJsonConverter() },
    };

    /// <summary>Serializes this page to JSON.</summary>
    public string ToJson(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(this, options ?? DefaultJsonOptions);
    }
}

/// <summary>
/// A JSON converter that serializes <see cref="object"/> values using their runtime type
/// rather than the declared type, preventing anonymous types from being serialized as empty objects.
/// </summary>
internal sealed class RuntimeTypeJsonConverter : JsonConverter<object>
{
    /// <inheritdoc />
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize(ref reader, typeToConvert, options);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}
