using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEN_T_PAZAR.Models
{
    /// <summary>
    /// Kullanıcının kaydettiği favorileri temsil eder
    /// </summary>
    public class UserFavorite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Listing))]
        public int ListingId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ApplicationUser? User { get; set; }
        public virtual Listing? Listing { get; set; }
    }
}
