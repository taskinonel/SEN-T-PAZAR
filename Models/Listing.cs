using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEN_T_PAZAR.Models;

public class Listing
{
    [Key]
    public int Id { get; set; }
    
    // İletişim Bilgileri
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool AllowWhatsApp { get; set; } = true;
    public bool AllowMessages { get; set; } = true;
    
    // İlan Bilgileri
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Tags { get; set; }
    
    // Konum Bilgileri
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighborhood { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Kategori ve Tip
    public string Category { get; set; } = string.Empty;
    public string? SubCategory { get; set; }
    public string Type { get; set; } = string.Empty;
    
    // Fiyat Bilgileri
    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "TL";
    public string PriceType { get; set; } = "Total";
    public string? PriceDescription { get; set; }
    public bool Negotiable { get; set; } = true;
    public bool TradeIn { get; set; } = false;
    public string AdvertiserType { get; set; } = "Owner";
    
    // Emlak Özellikleri
    public int? EstateNetArea { get; set; }
    public int? EstateGrossArea { get; set; }
    public string? EstateRoomCount { get; set; }
    public string? EstateBuildingAge { get; set; }
    public int? EstateTotalFloors { get; set; }
    public string? EstateFloorLocation { get; set; }
    public string? HeatingType { get; set; }
    public bool? EstateFurnished { get; set; }
    public bool? InSite { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? HasElevator { get; set; }
    public bool? HasParking { get; set; }
    public bool? HasPool { get; set; }
    public bool? HasSecurity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DuesAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DepositAmount { get; set; }
    
    // Araç Bilgileri
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public string? VehicleFuelType { get; set; }
    public string? VehicleTransmission { get; set; }
    public int? VehicleKM { get; set; }
    public string? VehicleBodyType { get; set; }
    public int? EngineCapacity { get; set; }
    public int? EnginePower { get; set; }
    public string? VehicleColor { get; set; }
    public string? VehiclePlate { get; set; }
    public bool? UnderWarranty { get; set; }
    public string? AccidentRecord { get; set; }
    
    // Ürün Bilgileri
    public string? ProductBrand { get; set; }
    public string? ProductModel { get; set; }
    public string? ProductCondition { get; set; }
    public string? WarrantyPeriod { get; set; }
    public string? SerialNumber { get; set; }
    public string? UsageDuration { get; set; }
    
    // Medya
    public string? VideoUrl { get; set; }
    public int CoverImageIndex { get; set; } = 0;
    
    // Durum
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; } = false;
    public string? UserId { get; set; }
    
    // Değerlendirme Sistemi
    public double AverageRating { get; set; } = 0.0;
    public int ReviewCount { get; set; } = 0;
    
    // =====================
    // VİTRİN & ÖNE ÇIKAN
    // =====================
    public bool IsFeatured { get; set; } = false;        // Öne çıkan ilan
    public bool IsVitrin { get; set; } = false;           // Vitrinde göster
    public DateTime? FeaturedExpiryDate { get; set; }    // Öne çıkan bitiş
    public DateTime? VitrinExpiryDate { get; set; }      // Vitrin bitiş
    public string? FeaturedPackage { get; set; }         // "gold", "silver", "bronze"
    public string? VitrinPackage { get; set; }           // "vip", "super", "standart"
    
    // İlişkiler
    public List<ListingImage> Images { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public List<string> Highlights { get; set; } = new();
    public List<string> FeatureBadges { get; set; } = new();
}

public class ListingImage
{
    [Key]
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int ListingId { get; set; }
    public Listing? Listing { get; set; }
}
