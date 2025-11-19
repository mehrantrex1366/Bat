using Bogus;
using AutoBogus;

namespace Bat.Test;

public class BogusBuilder<T> where T : class
{
    private readonly AutoFaker<T> _faker;


    public BogusBuilder()
    {
     
        AutoFaker.Configure(x =>
        {
            x.WithTreeDepth(0);
            x.WithTreeDepth(1);
        });


        _faker = new AutoFaker<T>();
    }

    public BogusBuilder(AutoFaker<T> faker)
    {
        _faker = faker;
    }

    public BogusBuilder<T> WithNaturalInt()
    {
        _faker.RuleForType(typeof(int), f => f.Random.Int(1, int.MaxValue));
        _faker.RuleForType(typeof(int?), f => (int?)f.Random.Int(1, int.MaxValue));

        return this;

    }

    public BogusBuilder<T> WithNaturalLong()
    {
        _faker.RuleForType(typeof(long), f => f.Random.Long(1, long.MaxValue));

        return this;
    }

    public BogusBuilder<T> SetIntegersTo(int number)
    {
        _faker.RuleForType(typeof(int), f => number);

        return this;
    }

    public BogusBuilder<T> ShouldNotBe(Expression<Func<T, int>> property, int value, bool WithNaturalInt = true)
    {
        int excludedInt = Convert.ToInt32(value);
        _faker.RuleFor(property, f =>
        {
            int generatedNumber;

            do
            {
                generatedNumber = WithNaturalInt ? f.Random.Int(0, int.MaxValue) : f.Random.Int();
            } while (generatedNumber == excludedInt);

            return generatedNumber;
        });

        return this;
    }

    public BogusBuilder<T> ShouldNotBe<TEnum>(Expression<Func<T, TEnum>> property, TEnum value)
    {
        _faker.RuleFor(property, f =>
        {
            var allowedValues = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Where(v => !v.Equals(value))
                .ToArray();

            return f.PickRandom(allowedValues);
        });

        return this;
    }

    public BogusBuilder<T> ShouldNotBe(Expression<Func<T, int>> property, List<int> value, bool WithNaturalInt = true)
    {
        _faker.RuleFor(property, f =>
        {
            int generatedNumber;

            do
            {
                generatedNumber = WithNaturalInt ? f.Random.Int(0, int.MaxValue) : f.Random.Int();
            } while (value.Contains(generatedNumber) is false);

            return generatedNumber;
        });

        return this;
    }

    public BogusBuilder<T> SetBetween<TProperty>(Expression<Func<T, TProperty>> property, List<TProperty> values)
    {
        _faker.RuleFor(property, f => f.PickRandom(values));

        return this;
    }

    public BogusBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> property, Func<Faker, TProperty> valueFactory)
    {
        _faker.RuleFor(property, valueFactory);

        return this;
    }

    public BogusBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> property, TProperty value)
    {
        _faker.RuleFor(property, f => value);
        return this;
    }

    public BogusBuilder<T> SetNull<TProperty>(Expression<Func<T, TProperty>> property)
    where TProperty : class
    {
        _faker.RuleFor(property, f => null);
        return this;
    }

    public BogusBuilder<T> SetDefault<TProperty>(Expression<Func<T, TProperty>> property)
    where TProperty : class
    {
        _faker.RuleFor(property, f => default);
        return this;
    }

    public BogusBuilder<T> SetEmptyList<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> property)
    where TProperty : class
    {
        _faker.RuleFor(property, f => new List<TProperty>());
        return this;
    }

    public BogusBuilder<T> SetNull<TProperty>(Expression<Func<T, TProperty?>> property)
    where TProperty : struct
    {
        _faker.RuleFor(property, _ => (TProperty?)null);
        return this;
    }

    public BogusBuilder<T> SetList<TProperty>(Expression<Func<T, List<TProperty>>> property, int length)
        
    {
        var itemFaker = new Faker();
        List<TProperty> list = new () ;

        for (var i = 0; i < length; i++)
            list.Add(itemFaker.Randomizer<TProperty>());

        _faker.RuleFor(property, f => list);

        return this;
    }

    public BogusBuilder<T> SetString(Expression<Func<T, string>> property, int length)
    {

        _faker.RuleFor(property, f => f.Random.String(length));

        return this;

    }

    public BogusBuilder<T> SetString(Expression<Func<T, string>> property, int minLength, int maxLength)
    {
        _faker.RuleFor(property, f => f.Random.String(new Faker().Random.Int(minLength, maxLength)));

        return this;

    }

    public BogusBuilder<T> With<TProperty>(Expression<Func<T, TProperty>> property)
        where TProperty : class
    {
        var propBuilder = new BogusBuilder<TProperty>();
        var fakeProp = propBuilder.Generate();

        _faker.RuleFor(property, _ => fakeProp);

        return this;
    }

    public BogusBuilder<T> With<TProperty, TNestedProperty>(Expression<Func<T, TProperty>> property, Func<BogusBuilder<TProperty>, BogusBuilder<TProperty>> propertyBuilder,
                                Expression<Func<TProperty, TNestedProperty>> nestedProperty,
                                Func<BogusBuilder<TNestedProperty>, BogusBuilder<TNestedProperty>> nestedBuilder)
    where TProperty : class
    where TNestedProperty : class
    {
        var nestedValue = nestedBuilder(new BogusBuilder<TNestedProperty>()).Generate();

        var propertyValue = propertyBuilder(new BogusBuilder<TProperty>())
            .Set(nestedProperty, nestedValue)
            .Generate();

        _faker.RuleFor(property, _ => propertyValue);

        return this;
    }

    public BogusBuilder<T> With<TProperty>(Expression<Func<T, TProperty>> property, Func<BogusBuilder<TProperty>, BogusBuilder<TProperty>> subBuilder = null)
           where TProperty : class
    {
        var builder = new BogusBuilder<TProperty>();
        if (subBuilder != null)
            builder = subBuilder(builder);

        var subFakedObject = builder.Generate();
        _faker.RuleFor(property, _ => subFakedObject);

        return this;
    }

    public BogusBuilder<T> With<TLevel1, TLevel2, TLevel3>(Expression<Func<T, TLevel1>> level1Property, Func<BogusBuilder<TLevel1>, BogusBuilder<TLevel1>> level1Builder,
                                    Expression<Func<TLevel1, TLevel2>> level2Property, Func<BogusBuilder<TLevel2>, BogusBuilder<TLevel2>> level2Builder,
                                    Expression<Func<TLevel2, TLevel3>> level3Property, Func<BogusBuilder<TLevel3>, BogusBuilder<TLevel3>> level3Builder)
    where TLevel1 : class
    where TLevel2 : class
    where TLevel3 : class
    {
        var level3Value = level3Builder(new BogusBuilder<TLevel3>()).Generate();

        var level2Value = level2Builder(new BogusBuilder<TLevel2>())
            .Set(level3Property, level3Value)
            .Generate();

        var level1Value = level1Builder(new BogusBuilder<TLevel1>())
            .Set(level2Property, level2Value)
            .Generate();

        _faker.RuleFor(level1Property, _ => level1Value);

        return this;
    }

    public BogusBuilder<T> WithMany<TChild>(Expression<Func<T, IEnumerable<TChild>>> property,
        Func<BogusBuilder<TChild>, BogusBuilder<TChild>>? childBuilder = null, int count = 1)
        where TChild : class
    {
        var builder = new BogusBuilder<TChild>();

        if (childBuilder != null)
            builder = childBuilder(builder);

        var fakeChildren = builder.Generate(count).ToList();

        _faker.RuleFor(property, _ => fakeChildren);

        return this;
    }

    public T Generate() 
        => _faker.Generate();

    public List<T> Generate(int count) 
        => _faker.Generate(count);
}