using System.Reflection;

namespace Bat.Core;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumObj)
    {
        var fields = enumObj.GetType().GetField(enumObj.ToString());
        var attributes = (DisplayAttribute[])fields.GetCustomAttributes(typeof(DisplayAttribute), false);

        if (attributes.Length > 0) return attributes[0].Name;
        else return enumObj.ToString();
    }

    // Returns string.Empty when the value is null or is not a defined member (e.g. (MyEnum)99);
    // returns the member name when it has no [Description].
    public static string GetDescription(this Enum enumObj)
    {
        if (enumObj is null) return string.Empty;

        var name = enumObj.ToString();
        var fieldInfo = enumObj.GetType().GetField(name);
        if (fieldInfo is null) return string.Empty;

        return fieldInfo.GetCustomAttribute<DescriptionAttribute>(false)?.Description ?? name;
    }

    public static IEnumerable<Core.PropertyInfo> GetEnumElements<T>() where T : Enum
    {
        var result = new List<Core.PropertyInfo>();
        if (typeof(T).BaseType == typeof(Enum))
        {
            foreach (var item in Enum.GetValues(typeof(T)))
            {
                Enum val = Enum.Parse(typeof(T), item.ToString()) as Enum;
                result.Add(new Core.PropertyInfo
                {
                    Name = item.ToString(),
                    Type = item.GetType().Name,
                    Value = Convert.ToInt32(val),
                    DisplayName = item.GetDisplayName(),
                    DataType = item.GetType().BaseType.Name,
                    Description = item.GetDescription()
                });
            }
        }
        return result;
    }

    public static IEnumerable<TEnum> FilterEnumWithAttributeOf<TEnum, TAttribute>() where TEnum : struct where TAttribute : class
    {
        foreach (var field in typeof(TEnum).GetFields(BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetCustomAttributes(typeof(TAttribute), false).Length > 0)
                yield return (TEnum)field.GetValue(null);
        }
    }

}