using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Controllers;

[ApiController]
[Route("api/Showcase")]
[Route("api/v1/Showcase")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ApiShowcaseController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ApiShowcaseController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetShowcase()
    {
        var nowUtc = DateTime.UtcNow;

        var rows = await _db.Listings
            .AsNoTracking()
            .Where(x => x.IsApproved && !x.IsClosed)
            .Where(x =>
                (x.IsVitrin && (x.VitrinExpiryDate == null || x.VitrinExpiryDate > nowUtc)) ||
                (x.IsFeatured && (x.FeaturedExpiryDate == null || x.FeaturedExpiryDate > nowUtc)))
            .OrderByDescending(x => x.IsVitrin)
            .ThenByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.CreatedAt)
            .Take(30)
            .Select(x => new
            {
                Id = x.Id,
                Title = x.Title,
                Price = x.PriceAmount,
                Location = string.IsNullOrWhiteSpace(x.District) ? x.City : x.City + ", " + x.District,
                ImagePath = _db.ListingImages
                    .Where(i => i.ListingId == x.Id)
                    .OrderBy(i => i.Id)
                    .Select(i => i.FilePath)
                    .FirstOrDefault(),
                IsSponsored = true
            })
            .ToListAsync();

        var items = rows.Select(x => new MobileAdListItemDto
        {
            Id = x.Id,
            Title = x.Title,
            Price = x.Price,
            Location = x.Location,
            ImageUrl = AbsoluteImageUrl(x.ImagePath),
            IsSponsored = x.IsSponsored
        }).ToList();

        return Ok(items);
    }

    private string AbsoluteImageUrl(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return $"{Request.Scheme}://{Request.Host}/img/logo.png";
        }

        if (Uri.TryCreate(rawPath, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        var normalized = rawPath.StartsWith('/') ? rawPath : "/" + rawPath;
        return $"{Request.Scheme}://{Request.Host}{normalized}";
    }
}
