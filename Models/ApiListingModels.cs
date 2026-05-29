using System.ComponentModel.DataAnnotations;

namespace SEN_T_PAZAR.Models;

public sealed class ApiListingUpsertRequest : IValidatableObject
{
    [Required(ErrorMessage = "İlan başlığı zorunludur.")]
    [StringLength(120, ErrorMessage = "İlan başlığı en fazla 120 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlan açıklaması zorunludur.")]
    [StringLength(5000, ErrorMessage = "İlan açıklaması en fazla 5000 karakter olabilir.")]
    [MinLength(40, ErrorMessage = "İlan açıklaması en az 40 karakter olmalıdır.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori zorunludur.")]
    [StringLength(60)]
    public string Category { get; set; } = string.Empty;

    [StringLength(60)]
    public string? SubCategory { get; set; }

    [Required(ErrorMessage = "İlan tipi zorunludur.")]
    [StringLength(60)]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şehir zorunludur.")]
    [StringLength(80)]
    public string City { get; set; } = string.Empty;

    [StringLength(80)]
    public string? District { get; set; }

    [StringLength(80)]
    public string? Neighborhood { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Range(1, 1000000000, ErrorMessage = "Fiyat 0'dan büyük olmalıdır.")]
    public decimal PriceAmount { get; set; }

    [Required(ErrorMessage = "Para birimi zorunludur.")]
    [StringLength(10)]
    public string PriceCurrency { get; set; } = "TL";

    [StringLength(50)]
    public string PriceType { get; set; } = "Total";

    [StringLength(150)]
    public string? PriceDescription { get; set; }

    public bool Negotiable { get; set; } = true;
    public bool TradeIn { get; set; }

    [StringLength(30)]
    public string AdvertiserType { get; set; } = "Owner";

    [StringLength(120)]
    public string? FullName { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
    public string? Phone { get; set; }

    public bool AllowWhatsApp { get; set; } = true;
    public bool AllowMessages { get; set; } = true;

    [Url(ErrorMessage = "Video bağlantısı geçerli bir URL olmalıdır.")]
    public string? VideoUrl { get; set; }

    [Url(ErrorMessage = "360 tur bağlantısı geçerli bir URL olmalıdır.")]
    public string? Tour360Url { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public List<string>? ImageUrls { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in ListingSubmissionRules.ValidateCoreFields(Title, Description, Category, SubCategory, Type, City))
        {
            yield return result;
        }

        if (decimal.Truncate(PriceAmount) != PriceAmount)
        {
            yield return new ValidationResult("Fiyat tam sayı olmalıdır.", new[] { nameof(PriceAmount) });
        }

        if (Latitude is < -90 or > 90)
        {
            yield return new ValidationResult("Enlem değeri geçersiz.", new[] { nameof(Latitude) });
        }

        if (Longitude is < -180 or > 180)
        {
            yield return new ValidationResult("Boylam değeri geçersiz.", new[] { nameof(Longitude) });
        }

        if (ImageUrls == null || ImageUrls.Count == 0)
        {
            yield return new ValidationResult("API ile ilan oluştururken en az bir görsel URL göndermelisiniz.", new[] { nameof(ImageUrls) });
        }

        if (ImageUrls is { Count: > 20 })
        {
            yield return new ValidationResult("En fazla 20 görsel URL gönderebilirsiniz.", new[] { nameof(ImageUrls) });
        }
    }
}

public sealed class ApiListingResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? SubCategory { get; set; }
    public string Type { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? Address { get; set; }
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "TL";
    public string PriceType { get; set; } = "Total";
    public string? PriceDescription { get; set; }
    public bool Negotiable { get; set; }
    public bool TradeIn { get; set; }
    public string AdvertiserType { get; set; } = "Owner";
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool AllowWhatsApp { get; set; }
    public bool AllowMessages { get; set; }
    public string? VideoUrl { get; set; }
    public string? Tour360Url { get; set; }
    public bool Has360Tour { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsApproved { get; set; }
    public bool IsClosed { get; set; }
    public string DealStatus { get; set; } = "open";
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}