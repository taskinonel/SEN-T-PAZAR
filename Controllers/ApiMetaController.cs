using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SEN_T_PAZAR.Models;
using SEN_T_PAZAR.Services;

namespace SEN_T_PAZAR.Controllers;

[ApiController]
[Route("api")]
[Route("api/v1")]
public sealed class ApiMetaController : ControllerBase
{
    private readonly IListingCatalogService _catalog;
    private readonly ApplicationDbContext _db;

    public ApiMetaController(IListingCatalogService catalog, ApplicationDbContext db)
    {
        _catalog = catalog;
        _db = db;
    }

    [HttpGet("Categories")]
    public IActionResult GetCategories()
    {
        var language = GetLanguageCode();
        var localizer = BuildLocalizer(language);

        var categories = _catalog.Categories
            .Where(x => !x.Equals("all", StringComparison.OrdinalIgnoreCase))
            .Select(x => new MobileCategoryDto
            {
                Code = x,
                Name = localizer.CategoryLabel(x),
                Slug = localizer.CategorySlug(x)
            })
            .ToList();

        if (TryHandleEtag(categories))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return Ok(categories);
    }

    [HttpGet("Locations")]
    public IActionResult GetLocations()
    {
        var data = _db.Listings
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.City))
            .Select(x => new { x.City, x.District })
            .AsEnumerable()
            .GroupBy(x => x.City!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new MobileLocationDto
            {
                City = g.Key,
                Districts = g
                    .Select(x => x.District)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList()
            })
            .OrderBy(x => x.City)
            .ToList();

        if (data.Count == 0)
        {
            data = _catalog.Cities
                .Where(x => !x.Equals("all", StringComparison.OrdinalIgnoreCase))
                .Select(x => new MobileLocationDto { City = x, Districts = new List<string>() })
                .ToList();
        }

        if (TryHandleEtag(data))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return Ok(data);
    }

    private bool TryHandleEtag<T>(T payload)
    {
        var raw = System.Text.Json.JsonSerializer.Serialize(payload);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var etag = $"W/\"{hash}\"";

        Response.Headers.ETag = etag;
        var incoming = Request.Headers.IfNoneMatch.ToString();
        return !string.IsNullOrWhiteSpace(incoming) && string.Equals(incoming, etag, StringComparison.Ordinal);
    }

    private static string GetLanguageCodeFromAcceptLanguage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "tr";
        }

        var part = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "tr";
        var code = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "tr";
        return code.ToLowerInvariant() switch
        {
            "en" => "en",
            "ru" => "ru",
            "ar" => "ar",
            "fa" => "fa",
            _ => "tr"
        };
    }

    private string GetLanguageCode()
    {
        return GetLanguageCodeFromAcceptLanguage(Request.Headers.AcceptLanguage.ToString());
    }

    private SiteLocalizer BuildLocalizer(string language)
    {
        var current = Thread.CurrentThread.CurrentUICulture;
        Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(language);
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(language);
        try
        {
            var accessor = HttpContext.RequestServices.GetRequiredService<IHttpContextAccessor>();
            return new SiteLocalizer(accessor);
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = current;
            Thread.CurrentThread.CurrentCulture = current;
        }
    }
}
