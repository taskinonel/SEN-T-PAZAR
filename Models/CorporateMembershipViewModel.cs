using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SEN_T_PAZAR.Models;

public class CorporateMembershipViewModel
{
    // Şirket Bilgileri
    [Required(ErrorMessage = "Şirket adı zorunludur.")]
    [Display(Name = "Şirket / Marka Adı")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Şirket adı 2 ile 100 karakter arasında olmalıdır.")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi numarası zorunludur.")]
    [Display(Name = "Vergi Numarası")]
    [RegularExpression("^[0-9]{6,10}$", ErrorMessage = "KKTC vergi numarası 6 ile 10 rakam arasında olmalıdır.")]
    public string CompanyTaxNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi dairesi zorunludur.")]
    [Display(Name = "Vergi Dairesi")]
    [StringLength(50, ErrorMessage = "Vergi dairesi en fazla 50 karakter olabilir.")]
    public string CompanyTaxOffice { get; set; } = string.Empty;

    [Display(Name = "Şirket Sicil / Mukayyitlik No")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Şirket sicil numarası 3 ile 20 karakter arasında olmalıdır.")]
    public string? CompanyMersisNumber { get; set; }

    [Required(ErrorMessage = "Şirket telefonu zorunludur.")]
    [Display(Name = "Şirket Telefonu")]
    [RegularExpression("^(\\+90\\s?)?(392|5\\d{2})[\\s-]?\\d{3}[\\s-]?\\d{2}[\\s-]?\\d{2}$", ErrorMessage = "KKTC telefon formatı girin. Örnek: 0392 123 45 67 veya 0533 123 45 67")]
    public string CompanyPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şirket adresi zorunludur.")]
    [Display(Name = "Şirket Adresi")]
    [StringLength(500, ErrorMessage = "Şirket adresi en fazla 500 karakter olabilir.")]
    public string CompanyAddress { get; set; } = string.Empty;

    [Display(Name = "Web Sitesi")]
    [Url]
    public string? CompanyWebSite { get; set; }

    [Display(Name = "Şirket Logosu")]
    public string? CompanyLogoUrl { get; set; }

    [Display(Name = "Şirket Logosu Dosyası")]
    public IFormFile? CompanyLogoFile { get; set; }

    // İletişim Yetkilisi
    [Required(ErrorMessage = "Yetkili adı zorunludur.")]
    [Display(Name = "Yetkili Adı Soyadı")]
    public string ContactPersonName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yetkili telefonu zorunludur.")]
    [Display(Name = "Yetkili Telefonu")]
    [RegularExpression("^(\\+90\\s?)?(392|5\\d{2})[\\s-]?\\d{3}[\\s-]?\\d{2}[\\s-]?\\d{2}$", ErrorMessage = "KKTC telefon formatı girin. Örnek: 0392 123 45 67 veya 0533 123 45 67")]
    public string ContactPersonPhone { get; set; } = string.Empty;

    [Display(Name = "Yetkili E-postası")]
    [EmailAddress]
    public string? ContactPersonEmail { get; set; }

    // Abonelik Planı
    [Required(ErrorMessage = "Lütfen bir plan seçin.")]
    [Display(Name = "Abonelik Planı")]
    public string SelectedPlan { get; set; } = "free";

    // Sözleşmeler
    [Required(ErrorMessage = "Kurumsal üyelik sözleşmesini onaylamanız gerekmektedir.")]
    [Display(Name = "Kurumsal Üyelik Sözleşmesi")]
    public bool AcceptCorporateAgreement { get; set; } = false;

    [Required(ErrorMessage = "KVKK metnini onaylamanız gerekmektedir.")]
    [Display(Name = "KVKK Aydınlatma Metni")]
    public bool AcceptKvkk { get; set; } = false;

    [Display(Name = "E-posta bültenleri almak istiyorum")]
    public bool AcceptNewsletter { get; set; } = false;

    public bool IsCorporateMember { get; set; } = false;
    public string? SubscriptionPlan { get; set; }
    public DateTime? CompanyApprovalDate { get; set; }
    public string? CorporateNote { get; set; }
    public string? Filter { get; set; }
}

public class CorporatePlanViewModel
{
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int ListingsPerMonth { get; set; }
    public int FeaturedListings { get; set; }
    public int VitrinListings { get; set; }
    public bool HasAnalytics { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool HasCustomLogo { get; set; }
    public bool HasVerifiedBadge { get; set; }
    public bool HasApiAccess { get; set; }
    public List<string> Features { get; set; } = new();
}
