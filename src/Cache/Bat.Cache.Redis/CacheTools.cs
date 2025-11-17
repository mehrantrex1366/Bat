namespace Bat.Cache.Redis;

public static class CacheTools
{
    public static string CreateKey<T>(ICollection<string> parameters)
    {
        var typeName = typeof(T).Name.ToLower();
        var key = string.Join(":", parameters.Select(p => p?.ToLower() ?? string.Empty));
        return $"{typeName}:{key}";
    }

    public static string CreateKey<T>(ICollection<object> parameters)
    {
        var typeName = typeof(T).Name.ToLower();
        var key = string.Join(":", parameters.Select(p => p?.ToString()?.ToLower() ?? string.Empty));
        return $"{typeName}:{key}";
    }

    public static string CreateKey<T>(params object[] parameters)
    {
        var typeName = typeof(T).Name.ToLower();
        var key = string.Join(":", parameters.Select(p => p?.ToString()?.ToLower() ?? string.Empty));
        return $"{typeName}:{key}";
    }
}