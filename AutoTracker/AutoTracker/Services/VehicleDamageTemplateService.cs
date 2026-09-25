using AutoTracker.Data;
using AutoTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Services;

public class VehicleDamageTemplateService
{
    private readonly AppDbContext _db;

    public VehicleDamageTemplateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureDamageItemsAsync(int repairJobId)
    {
        if (await _db.VehicleDamageItems.AnyAsync(x => x.RepairJobId == repairJobId))
            return;

        var items = new List<VehicleDamageItem>();

        void Add(string view, params string[] parts)
        {
            foreach (var part in parts)
            {
                items.Add(new VehicleDamageItem
                {
                    RepairJobId = repairJobId,
                    ViewSide = view,
                    BodyPart = part,
                    Condition = "Not Inspected",
                    RepairType = "None"
                });
            }
        }

        Add("Front", "Bonnet", "Front Bumper", "Front Grille", "Left Headlight", "Right Headlight", "Left Front Fender", "Right Front Fender");
        Add("Left Side", "Left Front Door", "Left Rear Door", "Left Sill", "Left Mirror", "Left Quarter Panel", "Left Front Tyre", "Left Rear Tyre");
        Add("Right Side", "Right Front Door", "Right Rear Door", "Right Sill", "Right Mirror", "Right Quarter Panel", "Right Front Tyre", "Right Rear Tyre");
        Add("Rear", "Boot / Tailgate", "Rear Bumper", "Left Taillight", "Right Taillight", "Rear Window");
        Add("Top", "Roof", "Sunroof", "Windscreen");
        Add("Interior", "Dashboard", "Seats", "Carpets", "Boot Interior", "Accessories", "Customer Belongings");

        _db.VehicleDamageItems.AddRange(items);
        await _db.SaveChangesAsync();
    }
}