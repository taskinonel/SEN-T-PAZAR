using Microsoft.AspNetCore.Identity;

namespace SEN_T_PAZAR.Models;

public class ApplicationUser : IdentityUser
{
    // Temel bilgiler
    public string FullName { get; set; } = string.Empty;
    
    // Adres bilgileri
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    
    // Profil bilgileri
    public string? AvatarUrl { get; set; }

    // Mobil bildirim bilgileri
    public string? FcmToken { get; set; }
    public DateTime? FcmTokenUpdatedAtUtc { get; set; }
    
    // Bildirim ayarları
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    
    // =====================
    // KURUMSAL ÜYE ALANLARI
    // =====================
    
    // Şirket Bilgileri
    public bool IsCorporateMember { get; set; } = false;
    public string? CompanyName { get; set; }
    public string? CompanyTaxNumber { get; set; }
    public string? CompanyTaxOffice { get; set; }
    public string? CompanyMersisNumber { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyWebSite { get; set; }
    public string? CompanyLogoUrl { get; set; }
    
    // Abonelik Bilgileri
    public string? SubscriptionPlan { get; set; } // "free", "basic", "pro", "enterprise"
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public bool IsSubscriptionActive { get; set; } = false;
    
    // İstatistikler
    public int ListingsRemainingThisMonth { get; set; } = 0;
    public int TotalListingsAllowed { get; set; } = 0;
    public int FeaturedListingsIncluded { get; set; } = 0;
    public int FeaturedListingsUsed { get; set; } = 0;
    public int VitrinListingsIncluded { get; set; } = 0;
    public int VitrinListingsUsed { get; set; } = 0;
    
    // Onay Bilgileri
    public bool IsCorporateApproved { get; set; } = false;
    public DateTime? CorporateApprovalDate { get; set; }
    public string? CorporateNote { get; set; }

    // Satıcı Doğrulama Bilgileri
    public bool IsVerifiedSeller { get; set; } = false;
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedByAdminId { get; set; }
    public string? VerificationNotes { get; set; }
    
    // Belgeler
    public List<Document> Documents { get; set; } = new();

    public int EffectiveListingsRemainingThisMonth => IsCorporateMember ? int.MaxValue : ListingsRemainingThisMonth;
    public int EffectiveTotalListingsAllowed => IsCorporateMember ? int.MaxValue : TotalListingsAllowed;
}
