using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Models;

public class AdminDashboardViewModel
{
    public int TotalListings { get; set; }
    public int PendingListings { get; set; }
    public int ApprovedListings { get; set; }
    public int FeaturedListings { get; set; }
    public int VitrinListings { get; set; }
    public int TotalUsers { get; set; }
    public int CorporateUsers { get; set; }
    public int PendingCorporate { get; set; }
    public List<Listing> RecentListings { get; set; } = new();
}
