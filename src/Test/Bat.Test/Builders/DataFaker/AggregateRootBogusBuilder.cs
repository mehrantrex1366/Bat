using AutoBogus;

namespace Bat.Test;

public class AggregateRootBogusBuilder<TAggregate, TKey>
    where TAggregate : class
{
    private readonly AutoFaker<TAggregate> _faker;

    public AggregateRootBogusBuilder()
    {
        AutoFaker.Configure(cfg => cfg.WithTreeDepth(1));
        _faker = new AutoFaker<TAggregate>();
    }

    /* =========================
       Identity Control
       ========================= */

    public AggregateRootBogusBuilder<TAggregate, TKey> WithId(
        Expression<Func<TAggregate, TKey>> idProperty,
        TKey id)
    {
        _faker.RuleFor(idProperty, _ => id);
        return this;
    }

    public AggregateRootBogusBuilder<TAggregate, TKey> WithGeneratedIntId(
        Expression<Func<TAggregate, int>> idProperty,
        int startFrom = 1)
    {
        int current = startFrom;

        _faker.RuleFor(idProperty, _ => current++);
        return this;
    }

    /* =========================
       Aggregate Fields
       ========================= */

    public AggregateRootBogusBuilder<TAggregate, TKey> Set<TProperty>(
        Expression<Func<TAggregate, TProperty>> property,
        TProperty value)
    {
        _faker.RuleFor(property, _ => value);
        return this;
    }

    /* =========================
       Owned / Value Objects
       ========================= */

    public AggregateRootBogusBuilder<TAggregate, TKey> WithOwned<TOwned>(
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
       Child Entities (1 → Many)
       ========================= */

    public AggregateRootBogusBuilder<TAggregate, TKey> WithChildren<TChild>(
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
       Generate
       ========================= */

    public TAggregate Generate() => _faker.Generate();

    public List<TAggregate> Generate(int count) => _faker.Generate(count);
}