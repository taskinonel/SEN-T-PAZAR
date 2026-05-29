using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Controllers
{
    /// <summary>
    /// Favoriler sayfası controller'ı
    /// </summary>
    [Authorize]
    public class FavoritesPageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int PAGE_SIZE = 12;

        public FavoritesPageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Favoriler listesi sayfası
        /// </summary>
        [HttpGet("/Account/Favorites")]
        public async Task<IActionResult> Index(int page = 1, string sort = "recent")
        {
            try {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                // Query'ı oluştur - OrderBy include'den BEFORE yapılır
                IQueryable<UserFavorite> query = sort switch
                {
                    "price-low" => _context.UserFavorites
                        .Where(f => f.UserId == userId)
                        .OrderBy(f => f.Listing!.PriceAmount),
                    "price-high" => _context.UserFavorites
                        .Where(f => f.UserId == userId)
                        .OrderByDescending(f => f.Listing!.PriceAmount),
                    "title-az" => _context.UserFavorites
                        .Where(f => f.UserId == userId)
                        .OrderBy(f => f.Listing!.Title),
                    _ => _context.UserFavorites
                        .Where(f => f.UserId == userId)
                        .OrderByDescending(f => f.CreatedAt) // recent (varsayılan)
                };

                query = query
                    .Include(f => f.Listing!)
                    .ThenInclude(l => l.Images);

                // Pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / PAGE_SIZE);

                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                var favorites = await query
                    .Skip((page - 1) * PAGE_SIZE)
                    .Take(PAGE_SIZE)
                    .ToListAsync();

                var viewModel = new FavoritesPageViewModel
                {
                    Favorites = favorites,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalItems = totalCount,
                    PageSize = PAGE_SIZE,
                    SortBy = sort
                };

                return View("~/Views/Account/Favorites.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Favoriler sayfası hatası: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Favorileri toplu silme
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<int> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
                return BadRequest("Hiçbir öğe seçilmedi.");

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var favoritesToDelete = await _context.UserFavorites
                    .Where(f => f.UserId == userId && selectedIds.Contains(f.ListingId))
                    .ToListAsync();

                _context.UserFavorites.RemoveRange(favoritesToDelete);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"{favoritesToDelete.Count} iklem favorilerden çıkarıldı.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Silme işlemi başarısız oldu.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Tüm favorileri temizle
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var count = await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .CountAsync();

                await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .ExecuteDeleteAsync();

                TempData["Success"] = $"Tüm {count} favori temizlendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Temizleme işlemi başarısız oldu.";
                return RedirectToAction(nameof(Index));
            }
        }
    }

    /// <summary>
    /// Favoriler sayfası view model'i
    /// </summary>
    public class FavoritesPageViewModel
    {
        public List<UserFavorite> Favorites { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; } = "recent";

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
