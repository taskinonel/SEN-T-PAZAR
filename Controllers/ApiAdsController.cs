using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SEN_T_PAZAR.Models;
using SEN_T_PAZAR.Services;

namespace SEN_T_PAZAR.Controllers;

[ApiController]
[Route("api/Ads")]
[Route("api/v1/Ads")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ApiAdsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITextTranslationService _translationService;
    private readonly IUploadStorageService _uploadStorage;

    public ApiAdsController(ApplicationDbContext db, ITextTranslationService translationService, IUploadStorageService uploadStorage)
    {
        _db = db;
        _translationService = translationService;
        _uploadStorage = uploadStorage;
    }

    [HttpPost("Upload")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { error = "Yüklenecek dosya bulunamadı." });
        }

        var uploadsRoot = _uploadStorage.EnsureDirectory();

        var urls = new List<string>();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        foreach (var file in files.Take(20))
        {
            if (file.Length <= 0)
            {
                continue;
            }

            if (file.Length > 15 * 1024 * 1024)
            {
                continue;
            }

            var ext = Path.GetExtension(file.FileName);
            if (!allowed.Contains(ext))
            {
                continue;
            }

            var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var fullPath = Path.Combine(uploadsRoot, fileName);
            await using var fs = System.IO.File.Create(fullPath);
            await file.CopyToAsync(fs);

            var url = $"{Request.Scheme}://{Request.Host}{_uploadStorage.GetPublicDirectory()}/{fileName}";
            urls.Add(url);
        }

        return Ok(new { urls });
    }

    [HttpGet]
    public async Task<IActionResult> GetAds(
        [FromQuery] string? category = null,
        [FromQuery] string? query = null,
        [FromQuery] string? listingType = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int page = 1)
    {
        var language = GetLanguageCode();
        var pageSize = 20;
        page = Math.Max(1, page);

        var q = _db.Listings
            .AsNoTracking()
            .Where(x => x.IsApproved && !x.IsClosed);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var c = category.Trim().ToLowerInvariant();
            q = q.Where(x => x.Category.ToLower() == c || (x.SubCategory != null && x.SubCategory.ToLower() == c));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.Description.ToLower().Contains(term) ||
                x.City.ToLower().Contains(term) ||
                x.District.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(listingType))
        {
            var lt = listingType.Trim().ToLowerInvariant();
            q = q.Where(x => x.Type.ToLower().Contains(lt));
        }

        if (minPrice.HasValue)
        {
            q = q.Where(x => x.PriceAmount >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            q = q.Where(x => x.PriceAmount <= maxPrice.Value);
        }

        var items = await q
            .OrderByDescending(x => x.IsVitrin)
            .ThenByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MobileAdListItemDto
            {
                Id = x.Id,
                Title = language == "en" ? (x.TitleEn ?? x.Title)
                    : language == "ru" ? (x.TitleRu ?? x.Title)
                    : language == "ar" ? (x.TitleAr ?? x.Title)
                    : language == "fa" ? (x.TitleFa ?? x.Title)
                    : x.Title,
                Price = x.PriceAmount,
                Location = x.City + ", " + x.District,
                ImageUrl = AbsoluteImageUrl(_db.ListingImages
                    .Where(i => i.ListingId == x.Id)
                    .OrderBy(i => i.Id)
                    .Select(i => i.FilePath)
                    .FirstOrDefault()),
                IsSponsored = x.IsFeatured || x.IsVitrin
            })
            .ToListAsync();

        if (TryHandleEtag(items))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return Ok(items);
    }

    [AllowAnonymous]
    [HttpGet("Suggest")]
    public async Task<IActionResult> Suggest([FromQuery] string? q = null)
    {
        var term = (q ?? string.Empty).Trim();
        if (term.Length < 2)
        {
            return Ok(Array.Empty<string>());
        }

        var lowered = term.ToLowerInvariant();
        var suggestions = await _db.Listings
            .AsNoTracking()
            .Where(x => x.IsApproved && !x.IsClosed &&
                (x.Title.ToLower().Contains(lowered) ||
                 x.Description.ToLower().Contains(lowered) ||
                 x.City.ToLower().Contains(lowered) ||
                 x.District.ToLower().Contains(lowered)))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Title)
            .Distinct()
            .Take(10)
            .ToListAsync();

        return Ok(suggestions);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAdDetails(int id)
    {
        var language = GetLanguageCode();
        var ad = await _db.Listings
            .AsNoTracking()
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsApproved && !x.IsClosed);

        if (ad == null)
        {
            return NotFound(new { error = "İlan bulunamadı." });
        }

        var dto = new MobileAdDetailsDto
        {
            Id = ad.Id,
            Title = language == "en" ? (ad.TitleEn ?? ad.Title)
                : language == "ru" ? (ad.TitleRu ?? ad.Title)
                : language == "ar" ? (ad.TitleAr ?? ad.Title)
                : language == "fa" ? (ad.TitleFa ?? ad.Title)
                : ad.Title,
            Description = language == "en" ? (ad.DescriptionEn ?? ad.Description)
                : language == "ru" ? (ad.DescriptionRu ?? ad.Description)
                : language == "ar" ? (ad.DescriptionAr ?? ad.Description)
                : language == "fa" ? (ad.DescriptionFa ?? ad.Description)
                : ad.Description,
            Price = ad.PriceAmount,
            PriceCurrency = ad.PriceCurrency,
            Category = ad.Category,
            ListingType = ad.Type,
            Location = string.IsNullOrWhiteSpace(ad.District) ? ad.City : ad.City + ", " + ad.District,
            IsSponsored = ad.IsFeatured || ad.IsVitrin,
            ImageUrls = ad.Images.OrderBy(i => i.Id).Select(i => AbsoluteImageUrl(i.FilePath)).ToList(),
            Seller = new MobileAdSellerDto
            {
                Name = ad.FullName,
                Phone = ad.Phone,
                AllowWhatsApp = ad.AllowWhatsApp,
                AllowMessages = ad.AllowMessages
            }
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAd([FromBody] ApiListingUpsertRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Kullanıcı doğrulanamadı." });
        }

        var urls = (request.ImageUrls ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .Select(x => x.Trim())
            .ToList();

        var imageList = new List<ListingImage>();
        foreach (var u in urls)
        {
            imageList.Add(new ListingImage { FilePath = u, UserId = userId });
        }

        var listing = new Listing
        {
            UserId = userId,
            FullName = (request.FullName ?? string.Empty).Trim(),
            Phone = (request.Phone ?? string.Empty).Trim(),
            AllowWhatsApp = request.AllowWhatsApp,
            AllowMessages = request.AllowMessages,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            SubCategory = string.IsNullOrWhiteSpace(request.SubCategory) ? null : request.SubCategory.Trim(),
            Type = request.Type.Trim(),
            City = request.City.Trim(),
            District = string.IsNullOrWhiteSpace(request.District) ? string.Empty : request.District.Trim(),
            Neighborhood = string.IsNullOrWhiteSpace(request.Neighborhood) ? null : request.Neighborhood.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            PriceAmount = request.PriceAmount,
            PriceCurrency = string.IsNullOrWhiteSpace(request.PriceCurrency) ? "TL" : request.PriceCurrency.Trim(),
            PriceType = string.IsNullOrWhiteSpace(request.PriceType) ? "Total" : request.PriceType.Trim(),
            PriceDescription = string.IsNullOrWhiteSpace(request.PriceDescription) ? null : request.PriceDescription.Trim(),
            Negotiable = request.Negotiable,
            TradeIn = request.TradeIn,
            AdvertiserType = string.IsNullOrWhiteSpace(request.AdvertiserType) ? "Owner" : request.AdvertiserType.Trim(),
            VideoUrl = string.IsNullOrWhiteSpace(request.VideoUrl) ? null : request.VideoUrl.Trim(),
            Tour360Url = string.IsNullOrWhiteSpace(request.Tour360Url) ? null : request.Tour360Url.Trim(),
            Has360Tour = !string.IsNullOrWhiteSpace(request.Tour360Url),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAt = DateTime.UtcNow,
            IsApproved = false,
            IsClosed = false,
            DealStatus = "open",
            Images = imageList
        };

            await PopulateListingTranslationsAsync(listing);

        _db.Listings.Add(listing);
        await _db.SaveChangesAsync();

        return Ok(new { id = listing.Id, status = "pending" });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAd(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Kullanıcı doğrulanamadı." });
        }

        var ad = await _db.Listings.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (ad == null)
        {
            return NotFound(new { error = "İlan bulunamadı." });
        }

        if (!string.Equals(ad.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        _db.ListingImages.RemoveRange(ad.Images);
        _db.Listings.Remove(ad);
        await _db.SaveChangesAsync();
        return NoContent();
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

    private bool TryHandleEtag<T>(T payload)
    {
        var raw = System.Text.Json.JsonSerializer.Serialize(payload);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var etag = $"W/\"{hash}\"";
        Response.Headers.ETag = etag;

        var incoming = Request.Headers.IfNoneMatch.ToString();
        return !string.IsNullOrWhiteSpace(incoming) && string.Equals(incoming, etag, StringComparison.Ordinal);
    }

    private string GetLanguageCode()
    {
        var header = Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return "tr";
        }

        var lang = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "tr";
        var code = lang.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "tr";
        return code.ToLowerInvariant() switch
        {
            "en" => "en",
            "ru" => "ru",
            "ar" => "ar",
            "fa" => "fa",
            _ => "tr"
        };
    }

    private async Task PopulateListingTranslationsAsync(Listing listing)
    {
        var titleSource = listing.Title?.Trim() ?? string.Empty;
        var descriptionSource = listing.Description?.Trim() ?? string.Empty;

        listing.TitleEn = await TranslateMissingAsync(listing.TitleEn, titleSource, "en");
        listing.TitleRu = await TranslateMissingAsync(listing.TitleRu, titleSource, "ru");
        listing.TitleAr = await TranslateMissingAsync(listing.TitleAr, titleSource, "ar");
        listing.TitleFa = await TranslateMissingAsync(listing.TitleFa, titleSource, "fa");
        listing.DescriptionEn = await TranslateMissingAsync(listing.DescriptionEn, descriptionSource, "en");
        listing.DescriptionRu = await TranslateMissingAsync(listing.DescriptionRu, descriptionSource, "ru");
        listing.DescriptionAr = await TranslateMissingAsync(listing.DescriptionAr, descriptionSource, "ar");
        listing.DescriptionFa = await TranslateMissingAsync(listing.DescriptionFa, descriptionSource, "fa");
    }

    private async Task<string?> TranslateMissingAsync(string? currentValue, string sourceText, string targetLanguage)
    {
        if (!string.IsNullOrWhiteSpace(currentValue) || string.IsNullOrWhiteSpace(sourceText))
        {
            return currentValue;
        }

        return await _translationService.TranslateAsync(sourceText, targetLanguage, "auto");
    }
}
