using System.Reflection;
using System.Linq.Expressions;

namespace Bat.Core;

public static class ReflectionExtensions
{
    public static bool IsInheritFrom(this Type type, Type parentType) => parentType.IsAssignableFrom(type);

    public static bool IsInheritFrom<T>(this Type type) => IsInheritFrom(type, typeof(T));

    public static bool BaseTypeIsGeneric(this Type type, Type genericType) => type.BaseType?.IsGenericType == true && type.BaseType.GetGenericTypeDefinition() == genericType;

    public static MemberInfo GetMemberInfo<T>(this Expression<Func<T, object>> expression)
    {
        var mbody = expression.Body as MemberExpression;
        var ubody = expression.Body as UnaryExpression;

        if (mbody != null) return mbody.Member;
        if (ubody != null) mbody = ubody.Operand as MemberExpression;
        if (mbody == null) throw new ArgumentException("Expression is not a MemberExpression", nameof(expression));

        return mbody.Member;
    }

    public static string GetNameProperty<T>(this Expression<Func<T, object>> expression) => GetMemberInfo(expression).Name;

    public static string GetDisplayProperty<T>(this Expression<Func<T, object>> expression)
    {
        var propertyMember = GetMemberInfo(expression);
        var displayAttributes = propertyMember.GetCustomAttributes(typeof(DisplayNameAttribute), true);
        return displayAttributes.Length == 1 ? ((DisplayNameAttribute)displayAttributes[0]).DisplayName : propertyMember.Name;
    }

    // Same lookup order as before (member attributes first, then attributes of the object's type),
    // but without using exceptions (failed casts) for control flow.
    public static object GetAttribute(this object obj, Type attributeName)
    {
        if (obj is null) return null;

        object[] attributes;
        if (obj is MemberInfo memberInfo)
        {
            attributes = memberInfo.GetCustomAttributes(attributeName, false);
            if (attributes.Length > 0) return attributes[0];
        }

        try
        {
            attributes = obj.GetType().GetCustomAttributes(attributeName, false);
            if (attributes.Length > 0) return attributes[0];
        }
        catch { }

        return null;
    }

    public static string GetDescription<T>(this System.Reflection.PropertyInfo genericType)
    {
        var type = genericType.GetType();
        var memberInfo = type.GetMember(genericType.ToString());

        object[] attributes = type.GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attributes != null && attributes.Length > 0) return ((DescriptionAttribute)attributes[0]).Description;
        else if (memberInfo != null && memberInfo.Length > 0)
        {
            attributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes != null && attributes.Length > 0) return ((DescriptionAttribute)attributes[0]).Description;
        }

        return genericType.ToString();
    }

    public static string GetDescription<T>(this T genericType)
    {
        var fieldInfo = genericType.GetType().GetField(genericType.ToString());
        var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes != null && attributes.Length > 0) return attributes[0].Description;
        else return genericType.ToString();
    }

    public static string GetDisplayName<T>(this T genericType)
    {
        var type = genericType.GetType();
        var fields = type.GetField(genericType.ToString());

        object[] attributes = (DisplayAttribute[])type.GetCustomAttributes(typeof(DisplayAttribute), false);
        if (attributes != null && attributes.Length > 0) return ((DisplayAttribute)attributes[0]).Name;
        else if (fields != null)
        {
            attributes = fields.GetCustomAttributes(typeof(DisplayAttribute), false);
            if (attributes != null && attributes.Length > 0) return ((DisplayAttribute)attributes[0]).Name;
        }

        return genericType.ToString();
    }

    public static IEnumerable<PropertyInfo> GetClassFields<T>(this T classType) where T : class
    {
        return typeof(T).GetProperties().Select(x => new PropertyInfo
        {
            Name = x.Name,
            Value = x.GetValue(classType, null),
            Type = x.MemberType.ToString(),
            DataType = x.PropertyType.Name,
            Description = (x.GetAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? string.Empty,
            DisplayName = (x.GetAttribute(typeof(DisplayAttribute)) as DisplayAttribute)?.GetName() ?? string.Empty
        });
    }

    public static IEnumerable<PropertyInfo> GetClassFields(this object classType, Type type) //where T : class
    {
        return type.GetProperties().Select(x => new PropertyInfo
        {
            Name = x.Name,
            Value = x.GetValue(classType, null),
            Type = x.MemberType.ToString(),
            DataType = x.PropertyType.Name,
            Description = (x.GetAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? string.Empty,
            DisplayName = (x.GetAttribute(typeof(DisplayAttribute)) as DisplayAttribute)?.GetName() ?? string.Empty
        });
    }

}