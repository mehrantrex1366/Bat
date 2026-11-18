using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;

namespace Bat.AspNetCore;

/// <summary>
/// Like <see cref="BatModelValidationAttribute"/>, and also runs the .NET 11 <b>async</b> DataAnnotations validation
/// (<c>AsyncValidationAttribute</c>, <c>IAsyncValidatableObject</c>) on every complex action argument, which MVC's
/// synchronous model validation does not run. Errors → 400 with <c>Response&lt;object&gt;</c> and <c>|</c>-joined messages
/// (same body as <see cref="BatModelValidationAttribute"/>). Attributes get <c>HttpContext.RequestServices</c> as service provider.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class BatAsyncValidationAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new Response<object>(context.ModelState.GetModelError()));
            return;
        }

        var errors = new List<ValidationResult>();
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null || IsSimple(argument.GetType()))
                continue;

            errors.AddRange(await argument.ValidateDataAnnotationsAsync(context.HttpContext.RequestServices, context.HttpContext.RequestAborted));
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(new Response<object>(errors.JoinErrors()));
            return;
        }

        await next();
    }

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(CancellationToken);
    }
}