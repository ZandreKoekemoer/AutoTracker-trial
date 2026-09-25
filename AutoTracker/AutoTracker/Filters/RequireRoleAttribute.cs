using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AutoTracker.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IActionFilter
{
    private readonly HashSet<string> _roles;

    public RequireRoleAttribute(params string[] roles) =>
        _roles = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var allowed = _roles.Any(role => context.HttpContext.User.IsInRole(role));
        if (!allowed)
        {
            var sessionRole = context.HttpContext.Session.GetString("UserRole") ?? "";
            allowed = _roles.Contains(sessionRole);
        }

        if (!allowed)
            context.Result = new RedirectToActionResult("Index", "Home", new { error = "Access denied" });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
