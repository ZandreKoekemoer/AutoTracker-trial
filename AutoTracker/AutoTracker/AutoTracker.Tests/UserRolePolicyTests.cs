using AutoTracker.Services;

namespace AutoTracker.Tests;

public class UserRolePolicyTests
{
    [Theory]
    [InlineData("Administrator", "Technician", "Administrator", true)]
    [InlineData("Workshop Manager", "Technician", "Receptionist", true)]
    [InlineData("Workshop Manager", "Technician", "Workshop Manager", false)]
    [InlineData("Workshop Manager", "Administrator", "Technician", false)]
    [InlineData("Receptionist", "Technician", "Receptionist", false)]
    public void CanManageRole_EnforcesPrivilegeBoundaries(string actorRole, string targetRole, string requestedRole, bool expected)
    {
        Assert.Equal(expected, UserRolePolicy.CanManageRole(actorRole, targetRole, requestedRole));
    }
}
