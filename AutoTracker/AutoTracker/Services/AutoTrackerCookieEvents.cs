using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace AutoTracker.Services;

public class AutoTrackerCookieEvents : CookieAuthenticationEvents
{
    private readonly UserSessionService _sessions;

    public AutoTrackerCookieEvents(UserSessionService sessions) => _sessions = sessions;

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var versionValue = context.Principal?.FindFirstValue("session_version");
        var valid = int.TryParse(userIdValue, out var userId)
            && int.TryParse(versionValue, out var version)
            && await _sessions.IsValidAsync(userId, version);

        if (!valid)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
