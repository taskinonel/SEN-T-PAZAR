using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SEN_T_PAZAR.Models;
using Xunit;

namespace SenTPazar.Tests;

public class CreateListingViewModelValidationTests
{
    [Fact]
    public void Validate_EstateWithoutGrossArea_DoesNotRequireGrossArea()
    {
        var model = BuildValidEstateModel();
        model.EstateGrossArea = null;

        var results = model.Validate(new ValidationContext(model)).ToList();

        Assert.DoesNotContain(results, x => x.MemberNames.Contains(nameof(CreateListingViewModel.EstateGrossArea)));
    }

    [Fact]
    public void Validate_EstateGrossAreaLowerThanNetArea_ReturnsValidationError()
    {
        var model = BuildValidEstateModel();
        model.EstateGrossArea = 110;
        model.EstateNetArea = 120;

        var results = model.Validate(new ValidationContext(model)).ToList();

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(CreateListingViewModel.EstateGrossArea)));
    }

    private static CreateListingViewModel BuildValidEstateModel()
    {
        return new CreateListingViewModel
        {
            FullName = "Test Kullanici",
            Phone = "+90 555 111 22 33",
            Title = "Satilik 2+1 daire",
            Description = "Merkezi konumda, ulasima yakin, temiz kullanilmis ve hemen teslim daire ilani ornegi.",
City = "Girne",
             Category = "estate",
            SubCategory = "konut",
            Type = "sale",
            PriceAmount = 100000,
            PriceCurrency = "TL",
            PriceType = PriceType.Total,
            AdvertiserType = AdvertiserType.Owner,
            EstateNetArea = 120,
            EstateGrossArea = 140,
            EstateRoomCount = EstateRoomCount.TwoOne,
            ImageFiles = new List<IFormFile> { CreateFakeImageFile() }
        };
    }

    private static IFormFile CreateFakeImageFile()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "ImageFiles", "test.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }
}
