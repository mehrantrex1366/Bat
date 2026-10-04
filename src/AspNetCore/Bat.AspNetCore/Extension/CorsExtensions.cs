using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Bat.AspNetCore;

public static class CorsExtensions
{
    public static void AddBatCors(this IServiceCollection services, string policyName, CorsPolicy corsPolicy)
    {
        services.AddCors(option =>
        {
            option.AddPolicy(policyName, corsPolicy);
        });
    }

    public static void AddBatCors(this IServiceCollection services, string policyName, List<string> domains = null, List<string> headers = null, List<string> methods = null)
    {
        var policyBuilder = new CorsPolicyBuilder();
        var corsPolicy = domains == null
                        ? policyBuilder.AllowAnyOrigin()
                        : policyBuilder.WithOrigins(domains.ToArray());
        corsPolicy = headers == null
                        ? policyBuilder.AllowAnyHeader()
                        : policyBuilder.WithHeaders(headers.ToArray());
        corsPolicy = methods == null
                        ? policyBuilder.AllowAnyMethod()
                        : policyBuilder.WithMethods(methods.ToArray());

        services.AddCors(option =>
        {
            option.AddPolicy(policyName, policyBuilder.Build());
        });
    }



    public static void UseBatCors(this IApplicationBuilder app, string policyName)
    {
        app.UseCors(policyName);
    }

    public static void UseBatCors(this IApplicationBuilder app)
    {
        app.UseCors(corsPolicyBuilder =>
        {
            corsPolicyBuilder.AllowAnyHeader();
            corsPolicyBuilder.AllowAnyMethod();
            corsPolicyBuilder.AllowAnyOrigin();
        });
    }

    public static void UseBatCors(this IApplicationBuilder app, List<string> domains = null, List<string> headers = null, List<string> methods = null)
    {
        // Fixed: `corsPolicyBuilder = corsPolicy` only reassigned the lambda parameter, so an empty policy
        // (nothing allowed) was applied. Configure the builder that UseCors actually uses.
        app.UseCors(builder =>
        {
            if (domains == null) builder.AllowAnyOrigin(); else builder.WithOrigins([.. domains]);
            if (headers == null) builder.AllowAnyHeader(); else builder.WithHeaders([.. headers]);
            if (methods == null) builder.AllowAnyMethod(); else builder.WithMethods([.. methods]);
        });
    }
}