namespace SEN_T_PAZAR.Models;

public sealed class HomePageViewModel
{
    public List<ProjectCard> FeaturedEmlak { get; set; } = new();
    public List<ProjectCard> FeaturedVasita { get; set; } = new();
    public List<ProjectCard> FeaturedElektronik { get; set; } = new();
    public List<ProjectCard> FeaturedEvEsya { get; set; } = new();
    public List<ProjectCard> FeaturedHizmet { get; set; } = new();
    public string HeroTitle { get; set; } = string.Empty;

    public string HeroSubtitle { get; set; } = string.Empty;

    public List<PropertyCard> FeaturedListings { get; set; } = [];

    public List<string> PopularRegions { get; set; } = [];

    public List<RegionSpot> RegionSpots { get; set; } = [];

    public List<ProjectCard> FeaturedProjects { get; set; } = [];

    public List<string> PartnerNames { get; set; } = [];

    public List<MarketCategory> MarketCategories { get; set; } = [];

    public List<MarketTile> MarketTiles { get; set; } = [];

    public List<string> ListingTypeOptions { get; set; } = [];

    public List<string> CityOptions { get; set; } = [];

    public List<string> CategoryOptions { get; set; } = [];

    public List<string> PriceRangeOptions { get; set; } = [];

    public List<string> SortOptions { get; set; } = [];

    public string ListingType { get; set; } = "all";

    public string City { get; set; } = "all";

    public string Category { get; set; } = "all";

    public string PriceRange { get; set; } = "any";

    public string Keyword { get; set; } = string.Empty;

    public string SortBy { get; set; } = "latest";

    public bool IsCategoryPage { get; set; }

    public string CurrentCategorySlug { get; set; } = string.Empty;

    public string CategoryHeroImage { get; set; } = string.Empty;

    public int TotalCount { get; set; }

    public int FilteredCount { get; set; }
}
