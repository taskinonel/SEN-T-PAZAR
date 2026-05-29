namespace SEN_T_PAZAR.Models;

public sealed class HomePageViewModel
{
    // Vitrin, Öne Çıkan, Popüler ilanlar için ayrı property'ler
    public List<PropertyCard> VitrinListings { get; set; } = new();
    public List<PropertyCard> FeaturedListings { get; set; } = new();
    public List<PropertyCard> PopularListings { get; set; } = new();
    public List<PropertyCard> SearchResults { get; set; } = new();
    public List<ProjectCard> FeaturedEmlak { get; set; } = new();
    public List<ProjectCard> FeaturedVasita { get; set; } = new();
    public List<ProjectCard> FeaturedElektronik { get; set; } = new();
    public List<ProjectCard> FeaturedEvEsya { get; set; } = new();
    public List<ProjectCard> FeaturedHizmet { get; set; } = new();
    public string HeroEyebrow { get; set; } = string.Empty;
    public string HeroTitle { get; set; } = string.Empty;

    public string HeroSubtitle { get; set; } = string.Empty;

    // Eski FeaturedListings property’si kaldırıldı (çift tanım engellendi)

    public List<PropertyCard> RecommendedListings { get; set; } = [];

    public List<string> PopularRegions { get; set; } = [];

    public List<RegionSpot> RegionSpots { get; set; } = [];

    public List<ProjectCard> FeaturedProjects { get; set; } = [];

    public List<CorporateProfileCard> CorporateProfiles { get; set; } = [];

    public List<string> PartnerNames { get; set; } = [];

    public List<MarketCategory> MarketCategories { get; set; } = [];

    public List<MarketTile> MarketTiles { get; set; } = [];

    public List<string> ListingTypeOptions { get; set; } = [];

    public List<string> CityOptions { get; set; } = [];

    public List<string> CategoryOptions { get; set; } = [];

    public List<string> PriceRangeOptions { get; set; } = [];

    public List<string> SortOptions { get; set; } = [];

    public List<RegionalCampaign> RegionalCampaigns { get; set; } = [];

    public List<SubCategoryFilter> SubCategoryFilters { get; set; } = [];

    public Dictionary<string, List<SubCategoryFilter>> CategorySubCategoryMap { get; set; } = new();

    public List<SearchTabOption> SearchTabs { get; set; } = [];

    public string CategorySubCategoryJson { get; set; } = "{}";

    public string ListingTypeCategoryJson { get; set; } = "{}";

    public List<string> VehicleBrandOptions { get; set; } = [];

    public Dictionary<string, List<string>> VehicleModelOptionsByBrand { get; set; } = new();

    public string ListingType { get; set; } = "all";

    public string City { get; set; } = "all";

    public string Category { get; set; } = "all";

    public string SubCategory { get; set; } = "all";

    public string PriceRange { get; set; } = "any";

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public string Keyword { get; set; } = string.Empty;

    public string SortBy { get; set; } = "latest";

    public bool IsCategoryPage { get; set; }

    public bool ShowResults { get; set; }

    public string CurrentCategorySlug { get; set; } = string.Empty;

    public string CategoryHeroImage { get; set; } = string.Empty;

    public SeoLandingContent? SeoLanding { get; set; }

    public List<SeoHubLink> SeoHubLinks { get; set; } = [];

    public int TotalCount { get; set; }

    public int FilteredCount { get; set; }
}

public sealed class SeoLandingContent
{
    public string Eyebrow { get; set; } = string.Empty;

    public string Heading { get; set; } = string.Empty;

    public string Intro { get; set; } = string.Empty;

    public string BodyTitle { get; set; } = string.Empty;

    public string BodyText { get; set; } = string.Empty;

    public string SecondaryText { get; set; } = string.Empty;

    public List<string> Highlights { get; set; } = [];

    public List<SeoLandingFaqItem> FaqItems { get; set; } = [];

    public List<SeoHubLink> RelatedLinks { get; set; } = [];
}

public sealed class SeoHubLink
{
    public string Badge { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}

public sealed class SeoLandingFaqItem
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;
}

public sealed class RegionalCampaign
{
    public string City { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string DiscountLabel { get; set; } = string.Empty;
    public List<PropertyCard> Listings { get; set; } = [];
}

public sealed class SubCategoryFilter
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class SearchTabOption
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ListingType { get; set; } = string.Empty;
    public string PresetCategory { get; set; } = "all";
    public bool IsActive { get; set; }
}
