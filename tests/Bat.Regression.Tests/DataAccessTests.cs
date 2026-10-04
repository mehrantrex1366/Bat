using Bat.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bat.Regression.Tests;

public class Item : IBaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class TestDb(DbContextOptions<TestDb> options) : BatDbContext(options)
{
    public DbSet<Item> Items => Set<Item>();
}

public class TvpRow
{
    public int A { get; set; }
    public int? B { get; set; }
    public DateTime? C { get; set; }
}

public class DataAccessTests
{
    private static TestDb CreateDb() => new(new DbContextOptionsBuilder<TestDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void SaveChanges_NormalizesPersianCharactersAndDigits()
    {
        using var db = CreateDb();
        db.Items.Add(new Item { Name = "كيك ۱۲" });
        db.SaveChanges();
        Assert.Equal("کیک 12", db.Items.Single().Name);
    }

    [Fact]
    public void RedisSslSettings_OldHostName_StillBinds()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["SslSettings:UseSsl"] = "true", ["SslSettings:Host"] = "old.example" })
            .Build();
        var settings = new Bat.Cache.Redis.RedisSettings();
        Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(config, settings);
        Assert.Equal("old.example", settings.SslSettings.SslHost);

        var config2 = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["SslSettings:SslHost"] = "new.example" })
            .Build();
        var settings2 = new Bat.Cache.Redis.RedisSettings();
        Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(config2, settings2);
        Assert.Equal("new.example", settings2.SslSettings.SslHost);
    }

    [Fact]
    public void ChangedEntityHelpers_ReturnTheRightStates()
    {
        using var db = CreateDb();
        db.Items.Add(new Item { Name = "n" });
        Assert.Single(db.GetAddedEntity());
        Assert.Empty(db.GetUpdatedEntity());
        Assert.Empty(db.GetDeletedEntity());
    }

    [Fact]
    public void OrderBy_String_SupportsDirectionAndNestedPaths()
    {
        var data = new[] { new Sample { Id = 2, Name = "b", Child = new() { Id = 9 } }, new Sample { Id = 1, Name = "a", Child = new() { Id = 1 } } }.AsQueryable();
        Assert.Equal([2, 1], data.OrderBy("name desc").Select(x => x.Id));
        Assert.Equal([1, 2], data.OrderBy("Child.Id").Select(x => x.Id));
        Assert.Equal([1, 2], data.OrderBy("Id").Select(x => x.Id));
    }

    [Fact]
    public async Task RepositoryFactories_DisposeWithoutRecursion()
    {
        new Bat.EntityFrameworkCore.Tools.RepositoryFactory(null).Dispose();
        await new Bat.EntityFrameworkCore.Tools.RepositoryFactory(null).DisposeAsync();
        new Bat.EntityFrameworkCore.Tools.BulkRepositoryFactory(null).Dispose();
        await new Bat.EntityFrameworkCore.Tools.BulkRepositoryFactory(null).DisposeAsync();
    }

    [Fact]
    public void TableValuedParameter_SupportsNullableProperties()
    {
        var tvp = Bat.Dapper.ParameterExtension.ToTableValuedParameter(new List<TvpRow> { new() { A = 1 } }, "dbo.T");
        Assert.NotNull(tvp);
    }

    [Fact]
    public void MemoryCacheProvider_Set_Overwrites()
    {
        var cache = new Bat.Cache.MemoryCacheProvider("tests-" + Guid.NewGuid());
        cache.Set("k", 1, DateTimeOffset.Now.AddMinutes(1));
        cache.Set("k", 2, DateTimeOffset.Now.AddMinutes(1));
        Assert.Equal(2, (int)cache.Get("k"));
    }
}
