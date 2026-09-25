using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager", "Receptionist")]
public class VehiclesController : Controller
{
    private readonly AppDbContext _db;
    public VehiclesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Vehicles.Include(v => v.Client).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.RegNumber.Contains(search) || v.Make.Contains(search) || v.ModelName.Contains(search));
        return View(await query.OrderBy(v => v.RegNumber).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await LoadClients();
        return View(new Vehicle());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Vehicle vehicle)
    {
        if (!ModelState.IsValid) { await LoadClients(); return View(vehicle); }
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadClients() => ViewBag.ClientId = new SelectList(await _db.Clients.OrderBy(c => c.FullName).ToListAsync(), "Id", "FullName");
}
