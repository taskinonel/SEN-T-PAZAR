using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;
using System.ComponentModel.DataAnnotations;

namespace SEN_T_PAZAR.Controllers;

public class PricingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

    public PricingController(
        ApplicationDbContext db,
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Fiyatlandırma sayfası
    [HttpGet]
    public async Task<IActionResult> Index(string? type = null)
    {
        var query = _db.PricingPackages.Where(p => p.IsActive);
        
        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(p => p.PackageType == type);
        }
        
        var packages = await query
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
            
        // Kullanıcı giriş yapmışsa, kullanıcının paketlerini al
        List<UserPackageInfo> userPackages = new();
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var up = await _db.UserPackages
                    .Where(p => p.UserId == user.Id && p.IsActive && (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow))
                    .Include(p => p.Package)
                    .ToListAsync();
                    
                userPackages = up.Select(p => new UserPackageInfo
                {
                    PackageName = p.Package != null ? p.Package.Name : "Bilinmiyor",
                    PackageType = p.Package != null ? p.Package.PackageType : "",
                    RemainingUses = p.RemainingCount,
                    ExpiryDate = p.ExpiryDate
                }).ToList();
            }
        }
        
        ViewData["UserPackages"] = userPackages;
        ViewData["SelectedType"] = type;
        
        return View(packages);
    }

    // Paket satın alma sayfası
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Purchase(int id)
    {
        var package = await _db.PricingPackages.FindAsync(id);
        if (package == null)
        {
            return NotFound();
        }
        
        var user = await _userManager.GetUserAsync(User);
        
        // Kullanıcının bu paketten kaç tane kaldı
        UserPackage? userPackage = null;
        if (user != null)
        {
            userPackage = await _db.UserPackages
                .Where(p => p.UserId == user.Id && p.PackageId == package.Id && p.IsActive)
                .FirstOrDefaultAsync();
        }
            
        var model = new PurchaseViewModel
        {
            PackageId = package.Id,
            PackageName = package.Name,
            PackageType = package.PackageType,
            Price = package.Price,
            Currency = package.Currency,
            DurationDays = package.DurationDays,
            Description = package.Description,
            AvailableCount = userPackage != null ? userPackage.RemainingCount : 0
        };
        
        return View(model);
    }

    // Paket satın alma işlemi (simülasyon - gerçek ödeme entegrasyonu eklenebilir)
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Purchase(PurchaseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        var user = await _userManager.GetUserAsync(User);
        var package = await _db.PricingPackages.FindAsync(model.PackageId);

        if (user == null)
        {
            return Unauthorized();
        }

        if (package == null)
        {
            return NotFound();
        }
        
        // Ödeme kaydı oluştur
        var payment = new Payment
        {
            UserId = user.Id,
            PackageId = package.Id,
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = "completed", // Simülasyon - gerçekte ödeme sağlayıcıdan gelen sonuç kullanılmalı
            Amount = package.Price,
            Currency = package.Currency,
            TransactionId = "TXN-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
            CompletedAt = DateTime.UtcNow,
            Note = "Paket satın alımı: " + package.Name
        };
        
        _db.Payments.Add(payment);
        
        // Kullanıcı paketini güncelle veya oluştur
        var userPackage = await _db.UserPackages
            .Where(p => p.UserId == user.Id && p.PackageId == package.Id && p.IsActive)
            .FirstOrDefaultAsync();
            
        if (userPackage != null)
        {
            userPackage.TotalPurchased += package.ListingsIncluded;
            userPackage.UsedCount = 0; // Reset used when new purchase
            userPackage.ExpiryDate = DateTime.UtcNow.AddDays(package.DurationDays);
        }
        else
        {
            _db.UserPackages.Add(new UserPackage
            {
                UserId = user.Id,
                PackageId = package.Id,
                TotalPurchased = package.ListingsIncluded,
                ExpiryDate = DateTime.UtcNow.AddDays(package.DurationDays)
            });
        }
        
        await _db.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Paket başarıyla satın alındı: " + package.Name;
        return RedirectToAction(nameof(Index));
    }

    // İlanı öne çıkan veya vitrin yap
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> PromoteListing(int listingId)
    {
        var user = await _userManager.GetUserAsync(User);
        
        var listing = await _db.Listings.FindAsync(listingId);
        if (user == null || listing == null || listing.UserId != user.Id)
        {
            return NotFound();
        }
        
        // Kullanıcının kullanılabilir paketlerini al
        var userPackages = await _db.UserPackages
            .Where(p => p.UserId == user.Id && p.IsActive && p.RemainingCount > 0 && (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow))
            .Include(p => p.Package)
            .ToListAsync();
            
        var model = new PromoteListingViewModel
        {
            ListingId = listing.Id,
            ListingTitle = listing.Title,
            CurrentFeatured = listing.IsFeatured,
            CurrentVitrin = listing.IsVitrin,
            FeaturedExpiryDate = listing.FeaturedExpiryDate,
            VitrinExpiryDate = listing.VitrinExpiryDate,
            AvailablePackages = userPackages
                .Select(p => new PackageOptionViewModel
                {
                    PackageId = p.PackageId,
                    PackageName = p.Package != null ? p.Package.Name : "Bilinmiyor",
                    PackageType = p.Package != null ? p.Package.PackageType : "",
                    RemainingUses = p.RemainingCount,
                    ExpiryDate = p.ExpiryDate
                })
                .ToList()
        };
        
        return View(model);
    }

    // İlanı öne çıkan veya vitrin yap
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteListing(PromoteListingViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        
        var listing = await _db.Listings.FindAsync(model.ListingId);
        if (user == null || listing == null || listing.UserId != user.Id)
        {
            return NotFound();
        }
        
        var package = await _db.PricingPackages.FindAsync(model.SelectedPackageId);
        if (package == null)
        {
            ModelState.AddModelError("", "Geçersiz paket seçimi");
            return View(model);
        }
        
        var userPackage = await _db.UserPackages
            .Where(p => p.UserId == user.Id && p.PackageId == package.Id && p.IsActive && p.RemainingCount > 0)
            .FirstOrDefaultAsync();
            
        if (userPackage == null)
        {
            ModelState.AddModelError("", "Bu paket için yeterli kullanım hakkınız yok");
            return View(model);
        }
        
        // Paketi kullan
        userPackage.UsedCount++;
        
        // İlanı güncelle
        if (package.PackageType == "featured" || package.PackageType == "combo")
        {
            listing.IsFeatured = true;
            listing.FeaturedExpiryDate = DateTime.UtcNow.AddDays(package.DurationDays);
            listing.FeaturedPackage = package.Tier;
        }
        
        if (package.PackageType == "vitrin" || package.PackageType == "combo")
        {
            listing.IsVitrin = true;
            listing.VitrinExpiryDate = DateTime.UtcNow.AddDays(package.DurationDays);
            listing.VitrinPackage = package.Tier;
        }
        
        // İlan promosyon kaydı oluştur
        _db.ListingPromotions.Add(new ListingPromotion
        {
            ListingId = listing.Id,
            UserId = user.Id,
            PromotionType = package.PackageType,
            PackageName = package.Name,
            DurationDays = package.DurationDays,
            ExpiresAt = DateTime.UtcNow.AddDays(package.DurationDays),
            PaymentId = null // Paket kullanımı olduğu için doğrudan ödeme ID yok
        });
        
        await _db.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "İlanınız başarıyla " + package.Name + " paketi ile güçlendirildi!";
        return RedirectToAction("Dashboard", "Account", new { tab = "listings" });
    }
}

// ViewModels
public class PurchaseViewModel
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string PackageType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TL";
    public int DurationDays { get; set; }
    public string? Description { get; set; }
    public int AvailableCount { get; set; }
    
    [Required(ErrorMessage = "Ödeme yöntemi seçiniz")]
    public string PaymentMethod { get; set; } = "credit_card";
}

public class PromoteListingViewModel
{
    public int ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public bool CurrentFeatured { get; set; }
    public bool CurrentVitrin { get; set; }
    public DateTime? FeaturedExpiryDate { get; set; }
    public DateTime? VitrinExpiryDate { get; set; }
    public List<PackageOptionViewModel> AvailablePackages { get; set; } = new();
    
    [Required(ErrorMessage = "Lütfen bir paket seçiniz")]
    public int SelectedPackageId { get; set; }
}

public class PackageOptionViewModel
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string PackageType { get; set; } = string.Empty;
    public int RemainingUses { get; set; }
    public DateTime? ExpiryDate { get; set; }
}