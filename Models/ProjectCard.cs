namespace SEN_T_PAZAR.Models;

public sealed class ProjectCard
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string DeliveryDate { get; set; } = string.Empty;

    public string PriceFrom { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
