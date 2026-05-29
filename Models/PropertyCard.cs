namespace SEN_T_PAZAR.Models;

public sealed class PropertyCard
{
    public int Id { get; set; }
    public bool IsImported { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsVitrin { get; set; }
    public int? FeaturedOrder { get; set; }
    public int? VitrinOrder { get; set; }
    public int? PopularOrder { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string SubCategory { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal PriceAmount { get; set; }

    public string PriceLabel { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string PrimarySpec { get; set; } = string.Empty;

    public string SecondarySpec { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string Rooms { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public List<string> GalleryImages { get; set; } = [];

    public List<ListingFact> Facts { get; set; } = [];

    public List<string> Highlights { get; set; } = [];

    public List<string> FeatureBadges { get; set; } = [];

    public List<string> ExteriorFeatures { get; set; } = [];

    public List<string> InteriorFeatures { get; set; } = [];

    public List<string> LocationFeatures { get; set; } = [];

    public bool IsResidentialEstate { get; set; }

    public string SellerName { get; set; } = string.Empty;

    public string SellerRole { get; set; } = string.Empty;

    public string SellerCity { get; set; } = string.Empty;

    public string SellerPhone { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public bool SellerIsCorporate { get; set; }

    public string SellerAvatarUrl { get; set; } = string.Empty;

    public string SellerCompanyName { get; set; } = string.Empty;

    public string SellerCompanyLogoUrl { get; set; } = string.Empty;
    public bool IsVerifiedSeller { get; set; } = false;

    public bool AllowWhatsApp { get; set; } = true;

    public bool AllowMessages { get; set; } = true;

    public string PostedAtLabel { get; set; } = string.Empty;

    public string ListingCode { get; set; } = string.Empty;

    public string AvailabilityNote { get; set; } = string.Empty;

    public string DetailBody { get; set; } = string.Empty;

    public string Neighborhood { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public double AverageRating { get; set; } = 0.0;

    public int ReviewCount { get; set; } = 0;

    public List<Review> Reviews { get; set; } = [];
    public string? VideoUrl { get; set; }
    public string? Tour360Url { get; set; }
    public bool Has360Tour { get; set; }

    public List<PropertyCard> ShowcaseListings { get; set; } = [];

    public List<PropertyCard> FeaturedSideListings { get; set; } = [];

    public List<PropertyCard> PopularListings { get; set; } = [];

    public List<CorporateProfileCard> CorporateProfiles { get; set; } = [];
}

public sealed class CorporateProfileCard
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public string LogoUrl { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    public int ActiveListingCount { get; set; }
}

public sealed class ListingFact
{
    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
