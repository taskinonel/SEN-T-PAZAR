using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SEN_T_PAZAR.Models;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Listing> Listings { get; set; }
    public DbSet<ListingImage> ListingImages { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Document> Documents { get; set; }

    // Kullan�c� Etkile�imleri
    public DbSet<UserFavorite> UserFavorites { get; set; }
    
    // Fiyatland�rma ve �deme
    public DbSet<PricingPackage> PricingPackages { get; set; }
    public DbSet<UserPackage> UserPackages { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<ListingPromotion> ListingPromotions { get; set; }
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
    public DbSet<VisitorMessage> VisitorMessages { get; set; }
    public DbSet<ListingMessage> ListingMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserFavorite>()
            .HasIndex(x => new { x.UserId, x.ListingId })
            .IsUnique();

        builder.Entity<ListingImage>()
            .Property(x => x.Id)
            .UseIdentityColumn();

        builder.Entity<Review>()
            .HasIndex(x => new { x.UserId, x.ListingId })
            .IsUnique();
    }
}
