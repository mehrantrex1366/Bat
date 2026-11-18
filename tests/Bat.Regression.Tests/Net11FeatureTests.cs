using System.Text;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Bat.AspNetCore;
using Bat.Cache;
using Bat.Cache.Hybrid;
using Bat.Cache.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Bat.Regression.Tests;

/// <summary>Shared L2 for "two pods" tests. Not MemoryDistributedCache: HybridCache ignores that one as an L2 on purpose.</summary>
public sealed class FakeDistributedCache : IDistributedCache
{
    public readonly ConcurrentDictionary<string, byte[]> Items = new();
    public byte[] Get(string key) => Items.TryGetValue(key, out var v) ? v : null;
    public Task<byte[]> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
    public void Remove(string key) => Items.TryRemove(key, out _);
    public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => Items[key] = value;
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) { Set(key, value, options); return Task.CompletedTask; }
}

public class CachedPerson
{
    public int Age;                       // public field: dropped by the System.Text.Json defaults
    public string FirstName { get; set; }
}

/// <summary>Collects bat.cache.requests measurements (thread-safe; other tests may record in parallel, so filter by provider).</summary>
public sealed class CacheMetricsCollector : IDisposable
{
    private readonly MeterListener _listener = new();
    public readonly ConcurrentQueue<(string Provider, string Result)> Requests = new();

    public CacheMetricsCollector()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == BatCacheMetrics.MeterName && instrument.Name == BatCacheMetrics.RequestsInstrumentName)
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            string provider = null, result = null;
            foreach (var tag in tags)
            {
                if (tag.Key == BatCacheMetrics.ProviderTagName) provider = tag.Value as string;
                if (tag.Key == BatCacheMetrics.ResultTagName) result = tag.Value as string;
            }
            Requests.Enqueue((provider, result));
        });
        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();
}

public class HybridCacheNet11Tests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static IHybridCacheProvider Create(FakeDistributedCache l2)
        => new ServiceCollection().AddSingleton<IDistributedCache>(l2).AddBatHybridCache()
            .BuildServiceProvider().GetRequiredService<IHybridCacheProvider>();

    [Fact]
    public async Task L2Payload_UsesBatJsonContract_AndIsSharedBetweenPods()
    {
        var l2 = new FakeDistributedCache();
        var pod1 = Create(l2);
        var pod2 = Create(l2);

        await pod1.GetOrSetAsync("person", _ => ValueTask.FromResult(new CachedPerson { Age = 30, FirstName = "علی" }), TimeSpan.FromMinutes(1), Ct);

        var payload = Encoding.UTF8.GetString(l2.Items["person"]);
        Assert.Contains("\"firstName\":\"علی\"", payload);   // camelCase + relaxed escaping (no \uXXXX)
        Assert.Contains("\"age\":30", payload);               // public field kept (IncludeFields)

        var calls = 0;
        var fromL2 = await pod2.GetOrSetAsync("person", _ => { calls++; return ValueTask.FromResult(new CachedPerson()); }, TimeSpan.FromMinutes(1), Ct);
        Assert.Equal(0, calls);
        Assert.Equal(30, fromL2.Age);
        Assert.Equal("علی", fromL2.FirstName);
    }

    [Fact]
    public async Task StringsAndBytes_KeepHybridCacheRawFormat()
    {
        var l2 = new FakeDistributedCache();
        var cache = Create(l2);
        await cache.GetOrSetAsync("s", _ => ValueTask.FromResult("plain"), TimeSpan.FromMinutes(1), Ct);
        Assert.EndsWith("plain", Encoding.UTF8.GetString(l2.Items["s"]));
        Assert.DoesNotContain("\"plain\"", Encoding.UTF8.GetString(l2.Items["s"]));
    }

    [Fact]
    public async Task OptionsOverload_CombinesFlagsAndTags()
    {
        var l2 = new FakeDistributedCache();
        var cache = Create(l2);
        var calls = 0;
        ValueTask<int> Factory(CancellationToken _) => ValueTask.FromResult(++calls);
        var options = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(1), Flags = HybridCacheEntryFlags.DisableDistributedCache };

        Assert.Equal(1, await cache.GetOrSetAsync("o", Factory, options, ["grp"], Ct));
        Assert.Equal(1, await cache.GetOrSetAsync("o", Factory, options, ["grp"], Ct));
        Assert.False(l2.Items.ContainsKey("o"));             // flag honoured

        await cache.DeleteByTagAsync("grp", Ct);              // tag honoured
        Assert.Equal(2, await cache.GetOrSetAsync("o", Factory, options, ["grp"], Ct));
    }

    [Fact]
    public async Task StateAndContextOverloads_AndSetAsync()
    {
        var cache = Create(new FakeDistributedCache());

        var value = await cache.GetOrSetWithStateAsync("st", (Name: "bat", Count: 2), static (s, _) => ValueTask.FromResult($"{s.Name}:{s.Count}"), ct: Ct);
        Assert.Equal("bat:2", value);

        HybridCacheEntryContext seen = null;
        var fromContext = await cache.GetOrSetWithContextAsync("ctx", (context, _) =>
        {
            seen = context;
            context.Expiration = TimeSpan.FromSeconds(10);   // e.g. shorter lifetime for "not found" results
            return ValueTask.FromResult(42);
        }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) }, ct: Ct);
        Assert.Equal(42, fromContext);
        Assert.NotNull(seen);

        await cache.SetAsync("set", 7, ct: Ct);
        Assert.Equal(7, await cache.GetOrSetAsync("set", _ => ValueTask.FromResult(-1), TimeSpan.FromMinutes(1), Ct));
    }

    [Fact]
    public async Task ExistingCallShapes_StillCompileAndWork()
    {
        // The call shapes of Bat 10.0.x must keep compiling against 11.0.0 (no new ambiguity from the added overloads).
        var cache = Create(new FakeDistributedCache());
        Func<CancellationToken, ValueTask<int>> factory = _ => ValueTask.FromResult(1);
        Assert.Equal(1, await cache.GetOrSetAsync("a", factory, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10), HybridCacheEntryFlags.None, Ct));
        Assert.Equal(1, await cache.GetOrSetAsync("b", factory, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10), new List<string> { "t" }, Ct));
        Assert.Equal(1, await cache.GetOrSetAsync("c", factory, TimeSpan.FromMinutes(1), flags: HybridCacheEntryFlags.None, ct: Ct));
        Assert.Equal(1, await cache.GetOrSetAsync("d", factory, TimeSpan.FromMinutes(1), tags: ["t"], ct: Ct));
        Assert.Equal(1, await cache.GetOrSetAsync("e", async ct => { await Task.Yield(); return 1; }, TimeSpan.FromMinutes(1), localExpiration: TimeSpan.FromSeconds(5), flags: HybridCacheEntryFlags.None, ct: Ct));
        await cache.DeleteAsync("a", Ct);
        await cache.DeleteAsync(["b", "c"], Ct);
    }

    [Fact]
    public async Task Metrics_RecordHitsAndMisses()
    {
        using var metrics = new CacheMetricsCollector();
        var cache = Create(new FakeDistributedCache());
        var key = Guid.NewGuid().ToString();

        await cache.GetOrSetAsync(key, _ => ValueTask.FromResult(1), TimeSpan.FromMinutes(1), Ct);
        await cache.GetOrSetAsync(key, _ => ValueTask.FromResult(1), TimeSpan.FromMinutes(1), Ct);

        var hybrid = metrics.Requests.Where(r => r.Provider == "hybrid").Select(r => r.Result).ToList();
        Assert.Contains("miss", hybrid);
        Assert.Contains("hit", hybrid);
    }

    [Fact]
    public void AddBatHybridCacheWithRedis_ReusesBatConnection_WithoutConnecting()
    {
        var services = new ServiceCollection().AddBatHybridCacheWithRedis("crm:hc:");
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        Assert.Equal("crm:hc:", options.InstanceName);
        Assert.NotNull(options.ConnectionMultiplexerFactory);
        Assert.IsAssignableFrom<RedisCache>(provider.GetRequiredService<IDistributedCache>());
        Assert.NotNull(provider.GetRequiredService<IHybridCacheProvider>());
    }

    [Fact]
    public void AddBatCacheStatistics_TurnsOnMemoryCacheStatistics()
    {
        using var provider = new ServiceCollection().AddBatCacheStatistics().BuildServiceProvider();
        Assert.True(provider.GetRequiredService<IOptions<MemoryCacheOptions>>().Value.TrackStatistics);
        Assert.NotNull(provider.GetRequiredService<IMemoryCache>().GetCurrentStatistics());
    }

    [Fact]
    public void MemoryCacheProvider_RecordsHitsAndMisses()
    {
        using var metrics = new CacheMetricsCollector();
        var cache = new MemoryCacheProvider(Guid.NewGuid().ToString());
        cache.Set("k", "v", DateTimeOffset.Now.AddMinutes(1));

        Assert.Equal("v", cache.Get("k"));
        Assert.Null(cache.Get("missing"));

        var memory = metrics.Requests.Where(r => r.Provider == "memory").Select(r => r.Result).ToList();
        Assert.Contains("hit", memory);
        Assert.Contains("miss", memory);
    }
}

public class CompressionAndJsonLinesTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Zstd_RoundTrips_AndWritesStandardFrames()
    {
        var text = string.Concat(Enumerable.Repeat("سلام Bat zstd 123 ", 500));
        var bytes = Encoding.UTF8.GetBytes(text);

        var compressed = bytes.CompressZstd();
        Assert.Equal(new byte[] { 0x28, 0xB5, 0x2F, 0xFD }, compressed[..4]);   // zstd frame magic number
        Assert.True(compressed.Length < bytes.Length / 10);
        Assert.Equal(bytes, compressed.DecompressZstd());
        Assert.Equal(bytes, bytes.CompressZstd(quality: 19).DecompressZstd());

        Assert.Equal(text, text.CompressZstdToBase64().DecompressZstdFromBase64());

        using var source = new MemoryStream(bytes);
        using var packed = new MemoryStream();
        await source.CompressZstdAsync(packed, cancellationToken: Ct);
        packed.Position = 0;
        using var unpacked = new MemoryStream();
        await packed.DecompressZstdAsync(unpacked, Ct);
        Assert.Equal(bytes, unpacked.ToArray());
    }

    [Fact]
    public async Task JsonLines_UseBatContract_AndRoundTrip()
    {
        var people = new[] { new CachedPerson { Age = 1, FirstName = "رضا" }, new CachedPerson { Age = 2, FirstName = "Sara" } };
        using var stream = new MemoryStream();
        await people.SerializeToJsonLinesAsync(stream, Ct);

        var lines = Encoding.UTF8.GetString(stream.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("\"firstName\":\"رضا\"", lines[0]);   // camelCase + relaxed escaping
        Assert.Contains("\"age\":1", lines[0]);                // public field (IncludeFields)

        stream.Position = 0;
        var back = new List<CachedPerson>();
        await foreach (var p in stream.DeSerializeJsonLinesAsync<CachedPerson>(Ct))
            back.Add(p);
        Assert.Equal([1, 2], back.Select(p => p.Age));
        Assert.Equal("رضا", back[0].FirstName);
    }
}

/// <summary>Async-only rule (e.g. a database lookup). The sync path accepts, so MVC's synchronous validation lets it through.</summary>
public sealed class NotTakenAttribute : AsyncValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext) => ValidationResult.Success;

    protected override async Task<ValidationResult> IsValidAsync(object value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return value as string == "taken" ? new ValidationResult($"{validationContext.MemberName} is already registered.") : ValidationResult.Success;
    }
}

public class SignUpModel
{
    [Required] public string Name { get; set; }
    [NotTaken] public string UserName { get; set; }
}

[ApiController, Route("api/signup")]
public class SignUpController : ControllerBase
{
    [HttpPost, BatAsyncValidation] public string Post(SignUpModel model) => "ok:" + model.UserName;
}

public class AsyncValidationTests(SwaggerServer server) : IClassFixture<SwaggerServer>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ValidateDataAnnotationsAsync_RunsSyncAndAsyncRules()
    {
        Assert.Empty(await new SignUpModel { Name = "a", UserName = "free" }.ValidateDataAnnotationsAsync(cancellationToken: Ct));

        var errors = await new SignUpModel { UserName = "taken" }.ValidateDataAnnotationsAsync(cancellationToken: Ct);
        Assert.Equal(2, errors.Count);
        Assert.Contains("UserName is already registered.", errors.JoinErrors());
        Assert.False(await new SignUpModel { Name = "a", UserName = "taken" }.IsValidDataAnnotationsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task BatAsyncValidationAttribute_Returns400ForAsyncRules()
    {
        using var client = new HttpClient();
        var ok = await client.PostAsync($"{server.Url}/api/signup", JsonContent("{\"name\":\"a\",\"userName\":\"free\"}"), Ct);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var taken = await client.PostAsync($"{server.Url}/api/signup", JsonContent("{\"name\":\"a\",\"userName\":\"taken\"}"), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, taken.StatusCode);
        Assert.Contains("UserName is already registered.", await taken.Content.ReadAsStringAsync(Ct));
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");
}

public class SettingsValidationTests
{
    private static readonly JwtSettings Valid = new()
    {
        SecretKey = "a-test-secret-key-that-is-long-enough-1234567890",
        Encryptionkey = "16-char-enc-key!",
        ExpirationMinutes = 30,
    };

    [Fact]
    public void JwtSettingsValidator_ReportsUnsafeSettings()
    {
        Assert.Empty(JwtSettingsValidator.GetErrors(Valid));
        Assert.Equal(2, JwtSettingsValidator.GetErrors(new JwtSettings()).Count);        // both fallback keys
        Assert.Equal(2, JwtSettingsValidator.GetErrors(new JwtSettings { SecretKey = "short", Encryptionkey = "17-chars-enc-key!" }).Count);
        Assert.Single(JwtSettingsValidator.GetErrors(new JwtSettings { SecretKey = Valid.SecretKey, Encryptionkey = Valid.Encryptionkey, ExpirationMinutes = -1 }));
    }

    [Fact]
    public void ValidateBatJwtSettings_FailsWhenOptionsAreRead()
    {
        var services = new ServiceCollection();
        services.AddOptions<JwtSettings>().Configure(o => o.SecretKey = "short").ValidateBatJwtSettings();
        using var provider = services.BuildServiceProvider();
        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<JwtSettings>>().Value);
        Assert.Contains(ex.Failures, f => f.Contains("SecretKey"));

        var ok = new ServiceCollection();
        ok.AddOptions<JwtSettings>().Configure(o => { o.SecretKey = Valid.SecretKey; o.Encryptionkey = Valid.Encryptionkey; }).ValidateBatJwtSettings();
        using var okProvider = ok.BuildServiceProvider();
        Assert.Equal(Valid.SecretKey, okProvider.GetRequiredService<IOptions<JwtSettings>>().Value.SecretKey);
    }

    [Fact]
    public void RedisSettingsValidator_ReportsMissingServerAndSsl()
    {
        Assert.Empty(RedisSettingsValidator.GetErrors(new RedisSettings { Server1 = "redis", Port1 = 6379 }));
        Assert.Contains(RedisSettingsValidator.GetErrors(new RedisSettings()), e => e.Contains("Server1"));
        Assert.Contains(RedisSettingsValidator.GetErrors(new RedisSettings { Server1 = "redis", SslSettings = new RedisSslSettings { UseSsl = true } }), e => e.Contains("SslHost"));

        var services = new ServiceCollection();
        services.AddOptions<RedisSettings>().ValidateBatRedisSettings();
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<RedisSettings>>().Value);
    }
}

public class ModernCryptoTests
{
    [Fact]
    public void AuthenticatedEncryption_RoundTrips_IsRandomized_AndDetectsTampering()
    {
        var key = AuthenticatedEncryption.GenerateKey();
        const string text = "کارت 6037-9911-1234-5678";

        var a = AuthenticatedEncryption.Encrypt(text, key, associatedData: "user:42");
        var b = AuthenticatedEncryption.Encrypt(text, key, associatedData: "user:42");
        Assert.NotEqual(a, b);
        Assert.Equal(text, AuthenticatedEncryption.Decrypt(a, key, "user:42"));

        Assert.False(AuthenticatedEncryption.TryDecrypt(a, key, out _, "user:43"));               // wrong associated data
        Assert.False(AuthenticatedEncryption.TryDecrypt(a, AuthenticatedEncryption.GenerateKey(), out _, "user:42")); // wrong key
        var tampered = Convert.FromBase64String(a);
        tampered[^1] ^= 1;
        Assert.False(AuthenticatedEncryption.TryDecrypt(Convert.ToBase64String(tampered), key, out _, "user:42"));
        Assert.False(AuthenticatedEncryption.TryDecrypt("not base64!", key, out _));

        Assert.Throws<ArgumentException>(() => AuthenticatedEncryption.Encrypt(text, Convert.ToBase64String(new byte[20])));
        Assert.Equal(1 + 40 + 12 + 16 + 5, AuthenticatedEncryption.Encrypt([1, 2, 3, 4, 5], RandomNumberGenerator.GetBytes(16)).Length);
    }

    [Fact]
    public void AuthenticatedEncryption_UsesStandardAesKeyWrap()
    {
        // RFC 3394 section 4.1 test vector: proves the wrapped-key part is interoperable AES Key Wrap.
        using var aes = Aes.Create();
        aes.Key = Convert.FromHexString("000102030405060708090A0B0C0D0E0F");
        var wrapped = aes.EncryptKeyWrap(Convert.FromHexString("00112233445566778899AABBCCDDEEFF"));
        Assert.Equal("1FA68B0A8112B447AEF34BD8FB5A7B829D3E862371D2CFE5", Convert.ToHexString(wrapped));
    }

    [Fact]
    public void X25519_BothSidesDeriveTheSameKey()
    {
        Assert.SkipUnless(X25519KeyAgreement.IsSupported, "X25519 is not supported on this OS.");

        var alice = X25519KeyAgreement.GenerateKeyPair();
        var bob = X25519KeyAgreement.GenerateKeyPair();

        var k1 = X25519KeyAgreement.DeriveSharedKeyBase64(alice.PrivateKey, bob.PublicKey, "payment-callback");
        var k2 = X25519KeyAgreement.DeriveSharedKeyBase64(bob.PrivateKey, alice.PublicKey, "payment-callback");
        Assert.Equal(k1, k2);
        Assert.NotEqual(k1, X25519KeyAgreement.DeriveSharedKeyBase64(alice.PrivateKey, bob.PublicKey, "other-purpose"));

        var secret = AuthenticatedEncryption.Encrypt("hello", k1);
        Assert.Equal("hello", AuthenticatedEncryption.Decrypt(secret, k2));
    }
}

public class ProcessToolsTests
{
    [Fact]
    public async Task RunAsync_CapturesOutputAndExitCode()
    {
        var result = await ProcessTools.RunAsync("dotnet", ["--version"], TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded, result.StandardError);
        Assert.StartsWith("11.", result.StandardOutput.Trim());
    }

    [Fact]
    public async Task RunAsync_KillsTheProcessOnTimeout()
    {
        var (file, args) = OperatingSystem.IsWindows()
            ? ("powershell", new[] { "-NoProfile", "-Command", "Start-Sleep -Seconds 30" })
            : ("sleep", new[] { "30" });

        var started = DateTime.UtcNow;
        var result = await ProcessTools.RunAsync(file, args, TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
        Assert.True(result.TimedOut);
        Assert.False(result.Succeeded);
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(20));
    }
}

/// <summary>Health probes + response compression on a local server.</summary>
public sealed class PlatformServer : IAsyncLifetime
{
    private WebApplication _app;
    public string Url { get; private set; }
    public static readonly string LargeJson = new { items = Enumerable.Range(0, 300).Select(i => new { id = i, name = "مورد " + i }) }.SerializeToJson();

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddBatResponseCompression();
        builder.Services.AddBatHealthChecks()
            .AddCheck("database", () => HealthCheckResult.Unhealthy("down"));
        _app = builder.Build();
        _app.Urls.Add("http://127.0.0.1:0");
        _app.UseBatResponseCompression();
        _app.UseRouting();
        // Would answer 418 for the probes if it ran: short-circuited endpoints execute right after routing, so it never does.
        _app.Use((HttpContext context, RequestDelegate next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/health"))
                return next(context);
            context.Response.StatusCode = 418;
            return Task.CompletedTask;
        });
        _app.MapBatHealthChecks(detailed: true);
        _app.MapGet("/large", () => Results.Text(LargeJson, "application/json"));
        await _app.StartAsync();
        Url = _app.Urls.First();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}

public class HealthAndCompressionTests(PlatformServer server) : IClassFixture<PlatformServer>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Probes_ShortCircuit_AndSeparateLivenessFromReadiness()
    {
        using var client = new HttpClient();

        var live = await client.GetAsync($"{server.Url}/health/live", Ct);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);                 // the 418 middleware was skipped
        var liveBody = (await live.Content.ReadAsStringAsync(Ct)).DeSerializeJson();
        Assert.Equal("Healthy", liveBody.GetProperty("status").GetString());
        Assert.Single(liveBody.GetProperty("checks").EnumerateArray());   // only "self"

        var ready = await client.GetAsync($"{server.Url}/health/ready", Ct);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Contains("database", await ready.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task ResponseCompression_PrefersZstd()
    {
        using var client = new HttpClient();   // no automatic decompression: we see the raw encoded body
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{server.Url}/large");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));

        var response = await client.SendAsync(request, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("zstd", Assert.Single(response.Content.Headers.ContentEncoding));
        var raw = await response.Content.ReadAsByteArrayAsync(Ct);
        Assert.True(raw.Length < Encoding.UTF8.GetByteCount(PlatformServer.LargeJson) / 3);
        Assert.Equal(PlatformServer.LargeJson, Encoding.UTF8.GetString(raw.DecompressZstd()));
    }
}
