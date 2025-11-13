using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Bat.AspNetCore;

public class BatSwaggerExcludeOperationFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var excludedPaths = context.ApiDescriptions
            .Where(desc =>
            {
                return desc.ActionDescriptor is ControllerActionDescriptor action && (
                    action.MethodInfo.GetCustomAttributes(typeof(SwaggerExcludeAttribute), false).Length != 0 ||
                    action.ControllerTypeInfo.GetCustomAttributes(typeof(SwaggerExcludeAttribute), false).Length != 0);
            })
            .Select(desc => "/" + desc.RelativePath.TrimEnd('/'))
            .Distinct()
            .ToList();

        foreach (var path in excludedPaths)
            swaggerDoc.Paths.Remove(path);
    }
}