using System.Globalization;

namespace SEN_T_PAZAR.Models;

public sealed class ListingCategoryTaxonomy
{
    public required string Key { get; init; }

    public IReadOnlyList<string> SubCategories { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ListingTypes { get; init; } = Array.Empty<string>();

    public bool RequiresProductCondition { get; init; }
}

public sealed class SearchCategoryDefinition
{
    public required string Key { get; init; }

    public required string TranslationKey { get; init; }

    public required string DefaultSlug { get; init; }

    public string? EnglishSlug { get; init; }

    public required string HeroImageUrl { get; init; }
}

public sealed class SearchTabDefinition
{
    public required string Key { get; init; }

    public required string ListingType { get; init; }

    public string PresetCategory { get; init; } = "all";
}

public sealed class PublishProductFieldVisibility
{
    public required bool ShowBrand { get; init; }

    public required bool ShowModel { get; init; }

    public required bool ShowWarranty { get; init; }

    public required bool ShowSerial { get; init; }

    public required bool ShowUsage { get; init; }
}

public static class ListingTaxonomy
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private static readonly TextInfo TurkishTextInfo = CultureInfo.GetCultureInfo("tr-TR").TextInfo;
    private static readonly PublishProductFieldVisibility DefaultProductFieldVisibility = new()
    {
        ShowBrand = true,
        ShowModel = true,
        ShowWarranty = true,
        ShowSerial = true,
        ShowUsage = true
    };

    private static readonly IReadOnlyList<string> SearchListingTypes = ["all", "sale", "rent", "daily", "service", "lesson", "job", "adoption"];

    private static readonly IReadOnlyList<SearchCategoryDefinition> SearchCategoryDefinitions =
    [
        new()
        {
            Key = "realestate",
            TranslationKey = "cat_realestate",
            DefaultSlug = "emlak",
            EnglishSlug = "real-estate",
            HeroImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498f?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "land",
            TranslationKey = "cat_land",
            DefaultSlug = "arsa",
            EnglishSlug = "land",
            HeroImageUrl = "https://images.unsplash.com/photo-1500382017468-9049fed747ef?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "vehicle",
            TranslationKey = "cat_vehicle",
            DefaultSlug = "vasita",
            EnglishSlug = "vehicles",
            HeroImageUrl = "https://images.unsplash.com/photo-1549924231-f129b911e442?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "yacht",
            TranslationKey = "cat_yacht",
            DefaultSlug = "yat-tekne",
            EnglishSlug = "yacht-boat",
            HeroImageUrl = "https://images.unsplash.com/photo-1562281302-809108fd533c?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "caravan",
            TranslationKey = "cat_caravan",
            DefaultSlug = "karavan",
            EnglishSlug = "caravan",
            HeroImageUrl = "https://images.unsplash.com/photo-1520256862855-398228c41684?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "phone",
            TranslationKey = "cat_phone",
            DefaultSlug = "telefon",
            EnglishSlug = "phone",
            HeroImageUrl = "https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "computer",
            TranslationKey = "cat_computer",
            DefaultSlug = "bilgisayar",
            EnglishSlug = "computer",
            HeroImageUrl = "https://images.unsplash.com/photo-1498050108023-c5249f4df085?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "watch",
            TranslationKey = "cat_watch",
            DefaultSlug = "saat",
            EnglishSlug = "watch",
            HeroImageUrl = "https://images.unsplash.com/photo-1508057198894-247b23fe5ade?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "jewelry",
            TranslationKey = "cat_jewelry",
            DefaultSlug = "mucevher",
            EnglishSlug = "jewelry",
            HeroImageUrl = "https://images.unsplash.com/photo-1617038220319-276d3cfab638?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "electronics",
            TranslationKey = "cat_electronics",
            DefaultSlug = "elektronik",
            EnglishSlug = "electronics",
            HeroImageUrl = "https://images.unsplash.com/photo-1550009158-9ebf69173e03?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "equipment",
            TranslationKey = "cat_equipment",
            DefaultSlug = "is-makineleri",
            EnglishSlug = "heavy-equipment",
            HeroImageUrl = "https://images.unsplash.com/photo-1504307651254-35680f356dfd?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "home",
            TranslationKey = "cat_home",
            DefaultSlug = "ev-yasam",
            EnglishSlug = "home-living",
            HeroImageUrl = "https://images.unsplash.com/photo-1616594039964-86d2c2bdc96f?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "fashion",
            TranslationKey = "cat_fashion",
            DefaultSlug = "moda",
            EnglishSlug = "fashion",
            HeroImageUrl = "https://images.unsplash.com/photo-1445205170230-053b83016050?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "services",
            TranslationKey = "cat_services",
            DefaultSlug = "hizmet",
            EnglishSlug = "services",
            HeroImageUrl = "https://images.unsplash.com/photo-1521790797524-b2497295b8a0?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "tutoring",
            TranslationKey = "cat_tutoring",
            DefaultSlug = "ozel-ders",
            EnglishSlug = "private-tutors",
            HeroImageUrl = "https://images.unsplash.com/photo-1513258496099-48168024aec0?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "jobs",
            TranslationKey = "cat_jobs",
            DefaultSlug = "is-ilanlari",
            EnglishSlug = "job-listings",
            HeroImageUrl = "https://images.unsplash.com/photo-1521737604893-d14cc237f11d?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "helper",
            TranslationKey = "cat_helper",
            DefaultSlug = "yardimci",
            EnglishSlug = "helper-search",
            HeroImageUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "secondhand",
            TranslationKey = "cat_secondhand",
            DefaultSlug = "ikinci-el",
            EnglishSlug = "second-hand",
            HeroImageUrl = "https://images.unsplash.com/photo-1481437156560-3205f6a55735?auto=format&fit=crop&w=1800&q=80"
        }
        ,
        new()
        {
            Key = "pets",
            TranslationKey = "cat_pets",
            DefaultSlug = "hayvanlar",
            EnglishSlug = "pets",
            HeroImageUrl = "https://images.unsplash.com/photo-1517841905240-472988babdf9?auto=format&fit=crop&w=1800&q=80"
        },
        new()
        {
            Key = "adoption",
            TranslationKey = "cat_adoption",
            DefaultSlug = "sahiplendirme",
            EnglishSlug = "adoption",
            HeroImageUrl = "https://images.unsplash.com/photo-1517423440428-a5a00ad493e8?auto=format&fit=crop&w=1800&q=80"
        }
    ];

    private static readonly Dictionary<string, SearchCategoryDefinition> SearchCategoryDefinitionMap = SearchCategoryDefinitions
        .ToDictionary(item => item.Key, item => item, Comparer);

    // Homepage and global search should expose only parent categories.
    private static readonly IReadOnlyList<string> SearchTopLevelCategoryKeys =
    [
        "realestate",
        "vehicle",
        "electronics",
        "fashion",
        "secondhand",
        "pets",
        "adoption",
        "services",
        "tutoring",
        "jobs"
    ];

    private static readonly Dictionary<string, string> CategoryTranslationKeys = new(Comparer)
    {
        ["estate"] = "cat_estate",
        ["realestate"] = "cat_realestate",
        ["land"] = "cat_land",
        ["vehicle"] = "cat_vehicle",
        ["yacht"] = "cat_yacht",
        ["caravan"] = "cat_caravan",
        ["secondhand"] = "cat_secondhand",
        ["parts"] = "cat_parts",
        ["phone"] = "cat_phone",
        ["computer"] = "cat_computer",
        ["watch"] = "cat_watch",
        ["jewelry"] = "cat_jewelry",
        ["electronics"] = "cat_electronics",
        ["equipment"] = "cat_equipment",
        ["home"] = "cat_home",
        ["furniture"] = "cat_furniture",
        ["fashion"] = "cat_fashion",
        ["services"] = "cat_services",
        ["tutoring"] = "cat_tutoring",
        ["jobs"] = "cat_jobs",
        ["pets"] = "cat_pets",
        ["helper"] = "cat_helper",
        ["other"] = "cat_other",
        ["adoption"] = "cat_adoption"
    };

    private static readonly Dictionary<string, IReadOnlyList<string>> SearchCategoriesByListingType = new(Comparer)
    {
        ["sale"] = ["realestate", "vehicle", "electronics", "fashion", "secondhand", "pets"],
        ["rent"] = ["realestate", "vehicle"],
        ["daily"] = ["realestate", "vehicle"],
        ["service"] = ["services"],
        ["lesson"] = ["tutoring"],
        ["job"] = ["jobs"],
        ["adoption"] = ["adoption"]
    };

    private static readonly IReadOnlyList<SearchTabDefinition> HomeSearchTabs =
    [
        new() { Key = "sale", ListingType = "sale" },
        new() { Key = "rent", ListingType = "rent" },
        new() { Key = "service", ListingType = "service", PresetCategory = "services" },
        new() { Key = "lesson", ListingType = "lesson", PresetCategory = "tutoring" },
        new() { Key = "job", ListingType = "job", PresetCategory = "jobs" },
        new() { Key = "adoption", ListingType = "adoption", PresetCategory = "pets" }
    ];

    private static readonly Dictionary<string, string> SubCategoryDisplayLabels = new(Comparer)
    {
        ["anne-bebek"] = "Anne & Bebek",
        ["bahce-bakimi"] = "Bahçe Bakımı",
        ["gunluk-kiralik"] = "Günlük Kiralık",
        ["tasima"] = "Taşıma",
        ["bahce-ekipmani"] = "Bahçe Ekipmanı",
        ["bahce-malzemeleri"] = "Bahçe Malzemeleri",
        ["bahce-mobilyasi"] = "Bahçe Mobilyası",
        ["bakim-urunleri"] = "Bakım Ürünleri",
        ["boya-badana"] = "Boya / Badana",
        ["cicek-bitki"] = "Çiçek & Bitki",
        ["cocuk-bakimi"] = "Çocuk Bakımı",
        ["cocuk-odasi"] = "Çocuk Odası",
        ["dijital-hizmet"] = "Dijital Hizmet",
        ["elektrik-tesisat"] = "Elektrik / Tesisat",
        ["elektrikli-ev-aletleri"] = "Elektrikli Ev Aletleri",
        ["ev-yardimcisi"] = "Ev Yardımcısı",
        ["ev-yasam"] = "Ev Yaşam",
        ["fen-bilimleri"] = "Fen Bilimleri",
        ["yazilim"] = "Yazılım",
        ["diger"] = "Diğer",
        ["guzellik-bakim"] = "Güzellik & Bakım",
        ["hasta-bakimi"] = "Hasta Bakımı",
        ["hirdavat-el-aleti"] = "Hırdavat / El Aleti",
        ["ilkokul-ortaokul"] = "İlkokul / Ortaokul",
        ["is-makinesi"] = "İş Makinesi",
        ["kamyonet"] = "Kamyonet",
        ["kitap-dergi"] = "Kitap & Dergi",
        ["kitap-muzik-oyun"] = "Kitap / Müzik / Oyun",
        ["kamera-ses"] = "Kamera & Ses",
        ["kaynak-kompresor"] = "Kaynak / Kompresör",
        ["lastik-jant"] = "Lastik & Jant",
        ["minibus"] = "Minibüs",
        ["motosiklet"] = "Motosiklet",
        ["masaustu-bilgisayar"] = "Masaüstü Bilgisayar",
        ["motosiklet-ekipman"] = "Motosiklet Ekipmanı",
        ["muzik-enstruman"] = "Müzik Enstrümanı",
        ["muzik-sanat"] = "Müzik / Sanat",
        ["ofis-esyasi"] = "Ofis Eşyası",
        ["ofis-kirtasiye"] = "Ofis & Kırtasiye",
        ["ofis-yardimci"] = "Ofis Yardımcısı",
        ["oyun-konsolu"] = "Oyun Konsolu",
        ["pickup"] = "Pick-up",
        ["platform-vinc"] = "Platform & Vinç",
        ["proje-bazli"] = "Proje Bazlı",
        ["mucevher"] = "Mücevher",
        ["sanayi-ekipman"] = "Sanayi Ekipmanı",
        ["saat"] = "Saat",
        ["suv"] = "SUV",
        ["yat"] = "Yat / Tekne",
        ["ses-goruntu"] = "Ses ve Görüntü",
        ["sinav-hazirlik"] = "Sınav Hazırlık",
        ["diger"] = "Diğer",
        ["daire"] = "Daire",
        ["mustakil-ev"] = "Müstakil Ev",
        ["villa"] = "Villa",
        ["ikiz-villa"] = "İkiz Villa",
        ["bina"] = "Bina",
        ["bahceli"] = "Bahçeli",

    };

    private static readonly Dictionary<string, string> TurkishWordDisplayMap = new(Comparer)
    {
        ["bakici"] = "bakıcı",
        ["bakim"] = "bakım",
        ["bahce"] = "bahçe",
        ["cicek"] = "çiçek",
        ["cocuk"] = "çocuk",
        ["ogrenci"] = "öğrenci",
        ["yazlik"] = "yazlık",
        ["magaza"] = "mağaza",
        ["dukan"] = "dükkan",
        ["arac"] = "araç",
        ["saglik"] = "sağlık",
        ["endustriyel"] = "endüstriyel",
        ["danismanlik"] = "danışmanlık",
        ["egitim"] = "eğitim",
        ["enstruman"] = "enstrüman",
        ["esya"] = "eşya",
        ["goruntu"] = "görüntü",
        ["guzellik"] = "güzellik",
        ["hazirlik"] = "hazırlık",
        ["hirdavat"] = "hırdavat",
        ["is"] = "iş",
        ["jant"] = "jant",
        ["jenerator"] = "jeneratör",
        ["kirtasiye"] = "kırtasiye",
        ["kopek"] = "köpek",
        ["kompresor"] = "kompresör",
        ["kus"] = "kuş",
        ["makinesi"] = "makinesi",
        ["masaustu"] = "masaüstü",
        ["muzik"] = "müzik",
        ["odasi"] = "odası",
        ["ozel"] = "özel",
        ["parca"] = "parça",
        ["surungen"] = "sürüngen",
        ["sinav"] = "sınav",
        ["tarim"] = "tarım",
        ["universite"] = "üniversite",
        ["vinc"] = "vinç",
        ["yardimci"] = "yardımcı",
        ["yardimcisi"] = "yardımcısı",
        ["yari"] = "yarı",
        ["yasli"] = "yaşlı",
        ["yatili"] = "yatılı",
        ["zamanli"] = "zamanlı"
    };

    private static readonly Dictionary<string, ListingCategoryTaxonomy> Categories = new(Comparer)
    {
        ["estate"] = new ListingCategoryTaxonomy
        {
            Key = "estate",
            SubCategories = ["konut", "daire", "mustakil-ev", "villa", "ikiz-villa", "bina", "bahceli", "ticari", "arsa", "gunluk-kiralik", "magaza-dukan", "ofis", "depo-ambar", "endustriyel-sahalar", "kat-otel", "ticari-arazi", "gastronomi-mekanlari", "saglik-tesisleri", "diger"],
            ListingTypes = ["sale", "rent", "daily"]
        },
        ["land"] = new ListingCategoryTaxonomy
        {
            Key = "land",
            SubCategories = ["arsa"],
            ListingTypes = ["sale", "rent"]
        },
        ["vehicle"] = new ListingCategoryTaxonomy
        {
            Key = "vehicle",
            SubCategories = ["araba", "suv", "ticari-arac", "kamyonet", "minibus", "van", "pickup", "motosiklet", "is-makinesi", "yat", "karavan"],
            ListingTypes = ["sale", "rent", "daily"]
        },
        ["parts"] = new ListingCategoryTaxonomy
        {
            Key = "parts",
            SubCategories = ["yedek-parca", "aksesuar", "modifiye", "lastik-jant", "ses-goruntu", "motosiklet-ekipman", "bakim-urunleri", "navigasyon"],
            ListingTypes = ["sale"],
            RequiresProductCondition = true
        },
        ["secondhand"] = new ListingCategoryTaxonomy
        {
            Key = "secondhand",
            SubCategories = ["ev-yasam", "bahce-ekipmani", "hirdavat-el-aleti", "koleksiyon", "spor-outdoor", "anne-bebek", "kitap-muzik-oyun", "ofis-kirtasiye"],
            ListingTypes = ["sale"],
            RequiresProductCondition = true
        },
        ["equipment"] = new ListingCategoryTaxonomy
        {
            Key = "equipment",
            SubCategories = ["forklift", "sanayi-ekipman", "tarim-makineleri", "platform-vinc"],
            ListingTypes = ["sale", "rent"]
        },
        ["services"] = new ListingCategoryTaxonomy
        {
            Key = "services",
            SubCategories = ["temizlik", "tasima", "tamir", "egitim", "danismanlik", "organizasyon", "elektrik-tesisat", "boya-badana", "guzellik-bakim", "dijital-hizmet", "diger"],
            ListingTypes = ["service"]
        },
        ["tutoring"] = new ListingCategoryTaxonomy
        {
            Key = "tutoring",
            SubCategories = ["matematik", "dil", "sinav-hazirlik", "muzik-sanat", "ilkokul-ortaokul", "fen-bilimleri", "universite-dersleri", "yazilim"],
            ListingTypes = ["lesson"]
        },
        ["jobs"] = new ListingCategoryTaxonomy
        {
            Key = "jobs",
            SubCategories = ["tam-zamanli", "yari-zamanli", "freelance", "staj", "uzaktan", "vardiyali", "proje-bazli"],
            ListingTypes = ["job"]
        },
        ["pets"] = new ListingCategoryTaxonomy
        {
            Key = "pets",
            SubCategories = ["kedi", "kopek", "kus", "akvaryum", "malzeme", "kemirgen", "surungen"],
            ListingTypes = ["sale", "adoption"],
            RequiresProductCondition = true
        },
        ["helper"] = new ListingCategoryTaxonomy
        {
            Key = "helper",
            SubCategories = ["ev-yardimcisi", "bakici", "yasli-bakimi", "cocuk-bakimi", "ofis-yardimci", "hasta-bakimi", "yatili-yardimci", "bahce-bakimi"],
            ListingTypes = ["job"]
        },
        ["electronics"] = new ListingCategoryTaxonomy
        {
            Key = "electronics",
            SubCategories = ["telefon", "tablet", "bilgisayar", "laptop", "masaustu-bilgisayar", "oyun-konsolu", "kamera-ses", "televizyon", "beyaz-esya", "elektrikli-ev-aletleri", "aksesuar"],
            ListingTypes = ["sale"],
            RequiresProductCondition = true
        },
        ["furniture"] = new ListingCategoryTaxonomy
        {
            Key = "furniture",
            SubCategories = ["koltuk", "yemek-odasi", "yatak-odasi", "beyaz-esya", "bahce-mobilyasi", "dekorasyon", "ofis-esyasi", "mutfak", "cocuk-odasi"],
            ListingTypes = ["sale"],
            RequiresProductCondition = true
        },
        ["other"] = new ListingCategoryTaxonomy
        {
            Key = "other",
            SubCategories = ["hobi", "koleksiyon", "spor", "anne-bebek", "bahce-malzemeleri", "cicek-bitki", "hirdavat", "kitap-dergi", "muzik-enstruman", "genel"],
            ListingTypes = ["sale"],
            RequiresProductCondition = true
        }
    };

    private static readonly Dictionary<string, PublishProductFieldVisibility> ProductFieldVisibilityMap = new(Comparer)
    {
        ["parts"] = DefaultProductFieldVisibility,
        ["secondhand"] = DefaultProductFieldVisibility,
        ["equipment"] = DefaultProductFieldVisibility,
        ["services"] = DefaultProductFieldVisibility,
        ["electronics"] = DefaultProductFieldVisibility,
        ["furniture"] = DefaultProductFieldVisibility,
        ["other"] = DefaultProductFieldVisibility,
        ["tutoring"] = new PublishProductFieldVisibility
        {
            ShowBrand = true,
            ShowModel = true,
            ShowWarranty = true,
            ShowSerial = false,
            ShowUsage = true
        },
        ["jobs"] = new PublishProductFieldVisibility
        {
            ShowBrand = true,
            ShowModel = true,
            ShowWarranty = true,
            ShowSerial = false,
            ShowUsage = false
        },
        ["helper"] = new PublishProductFieldVisibility
        {
            ShowBrand = true,
            ShowModel = true,
            ShowWarranty = true,
            ShowSerial = false,
            ShowUsage = true
        },
        ["pets"] = new PublishProductFieldVisibility
        {
            ShowBrand = false,
            ShowModel = true,
            ShowWarranty = true,
            ShowSerial = true,
            ShowUsage = true
        }
    };

    private static readonly Dictionary<string, string> PublishCategoryAliases = new(Comparer)
    {
        ["emlak"] = "estate",
        ["realestate"] = "estate",
        ["estate"] = "estate",
        ["arsa"] = "land",
        ["land"] = "land",
        ["vasita"] = "vehicle",
        ["vehicle"] = "vehicle",
        ["arac"] = "vehicle",
        ["otomobil"] = "vehicle",
        ["yedekparca"] = "parts",
        ["yedek-parca"] = "parts",
        ["parts"] = "parts",
        ["ikinciel"] = "secondhand",
        ["ikinci-el"] = "secondhand",
        ["secondhand"] = "secondhand",
        ["ismakinesi"] = "equipment",
        ["is-makinesi"] = "equipment",
        ["equipment"] = "equipment",
        ["hizmet"] = "services",
        ["service"] = "services",
        ["services"] = "services",
        ["ozelders"] = "tutoring",
        ["ozel-ders"] = "tutoring",
        ["tutoring"] = "tutoring",
        ["isilanlari"] = "jobs",
        ["is-ilanlari"] = "jobs",
        ["jobs"] = "jobs",
        ["hayvanlar"] = "pets",
        ["pets"] = "pets",
        ["yardimci"] = "helper",
        ["helper"] = "helper",
        ["elektronik"] = "electronics",
        ["electronics"] = "electronics",
        ["phone"] = "electronics",
        ["computer"] = "electronics",
        ["watch"] = "electronics",
        ["jewelry"] = "electronics",
        ["mobilya"] = "furniture",
        ["furniture"] = "furniture",
        ["home"] = "furniture",
        ["diger"] = "other",
        ["other"] = "other",
        ["fashion"] = "other"
    };

    private static readonly Dictionary<string, IReadOnlyList<string>> CategoryPhotoMap = new(Comparer)
    {
        ["realestate"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Katsura_Imperial_Villa_in_Spring.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Villa_Medici_a_Fiesole_1.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/2019.07.10_metro_California-housing_Blog-post_related.webp",
            "https://commons.wikimedia.org/wiki/Special:FilePath/West_side_of_Manhattan_from_Hudson_Commons_(95103p).jpg"
        ],
        ["land"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Farmland_Ready_For_Paddy_Cultivation.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Iglesia_de_Nuestra_Se%C3%B1ora_de_La_Blanca%2C_Cardej%C3%B3n%2C_Espa%C3%B1a%2C_2012-09-01%2C_DD_02.JPG",
            "https://commons.wikimedia.org/wiki/Special:FilePath/%D0%9A%D1%80%D0%B0%D1%81%D0%BD%D0%BE%D0%BA%D0%BE%D0%B2%D1%8B%D0%BB%D1%8C%D0%BD%D0%B0%D1%8F_%D1%81%D1%82%D0%B5%D0%BF%D1%8C_%D0%B2_%D0%B1%D0%B0%D1%81%D1%81%D0%B5%D0%B9%D0%BD%D0%B5_%D0%9A%D1%83%D0%BA%D1%83%D0%B9%D0%BA%D0%B8_%D0%B2_%D0%9A%D1%83%D1%80%D1%8C%D0%B8%D0%BD%D1%81%D0%BA%D0%BE%D0%BC_%D1%80%D0%B0%D0%B9%D0%BE%D0%BD%D0%B5.JPG"
        ],
        ["vehicle"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/1925_Ford_Model_T_touring.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/IBMTorontoSoftwareLabEVChargers4.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/97-01_Jeep_Cherokee.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Ford_F-150_crew_cab_--_05-28-2011.jpg"
        ],
        ["yacht"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Bavaria_Cruiser_45.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Motorboat_at_Kankaria_lake.JPG"
        ],
        ["caravan"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Caravan.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/White_Fiat_Ducato_Campervan_2006.jpg"
        ],
        ["equipment"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Tzama02.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Excavator_Postiguet_Beach_2.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Backhoe_loader_Cat420E_left.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/CatD9T.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Ford_8N.jpg"
        ],
        ["electronics"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Laptop_collage.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Wikipedia_homepage_on_a_large_Android_phone%2C_2015-04-16.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/IPad_Mini_6_-_1.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Cptvdisplay.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/LG_%EB%93%9C%EB%9F%BC%EC%84%B8%ED%83%81%EA%B8%B0%EC%99%80_%EC%8B%9D%EA%B8%B0%EC%84%B8%EC%B2%99%EA%B8%B0%2C_%EC%98%81%EA%B5%AD%EC%84%9C_%EB%AC%BC%EC%82%AC%EC%9A%A9_%ED%9A%A8%EC%9C%A8_%EC%B5%9C%EC%9A%B0%EC%88%98_%EC%A0%9C%ED%92%88_%EC%88%98%EC%83%81.jpg"
        ],
        ["phone"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Wikipedia_homepage_on_a_large_Android_phone%2C_2015-04-16.jpg"
        ],
        ["computer"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Laptop_collage.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/IPad_Mini_6_-_1.jpg"
        ],
        ["home"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Sittingroom-edit1.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/La_cuisine_(mus%C3%A9e_dart_nouveau%2C_Riga)_(7563655820).jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Heiligengrabe%2C_Kloster_Stift_zum_Heiligengrabe%2C_Abtei%2C_Speiseraum_--_2017_--_7082-8.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Berlin_Villa_Borsig_Tegel_asv2019-08_img09.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/2009-05-16_Main_office_lobby_at_Hampton_Forest_Apartments.jpg"
        ],
        ["services"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Plumber_at_work.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Classroom_at_a_seconday_school_in_Pendembu_Sierra_Leone_Adapted.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/FEMA_-_42428_-_Home_Repair_after_Flood.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/West_side_of_Manhattan_from_Hudson_Commons_(95103p).jpg"
        ],
        ["tutoring"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Classroom_at_a_seconday_school_in_Pendembu_Sierra_Leone_Adapted.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Plumber_at_work.jpg"
        ],
        ["jobs"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/West_side_of_Manhattan_from_Hudson_Commons_(95103p).jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Classroom_at_a_seconday_school_in_Pendembu_Sierra_Leone_Adapted.jpg"
        ],
        ["helper"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/FEMA_-_42428_-_Home_Repair_after_Flood.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Plumber_at_work.jpg"
        ],
        ["secondhand"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Puces_de_Montsoreau.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Yard_Sale_Northern_CA_2005.JPG",
            "https://commons.wikimedia.org/wiki/Special:FilePath/06_Restoration_of_gilded_mirror_in_Muzeum_Gornoslaskie%2C_Bytom%2C_Poland_-_furniture_restorer_working.jpg"
        ],
        ["fashion"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Pectoral_and_Necklace_of_Sithathoryunet_with_the_Name_of_Senwosret_II_MET_DT531.jpg"
        ],
        ["jewelry"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Pectoral_and_Necklace_of_Sithathoryunet_with_the_Name_of_Senwosret_II_MET_DT531.jpg"
        ],
        ["watch"] =
        [
            "https://commons.wikimedia.org/wiki/Special:FilePath/Pocket_Watch_(Savonette).jpg"
        ]
    };

    public static IReadOnlyList<string> HeroRotationImages { get; } = BuildHeroRotationImages();

    public static IReadOnlyList<string> GetSearchCategoryKeys()
    {
        return SearchTopLevelCategoryKeys;
    }

    public static IReadOnlyList<string> GetSearchListingTypes()
    {
        return SearchListingTypes;
    }

    public static IReadOnlyList<SearchTabDefinition> GetHomepageSearchTabs()
    {
        return HomeSearchTabs;
    }

    public static Dictionary<string, List<string>> BuildSearchCategoryMapByListingType(IEnumerable<string> categoryKeys)
    {
        var availableCategories = categoryKeys
            .Select(NormalizeSearchCategory)
            .Where(item => !string.IsNullOrWhiteSpace(item) && !string.Equals(item, "all", StringComparison.OrdinalIgnoreCase))
            .Distinct(Comparer)
            .ToList();

        static List<string> WithAllPrefix(IEnumerable<string> items)
        {
            var result = new List<string> { "all" };
            result.AddRange(items);
            return result;
        }

        List<string> FilterAvailable(IEnumerable<string> requested)
        {
            return availableCategories
                .Where(item => requested.Contains(item, Comparer))
                .ToList();
        }

        var result = new Dictionary<string, List<string>>(Comparer)
        {
            ["all"] = WithAllPrefix(availableCategories)
        };

        foreach (var pair in SearchCategoriesByListingType)
        {
            result[pair.Key] = WithAllPrefix(FilterAvailable(pair.Value));
        }

        return result;
    }

    public static string GetCategoryTranslationKey(string? categoryCode)
    {
        var normalizedSearchCategory = NormalizeSearchCategory(categoryCode);
        if (SearchCategoryDefinitionMap.TryGetValue(normalizedSearchCategory, out var searchDefinition))
        {
            return searchDefinition.TranslationKey;
        }

        var publishCategory = NormalizePublishCategory(categoryCode);
        return CategoryTranslationKeys.TryGetValue(publishCategory, out var translationKey)
            ? translationKey
            : CategoryTranslationKeys.TryGetValue(normalizedSearchCategory, out var searchTranslationKey)
                ? searchTranslationKey
                : "allCategories";
    }

    public static bool TryResolveSearchCategoryFromSlug(string? slug, out string categoryCode)
    {
        var normalizedSlug = NormalizeText(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            categoryCode = string.Empty;
            return false;
        }

        foreach (var definition in SearchCategoryDefinitions)
        {
            if (string.Equals(normalizedSlug, NormalizeText(definition.Key), StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedSlug, NormalizeText(definition.DefaultSlug), StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(definition.EnglishSlug) && string.Equals(normalizedSlug, NormalizeText(definition.EnglishSlug), StringComparison.OrdinalIgnoreCase)))
            {
                categoryCode = definition.Key;
                return true;
            }
        }

        categoryCode = string.Empty;
        return false;
    }

    public static string GetSearchCategorySlug(string? categoryCode, string? cultureCode = null)
    {
        var normalizedCategory = NormalizeSearchCategory(categoryCode);
        if (!SearchCategoryDefinitionMap.TryGetValue(normalizedCategory, out var definition))
        {
            return "kategori";
        }

        return string.Equals(cultureCode, "en", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(definition.EnglishSlug)
            ? definition.EnglishSlug!
            : definition.DefaultSlug;
    }

    public static string GetSearchCategoryHeroImage(string? categoryCode)
    {
        var normalizedCategory = NormalizeSearchCategory(categoryCode);
        return SearchCategoryDefinitionMap.TryGetValue(normalizedCategory, out var definition)
            ? definition.HeroImageUrl
            : "https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=1800&q=80";
    }

    public static string GetCategoryCardImageUrl(string? category)
    {
        var normalized = NormalizeSearchCategory(category);
        return CategoryPhotoMap.TryGetValue(normalized, out var images) && images.Count > 0
            ? images[0]
            : HeroRotationImages[0];
    }

    public static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ş', 's')
            .Replace('ö', 'o')
            .Replace('ç', 'c');
    }

    public static string NormalizeSearchCategory(string? category)
    {
        var normalized = NormalizeText(category);

        return normalized switch
        {
            "" or "all" => "all",
            "estate" or "realestate" or "emlak" => "realestate",
            "land" or "arsa" => "land",
            "vehicle" or "vasita" or "arac" or "otomobil" or "yat-tekne" => "vehicle",
            "electronics" or "electronic" or "elektronik" => "electronics",
            "phone" => "phone",
            "computer" => "computer",
            "watch" => "watch",
            "jewelry" => "jewelry",
            "equipment" or "is-makinesi" => "equipment",
            "home" or "furniture" or "mobilya" => "home",
            "fashion" => "fashion",
            "service" or "services" or "hizmet" => "services",
            "tutoring" or "ozel-ders" => "tutoring",
            "jobs" or "is-ilanlari" => "jobs",
            "helper" or "yardimci" => "helper",
            "secondhand" or "other" or "diger" => "secondhand",
            "pets" or "hayvanlar" => "pets",
            "adoption" or "sahiplendirme" => "adoption",
            "yacht" => "yacht",
            "caravan" => "caravan",
            _ => normalized
        };
    }

    public static string NormalizePublishCategory(string? category)
    {
        var normalized = NormalizeText(category);
        return PublishCategoryAliases.TryGetValue(normalized, out var mapped)
            ? mapped
            : normalized;
    }

    public static string NormalizeListingType(string? type) => NormalizeText(type);

    public static bool IsPetAdoption(string? category, string? type)
    {
        return NormalizePublishCategory(category) == "pets"
            && NormalizeListingType(type) == "adoption";
    }

    public static string? NormalizeSubCategory(string? subCategory)
    {
        if (string.IsNullOrWhiteSpace(subCategory))
        {
            return null;
        }

        return NormalizeText(subCategory)
            .Replace(' ', '-')
            .Replace("--", "-");
    }

    public static (string Category, string? SubCategory) NormalizeForPersistence(string? category, string? subCategory)
    {
        var normalizedCategory = NormalizePublishCategory(category);
        var normalizedSubCategory = NormalizeSubCategory(subCategory);

        if (normalizedCategory == "land")
        {
            return ("land", "arsa");
        }

        if (normalizedCategory == "yacht")
        {
            return ("vehicle", "yat");
        }

        if (normalizedCategory == "caravan")
        {
            return ("vehicle", "karavan");
        }

        if (normalizedCategory == "vehicle")
        {
            if (string.IsNullOrWhiteSpace(normalizedSubCategory))
            {
                return ("vehicle", "araba");
            }

            return IsValidSubCategory("vehicle", normalizedSubCategory)
                ? ("vehicle", normalizedSubCategory)
                : ("vehicle", null);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSubCategory)
            && !IsValidSubCategory(normalizedCategory, normalizedSubCategory))
        {
            normalizedSubCategory = null;
        }

        return (normalizedCategory, normalizedSubCategory);
    }

    public static IReadOnlyList<string> GetAllowedListingTypes(string? category)
    {
        var key = NormalizePublishCategory(category);
        // Keep 'land' as its own publish category (do not collapse to 'estate')
        return Categories.TryGetValue(key, out var definition)
            ? definition.ListingTypes
            : Array.Empty<string>();
    }

    public static IReadOnlyList<string> GetAllowedSubCategories(string? category)
    {
        var key = NormalizePublishCategory(category);
        // Keep 'land' as its own publish category (do not collapse to 'estate')
        return Categories.TryGetValue(key, out var definition)
            ? definition.SubCategories
            : Array.Empty<string>();
    }

    public static bool RequiresSubCategory(string? category) => GetAllowedSubCategories(category).Count > 0;

    public static bool RequiresProductCondition(string? category)
    {
        var key = NormalizePublishCategory(category);
        return Categories.TryGetValue(key, out var definition) && definition.RequiresProductCondition;
    }

    public static bool IsEstateCategory(string? category) => NormalizeForPersistence(category, null).Category == "estate";

    public static bool IsVehicleCategory(string? category) => NormalizePublishCategory(category) == "vehicle";

    public static bool IsProductPanelCategory(string? category)
    {
        var normalized = NormalizePublishCategory(category);
        return normalized is "parts" or "secondhand" or "equipment" or "services" or "tutoring" or "jobs" or "pets" or "helper" or "electronics" or "furniture" or "other";
    }

    public static PublishProductFieldVisibility GetPublishProductFieldVisibility(string? category)
    {
        var normalized = NormalizePublishCategory(category);
        return ProductFieldVisibilityMap.TryGetValue(normalized, out var visibility)
            ? visibility
            : DefaultProductFieldVisibility;
    }

    public static bool IsResidentialEstate(string? category, string? subCategory)
    {
        var normalized = NormalizeForPersistence(category, subCategory);
        return normalized.Category == "estate" && string.Equals(normalized.SubCategory, "konut", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLandEstate(string? category, string? subCategory)
    {
        var normalized = NormalizeForPersistence(category, subCategory);
        return normalized.Category == "land" && string.Equals(normalized.SubCategory, "arsa", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidListingType(string? category, string? type)
    {
        var normalizedType = NormalizeListingType(type);
        if (string.IsNullOrWhiteSpace(normalizedType))
        {
            return false;
        }

        return GetAllowedListingTypes(category).Contains(normalizedType, Comparer);
    }

    public static bool IsValidSubCategory(string? category, string? subCategory)
    {
        var allowedValues = GetAllowedSubCategories(category);
        var normalizedSubCategory = NormalizeSubCategory(subCategory);

        if (allowedValues.Count == 0)
        {
            return string.IsNullOrWhiteSpace(normalizedSubCategory);
        }

        return !string.IsNullOrWhiteSpace(normalizedSubCategory)
            && allowedValues.Contains(normalizedSubCategory, Comparer);
    }

    public static string GetBrowseCategoryCode(string? category, string? subCategory)
    {
        var rawNormalized = NormalizeText(category);
        if (rawNormalized is "phone" or "computer")
        {
            return "electronics";
        }

        if (rawNormalized is "watch" or "jewelry")
        {
            return "fashion";
        }

        if (rawNormalized is "home" or "fashion" or "services" or "tutoring" or "jobs" or "helper" or "pets")
        {
            return rawNormalized;
        }

        if (rawNormalized is "yacht" or "caravan")
        {
            return "vehicle";
        }

        var normalizedCategory = NormalizePublishCategory(category);
        var normalizedSubCategory = NormalizeSubCategory(subCategory);

        return normalizedCategory switch
        {
            "estate" => "realestate",
            "land" => "land",
            "vehicle" => "vehicle",
            "parts" => "vehicle",
            "electronics" => "electronics",
            "furniture" => "home",
            "services" => "services",
            "tutoring" => "tutoring",
            "jobs" => "jobs",
            "helper" => "helper",
            "equipment" => "equipment",
            "secondhand" or "other" or "pets" => "secondhand",
            _ => NormalizeSearchCategory(category)
        };
    }

    public static bool MatchesSearchCategory(string? requestedCategory, string? listingCategory, string? listingSubCategory)
    {
        var requested = NormalizeSearchCategory(requestedCategory);
        if (requested == "all")
        {
            return true;
        }

        var browseCategory = GetBrowseCategoryCode(listingCategory, listingSubCategory);
        var rawNormalized = NormalizeText(listingCategory);
        var normalizedCategory = NormalizePublishCategory(listingCategory);
        var normalizedSubCategory = NormalizeSubCategory(listingSubCategory);

        return requested switch
        {
            "realestate" => browseCategory == "realestate" || normalizedSubCategory == "arsa",
            "land" => browseCategory == "land" || normalizedSubCategory == "arsa",
            "vehicle" => browseCategory == "vehicle",
            "electronics" => browseCategory == "electronics",
            "phone" => rawNormalized == "phone" || (browseCategory == "electronics" && normalizedSubCategory == "telefon"),
            "computer" => rawNormalized == "computer" || (browseCategory == "electronics" && normalizedSubCategory is "bilgisayar" or "laptop" or "masaustu-bilgisayar"),
            "watch" => rawNormalized == "watch" || (browseCategory == "fashion" && normalizedSubCategory == "saat"),
            "jewelry" => rawNormalized == "jewelry" || (browseCategory == "fashion" && normalizedSubCategory == "mucevher"),
            "services" => normalizedCategory == "services",
            "tutoring" => normalizedCategory == "tutoring",
            "jobs" => normalizedCategory == "jobs",
            "helper" => normalizedCategory == "helper",
            "fashion" => browseCategory == "fashion",
            "secondhand" => browseCategory == "secondhand",
            "pets" => browseCategory == "pets",
            "adoption" => rawNormalized == "pets" || browseCategory == "pets",
            _ => browseCategory == requested
        };
    }

    public static bool MatchesSearchSubCategory(string? requestedCategory, IEnumerable<string> requestedSubCategories, string? listingCategory, string? listingSubCategory)
    {
        var requestedTerms = requestedSubCategories
            .Select(NormalizeSubCategory)
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Select(term => term!)
            .ToHashSet(Comparer);

        if (requestedTerms.Count == 0)
        {
            return true;
        }

        var normalizedListingSubCategory = NormalizeSubCategory(listingSubCategory);
        if (!string.IsNullOrWhiteSpace(normalizedListingSubCategory) && requestedTerms.Contains(normalizedListingSubCategory))
        {
            return true;
        }

        var normalizedListingCategory = NormalizeText(listingCategory);
        return NormalizeSearchCategory(requestedCategory) switch
        {
            "vehicle" =>
                requestedTerms.Contains("araba") && normalizedListingCategory == "vehicle"
                || requestedTerms.Contains("yat") && normalizedListingCategory == "yacht"
                || requestedTerms.Contains("karavan") && normalizedListingCategory == "caravan",
            "electronics" =>
                (requestedTerms.Contains("telefon") && normalizedListingCategory == "phone")
                || (requestedTerms.Overlaps(["bilgisayar", "laptop", "masaustu-bilgisayar"]) && normalizedListingCategory == "computer"),
            "fashion" =>
                (requestedTerms.Contains("saat") && normalizedListingCategory == "watch")
                || (requestedTerms.Contains("mucevher") && normalizedListingCategory == "jewelry"),
            _ => false
        };
    }

    public static Dictionary<string, List<SubCategoryFilter>> BuildSearchCategorySubCategoryMap(IEnumerable<string> categoryKeys)
    {
        var result = new Dictionary<string, List<SubCategoryFilter>>(Comparer);

        foreach (var categoryKey in categoryKeys.Where(x => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, "all", StringComparison.OrdinalIgnoreCase)))
        {
            result[categoryKey] = GetSearchSubCategoryFilters(categoryKey);
        }

        return result;
    }

    public static List<SubCategoryFilter> GetSearchSubCategoryFilters(string? searchCategory)
    {
        return NormalizeSearchCategory(searchCategory) switch
        {
            "realestate" => ToFilters(new[] { "konut", "ticari", "arsa", "gunluk-kiralik" }),
            "land" => ToFilters(["arsa"]),
            "vehicle" => ToFilters(GetAllowedSubCategories("vehicle")),
            "yacht" => ToFilters(GetAllowedSubCategories("vehicle")),
            "caravan" => ToFilters(GetAllowedSubCategories("vehicle")),
            "electronics" => ToFilters(GetAllowedSubCategories("electronics")),
            "phone" => ToFilters(["telefon"]),
            "computer" => ToFilters(["bilgisayar", "laptop", "masaustu-bilgisayar"]),
            "home" => ToFilters(MergeDistinctValues(GetAllowedSubCategories("furniture"), new[] { "ev-yasam" })),
            "services" => ToFilters(GetAllowedSubCategories("services")),
            "tutoring" => ToFilters(GetAllowedSubCategories("tutoring")),
            "jobs" => ToFilters(GetAllowedSubCategories("jobs")),
            "helper" => ToFilters(GetAllowedSubCategories("helper")),
            "equipment" => ToFilters(GetAllowedSubCategories("equipment")),
            "secondhand" => ToFilters(MergeDistinct("secondhand", "other")),
            "fashion" => ToFilters(["saat", "mucevher"]),
            "pets" => ToFilters(GetAllowedSubCategories("pets")),
            "adoption" => ToFilters(GetAllowedSubCategories("pets")),
            _ => []
        };
    }

    public static Dictionary<string, List<string>> BuildPublishSubCategoryValueMap()
    {
        return Categories.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.SubCategories.ToList(),
            Comparer);
    }

    public static Dictionary<string, List<string>> BuildPublishTypeValueMap()
    {
        return Categories.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ListingTypes.ToList(),
            Comparer);
    }

    public static Dictionary<string, PublishProductFieldVisibility> BuildPublishProductFieldVisibilityMap()
    {
        return ProductFieldVisibilityMap.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            Comparer);
    }

    public static string HumanizeSubCategory(string rawValue)
    {
        var normalized = NormalizeSubCategory(rawValue);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        if (SubCategoryDisplayLabels.TryGetValue(normalized, out var label))
        {
            return label;
        }

        var words = normalized
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(HumanizeWord);

        return string.Join(' ', words);
    }

    private static List<SubCategoryFilter> ToFilters(IEnumerable<string> rawValues)
    {
        return rawValues
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(Comparer)
            .Select(x => new SubCategoryFilter
            {
                Value = x,
                Label = HumanizeSubCategory(x)
            })
            .ToList();
    }

    private static List<string> MergeDistinct(params string[] categoryKeys)
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(Comparer);

        foreach (var categoryKey in categoryKeys)
        {
            foreach (var item in GetAllowedSubCategories(categoryKey))
            {
                if (seen.Add(item))
                {
                    merged.Add(item);
                }
            }
        }

        return merged;
    }

    private static List<string> MergeDistinctValues(params IEnumerable<string>[] valueSets)
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(Comparer);

        foreach (var valueSet in valueSets)
        {
            foreach (var value in valueSet)
            {
                if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
                {
                    merged.Add(value);
                }
            }
        }

        return merged;
    }

    private static string HumanizeWord(string rawWord)
    {
        if (TurkishWordDisplayMap.TryGetValue(rawWord, out var mappedWord))
        {
            return TurkishTextInfo.ToTitleCase(mappedWord);
        }

        return TurkishTextInfo.ToTitleCase(rawWord);
    }

    private static IReadOnlyList<string> BuildHeroRotationImages()
    {
        return new[]
        {
            "https://commons.wikimedia.org/wiki/Special:FilePath/Kyrenia_01-2017_img02_Castle_exterior.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Kyrenia_01-2017_img04_view_from_castle_bastion.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Villa_Medici_a_Fiesole_1.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Katsura_Imperial_Villa_in_Spring.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Farmland_Ready_For_Paddy_Cultivation.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/97-01_Jeep_Cherokee.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Ford_F-150_crew_cab_--_05-28-2011.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Trucking_semi_truck_under_overpass.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Excavator_Postiguet_Beach_2.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/White_Fiat_Ducato_Campervan_2006.jpg",
            "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1800&q=80",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Wikipedia_homepage_on_a_large_Android_phone%2C_2015-04-16.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Laptop_collage.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Cptvdisplay.jpg",
            "https://images.unsplash.com/photo-1545239351-1141bd82e8a6?auto=format&fit=crop&w=1800&q=80",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Sittingroom-edit1.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/A_modern_dinner_set,_table_and_chairs_in_a_beach_house,_Auckland_-_1028.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Garden_furniture_at_Nuthurst_West_Sussex_England.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Cordless_Drill_-_unbranded.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Hard-Core_drill-Hilti_DD-150-U_(hand-operated)-02ASD.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Pruning_shears.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Hand_tools_for_work.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Local_Tailor_at_Arambol,_Goa.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Window_cleaner_at_work_-_geograph.org.uk_-_3173658.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Software_Developer_at_work_01.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Town_gardener_in_Tomasz%C3%B3w_Mazowiecki,_Poland.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Toro_22_Inch_Recycler_Walk_Behind_Lawn_Mower.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Panasonic_HOME_REFRIGERATOR_NR-C320WP-N.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Cast_iron_Stothert_of_Bath_oven,_No_1_Royal_Crescent,_Bath.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Schiffsmodell_Belem2.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Watch_Collection_1.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/An_UAV_displayed_by_Vyomik_drones_at_Amaravati_Drone_summit_(01).jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Set_of_pots.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Students_and_teacher_in_a_high_school_classroom_in_North_Carolina_07.jpg",
            "https://commons.wikimedia.org/wiki/Special:FilePath/Mercedes-Benz_Axor_based_cement_mixer_truck.JPG"
        };
    }

    private static void AppendUnique(List<string> ordered, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || ordered.Any(existing => string.Equals(existing, imageUrl, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        ordered.Add(imageUrl);
    }
}