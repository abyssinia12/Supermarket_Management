using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Models;
using SuperMarket.Data; // Ensure this matches your actual DbContext namespace

namespace SuperMarket.Controllers;

public class LocationsController : Controller
{
    private readonly SupermarketDbContext _context;

    public LocationsController(SupermarketDbContext context)
    {
        _context = context;
    }

    // GET: Locations
    public async Task<IActionResult> Index()
    {
        var locations = await _context.Locations
            .Where(l => !l.IsDeleted)
            .ToListAsync();
        return View(locations);
    }

    // POST: Locations/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string locationName, string locationType)
    {
        if (!string.IsNullOrWhiteSpace(locationName) && !string.IsNullOrWhiteSpace(locationType))
        {
            var location = new Location
            {
                LocationName = locationName,
                LocationType = locationType,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Location created successfully.";
        }
        else
        {
            TempData["Error"] = "Please fill in all required fields.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Locations/ToggleStatus
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var location = await _context.Locations.FindAsync(id);
        if (location != null)
        {
            location.IsActive = !(location.IsActive ?? true);
            location.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Location status updated.";
        }
        return RedirectToAction(nameof(Index));
    }
}