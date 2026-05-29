using System.ComponentModel.DataAnnotations;

namespace SEN_T_PAZAR.Models;

public class ListingMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ListingId { get; set; }

    [Required]
    [MaxLength(36)]
    public string SenderUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(36)]
    public string ReceiverUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    public Listing? Listing { get; set; }
    public ApplicationUser? SenderUser { get; set; }
    public ApplicationUser? ReceiverUser { get; set; }
}

public sealed class ListingMessageInboxViewModel
{
    public List<ListingMessageConversationItemViewModel> Conversations { get; set; } = new();
}

public sealed class ListingMessageConversationItemViewModel
{
    public int ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserDisplayName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public bool LastMessageFromCurrentUser { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class ListingMessageThreadViewModel
{
    public int ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserDisplayName { get; set; } = string.Empty;
    public bool CanSendMessages { get; set; }
    public List<ListingMessageThreadItemViewModel> Messages { get; set; } = new();
}

public sealed class ListingMessageThreadItemViewModel
{
    public int Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsMine { get; set; }
}