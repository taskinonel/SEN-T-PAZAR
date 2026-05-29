using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Controllers;

public class ListingCompareController : Controller
{
    private readonly ApplicationDbContext _context;

    public ListingCompareController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string ids)
    {
        if (string.IsNullOrEmpty(ids))
        {
            return View(new List<Listing>());
        }

        var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.TryParse(id, out var i) ? i : 0)
                        .Where(i => i > 0)
                        .Take(4) // Max 4 listings for comparison
                        .ToList();

        var listings = await _context.Listings
            .Include(l => l.Images)
            .Where(l => idList.Contains(l.Id))
            .ToListAsync();

        // Maintain original order
        var sortedListings = idList
            .Select(id => listings.FirstOrDefault(l => l.Id == id))
            .Where(l => l != null)
            .ToList();

        return View(sortedListings!);
    }
}
