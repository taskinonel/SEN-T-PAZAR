using System.ComponentModel.DataAnnotations;
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

public class HomeControllerPublishTests
{
    [Fact]
    public async Task Publish_JobsListing_ClearsIrrelevantProductFieldsBeforeSave()
    {
        using var harness = new TestHarness();

        var controller = CreateController(harness);
        var model = BuildValidPublishModel();
        model.Category = "jobs";
        model.SubCategory = "tam-zamanli";
        model.Type = "job";
        model.ProductBrand = "Kuzey Lojistik";
        model.ProductModel = "Depo Operasyon Sorumlusu";
        model.WarrantyPeriod = "Hafta ici 09:00-18:00";
        model.SerialNumber = "REF-445";
        model.UsageDuration = "6 ay";
        model.ProductCondition = ConditionState.Good;

        var action = await controller.Publish(model);

        var redirect = Assert.IsType<RedirectToActionResult>(action);
        Assert.Equal("Publish", redirect.ActionName);

        var listing = Assert.Single(harness.Db.Listings);
        Assert.Equal("jobs", listing.Category);
        Assert.Equal("tam-zamanli", listing.SubCategory);
        Assert.Equal("job", listing.Type);
        Assert.Equal("Kuzey Lojistik", listing.ProductBrand);
        Assert.Equal("Depo Operasyon Sorumlusu", listing.ProductModel);
        Assert.Equal("Hafta ici 09:00-18:00", listing.WarrantyPeriod);
        Assert.Null(listing.SerialNumber);
        Assert.Null(listing.UsageDuration);
        Assert.Null(listing.ProductCondition);
    }

    [Fact]
    public async Task Publish_InvalidEstateModel_ReturnsViewWithPublishMaps()
    {
        using var harness = new TestHarness();

        harness.Db.Listings.AddRange(
            new Listing { Category = "vehicle", SubCategory = "araba", VehicleBrand = "BMW", Title = "BMW", Description = "test", City = "Girne", District = "Merkez", Type = "sale" },
            new Listing { Category = "vehicle", SubCategory = "araba", VehicleBrand = "Audi", Title = "Audi", Description = "test", City = "Girne", District = "Merkez", Type = "sale" },
            new Listing { Category = "vehicle", SubCategory = "araba", VehicleBrand = "bmw", Title = "BMW 2", Description = "test", City = "Girne", District = "Merkez", Type = "sale" });
        await harness.Db.SaveChangesAsync();

        var controller = CreateController(harness);
        var model = BuildValidPublishModel();
        model.Category = "estate";
        model.SubCategory = "konut";
        model.Type = "sale";
        model.EstateNetArea = null;

        ValidateModel(controller, model);

        var action = await controller.Publish(model);

        var view = Assert.IsType<ViewResult>(action);
        var returnedModel = Assert.IsType<CreateListingViewModel>(view.Model);
        Assert.Equal("estate", returnedModel.Category);
        Assert.Equal("konut", returnedModel.SubCategory);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(CreateListingViewModel.EstateNetArea)));
        Assert.NotNull(view.ViewData["PublishSubCategoryValueMap"]);
        Assert.NotNull(view.ViewData["PublishTypeValueMap"]);
        Assert.NotNull(view.ViewData["PublishProductFieldVisibilityMap"]);
        var brandOptions = Assert.IsType<List<string>>(view.ViewData["VehicleBrandOptions"]);
        Assert.Equal(new[] { "Audi", "BMW" }, brandOptions);
        Assert.Equal(3, harness.Db.Listings.Count());
    }

    private static HomeController CreateController(TestHarness harness)
    {
        var httpContext = new DefaultHttpContext();
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

    private static CreateListingViewModel BuildValidPublishModel()
    {
        return new CreateListingViewModel
        {
            FullName = "Test Kullanici",
            Phone = "+90 555 111 22 33",
            Title = "Yayin testi icin ornek ilan",
            Description = "Bu aciklama publish testi icin yeterince uzun ve gecerli bir ornek metindir.",
City = "Girne",
             Neighborhood = "Karakum",
            Address = "Test adresi 10",
            Category = "secondhand",
            SubCategory = "ikinci-el",
            Type = "sale",
            PriceAmount = 1500,
            PriceCurrency = "TL",
            PriceType = PriceType.Total,
            AdvertiserType = AdvertiserType.Owner,
            ProductCondition = ConditionState.Good
        };
    }

    private static void ValidateModel(Controller controller, object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        foreach (var result in results)
        {
            var memberNames = result.MemberNames.Any() ? result.MemberNames : new[] { string.Empty };
            foreach (var memberName in memberNames)
            {
                controller.ModelState.AddModelError(memberName, result.ErrorMessage ?? "Validation error");
            }
        }
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