using Bogus;

namespace Bat.Test;

public static class TestExtensions
{
    public static T Randomizer<T>(this Faker faker)
    {
        if(typeof(T) == typeof(int))
            return (T)(object)faker.Random.Int(1, int.MaxValue);

        if(typeof(T) == typeof(long))
            return (T)(object)faker.Random.Long(1, long.MaxValue);

        if (typeof(T) == typeof(string))
            return (T)(object)faker.Random.String();

        if (typeof(T) == typeof(Guid))
            return (T)(object)Guid.NewGuid();

        if (typeof(T) == typeof(DateTime))
            return (T)(object)faker.Date.Past();

        return default;
    }

    public static string GenerateString(int length)
        => new Faker().Random.String(length);
}