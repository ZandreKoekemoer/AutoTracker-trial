using AutoTracker.Models;

namespace AutoTracker.ViewModels;

public class DashboardViewModel
{
    public int TotalActiveJobs { get; set; }
    public int WaitingApproval { get; set; }
    public int ReadyForCollection { get; set; }
    public int OverdueJobs { get; set; }
    public int MissingDocuments { get; set; }
    public int CollectedThisMonth { get; set; }
    public decimal MonthlyProfit { get; set; }

    public List<RepairJob> RecentJobs { get; set; } = new();
    public List<RepairJob> JobsWithMissingRequirements { get; set; } = new();
    public List<JobAuditLog> RecentActivity { get; set; } = new();
}