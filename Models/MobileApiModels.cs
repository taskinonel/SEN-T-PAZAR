using System.ComponentModel.DataAnnotations;

namespace SEN_T_PAZAR.Models;

public sealed class MobileRegisterRequest
{
    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
}

public sealed class MobileLoginRequest
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class MobileDeviceTokenRequest
{
    [Required]
    [StringLength(512)]
    public string FcmToken { get; set; } = string.Empty;
}

public sealed class MobileAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public MobileUserDto User { get; set; } = new();
}

public sealed class MobileUserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class MobileAdListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsSponsored { get; set; }
}

public sealed class MobileAdDetailsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string PriceCurrency { get; set; } = "TL";
    public string Category { get; set; } = string.Empty;
    public string ListingType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsSponsored { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public MobileAdSellerDto Seller { get; set; } = new();
}

public sealed class MobileAdSellerDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool AllowWhatsApp { get; set; }
    public bool AllowMessages { get; set; }
}

public sealed class MobileCategoryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public sealed class MobileLocationDto
{
    public string City { get; set; } = string.Empty;
    public List<string> Districts { get; set; } = new();
}

public sealed class MobileProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public bool EmailNotifications { get; set; }
}

public sealed class MobileMyAdDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string PriceCurrency { get; set; } = "TL";
    public string Category { get; set; } = string.Empty;
    public string ListingType { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAtUtc { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public sealed class MobileMessageThreadDto
{
    public string ConversationId { get; set; } = string.Empty;
    public int RootMessageId { get; set; }
    public int ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string? SenderPhone { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<MobileMessageEntryDto> Messages { get; set; } = new();
}

public sealed class MobileMessageEntryDto
{
    public int Id { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class MobileReplyMessageRequest
{
    [Required]
    public int MessageId { get; set; }

    [Required]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;
}
