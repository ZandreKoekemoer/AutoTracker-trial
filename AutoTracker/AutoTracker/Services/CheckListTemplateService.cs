using AutoTracker.Data;
using AutoTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Services;

public class ChecklistTemplateService
{
    private readonly AppDbContext _db;

    public ChecklistTemplateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureChecklistItemsAsync(int repairJobId)
    {
        if (await _db.ChecklistItems.AnyAsync(x => x.RepairJobId == repairJobId))
            return;

        var items = new List<ChecklistItem>();

        void Add(string section, params string[] names)
        {
            foreach (var name in names)
            {
                items.Add(new ChecklistItem
                {
                    RepairJobId = repairJobId,
                    Section = section,
                    ItemName = name
                });
            }
        }

        Add("Vehicle Info", "Fuel Level", "Mileage", "Colour", "Licence Disc", "VIN");
        Add("Doors", "Left Front Door", "Right Front Door", "Left Rear Door", "Right Rear Door", "Sliding Door", "Tailgate / Bootlid");
        Add("Panels", "Bonnet", "Roof", "Left Front Fender", "Right Front Fender", "Left Rear Quarter", "Right Rear Quarter");
        Add("Bumpers", "Front Bumper", "Rear Bumper", "Grille");
        Add("Lights", "Left Headlight", "Right Headlight", "Left Taillight", "Right Taillight", "Indicators", "Fog Lights");
        Add("Glass", "Windscreen", "Rear Window", "Left Front Window", "Right Front Window", "Left Rear Window", "Right Rear Window");
        Add("Mirrors", "Left Mirror", "Right Mirror", "Rear View Mirror");
        Add("Tyres", "Left Front Tyre", "Right Front Tyre", "Left Rear Tyre", "Right Rear Tyre", "Spare Wheel");
        Add("Electrical", "Battery", "Radio", "CD Player", "Amplifier", "Speakers", "Alarm", "Horn");
        Add("Interior", "Dashboard", "Seats", "Seat Covers", "Floor Mats", "Boot Mat", "Tools", "Jack");
        Add("Accessories", "Hubcaps / Mags", "Mudflaps", "Towbar", "Canopy", "Roof Racks");
        Add("Customer Items", "Personal Belongings", "Documents", "Keys", "Remote");
        Add("Final QC", "Panel Gaps", "Paint Match", "Wash Completed", "Road Test", "Client Notified");

        _db.ChecklistItems.AddRange(items);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateChecklistItemAsync(int id, string condition, string notes)
    {
        var item = await _db.ChecklistItems.FindAsync(id);
        if (item == null) return;

        item.Condition = condition;
        item.Notes = notes;
        item.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsCompleteAsync(int repairJobId)
    {
        var items = await _db.ChecklistItems
            .Where(x => x.RepairJobId == repairJobId)
            .ToListAsync();

        return items.Any() && items.All(x => x.Condition != "Not Checked");
    }
}