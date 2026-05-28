using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;
using System.Security.Claims;

namespace SEN_T_PAZAR.Controllers
{
    /// <summary>
    /// Favoriler API endpoint'leri
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// İlanı favorilere ekle
        /// </summary>
        /// <param name="listingId">İlan ID'si</param>
        /// <returns>Başarı durumu</returns>
        [HttpPost("{listingId}")]
        public async Task<IActionResult> AddFavorite(int listingId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Kullanıcı oturumu bulunamadı.");

                // İlanın var olup olmadığını kontrol et
                var listing = await _context.Listings.FindAsync(listingId);
                if (listing == null)
                    return NotFound("İlan bulunamadı.");

                // Zaten favoride mi kontrol et
                var existing = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ListingId == listingId);

                if (existing != null)
                    return BadRequest("Bu ilan zaten favorilerde.");

                // Favoriye ekle
                var favorite = new UserFavorite
                {
                    UserId = userId,
                    ListingId = listingId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserFavorites.Add(favorite);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "İlan favorilere eklendi."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Hata oluştu: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// İlanı favorilerden çıkar
        /// </summary>
        /// <param name="listingId">İlan ID'si</param>
        /// <returns>Başarı durumu</returns>
        [HttpDelete("{listingId}")]
        public async Task<IActionResult> RemoveFavorite(int listingId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Kullanıcı oturumu bulunamadı.");

                var favorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ListingId == listingId);

                if (favorite == null)
                    return NotFound("Favori bulunamadı.");

                _context.UserFavorites.Remove(favorite);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "İlan favorilerden çıkarıldı."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Hata oluştu: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Kullanıcının tüm favorilerini getir
        /// </summary>
        /// <param name="page">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="pageSize">Sayfa seçisi (varsayılan: 10)</param>
        /// <returns>Favori ilanlar listesi</returns>
        [HttpGet]
        public async Task<IActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Kullanıcı oturumu bulunamadı.");

                var skipCount = (page - 1) * pageSize;

                var favorites = await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip(skipCount)
                    .Take(pageSize)
                    .Include(f => f.Listing)
                    .Select(f => new
                    {
                        f.Listing.Id,
                        f.Listing.Title,
                        Price = f.Listing.PriceAmount,
                        f.Listing.Category,
                        f.Listing.City,
                        AddedAt = f.CreatedAt
                    })
                    .ToListAsync();

                var totalCount = await _context.UserFavorites
                    .CountAsync(f => f.UserId == userId);

                return Ok(new
                {
                    success = true,
                    data = favorites,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Hata oluştu: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// İlanın favoride olup olmadığını kontrol et
        /// </summary>
        /// <param name="listingId">İlan ID'si</param>
        /// <returns>true/false</returns>
        [HttpGet("{listingId}/is-favorite")]
        public async Task<IActionResult> IsFavorite(int listingId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Kullanıcı oturumu bulunamadı.");

                var isFavorite = await _context.UserFavorites
                    .AnyAsync(f => f.UserId == userId && f.ListingId == listingId);

                return Ok(new
                {
                    success = true,
                    isFavorite = isFavorite
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Hata oluştu: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Tüm favori sayısını getir
        /// </summary>
        /// <returns>Favorilerin sayısı</returns>
        [HttpGet("count")]
        public async Task<IActionResult> GetFavoriteCount()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Kullanıcı oturumu bulunamadı.");

                var count = await _context.UserFavorites
                    .CountAsync(f => f.UserId == userId);

                return Ok(new
                {
                    success = true,
                    count = count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Hata oluştu: {ex.Message}"
                });
            }
        }
    }
}
