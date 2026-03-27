using System.Globalization;

namespace SEN_T_PAZAR.Services;

public sealed class SiteLocalizer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly HashSet<string> SupportedLanguages = ["tr", "en", "ru", "ar"];

    private static readonly Dictionary<string, Dictionary<string, string>> Dictionary = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tr"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["brand"] = "SEN-T PAZAR",
            ["home"] = "Ana Sayfa",
            ["forSale"] = "Satılık",
            ["forRent"] = "Kiralık",
            ["projects"] = "Koleksiyonlar",
            ["postAd"] = "Ücretsiz İlan Ver",
            ["favorites"] = "Favoriler",
            ["messages"] = "Mesajlar",
            ["membership"] = "Üyeliğiniz",
            ["heroEyebrow"] = "",
            ["heroTitle"] = "Aradığın Her Şey Burada",
            ["heroSubtitle"] = "Emlaktan araca, elektronikten iş makinelerine kadar binlerce ilan",
            ["listingType"] = "İlan Tipi",
            ["city"] = "Şehir",
            ["priceRange"] = "Bütçe",
            ["keyword"] = "Anahtar Kelime",
            ["category"] = "Kategori",
            ["allTypes"] = "Tümü",
            ["allCities"] = "Tüm Şehirler",
            ["allCategories"] = "Tüm Kategoriler",
            ["cat_realestate"] = "Emlak",
            ["cat_land"] = "Arsa",
            ["cat_vehicle"] = "Vasıta",
            ["cat_yacht"] = "Yat/Tekne",
            ["cat_caravan"] = "Karavan",
            ["cat_secondhand"] = "2. El Eşya",
            ["cat_phone"] = "Telefon",
            ["cat_computer"] = "Bilgisayar",
            ["cat_watch"] = "Saat",
            ["cat_jewelry"] = "Mücevher",
            ["cat_electronics"] = "Elektronik",
            ["cat_equipment"] = "İş Makineleri",
            ["cat_home"] = "Ev Eşyası",
            ["cat_fashion"] = "Moda",
            ["cat_services"] = "Hizmet",
            ["anyPrice"] = "Fark Etmez",
            ["priceLow"] = "0 - 150,000 GBP",
            ["priceMid"] = "150,000 - 300,000 GBP",
            ["priceHigh"] = "300,000+ GBP",
            ["sortBy"] = "Sıralama",
            ["sort_latest"] = "En yeni",
            ["sort_priceAsc"] = "Fiyat artan",
            ["sort_priceDesc"] = "Fiyat azalan",
            ["sort_name"] = "Başlık A-Z",
            ["search"] = "İlanları Listele",
            ["clearFilters"] = "Filtreleri Temizle",
            ["showcase"] = "Vitrin İlanları",
            ["resultsCount"] = "{0} / {1} ilan görüntüleniyor",
            ["noResultsTitle"] = "Aradığınıza uygun ilan bulunamadı",
            ["noResultsText"] = "Filtreleri genişletip tekrar deneyin veya ilan vererek pazarı büyütün.",
            ["details"] = "Detayları Gör",
            ["regionsTitle"] = "Popüler Bölgeler",
            ["regionsSubtitle"] = "Konuma göre ilan keşfi",
            ["categoriesTitle"] = "Kategoriler",
            ["marketStreamTitle"] = "Öne Çıkan İlan Akışı",
            ["allListings"] = "Tümünü Gör",
            ["projectsTitle"] = "Öne Çıkanlar",
            ["projectsSubtitle"] = "Araçtan elektroniğe, yattan mücevhere trend pazar vitrinleri",
            ["newProjectAlert"] = "Yeni Koleksiyon Alarmı",
            ["newProjectText"] = "İlgi alanınıza göre yeni koleksiyonlar yayınlandığında bildirim alın.",
            ["createRequest"] = "Talep Oluştur",
            ["partnersTitle"] = "Acenteler, mağazalar ve kurumsal satıcılar",
            ["copyright"] = "Tüm hakları saklıdır.",
            ["publishTitle"] = "İlan Ver",
            ["publishDesc"] = "Formu doldurun, ilanınız moderasyon sonrası yayına alınsın.",
            ["submitAd"] = "İlan Gönder",
            ["detailsTitle"] = "İlan Detayı",
            ["backToList"] = "Listeye Dön",
            ["similarPost"] = "Benzer İlan Ver",
            ["price"] = "Fiyat",
            ["spec1"] = "Özellik 1",
            ["spec2"] = "Özellik 2",
            ["sale"] = "Satılık",
            ["rent"] = "Kiralık",
            ["project"] = "Koleksiyon"
        },
        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["brand"] = "SEN-T PAZAR",
            ["home"] = "Home",
            ["forSale"] = "For Sale",
            ["forRent"] = "For Rent",
            ["projects"] = "Collections",
            ["postAd"] = "Post Ad",
            ["favorites"] = "Favorites",
            ["messages"] = "Messages",
            ["membership"] = "Account",
            ["heroEyebrow"] = "Cyprus all-in-one marketplace for sales and rentals",
            ["heroTitle"] = "Find what you need in one place",
            ["heroSubtitle"] = "From real estate to vehicles, electronics and equipment",
            ["listingType"] = "Listing Type",
            ["city"] = "City",
            ["priceRange"] = "Budget",
            ["keyword"] = "Keyword",
            ["category"] = "Category",
            ["allTypes"] = "All",
            ["allCities"] = "All Cities",
            ["allCategories"] = "All Categories",
            ["cat_realestate"] = "Real Estate",
            ["cat_land"] = "Land",
            ["cat_vehicle"] = "Vehicles",
            ["cat_yacht"] = "Yacht/Boat",
            ["cat_caravan"] = "Caravan",
            ["cat_secondhand"] = "Second-hand",
            ["cat_phone"] = "Phone",
            ["cat_computer"] = "Computer",
            ["cat_watch"] = "Watch",
            ["cat_jewelry"] = "Jewelry",
            ["cat_electronics"] = "Electronics",
            ["cat_equipment"] = "Heavy Equipment",
            ["cat_home"] = "Home & Living",
            ["cat_fashion"] = "Fashion",
            ["cat_services"] = "Services",
            ["anyPrice"] = "Any",
            ["priceLow"] = "0 - 150,000 GBP",
            ["priceMid"] = "150,000 - 300,000 GBP",
            ["priceHigh"] = "300,000+ GBP",
            ["sortBy"] = "Sort",
            ["sort_latest"] = "Latest",
            ["sort_priceAsc"] = "Price low to high",
            ["sort_priceDesc"] = "Price high to low",
            ["sort_name"] = "Title A-Z",
            ["search"] = "Show Listings",
            ["clearFilters"] = "Clear Filters",
            ["showcase"] = "Featured Listings",
            ["resultsCount"] = "Showing {0} of {1} listings",
            ["noResultsTitle"] = "No matching listings found",
            ["noResultsText"] = "Broaden your filters or post a new listing.",
            ["details"] = "View Details",
            ["regionsTitle"] = "Popular Regions",
            ["regionsSubtitle"] = "Explore by location",
            ["categoriesTitle"] = "Categories",
            ["marketStreamTitle"] = "Trending Listing Stream",
            ["allListings"] = "View All",
            ["projectsTitle"] = "Featured Collections",
            ["projectsSubtitle"] = "From vehicles to electronics, yachts to jewelry",
            ["newProjectAlert"] = "New Collection Alert",
            ["newProjectText"] = "Get notified when a new collection goes live.",
            ["createRequest"] = "Create Request",
            ["partnersTitle"] = "Agencies, stores and enterprise sellers",
            ["copyright"] = "All rights reserved.",
            ["publishTitle"] = "Post Ad",
            ["publishDesc"] = "Fill in the form and publish after moderation.",
            ["submitAd"] = "Submit Listing",
            ["detailsTitle"] = "Listing Details",
            ["backToList"] = "Back to List",
            ["similarPost"] = "Post Similar",
            ["price"] = "Price",
            ["spec1"] = "Spec 1",
            ["spec2"] = "Spec 2",
            ["sale"] = "For Sale",
            ["rent"] = "For Rent",
            ["project"] = "Collection"
        },
        ["ru"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["brand"] = "SEN-T PAZAR",
            ["home"] = "Главная",
            ["forSale"] = "Продажа",
            ["forRent"] = "Аренда",
            ["projects"] = "Коллекции",
            ["postAd"] = "Подать объявление",
            ["favorites"] = "Избранное",
            ["messages"] = "Сообщения",
            ["membership"] = "Профиль",
            ["heroEyebrow"] = "Универсальная платформа Кипра для продажи и аренды",
            ["heroTitle"] = "Найдите всё в одном месте",
            ["heroSubtitle"] = "Недвижимость, авто, электроника и многое другое",
            ["listingType"] = "Тип объявления",
            ["city"] = "Город",
            ["priceRange"] = "Бюджет",
            ["keyword"] = "Ключевое слово",
            ["category"] = "Категория",
            ["allTypes"] = "Все",
            ["allCities"] = "Все города",
            ["allCategories"] = "Все категории",
            ["cat_realestate"] = "Недвижимость",
            ["cat_land"] = "Земля",
            ["cat_vehicle"] = "Транспорт",
            ["cat_yacht"] = "Яхты/Лодки",
            ["cat_caravan"] = "Караваны",
            ["cat_secondhand"] = "Б/у товары",
            ["cat_phone"] = "Телефоны",
            ["cat_computer"] = "Компьютеры",
            ["cat_watch"] = "Часы",
            ["cat_jewelry"] = "Украшения",
            ["cat_electronics"] = "Электроника",
            ["cat_equipment"] = "Спецтехника",
            ["cat_home"] = "Товары для дома",
            ["cat_fashion"] = "Мода",
            ["cat_services"] = "Услуги",
            ["anyPrice"] = "Любая",
            ["priceLow"] = "0 - 150,000 GBP",
            ["priceMid"] = "150,000 - 300,000 GBP",
            ["priceHigh"] = "300,000+ GBP",
            ["sortBy"] = "Сортировка",
            ["sort_latest"] = "Сначала новые",
            ["sort_priceAsc"] = "Цена по возрастанию",
            ["sort_priceDesc"] = "Цена по убыванию",
            ["sort_name"] = "Название А-Я",
            ["search"] = "Показать объявления",
            ["clearFilters"] = "Сбросить фильтры",
            ["showcase"] = "Рекомендованные объявления",
            ["resultsCount"] = "Показано {0} из {1} объявлений",
            ["noResultsTitle"] = "Подходящих объявлений не найдено",
            ["noResultsText"] = "Расширьте фильтры или добавьте новое объявление.",
            ["details"] = "Подробнее",
            ["regionsTitle"] = "Популярные регионы",
            ["regionsSubtitle"] = "Поиск по локации",
            ["categoriesTitle"] = "Категории",
            ["marketStreamTitle"] = "Лента популярных объявлений",
            ["allListings"] = "Смотреть все",
            ["projectsTitle"] = "Популярные коллекции",
            ["projectsSubtitle"] = "От транспорта до электроники, от яхт до украшений",
            ["newProjectAlert"] = "Оповещение о коллекции",
            ["newProjectText"] = "Получайте уведомления о новых коллекциях.",
            ["createRequest"] = "Создать запрос",
            ["partnersTitle"] = "Агентства, магазины и корпоративные продавцы",
            ["copyright"] = "Все права защищены.",
            ["publishTitle"] = "Подать объявление",
            ["publishDesc"] = "Заполните форму, публикация после модерации.",
            ["submitAd"] = "Отправить объявление",
            ["detailsTitle"] = "Детали объявления",
            ["backToList"] = "Назад к списку",
            ["similarPost"] = "Похожее объявление",
            ["price"] = "Цена",
            ["spec1"] = "Характеристика 1",
            ["spec2"] = "Характеристика 2",
            ["sale"] = "Продажа",
            ["rent"] = "Аренда",
            ["project"] = "Коллекция"
        },
        ["ar"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["brand"] = "SEN-T PAZAR",
            ["home"] = "الرئيسية",
            ["forSale"] = "للبيع",
            ["forRent"] = "للإيجار",
            ["projects"] = "المجموعات",
            ["postAd"] = "أضف إعلاناً",
            ["favorites"] = "المفضلة",
            ["messages"] = "الرسائل",
            ["membership"] = "الحساب",
            ["heroEyebrow"] = "منصة قبرص الشاملة للبيع والإيجار",
            ["heroTitle"] = "اعثر على كل ما تحتاجه في مكان واحد",
            ["heroSubtitle"] = "عقارات، سيارات، إلكترونيات ومعدات",
            ["listingType"] = "نوع الإعلان",
            ["city"] = "المدينة",
            ["priceRange"] = "الميزانية",
            ["keyword"] = "كلمة مفتاحية",
            ["category"] = "الفئة",
            ["allTypes"] = "الكل",
            ["allCities"] = "كل المدن",
            ["allCategories"] = "كل الفئات",
            ["cat_realestate"] = "عقارات",
            ["cat_land"] = "أراضٍ",
            ["cat_vehicle"] = "مركبات",
            ["cat_yacht"] = "يخوت/قوارب",
            ["cat_caravan"] = "كرفانات",
            ["cat_secondhand"] = "مستعمل",
            ["cat_phone"] = "هواتف",
            ["cat_computer"] = "حاسوب",
            ["cat_watch"] = "ساعات",
            ["cat_jewelry"] = "مجوهرات",
            ["cat_electronics"] = "إلكترونيات",
            ["cat_equipment"] = "معدات ثقيلة",
            ["cat_home"] = "مستلزمات المنزل",
            ["cat_fashion"] = "موضة",
            ["cat_services"] = "خدمات",
            ["anyPrice"] = "أي سعر",
            ["priceLow"] = "0 - 150,000 GBP",
            ["priceMid"] = "150,000 - 300,000 GBP",
            ["priceHigh"] = "300,000+ GBP",
            ["sortBy"] = "الترتيب",
            ["sort_latest"] = "الأحدث",
            ["sort_priceAsc"] = "السعر تصاعدياً",
            ["sort_priceDesc"] = "السعر تنازلياً",
            ["sort_name"] = "الاسم أ-ي",
            ["search"] = "عرض الإعلانات",
            ["clearFilters"] = "مسح الفلاتر",
            ["showcase"] = "إعلانات مميزة",
            ["resultsCount"] = "عرض {0} من {1} إعلان",
            ["noResultsTitle"] = "لا توجد نتائج مطابقة",
            ["noResultsText"] = "وسّع الفلاتر أو أضف إعلاناً جديداً.",
            ["details"] = "عرض التفاصيل",
            ["regionsTitle"] = "المناطق الشائعة",
            ["regionsSubtitle"] = "اكتشف حسب الموقع",
            ["categoriesTitle"] = "الفئات",
            ["marketStreamTitle"] = "تدفق الإعلانات الرائجة",
            ["allListings"] = "عرض الكل",
            ["projectsTitle"] = "مجموعات مميزة",
            ["projectsSubtitle"] = "من المركبات إلى الإلكترونيات ومن اليخوت إلى المجوهرات",
            ["newProjectAlert"] = "تنبيه مجموعة جديدة",
            ["newProjectText"] = "احصل على إشعار عند نشر مجموعة جديدة.",
            ["createRequest"] = "إنشاء طلب",
            ["partnersTitle"] = "وكالات ومتاجر وبائعون مؤسسيون",
            ["copyright"] = "جميع الحقوق محفوظة.",
            ["publishTitle"] = "أضف إعلاناً",
            ["publishDesc"] = "املأ النموذج وسيُنشر بعد المراجعة.",
            ["submitAd"] = "إرسال الإعلان",
            ["detailsTitle"] = "تفاصيل الإعلان",
            ["backToList"] = "العودة للقائمة",
            ["similarPost"] = "إضافة مشابه",
            ["price"] = "السعر",
            ["spec1"] = "المواصفة 1",
            ["spec2"] = "المواصفة 2",
            ["sale"] = "للبيع",
            ["rent"] = "للإيجار",
            ["project"] = "مجموعة"
        }
    };

    public SiteLocalizer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CultureCode
    {
        get
        {
            var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            return SupportedLanguages.Contains(code) ? code : "tr";
        }
    }

    public bool IsRtl => CultureCode == "ar";

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        if (Dictionary.TryGetValue(CultureCode, out var selected) && selected.TryGetValue(key, out var value))
        {
            return value;
        }

        if (Dictionary["tr"].TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    public string TypeLabel(string typeCode)
    {
        return typeCode switch
        {
            "sale" => Get("sale"),
            "rent" => Get("rent"),
            "project" => Get("project"),
            _ => Get("allTypes")
        };
    }

    public string SortLabel(string sortCode)
    {
        return sortCode switch
        {
            "latest" => Get("sort_latest"),
            "priceAsc" => Get("sort_priceAsc"),
            "priceDesc" => Get("sort_priceDesc"),
            "name" => Get("sort_name"),
            _ => Get("sort_latest")
        };
    }

    public string CategoryLabel(string categoryCode)
    {
        return categoryCode switch
        {
            "realestate" => Get("cat_realestate"),
            "land" => Get("cat_land"),
            "vehicle" => Get("cat_vehicle"),
            "yacht" => Get("cat_yacht"),
            "caravan" => Get("cat_caravan"),
            "secondhand" => Get("cat_secondhand"),
            "phone" => Get("cat_phone"),
            "computer" => Get("cat_computer"),
            "watch" => Get("cat_watch"),
            "jewelry" => Get("cat_jewelry"),
            "electronics" => Get("cat_electronics"),
            "equipment" => Get("cat_equipment"),
            "home" => Get("cat_home"),
            "fashion" => Get("cat_fashion"),
            "services" => Get("cat_services"),
            _ => Get("allCategories")
        };
    }

    public string CategorySlug(string categoryCode)
    {
        var code = CultureCode;
        return (code, categoryCode) switch
        {
            (_, "realestate") => "emlak",
            (_, "land") => "arsa",
            ("en", "vehicle") => "vehicles",
            (_, "vehicle") => "vasita",
            ("en", "yacht") => "yacht-boat",
            (_, "yacht") => "yat-tekne",
            (_, "caravan") => "karavan",
            ("en", "secondhand") => "second-hand",
            (_, "secondhand") => "ikinci-el",
            (_, "phone") => "telefon",
            (_, "computer") => "bilgisayar",
            (_, "watch") => "saat",
            (_, "jewelry") => "mucevher",
            (_, "electronics") => "elektronik",
            (_, "equipment") => "is-makineleri",
            (_, "home") => "ev-yasam",
            (_, "fashion") => "moda",
            (_, "services") => "hizmet",
            _ => "kategori"
        };
    }

    public string CategorySection()
    {
        return CultureCode switch
        {
            "en" => "category",
            "ru" => "kategoriya",
            "ar" => "tasnif",
            _ => "kategori"
        };
    }

    public string BuildCategoryUrl(string categoryCode)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return "/";
        }

        var query = context.Request.Query
            .ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        query.Remove("category");

        var queryString = string.Join("&", query.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        var path = $"/{CategorySection()}/{CategorySlug(categoryCode)}";
        return string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
    }

    public string BuildCultureUrl(string cultureCode)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return "/";
        }

        var code = SupportedLanguages.Contains(cultureCode) ? cultureCode : "tr";
        var query = context.Request.Query
            .ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        query["culture"] = code;

        var queryString = string.Join("&", query.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        return string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
    }
}
