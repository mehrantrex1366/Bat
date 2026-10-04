using System.Collections.Concurrent;

namespace Bat.Core;

public static class ObjectExtensions
{
    // Type.GetProperties() allocates a new array on every call; cache per type.
    private static readonly ConcurrentDictionary<Type, System.Reflection.PropertyInfo[]> _properties = new();

    private static System.Reflection.PropertyInfo[] GetCachedProperties(Type type)
        => _properties.GetOrAdd(type, static t => t.GetProperties());

    private static System.Reflection.PropertyInfo FindByName(System.Reflection.PropertyInfo[] properties, string name)
    {
        foreach (var property in properties)
            if (property.Name == name) return property;

        return null;
    }


    public static T GetInstance<T>(this T obj) where T : new() => obj == null ? new T() : obj;


    public static TDestination CopyFrom<TDestination>(object sourceObject) where TDestination : class, new()
    {
        var result = new TDestination();
        return result.CopyFrom(sourceObject);
    }


    public static TDestination CopyFrom<TDestination, TSource>(this TDestination destinationObject, TSource sourceObject) where TDestination : class where TSource : class
    {
        var sourceProperties = GetCachedProperties(sourceObject.GetType());
        var destinationProperties = GetCachedProperties(destinationObject.GetType());
        foreach (var property in destinationProperties)
        {
            try
            {
                var tempProperty = FindByName(sourceProperties, property.Name);
                if (tempProperty != null && tempProperty.CanWrite) property.SetValue(destinationObject, tempProperty.GetValue(sourceObject));
            }
            catch { }
        }

        return destinationObject;
    }


    public static void UpdateWith<TDestination, TSource>(this TDestination destinationObject, TSource sourceObject) where TDestination : class where TSource : class
    {
        var sourceProperties = GetCachedProperties(sourceObject.GetType());
        var destinationProperties = GetCachedProperties(destinationObject.GetType());
        foreach (var property in destinationProperties)
        {
            try
            {
                var tempProperty = FindByName(sourceProperties, property.Name);
                if (tempProperty != null) property.SetValue(destinationObject, tempProperty.GetValue(sourceObject));
            }
            catch { }
        }
    }


    public static void SetProperty<TEntity, TItem>(this TEntity objec, string name, TItem value)
        => typeof(TEntity).GetProperty(name).SetValue(objec, value, null);

    public static TItem GetProperty<TEntity, TItem>(this TEntity objec, string name) where TEntity : class
    {
        if (objec != null)
        {
            var value = typeof(TEntity).GetProperty(name).GetValue(objec, null);
            if (value != null)
            {
                return (TItem)(value);
            }
        }
        return default;
    }

    // Fixed: the condition was inverted (it returned null for every non-null object and threw for null).
    public static object GetProperty(this object objec, string name)
        => objec != null ? objec.GetType().GetProperty(name)?.GetValue(objec, null) : null;

}
