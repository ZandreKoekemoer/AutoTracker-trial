namespace AutoTracker.Services;

public static class UserRolePolicy
{
    public static readonly string[] AllowedRoles =
    [
        "Administrator", "Workshop Manager", "Receptionist", "Technician"
    ];

    public static bool IsAllowedRole(string? role) => AllowedRoles.Contains(role ?? "", StringComparer.OrdinalIgnoreCase);

    public static bool CanManageRole(string actorRole, string targetRole, string requestedRole)
    {
        if (!IsAllowedRole(requestedRole)) return false;
        if (string.Equals(actorRole, "Administrator", StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.Equals(actorRole, "Workshop Manager", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(targetRole, "Administrator", StringComparison.OrdinalIgnoreCase)) return false;
        return requestedRole is "Receptionist" or "Technician";
    }
}
