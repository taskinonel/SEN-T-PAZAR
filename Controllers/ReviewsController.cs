using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;
using System.Security.Claims;

namespace SEN_T_PAZAR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("{listingId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviewsForListing(int listingId)
        {
            var listing = await _context.Listings.FindAsync(listingId);
            if (listing == null)
                return NotFound();

            var reviews = await _context.Reviews
                .Where(r => r.ListingId == listingId && r.ModerationStatus == ReviewModerationStatus.Approved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPost]
        public async Task<ActionResult<Review>> AddReview([FromBody] Review review)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var listing = await _context.Listings.FindAsync(review.ListingId);
            if (listing == null)
                return NotFound("Listing not found");

            if (listing.UserId == userId)
                return BadRequest("You cannot review your own listing");

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ListingId == review.ListingId && r.UserId == userId);
            if (existingReview != null)
                return BadRequest("You have already reviewed this listing");

            review.UserId = userId;
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);
            review.UserName = user?.UserName ?? "Anonymous";
            review.CreatedAt = DateTime.UtcNow;
            review.ModerationStatus = ReviewModerationStatus.Pending;

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviewsForListing), new { listingId = review.ListingId }, review);
        }

        /// <summary>
        /// Get all reviews with optional moderation status filter (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Review>>> GetAllReviews([FromQuery] ReviewModerationStatus? status = null)
        {
            var query = _context.Reviews.AsQueryable();

            if (status.HasValue)
                query = query.Where(r => r.ModerationStatus == status.Value);

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(100)
                .ToListAsync();

            return Ok(reviews);
        }

        /// <summary>
        /// Approve a review (Admin only)
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            review.ModerationStatus = ReviewModerationStatus.Approved;
            review.ModeratedByUserId = userId;
            review.ModeratedAt = DateTime.UtcNow;

            await UpdateListingRatingAsync(review.ListingId);
            await _context.SaveChangesAsync();

            return Ok(review);
        }

        /// <summary>
        /// Reject a review with optional note (Admin only)
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectReview(int id, [FromBody] ModerationRequest request)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            review.ModerationStatus = ReviewModerationStatus.Rejected;
            review.ModerationNote = request?.Note;
            review.ModeratedByUserId = userId;
            review.ModeratedAt = DateTime.UtcNow;

            await UpdateListingRatingAsync(review.ListingId);
            await _context.SaveChangesAsync();

            return Ok(review);
        }

        /// <summary>
        /// Bulk moderation: approve/reject multiple reviews (Admin only)
        /// </summary>
        [HttpPost("bulk-moderate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkModerate([FromBody] BulkModerationRequest request)
        {
            if (request?.Ids == null || request.Ids.Length == 0)
                return BadRequest("No review IDs provided");

            var reviews = await _context.Reviews
                .Where(r => request.Ids.Contains(r.Id))
                .ToListAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var status = request.Action == "approve" ? ReviewModerationStatus.Approved : ReviewModerationStatus.Rejected;
            var affectedListingIds = new HashSet<int>();

            foreach (var review in reviews)
            {
                review.ModerationStatus = status;
                review.ModerationNote = request.Note;
                review.ModeratedByUserId = userId;
                review.ModeratedAt = DateTime.UtcNow;
                affectedListingIds.Add(review.ListingId);
            }

            foreach (var listingId in affectedListingIds)
                await UpdateListingRatingAsync(listingId);

            await _context.SaveChangesAsync();

            return Ok(new { Modified = reviews.Count });
        }

        private async Task UpdateListingRatingAsync(int listingId)
        {
            var listing = await _context.Listings.FindAsync(listingId);
            if (listing == null) return;

            var approvedReviews = await _context.Reviews
                .Where(r => r.ListingId == listingId && r.ModerationStatus == ReviewModerationStatus.Approved)
                .ToListAsync();

            listing.ReviewCount = approvedReviews.Count;
            listing.AverageRating = approvedReviews.Count > 0
                ? (float)approvedReviews.Average(r => r.Rating)
                : 0;

            await _context.SaveChangesAsync();
        }
    }

    public class ModerationRequest
    {
        public string? Note { get; set; }
    }

    public class BulkModerationRequest
    {
        public int[] Ids { get; set; } = Array.Empty<int>();
        public string Action { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
