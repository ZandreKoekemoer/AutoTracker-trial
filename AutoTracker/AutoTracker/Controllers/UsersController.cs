using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public UsersController(AppDbContext db) => _db = db;

    private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private string CurrentRole() => User.FindFirstValue(ClaimTypes.Role) ?? "";
    private int CurrentCompanyId() => int.TryParse(User.FindFirstValue("company_id"), out var id) ? id : 1;

    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = UserRolePolicy.AllowedRoles;
        ViewBag.CurrentUserIsAdmin = string.Equals(CurrentRole(), "Administrator", StringComparison.OrdinalIgnoreCase);
        ViewBag.CurrentUserId = CurrentUserId();
        return View(await _db.AppUsers
            .Where(u => u.CompanyId == CurrentCompanyId())
            .OrderBy(u => u.FullName)
            .ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(string fullName, string email, string password, string role, bool isActive = true)
    {
        fullName = (fullName ?? "").Trim();
        email = (email ?? "").Trim().ToLowerInvariant();
        role = (role ?? "").Trim();

        if (!UserRolePolicy.CanManageRole(CurrentRole(), "Technician", role))
            return Denied("You do not have permission to grant that role.");
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Denied("Name, email and password are required.");
        if (password.Length < 8)
            return Denied("Password must be at least 8 characters.");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return Denied("Enter a valid email address.");
        if (await _db.AppUsers.AnyAsync(u => u.Email == email))
            return Denied("A user with this email already exists.");
        if (await _db.AppUsers.AnyAsync(u => u.FullName.ToLower() == fullName.ToLower()))
            return Denied("A user with this name already exists.");

        var user = new AppUser
        {
            FullName = fullName,
            Email = email,
            Role = role,
            IsActive = isActive,
            CompanyId = CurrentCompanyId(),
            SessionVersion = 1,
            CreatedAt = DateTime.Now
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
        await AddAuditAsync(user.Id, "User created");

        TempData["Success"] = "User created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUser(int id, string fullName, string email, string role, bool isActive, string? newPassword)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == CurrentCompanyId());
        if (user == null) return NotFound();

        fullName = (fullName ?? "").Trim();
        email = (email ?? "").Trim().ToLowerInvariant();
        role = (role ?? "").Trim();

        if (!UserRolePolicy.CanManageRole(CurrentRole(), user.Role, role))
            return Denied("You do not have permission to manage that role.");
        if (id == CurrentUserId() && (!isActive || !string.Equals(role, user.Role, StringComparison.OrdinalIgnoreCase)))
            return Denied("Use another administrator to change your own role or active status.");
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            return Denied("Name and email are required.");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return Denied("Enter a valid email address.");
        if (await _db.AppUsers.AnyAsync(u => u.Id != id && u.Email == email))
            return Denied("Another user already has that email.");
        if (await _db.AppUsers.AnyAsync(u => u.Id != id && u.FullName.ToLower() == fullName.ToLower()))
            return Denied("Another user already has that name.");
        if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Trim().Length < 8)
            return Denied("Password must be at least 8 characters.");

        var securityChanged = user.IsActive != isActive ||
            !string.Equals(user.Role, role, StringComparison.Ordinal) ||
            !string.Equals(user.FullName, fullName, StringComparison.Ordinal) ||
            !string.Equals(user.Email, email, StringComparison.Ordinal);
        user.FullName = fullName;
        user.Email = email;
        user.Role = role;
        user.IsActive = isActive;
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword.Trim());
            securityChanged = true;
        }
        if (securityChanged) user.SessionVersion++;

        await _db.SaveChangesAsync();
        if (role == "Technician" && isActive)
        {
            await _db.RepairJobs.Where(j => j.AssignedTechnicianUserId == user.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(j => j.AssignedTechnician, fullName));
        }
        else
        {
            await _db.RepairJobs.Where(j => j.AssignedTechnicianUserId == user.Id)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(j => j.AssignedTechnicianUserId, (int?)null)
                    .SetProperty(j => j.AssignedTechnician, ""));
        }
        await AddAuditAsync(user.Id, securityChanged ? "User updated and sessions revoked" : "User profile updated");
        TempData["Success"] = securityChanged ? "User updated. Existing sessions were revoked." : "User updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RevokeSessions(int id)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == CurrentCompanyId());
        if (user == null) return NotFound();
        if (!UserRolePolicy.CanManageRole(CurrentRole(), user.Role, user.Role))
            return Denied("You do not have permission to revoke this user's sessions.");
        if (user.Id == CurrentUserId())
            return Denied("Use Log out to end your own session.");

        user.SessionVersion++;
        await _db.SaveChangesAsync();
        await AddAuditAsync(user.Id, "Sessions revoked");
        TempData["Success"] = "The user will be signed out on their next request.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == CurrentCompanyId());
        if (user == null) return NotFound();
        if (!UserRolePolicy.CanManageRole(CurrentRole(), user.Role, user.Role))
            return Denied("You do not have permission to deactivate this user.");
        if (user.Id == CurrentUserId())
            return Denied("You cannot deactivate your own account.");

        user.IsActive = false;
        user.SessionVersion++;
        await _db.RepairJobs.Where(j => j.AssignedTechnicianUserId == user.Id)
            .ExecuteUpdateAsync(update => update
                .SetProperty(j => j.AssignedTechnicianUserId, (int?)null)
                .SetProperty(j => j.AssignedTechnician, ""));
        await _db.SaveChangesAsync();
        await AddAuditAsync(user.Id, "User deactivated and sessions revoked");
        TempData["Success"] = "User deactivated. Existing sessions were revoked.";
        return RedirectToAction(nameof(Index));
    }

    private async Task AddAuditAsync(int targetUserId, string action)
    {
        _db.UserSecurityAuditLogs.Add(new UserSecurityAuditLog
        {
            ActorUserId = CurrentUserId(),
            TargetUserId = targetUserId,
            Action = action,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private IActionResult Denied(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
