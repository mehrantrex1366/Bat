using System.Security.Claims;
using Bat.AspNetCore;
using Bat.Cache.Hybrid;
using Bat.Di;
using Bat.EntityFrameworkCore.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Bat.Regression.Tests;

public interface ISampleScopedService { Guid Id { get; } }
public class SampleScopedService : ISampleScopedService, IScopedInjection { public Guid Id { get; } = Guid.NewGuid(); }

public interface ISampleSingletonService { Guid Id { get; } }
public class SampleSingletonService : ISampleSingletonService, ISingletonInjection { public Guid Id { get; } = Guid.NewGuid(); }

public interface ISampleGenericRepo<T> { Type Item { get; } }
public class SampleGenericRepo<T> : ISampleGenericRepo<T>, ITransientInjection { public Type Item => typeof(T); }

public class DiTests
{
    private static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddBatDynamicScoped(typeof(DiTests).Assembly);
        services.AddBatDynamicSingleton(typeof(DiTests).Assembly);
        services.AddBatDynamicTransient(typeof(DiTests).Assembly);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void MarkerInterfaces_AreRegisteredWithTheirLifetime()
    {
        using var provider = Build();

        using (var scope = provider.CreateScope())
        {
            var a = scope.ServiceProvider.GetRequiredService<ISampleScopedService>();
            Assert.Same(a, scope.ServiceProvider.GetRequiredService<ISampleScopedService>());
            using var other = provider.CreateScope();
            Assert.NotSame(a, other.ServiceProvider.GetRequiredService<ISampleScopedService>());
        }

        Assert.Same(provider.GetRequiredService<ISampleSingletonService>(), provider.GetRequiredService<ISampleSingletonService>());
    }

    [Fact]
    public void GenericClasses_AreRegisteredAsOpenGenerics()
    {
        using var provider = Build();
        Assert.Equal(typeof(int), provider.GetRequiredService<ISampleGenericRepo<int>>().Item);
        Assert.Equal(typeof(string), provider.GetRequiredService<ISampleGenericRepo<string>>().Item);
    }

    [Fact]
    public void InternalHybridCacheProvider_IsNotAutoRegistered()
    {
        using var provider = Build();
        Assert.Null(provider.GetService<IHybridCacheProvider>());
    }
}

public class JwtTests
{
    private static readonly JwtSettings Settings = new()
    {
        SecretKey = "a-test-secret-key-that-is-long-enough-1234567890",
        Encryptionkey = "16-char-enc-key!",
        Issuer = "bat-tests",
        Audience = "bat-clients",
        ExpirationMinutes = 5,
    };

    private readonly JwtService _jwt = new();

    [Fact]
    public void CreateToken_ThenGetClaimsPrincipal_RoundTrips()
    {
        var token = _jwt.CreateToken([new Claim(ClaimTypes.NameIdentifier, "42"), new Claim("tenant", "t-1")], Settings);

        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal(32, token.RefreshToken.Length);
        Assert.Equal(5, token.Token.Split('.').Length); // JWE compact serialization (signed, then encrypted)

        var principal = _jwt.GetClaimsPrincipal(token.Token, Settings);
        Assert.NotNull(principal);
        Assert.Equal("42", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("t-1", principal.FindFirst("tenant")?.Value);
    }

    [Fact]
    public void GetClaimsPrincipal_RejectsWrongKeyAndWrongAudience()
    {
        var token = _jwt.CreateToken("user-data", Settings).Token;

        Assert.ThrowsAny<SecurityTokenException>(() => _jwt.GetClaimsPrincipal(token, new JwtSettings
        {
            SecretKey = Settings.SecretKey, Encryptionkey = "another-16-chars", Issuer = Settings.Issuer, Audience = Settings.Audience,
        }));
        Assert.ThrowsAny<SecurityTokenException>(() => _jwt.GetClaimsPrincipal(token, new JwtSettings
        {
            SecretKey = Settings.SecretKey, Encryptionkey = Settings.Encryptionkey, Issuer = Settings.Issuer, Audience = "someone-else",
        }));
    }
}

public class HybridCacheTests
{
    private static ServiceProvider Build()
        => new ServiceCollection().AddBatHybridCache().BuildServiceProvider();

    [Fact]
    public void AddBatHybridCache_RegistersOneSingletonProvider()
    {
        var services = new ServiceCollection().AddBatHybridCache().AddBatHybridCache();
        Assert.Single(services, d => d.ServiceType == typeof(IHybridCacheProvider));

        using var provider = services.BuildServiceProvider();
        Assert.Same(provider.GetRequiredService<IHybridCacheProvider>(), provider.GetRequiredService<IHybridCacheProvider>());
    }

    [Fact]
    public async Task GetOrSet_CachesValue_AndDeleteRemovesIt()
    {
        using var provider = Build();
        var cache = provider.GetRequiredService<IHybridCacheProvider>();
        var ct = TestContext.Current.CancellationToken;
        var calls = 0;
        ValueTask<int> Factory(CancellationToken _) => ValueTask.FromResult(++calls);

        // Three-argument call: was a CS0121 ambiguity before 11.0.0.
        Assert.Equal(1, await cache.GetOrSetAsync("k", Factory, TimeSpan.FromMinutes(1)));
        Assert.Equal(1, await cache.GetOrSetAsync("k", Factory, TimeSpan.FromMinutes(1), ct));

        await cache.DeleteAsync("k", ct);
        Assert.Equal(2, await cache.GetOrSetAsync("k", Factory, TimeSpan.FromMinutes(1), ct));
    }

    [Fact]
    public async Task DeleteByTag_RemovesTaggedEntries()
    {
        using var provider = Build();
        var cache = provider.GetRequiredService<IHybridCacheProvider>();
        var ct = TestContext.Current.CancellationToken;
        var calls = 0;
        ValueTask<int> Factory(CancellationToken _) => ValueTask.FromResult(++calls);

        Assert.Equal(1, await cache.GetOrSetAsync("t", Factory, TimeSpan.FromMinutes(1), tags: ["users"], ct: ct));
        Assert.Equal(1, await cache.GetOrSetAsync("t", Factory, TimeSpan.FromMinutes(1), tags: ["users"], ct: ct));

        await cache.DeleteByTagAsync("users", ct);
        Assert.Equal(2, await cache.GetOrSetAsync("t", Factory, TimeSpan.FromMinutes(1), tags: ["users"], ct: ct));
    }
}

public class EncryptorConverterTests
{
    [Fact]
    public void Converters_StoreTheSameCiphertextAsAesEncryption_AndRoundTrip()
    {
        // Stored data depends on these values: the converters must keep producing AesEncryption output.
        var s = new StringEncryptorConverter();
        Assert.Equal("wNrN1Lyu+nG/jpihCHhmBg==", s.ConvertToProvider("hello"));
        Assert.Equal("hello", s.ConvertFromProvider("wNrN1Lyu+nG/jpihCHhmBg=="));

        var i = new IntEncryptorConverter();
        Assert.Equal(AesEncryption.Encrypt("12345"), i.ConvertToProvider(12345));
        Assert.Equal(12345, i.ConvertFromProvider(i.ConvertToProvider(12345)));

        var guid = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");
        var g = new GuidEncryptorConverter();
        Assert.Equal(AesEncryption.Encrypt(guid.ToString()), g.ConvertToProvider(guid));
        Assert.Equal(guid, g.ConvertFromProvider(g.ConvertToProvider(guid)));
    }
}
