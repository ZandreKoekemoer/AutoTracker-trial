using AutoTracker.Data;
using AutoTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AutoTracker.Services;

public class ClientTrackingService
{
    private readonly AppDbContext _db;

    public ClientTrackingService(AppDbContext db) => _db = db;

    public sealed record GeneratedAccess(string RawToken, DateTime ExpiresAtUtc);

    public async Task<GeneratedAccess> GenerateAsync(int repairJobId, int createdByUserId, TimeSpan lifetime)
    {
        var jobExists = await _db.RepairJobs.AnyAsync(j => j.Id == repairJobId);
        if (!jobExists) throw new InvalidOperationException("Repair job could not be found.");

        var now = DateTime.UtcNow;
        var active = await _db.ClientTrackingAccesses
            .Where(x => x.RepairJobId == repairJobId && x.RevokedAtUtc == null)
            .ToListAsync();
        foreach (var access in active) access.RevokedAtUtc = now;

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var expires = now.Add(lifetime);
        _db.ClientTrackingAccesses.Add(new ClientTrackingAccess
        {
            RepairJobId = repairJobId,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = expires,
            CreatedAtUtc = now,
            CreatedByUserId = createdByUserId
        });
        await _db.SaveChangesAsync();
        return new GeneratedAccess(rawToken, expires);
    }

    public async Task<RepairJob?> FindValidJobAsync(string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return null;
        var hash = HashToken(rawToken.Trim());
        var now = DateTime.UtcNow;
        var access = await _db.ClientTrackingAccesses
            .Include(x => x.RepairJob)!.ThenInclude(j => j!.Client)
            .Include(x => x.RepairJob)!.ThenInclude(j => j!.Vehicle)
            .Include(x => x.RepairJob)!.ThenInclude(j => j!.Photos)
            .Include(x => x.RepairJob)!.ThenInclude(j => j!.TimelineEntries)
            .FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAtUtc == null && x.ExpiresAtUtc > now);

        if (access?.RepairJob == null) return null;
        access.LastAccessedAtUtc = now;
        await _db.SaveChangesAsync();
        return access.RepairJob;
    }

    public async Task RevokeAsync(int repairJobId)
    {
        var now = DateTime.UtcNow;
        var active = await _db.ClientTrackingAccesses
            .Where(x => x.RepairJobId == repairJobId && x.RevokedAtUtc == null)
            .ToListAsync();
        foreach (var access in active) access.RevokedAtUtc = now;
        await _db.SaveChangesAsync();
    }

    public async Task<ClientTrackingAccess?> GetCurrentAsync(int repairJobId) =>
        await _db.ClientTrackingAccesses
            .Where(x => x.RepairJobId == repairJobId && x.RevokedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
