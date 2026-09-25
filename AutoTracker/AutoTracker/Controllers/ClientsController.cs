using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager", "Receptionist")]
public class ClientsController : Controller
{
    private readonly AppDbContext _db;
    public ClientsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Clients.Include(c => c.Vehicles).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.FullName.Contains(search) || c.Phone.Contains(search) || c.Email.Contains(search));
        return View(await query.OrderBy(c => c.FullName).ToListAsync());
    }

    public IActionResult Create() => View(new Client());

    [HttpPost]
    public async Task<IActionResult> Create(Client client)
    {
        if (!ModelState.IsValid) return View(client);
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var client = await _db.Clients.Include(c => c.Vehicles).ThenInclude(v => v.RepairJobs).FirstOrDefaultAsync(c => c.Id == id);
        return client is null ? NotFound() : View(client);
    }
}
