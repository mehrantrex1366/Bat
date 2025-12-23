using Bogus;
using AutoBogus;

namespace Bat.Test;

public class GeneralBogusBuilder<T> where T : class
{
    private readonly AutoFaker<T> _faker;

    public GeneralBogusBuilder()
    {
        AutoFaker.Configure(x => x.WithTreeDepth(1));
        _faker = new AutoFaker<T>();
    }

    public GeneralBogusBuilder(AutoFaker<T> faker)
    {
        _faker = faker;
    }

    /* =========================
       Global Rules
       ========================= */

    public GeneralBogusBuilder<T> WithNaturalInt()
    {
        _faker.RuleForType(typeof(int), f => f.Random.Int(1, int.MaxValue - 1));
        _faker.RuleForType(typeof(int?), f => f.Random.Int(1, int.MaxValue - 1));
        return this;
    }

    public GeneralBogusBuilder<T> WithNaturalLong()
    {
        _faker.RuleForType(typeof(long), f => f.Random.Long(1, long.MaxValue - 1));
        return this;
    }

    public GeneralBogusBuilder<T> SetIntegersTo(int number)
    {
        _faker.RuleForType(typeof(int), _ => number);
        _faker.RuleForType(typeof(int?), _ => number);
        return this;
    }

    /* =========================
       Negative Constraints
       ========================= */

    public GeneralBogusBuilder<T> ShouldNotBe(
        Expression<Func<T, int>> property,
        int value,
        bool natural = true)
    {
        _faker.RuleFor(property, f =>
        {
            int result;
            do
            {
                result = natural
                    ? f.Random.Int(0, int.MaxValue - 1)
                    : f.Random.Int();
            } while (result == value);

            return result;
        });

        return this;
    }

    public GeneralBogusBuilder<T> ShouldNotBe(
        Expression<Func<T, int>> property,
        List<int> excludedValues,
        bool natural = true)
    {
        _faker.RuleFor(property, f =>
        {
            int result;
            do
            {
                result = natural
                    ? f.Random.Int(0, int.MaxValue - 1)
                    : f.Random.Int();
            } while (excludedValues.Contains(result));

            return result;
        });

        return this;
    }

    public GeneralBogusBuilder<T> ShouldNotBe<TEnum>(
        Expression<Func<T, TEnum>> property,
        TEnum value)
        where TEnum : struct, Enum
    {
        _faker.RuleFor(property, f =>
        {
            var allowed = Enum.GetValues<TEnum>()
                .Where(x => !x.Equals(value))
                .ToArray();

            if (allowed.Length == 0)
                throw new InvalidOperationException(
                    $"Enum {typeof(TEnum).Name} has no alternative values.");

            return f.PickRandom(allowed);
        });

        return this;
    }

    /* =========================
       Direct Setters
       ========================= */

    public GeneralBogusBuilder<T> Set<TProperty>(
        Expression<Func<T, TProperty>> property,
        TProperty value)
    {
        _faker.RuleFor(property, _ => value);
        return this;
    }

    public GeneralBogusBuilder<T> Set<TProperty>(
        Expression<Func<T, TProperty>> property,
        Func<Faker, TProperty> factory)
    {
        _faker.RuleFor(property, factory);
        return this;
    }

    public GeneralBogusBuilder<T> SetFrom<TProperty>(
        Expression<Func<T, TProperty>> property,
        IEnumerable<TProperty> values)
    {
        var list = values.ToList();
        _faker.RuleFor(property, f => f.PickRandom(list));
        return this;
    }

    public GeneralBogusBuilder<T> SetDefault<TProperty>(
        Expression<Func<T, TProperty>> property)
    {
        _faker.RuleFor(property, _ => default!);
        return this;
    }

    public GeneralBogusBuilder<T> SetNull<TProperty>(
        Expression<Func<T, TProperty>> property)
        where TProperty : class
    {
        _faker.RuleFor(property, _ => null);
        return this;
    }

    public GeneralBogusBuilder<T> SetNull<TProperty>(
        Expression<Func<T, TProperty?>> property)
        where TProperty : struct
    {
        _faker.RuleFor(property, _ => null);
        return this;
    }

    public GeneralBogusBuilder<T> SetEmptyList<TItem>(
        Expression<Func<T, IEnumerable<TItem>>> property)
    {
        _faker.RuleFor(property, _ => Enumerable.Empty<TItem>());
        return this;
    }

    public GeneralBogusBuilder<T> SetList<TItem>(
        Expression<Func<T, List<TItem>>> property,
        int length)
    {
        _faker.RuleFor(property, _ =>
        {
            var faker = new Faker();
            return Enumerable.Range(0, length)
                .Select(_ => faker.Randomizer<TItem>())
                .ToList();
        });

        return this;
    }

    public GeneralBogusBuilder<T> SetString(
        Expression<Func<T, string>> property,
        int length)
    {
        _faker.RuleFor(property, f => f.Random.String(length));
        return this;
    }

    public GeneralBogusBuilder<T> SetString(
        Expression<Func<T, string>> property,
        int minLength,
        int maxLength)
    {
        _faker.RuleFor(property,
            f => f.Random.String(f.Random.Int(minLength, maxLength)));
        return this;
    }

    /* =========================
       Object Graph Builders
       ========================= */

    public GeneralBogusBuilder<T> With<TProperty>(
        Expression<Func<T, TProperty>> property,
        Func<GeneralBogusBuilder<TProperty>, GeneralBogusBuilder<TProperty>> builder = null)
        where TProperty : class
    {
        _faker.RuleFor(property, _ =>
        {
            var b = new GeneralBogusBuilder<TProperty>();
            if (builder != null)
                b = builder(b);

            return b.Generate();
        });

        return this;
    }

    public GeneralBogusBuilder<T> WithMany<TChild>(
        Expression<Func<T, IEnumerable<TChild>>> property,
        Func<GeneralBogusBuilder<TChild>, GeneralBogusBuilder<TChild>> childBuilder = null,
        int count = 1)
        where TChild : class
    {
        _faker.RuleFor(property, _ =>
        {
            var builder = new GeneralBogusBuilder<TChild>();
            if (childBuilder != null)
                builder = childBuilder(builder);

            return builder.Generate(count);
        });

        return this;
    }

    /* =========================
       Generate
       ========================= */

    public T Generate() => _faker.Generate();

    public List<T> Generate(int count) => _faker.Generate(count);
}