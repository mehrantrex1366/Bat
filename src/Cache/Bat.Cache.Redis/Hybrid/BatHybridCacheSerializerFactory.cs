using System.Buffers;
using System.Text.Json;

namespace Bat.Cache.Hybrid;

/// <summary>
/// HybridCache serializer that writes the L2 (Redis) payload with Bat's JSON contract
/// (<see cref="SerializationExtension.DefaultOptions"/>: camelCase, public fields, relaxed escaping, Bat converters),
/// the same format <c>RedisCacheProvider</c> uses. Without it HybridCache uses the System.Text.Json defaults:
/// PascalCase names and public fields silently dropped.
/// </summary>
/// <remarks><see cref="string"/> and <see cref="byte"/>[] are left to HybridCache's built-in (raw) serializers.</remarks>
public sealed class BatHybridCacheSerializerFactory : IHybridCacheSerializerFactory
{
    public bool TryCreateSerializer<T>(out IHybridCacheSerializer<T> serializer)
    {
        if (typeof(T) == typeof(string) || typeof(T) == typeof(byte[]))
        {
            serializer = null;
            return false;
        }

        serializer = BatJsonSerializer<T>.Instance;
        return true;
    }

    private sealed class BatJsonSerializer<T> : IHybridCacheSerializer<T>
    {
        public static readonly BatJsonSerializer<T> Instance = new();

        // Utf8JsonWriter uses its own encoder/indentation settings, not the ones on JsonSerializerOptions:
        // copy them so Persian text is written exactly like SerializeToJsonUtf8Bytes does (relaxed escaping, not \uXXXX).
        private static readonly JsonWriterOptions _writerOptions = new()
        {
            Encoder = SerializationExtension.DefaultOptions.Encoder,
            Indented = SerializationExtension.DefaultOptions.WriteIndented,
            MaxDepth = SerializationExtension.DefaultOptions.MaxDepth,
            SkipValidation = true,
        };

        private static readonly JsonReaderOptions _readerOptions = new()
        {
            AllowTrailingCommas = SerializationExtension.DefaultOptions.AllowTrailingCommas,
            CommentHandling = SerializationExtension.DefaultOptions.ReadCommentHandling,
            MaxDepth = SerializationExtension.DefaultOptions.MaxDepth,
        };

        public T Deserialize(ReadOnlySequence<byte> source)
        {
            var reader = new Utf8JsonReader(source, _readerOptions);
            return JsonSerializer.Deserialize<T>(ref reader, SerializationExtension.DefaultOptions);
        }

        public void Serialize(T value, IBufferWriter<byte> target)
        {
            using var writer = new Utf8JsonWriter(target, _writerOptions);
            JsonSerializer.Serialize(writer, value, SerializationExtension.DefaultOptions);
        }
    }
}