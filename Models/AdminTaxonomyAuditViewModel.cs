namespace SEN_T_PAZAR.Models;

public sealed class AdminTaxonomyAuditViewModel
{
    public int TotalListings { get; set; }

    public int DriftedListingCount { get; set; }

    public int InvalidCategoryCount { get; set; }

    public int InvalidSubCategoryCount { get; set; }

    public int InvalidTypeCount { get; set; }

    public List<AdminTaxonomyAuditCategoryRow> Categories { get; set; } = [];

    public List<AdminTaxonomyAuditSubCategoryRow> SubCategories { get; set; } = [];

    public List<AdminTaxonomyAuditTypeRow> ListingTypes { get; set; } = [];

    public List<AdminTaxonomyAuditListingIssueRow> ListingIssues { get; set; } = [];
}

public sealed class AdminTaxonomyAuditCategoryRow
{
    public string RawCategory { get; set; } = string.Empty;

    public string NormalizedCategory { get; set; } = string.Empty;

    public int Count { get; set; }

    public bool IsKnown { get; set; }

    public bool HasDrift { get; set; }
}

public sealed class AdminTaxonomyAuditSubCategoryRow
{
    public string RawCategory { get; set; } = string.Empty;

    public string RawSubCategory { get; set; } = string.Empty;

    public string NormalizedCategory { get; set; } = string.Empty;

    public string NormalizedSubCategory { get; set; } = string.Empty;

    public int Count { get; set; }

    public bool IsValid { get; set; }

    public bool HasDrift { get; set; }
}

public sealed class AdminTaxonomyAuditTypeRow
{
    public string RawCategory { get; set; } = string.Empty;

    public string RawType { get; set; } = string.Empty;

    public string NormalizedCategory { get; set; } = string.Empty;

    public string NormalizedType { get; set; } = string.Empty;

    public int Count { get; set; }

    public bool IsValid { get; set; }

    public bool HasDrift { get; set; }
}

public sealed class AdminTaxonomyAuditListingIssueRow
{
    public int ListingId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string RawCategory { get; set; } = string.Empty;

    public string RawSubCategory { get; set; } = string.Empty;

    public string RawType { get; set; } = string.Empty;

    public string NormalizedCategory { get; set; } = string.Empty;

    public string NormalizedSubCategory { get; set; } = string.Empty;

    public string NormalizedType { get; set; } = string.Empty;

    public bool HasCategoryIssue { get; set; }

    public bool HasSubCategoryIssue { get; set; }

    public bool HasTypeIssue { get; set; }

    public bool HasDrift { get; set; }

    public List<string> Issues { get; set; } = [];
}