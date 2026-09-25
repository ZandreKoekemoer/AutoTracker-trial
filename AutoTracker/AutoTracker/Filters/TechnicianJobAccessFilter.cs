using AutoTracker.Data;
using AutoTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoTracker.Filters;

public class TechnicianJobAccessFilter : IAsyncActionFilter
{
    private readonly AppDbContext _db;
    public TechnicianJobAccessFilter(AppDbContext db) => _db = db;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.User.IsInRole("Technician"))
        {
            await next();
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        if (controller is not ("RepairJobs" or "Parts"))
        {
            await next();
            return;
        }

        int? repairJobId = ValueAsInt(GetArgument(context.ActionArguments, "repairJobId"));
        if (repairJobId == null && controller == "RepairJobs")
            repairJobId = ValueAsInt(GetArgument(context.ActionArguments, "id"));
        if (repairJobId == null && GetArgument(context.ActionArguments, "part") is Part part)
            repairJobId = part.RepairJobId;
        if (repairJobId == null && GetArgument(context.ActionArguments, "checklist") is Checklist checklist)
            repairJobId = checklist.RepairJobId;
        if (repairJobId == null)
        {
            await next();
            return;
        }

        var userName = context.HttpContext.User.FindFirstValue(ClaimTypes.Name) ?? "";
        var userIdText = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdText, out var userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var allowed = await _db.RepairJobs.AnyAsync(j =>
            j.Id == repairJobId &&
            (j.AssignedTechnicianUserId == userId ||
             (j.AssignedTechnicianUserId == null && j.AssignedTechnician == userName &&
              !_db.AppUsers.Any(u => u.IsActive && u.Id != userId && u.FullName == userName))));
        if (!allowed)
        {
            context.Result = new ForbidResult();
            return;
        }
        await next();
    }

    private static object? GetArgument(IDictionary<string, object?> arguments, string name) =>
        arguments.TryGetValue(name, out var value) ? value : null;

    private static int? ValueAsInt(object? value) => value switch
    {
        int id when id > 0 => id,
        string text when int.TryParse(text, out var id) && id > 0 => id,
        _ => null
    };
}
