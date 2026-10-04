namespace Bat.Core;

public static class LogicExtensions
{
    public static bool IsNull(this string str) => string.IsNullOrWhiteSpace(str);

    public static bool IsNotNull(this string str) => !string.IsNullOrWhiteSpace(str);

    public static bool IsNull(this object obj) => obj == null;

    public static bool IsNotNull(this object obj) => obj != null;

    public static bool IsNull(this Guid? token) => token == null || token == Guid.Empty;

    // Fixed: this used to call IsNull(object) (boxing) and was therefore always true, even for Guid.Empty.
    public static bool IsNotNull(this Guid token) => token != Guid.Empty;

    public static bool CanBeCastTo<T>(this string value)
    {
        var type = typeof(T);
        if (!type.IsEnum) return false;

        var canBeCast = Enum.GetNames(type).Contains(value);
        if (!canBeCast) return false;

        return true;
    }

    public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
    {
        foreach (var item in items) action(item);
    }

    // Was `async void`: callers could not await it and any exception crashed the process.
    // Now returns a Task; existing call sites still compile (await it to observe completion/errors).
    public async static Task ForEach<T>(this IAsyncEnumerable<T> items, Action<T> action)
    {
        await foreach (var item in items) action(item);
    }

    public static void ForEach<T>(this List<T> items, Action<T> action)
    {
        foreach (var item in items) action(item);
    }
}