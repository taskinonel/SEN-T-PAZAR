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
    [Route("api/v1/[controller]")]
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "Kullanıcı oturumu bulunamadı." });

                // İlanın var olup olmadığını kontrol et
                var listing = await _context.Listings.FindAsync(listingId);
                if (listing == null)
                    return NotFound(new { success = false, message = "İlan bulunamadı." });

                var existingFavorites = await _context.UserFavorites
                    .Where(f => f.UserId == userId && f.ListingId == listingId)
                    .OrderBy(f => f.Id)
                    .ToListAsync();

                if (existingFavorites.Count > 0)
                {
                    if (existingFavorites.Count > 1)
                    {
                        _context.UserFavorites.RemoveRange(existingFavorites.Skip(1));
                        await _context.SaveChangesAsync();
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "İlan zaten favorilerde.",
                        isFavorite = true
                    });
                }

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
                    message = "İlan favorilere eklendi.",
                    isFavorite = true
                });
            }
            catch (DbUpdateException)
            {
                return Ok(new
                {
                    success = true,
                    message = "İlan zaten favorilerde.",
                    isFavorite = true
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

        [HttpPost("Add")]
        public async Task<IActionResult> AddFavoriteFromBody([FromBody] FavoriteAddRequest request)
        {
            if (request == null || request.ListingId <= 0)
            {
                return BadRequest(new { success = false, message = "Geçerli bir listingId gönderin." });
            }

            return await AddFavorite(request.ListingId);
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "Kullanıcı oturumu bulunamadı." });

                var favorites = await _context.UserFavorites
                    .Where(f => f.UserId == userId && f.ListingId == listingId)
                    .ToListAsync();

                if (favorites.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "İlan zaten favorilerde değil.",
                        isFavorite = false
                    });
                }

                _context.UserFavorites.RemoveRange(favorites);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "İlan favorilerden çıkarıldı.",
                    isFavorite = false,
                    removedCount = favorites.Count
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Kullanıcı oturumu bulunamadı.");

                var skipCount = (page - 1) * pageSize;

                var favorites = await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip(skipCount)
                    .Take(pageSize)
                    .Include(f => f.Listing)
                    .Where(f => f.Listing != null)
                    .Select(f => new
                    {
                        Id = f.Listing!.Id,
                        Title = f.Listing.Title,
                        Price = f.Listing.PriceAmount,
                        Category = f.Listing.Category,
                        City = f.Listing.City,
                        AddedAt = f.CreatedAt
                    })
                    .ToListAsync();

                var totalCount = await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.ListingId)
                    .Distinct()
                    .CountAsync();

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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Kullanıcı oturumu bulunamadı.");

                var count = await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.ListingId)
                    .Distinct()
                    .CountAsync();

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

    public sealed class FavoriteAddRequest
    {
        public int ListingId { get; set; }
    }
}
