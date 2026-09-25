using AutoTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Services;

public class UserSessionService
{
    private readonly AppDbContext _db;

    public UserSessionService(AppDbContext db) => _db = db;

    public Task<bool> IsValidAsync(int userId, int sessionVersion) =>
        _db.AppUsers.AnyAsync(u => u.Id == userId && u.IsActive && u.SessionVersion == sessionVersion);
}
