using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEN_T_PAZAR.Models;

public class Document
{
    [Key]
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DocumentType DocumentType { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
}

public enum DocumentType
{
    Invoice,
    Contract,
    Warranty
}