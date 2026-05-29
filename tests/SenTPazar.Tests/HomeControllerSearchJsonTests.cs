using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SEN_T_PAZAR.Controllers;
using SEN_T_PAZAR.Models;
using SEN_T_PAZAR.Services;
using Xunit;

namespace SenTPazar.Tests;

public class HomeControllerSearchJsonTests
{
    [Fact]
    public void Category_YatTekneSlug_FoldsIntoVehicleWithPresetSubCategory()
    {
        using var harness = new TestHarness();

        var controller = CreateController(harness);

        var action = controller.Category("yat-tekne", showResults: true);

        var view = Assert.IsType<ViewResult>(action);
        var model = Assert.IsType<HomePageViewModel>(view.Model);

        Assert.Equal("vehicle", model.Category);
        Assert.Equal("yat", model.SubCategory);
    }

    [Fact]
    public void Category_KaravanSlug_FoldsIntoVehicleWithPresetSubCategory()
    {
        using var harness = new TestHarness();

        var controller = CreateController(harness);

        var action = controller.Category("karavan", showResults: true);

        var view = Assert.IsType<ViewResult>(action);
        var model = Assert.IsType<HomePageViewModel>(view.Model);

        Assert.Equal("vehicle", model.Category);
        Assert.Equal("karavan", model.SubCategory);
    }

    [Fact]
    public void KeywordLanding_SatilikArsa_FoldsLandIntoRealEstateWithPresetSubCategory()
    {
        using var harness = new TestHarness();

        var controller = CreateController(harness);

        var action = controller.KeywordLanding("satilik-arsa");

        var view = Assert.IsType<ViewResult>(action);
        var model = Assert.IsType<HomePageViewModel>(view.Model);

        Assert.Equal("realestate", model.Category);
        Assert.Equal("arsa", model.SubCategory);
        Assert.True(model.ShowResults);
        Assert.NotNull(model.SeoLanding);
    }

    [Fact]
    public void Category_BilgisayarSlug_FoldsComputerIntoElectronicsWithPresetSubCategory()
    {
        using var harness = new TestHarness();

        var controller = CreateController(harness);

        var action = controller.Category("bilgisayar", showResults: true);

        var view = Assert.IsType<ViewResult>(action);
        var model = Assert.IsType<HomePageViewModel>(view.Model);

        Assert.Equal("electronics", model.Category);
        Assert.Equal("bilgisayar", model.SubCategory);
    }

    [Fact]
    public void Category_HelperFlow_HidesHelperFromHomepageSearchJson()
    {
        using var harness = new TestHarness();

        var controller = CreateController(harness);

        var action = controller.Category("yardimci", listingType: "job", showResults: true);

        var view = Assert.IsType<ViewResult>(action);
        var model = Assert.IsType<HomePageViewModel>(view.Model);

        Assert.Contains(model.CategoryOptions, x => x == "tutoring");
        Assert.Contains(model.CategoryOptions, x => x == "jobs");
        Assert.DoesNotContain(model.CategoryOptions, x => x == "helper");

        using var listingTypeJson = JsonDocument.Parse(model.ListingTypeCategoryJson);
        var jobCategories = listingTypeJson.RootElement.GetProperty("job").EnumerateArray().Select(item => item.GetString()).ToList();
        Assert.Contains("jobs", jobCategories);
        Assert.DoesNotContain("helper", jobCategories);

        Assert.DoesNotContain(model.SearchTabs, tab => tab.Key == "helper");
        Assert.Contains(model.SearchTabs, tab => tab.Key == "job" && tab.IsActive);
    }

    [Fact]
    public void Index_SearchJson_FoldsLandUnderRealEstateAndKeepsVehicleOnCar()
    {
        using var harness = new TestHarness();

        harness.Db.Listings.AddRange(
            new Listing { IsApproved = true, IsClosed = false, Category = "vehicle", SubCategory = "araba", VehicleBrand = "Tesla", VehicleModel = "Model 3", Title = "Tesla Model 3", Description = "Gecerli arac ilani aciklamasi", City = "Girne", District = "Merkez", Type = "sale" },
            new Listing { IsApproved = true, IsClosed = false, Category = "vehicle", SubCategory = "araba", VehicleBrand = "BMW", VehicleModel = "X1", Title = "BMW X1", Description = "Gecerli arac ilani aciklamasi", City = "Girne", District = "Merkez", Type = "sale" });
        harness.Db.SaveChanges();

        var controller = CreateController(harness);

        var action = controller.Index(showResults: true);

        var view = Assert.IsType<ViewResult>(action);
        var model = Assert.IsType<HomePageViewModel>(view.Model);

        Assert.DoesNotContain("land", model.CategoryOptions);
        Assert.Contains("vehicle", model.CategoryOptions);
        Assert.Equal(new[] { "BMW", "Tesla" }, model.VehicleBrandOptions);
        Assert.Equal(new[] { "Model 3" }, model.VehicleModelOptionsByBrand["Tesla"]);

        using var categoryJson = JsonDocument.Parse(model.CategorySubCategoryJson);
        var realEstateFilters = categoryJson.RootElement.GetProperty("realestate").EnumerateArray().ToList();
        var vehicleFilters = categoryJson.RootElement.GetProperty("vehicle").EnumerateArray().ToList();

        Assert.Contains(realEstateFilters, item => item.GetProperty("value").GetString() == "arsa");
        Assert.Contains(vehicleFilters, item => item.GetProperty("value").GetString() == "araba");
    }

    [Fact]
    public void Index_SearchJson_HidesEquipmentAndHomeFromSaleAndRent()
    {
        using var harness = new TestHarness();
        var controller = CreateController(harness);

        var action = controller.Index(showResults: true);

        var view = Assert.IsType<ViewResult>(action);
        var model = Assert.IsType<HomePageViewModel>(view.Model);

        using var listingTypeJson = JsonDocument.Parse(model.ListingTypeCategoryJson);
        var saleCategories = listingTypeJson.RootElement.GetProperty("sale").EnumerateArray().Select(item => item.GetString()).ToList();
        var rentCategories = listingTypeJson.RootElement.GetProperty("rent").EnumerateArray().Select(item => item.GetString()).ToList();

        Assert.DoesNotContain("equipment", saleCategories);
        Assert.DoesNotContain("home", saleCategories);
        Assert.DoesNotContain("equipment", rentCategories);
        Assert.DoesNotContain("home", rentCategories);
    }

    private static HomeController CreateController(TestHarness harness)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        var controller = new HomeController(
            NullLogger<HomeController>.Instance,
            new ListingCatalogService(),
            harness.Db,
            new EmailSender("localhost", 25, string.Empty, string.Empty, "noreply@test.local"),
            new SiteLocalizer(new HttpContextAccessor { HttpContext = httpContext }),
            new TestWebHostEnvironment(),
            new EchoTranslationService(),
            harness.UserManager,
            new NullPushNotificationService(),
            new TestUploadStorageService(),
            new NoOpUserMessageAutomationService(),
            new ConfigurationBuilder().Build());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
        return controller;
    }

    private sealed class EchoTranslationService : ITextTranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "auto", CancellationToken cancellationToken = default)
            => Task.FromResult(text);
    }

    private sealed class NullPushNotificationService : IPushNotificationService
    {
        public Task SendToUserAsync(ApplicationUser user, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "SenTPazar.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = Path.GetTempPath();

        public string EnvironmentName { get; set; } = "Development";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}