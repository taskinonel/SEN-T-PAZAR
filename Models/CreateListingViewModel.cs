using System.ComponentModel.DataAnnotations;

namespace SEN_T_PAZAR.Models;

public enum EstateRoomCount
{
    [Display(Name = "1+0")] OneZero,
    [Display(Name = "1+1")] OneOne,
    [Display(Name = "2+0")] TwoZero,
    [Display(Name = "2+1")] TwoOne,
    [Display(Name = "3+0")] ThreeZero,
    [Display(Name = "3+1")] ThreeOne,
    [Display(Name = "4+0")] FourZero,
    [Display(Name = "4+1")] FourOne,
    [Display(Name = "5+1")] FiveOne,
    [Display(Name = "6+1")] SixOne,
    [Display(Name = "Stüdyo")] Studio
}

public enum EstateBuildingAge
{
    [Display(Name = "0-1 Yaşında")] New,
    [Display(Name = "2-5 Yaşında")] Recent,
    [Display(Name = "6-10 Yaşında")] Medium,
    [Display(Name = "11-15 Yaşında")] Old,
    [Display(Name = "16-20 Yaşında")] VeryOld,
    [Display(Name = "21+ Yaşında")] Ancient
}

public enum EstateFloorLocation
{
    [Display(Name = "Giriş Katı")] Ground,
    [Display(Name = "Bahçe Katı")] Garden,
    [Display(Name = "Yüksek Giriş")] HighGround,
    [Display(Name = "1. Kat")] First,
    [Display(Name = "2. Kat")] Second,
    [Display(Name = "3. Kat")] Third,
    [Display(Name = "4. Kat")] Fourth,
    [Display(Name = "5. Kat")] Fifth,
    [Display(Name = "6. Kat")] Sixth,
    [Display(Name = "7. Kat")] Seventh,
    [Display(Name = "8. Kat")] Eighth,
    [Display(Name = "9. Kat")] Ninth,
    [Display(Name = "10. Kat")] Tenth,
    [Display(Name = "11-15 Arası")] ElevenFifteen,
    [Display(Name = "15+ Kat")] AboveFifteen,
    [Display(Name = "En Üst Kat")] Penthouse,
    [Display(Name = "Çatı Katı")] Attic,
    [Display(Name = "Teras Katı")] Terrace
}

public enum HeatingType
{
    [Display(Name = "Kombi (Doğalgaz)")] NaturalGas,
    [Display(Name = "Merkezi Sistem")] Central,
    [Display(Name = "Soba")] Stove,
    [Display(Name = "Klima")] AC,
    [Display(Name = "Yerden Isıtma")] Floor,
    [Display(Name = "Güneş Enerjisi")] Solar,
    [Display(Name = "Elektrikli")] Electric,
    [Display(Name = "Yok")] None
}

public enum FuelType
{
    [Display(Name = "Benzin")] Gasoline,
    [Display(Name = "Dizel")] Diesel,
    [Display(Name = "LPG")] LPG,
    [Display(Name = "Elektrik")] Electric,
    [Display(Name = "Hibrit")] Hybrid,
    [Display(Name = "Plug-in Hibrit")] PluginHybrid
}

public enum TransmissionType
{
    [Display(Name = "Manuel")] Manual,
    [Display(Name = "Otomatik")] Automatic,
    [Display(Name = "Yarı Otomatik")] SemiAuto,
    [Display(Name = "Triptonik")] Tiptronic
}

public enum SteeringType
{
    [Display(Name = "Sağ")] Right,
    [Display(Name = "Sol")] Left
}

public enum VehicleConditionState
{
    [Display(Name = "Sıfır")] New,
    [Display(Name = "İkinci El - Çok İyi")] VeryGood,
    [Display(Name = "İkinci El - İyi")] Good,
    [Display(Name = "İkinci El - Orta")] Fair,
    [Display(Name = "Kullanılmış - Bozuk Parçalar")] Poor
}

public enum BodyType
{
    [Display(Name = "Sedan")] Sedan,
    [Display(Name = "Hatchback")] Hatchback,
    [Display(Name = "Station Wagon")] Wagon,
    [Display(Name = "SUV")] SUV,
    [Display(Name = "Coupe")] Coupe,
    [Display(Name = "Cabrio")] Cabrio,
    [Display(Name = "Pick-up")] Pickup,
    [Display(Name = "Minivan")] Minivan,
    [Display(Name = "Panelvan")] Panelvan,
    [Display(Name = "Kamyonet")] Truck
}

public enum ConditionState
{
    [Display(Name = "Sıfır")] New,
    [Display(Name = "İkinci El - Çok İyi")] VeryGood,
    [Display(Name = "İkinci El - İyi")] Good,
    [Display(Name = "İkinci El - Orta")] Fair,
    [Display(Name = "Kullanılmış - Bozuk Parçalar")] Poor
}

public enum PriceType
{
    [Display(Name = "Toplam Fiyat")] Total,
    [Display(Name = "Metrekare Fiyatı")] PerSquareMeter
}

public enum AdvertiserType
{
    [Display(Name = "Sahibinden")] Owner,
    [Display(Name = "Emlak Ofisinden")] Agent,
    [Display(Name = "İnşaat Firmasından")] Developer
}

public sealed class CreateListingViewModel : IValidatableObject
{
    [Display(Name = "Ad Soyad")]
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Required(ErrorMessage = "Telefon zorunludur.")]
    [Phone(ErrorMessage = "Geçerli bir telefon girin.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "WhatsApp ile iletişime geçilebilir")]
    public bool AllowWhatsApp { get; set; } = true;

    [Display(Name = "Mesaj ile iletişime geçilebilir")]
    public bool AllowMessages { get; set; } = true;

    [Display(Name = "İlan Başlığı")]
    [Required(ErrorMessage = "İlan başlığı zorunludur.")]
    [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "İlan Açıklaması")]
    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [MinLength(40, ErrorMessage = "Açıklama en az 40 karakter olmalı.")]
    [StringLength(5000, ErrorMessage = "Açıklama en fazla 5000 karakter olabilir.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Şehir")]
    [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
    public string City { get; set; } = string.Empty;

    [Display(Name = "İlçe / Semt")]
    public string District { get; set; } = string.Empty;

    [Display(Name = "Mahalle")]
    public string? Neighborhood { get; set; }

    [Display(Name = "Ev / Bina No")]
    public string? HouseNumber { get; set; }

    [Display(Name = "Daire No")]
    public string? ApartmentNumber { get; set; }

    [Display(Name = "Açık Adres")]
    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    public string? Address { get; set; }

    [Display(Name = "Kategori")]
    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Alt Kategori")]
    public string? SubCategory { get; set; }

    [Display(Name = "İlan Tipi")]
    [Required(ErrorMessage = "İlan tipi zorunludur.")]
    public string Type { get; set; } = string.Empty;

    [Display(Name = "Fiyat")]
    public decimal? PriceAmount { get; set; }

    [Display(Name = "Para Birimi")]
    [Required(ErrorMessage = "Para birimi seçimi zorunludur.")]
    public string PriceCurrency { get; set; } = "TL";

    [Display(Name = "Fiyat Tipi")]
    public PriceType PriceType { get; set; } = PriceType.Total;

    [Display(Name = "Fiyat Açıklaması")]
    public string? PriceDescription { get; set; }

    [Display(Name = "Pazarlık Payı")]
    public bool Negotiable { get; set; } = true;

    [Display(Name = "Takas Olur")]
    public bool TradeIn { get; set; } = false;

    [Display(Name = "Kimden")]
    public AdvertiserType AdvertiserType { get; set; } = AdvertiserType.Owner;

    [Display(Name = "Görseller")]
    public List<IFormFile>? ImageFiles { get; set; }

    [Display(Name = "Kapak Fotoğrafı")]
    public int? CoverImageIndex { get; set; }

    [Display(Name = "YouTube Video Linki")]
    [Url(ErrorMessage = "Geçerli bir URL girin.")]
    public string? VideoUrl { get; set; }

    [Display(Name = "360° Tur Linki")]
    [Url(ErrorMessage = "Geçerli bir URL girin.")]
    public string? Tour360Url { get; set; }

    [Display(Name = "Harita Konumu (Lat)")]
    public double? Latitude { get; set; }

    [Display(Name = "Harita Konumu (Lng)")]
    public double? Longitude { get; set; }

    [Display(Name = "M² (Net)")]
    [Range(0, 100000, ErrorMessage = "Geçerli bir alan girin.")]
    public int? EstateNetArea { get; set; }

    [Display(Name = "M² (Brüt)")]
    [Range(0, 100000, ErrorMessage = "Geçerli bir alan girin.")]
    public int? EstateGrossArea { get; set; }

    [Display(Name = "Oda Sayısı")]
    public EstateRoomCount? EstateRoomCount { get; set; }

    [Display(Name = "Bina Yaşı")]
    public EstateBuildingAge? EstateBuildingAge { get; set; }

    [Display(Name = "Kat Sayısı")]
    [Range(1, 100, ErrorMessage = "Geçerli bir kat sayısı girin.")]
    public int? EstateTotalFloors { get; set; }

    [Display(Name = "Bulunduğu Kat")]
    public EstateFloorLocation? EstateFloorLocation { get; set; }

    [Display(Name = "Isıtma Tipi")]
    public HeatingType? HeatingType { get; set; }

    [Display(Name = "Eşyalı")]
    public bool EstateFurnished { get; set; }

    [Display(Name = "Site İçinde")]
    public bool InSite { get; set; }

    [Display(Name = "Balkon")]
    public bool HasBalcony { get; set; }

    [Display(Name = "Asansör")]
    public bool HasElevator { get; set; }

    [Display(Name = "Otopark / Garaj")]
    public bool HasParking { get; set; }

    [Display(Name = "Havuz")]
    public bool HasPool { get; set; }

    [Display(Name = "Güvenlik")]
    public bool HasSecurity { get; set; }

    [Display(Name = "Aidat")]
    [Range(0, 100000, ErrorMessage = "Geçerli bir aidat tutarı girin.")]
    public decimal? DuesAmount { get; set; }

    [Display(Name = "Depozito")]
    [Range(0, 1000000, ErrorMessage = "Geçerli bir depozito tutarı girin.")]
    public decimal? DepositAmount { get; set; }

    [Display(Name = "Marka")]
    public string? VehicleBrand { get; set; }

    [Display(Name = "Model")]
    public string? VehicleModel { get; set; }

    [Display(Name = "Model Yılı")]
    [Range(1950, 2030, ErrorMessage = "Geçerli bir model yılı girin.")]
    public int? VehicleYear { get; set; }

    [Display(Name = "Yakıt Tipi")]
    public FuelType? VehicleFuelType { get; set; }

    [Display(Name = "Vites Tipi")]
    public TransmissionType? VehicleTransmission { get; set; }

    [Display(Name = "Kilometre")]
    [Range(0, 2000000, ErrorMessage = "Geçerli bir kilometre girin.")]
    public int? VehicleKM { get; set; }

    [Display(Name = "Kasa Tipi")]
    public BodyType? VehicleBodyType { get; set; }

    [Display(Name = "Motor Hacmi (cc)")]
    [Range(500, 10000, ErrorMessage = "Geçerli bir motor hacmi girin.")]
    public int? EngineCapacity { get; set; }

    [Display(Name = "Motor Gücü (HP)")]
    [Range(20, 2000, ErrorMessage = "Geçerli bir motor gücü girin.")]
    public int? EnginePower { get; set; }

    [Display(Name = "Renk")]
    public string? VehicleColor { get; set; }

    [Display(Name = "Plaka / Uyruk")]
    public string? VehiclePlate { get; set; }

    [Display(Name = "Garanti Durumu")]
    public bool UnderWarranty { get; set; }

    [Display(Name = "Kaza Kaydı")]
    public string? AccidentRecord { get; set; }

    [Display(Name = "Araç Durumu")]
    public VehicleConditionState? VehicleCondition { get; set; }

    [Display(Name = "Direksiyon Tipi")]
    public SteeringType? VehicleSteeringType { get; set; }

    [Display(Name = "Ürün Markası")]
    public string? ProductBrand { get; set; }

    [Display(Name = "Ürün Modeli")]
    public string? ProductModel { get; set; }

    [Display(Name = "Durumu")]
    public ConditionState? ProductCondition { get; set; }

    [Display(Name = "Garanti Süresi")]
    public string? WarrantyPeriod { get; set; }

    [Display(Name = "Seri Numarası")]
    public string? SerialNumber { get; set; }

    [Display(Name = "Kullanım Durumu")]
    public string? UsageDuration { get; set; }

    [Display(Name = "Anahtar Kelimeler")]
    public string? Tags { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitude is < -90 or > 90)
        {
            yield return new ValidationResult("Enlem degeri gecersiz.", new[] { nameof(Latitude) });
        }

        if (Longitude is < -180 or > 180)
        {
            yield return new ValidationResult("Boylam degeri gecersiz.", new[] { nameof(Longitude) });
        }

        foreach (var result in ListingSubmissionRules.ValidateCoreFields(Title, Description, Category, SubCategory, Type, City))
        {
            yield return result;
        }

        var normalized = ListingTaxonomy.NormalizeForPersistence(Category, SubCategory);
        var normalizedCategory = normalized.Category;
        var normalizedSubCategory = normalized.SubCategory;

        if (!ListingTaxonomy.IsPetAdoption(normalizedCategory, Type))
        {
            if (!PriceAmount.HasValue)
            {
                yield return new ValidationResult("Fiyat zorunludur.", new[] { nameof(PriceAmount) });
            }
            else if (PriceAmount.Value <= 0)
            {
                yield return new ValidationResult("Fiyat 0'dan buyuk olmalidir.", new[] { nameof(PriceAmount) });
            }
        }

        if (string.IsNullOrWhiteSpace(normalizedCategory))
        {
            yield break;
        }

        if (PriceAmount.HasValue && PriceAmount.Value != Math.Truncate(PriceAmount.Value))
        {
            yield return new ValidationResult("Fiyat tam sayı olmalıdır.", new[] { nameof(PriceAmount) });
        }

        if (ImageFiles == null || ImageFiles.Count == 0)
        {
            yield return new ValidationResult("İlan yayınlamak için en az bir gerçek görsel yüklemelisiniz.", new[] { nameof(ImageFiles) });
        }

        if (CoverImageIndex.HasValue && ImageFiles != null && (CoverImageIndex < 0 || CoverImageIndex >= ImageFiles.Count))
        {
            yield return new ValidationResult("Kapak görseli seçimi yüklenen görsellerle uyuşmuyor.", new[] { nameof(CoverImageIndex) });
        }

        if (normalizedCategory == "estate")
        {
            if (!EstateNetArea.HasValue || EstateNetArea <= 0)
            {
                yield return new ValidationResult("Emlak ilanlari icin net metrekare zorunludur.", new[] { nameof(EstateNetArea) });
            }

            if (EstateGrossArea.HasValue && EstateGrossArea <= 0)
            {
                yield return new ValidationResult("Brut metrekare 0'dan buyuk olmalidir.", new[] { nameof(EstateGrossArea) });
            }

            if (EstateNetArea.HasValue && EstateGrossArea.HasValue && EstateGrossArea < EstateNetArea)
            {
                yield return new ValidationResult("Brüt metrekare net metrekareden küçük olamaz.", new[] { nameof(EstateGrossArea), nameof(EstateNetArea) });
            }

            if (string.Equals(normalizedSubCategory, "konut", StringComparison.OrdinalIgnoreCase) && !EstateRoomCount.HasValue)
            {
                yield return new ValidationResult("Konut ilanlari icin oda sayisi zorunludur.", new[] { nameof(EstateRoomCount) });
            }
        }

        if (normalizedCategory == "vehicle")
        {
            if (string.IsNullOrWhiteSpace(VehicleBrand))
            {
                yield return new ValidationResult("Vasita ilanlari icin marka zorunludur.", new[] { nameof(VehicleBrand) });
            }

            if (string.IsNullOrWhiteSpace(VehicleModel))
            {
                yield return new ValidationResult("Vasita ilanlari icin model zorunludur.", new[] { nameof(VehicleModel) });
            }

            if (!VehicleYear.HasValue)
            {
                yield return new ValidationResult("Vasita ilanlari icin model yili zorunludur.", new[] { nameof(VehicleYear) });
            }

            if (!VehicleKM.HasValue)
            {
                yield return new ValidationResult("Vasita ilanlari icin kilometre zorunludur.", new[] { nameof(VehicleKM) });
            }

            if (!VehicleFuelType.HasValue)
            {
                yield return new ValidationResult("Vasita ilanlari icin yakit tipi zorunludur.", new[] { nameof(VehicleFuelType) });
            }

            if (!VehicleTransmission.HasValue)
            {
                yield return new ValidationResult("Vasita ilanlari icin vites tipi zorunludur.", new[] { nameof(VehicleTransmission) });
            }

            if (!VehicleBodyType.HasValue)
            {
                yield return new ValidationResult("Vasita ilanlari icin kasa tipi zorunludur.", new[] { nameof(VehicleBodyType) });
            }

            if (!VehicleCondition.HasValue)
            {
                yield return new ValidationResult("Vasita ilanlari icin arac durumu zorunludur.", new[] { nameof(VehicleCondition) });
            }
        }

        if (ListingTaxonomy.IsProductPanelCategory(normalizedCategory) && ListingTaxonomy.RequiresProductCondition(normalizedCategory) && !ProductCondition.HasValue)
        {
            yield return new ValidationResult("Bu ilan kategorisi icin urun durumu zorunludur.", new[] { nameof(ProductCondition) });
        }

        if (ListingSubmissionRules.RequiresDetailedProductIdentity(normalizedCategory) &&
            string.IsNullOrWhiteSpace(ProductBrand) &&
            string.IsNullOrWhiteSpace(ProductModel))
        {
            yield return new ValidationResult("Bu kategori icin marka veya model bilgisinden en az biri zorunludur.", new[] { nameof(ProductBrand), nameof(ProductModel) });
        }

        if (ImageFiles is { Count: > 20 })
        {
            yield return new ValidationResult("En fazla 20 gorsel yukleyebilirsiniz.", new[] { nameof(ImageFiles) });
        }
    }
}
