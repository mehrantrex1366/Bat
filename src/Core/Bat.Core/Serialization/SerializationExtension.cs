using System.Text.Encodings.Web;
using System.Collections.Concurrent;
using System.Text.Json.Serialization.Metadata;

namespace Bat.Core;

public static class SerializationExtension
{
    // JsonSerializerOptions instances own the (expensive) per-type metadata cache.
    // Building a new instance per call forces System.Text.Json to rebuild all metadata
    // (and emit new IL accessors) on every call, so the defaults are created once and shared.
    private static readonly JsonSerializerOptions _defaultSerializerOptions = CreateReadOnlyOptions(null);
    private static readonly ConcurrentDictionary<int, JsonSerializerOptions> _defaultDepth = new();

    private static JsonSerializerOptions CreateOptions(int? depth)
    {
        var options = new JsonSerializerOptions()
        {
            IncludeFields = true,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        options.Converters.Add(new BatStringToBoolConverter());
        options.Converters.Add(new BatNumberToStringConverter());

        if (depth is not null) options.MaxDepth = (int)depth;

        return options;
    }

    private static JsonSerializerOptions CreateReadOnlyOptions(int? depth)
    {
        var options = CreateOptions(depth);
        options.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
        options.MakeReadOnly();
        return options;
    }

    private static JsonSerializerOptions Cached(int? depth)
        => depth is null
            ? _defaultSerializerOptions
            : _defaultDepth.GetOrAdd(depth.Value, static d => CreateReadOnlyOptions(d));

    /// <summary>
    /// Returns a NEW, mutable instance of the Bat default options (same behavior as before).
    /// Prefer <see cref="DefaultOptions"/> when you do not need to modify the options:
    /// creating options per call throws away System.Text.Json's metadata cache.
    /// </summary>
    public static JsonSerializerOptions GetDefaultOption(int? depth = null) => CreateOptions(depth);

    /// <summary>
    /// Shared, read-only Bat default options. Safe to use from any thread; cannot be modified.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => _defaultSerializerOptions;

    /// <summary>
    /// Shared, read-only Bat default options with the given MaxDepth.
    /// </summary>
    public static JsonSerializerOptions GetSharedOption(int? depth = null) => Cached(depth);


    public static string AddJsonArrayRoot(string JsonString)
        => "[" + JsonString + "]";

    public static string SerializeToJson(this object jsonObject)
    {
        if (jsonObject is null) return string.Empty;

        return JsonSerializer.Serialize(jsonObject, _defaultSerializerOptions);
    }

    public static string SerializeToJson<T>(this T jsonObject)
    {
        if (jsonObject is null) return string.Empty;

        return JsonSerializer.Serialize(jsonObject, _defaultSerializerOptions);
    }

    public static string SerializeToJson(this object jsonObject, int depth)
    {
        if (jsonObject is null) return string.Empty;

        return JsonSerializer.Serialize(jsonObject, Cached(depth));
    }

    public static string SerializeToJson<T>(this T jsonObject, int depth)
    {
        if (jsonObject is null) return string.Empty;

        return JsonSerializer.Serialize(jsonObject, Cached(depth));
    }

    public static string SerializeToJson(this object jsonObject, JsonSerializerOptions options)
    {
        if (jsonObject is null) return string.Empty;

        return JsonSerializer.Serialize(jsonObject, options);
    }

    public static string SerializeToJson<T>(this T jsonObject, JsonSerializerOptions options)
    {
        if (jsonObject is null) return string.Empty;

        return JsonSerializer.Serialize(jsonObject, options);
    }

    /// <summary>
    /// Serializes directly to UTF-8 bytes (no intermediate UTF-16 string). Returns an empty array for null.
    /// Use this when the result is written to a stream, Redis, a message broker, etc.
    /// </summary>
    public static byte[] SerializeToJsonUtf8Bytes<T>(this T jsonObject)
    {
        if (jsonObject is null) return [];

        return JsonSerializer.SerializeToUtf8Bytes(jsonObject, _defaultSerializerOptions);
    }

    public static T DeSerializeJson<T>(this string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        return JsonSerializer.Deserialize<T>(json, _defaultSerializerOptions);
    }

    public static T DeSerializeJson<T>(this string json, int depth)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        return JsonSerializer.Deserialize<T>(json, Cached(depth));
    }

    public static T DeSerializeJson<T>(this string json, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// Deserializes UTF-8 JSON bytes (no intermediate UTF-16 string). Returns default for null/empty/whitespace input.
    /// </summary>
    public static T DeSerializeJson<T>(this byte[] utf8Json)
    {
        if (utf8Json is null || IsNullOrWhiteSpace(utf8Json)) return default;

        return JsonSerializer.Deserialize<T>(utf8Json, _defaultSerializerOptions);
    }

    public static dynamic DeSerializeJsonToDynamic(this string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        return JsonSerializer.Deserialize<dynamic>(json, _defaultSerializerOptions);
    }

    public static dynamic DeSerializeJsonToDynamic(this string json, int depth)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        return JsonSerializer.Deserialize<dynamic>(json, Cached(depth));
    }

    public static dynamic DeSerializeJsonToDynamic(this string json, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;

        return JsonSerializer.Deserialize<dynamic>(json, options);
    }

    public static JsonElement DeSerializeJson(this string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        return JsonSerializer.Deserialize<JsonElement>(json, _defaultSerializerOptions);
    }

    public static JsonElement DeSerializeJsonToJsonElement(this string json, int depth)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        return JsonSerializer.Deserialize<JsonElement>(json, Cached(depth));
    }

    public static JsonElement DeSerializeJsonToJsonElement(this string json, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        return JsonSerializer.Deserialize<JsonElement>(json, options);
    }

    private static bool IsNullOrWhiteSpace(ReadOnlySpan<byte> utf8)
    {
        foreach (var b in utf8)
            if (b is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')) return false;

        return true;
    }
}