using System.Diagnostics;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;
using SEN_T_PAZAR.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace SEN_T_PAZAR.Controllers;

public class HomeController : Controller
{
    private const int MaxUploadImageDimension = 1600;
    private const int UploadJpegQuality = 78;

    private readonly ILogger<HomeController> _logger;
    private readonly IListingCatalogService _catalog;
    private readonly ApplicationDbContext _context;
    private readonly EmailSender _emailSender;
    private readonly SiteLocalizer _localizer;
    private readonly IWebHostEnvironment _env;
    private readonly ITextTranslationService _translationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IUploadStorageService _uploadStorage;
    private readonly IUserMessageAutomationService _userMessageAutomationService;
    private readonly IConfiguration _configuration;

    public HomeController(
        ILogger<HomeController> logger,
        IListingCatalogService catalog,
        ApplicationDbContext context,
        EmailSender emailSender,
        SiteLocalizer localizer,
        IWebHostEnvironment env,
        ITextTranslationService translationService,
        UserManager<ApplicationUser> userManager,
        IPushNotificationService pushNotificationService,
        IUploadStorageService uploadStorage,
        IUserMessageAutomationService userMessageAutomationService,
        IConfiguration configuration)
    {
        _logger = logger;
        _catalog = catalog;
        _context = context;
        _emailSender = emailSender;
        _localizer = localizer;
        _env = env;
        _translationService = translationService;
        _userManager = userManager;
        _pushNotificationService = pushNotificationService;
        _uploadStorage = uploadStorage;
        _userMessageAutomationService = userMessageAutomationService;
        _configuration = configuration;
    }

    private static string NormalizeSeoText(string? value)
    {
        return Regex.Replace((value ?? string.Empty).Trim(), "\\s+", " ");
    }

    private static string TrimSeoText(string? value, int maxLength = 160)
    {
        var normalized = NormalizeSeoText(value);
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length <= maxLength)
        {
            return normalized;
        }

        var shortened = normalized[..maxLength].TrimEnd();
        var lastSpaceIndex = shortened.LastIndexOf(' ');
        if (lastSpaceIndex > Math.Min(60, maxLength / 2))
        {
            shortened = shortened[..lastSpaceIndex];
        }

        return shortened.TrimEnd(' ', ',', '.', ';', '-', ':') + "...";
    }

    private static string BuildMetaKeywords(params string?[] values)
    {
        return string.Join(", ", values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeSeoText)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10));
    }

    private string BuildCanonicalUrl(string? path = null, string? queryString = null)
    {
        var canonicalPath = string.IsNullOrWhiteSpace(path) ? Request.Path.Value ?? "/" : path;
        if (!canonicalPath.StartsWith("/", StringComparison.Ordinal))
        {
            canonicalPath = "/" + canonicalPath;
        }

        var host = Request.Host.Host;
        var port = Request.Host.Port ?? -1;
        var uriBuilder = new UriBuilder(Request.Scheme, host, port, canonicalPath);

        if (!string.IsNullOrWhiteSpace(queryString))
        {
            var trimmedQuery = queryString.Trim();
            if (trimmedQuery.StartsWith("?", StringComparison.Ordinal))
            {
                trimmedQuery = trimmedQuery[1..];
            }
            uriBuilder.Query = trimmedQuery;
        }

        return uriBuilder.Uri.ToString();
    }

    private sealed record SeoLandingFaqDefinition(string Question, string Answer);

    private sealed record SeoLandingDefinition(
        string Topic,
        string LinkTitle,
        string LinkDescription,
        string MetaTitle,
        string MetaDescription,
        string HeroEyebrow,
        string HeroTitle,
        string HeroSubtitle,
        string Category,
        string ListingType,
        string SubCategory,
        string BodyTitle,
        string BodyText,
        string SecondaryText,
        string[] Highlights,
        string[] RelatedTopics,
        string[] TargetKeywords,
        SeoLandingFaqDefinition[] Faqs);

    private static readonly IReadOnlyDictionary<string, SeoLandingDefinition> SeoLandingDefinitions = BuildSeoLandingDefinitions();

    private static IReadOnlyDictionary<string, SeoLandingDefinition> BuildSeoLandingDefinitions()
    {
        return new Dictionary<string, SeoLandingDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["emlak"] = new(
                "emlak",
                "KKTC Emlak",
                "KKTC satılık ve kiralık emlak ilanlarını tek sayfada keşfedin.",
                "KKTC Emlak İlanları | Satılık ve Kiralık Evler | SEN-T Pazar",
                "KKTC emlak ilanları içinde satılık daire, kiralık ev, villa, arsa ve ticari mülk seçeneklerini Girne, Lefkoşa, İskele ve Gazimağusa odağında inceleyin.",
                "KKTC emlak aramalarında güçlü görünürlük",
                "KKTC Emlak İlanları",
                "KKTC satılık daire, kiralık ev, villa, arsa ve ticari mülk seçeneklerini tek sayfada karşılaştırın.",
                "realestate",
                "all",
                "all",
                "KKTC emlak aramalarında niyet odaklı sonuçlar",
                "Bu sayfa, KKTC emlak arayan kullanıcıların en çok baktığı satılık daire, kiralık ev, villa, arsa ve yatırım odaklı ilanları tek yerde toplar. Girne, Lefkoşa, İskele ve Gazimağusa gibi yüksek talep gören bölgelerdeki güncel ilanlara hızlı ulaşım sağlar.",
                "Emlak odaklı koleksiyon sayfası; fiyat karşılaştırması, lokasyon filtresi ve güncel ilan akışını aynı URL altında topladığı için hem kullanıcı niyetine hem de arama motoru eşleşmesine daha net cevap verir.",
                ["KKTC satılık daire ve villa ilanları", "KKTC kiralık ev ve rezidans seçenekleri", "Girne, Lefkoşa, İskele ve Gazimağusa filtreleri", "Yatırım ve oturum amaçlı emlak karşılaştırması"],
                ["kiralik", "satilik", "kiralik-ev", "satilik-arsa", "araba"],
                ["KKTC emlak", "KKTC satılık", "KKTC kiralık ev", "KKTC daire"],
                [
                    new("KKTC emlak sayfasında hangi ilanlar öne çıkıyor?", "Sayfa; satılık daire, kiralık ev, villa, arsa ve ticari mülk ilanlarını şehir ve ilan tipi filtresiyle birlikte sunar."),
                    new("KKTC emlak aramalarında neden ayrı bir landing page kullanılıyor?", "Ayrı landing page; kısa URL, net başlık ve ilgili içerik sayesinde hem kullanıcı niyetini hem de arama motoru sinyallerini güçlendirir.")
                ]),
            ["kiralik"] = new(
                "kiralik",
                "KKTC Kiralık",
                "KKTC kiralık ev, daire ve ofis ilanlarını filtreleyin.",
                "KKTC Kiralık Ev ve Daire İlanları | SEN-T Pazar",
                "KKTC kiralık ilanları içinde kiralık ev, daire, rezidans, ofis ve ticari alan seçeneklerini şehir ve bütçeye göre filtreleyin.",
                "Kiralık ilanlarda güncel KKTC aramaları",
                "KKTC Kiralık İlanları",
                "KKTC kiralık ev, daire, rezidans ve ofis ilanlarını fiyat, şehir ve yayın tarihi bazında karşılaştırın.",
                "realestate",
                "rent",
                "all",
                "KKTC kiralık aramasına özel hızlı keşif sayfası",
                "Kiralık niyetli kullanıcılar çoğunlukla kısa sürede şehir, bütçe ve ilan tipi karşılaştırması yapmak ister. Bu sayfa, KKTC kiralık ev ve daire sonuçlarını sade bir koleksiyon akışı içinde göstererek aranan içeriğe daha kısa yoldan ulaştırır.",
                "Kısa URL yapısı ve kiralık odaklı içerik blokları sayesinde sayfa, genel arama sayfalarından ayrışır ve kiralama niyetini daha net anlatır.",
                ["KKTC kiralık ev ve daire ilanları", "Lefkoşa, Girne ve İskele kiralık seçenekleri", "Bütçe ve şehir bazlı hızlı filtreleme", "Güncel kiralık emlak sonuçları"],
                ["emlak", "kiralik-ev", "satilik", "satilik-arsa"],
                ["KKTC kiralık", "KKTC kiralık ev", "KKTC kiralık daire", "Girne kiralık"],
                [
                    new("KKTC kiralık sayfası sadece ev ilanlarını mı gösterir?", "Sayfa öncelikli olarak emlak kiralama niyetine odaklanır; daire, ev, rezidans ve ofis gibi kiralık taşınmazları listeler."),
                    new("Kiralık landing page'in SEO katkısı nedir?", "Kiralık terimini URL, başlık, açıklama ve içerikte tutarlı biçimde kullandığı için arama motoruna net konu sinyali verir.")
                ]),
            ["satilik"] = new(
                "satilik",
                "KKTC Satılık",
                "KKTC satılık ilanları arasında ev, arsa, araba ve ikinci el ürünleri keşfedin.",
                "KKTC Satılık İlanları | Ev, Arsa, Araba ve İkinci El",
                "KKTC satılık ilanları arasında ev, arsa, araba, ikinci el ürün ve yatırım fırsatlarını tek sayfada inceleyin.",
                "Satılık aramalarında geniş KKTC koleksiyonu",
                "KKTC Satılık İlanları",
                "KKTC satılık ev, arsa, araba ve ikinci el ilanlarını tek sayfada karşılaştırın.",
                "all",
                "sale",
                "all",
                "KKTC satılık aramalarını tek çatıda toplayan sayfa",
                "Satılık niyetli aramalar, site içinde farklı kategori kümelerine dağılır. Bu koleksiyon sayfası, evden arabaya ve ikinci el ürünlere kadar satılık odaklı tüm akışları tek URL üzerinde birleştirerek daha güçlü bir hedef sayfa oluşturur.",
                "Bu yapı, hem kategori çeşitliliğini korur hem de satılık terimine özel bir içerik omurgası sunar. Böylece geniş anahtar kelime kümelerinde daha anlamlı bir giriş sayfası elde edilir.",
                ["KKTC satılık ev, arsa ve araba ilanları", "Satılık odaklı tüm kategori sonuçları", "Yatırım, yaşam ve ikinci el ürün akışı", "Tek sayfada geniş satılık koleksiyonu"],
                ["emlak", "ikinci-el", "araba", "satilik-araba"],
                ["KKTC satılık", "KKTC satılık ev", "KKTC satılık araba", "KKTC satılık ilanlar"],
                [
                    new("KKTC satılık sayfasında hangi kategoriler bulunur?", "Satılık ev, arsa, araba ve ikinci el ürün gibi kullanıcıların en çok aradığı satılık kategoriler birlikte gösterilir."),
                    new("Genel satılık sayfası neden önemlidir?", "Geniş niyetli satılık aramalarını tek sayfada topladığı için kullanıcıya giriş noktası, arama motoruna da net tema sayfası sağlar.")
                ]),
            ["ikinci-el"] = new(
                "ikinci-el",
                "KKTC İkinci El",
                "KKTC ikinci el ürün ilanlarını tek sayfada inceleyin.",
                "KKTC İkinci El İlanları | Kullanılmış Ürünler | SEN-T Pazar",
                "KKTC ikinci el ilanları içinde ev yaşam, spor, koleksiyon, ofis ve günlük kullanım ürünlerini güncel fiyatlarla inceleyin.",
                "KKTC ikinci el alışveriş aramaları için",
                "KKTC İkinci El İlanları",
                "KKTC ikinci el ürün ilanlarını fiyat, kategori ve yayın tarihiyle birlikte keşfedin.",
                "secondhand",
                "sale",
                "all",
                "KKTC ikinci el aramalarına odaklı ürün koleksiyonu",
                "İkinci el arayan kullanıcılar, hızlı fiyat karşılaştırması ve kategori çeşitliliği bekler. Bu sayfa; ev yaşam, ofis, spor ve koleksiyon gibi alt alanları tek koleksiyonda buluşturur.",
                "İkinci el terimi doğrudan URL, başlık ve açıklamada işlendiği için sayfa; genel marketplace yapısından ayrışan net bir hedef sayfa niteliği taşır.",
                ["KKTC ikinci el ev yaşam ve ofis ürünleri", "Güncel fiyatlı kullanılmış ürün ilanları", "Kategori bazlı hızlı ürün keşfi", "Tek URL altında ikinci el koleksiyonu"],
                ["satilik", "araba", "emlak"],
                ["KKTC ikinci el", "KKTC ikinci el ürün", "KKTC kullanılmış ürün"],
                [
                    new("KKTC ikinci el sayfasında hangi ürünler yer alır?", "Ev yaşam, ofis, koleksiyon, spor ve günlük kullanım ürünleri gibi farklı ikinci el alt kategorileri listelenir."),
                    new("İkinci el landing page neden ayrı tutuluyor?", "İkinci el niyeti, emlak veya araçtan farklıdır. Ayrı sayfa, bu niyeti doğrudan karşılayarak daha iyi konu bütünlüğü sağlar.")
                ]),
            ["araba"] = new(
                "araba",
                "KKTC Araba",
                "KKTC araba ilanları ve satılık otomobil seçenekleri burada.",
                "KKTC Araba İlanları | KKTC Satılık Araba Modelleri",
                "KKTC araba ilanları ve KKTC satılık araba seçeneklerini marka, model, kilometre, yakıt ve fiyat bilgileriyle karşılaştırın.",
                "Araç aramalarında yüksek niyetli trafik",
                "KKTC Araba İlanları",
                "KKTC satılık araba modellerini marka, model ve fiyat bilgileriyle karşılaştırın.",
                "vehicle",
                "sale",
                "all",
                "KKTC araba aramalarında hızlı karşılaştırma deneyimi",
                "Araç arayan kullanıcılar çoğunlukla doğrudan fiyat, kilometre, yakıt ve vites bilgisine göre karar verir. Bu sayfa, KKTC araba ve KKTC satılık araba aramalarını aynı yüksek niyetli koleksiyon altında toplar.",
                "Kısa URL ve araç odaklı başlık, detaylı filtrelerle birleştiğinde araç kategorisinin genel site akışından ayrışmasını sağlar ve daha net arama sinyali üretir.",
                ["KKTC satılık araba modelleri", "Marka, model ve kilometre bilgili sonuçlar", "Araç fiyat karşılaştırması için net koleksiyon", "KKTC otomobil aramaları için kısa URL"],
                ["satilik-araba", "satilik", "ikinci-el"],
                ["KKTC araba", "KKTC satılık araba", "KKTC otomobil", "KKTC araç"],
                [
                    new("KKTC araba sayfası ile satılık araba sayfası arasındaki fark nedir?", "Araba sayfası daha geniş araç niyetini hedefler; satılık araba sayfası ise daha işlem odaklı açıklama ve iç link kurgusuna sahiptir."),
                    new("KKTC araba sayfasında hangi bilgiler öne çıkar?", "Fiyat, kilometre, yakıt, model ve yayın tarihi gibi karar vermeyi hızlandıran araç verileri öne çıkarılır.")
                ]),
            ["satilik-araba"] = new(
                "satilik-araba",
                "KKTC Satılık Araba",
                "KKTC satılık araba ilanlarında güncel fiyat ve model karşılaştırması yapın.",
                "KKTC Satılık Araba İlanları | Güncel Otomobil Fiyatları",
                "KKTC satılık araba ilanlarını güncel fiyat, kilometre, yakıt ve vites bilgileriyle inceleyin; farklı otomobil modellerini tek sayfada karşılaştırın.",
                "Satılık araba aramalarında işlem odaklı sayfa",
                "KKTC Satılık Araba İlanları",
                "Güncel otomobil fiyatları, kilometre bilgileri ve model seçenekleriyle KKTC satılık araba ilanlarını inceleyin.",
                "vehicle",
                "sale",
                "all",
                "KKTC satılık araba anahtar kelimesine özel içerik bloğu",
                "Satılık araba aramalarında kullanıcı niyeti genellikle fiyat ve model karşılaştırmasıdır. Bu sayfa, aynı araç stoğunu daha işlem odaklı bir metin omurgası ile sunarak anahtar kelime eşleşmesini güçlendirir.",
                "Araç sonuçları aynı kategori akışından gelse de burada kullanılan başlıklar, açıklamalar ve SSS yapısı daha doğrudan satın alma niyeti taşır.",
                ["KKTC satılık araba için kısa ve net hedef URL", "Model, fiyat ve kilometre karşılaştırması", "İşlem niyetine uygun araç koleksiyon sayfası", "Güncel otomobil ilanları"],
                ["araba", "satilik", "ikinci-el"],
                ["KKTC satılık araba", "KKTC araba fiyatları", "KKTC otomobil ilanları"],
                [
                    new("KKTC satılık araba sayfası hangi aramaları hedefler?", "Satılık araba, otomobil fiyatları, ikinci el otomobil ve marka-model bazlı araç aramalarını hedefler."),
                    new("Araç landing page'de neden ayrı SSS içerikleri kullanılıyor?", "Satın alma niyetine yakın kullanıcıların sorduğu fiyat, kilometre ve model sorularına net yanıt vermek için ayrı içerik kullanılır.")
                ]),
            ["kiralik-ev"] = new(
                "kiralik-ev",
                "KKTC Kiralık Ev",
                "KKTC kiralık ev ve daire ilanlarını şehir bazında inceleyin.",
                "KKTC Kiralık Ev İlanları | Daire ve Rezidans Seçenekleri",
                "KKTC kiralık ev, kiralık daire ve rezidans ilanlarını Girne, Lefkoşa, İskele ve Gazimağusa odaklı filtreleyin.",
                "Kiralık ev aramalarında konut odaklı sayfa",
                "KKTC Kiralık Ev İlanları",
                "KKTC kiralık ev ve daire ilanlarını şehir, bütçe ve güncellik kriterlerine göre karşılaştırın.",
                "realestate",
                "rent",
                "konut",
                "Konut odaklı kiralık sayfası",
                "Kiralık ev ve daire aramalarında kullanıcılar doğrudan konut stoğunu görmek ister. Bu sayfa, emlak kategorisi içindeki konut alt başlığını öne çıkararak daha temiz bir sonuç kümesi sunar.",
                "Kiralık ev aramasının emlak geneline göre daha spesifik bir niyet taşıması, bu sayfayı uzun kuyruklu sorgular için güçlü hale getirir.",
                ["KKTC kiralık ev ve daire ilanları", "Konut alt kategorisine odaklı sonuçlar", "Şehir bazlı hızlı kiralık karşılaştırması"],
                ["kiralik", "emlak", "satilik-daire"],
                ["KKTC kiralık ev", "KKTC kiralık daire", "Girne kiralık ev"],
                [
                    new("Kiralık ev sayfası ile kiralık sayfası arasındaki fark nedir?", "Bu sayfa yalnızca konut odaklı kiralık emlak sonuçlarını öne çıkarır; daha genel kiralık sayfası daha geniş emlak akışını kapsar."),
                    new("Kiralık ev landing page hangi bölgelerde işe yarar?", "Girne, Lefkoşa, İskele ve Gazimağusa gibi yoğun kiralama aramalarında daha net sonuç kümesi sağlar.")
                ]),
            ["satilik-arsa"] = new(
                "satilik-arsa",
                "KKTC Satılık Arsa",
                "KKTC satılık arsa ilanlarını yatırım ve konum bilgileriyle inceleyin.",
                "KKTC Satılık Arsa İlanları | Yatırımlık Arsa Fırsatları",
                "KKTC satılık arsa ilanlarını imar durumu, lokasyon ve fiyat avantajlarına göre inceleyin; yatırımlık arsa fırsatlarını tek sayfada görün.",
                "Arsa yatırım aramalarında net hedef sayfa",
                "KKTC Satılık Arsa İlanları",
                "KKTC yatırımlık arsa ve satılık parsel ilanlarını fiyat ve konum avantajlarıyla karşılaştırın.",
                "land",
                "sale",
                "all",
                "KKTC arsa aramalarında yatırım niyetine odaklanın",
                "Arsa arayan kullanıcılar çoğu zaman yatırım potansiyeli, imar yapısı ve lokasyon detaylarını birlikte değerlendirmek ister. Bu sayfa, KKTC satılık arsa niyetine doğrudan cevap veren ayrı bir giriş noktası sağlar.",
                "Satılık arsa terimini URL, başlık ve açıklama seviyesinde net biçimde kullandığı için emlak geneline göre daha keskin bir konu sinyali üretir.",
                ["KKTC satılık arsa ve parsel ilanları", "Yatırım odaklı arazi karşılaştırması", "Lokasyon ve fiyat avantajlarını aynı akışta görme"],
                ["emlak", "satilik", "kiralik"],
                ["KKTC satılık arsa", "KKTC yatırımlık arsa", "KKTC arsa ilanları"],
                [
                    new("KKTC satılık arsa sayfası kimler için uygundur?", "Yatırımcılar, proje geliştirmek isteyenler ve lokasyon bazlı arsa arayan kullanıcılar için uygundur."),
                    new("Arsa aramaları için ayrı landing page neden yararlı?", "Arsa niyeti, konut niyetinden farklıdır. Ayrı sayfa, bu farkı arama motoruna ve kullanıcıya daha net anlatır.")
                ])
        };
    }

    private static string NormalizeSeoTopic(string? topic)
    {
        return ListingTaxonomy.NormalizeText(topic).Replace(' ', '-');
    }

    private static string BuildSeoLandingPath(string topic)
    {
        return $"/kktc-{NormalizeSeoTopic(topic)}";
    }

    private static SeoHubLink MapSeoHubLink(SeoLandingDefinition definition)
    {
        return new SeoHubLink
        {
            Badge = definition.TargetKeywords.FirstOrDefault() ?? "KKTC",
            Title = definition.LinkTitle,
            Description = definition.LinkDescription,
            Url = BuildSeoLandingPath(definition.Topic)
        };
    }

    private static List<SeoHubLink> BuildSeoHubLinks(IEnumerable<SeoLandingDefinition>? definitions = null)
    {
        return (definitions ?? SeoLandingDefinitions.Values)
            .Select(MapSeoHubLink)
            .ToList();
    }

    private static List<SeoHubLink> BuildSeoHubLinks(IEnumerable<string> topics)
    {
        return topics
            .Select(NormalizeSeoTopic)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(topic => SeoLandingDefinitions.ContainsKey(topic))
            .Select(topic => MapSeoHubLink(SeoLandingDefinitions[topic]))
            .ToList();
    }

private void PreparePublishViewData()
     {
         ViewData["PublishCities"] = _catalog.Cities.Where(x => x != "all").ToList();
         ViewData["PublishTypes"] = _catalog.ListingTypes.Where(x => x != "all").ToList();
         ViewData["PublishCategories"] = _catalog.Categories.Where(x => x != "all").ToList();
         ViewData["VehicleBrandOptions"] = _context.Listings
             .AsNoTracking()
             .Where(x => x.Category == "vehicle" && x.VehicleBrand != null && x.VehicleBrand != string.Empty)
             .Select(x => x.VehicleBrand!)
             .AsEnumerable()
             .Select(CleanOptionalText)
             .Where(x => !string.IsNullOrWhiteSpace(x))
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .OrderBy(x => x, StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true))
             .ToList();
         ViewData["PublishSubCategoryValueMap"] = ListingTaxonomy.BuildPublishSubCategoryValueMap();
         ViewData["PublishTypeValueMap"] = ListingTaxonomy.BuildPublishTypeValueMap();
         ViewData["PublishProductFieldVisibilityMap"] = ListingTaxonomy.BuildPublishProductFieldVisibilityMap();
     }

    public IActionResult Index(
        string listingType = "all",
        string city = "all",
        string category = "all",
        string subCategory = "all",
        string priceRange = "any",
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string keyword = "",
        string sortBy = "latest",
        bool showResults = false)
    {
        if (!string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
        {
            var slug = _catalog.GetDefaultSlug(category);
            return RedirectToAction(nameof(Category), new { slug, listingType, city, subCategory, priceRange, minPrice, maxPrice, keyword, sortBy, showResults });
        }

        ViewData["MetaDescription"] = "SEN-T Pazar'da emlak, vasita ve ikinci el ilanlarini guvenle kesfedin.";
        ViewData["CanonicalUrl"] = BuildCanonicalUrl(queryString: Request.QueryString.Value);

        return BuildListingPage(listingType, city, "all", subCategory, priceRange, minPrice, maxPrice, keyword, sortBy, showResults, isCategoryPage: false, currentCategorySlug: string.Empty);
    }

    public IActionResult Category(
        string slug,
        string listingType = "all",
        string city = "all",
        string subCategory = "all",
        string priceRange = "any",
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string keyword = "",
        string sortBy = "latest",
        bool showResults = false)
    {
        if (!_catalog.TryResolveCategoryFromSlug(slug, out var categoryCode))
        {
            return NotFound();
        }

        ViewData["MetaDescription"] = "SEN-T Pazar kategori sayfasi. Filtreleyin, karsilastirin, guvenli sekilde iletisime gecin.";
        ViewData["CanonicalUrl"] = BuildCanonicalUrl(queryString: Request.QueryString.Value);

        return BuildListingPage(listingType, city, categoryCode, subCategory, priceRange, null, null, keyword, sortBy, showResults, isCategoryPage: true, currentCategorySlug: slug);
    }

    [HttpGet("/kktc-{topic}")]
    public IActionResult KeywordLanding(string topic)
    {
        var normalizedTopic = NormalizeSeoTopic(topic);
        if (!SeoLandingDefinitions.TryGetValue(normalizedTopic, out var definition))
        {
            return NotFound();
        }

        return BuildListingPage(
            definition.ListingType,
            "all",
            definition.Category,
            definition.SubCategory,
            "any",
            null,
            null,
            string.Empty,
            "latest",
            showResults: true,
            isCategoryPage: false,
            currentCategorySlug: string.Empty,
            seoLanding: definition);
    }

    private IActionResult BuildListingPage(
        string listingType,
        string city,
        string category,
        string subCategory,
        string priceRange,
        decimal? minPrice,
        decimal? maxPrice,
        string keyword,
        string sortBy,
        bool showResults,
        bool isCategoryPage,
        string currentCategorySlug,
        SeoLandingDefinition? seoLanding = null)
    {
        listingType = _catalog.ListingTypes.Contains(listingType) ? listingType : "all";
        city = NormalizeCityFilter(city);
        category = NormalizeCategoryCode(category);
        (category, subCategory) = FoldLeafSearchCategories(category, subCategory);
        category = _catalog.Categories.Contains(category) ? category : "all";
        subCategory = NormalizeSearchSubCategorySelection(category, subCategory);
        priceRange = _catalog.PriceRanges.Contains(priceRange) ? priceRange : "any";
        sortBy = _catalog.SortOptions.Contains(sortBy) ? sortBy : "latest";
        keyword = NormalizeSearchKeyword(keyword);


var dbListings = _context.Listings
             .Include(x => x.Images)
             .Include(x => x.Reviews)
             .AsSplitQuery()
             .Where(x => x.IsApproved && !x.IsClosed && !x.IsDeleted)
             .AsEnumerable()
             .Where(x => !IsLikelySeedListing(x))
             .ToList();

            var allListings = OrderByPromotionPriority(dbListings.Select(MapListingToPropertyCard).ToList()).ToList();
        var nowUtc = DateTime.UtcNow;

        // Vitrin ilanlar (IsVitrin)
            var vitrinListings = OrderByVitrinPriority(
                dbListings
                    .Where(x => PromotionRules.IsVitrinActive(x.IsVitrin, x.VitrinExpiryDate, nowUtc))
                    .Select(MapListingToPropertyCard))
            .ToList();

        // Öne Çıkan ilanlar (IsFeatured)
            var featuredListings = OrderByFeaturedPriority(
                dbListings
                    .Where(x => PromotionRules.IsFeaturedActive(x.IsFeatured, x.FeaturedExpiryDate, nowUtc))
                    .Select(MapListingToPropertyCard))
            .ToList();

        // Popüler ilanlar (IsPopular)
        var popularListings = dbListings
            .Where(x => x.IsPopular)
            .Select(MapListingToPropertyCard)
            .OrderBy(x => x.PopularOrder ?? int.MaxValue)
            .ThenByDescending(x => x.Id)
            .ToList();

        var sellerIds = dbListings
            .Select(x => x.UserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (sellerIds.Count > 0)
        {
            var sellerSummaries = _context.Users
                .AsNoTracking()
                .Where(x => sellerIds.Contains(x.Id))
                .Select(x => new SellerCardSummary
                {
                    UserId = x.Id,
                    FullName = x.FullName,
                    AvatarUrl = x.AvatarUrl ?? string.Empty,
                    IsCorporateMember = x.IsCorporateMember,
                    CompanyName = x.CompanyName ?? string.Empty,
                    CompanyLogoUrl = x.CompanyLogoUrl ?? string.Empty
                })
                .ToDictionary(x => x.UserId, x => x, StringComparer.Ordinal);

            ApplySellerCardSummaries(allListings, sellerSummaries);
            ApplySellerCardSummaries(vitrinListings, sellerSummaries);
            ApplySellerCardSummaries(featuredListings, sellerSummaries);
            ApplySellerCardSummaries(popularListings, sellerSummaries);
        }

            var filtered = ApplySorting(
                ApplyFilters(allListings, listingType, city, category, priceRange, minPrice, maxPrice, keyword, subCategory),
                sortBy).ToList();

        var activeListingCounts = _context.Listings
            .AsNoTracking()
            .Where(x => x.IsApproved && !x.IsClosed && !string.IsNullOrWhiteSpace(x.UserId))
            .GroupBy(x => x.UserId!)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.UserId, x => x.Count);

        var corporateProfiles = _context.Users
            .AsNoTracking()
            .Where(x => x.IsCorporateMember)
            .AsEnumerable()
            .Select(x => new CorporateProfileCard
            {
                UserId = x.Id,
                DisplayName = !string.IsNullOrWhiteSpace(x.CompanyName) ? x.CompanyName! : (x.FullName ?? x.UserName ?? "Kurumsal Mağaza"),
                City = string.IsNullOrWhiteSpace(x.City) ? "Belirtilmemiş" : x.City!,
                ContactPhone = !string.IsNullOrWhiteSpace(x.CompanyPhone) ? x.CompanyPhone! : (x.PhoneNumber ?? string.Empty),
                LogoUrl = x.CompanyLogoUrl ?? string.Empty,
                WebsiteUrl = x.CompanyWebSite ?? string.Empty,
                ActiveListingCount = activeListingCounts.TryGetValue(x.Id, out var count) ? count : 0
            })
            .Where(x => x.ActiveListingCount > 0)
            .OrderByDescending(x => x.ActiveListingCount)
            .ThenBy(x => x.DisplayName)
            .Take(8)
            .ToList();

        var hiddenSearchCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "fashion",
            "pets",
            "helper"
        };

        var visibleSearchCategories = _catalog.Categories
            .Where(category => !hiddenSearchCategories.Contains(category))
            .ToList();

        var categorySubCategoryMap = BuildSubCategoryMap();
        var listingTypeCategoryMap = ListingTaxonomy.BuildSearchCategoryMapByListingType(visibleSearchCategories);
        var subCategoryFilters = GetSubCategoryFilters(categorySubCategoryMap, category);
        var recommended = BuildRecommendations(filtered, allListings, category, city);
        var regionalCampaigns = BuildRegionalCampaigns(allListings);
        var persistedVehicleListings = _context.Listings
            .AsNoTracking()
            .Where(x => x.Category == "vehicle" && x.VehicleBrand != null && x.VehicleBrand != string.Empty)
            .Select(x => new { x.VehicleBrand, x.VehicleModel })
            .AsEnumerable()
            .ToList();
        var vehicleBrandComparer = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
        var vehicleBrandGroups = persistedVehicleListings
            .GroupBy(x => x.VehicleBrand!.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, vehicleBrandComparer)
            .ToList();
        var vehicleBrandOptions = vehicleBrandGroups
            .Select(group => group.First().VehicleBrand!.Trim())
            .ToList();
        var vehicleModelOptionsByBrand = vehicleBrandGroups.ToDictionary(
            group => group.First().VehicleBrand!.Trim(),
            group => group
                .Select(item => item.VehicleModel?.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, vehicleBrandComparer)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
        // Vitrin ve öne çıkanları birleştir, yoksa tüm ilanları kullan
        var featuredCategorySource = OrderByPromotionPriority(
            vitrinListings.Concat(featuredListings).DistinctBy(x => x.Id).ToList()
        ).ToList();
        if (featuredCategorySource.Count == 0)
        {
            featuredCategorySource = OrderByPromotionPriority(allListings).ToList();
        }

        var model = new HomePageViewModel
        {
            HeroEyebrow = _localizer["heroEyebrow"],
            HeroTitle = _localizer["heroTitle"],
            HeroSubtitle = _localizer["heroSubtitle"],
            VitrinListings = vitrinListings,
            FeaturedListings = featuredListings,
            PopularListings = popularListings,
            SearchResults = filtered,
            RecommendedListings = recommended,
            PopularRegions = ["Girne", "İskele", "Lefkoşa", "Gazimağusa", "Lefke", "Güzelyurt"],
            RegionSpots =
            [
                new RegionSpot { Name = "Lefkoşa", ListingCount = allListings.Count(x => x.City == "Lefkoşa"), ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Nicosia%27s_skyline_2024.jpg/330px-Nicosia%27s_skyline_2024.jpg" },
                new RegionSpot { Name = "Girne", ListingCount = allListings.Count(x => x.City == "Girne"), ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c4/Kyrenia_01-2017_img04_view_from_castle_bastion.jpg/330px-Kyrenia_01-2017_img04_view_from_castle_bastion.jpg" },
                new RegionSpot { Name = "Gazimağusa", ListingCount = allListings.Count(x => x.City == "Gazimağusa"), ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/07/Varosha_utsikt.jpg/330px-Varosha_utsikt.jpg" },
                new RegionSpot { Name = "Güzelyurt", ListingCount = allListings.Count(x => x.City == "Güzelyurt"), ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6f/Morphou_orange_monument.jpg/330px-Morphou_orange_monument.jpg" },
                new RegionSpot { Name = "İskele", ListingCount = allListings.Count(x => x.City == "İskele"), ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/80/%C4%B0skele_Trikomo_main_square_July_2015.jpg/330px-%C4%B0skele_Trikomo_main_square_July_2015.jpg" },
                new RegionSpot { Name = "Lefke", ListingCount = allListings.Count(x => x.City == "Lefke"), ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/3/3b/Lefke_panoramik.jpg" }
            ],
               FeaturedEmlak = featuredCategorySource
                   .Where(x => ListingTaxonomy.MatchesSearchCategory("realestate", x.Category, x.SubCategory))
                   .Take(4)
                   .Select((x, i) => new SEN_T_PAZAR.Models.ProjectCard {
                   Id = x.Id,
                   Name = x.Title,
                   Location = x.City,
                   Company = "",
                   DeliveryDate = "",
                   PriceFrom = x.PriceLabel,
                   ImageUrl = x.ImageUrl
               }).ToList(),
               FeaturedVasita = featuredCategorySource
                   .Where(x => ListingTaxonomy.MatchesSearchCategory("vehicle", x.Category, x.SubCategory))
                   .Take(4)
                   .Select((x, i) => new SEN_T_PAZAR.Models.ProjectCard {
                   Id = x.Id,
                   Name = x.Title,
                   Location = x.City,
                   Company = "",
                   DeliveryDate = "",
                   PriceFrom = x.PriceLabel,
                   ImageUrl = x.ImageUrl
               }).ToList(),
               FeaturedElektronik = featuredCategorySource
                   .Where(x => ListingTaxonomy.MatchesSearchCategory("electronics", x.Category, x.SubCategory))
                   .Take(4)
                   .Select((x, i) => new SEN_T_PAZAR.Models.ProjectCard {
                   Id = x.Id,
                   Name = x.Title,
                   Location = x.City,
                   Company = "",
                   DeliveryDate = "",
                   PriceFrom = x.PriceLabel,
                   ImageUrl = x.ImageUrl
               }).ToList(),
               FeaturedEvEsya = featuredCategorySource
                   .Where(x => ListingTaxonomy.MatchesSearchCategory("home", x.Category, x.SubCategory))
                   .Take(4)
                   .Select((x, i) => new SEN_T_PAZAR.Models.ProjectCard {
                   Id = x.Id,
                   Name = x.Title,
                   Location = x.City,
                   Company = "",
                   DeliveryDate = "",
                   PriceFrom = x.PriceLabel,
                   ImageUrl = x.ImageUrl
               }).ToList(),
               FeaturedHizmet = featuredCategorySource
                   .Where(x => ListingTaxonomy.MatchesSearchCategory("services", x.Category, x.SubCategory))
                   .Take(4)
                   .Select((x, i) => new SEN_T_PAZAR.Models.ProjectCard {
                   Id = x.Id,
                   Name = x.Title,
                   Location = x.City,
                   Company = "",
                   DeliveryDate = "",
                   PriceFrom = x.PriceLabel,
                   ImageUrl = x.ImageUrl
               }).ToList(),
            CorporateProfiles = corporateProfiles,
            PartnerNames = ["Nova", "Apex", "BlueLine", "PrimeArc", "Kuzey Yapım", "Westland"],
            MarketCategories = visibleSearchCategories
                .Where(x => x != "all")
                .Select((code, index) => new MarketCategory
                {
                    Title = code,
                    Count = allListings.Count(x => ListingTaxonomy.MatchesSearchCategory(code, x.Category, x.SubCategory)),
                    AccentColor = index % 2 == 0 ? "#3d7bd8" : "#4f89a8"
                })
                .ToList(),
            MarketTiles = OrderByPromotionPriority(allListings)
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
            CategoryOptions = visibleSearchCategories.ToList(),
            PriceRangeOptions = _catalog.PriceRanges.ToList(),
            SortOptions = _catalog.SortOptions.ToList(),
            SubCategoryFilters = subCategoryFilters,
            CategorySubCategoryMap = categorySubCategoryMap,
            SearchTabs = BuildSearchTabs(listingType, category),
            CategorySubCategoryJson = SerializeCategorySubCategoryMap(categorySubCategoryMap),
            ListingTypeCategoryJson = JsonSerializer.Serialize(listingTypeCategoryMap),
            VehicleBrandOptions = vehicleBrandOptions,
            VehicleModelOptionsByBrand = vehicleModelOptionsByBrand,
            RegionalCampaigns = regionalCampaigns,
            ListingType = listingType,
            City = city,
            Category = category,
            SubCategory = subCategory,
            PriceRange = priceRange,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Keyword = keyword,
            SortBy = sortBy,
            IsCategoryPage = isCategoryPage,
            ShowResults = showResults,
            CurrentCategorySlug = currentCategorySlug,
            CategoryHeroImage = (isCategoryPage || seoLanding is not null) && !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase)
                ? _catalog.GetCategoryHeroImage(category)
                : string.Empty,
            SeoHubLinks = BuildSeoHubLinks(),
            TotalCount = isCategoryPage
                ? allListings.Count(x => ListingTaxonomy.MatchesSearchCategory(category, x.Category, x.SubCategory))
                : allListings.Count,
            FilteredCount = filtered.Count
        };

        if (seoLanding is not null)
        {
            model.HeroEyebrow = seoLanding.HeroEyebrow;
            model.HeroTitle = seoLanding.HeroTitle;
            model.HeroSubtitle = seoLanding.HeroSubtitle;
            model.SeoLanding = new SeoLandingContent
            {
                Eyebrow = seoLanding.HeroEyebrow,
                Heading = seoLanding.HeroTitle,
                Intro = seoLanding.HeroSubtitle,
                BodyTitle = seoLanding.BodyTitle,
                BodyText = seoLanding.BodyText,
                SecondaryText = seoLanding.SecondaryText,
                Highlights = seoLanding.Highlights.ToList(),
                FaqItems = seoLanding.Faqs.Select(faq => new SeoLandingFaqItem
                {
                    Question = faq.Question,
                    Answer = faq.Answer
                }).ToList(),
                RelatedLinks = BuildSeoHubLinks(seoLanding.RelatedTopics)
            };
        }

        var selectedCategoryLabel = !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase)
            ? _localizer.CategoryLabel(category)
            : string.Empty;
        var selectedSubCategoryLabel = !string.Equals(subCategory, "all", StringComparison.OrdinalIgnoreCase)
            ? ListingTaxonomy.HumanizeSubCategory(subCategory)
            : string.Empty;
        var selectedListingTypeLabel = !string.Equals(listingType, "all", StringComparison.OrdinalIgnoreCase)
            ? _localizer.TypeLabel(listingType)
            : string.Empty;
        var selectedCityLabel = !string.Equals(city, "all", StringComparison.OrdinalIgnoreCase)
            ? city
            : string.Empty;
        var seoSegments = new[]
        {
            selectedCategoryLabel,
            selectedSubCategoryLabel,
            selectedListingTypeLabel,
            selectedCityLabel,
            keyword
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
        var canonicalBaseUrl = seoLanding is not null
            ? BuildCanonicalUrl(BuildSeoLandingPath(seoLanding.Topic))
            : isCategoryPage && !string.IsNullOrWhiteSpace(currentCategorySlug)
                ? BuildCanonicalUrl($"/kategori/{currentCategorySlug}")
                : BuildCanonicalUrl();
        var hasNonCanonicalParameters = showResults
            || listingType != "all"
            || city != "all"
            || subCategory != "all"
            || priceRange != "any"
            || sortBy != "latest"
            || !string.IsNullOrWhiteSpace(keyword);
        var shouldIndexCollectionPage = seoLanding is not null || !hasNonCanonicalParameters;
        var hasSpecificSearchContext = isCategoryPage || seoSegments.Count > 0;
        var collectionLabel = seoSegments.Count > 0
            ? string.Join(" | ", seoSegments)
            : "KKTC ilanları";
        var seoTitle = seoLanding?.MetaTitle ?? (hasSpecificSearchContext
            ? $"{collectionLabel} - {filtered.Count} aktif ilan"
            : "SEN-T PAZAR-DİJİTAL ÇARŞI");
        var seoDescription = seoLanding?.MetaDescription ?? (hasSpecificSearchContext
            ? TrimSeoText($"{collectionLabel} için {filtered.Count} aktif ilanı inceleyin. Güncel filtreler, görseller ve doğrudan iletişim bilgileri SEN-T Pazar'da.")
            : "SEN-T Pazar'da emlak, vasita ve ikinci el ilanlarini guvenle kesfedin.");
        var seoImage = !string.IsNullOrWhiteSpace(model.CategoryHeroImage)
            ? model.CategoryHeroImage
            : filtered.FirstOrDefault()?.ImageUrl ?? vitrinListings.FirstOrDefault()?.ImageUrl;
        var collectionSchema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "CollectionPage",
            ["name"] = seoTitle,
            ["description"] = seoDescription,
            ["url"] = canonicalBaseUrl,
            ["inLanguage"] = CultureInfo.CurrentUICulture.Name,
            ["mainEntity"] = new Dictionary<string, object?>
            {
                ["@type"] = "ItemList",
                ["numberOfItems"] = filtered.Count
            }
        };
        var schemaPayload = new List<object> { collectionSchema };

        if (seoLanding is not null)
        {
            schemaPayload.Add(new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "BreadcrumbList",
                ["itemListElement"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = 1,
                        ["name"] = "SEN-T Pazar",
                        ["item"] = $"{Request.Scheme}://{Request.Host}/"
                    },
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = 2,
                        ["name"] = seoLanding.HeroTitle,
                        ["item"] = canonicalBaseUrl
                    }
                }
            });

            if (seoLanding.Faqs.Length > 0)
            {
                schemaPayload.Add(new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "FAQPage",
                    ["mainEntity"] = seoLanding.Faqs.Select(faq => new Dictionary<string, object?>
                    {
                        ["@type"] = "Question",
                        ["name"] = faq.Question,
                        ["acceptedAnswer"] = new Dictionary<string, object?>
                        {
                            ["@type"] = "Answer",
                            ["text"] = faq.Answer
                        }
                    }).ToArray()
                });
            }
        }

        ViewData["Title"] = seoTitle;
        if (seoLanding is not null)
        {
            ViewData["UseRawTitle"] = true;
        }
        else if (hasSpecificSearchContext)
        {
            ViewData["UseRawTitle"] = false;
        }

        ViewData["MetaDescription"] = seoDescription;
        ViewData["MetaKeywords"] = seoLanding is not null
            ? BuildMetaKeywords(seoLanding.TargetKeywords.Cast<string?>().ToArray())
            : BuildMetaKeywords(
                collectionLabel,
                selectedCategoryLabel,
                selectedSubCategoryLabel,
                selectedListingTypeLabel,
                selectedCityLabel,
                keyword,
                "KKTC ilan",
                "SEN-T Pazar");
        ViewData["MetaImage"] = seoImage;
        ViewData["MetaImageAlt"] = seoTitle;
        ViewData["OpenGraphType"] = "website";
        ViewData["CanonicalUrl"] = canonicalBaseUrl;
        ViewData["Robots"] = shouldIndexCollectionPage
            ? "index,follow,max-image-preview:large"
            : "noindex,follow,max-image-preview:large";
        ViewData["PageSchemaJsonLd"] = JsonSerializer.Serialize(schemaPayload.Count == 1 ? schemaPayload[0] : schemaPayload);

        return View("Index", model);
    }

    public IActionResult Details(int id)
    {
        // Önce veritabanından ilanı yükle (görsel ve yorumlarla birlikte)
        var dbListing = _context.Listings
            .Include(l => l.Images)
            .Include(l => l.Reviews)
            .FirstOrDefault(l => l.Id == id);
        PropertyCard? listing;

        if (dbListing != null)
        {
            if (IsLikelySeedListing(dbListing))
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
                if (!string.Equals(userEmail ?? string.Empty, "taskinonel@gmail.com", StringComparison.OrdinalIgnoreCase))
                {
                    dbListing.ViewCount = Math.Max(0, dbListing.ViewCount) + 1;
                    _context.SaveChanges();
                }
            }
            listing = MapListingToPropertyCard(dbListing);
            var detailCanonicalUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            var detailCategoryLabel = _localizer.CategoryLabel(ListingTaxonomy.GetBrowseCategoryCode(dbListing.Category, dbListing.SubCategory));
            var detailDescriptionSource = string.IsNullOrWhiteSpace(listing.Summary) ? listing.DetailBody : listing.Summary;
            var detailDescription = TrimSeoText(string.IsNullOrWhiteSpace(detailDescriptionSource) ? listing.Title : detailDescriptionSource);
            var detailImages = (listing.GalleryImages ?? new List<string>())
                .Where(image => !string.IsNullOrWhiteSpace(image))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToArray();

            if (detailImages.Length == 0 && !string.IsNullOrWhiteSpace(listing.ImageUrl))
            {
                detailImages = [listing.ImageUrl];
            }

            var detailSchema = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "Product",
                ["name"] = listing.Title,
                ["description"] = detailDescription,
                ["url"] = detailCanonicalUrl,
                ["image"] = detailImages,
                ["sku"] = listing.ListingCode,
                ["category"] = detailCategoryLabel
            };

            if (dbListing.PriceAmount > 0)
            {
                detailSchema["offers"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Offer",
                    ["price"] = dbListing.PriceAmount,
                    ["priceCurrency"] = string.IsNullOrWhiteSpace(dbListing.PriceCurrency) ? "TRY" : dbListing.PriceCurrency,
                    ["availability"] = "https://schema.org/InStock",
                    ["url"] = detailCanonicalUrl
                };
            }

            if (!string.IsNullOrWhiteSpace(dbListing.City) || !string.IsNullOrWhiteSpace(listing.Location))
            {
                detailSchema["areaServed"] = new Dictionary<string, object?>
                {
                    ["@type"] = "AdministrativeArea",
                    ["name"] = string.Join(" - ", new[] { dbListing.City, listing.Location }.Where(value => !string.IsNullOrWhiteSpace(value)))
                };
            }

            ViewData["Title"] = listing.Title;
            ViewData["MetaDescription"] = detailDescription;
            ViewData["MetaKeywords"] = BuildMetaKeywords(listing.Title, detailCategoryLabel, dbListing.City, listing.Location, dbListing.Type, listing.ListingCode, "KKTC ilan");
            ViewData["CanonicalUrl"] = detailCanonicalUrl;
            ViewData["MetaImage"] = detailImages.FirstOrDefault();
            ViewData["MetaImageAlt"] = listing.Title;
            ViewData["OpenGraphType"] = "product";
            ViewData["PageSchemaJsonLd"] = JsonSerializer.Serialize(detailSchema);
        }
        else
        {
            return NotFound();
        }

        listing.Reviews ??= new List<Review>();

        var listingOwner = string.IsNullOrWhiteSpace(dbListing.UserId)
            ? null
            : _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == dbListing.UserId);

        listing.SellerCity = !string.IsNullOrWhiteSpace(listingOwner?.City)
            ? listingOwner!.City!
            : string.IsNullOrWhiteSpace(dbListing.City) ? "Belirtilmemiş" : dbListing.City;

        // Favori sayısı (kaç kişi favorilere eklemiş)
        ViewData["FavoritesCount"] = _context.UserFavorites.Count(f => f.ListingId == id);

var relatedSource = _context.Listings
             .AsNoTracking()
             .Include(x => x.Images)
             .Include(x => x.Reviews)
             .Where(x => x.IsApproved && !x.IsClosed && !x.IsDeleted && x.Id != dbListing.Id)
             .ToList();

        var nowUtc = DateTime.UtcNow;

        var vitrinSource = relatedSource
            .Where(x => PromotionRules.IsVitrinActive(x.IsVitrin, x.VitrinExpiryDate, nowUtc))
            .ToList();

        if (PromotionRules.IsVitrinActive(dbListing.IsVitrin, dbListing.VitrinExpiryDate, nowUtc))
        {
            vitrinSource.Insert(0, dbListing);
        }

        var featuredSource = relatedSource
            .Where(x => PromotionRules.IsFeaturedActive(x.IsFeatured, x.FeaturedExpiryDate, nowUtc))
            .ToList();

        if (PromotionRules.IsFeaturedActive(dbListing.IsFeatured, dbListing.FeaturedExpiryDate, nowUtc))
        {
            featuredSource.Insert(0, dbListing);
        }

        var popularSource = relatedSource
            .Where(x => x.IsPopular)
            .ToList();

        if (dbListing.IsPopular)
        {
            popularSource.Insert(0, dbListing);
        }

        listing.ShowcaseListings = OrderByVitrinPriority(vitrinSource.Select(MapListingToPropertyCard))
            .Take(3)
            .ToList();

        listing.FeaturedSideListings = OrderByFeaturedPriority(featuredSource.Select(MapListingToPropertyCard))
            .Take(3)
            .ToList();

        listing.PopularListings = OrderByPopularPriority(popularSource.Select(MapListingToPropertyCard))
            .Take(3)
            .ToList();

        var activeListingCounts = _context.Listings
            .AsNoTracking()
            .Where(x => x.IsApproved && !x.IsClosed && !string.IsNullOrWhiteSpace(x.UserId))
            .GroupBy(x => x.UserId!)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.UserId, x => x.Count);

        listing.CorporateProfiles = _context.Users
            .AsNoTracking()
            .Where(x => x.IsCorporateMember)
            .AsEnumerable()
            .Select(x => new CorporateProfileCard
            {
                UserId = x.Id,
                DisplayName = !string.IsNullOrWhiteSpace(x.CompanyName) ? x.CompanyName! : x.FullName,
                City = string.IsNullOrWhiteSpace(x.City) ? "Belirtilmemiş" : x.City!,
                ContactPhone = !string.IsNullOrWhiteSpace(x.CompanyPhone) ? x.CompanyPhone! : (x.PhoneNumber ?? string.Empty),
                LogoUrl = x.CompanyLogoUrl ?? string.Empty,
                WebsiteUrl = x.CompanyWebSite ?? string.Empty,
                ActiveListingCount = activeListingCounts.ContainsKey(x.Id) ? activeListingCounts[x.Id] : 0
            })
            .Where(x => x.ActiveListingCount > 0)
            .OrderByDescending(x => x.ActiveListingCount)
            .ThenBy(x => x.DisplayName)
            .Take(6)
            .ToList();

        return View(listing);
    }

    [HttpGet("/magaza/{id}")]
    public IActionResult Store(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = _context.Users
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == id && x.IsCorporateMember);

        if (user == null)
        {
            return NotFound();
        }

        var listingCards = OrderByPromotionPriority(_context.Listings
                .AsNoTracking()
                .Include(x => x.Images)
                .Include(x => x.Reviews)
                .Where(x => x.IsApproved && !x.IsClosed && x.UserId == id)
                .AsEnumerable()
                .Select(MapListingToPropertyCard))
            .ToList();

        if (listingCards.Count == 0)
        {
            return NotFound();
        }

        var model = new CorporateStoreViewModel
        {
            UserId = user.Id,
            StoreName = !string.IsNullOrWhiteSpace(user.CompanyName) ? user.CompanyName! : (user.FullName ?? user.UserName ?? "Kurumsal Mağaza"),
            City = string.IsNullOrWhiteSpace(user.City) ? "Belirtilmemiş" : user.City!,
            ContactPhone = !string.IsNullOrWhiteSpace(user.CompanyPhone) ? user.CompanyPhone! : (user.PhoneNumber ?? string.Empty),
            WebsiteUrl = user.CompanyWebSite ?? string.Empty,
            LogoUrl = user.CompanyLogoUrl ?? string.Empty,
            ActiveListingCount = listingCards.Count,
            Listings = listingCards
        };

        ViewData["Title"] = model.StoreName + " | Kurumsal Mağaza";
        var storeCanonicalUrl = BuildCanonicalUrl($"/magaza/{id}");
        var storeDescription = TrimSeoText($"{model.StoreName} mağazasındaki {listingCards.Count} aktif ilanı ve iletişim bilgilerini inceleyin. {model.City} bölgesindeki kurumsal mağaza vitrini.");
        var storeSchema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Store",
            ["name"] = model.StoreName,
            ["description"] = storeDescription,
            ["url"] = storeCanonicalUrl,
            ["image"] = !string.IsNullOrWhiteSpace(model.LogoUrl) ? model.LogoUrl : model.Listings.FirstOrDefault()?.ImageUrl,
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["addressLocality"] = model.City
            },
            ["numberOfItems"] = model.ActiveListingCount
        };

        ViewData["MetaDescription"] = storeDescription;
        ViewData["MetaKeywords"] = BuildMetaKeywords(model.StoreName, model.City, "kurumsal mağaza", "aktif ilan", "SEN-T Pazar");
        ViewData["CanonicalUrl"] = storeCanonicalUrl;
        ViewData["MetaImage"] = !string.IsNullOrWhiteSpace(model.LogoUrl) ? model.LogoUrl : model.Listings.FirstOrDefault()?.ImageUrl;
        ViewData["MetaImageAlt"] = model.StoreName;
        ViewData["PageSchemaJsonLd"] = JsonSerializer.Serialize(storeSchema);

        return View("Store", model);
    }

    [HttpGet("/satici/{id}")]
    [HttpGet("/seller/{id}")]
    public IActionResult SellerProfile(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = _context.Users
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        var listingCards = OrderByPromotionPriority(_context.Listings
                .AsNoTracking()
                .Include(x => x.Images)
                .Include(x => x.Reviews)
                .Where(x => x.IsApproved && !x.IsClosed && x.UserId == id)
                .AsEnumerable()
                .Select(MapListingToPropertyCard))
            .ToList();

        if (listingCards.Count == 0)
        {
            return NotFound();
        }

        var model = new CorporateStoreViewModel
        {
            UserId = user.Id,
            StoreName = !string.IsNullOrWhiteSpace(user.CompanyName) ? user.CompanyName! : (user.FullName ?? user.UserName ?? "Satıcı Profili"),
            City = string.IsNullOrWhiteSpace(user.City) ? "Belirtilmemiş" : user.City!,
            ContactPhone = user.PhoneNumber ?? string.Empty,
            WebsiteUrl = user.CompanyWebSite ?? string.Empty,
            LogoUrl = user.CompanyLogoUrl ?? string.Empty,
            ActiveListingCount = listingCards.Count,
            Listings = listingCards
        };

        ViewData["Title"] = model.StoreName + " | Satıcı Profili";
        var profileCanonicalUrl = BuildCanonicalUrl($"/satici/{id}");
        var profileDescription = TrimSeoText($"{model.StoreName} satıcısının {listingCards.Count} aktif ilanını inceleyin. {model.City} bölgesindeki satıcı vitrini.");
        var profileSchema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Person",
            ["name"] = model.StoreName,
            ["description"] = profileDescription,
            ["url"] = profileCanonicalUrl,
            ["image"] = !string.IsNullOrWhiteSpace(model.LogoUrl) ? model.LogoUrl : model.Listings.FirstOrDefault()?.ImageUrl,
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["addressLocality"] = model.City
            }
        };

        ViewData["MetaDescription"] = profileDescription;
        ViewData["MetaKeywords"] = BuildMetaKeywords(model.StoreName, model.City, "satıcı profili", "aktif ilan", "SEN-T Pazar");
        ViewData["CanonicalUrl"] = profileCanonicalUrl;
        ViewData["MetaImage"] = !string.IsNullOrWhiteSpace(model.LogoUrl) ? model.LogoUrl : model.Listings.FirstOrDefault()?.ImageUrl;
        ViewData["MetaImageAlt"] = model.StoreName;
        ViewData["PageSchemaJsonLd"] = JsonSerializer.Serialize(profileSchema);

        return View("Store", model);
    }

    [HttpGet("/sitemap.xml")]
    public IActionResult SitemapXml()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var urls = new List<(string loc, DateTime? lastmod)>
        {
            ($"{baseUrl}/", null),
            ($"{baseUrl}/Home/Privacy", null),
            ($"{baseUrl}/Home/Terms", null),
            ($"{baseUrl}/Home/Contact", null)
        };

        try
        {
            urls.AddRange(_catalog.Categories
                .Where(x => !string.Equals(x, "all", StringComparison.OrdinalIgnoreCase))
                .Select(x => _catalog.GetDefaultSlug(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(slug => ($"{baseUrl}/kategori/{slug}", (DateTime?)null)));

            urls.AddRange(SeoLandingDefinitions.Values
                .Select(definition => ($"{baseUrl}{BuildSeoLandingPath(definition.Topic)}", (DateTime?)null)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "sitemap.xml icin statik URL listesi olusturulamadi.");
        }

        try
        {
            var listingRows = _context.Listings
                .AsNoTracking()
                .Where(x => x.IsApproved && !x.IsClosed)
                .OrderByDescending(x => x.CreatedAt)
                .Take(2000)
                .Select(x => new { x.Id, x.CreatedAt })
                .ToList();

            urls.AddRange(listingRows
                .Select(x => ($"{baseUrl}/Home/Details/{x.Id}", (DateTime?)x.CreatedAt)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "sitemap.xml icin ilan listesi sorgulanamadi.");
        }

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var item in urls.DistinctBy(x => x.loc))
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{HtmlEncoder.Default.Encode(item.loc)}</loc>");
            if (item.lastmod.HasValue)
            {
                sb.AppendLine($"    <lastmod>{item.lastmod.Value:yyyy-MM-dd}</lastmod>");
            }

            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    public IActionResult RobotsTxt()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var robots = $"User-agent: *\nAllow: /\nSitemap: {baseUrl}/sitemap.xml\n";
        return Content(robots, "text/plain", Encoding.UTF8);
    }


    [Authorize]
    [HttpGet]
    public IActionResult Publish()
    {
        PreparePublishViewData();
        return View(new CreateListingViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(CreateListingViewModel model)
    {
        try
        {
            PreparePublishViewData();

        var normalizedCategory = ListingTaxonomy.NormalizeForPersistence(model.Category, model.SubCategory);
        model.Category = normalizedCategory.Category;
        model.SubCategory = normalizedCategory.SubCategory;
        model.Type = ListingTaxonomy.NormalizeListingType(model.Type);
        SanitizePublishModel(model);
        var isPetAdoption = ListingTaxonomy.IsPetAdoption(model.Category, model.Type);


        // Para birimi boş veya null ise TL olarak ayarla
        if (string.IsNullOrWhiteSpace(model.PriceCurrency))
            model.PriceCurrency = "TL";

        if (isPetAdoption)
        {
            model.PriceAmount = null;
            model.PriceCurrency = "TL";
            model.PriceType = PriceType.Total;
            model.PriceDescription = null;
            model.Negotiable = false;
            model.TradeIn = false;
        }
        else if (model.PriceAmount.HasValue && decimal.Truncate(model.PriceAmount.Value) != model.PriceAmount.Value)
        {
            ModelState.AddModelError(nameof(model.PriceAmount), "Fiyat tam sayi olmalidir.");
        }

        if (model.ImageFiles is { Count: > 0 })
        {
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            foreach (var file in model.ImageFiles)
            {
                var extension = Path.GetExtension(file.FileName);
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(model.ImageFiles), "Sadece JPG, PNG ve WEBP dosyalari yukleyebilirsiniz.");
                    break;
                }

                if (file.Length > 15 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(model.ImageFiles), "Her bir gorsel en fazla 15 MB olabilir.");
                    break;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Görselleri kaydet
        var imagePaths = new List<string>();
        if (model.ImageFiles != null && model.ImageFiles.Count > 0)
        {
            var uploadsFolder = _uploadStorage.EnsureDirectory();
            
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
                    var savedImagePath = await SaveOptimizedImageAsync(file, uploadsFolder, _uploadStorage.GetPublicDirectory());
                    if (string.IsNullOrWhiteSpace(savedImagePath))
                    {
                        ModelState.AddModelError(nameof(model.ImageFiles), "Yüklenen görsellerden biri geçerli bir resim dosyası değil.");
                        return View(model);
                    }

                    imagePaths.Add(savedImagePath);
                }
            }
        }

        // İlanı oluştur ve kaydet
        var imageList = new List<ListingImage>();
        foreach (var p in imagePaths)
        {
            imageList.Add(new ListingImage { FilePath = p, UserId = null });
        }

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
             District = model.District ?? string.Empty,
             Neighborhood = model.Neighborhood,
             HouseNumber = model.HouseNumber,
             ApartmentNumber = model.ApartmentNumber,
             Address = model.Address,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            
            // Kategori ve Tip
            Category = model.Category,
            SubCategory = model.SubCategory,
            Type = model.Type,
            
            // Fiyat Bilgileri
            PriceAmount = isPetAdoption ? 0 : model.PriceAmount.GetValueOrDefault(),
            PriceCurrency = isPetAdoption ? "TL" : model.PriceCurrency,
            PriceType = isPetAdoption ? PriceType.Total.ToString() : model.PriceType.ToString(),
            PriceDescription = isPetAdoption ? null : model.PriceDescription,
            Negotiable = !isPetAdoption && model.Negotiable,
            TradeIn = !isPetAdoption && model.TradeIn,
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
            VehicleCondition = model.VehicleCondition?.ToString(),
            VehicleSteeringType = model.VehicleSteeringType?.ToString(),
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
            SellerIsCorporate = User.Identity?.IsAuthenticated == true 
                ? _context.Users.Where(u => u.UserName == User.Identity!.Name).Select(u => u.IsCorporateMember).FirstOrDefault() 
                : false,
             
            // Durum
            CreatedAt = DateTime.UtcNow,
            IsApproved = false,
            ViewCount = 0,
            IsClosed = false,
            PublishUntil = DateTime.UtcNow.AddDays(60),
            ExpiryReminderSent = false,
            DealStatus = "open",
            
            // Görseller
            Images = imageList
        };

        await AutoTranslateListingContentAsync(listing);
        
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();

        // Yöneticilere e-posta gönder
        var adminEmails = new[] { "taskinonel@gmail.com" };
        var subject = "Yeni İlan Başvurusu - " + model.Category.ToUpper();
        var priceLine = isPetAdoption
            ? "<b>Fiyat:</b> Ücretsiz sahiplendirme<br>"
            : $"<b>Fiyat:</b> {model.PriceAmount.GetValueOrDefault():N0} {model.PriceCurrency}<br>";
        var categoryDetails = ListingTaxonomy.IsLandEstate(model.Category, model.SubCategory)
            ? $@"<b>Arsa Detayları:</b><br>
                Net/Brüt M²: {model.EstateNetArea}/{model.EstateGrossArea}<br>
                Bölge: {model.City} / {model.Neighborhood}<br>
                Açıklama Notu: {model.PriceDescription}<br>"
            : model.Category switch
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
                _ => $@"<b>İlan Detayları:</b><br>
                    Alt Kategori: {model.SubCategory}<br>
                    Marka / Sağlayıcı: {model.ProductBrand}<br>
                    Model / Paket: {model.ProductModel}<br>
                    Durum: {model.ProductCondition}<br>"
            };
        
        var adminBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var body = $@"<h2>Yeni İlan Başvurusu</h2>
        <b>İlan No:</b> #{listing.Id}<br>
        <b>İlan Sahibi:</b> {model.FullName}<br>
        <b>Telefon:</b> {model.Phone}<br>
        <b>Başlık:</b> {model.Title}<br>
        <b>Konum:</b> {model.City}, {model.Neighborhood}<br>
        <b>Kategori:</b> {model.Category}<br>
        <b>Tip:</b> {model.Type}<br>
        {priceLine}
        <b>Kimden:</b> {model.AdvertiserType}<br>
        <hr>
        {categoryDetails}
        <hr>
        <b>Açıklama:</b><br>{model.Description}<br>
        <hr>
        <b>Görseller:</b><br>{string.Join("<br>", imagePaths.Select(p => $"<a href='{adminBaseUrl}{p}'>{p}</a>"))}
        <hr>
        <a href='{adminBaseUrl}/Admin/Listings' style='padding:10px 20px;background:#667eea;color:#fff;text-decoration:none;border-radius:5px;'>İlanı Yönet</a>
        ";
        
        foreach (var email in adminEmails)
        {
            try { _emailSender.SendAsync(email, subject, body).Wait(); } catch { }
        }

        await _userMessageAutomationService.SendListingSubmittedAsync(listing);

        TempData["PublishSuccess"] = "İlanınız başarıyla alındı! Onay sürecinden sonra yayına girecektir. İlan No: #" + listing.Id;
        return RedirectToAction(nameof(Publish));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Publish failed for user {UserId}", User.Identity?.Name);
            PreparePublishViewData();
            var errorMessage = $"Hata: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $" | Inner: {ex.InnerException.Message}";
            }
            ModelState.AddModelError(string.Empty, errorMessage);
            return View(model);
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    private static void SanitizePublishModel(CreateListingViewModel model)
    {
        var normalized = ListingTaxonomy.NormalizeForPersistence(model.Category, model.SubCategory);
        model.Category = normalized.Category;
        model.SubCategory = normalized.SubCategory;
        model.Type = ListingTaxonomy.NormalizeListingType(model.Type);
        model.ProductBrand = CleanOptionalText(model.ProductBrand);
        model.ProductModel = CleanOptionalText(model.ProductModel);
        model.WarrantyPeriod = CleanOptionalText(model.WarrantyPeriod);
        model.SerialNumber = CleanOptionalText(model.SerialNumber);
        model.UsageDuration = CleanOptionalText(model.UsageDuration);
        model.VehicleBrand = CleanOptionalText(model.VehicleBrand);
        model.VehicleModel = CleanOptionalText(model.VehicleModel);
        model.VehicleColor = CleanOptionalText(model.VehicleColor);
        model.VehiclePlate = CleanOptionalText(model.VehiclePlate);
        model.AccidentRecord = CleanOptionalText(model.AccidentRecord);

        if (!ListingTaxonomy.IsValidListingType(model.Category, model.Type))
        {
            model.Type = string.Empty;
        }

        if (!ListingTaxonomy.RequiresSubCategory(model.Category))
        {
            model.SubCategory = null;
        }
        else if (!ListingTaxonomy.IsValidSubCategory(model.Category, model.SubCategory))
        {
            model.SubCategory = null;
        }

        if (!ListingTaxonomy.IsEstateCategory(model.Category))
        {
            ClearEstateFields(model);
        }
        else if (!ListingTaxonomy.IsResidentialEstate(model.Category, model.SubCategory))
        {
            ClearResidentialEstateOnlyFields(model);
        }

        if (!ListingTaxonomy.IsVehicleCategory(model.Category))
        {
            ClearVehicleFields(model);
        }

        if (!ListingTaxonomy.IsProductPanelCategory(model.Category))
        {
            ClearProductFields(model);
        }
        else
        {
            var productFieldVisibility = ListingTaxonomy.GetPublishProductFieldVisibility(model.Category);
            if (!productFieldVisibility.ShowBrand)
            {
                model.ProductBrand = null;
            }

            if (!productFieldVisibility.ShowModel)
            {
                model.ProductModel = null;
            }

            if (!productFieldVisibility.ShowWarranty)
            {
                model.WarrantyPeriod = null;
            }

            if (!productFieldVisibility.ShowSerial)
            {
                model.SerialNumber = null;
            }

            if (!productFieldVisibility.ShowUsage)
            {
                model.UsageDuration = null;
            }

            if (!ListingTaxonomy.RequiresProductCondition(model.Category))
            {
                model.ProductCondition = null;
            }
        }
    }

    private static void ClearEstateFields(CreateListingViewModel model)
    {
        model.EstateNetArea = null;
        model.EstateGrossArea = null;
        model.EstateRoomCount = null;
        model.EstateBuildingAge = null;
        model.EstateTotalFloors = null;
        model.EstateFloorLocation = null;
        model.HeatingType = null;
        model.EstateFurnished = false;
        model.InSite = false;
        model.HasBalcony = false;
        model.HasElevator = false;
        model.HasParking = false;
        model.HasPool = false;
        model.HasSecurity = false;
        model.DuesAmount = null;
        model.DepositAmount = null;
    }

    private static void ClearResidentialEstateOnlyFields(CreateListingViewModel model)
    {
        model.EstateRoomCount = null;
        model.EstateBuildingAge = null;
        model.EstateTotalFloors = null;
        model.EstateFloorLocation = null;
        model.HeatingType = null;
        model.EstateFurnished = false;
        model.InSite = false;
        model.HasBalcony = false;
        model.HasElevator = false;
        model.HasParking = false;
        model.HasPool = false;
        model.HasSecurity = false;
        model.DuesAmount = null;
        model.DepositAmount = null;
    }

    private static void ClearVehicleFields(CreateListingViewModel model)
    {
        model.VehicleBrand = null;
        model.VehicleModel = null;
        model.VehicleYear = null;
        model.VehicleFuelType = null;
        model.VehicleTransmission = null;
        model.VehicleKM = null;
        model.VehicleBodyType = null;
        model.EngineCapacity = null;
        model.EnginePower = null;
        model.VehicleColor = null;
        model.VehiclePlate = null;
        model.UnderWarranty = false;
        model.AccidentRecord = null;
        model.VehicleCondition = null;
        model.VehicleSteeringType = null;
    }

    private static void ClearProductFields(CreateListingViewModel model)
    {
        model.ProductBrand = null;
        model.ProductModel = null;
        model.ProductCondition = null;
        model.WarrantyPeriod = null;
        model.SerialNumber = null;
        model.UsageDuration = null;
    }

    private static string? CleanOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    public IActionResult Terms()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(string name, string email, string subject, string message)
    {
        TempData["ContactSuccess"] = "Mesajınız alındı. Ekibimiz size en kısa sürede dönüş yapacaktır.";
        return RedirectToAction(nameof(Contact));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendListingMessage(int listingId, string senderName, string senderEmail, string? senderPhone, string subject, string message)
    {
        var listing = await _context.Listings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == listingId && x.AllowMessages);
        if (listing == null)
        {
            TempData["ContactSuccess"] = "Ilan bulunamadi veya mesajlasma kapali.";
            return RedirectToAction(nameof(Details), new { id = listingId });
        }

        senderName = (senderName ?? string.Empty).Trim();
        senderEmail = (senderEmail ?? string.Empty).Trim();
        subject = (subject ?? string.Empty).Trim();
        message = (message ?? string.Empty).Trim();
        senderPhone = string.IsNullOrWhiteSpace(senderPhone) ? null : senderPhone.Trim();

        if (string.IsNullOrWhiteSpace(senderName) || string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            TempData["ContactSuccess"] = "Mesaj gondermek icin tum zorunlu alanlari doldurun.";
            return RedirectToAction(nameof(Details), new { id = listingId });
        }

        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(senderEmail))
        {
            TempData["ContactSuccess"] = "Gecerli bir e-posta adresi giriniz.";
            return RedirectToAction(nameof(Details), new { id = listingId });
        }

        ApplicationUser? owner = null;
        if (!string.IsNullOrWhiteSpace(listing.UserId))
        {
            owner = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == listing.UserId);
        }

        var stored = new VisitorMessage
        {
            ListingId = listing.Id,
            ConversationId = Guid.NewGuid().ToString("N"),
            RecipientUserId = listing.UserId,
            RecipientPhone = listing.Phone,
            RecipientEmail = owner?.Email,
            SenderUserId = null,
            SenderName = senderName,
            SenderEmail = senderEmail,
            SenderPhone = senderPhone,
            SenderRole = "visitor",
            Subject = subject,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow,
            IsRead = false
        };

        _context.VisitorMessages.Add(stored);
        await _context.SaveChangesAsync();

        if (owner != null)
        {
            if (!string.IsNullOrWhiteSpace(owner?.Email) && owner.EmailNotifications)
            {
                try
                {
                    await _emailSender.SendAsync(owner.Email!, "Ilaniniz icin yeni mesaj", $"<p><b>{stored.SenderName}</b> kullanicisindan yeni mesaj aldiniz.</p><p><b>Konu:</b> {stored.Subject}</p><p>{stored.Message}</p>");
                }
                catch
                {
                    // no-op
                }
            }

            if (owner != null)
            {
                try
                {
                    await _pushNotificationService.SendToUserAsync(
                        owner,
                        "Yeni mesajınız var",
                        $"{stored.SenderName}, ilanınız için mesaj gönderdi.",
                        new Dictionary<string, string>
                        {
                            ["type"] = "listing_message",
                            ["listingId"] = listing.Id.ToString(),
                            ["messageId"] = stored.Id.ToString()
                        });
                }
                catch
                {
                    // no-op
                }
            }
        }

        // Notify admin about new visitor message
        var adminEmail = (_configuration["Notifications:AdminEmail"] ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            try
            {
                await _emailSender.SendAsync(adminEmail, "Yeni ziyaretçi mesajı", 
                    $"<p><b>{stored.SenderName}</b> adlı ziyaretçiden yeni bir mesaj alındı.</p>" +
                    $"<p><b>İlan:</b> {listing.Title}</p>" +
                    $"<p><b>Konu:</b> {stored.Subject}</p>" +
                    $"<p><b>Mesaj:</b> {stored.Message}</p>");
            }
            catch
            {
                // no-op
            }
        }

        TempData["ContactSuccess"] = "Mesajiniz gonderildi.";
        return RedirectToAction(nameof(Details), new { id = listingId });
    }

    public IActionResult Faq()
    {
        return View();
    }

    public IActionResult Favorites()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/Account/Favorites" });
        }

        return LocalRedirect("/Account/Favorites");
    }

    public IActionResult Messages()
    {
        return View();
    }

    public IActionResult Membership()
    {
        return View();
    }

    public IActionResult Kvkk()
    {
        return View();
    }

    public IActionResult CorporateAgreement()
    {
        return View();
    }

    public IActionResult SponsorInfo()
    {
        return View();
    }

    public IActionResult SponsorApplication()
    {
        return View();
    }

    [HttpPost]
    public IActionResult SponsorApplicationPost()
    {
        // Ödeme ve form işlemleri başarılı varsayılır
        TempData["SuccessMessage"] = "Sponsorluk başvurunuz ve ödemeniz başarıyla alındı. Editör onayının ardından kampanyanız başlayacaktır.";
        return RedirectToAction("Index");
    }

    public IActionResult VitrinAd()
    {
        return View();
    }

    public IActionResult SecurePayment()
    {
        return View();
    }

    public IActionResult SecurityPolicy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exceptionHandler = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var errorDetail = exceptionHandler?.Error?.ToString();
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorDetail = errorDetail
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SaveSearch([FromBody] SaveSearchRequest? request)
    {
        if (request == null || request.Query == null || request.Query.Count == 0)
        {
            return BadRequest(new { success = false, message = "Arama kriteri bulunamadi." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var serialized = await _userManager.GetAuthenticationTokenAsync(user, "SEN-TPAZAR", "saved-searches");
        var list = string.IsNullOrWhiteSpace(serialized)
            ? new List<SavedSearchItem>()
            : JsonSerializer.Deserialize<List<SavedSearchItem>>(serialized) ?? new List<SavedSearchItem>();

        list.Insert(0, new SavedSearchItem
        {
            CreatedAtUtc = DateTime.UtcNow,
            Query = request.Query,
            Path = string.IsNullOrWhiteSpace(request.Path) ? "/" : request.Path.Trim()
        });

        list = list
            .GroupBy(x => JsonSerializer.Serialize(x.Query))
            .Select(g => g.OrderByDescending(x => x.CreatedAtUtc).First())
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(10)
            .ToList();

        await _userManager.SetAuthenticationTokenAsync(
            user,
            "SEN-TPAZAR",
            "saved-searches",
            JsonSerializer.Serialize(list));

        return Ok(new { success = true, count = list.Count });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> SavedSearches()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var serialized = await _userManager.GetAuthenticationTokenAsync(user, "SEN-TPAZAR", "saved-searches");
        var list = string.IsNullOrWhiteSpace(serialized)
            ? new List<SavedSearchItem>()
            : JsonSerializer.Deserialize<List<SavedSearchItem>>(serialized) ?? new List<SavedSearchItem>();

        return Json(list.OrderByDescending(x => x.CreatedAtUtc));
    }

    [Route("Home/HttpStatusPage")]
    public IActionResult HttpStatusPage(int code)
    {
        Response.StatusCode = code;
        ViewData["StatusCode"] = code;
        return View();
    }

    public sealed class SaveSearchRequest
    {
        public string? Path { get; set; }
        public Dictionary<string, string>? Query { get; set; }
    }

    public sealed class SavedSearchItem
    {
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastNotifiedAtUtc { get; set; }
        public string Path { get; set; } = "/";
        public Dictionary<string, string> Query { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<PropertyCard> ApplyFilters(
        IEnumerable<PropertyCard> source,
        string listingType,
        string city,
        string category,
        string priceRange,
        decimal? minPrice,
        decimal? maxPrice,
        string keyword,
        string subCategory = "all")
    {
        var query = source;

        var normalizedSubCategory = ListingTaxonomy.NormalizeSubCategory(subCategory);
        var isRentDailySubCategory = string.Equals(listingType, "rent", StringComparison.OrdinalIgnoreCase)
            && string.Equals(normalizedSubCategory, "gunluk-kiralik", StringComparison.OrdinalIgnoreCase);

        if (listingType != "all" && !isRentDailySubCategory)
        {
            query = query.Where(x => x.Type.Equals(listingType, StringComparison.OrdinalIgnoreCase));
        }

        if (city != "all")
        {
            if (city.Contains("::", StringComparison.Ordinal))
            {
                var parts = city.Split("::", 2, StringSplitOptions.TrimEntries);
                var cityPart = parts[0];
                var villagePart = parts.Length > 1 ? parts[1] : string.Empty;

                query = query.Where(x =>
                    x.City.Equals(cityPart, StringComparison.OrdinalIgnoreCase) &&
                    (x.Neighborhood.Contains(villagePart, StringComparison.OrdinalIgnoreCase) ||
                     x.Location.Contains(villagePart, StringComparison.OrdinalIgnoreCase) ||
                     x.Summary.Contains(villagePart, StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                query = query.Where(x => x.City.Equals(city, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (category != "all")
        {
            query = query.Where(x => ListingTaxonomy.MatchesSearchCategory(category, x.Category, x.SubCategory));
        }

        if (!string.IsNullOrWhiteSpace(subCategory) && subCategory != "all")
        {
            if (isRentDailySubCategory)
            {
                query = query.Where(x => x.Type.Equals("daily", StringComparison.OrdinalIgnoreCase)
                    && ListingTaxonomy.MatchesSearchCategory("realestate", x.Category, x.SubCategory));
            }
            else
            {
                var terms = subCategory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(ListingTaxonomy.NormalizeSubCategory)
                    .Where(term => !string.IsNullOrWhiteSpace(term))
                    .Select(term => term!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (terms.Count > 0)
                {
                    query = query.Where(x => ListingTaxonomy.MatchesSearchSubCategory(category, terms, x.Category, x.SubCategory));
                }
            }
        }

        // Gelişmiş fiyat filtreleme (minPrice / maxPrice öncelikli)
        if (minPrice.HasValue)
        {
            query = query.Where(x => x.PriceAmount >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.PriceAmount <= maxPrice.Value);
        }

        // Geriye uyumluluk için eski priceRange preset'leri (eğer min/max verilmemişse)
        if (!minPrice.HasValue && !maxPrice.HasValue)
        {
            query = priceRange switch
            {
                "low" => query.Where(x => x.PriceAmount <= 5000),
                "mid" => query.Where(x => x.PriceAmount > 5000 && x.PriceAmount <= 50000),
                "high" => query.Where(x => x.PriceAmount > 50000),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var keywordTerms = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            query = query.Where(x => keywordTerms.All(term =>
                x.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.Location.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.Neighborhood.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.PrimarySpec.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.SecondarySpec.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        return query;
    }

    private string NormalizeCityFilter(string city)
    {
        if (string.IsNullOrWhiteSpace(city) || city.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return "all";
        }

        if (city.Contains("::", StringComparison.Ordinal))
        {
            var cityPart = city.Split("::", 2, StringSplitOptions.TrimEntries)[0];
            return _catalog.Cities.Contains(cityPart) ? city : "all";
        }

        return _catalog.Cities.Contains(city) ? city : "all";
    }

    private static string NormalizeSearchSubCategorySelection(string category, string? subCategory)
    {
        if (string.IsNullOrWhiteSpace(subCategory) || string.Equals(subCategory, "all", StringComparison.OrdinalIgnoreCase) || string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
        {
            return "all";
        }

        var allowed = ListingTaxonomy.GetSearchSubCategoryFilters(category)
            .Select(x => x.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var normalizedSelections = subCategory
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ListingTaxonomy.NormalizeSubCategory)
            .Where(x => !string.IsNullOrWhiteSpace(x) && allowed.Contains(x!))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalizedSelections.Count == 0 ? "all" : string.Join(',', normalizedSelections);
    }

    private static string NormalizeSearchKeyword(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return string.Empty;
        }

        var terms = keyword
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length >= 2)
            .Take(6);

        return string.Join(' ', terms);
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

    private Dictionary<string, List<SubCategoryFilter>> BuildSubCategoryMap()
    {
        return ListingTaxonomy.BuildSearchCategorySubCategoryMap(_catalog.Categories);
    }

    private List<SearchTabOption> BuildSearchTabs(string listingType, string category)
    {
        return ListingTaxonomy.GetHomepageSearchTabs()
            .Select(tab => new SearchTabOption
            {
                Key = tab.Key,
                Label = tab.Key == "helper"
                    ? _localizer.CategoryLabel(tab.PresetCategory)
                    : _localizer.TypeLabel(tab.ListingType),
                ListingType = tab.ListingType,
                PresetCategory = tab.PresetCategory,
                IsActive = tab.Key switch
                {
                    "helper" => string.Equals(listingType, "job", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(category, "helper", StringComparison.OrdinalIgnoreCase),
                    "job" => string.Equals(listingType, "job", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(category, "helper", StringComparison.OrdinalIgnoreCase),
                    _ => string.Equals(listingType, tab.ListingType, StringComparison.OrdinalIgnoreCase)
                }
            })
            .ToList();
    }

    private static string SerializeCategorySubCategoryMap(IReadOnlyDictionary<string, List<SubCategoryFilter>> categorySubCategoryMap)
    {
        var payload = categorySubCategoryMap.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.Select(item => new { value = item.Value, label = item.Label }).ToList(),
            StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(payload);
    }

    private static List<SubCategoryFilter> GetSubCategoryFilters(IReadOnlyDictionary<string, List<SubCategoryFilter>> categorySubCategoryMap, string category)
    {
        if (string.IsNullOrWhiteSpace(category) || string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return categorySubCategoryMap.TryGetValue(NormalizeCategoryCode(category), out var filters)
            ? filters
            : [];
    }

    private List<PropertyCard> BuildRecommendations(List<PropertyCard> filtered, IReadOnlyList<PropertyCard> source, string category, string city)
    {
        var recommendations = filtered
            .Where(x =>
                (category != "all" && ListingTaxonomy.MatchesSearchCategory(category, x.Category, x.SubCategory)) ||
                (city != "all" && x.City.Equals(city, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(_ => Guid.NewGuid())
            .Take(4)
            .ToList();

        if (recommendations.Count < 4)
        {
            var more = source
                .Except(recommendations)
                .OrderBy(_ => Guid.NewGuid())
                .Take(4 - recommendations.Count);
            
            recommendations.AddRange(more);
        }

        return recommendations;
    }

    private static void ApplySellerCardSummaries(IEnumerable<PropertyCard> cards, IReadOnlyDictionary<string, SellerCardSummary> sellerSummaries)
    {
        foreach (var card in cards)
        {
            if (string.IsNullOrWhiteSpace(card.UserId) || !sellerSummaries.TryGetValue(card.UserId, out var seller))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(card.SellerName) || string.Equals(card.SellerName, "Bilinmeyen Satıcı", StringComparison.OrdinalIgnoreCase))
            {
                card.SellerName = string.IsNullOrWhiteSpace(seller.FullName) ? card.SellerName : seller.FullName;
            }

            card.SellerIsCorporate = seller.IsCorporateMember;
            card.SellerAvatarUrl = seller.AvatarUrl;
            card.SellerCompanyName = seller.CompanyName;
            card.SellerCompanyLogoUrl = seller.CompanyLogoUrl;
        }
    }

    private sealed class SellerCardSummary
    {
        public string UserId { get; init; } = string.Empty;

        public string FullName { get; init; } = string.Empty;

        public string AvatarUrl { get; init; } = string.Empty;

        public bool IsCorporateMember { get; init; }

        public string CompanyName { get; init; } = string.Empty;

        public string CompanyLogoUrl { get; init; } = string.Empty;
    }

    private PropertyCard MapListingToPropertyCard(Listing l)
    {
        var culture = _localizer.CultureCode;
        var localizedTitle = GetLocalizedListingText(l.Title, l.TitleEn, l.TitleRu, l.TitleAr, l.TitleFa, culture);
        var localizedDescription = GetLocalizedListingText(l.Description, l.DescriptionEn, l.DescriptionRu, l.DescriptionAr, l.DescriptionFa, culture);

        if (!string.Equals(culture, "tr", StringComparison.OrdinalIgnoreCase))
        {
            var translated = EnsureListingTranslation(culture, l.Title, l.Description, l.TitleEn, l.TitleRu, l.TitleAr, l.TitleFa, l.DescriptionEn, l.DescriptionRu, l.DescriptionAr, l.DescriptionFa);
            localizedTitle = translated.title;
            localizedDescription = translated.description;
        }

        var resolvedGallery = (l.Images ?? new List<ListingImage>())
            .Where(i => !string.IsNullOrWhiteSpace(i.FilePath))
            .Select(i => i.FilePath)
            .ToList();
        var browseCategory = ListingTaxonomy.GetBrowseCategoryCode(l.Category, l.SubCategory);
        if (resolvedGallery.Count == 0)
        {
            resolvedGallery.Add(GetCategoryFallbackImageUrl(browseCategory));
        }

        var isResidentialEstate = ListingTaxonomy.IsResidentialEstate(l.Category, l.SubCategory);
        var estateFeatures = isResidentialEstate ? ExtractEstateFeaturePayload(l.Tags) : new EstateFeaturePayload();
        var exteriorFeatures = new List<string>(estateFeatures.Exterior);
        var interiorFeatures = new List<string>(estateFeatures.Interior);
        var locationFeatures = new List<string>(estateFeatures.Location);

        // Kategori bazlı Facts oluştur
        var facts = new List<ListingFact>();
        
        if (ListingTaxonomy.IsEstateCategory(l.Category))
        {
            if (l.EstateNetArea.HasValue) facts.Add(new ListingFact { Label = "Net M²", Value = l.EstateNetArea + " m²" });
            if (l.EstateGrossArea.HasValue) facts.Add(new ListingFact { Label = "Brüt M²", Value = l.EstateGrossArea + " m²" });
            if (!string.IsNullOrEmpty(l.EstateRoomCount)) facts.Add(new ListingFact { Label = "Oda", Value = DisplayEnumValue<EstateRoomCount>(l.EstateRoomCount) });
            if (!string.IsNullOrEmpty(l.EstateBuildingAge)) facts.Add(new ListingFact { Label = "Bina Yaşı", Value = DisplayEnumValue<EstateBuildingAge>(l.EstateBuildingAge) });
            if (l.EstateTotalFloors.HasValue) facts.Add(new ListingFact { Label = "Toplam Kat", Value = l.EstateTotalFloors.Value.ToString() });
            if (!string.IsNullOrEmpty(l.EstateFloorLocation)) facts.Add(new ListingFact { Label = "Bulunduğu Kat", Value = DisplayEnumValue<EstateFloorLocation>(l.EstateFloorLocation) });
            if (!string.IsNullOrEmpty(l.HeatingType)) facts.Add(new ListingFact { Label = "Isıtma", Value = DisplayEnumValue<HeatingType>(l.HeatingType) });
            if (l.EstateFurnished.HasValue) facts.Add(new ListingFact { Label = "Eşyalı", Value = l.EstateFurnished.Value ? "Evet" : "Hayır" });
            if (l.HasBalcony.HasValue && l.HasBalcony.Value) facts.Add(new ListingFact { Label = "Balkon", Value = "Var" });
            if (l.HasParking.HasValue && l.HasParking.Value) facts.Add(new ListingFact { Label = "Otopark", Value = "Var" });
            if (l.HasPool.HasValue && l.HasPool.Value) facts.Add(new ListingFact { Label = "Havuz", Value = "Var" });
            if (l.DuesAmount.HasValue && l.DuesAmount > 0) facts.Add(new ListingFact { Label = "Aidat", Value = l.DuesAmount.Value.ToString("N0") + " TL" });

            if (isResidentialEstate)
            {
                if (l.HasBalcony == true) AppendUnique(exteriorFeatures, "Balkon");
                if (l.HasPool == true) AppendUnique(exteriorFeatures, "Yüzme Havuzu");
                if (l.HasParking == true) AppendUnique(exteriorFeatures, "Açık Otopark");
                if (l.HasSecurity == true) AppendUnique(exteriorFeatures, "Güvenlik Kamerası");
                if (l.HasElevator == true) AppendUnique(exteriorFeatures, "Asansör");

                if (l.EstateFurnished == true) AppendUnique(interiorFeatures, "Eşyalı");
                if (!string.IsNullOrWhiteSpace(l.HeatingType)) AppendUnique(interiorFeatures, "Isıtma: " + l.HeatingType);
                if (!string.IsNullOrWhiteSpace(l.EstateRoomCount)) AppendUnique(interiorFeatures, "Oda: " + l.EstateRoomCount);

                if (!string.IsNullOrWhiteSpace(l.City)) AppendUnique(locationFeatures, "Şehir: " + l.City);
                if (!string.IsNullOrWhiteSpace(l.District)) AppendUnique(locationFeatures, "İlçe: " + l.District);
                if (!string.IsNullOrWhiteSpace(l.Neighborhood)) AppendUnique(locationFeatures, "Mahalle: " + l.Neighborhood);
                if (l.InSite == true) AppendUnique(locationFeatures, "Site İçerisinde");
            }
        }
        else if (ListingTaxonomy.IsVehicleCategory(l.Category))
        {
            if (!string.IsNullOrEmpty(l.VehicleBrand)) facts.Add(new ListingFact { Label = "Marka", Value = l.VehicleBrand });
            if (!string.IsNullOrEmpty(l.VehicleModel)) facts.Add(new ListingFact { Label = "Model", Value = l.VehicleModel });
            if (l.VehicleYear.HasValue) facts.Add(new ListingFact { Label = "Yıl", Value = l.VehicleYear.Value.ToString() });
            if (l.VehicleKM.HasValue) facts.Add(new ListingFact { Label = "KM", Value = l.VehicleKM.Value.ToString("N0") + " km" });
            if (!string.IsNullOrEmpty(l.VehicleFuelType)) facts.Add(new ListingFact { Label = "Yakıt", Value = DisplayEnumValue<FuelType>(l.VehicleFuelType) });
            if (!string.IsNullOrEmpty(l.VehicleTransmission)) facts.Add(new ListingFact { Label = "Vites", Value = DisplayEnumValue<TransmissionType>(l.VehicleTransmission) });
            if (!string.IsNullOrEmpty(l.VehicleBodyType)) facts.Add(new ListingFact { Label = "Kasa", Value = DisplayEnumValue<BodyType>(l.VehicleBodyType) });
            if (!string.IsNullOrEmpty(l.VehicleCondition)) facts.Add(new ListingFact { Label = "Araç Durumu", Value = DisplayEnumValue<VehicleConditionState>(l.VehicleCondition) });
            if (!string.IsNullOrEmpty(l.VehicleColor)) facts.Add(new ListingFact { Label = "Renk", Value = l.VehicleColor });
            if (l.EngineCapacity.HasValue) facts.Add(new ListingFact { Label = "Motor Hacmi", Value = l.EngineCapacity + " cc" });
            if (l.EnginePower.HasValue) facts.Add(new ListingFact { Label = "Motor Gücü", Value = l.EnginePower + " HP" });
            if (!string.IsNullOrEmpty(l.VehicleSteeringType)) facts.Add(new ListingFact { Label = "Direksiyon", Value = DisplayEnumValue<SteeringType>(l.VehicleSteeringType) });
            if (l.UnderWarranty.HasValue) facts.Add(new ListingFact { Label = "Garanti", Value = l.UnderWarranty.Value ? "Var" : "Yok" });
        }
        else // electronics, phone, computer, home, fashion, services, other
        {
            if (!string.IsNullOrEmpty(l.ProductBrand)) facts.Add(new ListingFact { Label = "Marka", Value = l.ProductBrand });
            if (!string.IsNullOrEmpty(l.ProductModel)) facts.Add(new ListingFact { Label = "Model", Value = l.ProductModel });
            if (!string.IsNullOrEmpty(l.ProductCondition)) facts.Add(new ListingFact { Label = "Durum", Value = DisplayEnumValue<ConditionState>(l.ProductCondition) });
            if (!string.IsNullOrEmpty(l.WarrantyPeriod)) facts.Add(new ListingFact { Label = "Garanti", Value = l.WarrantyPeriod });
            if (!string.IsNullOrEmpty(l.UsageDuration)) facts.Add(new ListingFact { Label = "Kullanım Süresi", Value = l.UsageDuration });
        }
        
        // Ya da hiç fact yoksa generik bilgi ekle
        if (facts.Count == 0)
        {
            facts.Add(new ListingFact { Label = "Kategori", Value = DisplayCategoryLabel(browseCategory) });
            facts.Add(new ListingFact { Label = "Tip", Value = DisplayListingTypeLabel(l.Type) });
        }

        var card = new PropertyCard
        {
            Id = l.Id,
            IsImported = false,
            SourceName = string.Empty,
            IsFeatured = l.IsFeatured && (l.FeaturedExpiryDate == null || l.FeaturedExpiryDate > DateTime.UtcNow),
            IsVitrin = l.IsVitrin && (l.VitrinExpiryDate == null || l.VitrinExpiryDate > DateTime.UtcNow),
            FeaturedOrder = ParseDisplayOrder(l.FeaturedPackage),
            VitrinOrder = ParseDisplayOrder(l.VitrinPackage),
            PopularOrder = l.PopularOrder,
            Title = localizedTitle,
            Summary = !string.IsNullOrEmpty(localizedDescription) && localizedDescription.Length > 120 ? localizedDescription.Substring(0, 120) + "..." : localizedDescription,
            Category = l.Category,
            SubCategory = l.SubCategory ?? string.Empty,
            City = l.City,
            Neighborhood = l.Neighborhood ?? "",
            Location = (l.District ?? "") + ", " + (l.City ?? ""),
            PriceAmount = l.PriceAmount,
            PriceLabel = GetListingPriceLabel(l),
            Type = l.Type,
            PrimarySpec = l.EstateNetArea.HasValue ? l.EstateNetArea + " m²" : (l.VehicleBrand ?? l.ProductBrand ?? ""),
            SecondarySpec = l.EstateRoomCount ?? l.VehicleModel ?? l.ProductModel ?? "",
            Area = l.EstateGrossArea.HasValue ? l.EstateGrossArea + " m²" : "",
            Rooms = l.EstateRoomCount ?? "",
            ImageUrl = resolvedGallery[0],
            GalleryImages = resolvedGallery,
            SellerName = l.FullName ?? "Bilinmeyen Satıcı",
            SellerRole = DisplaySellerRole(l.AdvertiserType),
            SellerCity = l.City ?? string.Empty,
            SellerPhone = l.Phone ?? "",
            UserId = l.UserId ?? string.Empty,
            AllowWhatsApp = l.AllowWhatsApp,
            AllowMessages = l.AllowMessages,
            PostedAtLabel = l.CreatedAt.ToString("dd.MM.yyyy"),
            ListingCode = BuildDbListingCode(l.Category, l.Id),
            DetailBody = localizedDescription ?? "",
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            AverageRating = l.AverageRating,
            ReviewCount = l.ReviewCount,
            Reviews = l.Reviews ?? new List<Review>(),
            Facts = facts,
            ExteriorFeatures = exteriorFeatures,
            InteriorFeatures = interiorFeatures,
            LocationFeatures = locationFeatures,
            IsResidentialEstate = isResidentialEstate,
            Highlights = exteriorFeatures.Concat(interiorFeatures).Concat(locationFeatures).Take(8).ToList(),
            VideoUrl = l.VideoUrl,
            Tour360Url = l.Tour360Url,
            Has360Tour = l.Has360Tour
        };
        return card;
    }

    private const string EstateFeaturePayloadPrefix = "__estate_features_b64:";

    private static EstateFeaturePayload ExtractEstateFeaturePayload(string? rawTags)
    {
        if (string.IsNullOrWhiteSpace(rawTags))
        {
            return new EstateFeaturePayload();
        }

        var marker = rawTags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(x => x.StartsWith(EstateFeaturePayloadPrefix, StringComparison.Ordinal));

        if (string.IsNullOrWhiteSpace(marker))
        {
            return new EstateFeaturePayload();
        }

        var encoded = marker[EstateFeaturePayloadPrefix.Length..];
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return new EstateFeaturePayload();
        }

        try
        {
            var bytes = Convert.FromBase64String(encoded);
            var json = Encoding.UTF8.GetString(bytes);
            var payload = JsonSerializer.Deserialize<EstateFeaturePayload>(json);
            if (payload == null)
            {
                return new EstateFeaturePayload();
            }

            payload.Exterior = payload.Exterior.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            payload.Interior = payload.Interior.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            payload.Location = payload.Location.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return payload;
        }
        catch
        {
            return new EstateFeaturePayload();
        }
    }

    private static void AppendUnique(List<string> source, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!source.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)))
        {
            source.Add(value);
        }
    }

    private sealed class EstateFeaturePayload
    {
        public List<string> Exterior { get; set; } = new();
        public List<string> Interior { get; set; } = new();
        public List<string> Location { get; set; } = new();
    }

    private static string DisplayCategoryLabel(string category)
    {
        return category switch
        {
            "realestate" => "Emlak",
            "land" => "Arsa",
            "vehicle" => "Araç",
            "yacht" => "Yat / Tekne",
            "caravan" => "Karavan",
            "secondhand" => "İkinci El",
            "phone" => "Telefon",
            "computer" => "Bilgisayar",
            "watch" => "Saat",
            "jewelry" => "Mücevher",
            "electronics" => "Elektronik",
            "equipment" => "İş Makineleri",
            "home" => "Ev / Yaşam",
            "fashion" => "Moda",
            "services" => "Hizmet",
            "tutoring" => "Özel Ders",
            "jobs" => "İş İlanları",
            "helper" => "Yardımcı Arayanlar",
            _ => string.IsNullOrWhiteSpace(category) ? "Kategori" : category
        };
    }

    private static (string Category, string SubCategory) FoldLeafSearchCategories(string category, string subCategory)
    {
        if (string.IsNullOrWhiteSpace(category) || string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
        {
            return ("all", "all");
        }

        var needsDefaultSubCategory = string.IsNullOrWhiteSpace(subCategory)
            || string.Equals(subCategory, "all", StringComparison.OrdinalIgnoreCase);

        return category switch
        {
            // Leaf categories should be represented under their parent categories in the homepage/category UX.
            "land" => ("realestate", needsDefaultSubCategory ? "arsa" : subCategory),
            "yacht" => ("vehicle", needsDefaultSubCategory ? "yat" : subCategory),
            "caravan" => ("vehicle", needsDefaultSubCategory ? "karavan" : subCategory),
            "phone" => ("electronics", needsDefaultSubCategory ? "telefon" : subCategory),
            "computer" => ("electronics", needsDefaultSubCategory ? "bilgisayar" : subCategory),
            "watch" => ("fashion", needsDefaultSubCategory ? "saat" : subCategory),
            "jewelry" => ("fashion", needsDefaultSubCategory ? "mucevher" : subCategory),
            _ => (category, subCategory)
        };
    }

    private static string NormalizeCategoryCode(string? category) => ListingTaxonomy.NormalizeSearchCategory(category);

    private static string HumanizeSubCategoryLabel(string rawValue) => ListingTaxonomy.HumanizeSubCategory(rawValue);

private static string GetCategoryFallbackImageUrl(string? category)
    {
        return "/img/placeholder.svg";
    }

    private static string NormalizePublishCategory(string? category) => ListingTaxonomy.NormalizePublishCategory(category);

    private static string? NormalizePublishSubCategory(string? subCategory) => ListingTaxonomy.NormalizeSubCategory(subCategory);

    private static string NormalizeTurkishText(string? value) => ListingTaxonomy.NormalizeText(value);

    private string GetListingPriceLabel(Listing listing)
    {
        if (!ListingTaxonomy.IsPetAdoption(listing.Category, listing.Type))
        {
            return CurrencyDisplay.Format(listing.PriceAmount, listing.PriceCurrency);
        }

        return _localizer.CultureCode switch
        {
            "en" => "Free adoption",
            "ru" => "Бесплатное пристройство",
            "ar" => "تبنٍّ مجاني",
            "fa" => "واگذاری رایگان",
            _ => "Ücretsiz sahiplendirme"
        };
    }

    private static string DisplayListingTypeLabel(string type)
    {
        return type switch
        {
            "sale" => "Satılık",
            "rent" => "Kiralık",
            "daily" => "Günlük Kiralık",
            "service" => "Hizmet Teklifi",
            "lesson" => "Ders Teklifi",
            "job" => "İş İlanı",
            "project" => "Proje",
            "adoption" => "Sahiplendirme",
            _ => string.IsNullOrWhiteSpace(type) ? "Tip" : type
        };
    }

    private static string DisplayEnumValue<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            var member = typeof(TEnum).GetMember(parsed.ToString()).FirstOrDefault();
            var displayName = member?.GetCustomAttribute<DisplayAttribute>()?.Name;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }
        }

        return Regex.Replace(value.Trim(), "([a-z])([A-Z])", "$1 $2");
    }

    private static string DisplaySellerRole(string? advertiserType)
    {
        return advertiserType?.Trim().ToLowerInvariant() switch
        {
            null or "" or "owner" or "bireysel" => "Sahibinden",
            "agent" => "Emlakçıdan",
            "developer" => "İnşaat firmasından",
            _ => "Sahibinden"
        };
    }

    private static bool IsLikelySeedListing(Listing listing)
    {
        if (!string.IsNullOrWhiteSpace(listing.UserId))
        {
            return false;
        }

        var title = listing.Title?.Trim() ?? string.Empty;
        return Regex.IsMatch(title, "#\\d{1,4}$", RegexOptions.CultureInvariant);
    }

    private async Task AutoTranslateListingContentAsync(Listing listing)
    {
        const string source = "auto";
        listing.TitleEn = await _translationService.TranslateAsync(listing.Title, "en", source);
        listing.TitleRu = await _translationService.TranslateAsync(listing.Title, "ru", source);
        listing.TitleAr = await _translationService.TranslateAsync(listing.Title, "ar", source);
        listing.TitleFa = await _translationService.TranslateAsync(listing.Title, "fa", source);
        listing.DescriptionEn = await _translationService.TranslateAsync(listing.Description, "en", source);
        listing.DescriptionRu = await _translationService.TranslateAsync(listing.Description, "ru", source);
        listing.DescriptionAr = await _translationService.TranslateAsync(listing.Description, "ar", source);
        listing.DescriptionFa = await _translationService.TranslateAsync(listing.Description, "fa", source);
    }

    private (string title, string description) EnsureListingTranslation(
        string culture,
        string originalTitle,
        string originalDescription,
        string? titleEn,
        string? titleRu,
        string? titleAr,
        string? titleFa,
        string? descEn,
        string? descRu,
        string? descAr,
        string? descFa)
    {
        if (culture == "en")
        {
            var t = string.IsNullOrWhiteSpace(titleEn) ? _translationService.TranslateAsync(originalTitle, "en", "auto").GetAwaiter().GetResult() : titleEn;
            var d = string.IsNullOrWhiteSpace(descEn) ? _translationService.TranslateAsync(originalDescription, "en", "auto").GetAwaiter().GetResult() : descEn;
            return (string.IsNullOrWhiteSpace(t) ? originalTitle : t, string.IsNullOrWhiteSpace(d) ? originalDescription : d);
        }

        if (culture == "ru")
        {
            var t = string.IsNullOrWhiteSpace(titleRu) ? _translationService.TranslateAsync(originalTitle, "ru", "auto").GetAwaiter().GetResult() : titleRu;
            var d = string.IsNullOrWhiteSpace(descRu) ? _translationService.TranslateAsync(originalDescription, "ru", "auto").GetAwaiter().GetResult() : descRu;
            return (string.IsNullOrWhiteSpace(t) ? originalTitle : t, string.IsNullOrWhiteSpace(d) ? originalDescription : d);
        }

        if (culture == "ar")
        {
            var t = string.IsNullOrWhiteSpace(titleAr) ? _translationService.TranslateAsync(originalTitle, "ar", "auto").GetAwaiter().GetResult() : titleAr;
            var d = string.IsNullOrWhiteSpace(descAr) ? _translationService.TranslateAsync(originalDescription, "ar", "auto").GetAwaiter().GetResult() : descAr;
            return (string.IsNullOrWhiteSpace(t) ? originalTitle : t, string.IsNullOrWhiteSpace(d) ? originalDescription : d);
        }

        if (culture == "fa")
        {
            var t = string.IsNullOrWhiteSpace(titleFa) ? _translationService.TranslateAsync(originalTitle, "fa", "auto").GetAwaiter().GetResult() : titleFa;
            var d = string.IsNullOrWhiteSpace(descFa) ? _translationService.TranslateAsync(originalDescription, "fa", "auto").GetAwaiter().GetResult() : descFa;
            return (string.IsNullOrWhiteSpace(t) ? originalTitle : t, string.IsNullOrWhiteSpace(d) ? originalDescription : d);
        }

        return (originalTitle, originalDescription);
    }

    private static string GetLocalizedListingText(string tr, string? en, string? ru, string? ar, string culture)
    {
        return GetLocalizedListingText(tr, en, ru, ar, null, culture);
    }

    private static string GetLocalizedListingText(string tr, string? en, string? ru, string? ar, string? fa, string culture)
    {
        return culture switch
        {
            "en" => string.IsNullOrWhiteSpace(en) ? tr : en,
            "ru" => string.IsNullOrWhiteSpace(ru) ? tr : ru,
            "ar" => string.IsNullOrWhiteSpace(ar) ? tr : ar,
            "fa" => string.IsNullOrWhiteSpace(fa) ? tr : fa,
            _ => tr
        };
    }

    private static List<string> BuildCategoryFallbackGallery(Listing listing, int count)
    {
        var category = listing.Category?.ToLowerInvariant() ?? "other";
        var pools = GetCategoryFallbackImagePool(category);

        var images = new List<string>();
        var maxCount = Math.Min(Math.Max(count, 1), pools.Length);
        var start = listing.Id > 0 ? listing.Id % pools.Length : 0;
        for (var i = 0; i < maxCount; i++)
        {
            images.Add(pools[(start + i) % pools.Length]);
        }

        return images;
    }

    private static string[] GetCategoryFallbackImagePool(string category)
    {
        return category switch
        {
            "realestate" or "estate" => new[]
            {
                "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1616594039964-3f5d0f2f0d8c?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1560185008-b033106af5c3?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1493666438817-866a91353ca9?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1560185007-5f0bb1866cab?auto=format&fit=crop&w=1200&q=80"
            },
            "land" => new[]
            {
                "https://images.unsplash.com/photo-1500382017468-9049fed747ef?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1472396961693-142e6e269027?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1464822759844-d150baec0494?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?auto=format&fit=crop&w=1200&q=80"
            },
            "vehicle" => new[]
            {
                "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1549921296-3a6b08bb7b57?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1511919884226-fd3cad34687c?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1555215695-3004980ad54e?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=1200&q=80"
            },
            "yacht" => new[]
            {
                "https://images.unsplash.com/photo-1569263979104-865ab7cd8d13?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1562281302-809108fd533c?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1544551763-46a013bb70d5?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1473186578172-c141e6798cf4?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1500375592092-40eb2168fd21?auto=format&fit=crop&w=1200&q=80"
            },
            "caravan" => new[]
            {
                "https://images.unsplash.com/photo-1527786356703-4b100091cd2c?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1501785888041-af3ef285b470?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1521336575822-6da63fb45455?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1470246973918-29a93221c455?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?auto=format&fit=crop&w=1200&q=80"
            },
            "secondhand" => new[]
            {
                "https://images.unsplash.com/photo-1512436991641-6745cdb1723f?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1519710884006-5f6bdb8fd0d3?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1577375729152-4c8b5fcda381?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1572635196237-14b3f281503f?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1493666438817-866a91353ca9?auto=format&fit=crop&w=1200&q=80"
            },
            "phone" => new[]
            {
                "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1512499617640-c74ae3a79d37?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1580910051074-3eb694886505?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1556656793-08538906a9f8?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1598327105666-5b89351aff97?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1605236453806-6ff36851218e?auto=format&fit=crop&w=1200&q=80"
            },
            "computer" => new[]
            {
                "https://images.unsplash.com/photo-1517336714739-489689fd1ca8?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1547082299-de196ea013d6?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1588702547919-26089e690ecc?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1484417894907-623942c8ee29?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1527443224154-c4f0617f5a5c?auto=format&fit=crop&w=1200&q=80"
            },
            "watch" => new[]
            {
                "https://images.unsplash.com/photo-1523170335258-f5ed11844a49?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1547996160-81dfa63595aa?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1434056886845-dac89ffe9b56?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1509048191080-d2e9a3f6a4a6?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1629581672162-420cb0f41da1?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1617043983671-adaadcaa2460?auto=format&fit=crop&w=1200&q=80"
            },
            "jewelry" => new[]
            {
                "https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1535632066927-ab7c9ab60908?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1617038260897-41a1f14a8ca0?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1602173574767-37ac01994b2a?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1596944924616-7b38e7cfac36?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1573408301185-9146fe634ad0?auto=format&fit=crop&w=1200&q=80"
            },
            "electronics" => new[]
            {
                "https://images.unsplash.com/photo-1588508065123-287b28e013da?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1583394838336-acd977736f90?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1593305841991-05c297ba4575?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1546054454-aa26e2b734c7?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1518444028785-8f7d8906599c?auto=format&fit=crop&w=1200&q=80"
            },
            "equipment" => new[]
            {
                "https://images.unsplash.com/photo-1581094794329-c8112a89af12?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1621905251918-48416bd8575a?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1590846083693-f23fdede3a7a?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1504917595217-d4dc5ebe6122?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1467473292607-16a3f9fbcf5f?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1587582423116-ec07293f0395?auto=format&fit=crop&w=1200&q=80"
            },
            "home" => new[]
            {
                "https://images.unsplash.com/photo-1556911220-bff31c812dba?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1556020685-ae41abfc9365?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1618220179428-22790b461013?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1616594039964-3f5d0f2f0d8c?auto=format&fit=crop&w=1200&q=80"
            },
            "fashion" => new[]
            {
                "https://images.unsplash.com/photo-1483985988355-763728e1935b?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1467043237213-65f2da53396f?auto=format&fit=crop&w=1200&q=80"
            },
            "services" => new[]
            {
                "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1581092580497-e0d23cbdf1dc?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1521791136064-7986c2920216?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1489515217757-5fd1be406fef?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1573497019940-1c28c88b4f3e?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?auto=format&fit=crop&w=1200&q=80"
            },
            _ => new[]
            {
                "https://images.unsplash.com/photo-1483985988355-763728e1935b?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1512436991641-6745cdb1723f?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=1200&q=80"
            }
        };
    }

    private static int? ParseDisplayOrder(string? packageMeta)
    {
        if (string.IsNullOrWhiteSpace(packageMeta))
        {
            return null;
        }

        var parts = packageMeta.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (!part.StartsWith("order=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(part[6..], out var parsed) && parsed > 0)
            {
                return parsed;
            }
        }

        return null;
    }

    private static IOrderedEnumerable<PropertyCard> OrderByPromotionPriority(IEnumerable<PropertyCard> source)
    {
        return source
            .OrderByDescending(x => x.IsVitrin)
            .ThenBy(x => x.IsVitrin ? (x.VitrinOrder ?? int.MaxValue) : int.MaxValue)
            .ThenByDescending(x => x.IsFeatured)
            .ThenBy(x => x.IsFeatured ? (x.FeaturedOrder ?? int.MaxValue) : int.MaxValue)
            .ThenByDescending(x => x.Id);
    }

    private static IOrderedEnumerable<PropertyCard> OrderByVitrinPriority(IEnumerable<PropertyCard> source)
    {
        return source
            .OrderByDescending(x => x.IsVitrin)
            .ThenBy(x => x.VitrinOrder ?? int.MaxValue)
            .ThenByDescending(x => x.Id);
    }

    private static IOrderedEnumerable<PropertyCard> OrderByFeaturedPriority(IEnumerable<PropertyCard> source)
    {
        return source
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.FeaturedOrder ?? int.MaxValue)
            .ThenByDescending(x => x.Id);
    }

    private static IOrderedEnumerable<PropertyCard> OrderByPopularPriority(IEnumerable<PropertyCard> source)
    {
        return source
            .OrderBy(x => x.PopularOrder ?? int.MaxValue)
            .ThenByDescending(x => x.Id);
    }

    private static string BuildDbListingCode(string category, int id)
    {
        var prefix = category switch
        {
            "realestate" => "EML",
            "land"       => "ARS",
            "vehicle"    => "VAS",
            "yacht"      => "YAT",
            "caravan"    => "KRV",
            "secondhand" => "IKE",
            "phone"      => "TEL",
            "computer"   => "BLG",
            "watch"      => "SAA",
            "jewelry"    => "MCH",
            "electronics"=> "ELK",
            "equipment"  => "ISM",
            "home"       => "EVY",
            "fashion"    => "MOD",
            "services"   => "HZM",
            _            => "ILN"
        };
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        return $"{prefix}-{datePart}-{id:000000}";
    }

    private async Task<string?> SaveOptimizedImageAsync(IFormFile file, string uploadsFolder, string webRelativeDirectory = "/uploads")
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!await IsValidImageFileAsync(file))
        {
            return null;
        }

        var outputName = $"{Guid.NewGuid():N}.jpg";
        var outputPath = Path.Combine(uploadsFolder, outputName);

        try
        {
            // Copy to memory stream because form file streams are forward-only and non-seekable
            // (IsValidImageFileAsync already read the stream above)
            await using var inputStream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            try
            {
                using var image = await Image.LoadAsync(memoryStream);

                if (image.Width > MaxUploadImageDimension || image.Height > MaxUploadImageDimension)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(MaxUploadImageDimension, MaxUploadImageDimension),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.Lanczos3
                    }));
                }

                var encoder = new JpegEncoder
                {
                    Quality = UploadJpegQuality
                };

                await image.SaveAsJpegAsync(outputPath, encoder);
                var normalizedDirectory = string.IsNullOrWhiteSpace(webRelativeDirectory)
                    ? "/uploads"
                    : webRelativeDirectory.TrimEnd('/');
                return normalizedDirectory + "/" + outputName;
            }
            catch (SixLabors.ImageSharp.UnknownImageFormatException imgEx)
            {
                _logger.LogWarning(imgEx, "Unsupported image format: {FileName} ({Length} bytes)", file.FileName, file.Length);
                return null;
            }
            catch (System.IO.InvalidDataException invEx)
            {
                _logger.LogWarning(invEx, "Invalid image data: {FileName} ({Length} bytes)", file.FileName, file.Length);
                return null;
            }
            catch (Exception innerEx) when (innerEx.GetType().Namespace?.StartsWith("SixLabors", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogWarning(innerEx, "ImageSharp processing error: {FileName} ({Length} bytes)", file.FileName, file.Length);
                return null;
            }
        }
        catch (Exception ex)
        {
            // rethrow unexpected exceptions (e.g., IO permission issues)
            throw;
        }
    }

    private List<RegionalCampaign> BuildRegionalCampaigns(IReadOnlyList<PropertyCard> listings)
    {
        var campaigns = new List<RegionalCampaign>
        {
            new()
            {
                City = "Girne",
                Title = _localizer["campaignGirneTitle"],
                Description = _localizer["campaignGirneDesc"],
                ImageUrl = "https://images.unsplash.com/photo-1464278533981-50106e6176b1?auto=format&fit=crop&w=900&q=80",
                DiscountLabel = "%15",
                Listings = listings.Where(x => x.City == "Girne").Take(6).ToList()
            },
            new()
            {
                City = "İskele",
                Title = _localizer["campaignIskeleTitle"],
                Description = _localizer["campaignIskeleDesc"],
                ImageUrl = "https://images.unsplash.com/photo-1505765050516-f72dcac9c60e?auto=format&fit=crop&w=900&q=80",
                DiscountLabel = "%20",
                Listings = listings.Where(x => x.City == "İskele").Take(6).ToList()
            },
            new()
            {
                City = "Lefkoşa",
                Title = _localizer["campaignLefkosaTitle"],
                Description = _localizer["campaignLefkosaDesc"],
                ImageUrl = "https://images.unsplash.com/photo-1467269204594-9661b134dd2b?auto=format&fit=crop&w=900&q=80",
                DiscountLabel = "%10",
                Listings = listings.Where(x => x.City == "Lefkoşa").Take(6).ToList()
            }
        };
        return campaigns;
    }

    private static async Task<bool> IsValidImageFileAsync(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var buffer = new byte[12];
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
            {
                return true;
            }

            if (read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 && buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
            {
                return true;
            }

            if (read >= 12 && buffer[0] == (byte)'R' && buffer[1] == (byte)'I' && buffer[2] == (byte)'F' && buffer[3] == (byte)'F' && buffer[8] == (byte)'W' && buffer[9] == (byte)'E' && buffer[10] == (byte)'B' && buffer[11] == (byte)'P')
            {
                return true;
            }
        }
        catch
        {
        }

        return false;
    }
}
