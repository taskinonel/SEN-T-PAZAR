using System.ComponentModel.DataAnnotations;

namespace SEN_T_PAZAR.Models;

public class CorporateMembershipViewModel
{
    // Şirket Bilgileri
    [Required(ErrorMessage = "Şirket adı zorunludur.")]
    [Display(Name = "Şirket / Marka Adı")]
    [StringLength(100, MinimumLength = 2)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi numarası zorunludur.")]
    [Display(Name = "Vergi Numarası")]
    [StringLength(11, MinimumLength = 10)]
    public string CompanyTaxNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi dairesi zorunludur.")]
    [Display(Name = "Vergi Dairesi")]
    [StringLength(50)]
    public string CompanyTaxOffice { get; set; } = string.Empty;

    [Display(Name = "Mersis Numarası")]
    [StringLength(16)]
    public string? CompanyMersisNumber { get; set; }

    [Required(ErrorMessage = "Şirket telefonu zorunludur.")]
    [Display(Name = "Şirket Telefonu")]
    [Phone]
    public string CompanyPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şirket adresi zorunludur.")]
    [Display(Name = "Şirket Adresi")]
    [StringLength(500)]
    public string CompanyAddress { get; set; } = string.Empty;

    [Display(Name = "Web Sitesi")]
    [Url]
    public string? CompanyWebSite { get; set; }

    [Display(Name = "Şirket Logosu")]
    [Url]
    public string? CompanyLogoUrl { get; set; }

    // İletişim Yetkilisi
    [Required(ErrorMessage = "Yetkili adı zorunludur.")]
    [Display(Name = "Yetkili Adı Soyadı")]
    public string ContactPersonName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yetkili telefonu zorunludur.")]
    [Display(Name = "Yetkili Telefonu")]
    [Phone]
    public string ContactPersonPhone { get; set; } = string.Empty;

    [Display(Name = "Yetkili E-postası")]
    [EmailAddress]
    public string? ContactPersonEmail { get; set; }

    // Abonelik Planı
    [Required(ErrorMessage = "Lütfen bir plan seçin.")]
    [Display(Name = "Abonelik Planı")]
    public string SelectedPlan { get; set; } = "basic";

    // Sözleşmeler
    [Required(ErrorMessage = "Kurumsal üyelik sözleşmesini onaylamanız gerekmektedir.")]
    [Display(Name = "Kurumsal Üyelik Sözleşmesi")]
    public bool AcceptCorporateAgreement { get; set; } = false;

    [Required(ErrorMessage = "KVKK metnini onaylamanız gerekmektedir.")]
    [Display(Name = "KVKK Aydınlatma Metni")]
    public bool AcceptKvkk { get; set; } = false;

    [Display(Name = "E-posta bültenleri almak istiyorum")]
    public bool AcceptNewsletter { get; set; } = false;
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
