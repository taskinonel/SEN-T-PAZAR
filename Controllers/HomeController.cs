using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SEN_T_PAZAR.Models;
using SEN_T_PAZAR.Services;

namespace SEN_T_PAZAR.Controllers;

public class HomeController : Controller
{
    private readonly IListingCatalogService _catalog;
    private readonly ApplicationDbContext _context;
    private readonly EmailSender _emailSender;

    public HomeController(IListingCatalogService catalog, ApplicationDbContext context, EmailSender emailSender)
    {
        _catalog = catalog;
        _context = context;
        _emailSender = emailSender;
    }

    public IActionResult Index(
        string listingType = "all",
        string city = "all",
        string category = "all",
        string priceRange = "any",
        string keyword = "",
        string sortBy = "latest")
    {
        if (!string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
        {
            var slug = _catalog.GetDefaultSlug(category);
            return RedirectToAction(nameof(Category), new { slug, listingType, city, priceRange, keyword, sortBy });
        }

        return BuildListingPage(listingType, city, "all", priceRange, keyword, sortBy, isCategoryPage: false, currentCategorySlug: string.Empty);
    }

    public IActionResult Category(
        string slug,
        string listingType = "all",
        string city = "all",
        string priceRange = "any",
        string keyword = "",
        string sortBy = "latest")
    {
        if (!_catalog.TryResolveCategoryFromSlug(slug, out var categoryCode))
        {
            return NotFound();
        }

        return BuildListingPage(listingType, city, categoryCode, priceRange, keyword, sortBy, isCategoryPage: true, currentCategorySlug: slug);
    }

    private IActionResult BuildListingPage(
        string listingType,
        string city,
        string category,
        string priceRange,
        string keyword,
        string sortBy,
        bool isCategoryPage,
        string currentCategorySlug)
    {
        listingType = _catalog.ListingTypes.Contains(listingType) ? listingType : "all";
        city = _catalog.Cities.Contains(city) ? city : "all";
        category = _catalog.Categories.Contains(category) ? category : "all";
        priceRange = _catalog.PriceRanges.Contains(priceRange) ? priceRange : "any";
        sortBy = _catalog.SortOptions.Contains(sortBy) ? sortBy : "latest";

        var filtered = ApplySorting(
            ApplyFilters(_catalog.Listings, listingType, city, category, priceRange, keyword),
            sortBy).ToList();

        var model = new HomePageViewModel
        {
            HeroTitle = "Aradığın ürünü veya mülkü tek yerde bul",
            HeroSubtitle = "Satılabilen ve kiralanabilen her şey için yeni nesil pazar yeri",
            FeaturedListings = filtered.Take(18).ToList(),
            PopularRegions = ["Girne", "İskele", "Lefkoşa", "Gazimağusa", "Karpaz", "Güzelyurt"],
            RegionSpots =
            [
                new RegionSpot { Name = "Lefkoşa", ListingCount = _catalog.Listings.Count(x => x.City == "Lefkoşa"), ImageUrl = "https://images.unsplash.com/photo-1467269204594-9661b134dd2b?auto=format&fit=crop&w=480&q=80" },
                new RegionSpot { Name = "Girne", ListingCount = _catalog.Listings.Count(x => x.City == "Girne"), ImageUrl = "https://images.unsplash.com/photo-1464278533981-50106e6176b1?auto=format&fit=crop&w=480&q=80" },
                new RegionSpot { Name = "Gazimağusa", ListingCount = _catalog.Listings.Count(x => x.City == "Gazimağusa"), ImageUrl = "https://images.unsplash.com/photo-1505765050516-f72dcac9c60e?auto=format&fit=crop&w=480&q=80" },
                new RegionSpot { Name = "Güzelyurt", ListingCount = _catalog.Listings.Count(x => x.City == "Güzelyurt"), ImageUrl = "https://images.unsplash.com/photo-1494526585095-c41746248156?auto=format&fit=crop&w=480&q=80" },
                new RegionSpot { Name = "İskele", ListingCount = _catalog.Listings.Count(x => x.City == "İskele"), ImageUrl = "https://images.unsplash.com/photo-1494526585095-c41746248156?auto=format&fit=crop&w=480&q=80" },
                new RegionSpot { Name = "Karpaz", ListingCount = _catalog.Listings.Count(x => x.City == "Karpaz"), ImageUrl = "https://images.unsplash.com/photo-1505691938895-1758d7feb511?auto=format&fit=crop&w=480&q=80" }
            ],
            // 5 kategori için 12'şer ProjectCard
            FeaturedEmlak = _catalog.Listings.Where(x => x.Category == "realestate").Take(12).Select((x, i) => new ProjectCard {
                Id = x.Id,
                Name = x.Title,
                Location = x.City,
                Company = "",
                DeliveryDate = "",
                PriceFrom = x.PriceLabel,
                ImageUrl = x.ImageUrl,
                Description = ""
            }).ToList(),
            FeaturedVasita = _catalog.Listings.Where(x => x.Category == "vehicle").Take(12).Select((x, i) => new ProjectCard {
                Id = x.Id,
                Name = x.Title,
                Location = x.City,
                Company = "",
                DeliveryDate = "",
                PriceFrom = x.PriceLabel,
                ImageUrl = x.ImageUrl,
                Description = ""
            }).ToList(),
            FeaturedElektronik = _catalog.Listings.Where(x => x.Category == "electronics").Take(12).Select((x, i) => new ProjectCard {
                Id = x.Id,
                Name = x.Title,
                Location = x.City,
                Company = "",
                DeliveryDate = "",
                PriceFrom = x.PriceLabel,
                ImageUrl = x.ImageUrl,
                Description = ""
            }).ToList(),
            FeaturedEvEsya = _catalog.Listings.Where(x => x.Category == "home").Take(12).Select((x, i) => new ProjectCard {
                Id = x.Id,
                Name = x.Title,
                Location = x.City,
                Company = "",
                DeliveryDate = "",
                PriceFrom = x.PriceLabel,
                ImageUrl = x.ImageUrl,
                Description = ""
            }).ToList(),
            FeaturedHizmet = _catalog.Listings.Where(x => x.Category == "services").Take(12).Select((x, i) => new ProjectCard {
                Id = x.Id,
                Name = x.Title,
                Location = x.City,
                Company = "",
                DeliveryDate = "",
                PriceFrom = x.PriceLabel,
                ImageUrl = x.ImageUrl,
                Description = ""
            }).ToList(),
            PartnerNames = ["Nova", "Apex", "BlueLine", "PrimeArc", "Kuzey Yapım", "Westland"],
            MarketCategories = _catalog.Categories
                .Where(x => x != "all")
                .Select((code, index) => new MarketCategory
                {
                    Title = code,
                    Count = _catalog.Listings.Count(x => x.Category == code),
                    AccentColor = index % 2 == 0 ? "#3d7bd8" : "#4f89a8"
                })
                .ToList(),
            MarketTiles = _catalog.Listings
                .OrderByDescending(x => x.Id)
                .Take(18)
                .Select(x => new MarketTile
                {
                    ListingId = x.Id,
                    ImageUrl = x.ImageUrl,
                    Label = x.Title,
                    PriceLabel = x.PriceLabel
                })
                .ToList(),
            ListingTypeOptions = _catalog.ListingTypes.ToList(),
            CityOptions = _catalog.Cities.ToList(),
            CategoryOptions = _catalog.Categories.ToList(),
            PriceRangeOptions = _catalog.PriceRanges.ToList(),
            SortOptions = _catalog.SortOptions.ToList(),
            ListingType = listingType,
            City = city,
            Category = category,
            PriceRange = priceRange,
            Keyword = keyword,
            SortBy = sortBy,
            IsCategoryPage = isCategoryPage,
            CurrentCategorySlug = currentCategorySlug,
            CategoryHeroImage = isCategoryPage ? _catalog.GetCategoryHeroImage(category) : string.Empty,
            TotalCount = isCategoryPage
                ? _catalog.Listings.Count(x => x.Category == category)
                : _catalog.Listings.Count,
            FilteredCount = filtered.Count
        };

        return View("Index", model);
    }

    public IActionResult Details(int id)
    {
        var listing = _catalog.Listings.FirstOrDefault(x => x.Id == id);
        if (listing is null)
        {
            return NotFound();
        }

        return View(listing);
    }

    [HttpGet]
    public IActionResult Publish()
    {
        ViewData["PublishCities"] = _catalog.Cities.Where(x => x != "all").ToList();
        ViewData["PublishTypes"] = _catalog.ListingTypes.Where(x => x != "all").ToList();
        ViewData["PublishCategories"] = _catalog.Categories.Where(x => x != "all").ToList();
        return View(new CreateListingViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Publish(CreateListingViewModel model)
    {
        ViewData["PublishCities"] = _catalog.Cities.Where(x => x != "all").ToList();
        ViewData["PublishTypes"] = _catalog.ListingTypes.Where(x => x != "all").ToList();
        ViewData["PublishCategories"] = _catalog.Categories.Where(x => x != "all").ToList();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Görselleri kaydet
        var imagePaths = new List<string>();
        if (model.ImageFiles != null && model.ImageFiles.Count > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            
            // Kapak fotoğrafı indeksini kontrol et
            var coverIndex = model.CoverImageIndex ?? 0;
            if (coverIndex >= model.ImageFiles.Count) coverIndex = 0;
            
            // Dosyaları sıralı kaydet (kapak fotoğrafı ilk sırada olacak)
            var orderedFiles = new List<IFormFile>();
            if (coverIndex > 0 && coverIndex < model.ImageFiles.Count)
            {
                orderedFiles.Add(model.ImageFiles[coverIndex]);
                for (int i = 0; i < model.ImageFiles.Count; i++)
                {
                    if (i != coverIndex) orderedFiles.Add(model.ImageFiles[i]);
                }
            }
            else
            {
                orderedFiles = model.ImageFiles.ToList();
            }
            
            foreach (var file in orderedFiles)
            {
                if (file.Length > 0)
                {
                    var uniqueName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                    imagePaths.Add("/uploads/" + uniqueName);
                }
            }
        }

        // İlanı oluştur ve kaydet
        var listing = new Listing
        {
            // İletişim Bilgileri
            FullName = model.FullName,
            Phone = model.Phone,
            AllowWhatsApp = model.AllowWhatsApp,
            AllowMessages = model.AllowMessages,
            
            // İlan Bilgileri
            Title = model.Title,
            Description = model.Description,
            Tags = model.Tags,
            
            // Konum Bilgileri
            City = model.City,
            District = model.District,
            Neighborhood = model.Neighborhood,
            Address = model.Address,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            
            // Kategori ve Tip
            Category = model.Category,
            SubCategory = model.SubCategory,
            Type = model.Type,
            
            // Fiyat Bilgileri
            PriceAmount = model.PriceAmount,
            PriceCurrency = model.PriceCurrency,
            PriceType = model.PriceType.ToString(),
            PriceDescription = model.PriceDescription,
            Negotiable = model.Negotiable,
            TradeIn = model.TradeIn,
            AdvertiserType = model.AdvertiserType.ToString(),
            
            // Emlak Özellikleri
            EstateNetArea = model.EstateNetArea,
            EstateGrossArea = model.EstateGrossArea,
            EstateRoomCount = model.EstateRoomCount?.ToString(),
            EstateBuildingAge = model.EstateBuildingAge?.ToString(),
            EstateTotalFloors = model.EstateTotalFloors,
            EstateFloorLocation = model.EstateFloorLocation?.ToString(),
            HeatingType = model.HeatingType?.ToString(),
            EstateFurnished = model.EstateFurnished,
            InSite = model.InSite,
            HasBalcony = model.HasBalcony,
            HasElevator = model.HasElevator,
            HasParking = model.HasParking,
            HasPool = model.HasPool,
            HasSecurity = model.HasSecurity,
            DuesAmount = model.DuesAmount,
            DepositAmount = model.DepositAmount,
            
            // Araç Bilgileri
            VehicleBrand = model.VehicleBrand,
            VehicleModel = model.VehicleModel,
            VehicleYear = model.VehicleYear,
            VehicleFuelType = model.VehicleFuelType?.ToString(),
            VehicleTransmission = model.VehicleTransmission?.ToString(),
            VehicleKM = model.VehicleKM,
            VehicleBodyType = model.VehicleBodyType?.ToString(),
            EngineCapacity = model.EngineCapacity,
            EnginePower = model.EnginePower,
            VehicleColor = model.VehicleColor,
            VehiclePlate = model.VehiclePlate,
            UnderWarranty = model.UnderWarranty,
            AccidentRecord = model.AccidentRecord,
            
            // Ürün Bilgileri
            ProductBrand = model.ProductBrand,
            ProductModel = model.ProductModel,
            ProductCondition = model.ProductCondition?.ToString(),
            WarrantyPeriod = model.WarrantyPeriod,
            SerialNumber = model.SerialNumber,
            UsageDuration = model.UsageDuration,
            
            // Medya
            VideoUrl = model.VideoUrl,
            CoverImageIndex = model.CoverImageIndex ?? 0,
            
            // Kullanıcı
            UserId = User.Identity?.IsAuthenticated == true 
                ? _context.Users.Where(u => u.UserName == User.Identity!.Name).Select(u => u.Id).FirstOrDefault() 
                : null,
            
            // Durum
            CreatedAt = DateTime.UtcNow,
            IsApproved = false,
            
            // Görseller
            Images = imagePaths.Select(p => new ListingImage { FilePath = p }).ToList()
        };
        
        _context.Listings.Add(listing);
        _context.SaveChanges();

        // Yöneticilere e-posta gönder
        var adminEmails = new[] { "taskinonel@gmail.com" };
        var subject = "Yeni İlan Başvurusu - " + model.Category.ToUpper();
        var categoryDetails = model.Category switch
        {
            "estate" => $@"<b>Emlak Detayları:</b><br>
                Net/Brüt M²: {model.EstateNetArea}/{model.EstateGrossArea}<br>
                Oda: {model.EstateRoomCount}, Bina Yaşı: {model.EstateBuildingAge}<br>
                Kat: {model.EstateFloorLocation}/{model.EstateTotalFloors}<br>
                Isıtma: {model.HeatingType}, Aidat: {model.DuesAmount}₺<br>",
            "vehicle" => $@"<b>Araç Detayları:</b><br>
                Marka/Model: {model.VehicleBrand} {model.VehicleModel}<br>
                Yıl: {model.VehicleYear}, KM: {model.VehicleKM}<br>
                Yakıt: {model.VehicleFuelType}, Vites: {model.VehicleTransmission}<br>
                Renk: {model.VehicleColor}<br>",
            _ => $@"<b>Ürün Detayları:</b><br>
                Marka: {model.ProductBrand}, Model: {model.ProductModel}<br>
                Durum: {model.ProductCondition}<br>
"
        };
        
        var body = $@"<h2>Yeni İlan Başvurusu</h2>
        <b>İlan No:</b> #{listing.Id}<br>
        <b>İlan Sahibi:</b> {model.FullName}<br>
        <b>Telefon:</b> {model.Phone}<br>
        <b>Başlık:</b> {model.Title}<br>
        <b>Konum:</b> {model.City}, {model.District}, {model.Neighborhood}<br>
        <b>Kategori:</b> {model.Category}<br>
        <b>Tip:</b> {model.Type}<br>
        <b>Fiyat:</b> {model.PriceAmount:N0} {model.PriceCurrency}<br>
        <b>Kimden:</b> {model.AdvertiserType}<br>
        <hr>
        {categoryDetails}
        <hr>
        <b>Açıklama:</b><br>{model.Description}<br>
        <hr>
        <b>Görseller:</b><br>{string.Join("<br>", imagePaths.Select(p => $"<a href='https://localhost:5080{p}'>{p}</a>"))}
        <hr>
        <a href='https://localhost:5080/Admin/Listings' style='padding:10px 20px;background:#667eea;color:#fff;text-decoration:none;border-radius:5px;'>İlanı Yönet</a>
        ";
        
        foreach (var email in adminEmails)
        {
            try { _emailSender.SendAsync(email, subject, body).Wait(); } catch { }
        }

        TempData["PublishSuccess"] = "İlanınız başarıyla alındı! Onay sürecinden sonra yayına girecektir. İlan No: #" + listing.Id;
        return RedirectToAction(nameof(Publish));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Favorites()
    {
        return View();
    }

    public IActionResult Messages()
    {
        return View();
    }

    public IActionResult Membership()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static IEnumerable<PropertyCard> ApplyFilters(
        IEnumerable<PropertyCard> source,
        string listingType,
        string city,
        string category,
        string priceRange,
        string keyword)
    {
        var query = source;

        if (listingType != "all")
        {
            query = query.Where(x => x.Type.Equals(listingType, StringComparison.OrdinalIgnoreCase));
        }

        if (city != "all")
        {
            query = query.Where(x => x.City.Equals(city, StringComparison.OrdinalIgnoreCase));
        }

        if (category != "all")
        {
            query = query.Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        query = priceRange switch
        {
            "low" => query.Where(x => x.PriceAmount <= 5000),
            "mid" => query.Where(x => x.PriceAmount > 5000 && x.PriceAmount <= 50000),
            "high" => query.Where(x => x.PriceAmount > 50000),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Location.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.PrimarySpec.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.SecondarySpec.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    private static IEnumerable<PropertyCard> ApplySorting(IEnumerable<PropertyCard> source, string sortBy)
    {
        return sortBy switch
        {
            "priceAsc" => source.OrderBy(x => x.PriceAmount),
            "priceDesc" => source.OrderByDescending(x => x.PriceAmount),
            "name" => source.OrderBy(x => x.Title),
            _ => source.OrderByDescending(x => x.Id)
        };
    }
}
