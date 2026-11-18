namespace Bat.Core;

/// <summary>
/// JSON Lines (one JSON value per line, <c>application/jsonl</c>) with Bat's JSON contract (<see cref="SerializationExtension.DefaultOptions"/>).
/// Streams large result sets (exports, reports, data sync between services) without building one big JSON array in memory.
/// </summary>
public static class JsonLinesExtensions
{
    /// <summary>Writes every item of <paramref name="items"/> as one JSON line to <paramref name="stream"/> (.NET 11 <c>SerializeAsyncEnumerable</c>).</summary>
    public static Task SerializeToJsonLinesAsync<T>(this IAsyncEnumerable<T> items, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(stream);
        return JsonSerializer.SerializeAsyncEnumerable(stream, items, topLevelValues: true, SerializationExtension.DefaultOptions, cancellationToken);
    }

    /// <summary>Writes every item of <paramref name="items"/> as one JSON line to <paramref name="stream"/>.</summary>
    public static Task SerializeToJsonLinesAsync<T>(this IEnumerable<T> items, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return ToAsync(items).SerializeToJsonLinesAsync(stream, cancellationToken);
    }

    /// <summary>Reads a JSON Lines stream item by item.</summary>
    public static IAsyncEnumerable<T> DeSerializeJsonLinesAsync<T>(this Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return JsonSerializer.DeserializeAsyncEnumerable<T>(stream, topLevelValues: true, SerializationExtension.DefaultOptions, cancellationToken);
    }

#pragma warning disable CS1998 // async iterator without await: adapts a synchronous sequence.
    private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
            yield return item;
    }
#pragma warning restore CS1998
}