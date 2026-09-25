using AutoTracker.Models;

namespace AutoTracker.ViewModels;

public class NotificationViewModel
{
    public List<NotificationItem> Items { get; set; } = new();
}

public class NotificationItem
{
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "Info";
    public int? RepairJobId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}