namespace SEN_T_PAZAR.Models;

public sealed class CorporateStoreViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    public string LogoUrl { get; set; } = string.Empty;

    public int ActiveListingCount { get; set; }

    public List<PropertyCard> Listings { get; set; } = [];
}