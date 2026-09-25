using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace AutoTracker.Filters;

public class SessionAuthorizeFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
        var action = context.RouteData.Values["action"]?.ToString() ?? "";
        var publicRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Account:Login", "Account:Register", "ClientTracker:Index", "ClientTracker:Job", "ClientTracker:Media"
        };

        if (!publicRoutes.Contains($"{controller}:{action}"))
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var user = context.HttpContext.User;
            if (int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                context.HttpContext.Session.SetInt32("UserId", userId);
            context.HttpContext.Session.SetString("UserName", user.Identity?.Name ?? "");
            context.HttpContext.Session.SetString("UserRole", user.FindFirstValue(ClaimTypes.Role) ?? "");
            if (int.TryParse(user.FindFirstValue("company_id"), out var companyId))
                context.HttpContext.Session.SetInt32("CompanyId", companyId);
        }

        await next();
    }
}
