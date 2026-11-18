using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace Bat.AspNetCore;

public static class ResponseCompressionExtensions
{
    /// <summary>MIME types Bat compresses in addition to <see cref="ResponseCompressionDefaults.MimeTypes"/>.</summary>
    public static readonly string[] AdditionalMimeTypes = ["application/jsonl", "application/x-ndjson", "application/problem+json", "image/svg+xml"];

    /// <summary>
    /// Response compression with the .NET 11 providers: <b>Zstandard</b> (preferred: fastest), Brotli and gzip, all at
    /// <see cref="CompressionLevel.Fastest"/> (best trade-off for API responses), plus JSON Lines / problem+json MIME types.
    /// Pair with <see cref="UseBatResponseCompression"/>.
    /// </summary>
    /// <param name="enableForHttps">
    /// Off by default like ASP.NET Core: compressing HTTPS responses that mix secrets and attacker-controlled input can enable
    /// BREACH/CRIME attacks. Enable it for APIs whose responses don't reflect user input next to secrets.
    /// When the service runs behind a proxy/ingress that already compresses, don't use this at all.
    /// </param>
    public static IServiceCollection AddBatResponseCompression(this IServiceCollection services, bool enableForHttps = false)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = enableForHttps;
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(AdditionalMimeTypes);
            options.Providers.Add<ZstandardCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

        return services;
    }

    /// <summary>Adds the response compression middleware. Call it early, before the middlewares that write responses.</summary>
    public static IApplicationBuilder UseBatResponseCompression(this IApplicationBuilder app)
        => app.UseResponseCompression();
}