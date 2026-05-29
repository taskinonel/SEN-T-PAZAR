using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Controllers;

[Authorize]
public class ListingMessagesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ListingMessagesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var currentUserId = currentUser.Id;

        var rawMessages = await _db.ListingMessages
            .AsNoTracking()
            .Where(x => x.SenderUserId == currentUserId || x.ReceiverUserId == currentUserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.ListingId,
                x.SenderUserId,
                x.ReceiverUserId,
                x.Body,
                x.CreatedAt,
                x.ReadAt
            })
            .ToListAsync();

        var listingIds = rawMessages.Select(x => x.ListingId).Distinct().ToList();
        var userIds = rawMessages
            .SelectMany(x => new[] { x.SenderUserId, x.ReceiverUserId })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var listingLookup = await _db.Listings
            .AsNoTracking()
            .Where(x => listingIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Title })
            .ToDictionaryAsync(x => x.Id, x => x.Title);

        var userLookup = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName, x.UserName, x.Email })
            .ToDictionaryAsync(
                x => x.Id,
                x => !string.IsNullOrWhiteSpace(x.FullName)
                    ? x.FullName
                    : (!string.IsNullOrWhiteSpace(x.UserName) ? x.UserName : (x.Email ?? "Kullanici")));

        var conversations = rawMessages
            .GroupBy(x => new
            {
                x.ListingId,
                OtherUserId = x.SenderUserId == currentUserId ? x.ReceiverUserId : x.SenderUserId
            })
            .Select(group =>
            {
                var latest = group.OrderByDescending(x => x.CreatedAt).First();
                var unreadCount = group.Count(x => x.ReceiverUserId == currentUserId && x.ReadAt == null);
                return new ListingMessageConversationItemViewModel
                {
                    ListingId = group.Key.ListingId,
                    ListingTitle = listingLookup.TryGetValue(group.Key.ListingId, out var listingTitle)
                        ? listingTitle
                        : $"Ilan #{group.Key.ListingId}",
                    OtherUserId = group.Key.OtherUserId,
                    OtherUserDisplayName = userLookup.TryGetValue(group.Key.OtherUserId, out var displayName)
                        ? displayName
                        : "Kullanici",
                    LastMessage = latest.Body,
                    LastMessageAt = latest.CreatedAt,
                    LastMessageFromCurrentUser = latest.SenderUserId == currentUserId,
                    UnreadCount = unreadCount
                };
            })
            .OrderByDescending(x => x.LastMessageAt)
            .ToList();

        var model = new ListingMessageInboxViewModel
        {
            Conversations = conversations
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Thread(int listingId, string userId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction(nameof(Index));
        }

        var listing = await _db.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == listingId);

        if (listing == null)
        {
            return NotFound();
        }

        var otherUser = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (otherUser == null)
        {
            return NotFound();
        }

        var currentUserId = currentUser.Id;
        var messages = await _db.ListingMessages
            .Where(x => x.ListingId == listingId &&
                        ((x.SenderUserId == currentUserId && x.ReceiverUserId == userId) ||
                         (x.SenderUserId == userId && x.ReceiverUserId == currentUserId)))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var unreadIncoming = messages
            .Where(x => x.ReceiverUserId == currentUserId && x.ReadAt == null)
            .ToList();

        if (unreadIncoming.Count > 0)
        {
            foreach (var item in unreadIncoming)
            {
                item.ReadAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        var model = new ListingMessageThreadViewModel
        {
            ListingId = listing.Id,
            ListingTitle = listing.Title,
            OtherUserId = otherUser.Id,
            OtherUserDisplayName = !string.IsNullOrWhiteSpace(otherUser.FullName)
                ? otherUser.FullName
                : (!string.IsNullOrWhiteSpace(otherUser.UserName) ? otherUser.UserName : (otherUser.Email ?? "Kullanici")),
            CanSendMessages = listing.AllowMessages,
            Messages = messages.Select(x => new ListingMessageThreadItemViewModel
            {
                Id = x.Id,
                Body = x.Body,
                CreatedAt = x.CreatedAt,
                IsMine = x.SenderUserId == currentUserId
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendFromListing(int listingId, string body)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var listing = await _db.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == listingId);

        if (listing == null)
        {
            return NotFound();
        }

        if (!listing.AllowMessages)
        {
            TempData["MessageError"] = "Bu ilan icin mesajlasma kapatilmis.";
            return RedirectToAction("Details", "Home", new { id = listingId });
        }

        if (string.IsNullOrWhiteSpace(listing.UserId))
        {
            TempData["MessageError"] = "Ilan sahibine mesaj gonderilemiyor.";
            return RedirectToAction("Details", "Home", new { id = listingId });
        }

        var normalizedBody = (body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedBody))
        {
            TempData["MessageError"] = "Mesaj metni bos olamaz.";
            return RedirectToAction("Details", "Home", new { id = listingId });
        }

        if (normalizedBody.Length > 2000)
        {
            TempData["MessageError"] = "Mesaj en fazla 2000 karakter olabilir.";
            return RedirectToAction("Details", "Home", new { id = listingId });
        }

        if (listing.UserId == currentUser.Id)
        {
            TempData["MessageError"] = "Kendi ilaniniza mesaj gonderemezsiniz.";
            return RedirectToAction("Details", "Home", new { id = listingId });
        }

        var entity = new ListingMessage
        {
            ListingId = listingId,
            SenderUserId = currentUser.Id,
            ReceiverUserId = listing.UserId,
            Body = normalizedBody,
            CreatedAt = DateTime.UtcNow
        };

        _db.ListingMessages.Add(entity);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Thread), new { listingId, userId = listing.UserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int listingId, string userId, string body)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction(nameof(Index));
        }

        var listing = await _db.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == listingId);

        if (listing == null)
        {
            return NotFound();
        }

        if (!listing.AllowMessages)
        {
            TempData["MessageError"] = "Bu ilan icin mesajlasma kapatilmis.";
            return RedirectToAction(nameof(Thread), new { listingId, userId });
        }

        var normalizedBody = (body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedBody))
        {
            TempData["MessageError"] = "Mesaj metni bos olamaz.";
            return RedirectToAction(nameof(Thread), new { listingId, userId });
        }

        if (normalizedBody.Length > 2000)
        {
            TempData["MessageError"] = "Mesaj en fazla 2000 karakter olabilir.";
            return RedirectToAction(nameof(Thread), new { listingId, userId });
        }

        if (userId == currentUser.Id)
        {
            TempData["MessageError"] = "Kendinize mesaj gonderemezsiniz.";
            return RedirectToAction(nameof(Thread), new { listingId, userId });
        }

        var entity = new ListingMessage
        {
            ListingId = listingId,
            SenderUserId = currentUser.Id,
            ReceiverUserId = userId,
            Body = normalizedBody,
            CreatedAt = DateTime.UtcNow
        };

        _db.ListingMessages.Add(entity);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Thread), new { listingId, userId });
    }
}
