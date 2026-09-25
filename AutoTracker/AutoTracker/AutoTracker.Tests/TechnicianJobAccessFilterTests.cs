using AutoTracker.Filters;
using AutoTracker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace AutoTracker.Tests;

public class TechnicianJobAccessFilterTests
{
    [Fact]
    public async Task DuplicateLegacyNames_DoNotGrantJobAccess()
    {
        using var database = new TestDatabase();
        var company = new Company { Name = "Workshop" };
        var first = new AppUser { FullName = "Same Name", Email = "one@example.test", Role = "Technician", Company = company, IsActive = true };
        var second = new AppUser { FullName = "Same Name", Email = "two@example.test", Role = "Technician", Company = company, IsActive = true };
        var client = new Client { FullName = "Client" };
        var vehicle = new Vehicle { RegNumber = "DUP1", Client = client };
        var job = new RepairJob { Client = client, Vehicle = vehicle, AssignedTechnician = "Same Name", AssignedTechnicianUserId = null };
        database.Db.AddRange(first, second, job);
        await database.Db.SaveChangesAsync();

        var (context, executed) = CreateContext(first, job.Id);
        await new TechnicianJobAccessFilter(database.Db).OnActionExecutionAsync(context, executed);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task StableUserId_AllowsAccessAfterANameChange()
    {
        using var database = new TestDatabase();
        var company = new Company { Name = "Workshop" };
        var technician = new AppUser { FullName = "New Name", Email = "tech@example.test", Role = "Technician", Company = company, IsActive = true };
        var client = new Client { FullName = "Client" };
        var vehicle = new Vehicle { RegNumber = "ID1", Client = client };
        var job = new RepairJob { Client = client, Vehicle = vehicle, AssignedTechnician = "Old Name" };
        database.Db.AddRange(technician, job);
        await database.Db.SaveChangesAsync();
        job.AssignedTechnicianUserId = technician.Id;
        await database.Db.SaveChangesAsync();

        var called = false;
        var (context, _) = CreateContext(technician, job.Id);
        ActionExecutionDelegate next = () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        };

        await new TechnicianJobAccessFilter(database.Db).OnActionExecutionAsync(context, next);

        Assert.True(called);
        Assert.Null(context.Result);
    }

    private static (ActionExecutingContext Context, ActionExecutionDelegate Next) CreateContext(AppUser user, int jobId)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, "Technician")
            ], "Test"))
        };
        var route = new RouteData();
        route.Values["controller"] = "RepairJobs";
        var actionContext = new ActionContext(http, route, new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?> { ["id"] = jobId }, new object());
        ActionExecutionDelegate next = () => Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
        return (context, next);
    }
}
