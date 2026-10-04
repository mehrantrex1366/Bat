using System.Collections.Concurrent;

namespace Bat.Dapper;

public static class ParameterExtension
{
    // Column metadata per CLR type (the reflection + attribute checks used to run on every call).
    private static readonly ConcurrentDictionary<Type, System.Reflection.PropertyInfo[]> _fields = new();

    private static System.Reflection.PropertyInfo[] GetFields(Type type)
        => _fields.GetOrAdd(type, static t =>
        {
            var assemblyName = t.Assembly.FullName.Split(',')[0];
            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite
                && x.GetCustomAttribute(typeof(NotMappedAttribute)) == null
                && x.GetCustomAttribute(typeof(ForeignKeyAttribute)) == null
                && (x.PropertyType.FullName.StartsWith("Bat.") ||
                x.PropertyType.FullName.StartsWith(assemblyName) ||
                (x.PropertyType.FullName.StartsWith("System.")
                && (!x.PropertyType.FullName.Contains("System.Collection"))))).ToArray();
        });

    private static DataTable CreateTable(System.Reflection.PropertyInfo[] fields)
    {
        var dataTable = new DataTable();
        foreach (var field in fields)
        {
            // Fixed: DataTable does not support Nullable<T> columns ("DataSet does not support System.Nullable<>"),
            // so any int?/DateTime?/... property made this throw. Use the underlying type; nulls become DBNull.
            var type = Nullable.GetUnderlyingType(field.PropertyType) ?? field.PropertyType;
            if (type.IsEnum)
                if (type.IsInheritFrom(typeof(byte))) dataTable.Columns.Add(field.Name, typeof(byte));
                else dataTable.Columns.Add(field.Name, typeof(int));
            else
                dataTable.Columns.Add(field.Name, type);
        }

        return dataTable;
    }

    private static object[] GetRow(System.Reflection.PropertyInfo[] fields, object obj)
    {
        var row = new object[fields.Length];
        for (int i = 0; i < fields.Length; i++)
            row[i] = fields[i].GetValue(obj, null) ?? DBNull.Value;

        return row;
    }

    /// <summary>
    /// This extension converts an enumerable set to a Dapper TVP
    /// </summary>
    /// <typeparam name="T">type of enumerbale</typeparam>
    /// <param name="parameter">list of values</param>
    /// <param name="typeName">database type name</param>
    /// <returns>a custom query parameter</returns>
    public static SqlMapper.ICustomQueryParameter ToTableValuedParameter<T>
        (this List<T> parameter, string typeName)
    {
        var fields = GetFields(typeof(T));
        var dataTable = CreateTable(fields);

        foreach (T obj in parameter)
            dataTable.Rows.Add(GetRow(fields, obj));

        return dataTable.AsTableValuedParameter(typeName);
    }

    /// <summary>
    /// This extension converts an enumerable set to a Dapper TVP
    /// </summary>
    /// <typeparam name="T">type of enumerbale</typeparam>
    /// <param name="parameter">list of values</param>
    /// <param name="typeName">database type name</param>
    /// <param name="columnNames">if more than one column in a TVP, 
    /// columns order must mtach order of columns in TVP</param>
    /// <returns>a custom query parameter</returns>
    public static SqlMapper.ICustomQueryParameter ToTableValuedParameter<T>
        (this T parameter, string typeName, string columnNames = null)
    {
        DataTable dataTable;
        if (typeof(T).IsValueType)// || typeof(T).FullName.Equals("System.String"))
        {
            dataTable = new DataTable();
            dataTable.Columns.Add(columnNames == null ? "NONAME" : columnNames, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
            dataTable.Rows.Add((object)parameter ?? DBNull.Value);
        }
        else
        {
            var fields = GetFields(typeof(T));
            dataTable = CreateTable(fields);
            dataTable.Rows.Add(GetRow(fields, parameter));
        }

        return dataTable.AsTableValuedParameter(typeName);
    }
}
