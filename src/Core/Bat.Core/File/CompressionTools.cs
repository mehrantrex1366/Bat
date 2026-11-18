using System.Text;
using System.IO.Compression;

namespace Bat.Core;

/// <summary>
/// Zstandard (zstd) compression with the .NET 11 built-in <see cref="ZstandardStream"/> (no native package needed).
/// zstd compresses about as well as Brotli/gzip at a much higher speed; use it for large Redis payloads, queue messages,
/// files and HTTP bodies. The output is the standard zstd frame format, readable by any zstd implementation.
/// </summary>
public static class CompressionTools
{
    /// <summary>Compresses <paramref name="data"/> with zstd. <paramref name="quality"/> = <c>null</c> uses the zstd default (3); 1 (fastest) to 22 (smallest).</summary>
    public static byte[] CompressZstd(this byte[] data, int? quality = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var output = new MemoryStream();
        using (var zstd = CreateCompressor(output, quality, leaveOpen: true))
            zstd.Write(data);

        return output.ToArray();
    }

    /// <summary>Decompresses a zstd frame produced by <see cref="CompressZstd(byte[], int?)"/> or any zstd encoder.</summary>
    public static byte[] DecompressZstd(this byte[] compressed)
    {
        ArgumentNullException.ThrowIfNull(compressed);

        using var input = new MemoryStream(compressed, writable: false);
        using var zstd = new ZstandardStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zstd.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>UTF-8 encodes <paramref name="text"/>, compresses it with zstd and returns Base64 (safe for JSON, headers, DB text columns).</summary>
    public static string CompressZstdToBase64(this string text, int? quality = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(text).CompressZstd(quality));
    }

    /// <summary>Reverses <see cref="CompressZstdToBase64(string, int?)"/>.</summary>
    public static string DecompressZstdFromBase64(this string base64)
    {
        ArgumentNullException.ThrowIfNull(base64);
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64).DecompressZstd());
    }

    /// <summary>Compresses <paramref name="source"/> into <paramref name="destination"/> (both streams stay open).</summary>
    public static async Task CompressZstdAsync(this Stream source, Stream destination, int? quality = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        await using var zstd = CreateCompressor(destination, quality, leaveOpen: true);
        await source.CopyToAsync(zstd, cancellationToken);
    }

    /// <summary>Decompresses <paramref name="source"/> into <paramref name="destination"/> (both streams stay open).</summary>
    public static async Task DecompressZstdAsync(this Stream source, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        await using var zstd = new ZstandardStream(source, CompressionMode.Decompress, leaveOpen: true);
        await zstd.CopyToAsync(destination, cancellationToken);
    }

    private static ZstandardStream CreateCompressor(Stream output, int? quality, bool leaveOpen)
        => quality is null
            ? new ZstandardStream(output, CompressionLevel.Optimal, leaveOpen)
            : new ZstandardStream(output, new ZstandardCompressionOptions { Quality = quality.Value }, leaveOpen);
}