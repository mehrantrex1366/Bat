using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Bat.AspNetCore;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizationFilter : ActionFilterAttribute, IAuthorizationFilter
{
    // Attribute lookups used to run (2-3 times) on every request; the result only depends on the action method.
    private static readonly ConcurrentDictionary<MethodInfo, (bool AllowAnonymous, AuthEqualTo AuthEqualTo)> actionInfo = new();

    private static (bool AllowAnonymous, AuthEqualTo AuthEqualTo) GetActionInfo(MethodInfo method)
        => actionInfo.GetOrAdd(method, static m =>
        {
            var attributes = m.GetCustomAttributes(inherit: true);
            return (attributes.Any(a => a.GetType() == typeof(AllowAnonymousAttribute)),
                    attributes.OfType<AuthEqualTo>().FirstOrDefault());
        });

    private static bool HasAccess(IEnumerable<UserAction> userActionList, string controller, string action)
    {
        if (userActionList == null) return false;

        foreach (var x in userActionList)
            if (string.Equals(x.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Action, action, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private static string GetUserId(AuthorizationFilterContext context)
        => context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private bool IsAuthorized(AuthorizationFilterContext context, string controller, string action)
    {
        var id = GetUserId(context);
        if (id is null) return false;

        var userActionList = (context.HttpContext.RequestServices.GetService(typeof(IUserActionProvider)) as IUserActionProvider).GetUserActions(id);
        return HasAccess(userActionList, controller, action);
    }

    private async Task<bool> IsAuthorizedAsync(AuthorizationFilterContext context, string controller, string action)
    {
        var id = GetUserId(context);
        if (id is null) return false;

        var userActionList = await (context.HttpContext.RequestServices.GetService(typeof(IUserActionProvider)) as IUserActionProvider).GetUserActionsAsync(id);
        return HasAccess(userActionList, controller, action);
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity.IsAuthenticated)
        {
            context.Result = new StatusCodeResult(403);
            return;
        }

        var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var actionInfo = GetActionInfo(controllerActionDescriptor.MethodInfo);
        if (actionInfo.AllowAnonymous) return;

        bool Authorize;
        if (actionInfo.AuthEqualTo is not null)
            Authorize = IsAuthorized(context, actionInfo.AuthEqualTo.ControllerName, actionInfo.AuthEqualTo.ActionName);
        else
            Authorize = IsAuthorized(context, context.RouteData.Values["controller"].ToString(), context.RouteData.Values["action"].ToString());

        if (!Authorize)
        {
            context.Result = new StatusCodeResult(401);
            return;
        }
    }

    // Note: MVC only calls this if the filter implements IAsyncAuthorizationFilter (it does not), so
    // OnAuthorization above is what runs. Kept for callers that invoke it directly.
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity.IsAuthenticated)
        {
            context.Result = new StatusCodeResult(403);
            return;
        }

        var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var actionInfo = GetActionInfo(controllerActionDescriptor.MethodInfo);
        if (actionInfo.AllowAnonymous) return;

        bool Authorize;
        if (actionInfo.AuthEqualTo is not null)
            Authorize = await IsAuthorizedAsync(context, actionInfo.AuthEqualTo.ControllerName, actionInfo.AuthEqualTo.ActionName);
        else
            Authorize = await IsAuthorizedAsync(context, context.RouteData.Values["controller"].ToString(), context.RouteData.Values["action"].ToString());

        if (!Authorize)
        {
            context.Result = new StatusCodeResult(401);
            return;
        }
    }
}
