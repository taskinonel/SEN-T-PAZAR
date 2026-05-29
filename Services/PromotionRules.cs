namespace SEN_T_PAZAR.Services;

public static class PromotionRules
{
    public static bool IsFeaturedActive(bool isFeatured, DateTime? featuredExpiryDateUtc, DateTime utcNow)
    {
        return isFeatured && (featuredExpiryDateUtc == null || featuredExpiryDateUtc > utcNow);
    }

    public static bool IsVitrinActive(bool isVitrin, DateTime? vitrinExpiryDateUtc, DateTime utcNow)
    {
        return isVitrin && (vitrinExpiryDateUtc == null || vitrinExpiryDateUtc > utcNow);
    }

    public static bool IsShowcase(bool isFeatured, DateTime? featuredExpiryDateUtc, bool isVitrin, DateTime? vitrinExpiryDateUtc, DateTime utcNow)
    {
        return IsFeaturedActive(isFeatured, featuredExpiryDateUtc, utcNow)
            || IsVitrinActive(isVitrin, vitrinExpiryDateUtc, utcNow);
    }
}
