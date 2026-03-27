namespace SEN_T_PAZAR.Models;

public sealed class MarketTile
{
    public int ListingId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string PriceLabel { get; set; } = string.Empty;
}
