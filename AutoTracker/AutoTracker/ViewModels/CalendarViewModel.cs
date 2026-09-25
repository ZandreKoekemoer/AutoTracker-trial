using AutoTracker.Models;

namespace AutoTracker.ViewModels;

public class CalendarViewModel
{
    public DateTime CurrentMonth { get; set; } = DateTime.Today;
    public List<RepairJob> Jobs { get; set; } = new();
    public List<CalendarEvent> Events { get; set; } = new();
}
