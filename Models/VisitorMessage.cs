using System.ComponentModel.DataAnnotations;

namespace SEN_T_PAZAR.Models;

public class VisitorMessage
{
    [Key]
    public int Id { get; set; }

    public int ListingId { get; set; }
    [StringLength(64)]
    public string ConversationId { get; set; } = string.Empty;
    public string? RecipientUserId { get; set; }
    public string? RecipientPhone { get; set; }
    [EmailAddress]
    [StringLength(180)]
    public string? RecipientEmail { get; set; }

    public string? SenderUserId { get; set; }

    [Required]
    [StringLength(120)]
    public string SenderName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(180)]
    public string SenderEmail { get; set; } = string.Empty;

    [StringLength(40)]
    public string? SenderPhone { get; set; }

    [Required]
    [StringLength(180)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public bool IsArchived { get; set; } = false;

    [Required]
    [StringLength(20)]
    public string SenderRole { get; set; } = "visitor";
}
