using Bogus;
using AutoBogus;

namespace Bat.Test;

public class EfBogusBuilder<TAggregate, TKey>
    where TAggregate : class
{
    private readonly AutoFaker<TAggregate> _faker;

    public EfBogusBuilder()
    {
        AutoFaker.Configure(cfg => cfg.WithTreeDepth(1));
        _faker = new AutoFaker<TAggregate>();
    }

    /* =========================
       Identity Control
       ========================= */

    public EfBogusBuilder<TAggregate, TKey> WithId<TProperty>(
        Expression<Func<TAggregate, TProperty>> property,
        TProperty value)
    {
        _faker.RuleFor(property, _ => value);
        return this;
    }

    /* =========================
       Direct Property Setter
       ========================= */

    public EfBogusBuilder<TAggregate, TKey> Set<TProperty>(
        Expression<Func<TAggregate, TProperty>> property,
        TProperty value)
    {
        _faker.RuleFor(property, _ => value);
        return this;
    }

    public EfBogusBuilder<TAggregate, TKey> Set<TProperty>(
        Expression<Func<TAggregate, TProperty>> property,
        Func<Faker, TProperty> factory)
    {
        _faker.RuleFor(property, factory);
        return this;
    }

    /* =========================
       Owned / Value Objects
       ========================= */

    public EfBogusBuilder<TAggregate, TKey> WithOwned<TOwned>(
        Expression<Func<TAggregate, TOwned>> property,
        Func<GeneralBogusBuilder<TOwned>, GeneralBogusBuilder<TOwned>> builder = null)
        where TOwned : class
    {
        _faker.RuleFor(property, _ =>
        {
            var b = new GeneralBogusBuilder<TOwned>();
            if (builder != null)
                b = builder(b);

            return b.Generate();
        });

        return this;
    }

    /* =========================
       One-to-Many
       ========================= */

    public EfBogusBuilder<TAggregate, TKey> WithChildren<TChild>(
        Expression<Func<TAggregate, ICollection<TChild>>> collectionProperty,
        Expression<Func<TAggregate, TKey>> aggregateKey,
        Expression<Func<TChild, TKey>> childForeignKey,
        Expression<Func<TChild, TAggregate>> parentNavigation,
        int count = 1,
        Func<GeneralBogusBuilder<TChild>, GeneralBogusBuilder<TChild>> childBuilder = null)
        where TChild : class
    {
        _faker.RuleFor(
            collectionProperty,
            (f, agg) =>
            {
                var children = new List<TChild>();
                var parentId = aggregateKey.Compile()(agg);

                for (int i = 0; i < count; i++)
                {
                    var builder = new GeneralBogusBuilder<TChild>();
                    if (childBuilder != null)
                        builder = childBuilder(builder);

                    var child = builder
                        .Set(childForeignKey, parentId)
                        .Set(parentNavigation, agg)
                        .Generate();

                    children.Add(child);
                }

                return children;
            });

        return this;
    }

    /* =========================
       Many-to-Many
       ========================= */

    public EfBogusBuilder<TAggregate, TKey> WithManyToMany<TChild, TJoin>(
        Expression<Func<TAggregate, ICollection<TChild>>> collectionProperty,
        Func<GeneralBogusBuilder<TChild>, GeneralBogusBuilder<TChild>> childBuilder = null,
        int count = 1)
        where TChild : class
        where TJoin : class, new()
    {
        _faker.RuleFor(collectionProperty, _ =>
        {
            var children = new List<TChild>();
            for (int i = 0; i < count; i++)
            {
                var builder = new GeneralBogusBuilder<TChild>();
                if (childBuilder != null)
                    builder = childBuilder(builder);

                var child = builder.Generate();
                children.Add(child);
            }
            return children;
        });

        return this;
    }

    /* =========================
       Shadow Property Setter
       ========================= */

    public EfBogusBuilder<TAggregate, TKey> SetShadow<TProperty>(
        TAggregate entity,
        string propertyName,
        TProperty value,
        DbContext dbContext)
    {
        dbContext.Entry(entity).Property(propertyName).CurrentValue = value!;
        return this;
    }

    /* =========================
       DbContext Seeder
       ========================= */

    public EfBogusBuilder<TAggregate, TKey> Seed(DbContext context)
    {
        var entity = Generate();
        context.Add(entity);
        context.SaveChanges();
        return this;
    }

    public EfBogusBuilder<TAggregate, TKey> SeedMany(DbContext context, int count)
    {
        var entities = Generate(count);
        context.AddRange(entities);
        context.SaveChanges();
        return this;
    }

    /* =========================
       Generate
       ========================= */

    public TAggregate Generate() => _faker.Generate();

    public List<TAggregate> Generate(int count) => _faker.Generate(count);
}