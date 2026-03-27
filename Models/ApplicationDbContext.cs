using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SEN_T_PAZAR.Models;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Listing> Listings { get; set; }
    public DbSet<ListingImage> ListingImages { get; set; }
    public DbSet<PricingPackage> PricingPackages { get; set; }
    public DbSet<UserPackage> UserPackages { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<ListingPromotion> ListingPromotions { get; set; }
}
