using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEN_T_PAZAR.Models;

public class Review
{
    [Key]
    public int Id { get; set; }
    
    public int ListingId { get; set; }
    public Listing? Listing { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    
    [Range(1, 5)]
    public int Rating { get; set; }
    
    public string Comment { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsApproved { get; set; } = false;
}