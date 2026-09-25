using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.AspNetCore.Identity;

namespace AutoTracker.Tests;

public class UserSessionServiceTests
{
    [Fact]
    public async Task IsValidAsync_RejectsDeactivatedOrRevokedUser()
    {
        using var database = new TestDatabase();
        var company = new Company { Name = "Test Company" };
        var user = new AppUser
        {
            FullName = "Staff Member",
            Email = "staff@example.test",
            Role = "Technician",
            Company = company,
            IsActive = true,
            SessionVersion = 3
        };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, "SafePassword123!");
        database.Db.AppUsers.Add(user);
        await database.Db.SaveChangesAsync();
        var service = new UserSessionService(database.Db);

        Assert.True(await service.IsValidAsync(user.Id, 3));
        Assert.False(await service.IsValidAsync(user.Id, 2));

        user.IsActive = false;
        await database.Db.SaveChangesAsync();
        Assert.False(await service.IsValidAsync(user.Id, 3));
    }
}
