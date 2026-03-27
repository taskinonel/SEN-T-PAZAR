using Microsoft.AspNetCore.Identity;

namespace SEN_T_PAZAR.Models;

public class ApplicationUser : IdentityUser
{
    // Temel bilgiler
    public string FullName { get; set; } = string.Empty;
    
    // Adres bilgileri
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    
    // Profil bilgileri
    public string? AvatarUrl { get; set; }
    
    // Bildirim ayarları
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
}
