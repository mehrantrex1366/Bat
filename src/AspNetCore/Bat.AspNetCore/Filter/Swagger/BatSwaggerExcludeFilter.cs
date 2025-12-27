using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bat.AspNetCore;

public class BatSwaggerExcludeFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema?.Properties == null || context?.Type == null)
            return;

        var excludedProperties = context.Type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<SwaggerExcludeAttribute>() != null)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var propName in excludedProperties)
            schema.Properties.Remove(propName);
    }
}