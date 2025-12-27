using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Bat.AspNetCore;

public class BatSwaggerAuthenticateFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            var haveAuthenticateAttribute = context.ApiDescription.CustomAttributes().Any(x => x.GetType().Name.Contains("Authenticate"));
            if (haveAuthenticateAttribute && !context.ApiDescription.CustomAttributes().Any((a) => a is AllowAnonymousAttribute))
            {
                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "Token",
                    Required = true,
                    In = ParameterLocation.Header,
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,

                        Default = "33159CFB-06DF-4007-8DFF-17F8D916D782"
                    },
                    Description = "Header Token For Authenticate Request",
                });
            }
        }
    }
}