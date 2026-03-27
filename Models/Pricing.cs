using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEN_T_PAZAR.Models;

/// <summary>
/// Fiyatlandırma paketi - Vitrin ve Öne Çıkan özellikler için
/// </summary>
public class PricingPackage
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "Gold Öne Çıkan", "VIP Vitrin" vb.
    
    [MaxLength(50)]
    public string PackageType { get; set; } = string.Empty; // "featured", "vitrin", "combo"
    
    [MaxLength(50)]
    public string Tier { get; set; } = string.Empty; // "gold", "silver", "bronze" veya "vip", "super", "standart"
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "TL";
    
    public int DurationDays { get; set; } // Paket süresi (gün)
    
    public int ListingsIncluded { get; set; } = 1; // Kaç ilan için geçerli
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int DisplayOrder { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Ödeme kaydı - Kullanıcıların satın aldığı paketleri takip eder
/// </summary>
public class UserPackage
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public int PackageId { get; set; }
    public PricingPackage? Package { get; set; }
    
    public int TotalPurchased { get; set; } = 1; // Toplam satın alınan
    public int UsedCount { get; set; } = 0; // Kullanılan
    
    public int RemainingCount => TotalPurchased - UsedCount;
    
    public DateTime? ExpiryDate { get; set; } // Paket bitiş tarihi
    
    public bool IsActive { get; set; } = true;
    
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Ödeme kaydı - Her ödeme işlemi için detaylar
/// </summary>
public class Payment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public int? PackageId { get; set; }
    public PricingPackage? Package { get; set; }
    
    // Ödeme bilgileri
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = string.Empty; // "credit_card", "bank_transfer", "wallet"
    
    [MaxLength(50)]
    public string PaymentStatus { get; set; } = "pending"; // "pending", "completed", "failed", "refunded"
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "TL";
    
    // İşlem bilgileri
    [MaxLength(200)]
    public string? TransactionId { get; set; } // Ödeme sağlayıcı işlem ID
    
    [MaxLength(200)]
    public string? ExternalPaymentId { get; set; } // Harici ödeme ID
    
    [MaxLength(500)]
    public string? PaymentDetails { get; set; } // JSON olarak detaylar
    
    public string? Note { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// İlanın vitrin/öne çıkan özelliklerini yönetmek için kullanılan sınıf
/// </summary>
public class ListingPromotion
{
    [Key]
    public int Id { get; set; }
    
    public int ListingId { get; set; }
    public Listing? Listing { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string PromotionType { get; set; } = string.Empty; // "featured", "vitrin"
    
    [MaxLength(50)]
    public string PackageName { get; set; } = string.Empty;
    
    public int DurationDays { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int? PaymentId { get; set; }
}