using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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
    public required List<VisitorMessageThreadViewModel> VisitorMessages { get; set; }
    public required DashboardStatsViewModel Stats { get; set; }
    public int NotificationCount { get; set; } = 0;
    public string Filter { get; set; } = "all";
    
    // Seçili mesaj thread ID (detay görünümü için)
    public int? SelectedThreadId { get; set; }
    
    // Aktif paketler
    public List<UserPackageInfo> ActivePackages { get; set; } = new();
    
    // Fiyatlandırma paketleri
    public List<PricingPackage> AvailablePackages { get; set; } = new();

    // Doğrulama formları
    public EmailVerificationViewModel EmailVerificationForm { get; set; } = new();
}

public class ProfileUpdateViewModel
{
    [Display(Name = "Ad")]
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Soyad")]
    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(60)]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Telefon formatı geçersiz.")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    [Display(Name = "Adres")]
    public string? AddressLine { get; set; }

    [StringLength(100)]
    [Display(Name = "Şehir")]
    public string? City { get; set; }

    [Display(Name = "Profil Fotoğrafı")]
    public IFormFile? AvatarFile { get; set; }
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
}

public class MyListingCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "TL";
    public bool IsApproved { get; set; }
    public int ViewCount { get; set; }
    public int FavoritesCount { get; set; }
    public bool IsClosed { get; set; }
    public string DealStatus { get; set; } = "open";
    public DateTime CreatedAt { get; set; }
    public string? RawFilePath { get; set; }
    
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

public class VisitorMessageThreadViewModel
{
    public string ConversationId { get; set; } = string.Empty;
    public int RootMessageId { get; set; }
    public int Id { get; set; }
    public int ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string? SenderPhone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string SenderRole { get; set; } = string.Empty;
    public List<VisitorMessageEntryViewModel> Messages { get; set; } = [];
    public string ReplyText { get; set; } = string.Empty;
}

public class VisitorMessageEntryViewModel
{
    public int Id { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public bool IsRead { get; set; }
}

public class DashboardStatsViewModel
{
    public int TotalListings { get; set; }
    public int ApprovedListings { get; set; }
    public int PendingListings { get; set; }
    public int TotalViews { get; set; }
    public int TotalFavorites { get; set; }
    public int TotalMessages { get; set; }
    public double AverageViewsPerListing { get; set; }
    
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

public class ListingEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "İlan başlığı zorunludur.")]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(5000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
        public string City { get; set; } = string.Empty;

        public string? District { get; set; }

        public string? Neighborhood { get; set; }
        public string? HouseNumber { get; set; }
        public string? ApartmentNumber { get; set; }
        public string? Address { get; set; }

        [Range(1, 1000000000)]
        public decimal PriceAmount { get; set; }

        public string PriceCurrency { get; set; } = "TL";

        [Display(Name = "Ad Soyad")]
        [StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Telefon")]
        [Phone(ErrorMessage = "Telefon formatı geçersiz.")]
        public string Phone { get; set; } = string.Empty;

        public List<ListingEditImageItemViewModel> ExistingImages { get; set; } = new();
        public List<IFormFile>? NewImageFiles { get; set; }
        public List<int> DeleteImageIds { get; set; } = new();
        public int? CoverImageId { get; set; }
    }

public class ListingEditImageItemViewModel
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public bool IsCover { get; set; }
}

public class EmailVerificationViewModel
{
    [Display(Name = "Yeni E-posta")]
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string NewEmail { get; set; } = string.Empty;

    [Display(Name = "E-posta Doğrulama Kodu")]
    [RegularExpression("^$|^[0-9]{6}$", ErrorMessage = "Kod 6 haneli olmalıdır.")]
    public string? VerificationCode { get; set; }
}


