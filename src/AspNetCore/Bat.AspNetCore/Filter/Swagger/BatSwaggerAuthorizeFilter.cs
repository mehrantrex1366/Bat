using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Bat.AspNetCore;

public class BatSwaggerAuthorizeFilter : IOperationFilter
{
    /// <summary>Name of the security scheme the filter references (<c>AddSecurityDefinition("Bearer", ...)</c>).</summary>
    public const string BearerSchemeName = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

            if (hasAllowAnonymous)
                return;

            var hasAuthorize =
            context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true ||
            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

            if (hasAuthorize is false)
                return;

            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            // Fixed (11.0.0): the reference was created with an empty id, which Microsoft.OpenApi 3 rejects
            // (ArgumentNullException -> swagger.json returned 500 for every [Authorize] action).
            // Reference the "Bearer" security scheme of the document being generated; define it with
            // option.AddSecurityDefinition("Bearer", ...) so the requirement is resolved in the output.
            var jwtScheme = new OpenApiSecuritySchemeReference(BearerSchemeName, context.Document);

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [ jwtScheme ] = []
                }
            };
        }
    }
}