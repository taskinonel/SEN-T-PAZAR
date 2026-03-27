using Microsoft.AspNetCore.Mvc;
using SEN_T_PAZAR.Models;
using System.Linq;

namespace SEN_T_PAZAR.Controllers;

using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Listings()
    {
        var listings = _context.Listings.OrderByDescending(x => x.CreatedAt).ToList();
        return View(listings);
    }

    [HttpPost]
    public IActionResult Approve(int id)
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null && !listing.IsApproved)
        {
            listing.IsApproved = true;
            _context.SaveChanges();
        }
        return RedirectToAction("Listings");
    }
}
