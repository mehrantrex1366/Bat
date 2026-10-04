using System.Security.Cryptography;

namespace Bat.Core;

public static class Randomizer
{
    // Uses the shared thread-safe Random instead of allocating a new Random per digit.
    public static int GetRandomInteger(int length)
        => int.Parse(Random.Shared.GetString("123456789", length));

    // Fixed: the index was random.Next(length) instead of random.Next(chars.Length), so only the first
    // `length` characters were ever used (and length > 36 threw IndexOutOfRangeException).
    // Uses a cryptographically secure generator because this is used for refresh tokens.
    public static string GetRandomString(int length)
        => RandomNumberGenerator.GetString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length);

    // Fixed: same index bug as above (random.Next(length) instead of pattern.Length).
    public static string GetRandomString(int length, string pattern)
        => RandomNumberGenerator.GetString(pattern, length);

    public static string GetUniqueKey(int length = 5)
    {
        string guidResult = string.Empty;
        while (guidResult.Length < length)
            guidResult += Guid.NewGuid().ToString().GetHashCode().ToString("x");

        return guidResult.Substring(0, length);
    }

    public static string GetUniqueKey2(int length = 5)
    {
        var randomNumber = new byte[length];
        using var rnd = RandomNumberGenerator.Create();
        rnd.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}