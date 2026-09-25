namespace AutoTracker.ViewModels;

public class ReportsViewModel
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int OverdueJobs { get; set; }

    public decimal MonthlyTurnover { get; set; }
    public decimal MonthlyProfit { get; set; }
    public decimal MonthlyCosts { get; set; }
    public double AverageRepairDays { get; set; }

    public List<StatusReportItem> JobsByStatus { get; set; } = new();
}

public class StatusReportItem
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}