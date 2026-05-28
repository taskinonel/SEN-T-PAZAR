using System.ComponentModel.DataAnnotations;

namespace SEN_T_PAZAR.Models;

public class AccountDashboardViewModel
{
    public required ApplicationUser User { get; set; }
    public required ProfileUpdateViewModel ProfileForm { get; set; }
    public required ChangePasswordViewModel PasswordForm { get; set; }
    public required NotificationSettingsViewModel NotificationForm { get; set; }
    public required List<MyListingCardViewModel> MyListings { get; set; }
    public required List<FavoriteItemViewModel> Favorites { get; set; }
    public required List<BillingItemViewModel> BillingItems { get; set; }
    public required DashboardStatsViewModel Stats { get; set; }
    
    // Aktif paketler
    public List<UserPackageInfo> ActivePackages { get; set; } = new();
    
    // Fiyatlandırma paketleri
    public List<PricingPackage> AvailablePackages { get; set; } = new();
}

public class ProfileUpdateViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Telefon formatı geçersiz.")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    [Display(Name = "Adres")]
    public string? AddressLine { get; set; }

    [StringLength(100)]
    [Display(Name = "Şehir")]
    public string? City { get; set; }

    [StringLength(150)]
    [Display(Name = "Profil Fotoğrafı URL")]
    [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Yeni şifreler eşleşmiyor.")]
    [Display(Name = "Yeni Şifre (Tekrar)")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class NotificationSettingsViewModel
{
    [Display(Name = "E-posta Bildirimleri")]
    public bool EmailNotifications { get; set; } = true;

    [Display(Name = "SMS Bildirimleri")]
    public bool SmsNotifications { get; set; } = false;
}

public class MyListingCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "TL";
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Vitrin & Öne Çıkan
    public bool IsFeatured { get; set; }
    public bool IsVitrin { get; set; }
    public DateTime? FeaturedExpiryDate { get; set; }
    public DateTime? VitrinExpiryDate { get; set; }
    
    // Görsel
    public string? CoverImageUrl { get; set; }
}

public class FavoriteItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "TL";
}

public class BillingItemViewModel
{
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TL";
    public string Status { get; set; } = "Ödendi";
}

public class DashboardStatsViewModel
{
    public int TotalListings { get; set; }
    public int ApprovedListings { get; set; }
    public int PendingListings { get; set; }
    public int FavoritesCount { get; set; }
    
    // Vitrin & Öne Çıkan İstatistikler
    public int FeaturedListings { get; set; }
    public int VitrinListings { get; set; }
}

public class UserPackageInfo
{
    public string PackageName { get; set; } = string.Empty;
    public string PackageType { get; set; } = string.Empty;
    public int RemainingUses { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
