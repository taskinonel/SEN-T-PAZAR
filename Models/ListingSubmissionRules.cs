using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SEN_T_PAZAR.Models;

public static class ListingSubmissionRules
{
    private static readonly Regex PlaceholderTextPattern = new(
        @"(^|\b)(ornek|sample|demo|dummy|placeholder|coming soon|cok yakinda|test amac|test amacli|deneme|lorem ipsum|fake|taslak)(\b|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IEnumerable<ValidationResult> ValidateCoreFields(
        string title,
        string description,
        string category,
        string? subCategory,
        string type,
        string city)
    {
        if (ContainsPlaceholderLikeText(title))
        {
            yield return new ValidationResult("İlan başlığında örnek, demo veya test ifadeleri kullanılamaz.", new[] { "Title" });
        }

        if (ContainsPlaceholderLikeText(description))
        {
            yield return new ValidationResult("İlan açıklamasında örnek, demo veya test ifadeleri kullanılamaz.", new[] { "Description" });
        }

        if (ContainsPlaceholderLikeText(city))
        {
            yield return new ValidationResult("Konum alanlarında geçici veya örnek ifade kullanılamaz.", new[] { "City" });
        }

        var normalized = ListingTaxonomy.NormalizeForPersistence(category, subCategory);
        var normalizedCategory = normalized.Category;
        var normalizedSubCategory = normalized.SubCategory;

        if (string.IsNullOrWhiteSpace(normalizedCategory))
        {
            yield return new ValidationResult("Kategori seçimi zorunludur.", new[] { "Category" });
            yield break;
        }

        if (!ListingTaxonomy.IsValidListingType(normalizedCategory, type))
        {
            yield return new ValidationResult("Seçilen kategori ile ilan tipi uyuşmuyor.", new[] { "Type" });
        }

        if (ListingTaxonomy.RequiresSubCategory(normalizedCategory))
        {
            if (string.IsNullOrWhiteSpace(normalizedSubCategory))
            {
                yield return new ValidationResult("Seçilen kategori için alt kategori zorunludur.", new[] { "SubCategory" });
            }
            else if (!ListingTaxonomy.IsValidSubCategory(normalizedCategory, normalizedSubCategory))
            {
                yield return new ValidationResult("Alt kategori seçimi ilan kategorisiyle uyuşmuyor.", new[] { "SubCategory" });
            }
        }
    }

    public static bool ContainsPlaceholderLikeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = ListingTaxonomy.NormalizeText(value).Replace('-', ' ');
        return PlaceholderTextPattern.IsMatch(normalized);
    }

    public static bool RequiresDetailedProductIdentity(string? category)
    {
        return ListingTaxonomy.NormalizePublishCategory(category) is "parts" or "electronics" or "furniture" or "equipment";
    }
}