using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SEN_T_PAZAR.Controllers;
using SEN_T_PAZAR.Models;
using Xunit;

namespace SenTPazar.Tests;

public class ApiShowcaseControllerTests
{
    [Fact]
    public async Task GetShowcase_ReturnsOnlyActiveSponsoredListings()
    {
        using var harness = new TestHarness();
        var now = DateTime.UtcNow;

        harness.Db.Listings.AddRange(
            new Listing
            {
                Id = 1,
                Title = "Active Featured",
                Category = "realestate",
                IsApproved = true,
                IsClosed = false,
                IsFeatured = true,
                FeaturedExpiryDate = now.AddDays(2),
                CreatedAt = now.AddMinutes(-10)
            },
            new Listing
            {
                Id = 2,
                Title = "Expired Featured",
                Category = "realestate",
                IsApproved = true,
                IsClosed = false,
                IsFeatured = true,
                FeaturedExpiryDate = now.AddDays(-1),
                CreatedAt = now.AddMinutes(-5)
            },
            new Listing
            {
                Id = 3,
                Title = "Active Vitrin",
                Category = "realestate",
                IsApproved = true,
                IsClosed = false,
                IsVitrin = true,
                VitrinExpiryDate = now.AddDays(1),
                CreatedAt = now.AddMinutes(-1)
            },
            new Listing
            {
                Id = 4,
                Title = "Normal Listing",
                Category = "realestate",
                IsApproved = true,
                IsClosed = false,
                CreatedAt = now
            });

        await harness.Db.SaveChangesAsync();

        var controller = new ApiShowcaseController(harness.Db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Request.Scheme = "https";
        controller.HttpContext.Request.Host = new HostString("sen-t.com");

        var action = await controller.GetShowcase();
        var ok = Assert.IsType<OkObjectResult>(action);
        var items = Assert.IsAssignableFrom<List<MobileAdListItemDto>>(ok.Value);

        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.Id == 1);
        Assert.Contains(items, x => x.Id == 3);
        Assert.DoesNotContain(items, x => x.Id == 2 || x.Id == 4);
    }
}
