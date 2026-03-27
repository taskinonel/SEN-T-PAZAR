using System.Globalization;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Services;

public interface IListingCatalogService
{
    IReadOnlyList<PropertyCard> Listings { get; }

    IReadOnlyList<string> ListingTypes { get; }

    IReadOnlyList<string> Cities { get; }

    IReadOnlyList<string> Categories { get; }

    IReadOnlyList<string> PriceRanges { get; }

    IReadOnlyList<string> SortOptions { get; }

    bool TryResolveCategoryFromSlug(string slug, out string categoryCode);

    string GetDefaultSlug(string categoryCode);

    string GetCategoryHeroImage(string categoryCode);
}

public sealed class ListingCatalogService : IListingCatalogService
{
    private const int TargetPerCategory = 30;

    private static readonly Dictionary<string, string> SlugToCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["emlak"] = "realestate",
        ["arsa"] = "land",
        ["vasita"] = "vehicle",
        ["vehicles"] = "vehicle",
        ["yat-tekne"] = "yacht",
        ["yacht-boat"] = "yacht",
        ["karavan"] = "caravan",
        ["ikinci-el"] = "secondhand",
        ["second-hand"] = "secondhand",
        ["telefon"] = "phone",
        ["bilgisayar"] = "computer",
        ["saat"] = "watch",
        ["mucevher"] = "jewelry",
        ["elektronik"] = "electronics",
        ["is-makineleri"] = "equipment",
        ["ev-yasam"] = "home",
        ["moda"] = "fashion",
        ["hizmet"] = "services"
    };

    private static readonly Dictionary<string, string> CategoryToDefaultSlug = new(StringComparer.OrdinalIgnoreCase)
    {
        ["realestate"] = "emlak",
        ["land"] = "arsa",
        ["vehicle"] = "vasita",
        ["yacht"] = "yat-tekne",
        ["caravan"] = "karavan",
        ["secondhand"] = "ikinci-el",
        ["phone"] = "telefon",
        ["computer"] = "bilgisayar",
        ["watch"] = "saat",
        ["jewelry"] = "mucevher",
        ["electronics"] = "elektronik",
        ["equipment"] = "is-makineleri",
        ["home"] = "ev-yasam",
        ["fashion"] = "moda",
        ["services"] = "hizmet"
    };

    private static readonly Dictionary<string, string> CategoryHeroImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["realestate"] = "https://images.unsplash.com/photo-1600607687644-c7171b42498f?auto=format&fit=crop&w=1800&q=80",
        ["land"] = "https://images.unsplash.com/photo-1500382017468-9049fed747ef?auto=format&fit=crop&w=1800&q=80",
        ["vehicle"] = "https://images.unsplash.com/photo-1549924231-f129b911e442?auto=format&fit=crop&w=1800&q=80",
        ["yacht"] = "https://images.unsplash.com/photo-1562281302-809108fd533c?auto=format&fit=crop&w=1800&q=80",
        ["caravan"] = "https://images.unsplash.com/photo-1520256862855-398228c41684?auto=format&fit=crop&w=1800&q=80",
        ["secondhand"] = "https://images.unsplash.com/photo-1481437156560-3205f6a55735?auto=format&fit=crop&w=1800&q=80",
        ["phone"] = "https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=1800&q=80",
        ["computer"] = "https://images.unsplash.com/photo-1498050108023-c5249f4df085?auto=format&fit=crop&w=1800&q=80",
        ["watch"] = "https://images.unsplash.com/photo-1508057198894-247b23fe5ade?auto=format&fit=crop&w=1800&q=80",
        ["jewelry"] = "https://images.unsplash.com/photo-1617038220319-276d3cfab638?auto=format&fit=crop&w=1800&q=80",
        ["electronics"] = "https://images.unsplash.com/photo-1550009158-9ebf69173e03?auto=format&fit=crop&w=1800&q=80",
        ["equipment"] = "https://images.unsplash.com/photo-1504307651254-35680f356dfd?auto=format&fit=crop&w=1800&q=80",
        ["home"] = "https://images.unsplash.com/photo-1616594039964-86d2c2bdc96f?auto=format&fit=crop&w=1800&q=80",
        ["fashion"] = "https://images.unsplash.com/photo-1445205170230-053b83016050?auto=format&fit=crop&w=1800&q=80",
        ["services"] = "https://images.unsplash.com/photo-1521790797524-b2497295b8a0?auto=format&fit=crop&w=1800&q=80"
    };

    private static readonly Dictionary<string, CategorySeed> SeedByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["realestate"] = new CategorySeed(
            "Daire", "Konut", "Kıbrıs'ta oturum ve yatırım için ideal daire.",
            ["2+1 Deniz Manzaralı Daire", "3+1 Geniş Aile Dairesi", "Site İçinde Modern Rezidans", "Merkezi Konumda Şehir Dairesi"],
            "m²", "bina yaşı", 185000, "apartment,interior,home"),
        ["land"] = new CategorySeed(
            "Arsa", "Parsel", "Altyapısı hazır ve yatırım değeri güçlü arsa.",
            ["İmarlı Yatırımlık Arsa", "Yola Cepheli Ticari Parsel", "Villa Projesine Uygun Arsa", "Denize Yakın Gelişim Bölgesi Arsası"],
            "m²", "imar durumu", 92000, "land,field,property"),
        ["vehicle"] = new CategorySeed(
            "Araç", "Otomobil", "Bakımları tam, ekspertiz raporlu araç.",
            ["Dizel Otomatik SUV", "Düşük KM Aile Sedan", "Şehir İçi Ekonomik Hatchback", "Premium Segment Executive"],
            "km", "vites", 15800, "car,vehicle,automobile"),
        ["yacht"] = new CategorySeed(
            "Yat", "Tekne", "Marina teslim, bakımlı motor yat.",
            ["45 ft Motor Yat", "Aile Tipi Gezi Teknesi", "Marina Çıkışlı Premium Yat", "Sezonluk Kiralık Yat"],
            "ft", "kapasite", 4800, "yacht,boat,sea"),
        ["caravan"] = new CategorySeed(
            "Karavan", "Mobil Yaşam", "Uzun yol ve kamp için tam donanım.",
            ["4 Kişilik Aile Karavanı", "Off-Grid Kamp Karavanı", "Panelvan Dönüşüm Karavan", "Lüks İç Tasarımlı Karavan"],
            "kapasite", "yakıt", 3200, "caravan,camper,rv"),
        ["secondhand"] = new CategorySeed(
            "İkinci El", "Kullanılmış", "Temiz kullanılmış ve uygun fiyatlı ürün.",
            ["Masif Ahşap Yemek Masası", "Ergonomik Ofis Koltuğu", "Az Kullanılmış Koşu Bandı", "Set Üstü Mutfak Paketi"],
            "durum", "garanti", 260, "second hand,furniture,used"),
        ["phone"] = new CategorySeed(
            "Telefon", "Mobil", "Kutulu, faturalı ve pil sağlığı yüksek cihaz.",
            ["256 GB Akıllı Telefon", "Kamera Odaklı Amiral Gemisi", "Uzun Batarya Ömürlü Model", "Kompakt Premium Telefon"],
            "depolama", "bağlantı", 690, "smartphone,mobile phone"),
        ["computer"] = new CategorySeed(
            "Bilgisayar", "Laptop", "İş ve oyun için yüksek performans.",
            ["RTX Ekran Kartlı Oyun Laptopu", "İçerik Üretimi için Workstation", "Ultra Hafif İş Bilgisayarı", "Masaüstü Performans Sistemi"],
            "ram", "disk", 1450, "computer,laptop,pc"),
        ["watch"] = new CategorySeed(
            "Saat", "Kol Saati", "Orijinal ve sertifikalı saat koleksiyonu.",
            ["İsviçre Mekanik Saat", "Klasik Çelik Kordon Model", "Spor Kronograf Saat", "Limitli Seri Koleksiyon"],
            "çap", "mekanizma", 3600, "watch,wristwatch,luxury"),
        ["jewelry"] = new CategorySeed(
            "Mücevher", "Takı", "Sertifikalı taş işçiliğiyle premium takı.",
            ["Pırlanta Kolye Seti", "Altın Bileklik Koleksiyonu", "Zümrüt Taşlı Yüzük", "Özel Tasarım Küpe"],
            "ayar", "sertifika", 2950, "jewelry,diamond,gold"),
        ["electronics"] = new CategorySeed(
            "Elektronik", "Cihaz", "Testleri yapılmış, yüksek performanslı elektronik.",
            ["4K OLED Televizyon", "Gürültü Engelleyici Kulaklık", "Akıllı Ev Kamera Seti", "Profesyonel Ses Sistemi"],
            "model", "durum", 740, "electronics,gadget,device"),
        ["equipment"] = new CategorySeed(
            "İş Makinesi", "Endüstriyel", "Sahaya hazır, bakımlı iş ekipmanı.",
            ["3.5 Ton Forklift", "Mini Ekskavatör", "Jeneratör Güç Ünitesi", "Platform Lift Sistemi"],
            "kapasite", "yakıt", 4600, "construction equipment,machine"),
        ["home"] = new CategorySeed(
            "Ev Yaşam", "Dekorasyon", "Modern ve dayanıklı ev yaşam ürünleri.",
            ["L Koltuk Takımı", "6 Kişilik Yemek Odası", "Yatak Odası Komple Set", "Dekoratif Aydınlatma Paketi"],
            "malzeme", "renk", 420, "home interior,furniture,living room"),
        ["fashion"] = new CategorySeed(
            "Moda", "Giyim", "Yeni sezon ve orijinal moda ürünleri.",
            ["Premium Deri Ceket", "Günlük Sneaker Koleksiyonu", "Kadın Tasarım Elbise", "Unisex Streetwear Set"],
            "beden", "koleksiyon", 220, "fashion,clothing,style"),
        ["services"] = new CategorySeed(
            "Hizmet", "Servis", "Kurumsal standartta profesyonel hizmet.",
            ["Kurumsal Temizlik Hizmeti", "Teknik Bakım ve Onarım", "Taşıma ve Lojistik Desteği", "Dijital Pazarlama Danışmanlığı"],
            "paket", "teslim", 140, "service,professional,team")
    };

    public IReadOnlyList<PropertyCard> Listings { get; }

    public IReadOnlyList<string> ListingTypes { get; } = ["all", "sale", "rent"];

    public IReadOnlyList<string> Cities { get; } = ["all", "Girne", "İskele", "Lefkoşa", "Gazimağusa", "Karpaz", "Güzelyurt"];

    public IReadOnlyList<string> Categories { get; } =
    [
        "all", "realestate", "land", "vehicle", "yacht", "caravan", "secondhand", "phone", "computer", "watch",
        "jewelry", "electronics", "equipment", "home", "fashion", "services"
    ];

    public IReadOnlyList<string> PriceRanges { get; } = ["any", "low", "mid", "high"];

    public IReadOnlyList<string> SortOptions { get; } = ["latest", "priceAsc", "priceDesc", "name"];

    public ListingCatalogService()
    {
        Listings = BuildListings();
    }

    public bool TryResolveCategoryFromSlug(string slug, out string categoryCode)
    {
        return SlugToCategory.TryGetValue(slug, out categoryCode!);
    }

    public string GetDefaultSlug(string categoryCode)
    {
        return CategoryToDefaultSlug.TryGetValue(categoryCode, out var slug) ? slug : "kategori";
    }

    public string GetCategoryHeroImage(string categoryCode)
    {
        return CategoryHeroImages.TryGetValue(categoryCode, out var image)
            ? image
            : "https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=1800&q=80";
    }

    private List<PropertyCard> BuildListings()
    {
        var trCulture = CultureInfo.GetCultureInfo("tr-TR");
        var cityPool = Cities.Where(x => x != "all").ToArray();
        var result = new List<PropertyCard>();
        var nextId = 1;

        foreach (var category in Categories.Where(x => x != "all"))
        {
            if (!SeedByCategory.TryGetValue(category, out var seed))
            {
                continue;
            }

            for (var i = 1; i <= TargetPerCategory; i++)
            {
                var type = i % 3 == 0 ? "rent" : "sale";
                var city = cityPool[(i - 1) % cityPool.Length];
                var variant = seed.TitleVariants[(i - 1) % seed.TitleVariants.Length];
                var priceAmount = CalculatePrice(seed.BasePrice, i, type);
                var neighborhood = BuildNeighborhood(category, i);
                var location = $"{city}, {neighborhood}";
                var primarySpec = BuildPrimarySpec(seed, i);
                var secondarySpec = BuildSecondarySpec(seed, i);
                var galleryImages = BuildGalleryImages(category, seed.ImageQuery, nextId);
                var area = BuildArea(category, i);
                var rooms = BuildRooms(category, i);

                result.Add(new PropertyCard
                {
                    Id = nextId,
                    Title = $"{variant} #{i:00}",
                    Summary = BuildSummary(variant, seed.SummaryLine, city, neighborhood, primarySpec, secondarySpec),
                    Category = category,
                    City = city,
                    Neighborhood = neighborhood,
                    Location = location,
                    PriceAmount = priceAmount,
                    PriceLabel = type == "rent"
                        ? $"GBP {priceAmount.ToString("N0", trCulture)} / ay"
                        : $"GBP {priceAmount.ToString("N0", trCulture)}",
                    Type = type,
                    PrimarySpec = primarySpec,
                    SecondarySpec = secondarySpec,
                    Area = area,
                    Rooms = rooms,
                    ImageUrl = galleryImages[0],
                    GalleryImages = galleryImages,
                    Facts = BuildFacts(category, type, city, neighborhood, primarySpec, secondarySpec, area, rooms, i),
                    Highlights = BuildHighlights(category, city, neighborhood, primarySpec, secondarySpec, i),
                    FeatureBadges = BuildFeatureBadges(category, type, i),
                    SellerName = BuildSellerName(category, i),
                    SellerRole = BuildSellerRole(category, type),
                    SellerPhone = BuildSellerPhone(nextId),
                    PostedAtLabel = BuildPostedAtLabel(i),
                    ListingCode = BuildListingCode(category, nextId),
                    AvailabilityNote = BuildAvailabilityNote(category, type, i),
                    DetailBody = BuildDetailBody(variant, seed.SummaryLine, city, neighborhood, type, i)
                });

                nextId++;
            }
        }

        return result;
    }

    private static decimal CalculatePrice(decimal basePrice, int index, string type)
    {
        var typeMultiplier = type == "rent" ? 0.13m : 1m;
        var progression = 1m + (((index - 1) % 10) * 0.05m);
        return Math.Round(basePrice * typeMultiplier * progression, 0, MidpointRounding.AwayFromZero);
    }

    private static string BuildPrimarySpec(CategorySeed seed, int index)
    {
        return seed.PrimaryUnit switch
        {
            "m²" => $"{90 + ((index - 1) % 11) * 12} m²",
            "km" => $"{22000 + ((index - 1) * 3800)} km",
            "ft" => $"{36 + ((index - 1) % 8) * 2} ft",
            "kapasite" => $"{2 + ((index - 1) % 5)} kişilik",
            "depolama" => (index % 3) switch
            {
                0 => "512 GB",
                1 => "128 GB",
                _ => "256 GB"
            },
            "ram" => (index % 3) switch
            {
                0 => "32 GB RAM",
                1 => "16 GB RAM",
                _ => "24 GB RAM"
            },
            "çap" => $"{38 + ((index - 1) % 6)} mm",
            "ayar" => (index % 2) == 0 ? "14 ayar" : "18 ayar",
            "model" => $"Model {2020 + ((index - 1) % 6)}",
            "malzeme" => (index % 2) == 0 ? "Masif Ahşap" : "Metal + Kumaş",
            "beden" => (index % 4) switch
            {
                0 => "S-M",
                1 => "M-L",
                2 => "L-XL",
                _ => "Standart"
            },
            "paket" => (index % 3) switch
            {
                0 => "Kurumsal Paket",
                1 => "Standart Paket",
                _ => "Premium Paket"
            },
            _ => $"Seri {(index - 1) % 7 + 1}"
        };
    }

    private static string BuildSecondarySpec(CategorySeed seed, int index)
    {
        return seed.SecondaryUnit switch
        {
            "bina yaşı" => $"{(index - 1) % 16 + 1} yaş",
            "imar durumu" => (index % 2) == 0 ? "Konut İmarlı" : "Ticari İmarlı",
            "vites" => (index % 2) == 0 ? "Otomatik" : "Manuel",
            "kapasite" => $"{6 + ((index - 1) % 6)} kişi",
            "yakıt" => (index % 2) == 0 ? "Dizel" : "Benzin",
            "garanti" => (index % 2) == 0 ? "6 ay garanti" : "12 ay garanti",
            "bağlantı" => (index % 2) == 0 ? "5G" : "4.5G",
            "disk" => (index % 2) == 0 ? "1 TB SSD" : "512 GB SSD",
            "mekanizma" => (index % 2) == 0 ? "Mekanik" : "Quartz",
            "sertifika" => "Sertifikalı",
            "durum" => (index % 2) == 0 ? "Sıfır Ayarında" : "Az Kullanılmış",
            "renk" => (index % 3) switch
            {
                0 => "Antrasit",
                1 => "Bej",
                _ => "Koyu Mavi"
            },
            "koleksiyon" => $"Sezon {(index - 1) % 4 + 1}",
            "teslim" => (index % 2) == 0 ? "Aynı gün" : "24 saat içinde",
            _ => "Standart"
        };
    }

    private static string BuildNeighborhood(string category, int index)
    {
        var pool = category switch
        {
            "realestate" => new[] { "Alsancak", "Bellapais", "Çatalköy", "Karaoğlanoğlu", "Yeni Boğaziçi", "Long Beach" },
            "land" => new[] { "Esentepe", "Tatlısu", "Karpaz", "Lapta", "Yeni Erenköy", "İskele Sahil" },
            "vehicle" => new[] { "Merkez", "Sanayi Bölgesi", "Showroom Hattı", "Marina Yolu", "Çevre Yolu", "Liman Bölgesi" },
            "yacht" => new[] { "Girne Marina", "İskele Sahili", "Gazimağusa Liman", "Lapta Marina", "Karpaz Koyu", "Esentepe Marina" },
            "caravan" => new[] { "Karpaz Kamp", "Tatlısu Sahili", "Lapta Kamp Alanı", "Esentepe", "İskele Kıyı", "Alsancak Doğa Hattı" },
            "services" => new[] { "Merkez", "Bölgesel Servis", "Ofis Bölgesi", "Sanayi Bölgesi", "Sahil Hattı", "Geniş Hizmet Alanı" },
            _ => new[] { "Merkez", "Sahil", "Çarşı", "Yenişehir", "Butik Bölge", "Prestij Hattı" }
        };

        return pool[(index - 1) % pool.Length];
    }

    private static string BuildArea(string category, int index)
    {
        return category switch
        {
            "realestate" => $"{95 + ((index - 1) % 10) * 14} m2",
            "land" => $"{620 + ((index - 1) % 10) * 110} m2",
            "home" => $"{40 + ((index - 1) % 5) * 10} m2 kullanım alanı",
            "services" => $"{2 + ((index - 1) % 4)} saatlik hizmet",
            _ => string.Empty
        };
    }

    private static string BuildRooms(string category, int index)
    {
        if (!string.Equals(category, "realestate", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return (index % 5) switch
        {
            0 => "1+1",
            1 => "2+1",
            2 => "3+1",
            3 => "4+1",
            _ => "2+1 Loft"
        };
    }

    private static string BuildSummary(string variant, string summaryLine, string city, string neighborhood, string primarySpec, string secondarySpec)
    {
        return $"{variant} - {summaryLine} {city} / {neighborhood} bölgesinde {primarySpec} ve {secondarySpec} detaylarıyla öne çıkar.";
    }

    private static List<string> BuildGalleryImages(string category, string baseQuery, int listingId)
    {
        var galleryQueries = category switch
        {
            "realestate" => new[] { "modern apartment exterior mediterranean", "luxury living room apartment", "apartment kitchen interior", "bedroom sea view apartment", "balcony sunset residence", "apartment bathroom design" },
            "land" => new[] { "coastal land aerial", "field parcel road", "development land mediterranean", "sea view empty plot", "investment land drone", "green open field" },
            "vehicle" => new[] { "luxury suv exterior", "sedan side profile", "car interior dashboard", "vehicle rear angle", "premium car front", "car leather seats" },
            "yacht" => new[] { "luxury yacht marina", "yacht deck sea", "yacht interior cabin", "boat helm station", "yacht sunset exterior", "motor yacht side profile" },
            "caravan" => new[] { "camper van road trip", "rv interior modern", "caravan campsite", "camper kitchen interior", "motorhome exterior", "camping van sunset" },
            "phone" => new[] { "premium smartphone product", "mobile phone camera closeup", "phone accessories box", "smartphone design back", "mobile device screen", "smartphone in hand" },
            "computer" => new[] { "gaming laptop setup", "workstation desk setup", "ultrabook desk clean", "desktop pc rgb", "laptop keyboard closeup", "creative computer workspace" },
            "watch" => new[] { "luxury watch macro", "chronograph watch studio", "watch steel bracelet", "watch leather strap premium", "timepiece dark background", "mechanical watch dial" },
            "jewelry" => new[] { "diamond necklace luxury", "gold bracelet closeup", "emerald ring macro", "jewelry box premium", "fine earrings gemstone", "luxury jewelry display" },
            "electronics" => new[] { "oled television living room", "headphones studio product", "smart home camera setup", "audio speaker premium", "gaming console setup", "electronics desk gadget" },
            "equipment" => new[] { "forklift warehouse equipment", "mini excavator construction", "industrial generator machine", "lift platform site", "heavy machinery detail", "construction machine sunset" },
            "home" => new[] { "living room sofa modern", "dining room furniture", "bedroom furniture set", "decorative lighting interior", "home decor styling", "console table interior" },
            "fashion" => new[] { "leather jacket fashion", "sneakers clean lifestyle", "designer dress boutique", "streetwear product style", "fashion accessories premium", "wardrobe styling" },
            "services" => new[] { "professional cleaning team", "technician repair service", "logistics team moving", "business consulting meeting", "service crew office", "professional maintenance worker" },
            _ => new[] { baseQuery, $"{baseQuery},detail", $"{baseQuery},premium", $"{baseQuery},interior", $"{baseQuery},showroom", $"{baseQuery},lifestyle" }
        };

        var gallery = new List<string>(galleryQueries.Length);

        for (var i = 0; i < galleryQueries.Length; i++)
        {
            gallery.Add(BuildUniqueImageUrl(galleryQueries[i], (listingId * 10) + i));
        }

        return gallery;
    }

    private static List<ListingFact> BuildFacts(string category, string type, string city, string neighborhood, string primarySpec, string secondarySpec, string area, string rooms, int index)
    {
        return category switch
        {
            "realestate" => new()
            {
                new() { Label = "İlan tipi", Value = type == "rent" ? "Kiralık" : "Satılık" },
                new() { Label = "Oda planı", Value = rooms },
                new() { Label = "Net alan", Value = area },
                new() { Label = "Bina yaşı", Value = secondarySpec },
                new() { Label = "Bölge", Value = $"{city} / {neighborhood}" },
                new() { Label = "Aidat", Value = $"{1200 + (index % 5) * 350} TL" }
            },
            "land" => new()
            {
                new() { Label = "Parsel alanı", Value = area },
                new() { Label = "İmar durumu", Value = secondarySpec },
                new() { Label = "Tapu", Value = "Müstakil tapu" },
                new() { Label = "Cephe", Value = $"{18 + (index % 7) * 4} metre" },
                new() { Label = "Bölge", Value = $"{city} / {neighborhood}" },
                new() { Label = "Altyapı", Value = "Yol ve elektrik hazır" }
            },
            "vehicle" => new()
            {
                new() { Label = "Kilometre", Value = primarySpec },
                new() { Label = "Şanzıman", Value = secondarySpec },
                new() { Label = "Yakıt", Value = (index % 2) == 0 ? "Dizel" : "Benzin" },
                new() { Label = "Kasa", Value = (index % 2) == 0 ? "SUV" : "Sedan" },
                new() { Label = "Tramer", Value = (index % 3) == 0 ? "Değişensiz" : "Parça parça lokal" },
                new() { Label = "Konum", Value = city }
            },
            "yacht" => new()
            {
                new() { Label = "Boy", Value = primarySpec },
                new() { Label = "Kapasite", Value = secondarySpec },
                new() { Label = "Motor saati", Value = $"{580 + (index % 6) * 70} saat" },
                new() { Label = "Bayrak", Value = "KKTC / TR" },
                new() { Label = "Teslim", Value = "Marina teslim" },
                new() { Label = "Liman", Value = $"{city} / {neighborhood}" }
            },
            "caravan" => new()
            {
                new() { Label = "Kapasite", Value = primarySpec },
                new() { Label = "Yakıt", Value = secondarySpec },
                new() { Label = "Yatak", Value = $"{2 + (index % 3)} adet" },
                new() { Label = "Isıtma", Value = "Webasto" },
                new() { Label = "Elektrik", Value = (index % 2) == 0 ? "Solar panel destekli" : "Harici bağlantıya hazır" },
                new() { Label = "Konum", Value = city }
            },
            "services" => new()
            {
                new() { Label = "Paket", Value = primarySpec },
                new() { Label = "Teslim", Value = secondarySpec },
                new() { Label = "Servis alanı", Value = $"{city} ve çevresi" },
                new() { Label = "Yanıt süresi", Value = $"{30 + (index % 4) * 15} dk" },
                new() { Label = "Ekip", Value = $"{2 + (index % 5)} kişilik ekip" },
                new() { Label = "Randevu", Value = "Ön rezervasyon ile" }
            },
            _ => new()
            {
                new() { Label = "Öne çıkan", Value = primarySpec },
                new() { Label = "Durum", Value = secondarySpec },
                new() { Label = "Lokasyon", Value = $"{city} / {neighborhood}" },
                new() { Label = "Teslim", Value = (index % 2) == 0 ? "Aynı gün" : "Kargo / elden teslim" },
                new() { Label = "Stok", Value = $"{5 + (index % 8)} adet / parça" },
                new() { Label = "Ek bilgi", Value = "Kontrolleri tamamlandı" }
            }
        };
    }

    private static List<string> BuildHighlights(string category, string city, string neighborhood, string primarySpec, string secondarySpec, int index)
    {
        return category switch
        {
            "realestate" => new()
            {
                $"{neighborhood} lokasyonunda yüksek talep gören bölgede yer alıyor",
                $"{primarySpec} kullanım alanı ile günlük yaşam konforunu artırıyor",
                $"{secondarySpec} yapısal profil için yenilenmiş detaylar sunuyor",
                "Taşınmaya veya yatırım amaçlı değerlendirmeye uygun teslim planı mevcut"
            },
            "land" => new()
            {
                $"{primarySpec} büyüklüğünde gelişime açık parsel",
                $"{secondarySpec} yapısı ile yatırımcı profiline hitap ediyor",
                $"{city} bağlantı yollarına yakın konum avantajı",
                "Bölgedeki proje hareketliliği nedeniyle değer potansiyeli güçlü"
            },
            "vehicle" => new()
            {
                $"{primarySpec} ile dengeli kullanım geçmişi",
                $"{secondarySpec} sürüş karakteri ve segment uyumu",
                "Bakım geçmişi ve ekspertiz notları paylaşılmaya hazır",
                $"Paket seviyesi {index % 5 + 1} ile günlük kullanıma ve uzun yola uygun"
            },
            "services" => new()
            {
                $"{city} / {neighborhood} hattında aktif hizmet veriyor",
                $"{primarySpec} ile farklı bütçelere uygun teklif yapısı sunuyor",
                $"{secondarySpec} teslim yaklaşımı planlamayı kolaylaştırıyor",
                "Kurumsal veya bireysel talepler için ölçeklenebilir çözüm sağlıyor"
            },
            _ => new()
            {
                $"{primarySpec} ile öne çıkan güncel ilan profili",
                $"{secondarySpec} bilgisi açık ve anlaşılır biçimde sunuldu",
                $"{city} lokasyonunda hızlı teslim / buluşma avantajı sağlıyor",
                "Görsel ve metin kurgusu gerçek ilana yakın deneyim için zenginleştirildi"
            }
        };
    }

    private static List<string> BuildFeatureBadges(string category, string type, int index)
    {
        var badges = new List<string>
        {
            type == "rent" ? "Hızlı kiralama" : "Hazır teslim",
            (index % 2) == 0 ? "Doğrulanmış bilgi" : "Güncel ilan"
        };

        badges.Add(category switch
        {
            "realestate" => "Yatırım fırsatı",
            "land" => "Gelişim bölgesi",
            "vehicle" => "Ekspertiz notlu",
            "yacht" => "Marina hazır",
            "caravan" => "Kamp uyumlu",
            "phone" => "Kutulu cihaz",
            "computer" => "Performans seçimi",
            "watch" => "Koleksiyon parçası",
            "jewelry" => "Sertifikalı",
            "electronics" => "Test edilmiş",
            "equipment" => "Sahaya hazır",
            "home" => "Dekor seçkisi",
            "fashion" => "Yeni sezon",
            "services" => "Randevu alınabilir",
            _ => "Öne çıkan"
        });

        return badges;
    }

    private static string BuildSellerName(string category, int index)
    {
        var pool = category switch
        {
            "realestate" => new[] { "Blue Coast Estates", "Northline Property", "Harbor Living", "Ada Portföy" },
            "land" => new[] { "Terra Invest", "Kuzey Arsa Ofisi", "Nova Parsel", "Horizon Land" },
            "vehicle" => new[] { "AutoPrime", "North Garage", "Marina Motors", "Cityline Auto" },
            "yacht" => new[] { "Marina Select", "Blue Sail", "Harbor Yacht", "Coastline Marine" },
            "caravan" => new[] { "Roadcamp", "Nomad Garage", "Vanlife Hub", "Campline" },
            "services" => new[] { "Ada Hizmet Grubu", "Prime Support", "North Works", "Saha Ekibi" },
            _ => new[] { "Premium Store", "Ada Seçki", "Northline Shop", "Urban Select" }
        };

        return pool[(index - 1) % pool.Length];
    }

    private static string BuildSellerRole(string category, string type)
    {
        return category switch
        {
            "realestate" or "land" => type == "rent" ? "Portföy Danışmanı" : "Satış Danışmanı",
            "vehicle" => "Yetkili Satıcı",
            "services" => "Hizmet Sağlayıcı",
            "yacht" => "Marina Temsilcisi",
            _ => "Kurumsal Satıcı"
        };
    }

    private static string BuildSellerPhone(int listingId)
    {
        return $"+90 548 {200 + (listingId % 700)} {10 + (listingId % 80):00} {20 + (listingId % 70):00}";
    }

    private static string BuildPostedAtLabel(int index)
    {
        return (index % 5) switch
        {
            0 => "Bugün güncellendi",
            1 => "Dün eklendi",
            2 => "2 gün önce güncellendi",
            3 => "Bu hafta eklendi",
            _ => "Son 7 gün içinde yayında"
        };
    }

    private static string BuildListingCode(string category, int listingId)
    {
        var prefix = category[..Math.Min(3, category.Length)].ToUpperInvariant();
        return $"{prefix}-{listingId:00000}";
    }

    private static string BuildAvailabilityNote(string category, string type, int index)
    {
        return category switch
        {
            "services" => (index % 2) == 0 ? "Bu hafta içinde randevu alınabilir." : "Yoğun dönem için ön rezervasyon önerilir.",
            "vehicle" => (index % 2) == 0 ? "Test sürüşü planlanabilir." : "Ekspertiz için önceden haber verilmesi yeterli.",
            "realestate" => type == "rent" ? "Taşınmaya uygun teslim planı hazır." : "Tapu ve ekspertiz süreci için uygun.",
            _ => (index % 2) == 0 ? "Stok / teslim durumu günceldir." : "Detaylı bilgi için satıcı ile iletişime geçin."
        };
    }

    private static string BuildDetailBody(string variant, string summaryLine, string city, string neighborhood, string type, int index)
    {
        var typeText = type == "rent" ? "kiralama" : "satın alma";
        var tone = (index % 2) == 0
            ? "Son kullanıcı deneyimi düşünülerek hazırlanmış, güçlü ilk izlenim veren bir kurguyla sunuluyor."
            : "İhtiyaca göre hızlı karar vermeyi kolaylaştıran net bir içerik yapısıyla destekleniyor.";

        return $"{variant}, {city} / {neighborhood} bölgesinde {typeText} odaklı arama yapan kullanıcılar için öne çıkarılmış demo ilandır. {summaryLine} {tone} Görseller, satıcı bilgileri ve temel nitelikler bir arada sunularak detay sayfasının gerçek bir ilan deneyimine daha yakın his vermesi hedeflenmiştir.";
    }

    private static string BuildUniqueImageUrl(string query, int uniqueId)
    {
        var escapedQuery = Uri.EscapeDataString(query);
        return $"https://source.unsplash.com/featured/1600x1000/?{escapedQuery}&sig={uniqueId}";
    }

    private sealed record CategorySeed(
        string LocationZone,
        string ListingFamily,
        string SummaryLine,
        string[] TitleVariants,
        string PrimaryUnit,
        string SecondaryUnit,
        decimal BasePrice,
        string ImageQuery);
}
