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
    private readonly string _cultureCode;
    private readonly CultureInfo _displayCulture;

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
        _cultureCode = NormalizeCultureCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        _displayCulture = _cultureCode switch
        {
            "en" => CultureInfo.GetCultureInfo("en-GB"),
            "ru" => CultureInfo.GetCultureInfo("ru-RU"),
            "ar" => CultureInfo.GetCultureInfo("ar-SA"),
            _ => CultureInfo.GetCultureInfo("tr-TR")
        };

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

    private static string NormalizeCultureCode(string? code)
    {
        return code?.ToLowerInvariant() switch
        {
            "en" => "en",
            "ru" => "ru",
            "ar" => "ar",
            _ => "tr"
        };
    }

    private string T(string tr, string en, string ru, string ar)
    {
        return _cultureCode switch
        {
            "en" => en,
            "ru" => ru,
            "ar" => ar,
            _ => tr
        };
    }

    private string[] GetTitleVariants(string category)
    {
        return category switch
        {
            "realestate" =>
            [
                T("2+1 Deniz Manzaralı Daire", "2+1 Sea View Apartment", "Квартира 2+1 с видом на море", "شقة 2+1 بإطلالة بحرية"),
                T("3+1 Geniş Aile Dairesi", "3+1 Spacious Family Apartment", "Просторная семейная квартира 3+1", "شقة عائلية واسعة 3+1"),
                T("Site İçinde Modern Rezidans", "Modern Residence in Complex", "Современная резиденция в комплексе", "سكن حديث داخل مجمع"),
                T("Merkezi Konumda Şehir Dairesi", "Central City Apartment", "Городская квартира в центре", "شقة مدينة بموقع مركزي")
            ],
            "land" =>
            [
                T("İmarlı Yatırımlık Arsa", "Zoned Investment Land", "Инвестиционный участок с разрешением", "أرض استثمارية مرخصة"),
                T("Yola Cepheli Ticari Parsel", "Commercial Plot on Main Road", "Коммерческий участок у дороги", "قطعة تجارية على الطريق"),
                T("Villa Projesine Uygun Arsa", "Land Suitable for Villa Project", "Участок под виллу", "أرض مناسبة لمشروع فيلا"),
                T("Denize Yakın Gelişim Bölgesi Arsası", "Development Land Near the Sea", "Участок в зоне развития у моря", "أرض تطوير قريبة من البحر")
            ],
            "vehicle" =>
            [
                T("Dizel Otomatik SUV", "Diesel Automatic SUV", "Дизельный SUV с автоматом", "سيارة SUV ديزل أوتوماتيك"),
                T("Düşük KM Aile Sedan", "Low Mileage Family Sedan", "Семейный седан с малым пробегом", "سيدان عائلية بعداد منخفض"),
                T("Şehir İçi Ekonomik Hatchback", "Urban Economy Hatchback", "Экономичный хэтчбек для города", "هاتشباك اقتصادية للمدينة"),
                T("Premium Segment Executive", "Executive Premium Sedan", "Премиальный представительский автомобиль", "سيارة تنفيذية فاخرة")
            ],
            "yacht" =>
            [
                T("45 ft Motor Yat", "45 ft Motor Yacht", "Моторная яхта 45 футов", "يخت بمحرك 45 قدم"),
                T("Aile Tipi Gezi Teknesi", "Family Cruising Boat", "Семейный прогулочный катер", "قارب رحلات عائلي"),
                T("Marina Çıkışlı Premium Yat", "Premium Yacht from Marina", "Премиальная яхта из марины", "يخت فاخر جاهز من المرسى"),
                T("Sezonluk Kiralık Yat", "Seasonal Rental Yacht", "Яхта в аренду на сезон", "يخت للإيجار الموسمي")
            ],
            "caravan" =>
            [
                T("4 Kişilik Aile Karavanı", "Family Caravan for 4", "Семейный караван на 4 человека", "كرفان عائلي لأربعة أشخاص"),
                T("Off-Grid Kamp Karavanı", "Off-Grid Camping Caravan", "Автодом для автономного кемпинга", "كرفان تخييم مستقل"),
                T("Panelvan Dönüşüm Karavan", "Converted Panel Van Caravan", "Переоборудованный фургон-кемпер", "فان محول إلى كرفان"),
                T("Lüks İç Tasarımlı Karavan", "Caravan with Luxury Interior", "Караван с роскошным интерьером", "كرفان بداخلية فاخرة")
            ],
            "secondhand" =>
            [
                T("Masif Ahşap Yemek Masası", "Solid Wood Dining Table", "Обеденный стол из массива дерева", "طاولة طعام من الخشب الصلب"),
                T("Ergonomik Ofis Koltuğu", "Ergonomic Office Chair", "Эргономичное офисное кресло", "كرسي مكتب مريح"),
                T("Az Kullanılmış Koşu Bandı", "Lightly Used Treadmill", "Беговая дорожка в отличном состоянии", "جهاز مشي مستعمل بحالة ممتازة"),
                T("Set Üstü Mutfak Paketi", "Countertop Kitchen Package", "Комплект кухонной техники", "باقة مطبخ متكاملة")
            ],
            "phone" =>
            [
                T("256 GB Akıllı Telefon", "256 GB Smartphone", "Смартфон 256 ГБ", "هاتف ذكي 256 جيجابايت"),
                T("Kamera Odaklı Amiral Gemisi", "Camera-Focused Flagship", "Флагман с продвинутой камерой", "هاتف رائد بكاميرا متقدمة"),
                T("Uzun Batarya Ömürlü Model", "Long Battery Life Model", "Модель с долгой автономностью", "هاتف ببطارية طويلة"),
                T("Kompakt Premium Telefon", "Compact Premium Phone", "Компактный премиум-смартфон", "هاتف فاخر مدمج")
            ],
            "computer" =>
            [
                T("RTX Ekran Kartlı Oyun Laptopu", "Gaming Laptop with RTX GPU", "Игровой ноутбук с RTX", "لابتوب ألعاب مع RTX"),
                T("İçerik Üretimi için Workstation", "Workstation for Content Creation", "Рабочая станция для создания контента", "محطة عمل لصناعة المحتوى"),
                T("Ultra Hafif İş Bilgisayarı", "Ultra-Light Business Laptop", "Ультралегкий бизнес-ноутбук", "حاسوب أعمال خفيف جدًا"),
                T("Masaüstü Performans Sistemi", "Desktop Performance System", "Производительная настольная система", "نظام مكتبي عالي الأداء")
            ],
            "watch" =>
            [
                T("İsviçre Mekanik Saat", "Swiss Mechanical Watch", "Швейцарские механические часы", "ساعة ميكانيكية سويسرية"),
                T("Klasik Çelik Kordon Model", "Classic Steel Bracelet Model", "Классическая модель со стальным браслетом", "ساعة بسوار فولاذي كلاسيكي"),
                T("Spor Kronograf Saat", "Sport Chronograph Watch", "Спортивный хронограф", "ساعة كرونوغراف رياضية"),
                T("Limitli Seri Koleksiyon", "Limited Edition Collection", "Лимитированная серия", "إصدار محدود")
            ],
            "jewelry" =>
            [
                T("Pırlanta Kolye Seti", "Diamond Necklace Set", "Комплект с бриллиантовым ожерельем", "طقم قلادة ألماس"),
                T("Altın Bileklik Koleksiyonu", "Gold Bracelet Collection", "Коллекция золотых браслетов", "مجموعة أساور ذهبية"),
                T("Zümrüt Taşlı Yüzük", "Emerald Ring", "Кольцо с изумрудом", "خاتم بحجر زمرد"),
                T("Özel Tasarım Küpe", "Custom Design Earrings", "Серьги авторского дизайна", "أقراط بتصميم خاص")
            ],
            "electronics" =>
            [
                T("4K OLED Televizyon", "4K OLED Television", "4K OLED телевизор", "تلفزيون OLED بدقة 4K"),
                T("Gürültü Engelleyici Kulaklık", "Noise Cancelling Headphones", "Наушники с шумоподавлением", "سماعات عازلة للضوضاء"),
                T("Akıllı Ev Kamera Seti", "Smart Home Camera Kit", "Комплект камер умного дома", "طقم كاميرات منزل ذكي"),
                T("Profesyonel Ses Sistemi", "Professional Audio System", "Профессиональная аудиосистема", "نظام صوت احترافي")
            ],
            "equipment" =>
            [
                T("3.5 Ton Forklift", "3.5 Ton Forklift", "Погрузчик 3.5 тонны", "رافعة شوكية 3.5 طن"),
                T("Mini Ekskavatör", "Mini Excavator", "Мини-экскаватор", "حفار صغير"),
                T("Jeneratör Güç Ünitesi", "Generator Power Unit", "Генераторная установка", "وحدة مولد كهربائي"),
                T("Platform Lift Sistemi", "Platform Lift System", "Платформенный подъемник", "نظام رفع منصات")
            ],
            "home" =>
            [
                T("L Koltuk Takımı", "L-Shaped Sofa Set", "Угловой диван", "طقم كنبة حرف L"),
                T("6 Kişilik Yemek Odası", "Dining Room for 6", "Столовая группа на 6 персон", "غرفة طعام لستة أشخاص"),
                T("Yatak Odası Komple Set", "Complete Bedroom Set", "Полный комплект спальни", "طقم غرفة نوم كامل"),
                T("Dekoratif Aydınlatma Paketi", "Decorative Lighting Package", "Комплект декоративного освещения", "باقة إضاءة ديكورية")
            ],
            "fashion" =>
            [
                T("Premium Deri Ceket", "Premium Leather Jacket", "Премиальная кожаная куртка", "جاكيت جلدي فاخر"),
                T("Günlük Sneaker Koleksiyonu", "Everyday Sneaker Collection", "Коллекция повседневных кроссовок", "مجموعة أحذية سنيكر يومية"),
                T("Kadın Tasarım Elbise", "Women's Designer Dress", "Женское дизайнерское платье", "فستان نسائي بتصميم مميز"),
                T("Unisex Streetwear Set", "Unisex Streetwear Set", "Комплект streetwear унисекс", "طقم ستريت وير للجنسين")
            ],
            "services" =>
            [
                T("Kurumsal Temizlik Hizmeti", "Corporate Cleaning Service", "Корпоративная уборка", "خدمة تنظيف للشركات"),
                T("Teknik Bakım ve Onarım", "Technical Maintenance and Repair", "Техническое обслуживание и ремонт", "صيانة وإصلاح فني"),
                T("Taşıma ve Lojistik Desteği", "Moving and Logistics Support", "Поддержка в перевозке и логистике", "خدمة نقل ودعم لوجستي"),
                T("Dijital Pazarlama Danışmanlığı", "Digital Marketing Consultancy", "Консалтинг по цифровому маркетингу", "استشارات تسويق رقمي")
            ],
            _ => [T("Öne Çıkan İlan", "Featured Listing", "Рекомендуемое объявление", "إعلان مميز")]
        };
    }

    private string GetSummaryLine(string category)
    {
        return category switch
        {
            "realestate" => T("Kıbrıs'ta oturum ve yatırım için ideal daire.", "An ideal apartment for living or investment in Cyprus.", "Идеальная квартира для жизни и инвестиций на Кипре.", "شقة مثالية للسكن أو الاستثمار في قبرص."),
            "land" => T("Altyapısı hazır ve yatırım değeri güçlü arsa.", "Infrastructure-ready land with strong investment value.", "Участок с готовой инфраструктурой и высоким инвестиционным потенциалом.", "أرض مجهزة بالبنية التحتية وتملك قيمة استثمارية قوية."),
            "vehicle" => T("Bakımları tam, ekspertiz raporlu araç.", "Well-maintained vehicle with inspection report.", "Автомобиль после обслуживания с отчетом экспертизы.", "مركبة مخدومة بالكامل مع تقرير فحص."),
            "yacht" => T("Marina teslim, bakımlı motor yat.", "Well-maintained motor yacht ready at the marina.", "Ухоженная моторная яхта, готовая в марине.", "يخت بمحرك جاهز في المرسى وبحالة ممتازة."),
            "caravan" => T("Uzun yol ve kamp için tam donanım.", "Fully equipped for road trips and camping.", "Полностью оснащен для путешествий и кемпинга.", "مجهز بالكامل للرحلات الطويلة والتخييم."),
            "secondhand" => T("Temiz kullanılmış ve uygun fiyatlı ürün.", "Clean pre-owned product at a fair price.", "Аккуратно использованный товар по разумной цене.", "منتج مستعمل بحالة جيدة وسعر مناسب."),
            "phone" => T("Kutulu, faturalı ve pil sağlığı yüksek cihaz.", "Boxed device with invoice and strong battery health.", "Устройство в коробке, с чеком и хорошим аккумулятором.", "جهاز مع العلبة والفاتورة وبطارية بحالة ممتازة."),
            "computer" => T("İş ve oyun için yüksek performans.", "High performance for work and gaming.", "Высокая производительность для работы и игр.", "أداء قوي للعمل والألعاب."),
            "watch" => T("Orijinal ve sertifikalı saat koleksiyonu.", "Original and certified watch selection.", "Оригинальные и сертифицированные часы.", "مجموعة ساعات أصلية ومعتمدة."),
            "jewelry" => T("Sertifikalı taş işçiliğiyle premium takı.", "Premium jewelry with certified stones.", "Премиальные украшения с сертифицированными камнями.", "مجوهرات فاخرة بأحجار معتمدة."),
            "electronics" => T("Testleri yapılmış, yüksek performanslı elektronik.", "Tested high-performance electronics.", "Проверенная электроника с высокой производительностью.", "إلكترونيات مجربة وعالية الأداء."),
            "equipment" => T("Sahaya hazır, bakımlı iş ekipmanı.", "Field-ready heavy equipment in maintained condition.", "Обслуженная техника, готовая к работе на площадке.", "معدات عمل جاهزة ومصانة جيدًا."),
            "home" => T("Modern ve dayanıklı ev yaşam ürünleri.", "Modern and durable home living products.", "Современные и долговечные товары для дома.", "منتجات منزلية حديثة ومتينة."),
            "fashion" => T("Yeni sezon ve orijinal moda ürünleri.", "Original fashion products from the new season.", "Оригинальные товары нового модного сезона.", "منتجات أزياء أصلية من الموسم الجديد."),
            "services" => T("Kurumsal standartta profesyonel hizmet.", "Professional service with enterprise standards.", "Профессиональная услуга корпоративного уровня.", "خدمة احترافية بمعايير مؤسسية."),
            _ => T("Güncel ilan.", "Current listing.", "Актуальное объявление.", "إعلان حديث.")
        };
    }

    private List<PropertyCard> BuildListings()
    {
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
                var titleVariants = GetTitleVariants(category);
                var variant = titleVariants[(i - 1) % titleVariants.Length];
                var summaryLine = GetSummaryLine(category);
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
                    Summary = BuildSummary(variant, summaryLine, city, neighborhood, primarySpec, secondarySpec),
                    Category = category,
                    City = city,
                    Neighborhood = neighborhood,
                    Location = location,
                    PriceAmount = priceAmount,
                    PriceLabel = type == "rent"
                        ? T($"GBP {priceAmount.ToString("N0", _displayCulture)} / ay",
                            $"GBP {priceAmount.ToString("N0", _displayCulture)} / month",
                            $"GBP {priceAmount.ToString("N0", _displayCulture)} / месяц",
                            $"GBP {priceAmount.ToString("N0", _displayCulture)} / شهر")
                        : $"GBP {priceAmount.ToString("N0", _displayCulture)}",
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
                    DetailBody = BuildDetailBody(variant, summaryLine, city, neighborhood, type, i)
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

    private string BuildPrimarySpec(CategorySeed seed, int index)
    {
        return seed.PrimaryUnit switch
        {
            "m²" => $"{90 + ((index - 1) % 11) * 12} m²",
            "km" => $"{22000 + ((index - 1) * 3800)} km",
            "ft" => $"{36 + ((index - 1) % 8) * 2} ft",
            "kapasite" => T(
                $"{2 + ((index - 1) % 5)} kişilik",
                $"{2 + ((index - 1) % 5)} berths",
                $"{2 + ((index - 1) % 5)} мест",
                $"تتسع لـ {2 + ((index - 1) % 5)} أشخاص"),
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
            "ayar" => (index % 2) == 0
                ? T("14 ayar", "14K", "14 карат", "14 قيراط")
                : T("18 ayar", "18K", "18 карат", "18 قيراط"),
            "model" => $"Model {2020 + ((index - 1) % 6)}",
            "malzeme" => (index % 2) == 0
                ? T("Masif Ahşap", "Solid Wood", "Массив дерева", "خشب صلب")
                : T("Metal + Kumaş", "Metal + Fabric", "Металл + ткань", "معدن + قماش"),
            "beden" => (index % 4) switch
            {
                0 => "S-M",
                1 => "M-L",
                2 => "L-XL",
                _ => T("Standart", "Standard", "Стандарт", "قياسي")
            },
            "paket" => (index % 3) switch
            {
                0 => T("Kurumsal Paket", "Enterprise Package", "Корпоративный пакет", "باقة مؤسسية"),
                1 => T("Standart Paket", "Standard Package", "Стандартный пакет", "باقة قياسية"),
                _ => T("Premium Paket", "Premium Package", "Премиум-пакет", "باقة مميزة")
            },
            _ => T($"Seri {(index - 1) % 7 + 1}", $"Series {(index - 1) % 7 + 1}", $"Серия {(index - 1) % 7 + 1}", $"الفئة {(index - 1) % 7 + 1}")
        };
    }

    private string BuildSecondarySpec(CategorySeed seed, int index)
    {
        return seed.SecondaryUnit switch
        {
            "bina yaşı" => T($"{(index - 1) % 16 + 1} yaş", $"{(index - 1) % 16 + 1} years old", $"{(index - 1) % 16 + 1} лет", $"{(index - 1) % 16 + 1} سنة"),
            "imar durumu" => (index % 2) == 0
                ? T("Konut İmarlı", "Residential Zoning", "Жилая зона", "تصنيف سكني")
                : T("Ticari İmarlı", "Commercial Zoning", "Коммерческая зона", "تصنيف تجاري"),
            "vites" => (index % 2) == 0 ? T("Otomatik", "Automatic", "Автомат", "أوتوماتيك") : T("Manuel", "Manual", "Механика", "يدوي"),
            "kapasite" => T($"{6 + ((index - 1) % 6)} kişi", $"{6 + ((index - 1) % 6)} people", $"{6 + ((index - 1) % 6)} человек", $"{6 + ((index - 1) % 6)} أشخاص"),
            "yakıt" => (index % 2) == 0 ? T("Dizel", "Diesel", "Дизель", "ديزل") : T("Benzin", "Petrol", "Бензин", "بنزين"),
            "garanti" => (index % 2) == 0 ? T("6 ay garanti", "6-month warranty", "Гарантия 6 месяцев", "ضمان 6 أشهر") : T("12 ay garanti", "12-month warranty", "Гарантия 12 месяцев", "ضمان 12 شهرًا"),
            "bağlantı" => (index % 2) == 0 ? "5G" : "4.5G",
            "disk" => (index % 2) == 0 ? "1 TB SSD" : "512 GB SSD",
            "mekanizma" => (index % 2) == 0 ? T("Mekanik", "Mechanical", "Механика", "ميكانيكي") : "Quartz",
            "sertifika" => T("Sertifikalı", "Certified", "Сертифицировано", "معتمد"),
            "durum" => (index % 2) == 0 ? T("Sıfır Ayarında", "Like New", "Как новый", "كالجديد") : T("Az Kullanılmış", "Lightly Used", "Немного использован", "مستعمل قليلًا"),
            "renk" => (index % 3) switch
            {
                0 => T("Antrasit", "Anthracite", "Антрацит", "أنثراسيت"),
                1 => T("Bej", "Beige", "Бежевый", "بيج"),
                _ => T("Koyu Mavi", "Dark Blue", "Темно-синий", "أزرق داكن")
            },
            "koleksiyon" => T($"Sezon {(index - 1) % 4 + 1}", $"Season {(index - 1) % 4 + 1}", $"Сезон {(index - 1) % 4 + 1}", $"الموسم {(index - 1) % 4 + 1}"),
            "teslim" => (index % 2) == 0 ? T("Aynı gün", "Same day", "В тот же день", "في نفس اليوم") : T("24 saat içinde", "Within 24 hours", "В течение 24 часов", "خلال 24 ساعة"),
            _ => T("Standart", "Standard", "Стандарт", "قياسي")
        };
    }

    private string LocalizeAreaLabel(string label)
    {
        return label switch
        {
            "Merkez" => T("Merkez", "City Center", "Центр", "المركز"),
            "Sanayi Bölgesi" => T("Sanayi Bölgesi", "Industrial Zone", "Промзона", "المنطقة الصناعية"),
            "Showroom Hattı" => T("Showroom Hattı", "Showroom Strip", "Линия автосалонов", "منطقة المعارض"),
            "Marina Yolu" => T("Marina Yolu", "Marina Road", "Дорога к марине", "طريق المرسى"),
            "Çevre Yolu" => T("Çevre Yolu", "Ring Road", "Окружная дорога", "الطريق الدائري"),
            "Liman Bölgesi" => T("Liman Bölgesi", "Harbor District", "Портовый район", "منطقة الميناء"),
            "İskele Sahil" => T("İskele Sahil", "İskele Coast", "Побережье Искеле", "ساحل إسكله"),
            "İskele Sahili" => T("İskele Sahili", "İskele Seafront", "Набережная Искеле", "واجهة إسكله البحرية"),
            "Karpaz Kamp" => T("Karpaz Kamp", "Karpaz Camp", "Кемпинг Карпаз", "مخيم كارباز"),
            "Tatlısu Sahili" => T("Tatlısu Sahili", "Tatlısu Coast", "Побережье Татлысу", "ساحل تاتليسو"),
            "Lapta Kamp Alanı" => T("Lapta Kamp Alanı", "Lapta Camp Area", "Кемпинг Лапта", "منطقة تخييم لابتا"),
            "İskele Kıyı" => T("İskele Kıyı", "İskele Shore", "Берег Искеле", "شاطئ إسكله"),
            "Alsancak Doğa Hattı" => T("Alsancak Doğa Hattı", "Alsancak Nature Line", "Природная зона Алсанджак", "منطقة الطبيعة في ألسنجاك"),
            "Bölgesel Servis" => T("Bölgesel Servis", "Regional Service", "Региональный сервис", "خدمة إقليمية"),
            "Ofis Bölgesi" => T("Ofis Bölgesi", "Office District", "Деловой район", "منطقة المكاتب"),
            "Sahil Hattı" => T("Sahil Hattı", "Coastal Line", "Прибрежная линия", "المنطقة الساحلية"),
            "Geniş Hizmet Alanı" => T("Geniş Hizmet Alanı", "Wide Service Area", "Широкая зона обслуживания", "نطاق خدمة واسع"),
            "Sahil" => T("Sahil", "Coast", "Побережье", "الساحل"),
            "Çarşı" => T("Çarşı", "Market District", "Торговый район", "منطقة السوق"),
            "Yenişehir" => T("Yenişehir", "New Town", "Новый город", "المدينة الجديدة"),
            "Butik Bölge" => T("Butik Bölge", "Boutique District", "Бутик-район", "منطقة بوتيك"),
            "Prestij Hattı" => T("Prestij Hattı", "Prestige Line", "Престижная линия", "منطقة راقية"),
            _ => label
        };
    }

    private string BuildNeighborhood(string category, int index)
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

        return LocalizeAreaLabel(pool[(index - 1) % pool.Length]);
    }

    private string BuildArea(string category, int index)
    {
        return category switch
        {
            "realestate" => $"{95 + ((index - 1) % 10) * 14} m2",
            "land" => $"{620 + ((index - 1) % 10) * 110} m2",
            "home" => T($"{40 + ((index - 1) % 5) * 10} m2 kullanım alanı", $"{40 + ((index - 1) % 5) * 10} m2 usable area", $"{40 + ((index - 1) % 5) * 10} м2 полезной площади", $"{40 + ((index - 1) % 5) * 10} م2 مساحة استخدام"),
            "services" => T($"{2 + ((index - 1) % 4)} saatlik hizmet", $"{2 + ((index - 1) % 4)}-hour service", $"{2 + ((index - 1) % 4)}-часовая услуга", $"خدمة لمدة {2 + ((index - 1) % 4)} ساعات"),
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

    private string BuildSummary(string variant, string summaryLine, string city, string neighborhood, string primarySpec, string secondarySpec)
    {
        return T(
            $"{variant} - {summaryLine} {city} / {neighborhood} bölgesinde {primarySpec} ve {secondarySpec} detaylarıyla öne çıkar.",
            $"{variant} - {summaryLine} Stands out in {city} / {neighborhood} with {primarySpec} and {secondarySpec}.",
            $"{variant} — {summaryLine} Выделяется в районе {city} / {neighborhood} благодаря {primarySpec} и {secondarySpec}.",
            $"{variant} - {summaryLine} يبرز في منطقة {city} / {neighborhood} بفضل {primarySpec} و{secondarySpec}.");
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

    private string Money(decimal amount, string currency)
    {
        return $"{amount.ToString("N0", _displayCulture)} {currency}";
    }

    private List<ListingFact> BuildFacts(string category, string type, string city, string neighborhood, string primarySpec, string secondarySpec, string area, string rooms, int index)
    {
        return category switch
        {
            "realestate" => new()
            {
                new() { Label = T("İlan tipi", "Listing type", "Тип объявления", "نوع الإعلان"), Value = type == "rent" ? T("Kiralık", "For Rent", "Аренда", "للإيجار") : T("Satılık", "For Sale", "Продажа", "للبيع") },
                new() { Label = T("Oda planı", "Room plan", "Планировка", "عدد الغرف"), Value = rooms },
                new() { Label = T("Net alan", "Net area", "Полезная площадь", "المساحة الصافية"), Value = area },
                new() { Label = T("Bina yaşı", "Building age", "Возраст здания", "عمر المبنى"), Value = secondarySpec },
                new() { Label = T("Bölge", "Area", "Район", "المنطقة"), Value = $"{city} / {neighborhood}" },
                new() { Label = T("Aidat", "Maintenance fee", "Ежемесячный взнос", "رسوم الصيانة"), Value = Money(1200 + (index % 5) * 350, "TL") }
            },
            "land" => new()
            {
                new() { Label = T("Parsel alanı", "Plot size", "Площадь участка", "مساحة الأرض"), Value = area },
                new() { Label = T("İmar durumu", "Zoning", "Зонирование", "حالة التنظيم"), Value = secondarySpec },
                new() { Label = T("Tapu", "Title deed", "Титул", "سند الملكية"), Value = T("Müstakil tapu", "Independent title deed", "Индивидуальный титул", "سند مستقل") },
                new() { Label = T("Cephe", "Frontage", "Фасад", "الواجهة"), Value = T($"{18 + (index % 7) * 4} metre", $"{18 + (index % 7) * 4} meters", $"{18 + (index % 7) * 4} м", $"{18 + (index % 7) * 4} متر") },
                new() { Label = T("Bölge", "Area", "Район", "المنطقة"), Value = $"{city} / {neighborhood}" },
                new() { Label = T("Altyapı", "Infrastructure", "Инфраструктура", "البنية التحتية"), Value = T("Yol ve elektrik hazır", "Road and electricity ready", "Дорога и электричество готовы", "الطريق والكهرباء جاهزان") }
            },
            "vehicle" => new()
            {
                new() { Label = T("Kilometre", "Mileage", "Пробег", "عدد الكيلومترات"), Value = primarySpec },
                new() { Label = T("Şanzıman", "Transmission", "Коробка передач", "ناقل الحركة"), Value = secondarySpec },
                new() { Label = T("Yakıt", "Fuel", "Топливо", "الوقود"), Value = (index % 2) == 0 ? T("Dizel", "Diesel", "Дизель", "ديزل") : T("Benzin", "Petrol", "Бензин", "بنزين") },
                new() { Label = T("Kasa", "Body type", "Тип кузова", "نوع الهيكل"), Value = (index % 2) == 0 ? "SUV" : T("Sedan", "Sedan", "Седан", "سيدان") },
                new() { Label = T("Tramer", "History", "История повреждений", "سجل الحوادث"), Value = (index % 3) == 0 ? T("Değişensiz", "No replaced parts", "Без замененных деталей", "بدون أجزاء مستبدلة") : T("Parça parça lokal", "Minor local paintwork", "Локальные косметические работы", "طلاء محلي بسيط") },
                new() { Label = T("Konum", "Location", "Локация", "الموقع"), Value = city }
            },
            "yacht" => new()
            {
                new() { Label = T("Boy", "Length", "Длина", "الطول"), Value = primarySpec },
                new() { Label = T("Kapasite", "Capacity", "Вместимость", "السعة"), Value = secondarySpec },
                new() { Label = T("Motor saati", "Engine hours", "Моточасы", "ساعات المحرك"), Value = T($"{580 + (index % 6) * 70} saat", $"{580 + (index % 6) * 70} hours", $"{580 + (index % 6) * 70} часов", $"{580 + (index % 6) * 70} ساعة") },
                new() { Label = T("Bayrak", "Flag", "Флаг", "العلم"), Value = "KKTC / TR" },
                new() { Label = T("Teslim", "Delivery", "Передача", "التسليم"), Value = T("Marina teslim", "Delivered at marina", "Передача в марине", "التسليم في المرسى") },
                new() { Label = T("Liman", "Harbor", "Марина", "المرسى"), Value = $"{city} / {neighborhood}" }
            },
            "caravan" => new()
            {
                new() { Label = T("Kapasite", "Capacity", "Вместимость", "السعة"), Value = primarySpec },
                new() { Label = T("Yakıt", "Fuel", "Топливо", "الوقود"), Value = secondarySpec },
                new() { Label = T("Yatak", "Beds", "Спальные места", "الأسرة"), Value = T($"{2 + (index % 3)} adet", $"{2 + (index % 3)} beds", $"{2 + (index % 3)} мест", $"{2 + (index % 3)} أسرّة") },
                new() { Label = T("Isıtma", "Heating", "Отопление", "التدفئة"), Value = "Webasto" },
                new() { Label = T("Elektrik", "Power", "Электрика", "الكهرباء"), Value = (index % 2) == 0 ? T("Solar panel destekli", "Solar-assisted", "С поддержкой солнечных панелей", "مدعوم بألواح شمسية") : T("Harici bağlantıya hazır", "Ready for external hookup", "Готов к внешнему подключению", "جاهز للتوصيل الخارجي") },
                new() { Label = T("Konum", "Location", "Локация", "الموقع"), Value = city }
            },
            "services" => new()
            {
                new() { Label = T("Paket", "Package", "Пакет", "الباقة"), Value = primarySpec },
                new() { Label = T("Teslim", "Availability", "Доступность", "التوفر"), Value = secondarySpec },
                new() { Label = T("Servis alanı", "Service area", "Зона обслуживания", "نطاق الخدمة"), Value = T($"{city} ve çevresi", $"{city} and nearby areas", $"{city} и окрестности", $"{city} والمناطق القريبة") },
                new() { Label = T("Yanıt süresi", "Response time", "Время ответа", "زمن الاستجابة"), Value = T($"{30 + (index % 4) * 15} dk", $"{30 + (index % 4) * 15} min", $"{30 + (index % 4) * 15} мин", $"{30 + (index % 4) * 15} دقيقة") },
                new() { Label = T("Ekip", "Team", "Команда", "الفريق"), Value = T($"{2 + (index % 5)} kişilik ekip", $"{2 + (index % 5)}-person team", $"Команда из {2 + (index % 5)} человек", $"فريق من {2 + (index % 5)} أشخاص") },
                new() { Label = T("Randevu", "Appointment", "Запись", "الموعد"), Value = T("Ön rezervasyon ile", "By prior booking", "По предварительной записи", "بموعد مسبق") }
            },
            _ => new()
            {
                new() { Label = T("Öne çıkan", "Featured", "Главное", "الأبرز"), Value = primarySpec },
                new() { Label = T("Durum", "Condition", "Состояние", "الحالة"), Value = secondarySpec },
                new() { Label = T("Lokasyon", "Location", "Локация", "الموقع"), Value = $"{city} / {neighborhood}" },
                new() { Label = T("Teslim", "Delivery", "Передача", "التسليم"), Value = (index % 2) == 0 ? T("Aynı gün", "Same day", "В тот же день", "في نفس اليوم") : T("Kargo / elden teslim", "Shipping / hand delivery", "Доставка / самовывоз", "شحن / تسليم يدوي") },
                new() { Label = T("Stok", "Stock", "Наличие", "المخزون"), Value = T($"{5 + (index % 8)} adet / parça", $"{5 + (index % 8)} units / pieces", $"{5 + (index % 8)} шт.", $"{5 + (index % 8)} قطع") },
                new() { Label = T("Ek bilgi", "Extra info", "Доп. информация", "معلومات إضافية"), Value = T("Kontrolleri tamamlandı", "Checks completed", "Проверка завершена", "تمت المراجعة") }
            }
        };
    }

    private List<string> BuildHighlights(string category, string city, string neighborhood, string primarySpec, string secondarySpec, int index)
    {
        return category switch
        {
            "realestate" => new()
            {
                T($"{neighborhood} lokasyonunda yüksek talep gören bölgede yer alıyor", $"Located in the sought-after {neighborhood} area", $"Расположено в востребованном районе {neighborhood}", $"يقع في منطقة {neighborhood} المطلوبة"),
                T($"{primarySpec} kullanım alanı ile günlük yaşam konforunu artırıyor", $"Offers comfortable daily use with {primarySpec}", $"Обеспечивает комфорт благодаря {primarySpec}", $"يوفر راحة يومية بفضل {primarySpec}"),
                T($"{secondarySpec} yapısal profil için yenilenmiş detaylar sunuyor", $"Presents updated details with {secondarySpec}", $"Предлагает обновленные детали и {secondarySpec}", $"يقدم تفاصيل محدثة مع {secondarySpec}"),
                T("Taşınmaya veya yatırım amaçlı değerlendirmeye uygun teslim planı mevcut", "Suitable for move-in or investment planning", "Подходит для проживания или инвестиции", "مناسب للسكن أو كفرصة استثمارية")
            },
            "land" => new()
            {
                T($"{primarySpec} büyüklüğünde gelişime açık parsel", $"Development-ready plot of {primarySpec}", $"Участок {primarySpec}, готовый к развитию", $"قطعة أرض بمساحة {primarySpec} قابلة للتطوير"),
                T($"{secondarySpec} yapısı ile yatırımcı profiline hitap ediyor", $"Appeals to investors with its {secondarySpec} profile", $"Подходит инвесторам благодаря статусу {secondarySpec}", $"يجذب المستثمرين بفضل حالة {secondarySpec}"),
                T($"{city} bağlantı yollarına yakın konum avantajı", $"Advantageous location close to {city} access routes", $"Удобное расположение рядом с дорогами {city}", $"موقع مميز قريب من طرق الوصول إلى {city}"),
                T("Bölgedeki proje hareketliliği nedeniyle değer potansiyeli güçlü", "Strong value potential thanks to nearby developments", "Высокий потенциал роста стоимости благодаря проектам рядом", "يتمتع بإمكانات قيمة قوية بسبب التطويرات المحيطة")
            },
            "vehicle" => new()
            {
                T($"{primarySpec} ile dengeli kullanım geçmişi", $"Balanced usage history with {primarySpec}", $"Сбалансированная история эксплуатации и {primarySpec}", $"سجل استخدام متوازن مع {primarySpec}"),
                T($"{secondarySpec} sürüş karakteri ve segment uyumu", $"{secondarySpec} driving character and segment fit", $"{secondarySpec} и удачный характер вождения", $"{secondarySpec} وطابع قيادة مناسب"),
                T("Bakım geçmişi ve ekspertiz notları paylaşılmaya hazır", "Maintenance history and inspection notes are ready to share", "История обслуживания и заметки экспертизы готовы", "سجل الصيانة وملاحظات الفحص جاهزة للمشاركة"),
                T($"Paket seviyesi {index % 5 + 1} ile günlük kullanıma ve uzun yola uygun", $"Trim level {index % 5 + 1} fits daily use and long trips", $"Комплектация {index % 5 + 1} подходит для города и дальних поездок", $"الفئة {index % 5 + 1} مناسبة للاستخدام اليومي والرحلات الطويلة")
            },
            "services" => new()
            {
                T($"{city} / {neighborhood} hattında aktif hizmet veriyor", $"Actively serving {city} / {neighborhood}", $"Активно обслуживает район {city} / {neighborhood}", $"يخدم منطقة {city} / {neighborhood} بشكل نشط"),
                T($"{primarySpec} ile farklı bütçelere uygun teklif yapısı sunuyor", $"Offers flexible pricing through {primarySpec}", $"Предлагает гибкий формат услуги через {primarySpec}", $"يقدم خيارات مناسبة لميزانيات مختلفة عبر {primarySpec}"),
                T($"{secondarySpec} teslim yaklaşımı planlamayı kolaylaştırıyor", $"{secondarySpec} availability makes planning easier", $"{secondarySpec} облегчает планирование", $"{secondarySpec} يجعل التخطيط أسهل"),
                T("Kurumsal veya bireysel talepler için ölçeklenebilir çözüm sağlıyor", "Scalable for both corporate and individual requests", "Подходит как для компаний, так и для частных клиентов", "حل قابل للتوسع للعملاء الأفراد والشركات")
            },
            _ => new()
            {
                T($"{primarySpec} ile öne çıkan güncel ilan profili", $"Current listing profile highlighted by {primarySpec}", $"Актуальное объявление с акцентом на {primarySpec}", $"إعلان حديث يتميز بـ {primarySpec}"),
                T($"{secondarySpec} bilgisi açık ve anlaşılır biçimde sunuldu", $"{secondarySpec} is presented clearly", $"{secondarySpec} указано понятно и прозрачно", $"تم عرض {secondarySpec} بشكل واضح"),
                T($"{city} lokasyonunda hızlı teslim / buluşma avantajı sağlıyor", $"Offers quick delivery or meetup in {city}", $"Доступна быстрая доставка или встреча в {city}", $"يوفر تسليمًا سريعًا أو لقاءً في {city}"),
                T("Görsel ve metin kurgusu gerçek ilana yakın deneyim için zenginleştirildi", "Enhanced visuals and copy create a realistic listing feel", "Визуал и текст приближены к реальному объявлению", "تم تحسين الصور والنصوص لتجربة أقرب للإعلان الحقيقي")
            }
        };
    }

    private List<string> BuildFeatureBadges(string category, string type, int index)
    {
        var badges = new List<string>
        {
            type == "rent"
                ? T("Hızlı kiralama", "Quick rental", "Быстрая аренда", "إيجار سريع")
                : T("Hazır teslim", "Ready to deliver", "Готово к передаче", "جاهز للتسليم"),
            (index % 2) == 0
                ? T("Doğrulanmış bilgi", "Verified info", "Проверенная информация", "معلومات موثقة")
                : T("Güncel ilan", "Fresh listing", "Актуальное объявление", "إعلان حديث")
        };

        badges.Add(category switch
        {
            "realestate" => T("Yatırım fırsatı", "Investment opportunity", "Инвестиционная возможность", "فرصة استثمارية"),
            "land" => T("Gelişim bölgesi", "Growth area", "Зона развития", "منطقة تطوير"),
            "vehicle" => T("Ekspertiz notlu", "Inspection-backed", "С отчетом экспертизы", "مع تقرير فحص"),
            "yacht" => T("Marina hazır", "Marina ready", "Готово в марине", "جاهز في المرسى"),
            "caravan" => T("Kamp uyumlu", "Camping ready", "Подходит для кемпинга", "مناسب للتخييم"),
            "phone" => T("Kutulu cihaz", "Boxed device", "Устройство в коробке", "جهاز مع العلبة"),
            "computer" => T("Performans seçimi", "Performance pick", "Выбор по производительности", "خيار أداء"),
            "watch" => T("Koleksiyon parçası", "Collector piece", "Коллекционный экземпляр", "قطعة لهواة الجمع"),
            "jewelry" => T("Sertifikalı", "Certified", "Сертифицировано", "معتمد"),
            "electronics" => T("Test edilmiş", "Tested", "Проверено", "تم اختباره"),
            "equipment" => T("Sahaya hazır", "Site ready", "Готово к работе", "جاهز للعمل"),
            "home" => T("Dekor seçkisi", "Decor pick", "Выбор для интерьера", "اختيار ديكور"),
            "fashion" => T("Yeni sezon", "New season", "Новый сезон", "الموسم الجديد"),
            "services" => T("Randevu alınabilir", "Bookable", "Можно записаться", "يمكن الحجز"),
            _ => T("Öne çıkan", "Featured", "Рекомендуемое", "مميز")
        });

        return badges;
    }

    private static string BuildSellerName(string category, int index)
    {
        var pool = category switch
        {
            "realestate" => new[] { "Blue Coast Estates", "Northline Property", "Harbor Living", "Island Portfolio" },
            "land" => new[] { "Terra Invest", "North Plot Office", "Nova Parcel", "Horizon Land" },
            "vehicle" => new[] { "AutoPrime", "North Garage", "Marina Motors", "Cityline Auto" },
            "yacht" => new[] { "Marina Select", "Blue Sail", "Harbor Yacht", "Coastline Marine" },
            "caravan" => new[] { "Roadcamp", "Nomad Garage", "Vanlife Hub", "Campline" },
            "services" => new[] { "Island Service Group", "Prime Support", "North Works", "Field Team" },
            _ => new[] { "Premium Store", "Island Select", "Northline Shop", "Urban Select" }
        };

        return pool[(index - 1) % pool.Length];
    }

    private string BuildSellerRole(string category, string type)
    {
        return category switch
        {
            "realestate" or "land" => type == "rent"
                ? T("Portföy Danışmanı", "Portfolio Advisor", "Консультант по портфелю", "مستشار محافظ")
                : T("Satış Danışmanı", "Sales Advisor", "Консультант по продажам", "مستشار مبيعات"),
            "vehicle" => T("Yetkili Satıcı", "Authorized Seller", "Официальный продавец", "بائع معتمد"),
            "services" => T("Hizmet Sağlayıcı", "Service Provider", "Поставщик услуг", "مزود خدمة"),
            "yacht" => T("Marina Temsilcisi", "Marina Representative", "Представитель марины", "ممثل المرسى"),
            _ => T("Kurumsal Satıcı", "Corporate Seller", "Корпоративный продавец", "بائع مؤسسي")
        };
    }

    private static string BuildSellerPhone(int listingId)
    {
        return $"+90 548 {200 + (listingId % 700)} {10 + (listingId % 80):00} {20 + (listingId % 70):00}";
    }

    private string BuildPostedAtLabel(int index)
    {
        return (index % 5) switch
        {
            0 => T("Bugün güncellendi", "Updated today", "Обновлено сегодня", "تم التحديث اليوم"),
            1 => T("Dün eklendi", "Added yesterday", "Добавлено вчера", "أضيف أمس"),
            2 => T("2 gün önce güncellendi", "Updated 2 days ago", "Обновлено 2 дня назад", "تم التحديث قبل يومين"),
            3 => T("Bu hafta eklendi", "Added this week", "Добавлено на этой неделе", "أضيف هذا الأسبوع"),
            _ => T("Son 7 gün içinde yayında", "Published within the last 7 days", "Опубликовано за последние 7 дней", "نُشر خلال آخر 7 أيام")
        };
    }

    private static string BuildListingCode(string category, int listingId)
    {
        var prefix = category[..Math.Min(3, category.Length)].ToUpperInvariant();
        return $"{prefix}-{listingId:00000}";
    }

    private string BuildAvailabilityNote(string category, string type, int index)
    {
        return category switch
        {
            "services" => (index % 2) == 0
                ? T("Bu hafta içinde randevu alınabilir.", "Appointments available this week.", "На этой неделе можно записаться.", "المواعيد متاحة هذا الأسبوع.")
                : T("Yoğun dönem için ön rezervasyon önerilir.", "Advance booking is recommended for busy periods.", "Для загруженных периодов рекомендуется бронь заранее.", "يُنصح بالحجز المسبق في الفترات المزدحمة."),
            "vehicle" => (index % 2) == 0
                ? T("Test sürüşü planlanabilir.", "A test drive can be arranged.", "Можно организовать тест-драйв.", "يمكن ترتيب تجربة قيادة.")
                : T("Ekspertiz için önceden haber verilmesi yeterli.", "Advance notice is enough for inspection.", "Для проверки достаточно предупредить заранее.", "يكفي الإبلاغ مسبقًا للفحص."),
            "realestate" => type == "rent"
                ? T("Taşınmaya uygun teslim planı hazır.", "Move-in ready delivery plan available.", "Объект готов к заселению.", "خطة تسليم جاهزة للسكن.")
                : T("Tapu ve ekspertiz süreci için uygun.", "Suitable for title transfer and valuation process.", "Подходит для оформления и оценки.", "مناسب لإجراءات التقييم ونقل الملكية."),
            _ => (index % 2) == 0
                ? T("Stok / teslim durumu günceldir.", "Stock and delivery status is current.", "Статус наличия и передачи актуален.", "حالة التوفر والتسليم محدثة.")
                : T("Detaylı bilgi için satıcı ile iletişime geçin.", "Contact the seller for detailed information.", "Свяжитесь с продавцом для подробностей.", "تواصل مع البائع للحصول على التفاصيل.")
        };
    }

    private string BuildDetailBody(string variant, string summaryLine, string city, string neighborhood, string type, int index)
    {
        var typeText = type == "rent"
            ? T("kiralama", "rental", "аренды", "الإيجار")
            : T("satın alma", "purchase", "покупки", "الشراء");
        var tone = (index % 2) == 0
            ? T(
                "Son kullanıcı deneyimi düşünülerek hazırlanmış, güçlü ilk izlenim veren bir kurguyla sunuluyor.",
                "Presented with a polished structure designed to create a strong first impression.",
                "Подается в выверенном формате, создающем сильное первое впечатление.",
                "يُعرض بصياغة متقنة تمنح انطباعًا أوليًا قويًا.")
            : T(
                "İhtiyaca göre hızlı karar vermeyi kolaylaştıran net bir içerik yapısıyla destekleniyor.",
                "Supported by a clear content structure that helps users decide quickly.",
                "Поддержано понятной структурой, помогающей быстро принять решение.",
                "مدعوم ببنية واضحة تساعد على اتخاذ القرار بسرعة.");

        return T(
            $"{variant}, {city} / {neighborhood} bölgesinde {typeText} odaklı arama yapan kullanıcılar için öne çıkarılmış demo ilandır. {summaryLine} {tone} Görseller, satıcı bilgileri ve temel nitelikler bir arada sunularak detay sayfasının gerçek bir ilan deneyimine daha yakın his vermesi hedeflenmiştir.",
            $"{variant} is a featured demo listing for users searching for a {typeText} option in {city} / {neighborhood}. {summaryLine} {tone} Images, seller information and core attributes are presented together to make the detail page feel closer to a real marketplace listing.",
            $"{variant} — это демонстрационное объявление для пользователей, ищущих вариант {typeText} в районе {city} / {neighborhood}. {summaryLine} {tone} Фотографии, данные продавца и ключевые характеристики собраны вместе, чтобы страница максимально напоминала настоящее объявление.",
            $"{variant} هو إعلان تجريبي مميز للمستخدمين الباحثين عن خيار {typeText} في منطقة {city} / {neighborhood}. {summaryLine} {tone} تم جمع الصور ومعلومات البائع والخصائص الأساسية في صفحة واحدة لتبدو أقرب إلى تجربة إعلان حقيقي.");
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
