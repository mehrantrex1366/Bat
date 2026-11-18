using Microsoft.OpenApi;
using System.Reflection;
using System.Collections.Concurrent;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bat.AspNetCore;

public class BatSwaggerExcludeFilter : ISchemaFilter
{
    private static readonly ConcurrentDictionary<Type, HashSet<string>> _excludedProperties = new();

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema?.Properties == null || schema.Properties.Count == 0 || context?.Type == null)
            return;

        var excluded = _excludedProperties.GetOrAdd(context.Type, static type => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<SwaggerExcludeAttribute>() != null)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase));

        if (excluded.Count == 0)
            return;

        // Fixed (11.0.0): schema property names follow the JSON naming policy (camelCase: "secret"), but the CLR name
        // ("Secret") was removed with the dictionary's ordinal comparer, so nothing was excluded.
        foreach (var key in schema.Properties.Keys.Where(excluded.Contains).ToList())
            schema.Properties.Remove(key);
    }
}