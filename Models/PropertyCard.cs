namespace SEN_T_PAZAR.Models;

public sealed class PropertyCard
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

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

    public string SellerName { get; set; } = string.Empty;

    public string SellerRole { get; set; } = string.Empty;

    public string SellerPhone { get; set; } = string.Empty;

    public string PostedAtLabel { get; set; } = string.Empty;

    public string ListingCode { get; set; } = string.Empty;

    public string AvailabilityNote { get; set; } = string.Empty;

    public string DetailBody { get; set; } = string.Empty;

    public string Neighborhood { get; set; } = string.Empty;

    public double AverageRating { get; set; } = 0.0;

    public int ReviewCount { get; set; } = 0;

    public List<Review> Reviews { get; set; } = [];
}

public sealed class ListingFact
{
    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
