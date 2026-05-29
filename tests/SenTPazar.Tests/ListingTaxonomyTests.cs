using SEN_T_PAZAR.Models;
using Xunit;

namespace SenTPazar.Tests;

public class ListingTaxonomyTests
{
    [Theory]
    [InlineData("cocuk-odasi", "Çocuk Odası")]
    [InlineData("guzellik-bakim", "Güzellik & Bakım")]
    [InlineData("lastik-jant", "Lastik & Jant")]
    public void HumanizeSubCategory_ReturnsTurkishLabels(string rawValue, string expected)
    {
        Assert.Equal(expected, ListingTaxonomy.HumanizeSubCategory(rawValue));
    }

    [Fact]
    public void GetSearchSubCategoryFilters_RealEstate_IncludesLandSubCategory()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("realestate");

        Assert.Contains(filters, x => x.Value == "konut" && x.Label == "Konut");
        Assert.Contains(filters, x => x.Value == "ticari" && x.Label == "Ticari");
        Assert.Contains(filters, x => x.Value == "arsa" && x.Label == "Arsa");
    }

    [Fact]
    public void MatchesSearchCategory_TreatsLandAsRealEstateSubCategory()
    {
        Assert.True(ListingTaxonomy.MatchesSearchCategory("realestate", "estate", "arsa"));
        Assert.True(ListingTaxonomy.MatchesSearchCategory("land", "estate", "arsa"));
    }

    [Fact]
    public void MatchesSearchCategory_MovesPartsFromElectronicsToVehicle()
    {
        Assert.True(ListingTaxonomy.MatchesSearchCategory("vehicle", "parts", "lastik-jant"));
        Assert.False(ListingTaxonomy.MatchesSearchCategory("electronics", "parts", "lastik-jant"));
    }

    [Fact]
    public void GetSearchSubCategoryFilters_Vehicle_UsesVehicleRelatedSubCategories()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("vehicle");

        Assert.Contains(filters, x => x.Value == "araba" && x.Label == "Araba");
        Assert.Contains(filters, x => x.Value == "suv" && x.Label == "SUV");
        Assert.Contains(filters, x => x.Value == "ticari-arac" && x.Label == "Ticari Araç");
        Assert.DoesNotContain(filters, x => x.Value == "lastik-jant");
    }

    [Fact]
    public void GetSearchSubCategoryFilters_Vehicle_IncludesYachtAndCaravan()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("vehicle");

        Assert.Contains(filters, x => x.Value == "yat" && x.Label == "Yat / Tekne");
        Assert.Contains(filters, x => x.Value == "karavan" && x.Label == "Karavan");
    }

    [Fact]
    public void GetSearchSubCategoryFilters_RealEstate_HidesSeasonalAndStudentOptions()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("realestate");

        Assert.Contains(filters, x => x.Value == "konut" && x.Label == "Konut");
        Assert.Contains(filters, x => x.Value == "ticari" && x.Label == "Ticari");
        Assert.Contains(filters, x => x.Value == "arsa" && x.Label == "Arsa");
        Assert.Contains(filters, x => x.Value == "gunluk-kiralik" && x.Label == "Günlük Kiralık");
        Assert.DoesNotContain(filters, x => x.Value == "yazlik");
        Assert.DoesNotContain(filters, x => x.Value == "ogrenci-evleri");
        Assert.DoesNotContain(filters, x => x.Value == "tatil-evleri");
    }

    [Fact]
    public void GetSearchSubCategoryFilters_Secondhand_DoesNotExposePetTypes()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("secondhand");

        Assert.DoesNotContain(filters, x => x.Value == "kedi");
        Assert.Contains(filters, x => x.Value == "ev-yasam" && x.Label == "Ev Yaşam");
    }

    [Fact]
    public void GetSearchSubCategoryFilters_Home_IncludesHomeLivingOptions()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("home");

        Assert.Contains(filters, x => x.Value == "cocuk-odasi" && x.Label == "Çocuk Odası");
        Assert.Contains(filters, x => x.Value == "ev-yasam" && x.Label == "Ev Yaşam");
    }

    [Fact]
    public void GetSearchCategoryKeys_UsesParentSearchCategoriesOnly()
    {
        var categories = ListingTaxonomy.GetSearchCategoryKeys();

        Assert.DoesNotContain("land", categories);
        Assert.DoesNotContain("helper", categories);
        Assert.Contains("electronics", categories);
        Assert.Contains("fashion", categories);
        Assert.Contains("pets", categories);
        Assert.DoesNotContain("phone", categories);
        Assert.DoesNotContain("computer", categories);
        Assert.DoesNotContain("watch", categories);
        Assert.DoesNotContain("jewelry", categories);
    }

    [Fact]
    public void GetSearchSubCategoryFilters_Helper_UsesHelperSpecificOptions()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("helper");

        Assert.Contains(filters, x => x.Value == "ev-yardimcisi" && x.Label == "Ev Yardımcısı");
        Assert.Contains(filters, x => x.Value == "yatili-yardimci" && x.Label == "Yatılı Yardımcı");
        Assert.DoesNotContain(filters, x => x.Value == "matematik");
    }

    [Fact]
    public void MatchesSearchCategory_SeparatesServiceFamilyCategories()
    {
        Assert.True(ListingTaxonomy.MatchesSearchCategory("services", "services", "temizlik"));
        Assert.False(ListingTaxonomy.MatchesSearchCategory("services", "helper", "ev-yardimcisi"));
        Assert.True(ListingTaxonomy.MatchesSearchCategory("jobs", "jobs", "tam-zamanli"));
        Assert.False(ListingTaxonomy.MatchesSearchCategory("jobs", "helper", "bakici"));
        Assert.True(ListingTaxonomy.MatchesSearchCategory("helper", "helper", "bakici"));
        Assert.False(ListingTaxonomy.MatchesSearchCategory("helper", "tutoring", "matematik"));
    }

    [Fact]
    public void MatchesSearchCategory_GroupsElectronicAndFashionLeafCategoriesUnderLogicalParents()
    {
        Assert.True(ListingTaxonomy.MatchesSearchCategory("electronics", "phone", null));
        Assert.True(ListingTaxonomy.MatchesSearchCategory("electronics", "computer", null));
        Assert.False(ListingTaxonomy.MatchesSearchCategory("electronics", "watch", null));
        Assert.False(ListingTaxonomy.MatchesSearchCategory("electronics", "jewelry", null));

        Assert.True(ListingTaxonomy.MatchesSearchCategory("fashion", "watch", null));
        Assert.True(ListingTaxonomy.MatchesSearchCategory("fashion", "jewelry", null));
    }

    [Fact]
    public void GetCategoryTranslationKey_PreservesSpecificSearchLeafLabels()
    {
        Assert.Equal("cat_phone", ListingTaxonomy.GetCategoryTranslationKey("phone"));
        Assert.Equal("cat_computer", ListingTaxonomy.GetCategoryTranslationKey("computer"));
        Assert.Equal("cat_watch", ListingTaxonomy.GetCategoryTranslationKey("watch"));
        Assert.Equal("cat_jewelry", ListingTaxonomy.GetCategoryTranslationKey("jewelry"));
    }

    [Fact]
    public void MatchesSearchSubCategory_UsesGroupedLeafMappings()
    {
        Assert.True(ListingTaxonomy.MatchesSearchSubCategory("vehicle", ["araba"], "vehicle", null));
        Assert.True(ListingTaxonomy.MatchesSearchSubCategory("electronics", ["telefon"], "phone", null));
        Assert.True(ListingTaxonomy.MatchesSearchSubCategory("electronics", ["bilgisayar"], "computer", null));
        Assert.True(ListingTaxonomy.MatchesSearchSubCategory("fashion", ["saat"], "watch", null));
        Assert.True(ListingTaxonomy.MatchesSearchSubCategory("fashion", ["mucevher"], "jewelry", null));
    }

    [Fact]
    public void NormalizeForPersistence_DefaultsVehicleSubCategoryToAraba()
    {
        var normalized = ListingTaxonomy.NormalizeForPersistence("vehicle", null);

        Assert.Equal("vehicle", normalized.Category);
        Assert.Equal("araba", normalized.SubCategory);
    }

    [Fact]
    public void NormalizeForPersistence_ClearsInvalidVehicleSubCategory()
    {
        var normalized = ListingTaxonomy.NormalizeForPersistence("vehicle", "telefon");

        Assert.Equal("vehicle", normalized.Category);
        Assert.Null(normalized.SubCategory);
    }

    [Fact]
    public void NormalizeForPersistence_ClearsInvalidEstateSubCategory()
    {
        var normalized = ListingTaxonomy.NormalizeForPersistence("estate", "araba");

        Assert.Equal("estate", normalized.Category);
        Assert.Null(normalized.SubCategory);
    }

    [Fact]
    public void GetSearchSubCategoryFilters_Fashion_DoesNotInjectUnrelatedOptions()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("fashion");

        Assert.Contains(filters, x => x.Value == "saat" && x.Label == "Saat");
        Assert.Contains(filters, x => x.Value == "mucevher" && x.Label == "Mücevher");
    }

    [Fact]
    public void GetSearchSubCategoryFilters_Pets_UsesPetSubCategories()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("pets");

        Assert.Contains(filters, x => x.Value == "kedi" && x.Label == "Kedi");
        Assert.Contains(filters, x => x.Value == "kopek" && x.Label == "Köpek");
        Assert.DoesNotContain(filters, x => x.Value == "hobi");
    }

    [Fact]
    public void GetSearchSubCategoryFilters_Adoption_UsesPetSubCategories()
    {
        var filters = ListingTaxonomy.GetSearchSubCategoryFilters("adoption");

        Assert.Contains(filters, x => x.Value == "kedi" && x.Label == "Kedi");
        Assert.Contains(filters, x => x.Value == "kopek" && x.Label == "Köpek");
        Assert.DoesNotContain(filters, x => x.Value == "hobi");
    }
}