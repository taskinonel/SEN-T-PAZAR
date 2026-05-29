using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;
using Microsoft.AspNetCore.Identity;
using SEN_T_PAZAR.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Globalization;

namespace SEN_T_PAZAR.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private const int MaxUploadImageDimension = 1600;
    private const int UploadJpegQuality = 78;

    private readonly ApplicationDbContext _context;
    private readonly SiteLocalizer _localizer;
    private readonly EmailSender _emailSender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AdminController> _logger;
    private readonly IUploadStorageService _uploadStorage;
    private readonly IUserMessageAutomationService _userMessageAutomationService;

    public AdminController(ApplicationDbContext context, SiteLocalizer localizer, EmailSender emailSender, UserManager<ApplicationUser> userManager, IAuditLogService auditLogService, ILogger<AdminController> logger, IUploadStorageService uploadStorage, IUserMessageAutomationService userMessageAutomationService)
    {
        _context = context;
        _localizer = localizer;
        _emailSender = emailSender;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _logger = logger;
        _uploadStorage = uploadStorage;
        _userMessageAutomationService = userMessageAutomationService;
    }

    private string T(string tr, string en, string ru, string ar, string? fa = null) => _localizer.CultureCode switch
    {
        "en" => en,
        "ru" => ru,
        "ar" => ar,
        "fa" => fa ?? en ?? tr,
        _ => tr
    };

    public IActionResult Index()
    {
        var stats = new AdminDashboardViewModel
        {
            TotalListings = _context.Listings.Count(),
            PendingListings = _context.Listings.Count(x => !x.IsApproved),
            ApprovedListings = _context.Listings.Count(x => x.IsApproved),
            FeaturedListings = _context.Listings.Count(x => x.IsFeatured),
            VitrinListings = _context.Listings.Count(x => x.IsVitrin),
            PopularListings = _context.Listings.Count(x => x.IsPopular),
            TotalUsers = _context.Users.Count(),
            CorporateUsers = _context.Users.Count(x => x.IsCorporateMember),
            PendingCorporate = _context.Users.Count(x => x.IsCorporateMember && !x.IsCorporateApproved),
            PendingReviews = _context.Reviews.Count(r => r.ModerationStatus == ReviewModerationStatus.Pending),
            RecentListings = _context.Listings
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .ToList()
        };
        return View(stats);
    }

    public IActionResult Listings(string status = "all", string userType = "all")
    {
        var query = _context.Listings.AsQueryable();
        query = status switch
        {
            "pending" => query.Where(x => !x.IsApproved),
            "approved" => query.Where(x => x.IsApproved),
            "featured" => query.Where(x => x.IsFeatured),
            "vitrin" => query.Where(x => x.IsVitrin),
            "popular" => query.Where(x => x.IsPopular),
            _ => query
        };

        // Kurumsal / Normal kullanıcı filtresi
        var corporateUserIds = _context.Users
            .Where(u => u.IsCorporateMember)
            .Select(u => u.Id)
            .ToList();

        query = userType switch
        {
            "corporate" => query.Where(x => !string.IsNullOrEmpty(x.UserId) && corporateUserIds.Contains(x.UserId!)),
            "regular" => query.Where(x => string.IsNullOrEmpty(x.UserId) || !corporateUserIds.Contains(x.UserId!)),
            _ => query
        };

        var listings = ApplyListingSort(query.AsEnumerable(), status).ToList();

        ViewData["CurrentStatus"] = status;
        ViewData["CurrentUserType"] = userType;
        return View(listings);
    }

    public async Task<IActionResult> Messages()
    {
        var threads = await LoadVisitorMessageThreadsAsync();
        return View(threads);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendBroadcastMessage(string audience, string subject, string message)
    {
        subject = (subject ?? string.Empty).Trim();
        message = (message ?? string.Empty).Trim();
        audience = (audience ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            TempData["AdminSuccess"] = T("Konu ve mesaj zorunludur.", "Subject and message are required.", "Тема и сообщение обязательны.", "الموضوع والرسالة مطلوبان.", "موضوع و پیام الزامی است.");
            return RedirectToAction(nameof(Messages));
        }

        var recipientsQuery = _context.Users.AsNoTracking().AsQueryable();
        recipientsQuery = audience switch
        {
            "corporate" => recipientsQuery.Where(x => x.IsCorporateMember),
            "registered" => recipientsQuery.Where(x => !x.IsCorporateMember),
            _ => recipientsQuery
        };

        var recipients = await recipientsQuery
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .ToListAsync();

        if (!recipients.Any())
        {
            TempData["AdminSuccess"] = T("Gönderilecek kullanıcı bulunamadı.", "No recipients found.", "Получателей не найдено.", "لم يتم العثور على مستلمين.", "گیرنده‌ای پیدا نشد.");
            return RedirectToAction(nameof(Messages));
        }

        var admin = await _userManager.GetUserAsync(User);
        var sentAt = DateTime.UtcNow;
        var broadcastId = Guid.NewGuid().ToString("N");

        foreach (var recipient in recipients)
        {
            _context.VisitorMessages.Add(new VisitorMessage
            {
                ListingId = 0,
                ConversationId = $"admin-broadcast-{broadcastId}-{recipient.Id}",
                RecipientUserId = recipient.Id,
                RecipientEmail = recipient.Email,
                RecipientPhone = recipient.PhoneNumber,
                SenderUserId = admin?.Id,
                SenderName = admin?.FullName ?? admin?.UserName ?? "Admin",
                SenderEmail = admin?.Email ?? string.Empty,
                SenderPhone = admin?.PhoneNumber,
                SenderRole = "admin",
                Subject = subject,
                Message = message,
                CreatedAtUtc = sentAt,
                IsRead = false
            });
        }

        await _context.SaveChangesAsync();
        TempData["AdminSuccess"] = T("Mesaj gönderildi.", "Message sent.", "Сообщение отправлено.", "تم إرسال الرسالة.", "پیام ارسال شد.");
        return RedirectToAction(nameof(Messages));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkMessageAsRead(int id)
    {
        var message = await _context.VisitorMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (message is null)
        {
            return RedirectToAction(nameof(Messages));
        }

        var conversationId = ResolveConversationId(message);
        var conversationMessages = await _context.VisitorMessages
            .Where(x => x.Id == message.Id || x.ConversationId == conversationId)
            .ToListAsync();

        foreach (var item in conversationMessages)
        {
            item.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Messages));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyMessage(int id, string replyText)
    {
        replyText = (replyText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(replyText))
        {
            TempData["AdminSuccess"] = T("Cevap metni boş olamaz.", "Reply text cannot be empty.", "Текст ответа не может быть пустым.", "لا يمكن أن تكون رسالة الرد فارغة.", "متن پاسخ نمی‌تواند خالی باشد.");
            return RedirectToAction(nameof(Messages));
        }

        var rootMessage = await _context.VisitorMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (rootMessage is null)
        {
            return RedirectToAction(nameof(Messages));
        }

        var admin = await _userManager.GetUserAsync(User);
        var conversationId = ResolveConversationId(rootMessage);
        var reply = new VisitorMessage
        {
            ListingId = rootMessage.ListingId,
            ConversationId = conversationId,
            RecipientUserId = rootMessage.SenderUserId,
            RecipientPhone = rootMessage.SenderPhone,
            RecipientEmail = rootMessage.SenderEmail,
            SenderUserId = admin?.Id,
            SenderName = admin?.FullName ?? admin?.UserName ?? "Admin",
            SenderEmail = admin?.Email ?? string.Empty,
            SenderPhone = admin?.PhoneNumber,
            SenderRole = "admin",
            Subject = rootMessage.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? rootMessage.Subject : $"Re: {rootMessage.Subject}",
            Message = replyText,
            CreatedAtUtc = DateTime.UtcNow,
            IsRead = false
        };

        _context.VisitorMessages.Add(reply);

        var relatedMessages = await _context.VisitorMessages
            .Where(x => x.Id == rootMessage.Id || x.ConversationId == conversationId)
            .ToListAsync();
        foreach (var item in relatedMessages)
        {
            item.IsRead = true;
        }

        await _context.SaveChangesAsync();

        TempData["AdminSuccess"] = T("Yanıt gönderildi.", "Reply sent.", "Ответ отправлен.", "تم إرسال الرد.", "پاسخ ارسال شد.");
        return RedirectToAction(nameof(Messages));
    }

    [HttpGet]
    public IActionResult EditListing(int id)
    {
        var listing = _context.Listings
            .Include(x => x.Images)
            .FirstOrDefault(x => x.Id == id);
        if (listing is null)
        {
            return NotFound();
        }

        var model = new ListingEditViewModel
        {
            Id = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            Category = listing.Category,
            Type = listing.Type,
            City = listing.City,
            Neighborhood = listing.Neighborhood,
            PriceAmount = listing.PriceAmount,
            PriceCurrency = listing.PriceCurrency,
            ExistingImages = listing.Images
                .OrderBy(x => x.Id)
                .Select((x, index) => new ListingEditImageItemViewModel
                {
                    Id = x.Id,
                    FilePath = x.FilePath,
                    IsCover = index == listing.CoverImageIndex
                })
                .ToList(),
            CoverImageId = listing.Images
                .OrderBy(x => x.Id)
                .Select((x, index) => new { x.Id, Index = index })
                .FirstOrDefault(x => x.Index == listing.CoverImageIndex)?.Id
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditListing(ListingEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var listingForValidation = await _context.Listings
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == model.Id);
            if (listingForValidation != null)
            {
                model.ExistingImages = listingForValidation.Images
                    .OrderBy(x => x.Id)
                    .Select((x, index) => new ListingEditImageItemViewModel
                    {
                        Id = x.Id,
                        FilePath = x.FilePath,
                        IsCover = index == listingForValidation.CoverImageIndex
                    })
                    .ToList();
            }
            return View(model);
        }

        var listing = await _context.Listings
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == model.Id);
        if (listing is null)
        {
            return NotFound();
        }

        listing.Title = model.Title.Trim();
        listing.Description = model.Description.Trim();
        listing.Category = model.Category.Trim();
        listing.Type = model.Type.Trim();
        listing.City = model.City.Trim();
        listing.District = string.Empty;
        listing.Neighborhood = string.IsNullOrWhiteSpace(model.Neighborhood) ? null : model.Neighborhood.Trim();
        listing.PriceAmount = model.PriceAmount;
        listing.PriceCurrency = string.IsNullOrWhiteSpace(model.PriceCurrency) ? "TL" : model.PriceCurrency.Trim();

        var deleteIds = (model.DeleteImageIds ?? new List<int>())
            .Distinct()
            .ToHashSet();

        if (deleteIds.Count > 0)
        {
            var imagesToDelete = listing.Images
                .Where(x => deleteIds.Contains(x.Id))
                .ToList();

            foreach (var image in imagesToDelete)
            {
                if (!string.IsNullOrWhiteSpace(image.FilePath))
                {
                    var physicalPath = _uploadStorage.TryGetPhysicalPath(image.FilePath);
                    if (!string.IsNullOrWhiteSpace(physicalPath) && System.IO.File.Exists(physicalPath))
                    {
                        try
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            _context.ListingImages.RemoveRange(imagesToDelete);
        }

        if (model.NewImageFiles != null && model.NewImageFiles.Count > 0)
        {
            var uploadsFolder = _uploadStorage.EnsureDirectory();

            foreach (var file in model.NewImageFiles)
            {
                if (file.Length <= 0)
                {
                    continue;
                }

                var savedPath = await SaveOptimizedImageAsync(file, uploadsFolder, _uploadStorage.GetPublicDirectory());
                if (!string.IsNullOrWhiteSpace(savedPath))
                {
                    listing.Images.Add(new ListingImage { FilePath = savedPath, UserId = listing.UserId, ListingId = listing.Id });
                }
            }
        }

        var orderedImages = listing.Images.OrderBy(x => x.Id).ToList();
        if (model.CoverImageId.HasValue)
        {
            var coverIndex = orderedImages.FindIndex(x => x.Id == model.CoverImageId.Value);
            listing.CoverImageIndex = coverIndex >= 0 ? coverIndex : 0;
        }
        else if (orderedImages.Count == 0)
        {
            listing.CoverImageIndex = 0;
        }
        else if (listing.CoverImageIndex >= orderedImages.Count)
        {
            listing.CoverImageIndex = 0;
        }

        await _context.SaveChangesAsync();
        var admin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.edit", "Listing", listing.Id.ToString(), $"Title={listing.Title}");
        TempData["AdminSuccess"] = T("İlan güncellendi.", "Listing updated.", "Объявление обновлено.", "تم تحديث الإعلان.");
        return RedirectToAction(nameof(Listings));
    }

    private static async Task<string?> SaveOptimizedImageAsync(IFormFile file, string uploadsFolder, string webRelativeDirectory = "/uploads")
    {
        // Basic validation: extension check + magic bytes content check
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!await IsValidImageFileAsync(file))
        {
            return null;
        }

        var outputName = $"{Guid.NewGuid():N}.jpg";
        var outputPath = Path.Combine(uploadsFolder, outputName);

        // Copy to memory stream because form file streams are forward-only and non-seekable
        // (IsValidImageFileAsync already read the stream above)
        await using var inputStream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await inputStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var image = await Image.LoadAsync(memoryStream);

        // Strip EXIF/metadata to avoid leaking sensitive data
        try
        {
            image.Metadata.ExifProfile = null;
        }
        catch
        {
            // ignore
        }

        if (image.Width > MaxUploadImageDimension || image.Height > MaxUploadImageDimension)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(MaxUploadImageDimension, MaxUploadImageDimension),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3
            }));
        }

        var encoder = new JpegEncoder
        {
            Quality = UploadJpegQuality
        };

        await image.SaveAsJpegAsync(outputPath, encoder);
        var normalizedDirectory = string.IsNullOrWhiteSpace(webRelativeDirectory)
            ? "/uploads"
            : webRelativeDirectory.TrimEnd('/');
        return normalizedDirectory + "/" + outputName;
    }

    private static async Task<bool> IsValidImageFileAsync(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var buffer = new byte[12];
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
            {
                // JPEG
                return true;
            }

            if (read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 && buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
            {
                // PNG
                return true;
            }

            if (read >= 12 && buffer[0] == (byte)'R' && buffer[1] == (byte)'I' && buffer[2] == (byte)'F' && buffer[3] == (byte)'F' && buffer[8] == (byte)'W' && buffer[9] == (byte)'E' && buffer[10] == (byte)'B' && buffer[11] == (byte)'P')
            {
                // WEBP
                return true;
            }
        }
        catch
        {
            // swallow and treat as invalid
        }

        return false;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDisplayOrder(
        int id,
        int? featuredOrder,
        int? vitrinOrder,
        int? popularOrder,
        bool? isFeatured = null,
        bool? isVitrin = null,
        bool? isPopular = null,
        bool autoSave = false,
        string status = "all")
    {
        var wantsJson = autoSave || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing is null)
        {
            var notFoundMessage = T("İlan bulunamadı.", "Listing not found.", "Объявление не найдено.", "لم يتم العثور على الإعلان.", "آگهی پیدا نشد.");
            if (wantsJson)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Json(new { success = false, message = notFoundMessage });
            }

            TempData["AdminSuccess"] = notFoundMessage;
            return RedirectToAction("Listings", new { status });
        }

        try
        {
            var targetFeatured = isFeatured ?? listing.IsFeatured;
            var targetVitrin = isVitrin ?? listing.IsVitrin;
            var targetPopular = isPopular ?? listing.IsPopular;

            var normalizedFeatured = NormalizePositiveOrder(featuredOrder);
            var normalizedVitrin = NormalizePositiveOrder(vitrinOrder);
            var normalizedPopular = NormalizePositiveOrder(popularOrder);

            listing.IsFeatured = targetFeatured;
            listing.IsVitrin = targetVitrin;
            listing.IsPopular = targetPopular;

            if (targetFeatured)
            {
                listing.FeaturedExpiryDate ??= DateTime.UtcNow.AddDays(30);
                normalizedFeatured ??= ParseDisplayOrder(listing.FeaturedPackage) ?? GetNextDisplayOrderForFeatured(listing.Id);
            }
            else
            {
                listing.FeaturedExpiryDate = null;
                normalizedFeatured = null;
            }

            if (targetVitrin)
            {
                listing.VitrinExpiryDate ??= DateTime.UtcNow.AddDays(30);
                normalizedVitrin ??= ParseDisplayOrder(listing.VitrinPackage) ?? GetNextDisplayOrderForVitrin(listing.Id);
            }
            else
            {
                listing.VitrinExpiryDate = null;
                normalizedVitrin = null;
            }

            if (targetPopular)
            {
                normalizedPopular ??= listing.PopularOrder ?? GetNextDisplayOrderForPopular(listing.Id);
            }
            else
            {
                normalizedPopular = null;
            }

            listing.FeaturedPackage = UpsertDisplayOrder(listing.FeaturedPackage, normalizedFeatured);
            listing.VitrinPackage = UpsertDisplayOrder(listing.VitrinPackage, normalizedVitrin);
            listing.PopularOrder = normalizedPopular;

            if (targetVitrin && normalizedVitrin.HasValue)
            {
                ReorderVitrinListings(listing.Id, normalizedVitrin.Value);
            }
            else
            {
                NormalizeVitrinOrderSequence();
            }

            if (targetFeatured && normalizedFeatured.HasValue)
            {
                ReorderFeaturedListings(listing.Id, normalizedFeatured.Value);
            }
            else
            {
                NormalizeFeaturedOrderSequence();
            }

            if (targetPopular && normalizedPopular.HasValue)
            {
                ReorderPopularListings(listing.Id, normalizedPopular.Value);
            }
            else
            {
                NormalizePopularOrderSequence();
            }

            await _context.SaveChangesAsync();
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.display-order.update", "Listing", listing.Id.ToString(), $"Featured={normalizedFeatured};Vitrin={normalizedVitrin};Popular={normalizedPopular};Flags={listing.IsFeatured}/{listing.IsVitrin}/{listing.IsPopular}");

            var successMessage = T("Promosyon ve sıralama güncellendi.", "Promotion settings and order updated.", "Настройки продвижения и порядок обновлены.", "تم تحديث إعدادات الترويج والترتيب.", "تنظیمات پروموشن و ترتیب به‌روزرسانی شد.");
            if (wantsJson)
            {
                var vitrinSequence = _context.Listings
                    .Where(x => x.IsVitrin)
                    .Select(x => new { x.Id, x.VitrinPackage })
                    .AsEnumerable()
                    .Select(x => new { id = x.Id, order = ParseDisplayOrder(x.VitrinPackage) })
                    .Where(x => x.order.HasValue)
                    .OrderBy(x => x.order!.Value)
                    .ToList();

                var featuredSequence = _context.Listings
                    .Where(x => x.IsFeatured)
                    .Select(x => new { x.Id, x.FeaturedPackage })
                    .AsEnumerable()
                    .Select(x => new { id = x.Id, order = ParseDisplayOrder(x.FeaturedPackage) })
                    .Where(x => x.order.HasValue)
                    .OrderBy(x => x.order!.Value)
                    .ToList();

                var popularSequence = _context.Listings
                    .Where(x => x.IsPopular)
                    .Select(x => new { id = x.Id, order = x.PopularOrder })
                    .Where(x => x.order.HasValue)
                    .OrderBy(x => x.order!.Value)
                    .ToList();

                return Json(new
                {
                    success = true,
                    message = successMessage,
                    isFeatured = listing.IsFeatured,
                    isVitrin = listing.IsVitrin,
                    isPopular = listing.IsPopular,
                    featuredOrder = ParseDisplayOrder(listing.FeaturedPackage),
                    vitrinOrder = ParseDisplayOrder(listing.VitrinPackage),
                    popularOrder = listing.PopularOrder,
                    vitrinSequence,
                    featuredSequence,
                    popularSequence
                });
            }

            TempData["SavedOrderListingId"] = listing.Id.ToString(CultureInfo.InvariantCulture);
            TempData["AdminSuccess"] = successMessage;
            return RedirectToAction("Listings", new { status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update display order for listing {ListingId}", id);
            var errorMessage = T("Promosyon ayarları şu anda kaydedilemiyor.", "Promotion settings cannot be saved right now.", "Сейчас не удается сохранить настройки продвижения.", "لا يمكن حفظ إعدادات الترويج الآن.", "در حال حاضر ذخیره تنظیمات پروموشن ممکن نیست.");
            if (wantsJson)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(new { success = false, message = errorMessage });
            }

            TempData["AdminSuccess"] = errorMessage;
            return RedirectToAction("Listings", new { status });
        }
    }

    private void ReorderVitrinListings(int currentListingId, int targetOrder)
    {
        var vitrinListings = _context.Listings
            .Where(x => x.IsVitrin)
            .AsEnumerable()
            .OrderBy(x => ParseDisplayOrder(x.VitrinPackage) ?? int.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        var current = vitrinListings.FirstOrDefault(x => x.Id == currentListingId);
        if (current is null)
        {
            return;
        }

        vitrinListings.Remove(current);
        var insertIndex = Math.Clamp(targetOrder - 1, 0, vitrinListings.Count);
        vitrinListings.Insert(insertIndex, current);

        for (var i = 0; i < vitrinListings.Count; i++)
        {
            var item = vitrinListings[i];
            item.VitrinPackage = UpsertDisplayOrder(item.VitrinPackage, i + 1);
        }
    }

    private void ReorderFeaturedListings(int currentListingId, int targetOrder)
    {
        var featuredListings = _context.Listings
            .Where(x => x.IsFeatured)
            .AsEnumerable()
            .OrderBy(x => ParseDisplayOrder(x.FeaturedPackage) ?? int.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        var current = featuredListings.FirstOrDefault(x => x.Id == currentListingId);
        if (current is null)
        {
            return;
        }

        featuredListings.Remove(current);
        var insertIndex = Math.Clamp(targetOrder - 1, 0, featuredListings.Count);
        featuredListings.Insert(insertIndex, current);

        for (var i = 0; i < featuredListings.Count; i++)
        {
            var item = featuredListings[i];
            item.FeaturedPackage = UpsertDisplayOrder(item.FeaturedPackage, i + 1);
        }
    }

    private void ReorderPopularListings(int currentListingId, int targetOrder)
    {
        var popularListings = _context.Listings
            .Where(x => x.IsPopular)
            .AsEnumerable()
            .OrderBy(x => x.PopularOrder ?? int.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        var current = popularListings.FirstOrDefault(x => x.Id == currentListingId);
        if (current is null)
        {
            return;
        }

        popularListings.Remove(current);
        var insertIndex = Math.Clamp(targetOrder - 1, 0, popularListings.Count);
        popularListings.Insert(insertIndex, current);

        for (var i = 0; i < popularListings.Count; i++)
        {
            popularListings[i].PopularOrder = i + 1;
        }
    }

    private static int GetPrimaryListOrder(Listing listing)
    {
        if (listing.IsVitrin)
        {
            return ParseDisplayOrder(listing.VitrinPackage) ?? int.MaxValue;
        }

        if (listing.IsFeatured)
        {
            return ParseDisplayOrder(listing.FeaturedPackage) ?? int.MaxValue;
        }

        if (listing.IsPopular)
        {
            return listing.PopularOrder ?? int.MaxValue;
        }

        return int.MaxValue;
    }

    private static IOrderedEnumerable<Listing> ApplyListingSort(IEnumerable<Listing> source, string? status)
    {
        var normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
        return normalizedStatus switch
        {
            "vitrin" => source
                .OrderBy(x => x.IsVitrin ? (ParseDisplayOrder(x.VitrinPackage) ?? int.MaxValue) : int.MaxValue)
                .ThenByDescending(x => x.CreatedAt),
            "featured" => source
                .OrderBy(x => x.IsFeatured ? (ParseDisplayOrder(x.FeaturedPackage) ?? int.MaxValue) : int.MaxValue)
                .ThenByDescending(x => x.CreatedAt),
            "popular" => source
                .OrderBy(x => x.IsPopular ? (x.PopularOrder ?? int.MaxValue) : int.MaxValue)
                .ThenByDescending(x => x.CreatedAt),
            _ => source
                .OrderByDescending(x => x.IsVitrin)
                .ThenBy(x => x.IsVitrin ? (ParseDisplayOrder(x.VitrinPackage) ?? int.MaxValue) : int.MaxValue)
                .ThenByDescending(x => x.IsFeatured)
                .ThenBy(x => x.IsFeatured ? (ParseDisplayOrder(x.FeaturedPackage) ?? int.MaxValue) : int.MaxValue)
                .ThenByDescending(x => x.IsPopular)
                .ThenBy(x => x.IsPopular ? (x.PopularOrder ?? int.MaxValue) : int.MaxValue)
                .ThenByDescending(x => x.CreatedAt)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            var wasApproved = listing.IsApproved;
            listing.IsApproved = true;
            await _context.SaveChangesAsync();
            if (!wasApproved)
            {
                await _userMessageAutomationService.SendListingApprovedAsync(listing);
            }
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.approve", "Listing", listing.Id.ToString());
        }
        return RedirectToAction("Listings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            listing.IsApproved = false;
            await _context.SaveChangesAsync();
            await _userMessageAutomationService.SendListingRejectedAsync(listing);
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.reject", "Listing", listing.Id.ToString());
        }
        return RedirectToAction("Listings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string reason = "")
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            var images = _context.ListingImages.Where(i => i.ListingId == id).ToList();
            _context.ListingImages.RemoveRange(images);
            _context.Listings.Remove(listing);
            _context.SaveChanges();
            
            // Listing sahibine email gönder
            if (!string.IsNullOrWhiteSpace(listing.UserId))
            {
                var user = await _userManager.FindByIdAsync(listing.UserId);
                if (user?.Email != null && user.EmailNotifications)
                {
                    var subject = "İlanınız Silinmiştir";
                    var body = $@"<h3>İlanınız Yönetici Tarafından Silinmiştir</h3>
                        <p><strong>İlan No:</strong> #{listing.Id}</p>
                        <p><strong>İlan Başlığı:</strong> {listing.Title}</p>
                        <p><strong>Silme Nedeni:</strong> {reason}</p>
                        <p>Daha fazla bilgi için lütfen destek ekibimizle iletişime geçiniz.</p>";
                    
                    try { await _emailSender.SendAsync(user.Email, subject, body); } catch { }
                }
            }

            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.delete", "Listing", id.ToString(), reason);
        }
        return RedirectToAction("Listings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendListing(int id, string reason = "")
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            listing.IsApproved = false;
            listing.IsClosed = true;
            _context.SaveChanges();
            await _userMessageAutomationService.SendListingSuspendedAsync(listing, reason);
            
            // Listing sahibine email gönder
            if (!string.IsNullOrWhiteSpace(listing.UserId))
            {
                var user = await _userManager.FindByIdAsync(listing.UserId);
                if (user?.Email != null && user.EmailNotifications)
                {
                    var subject = "İlanınız Askıya Alınmıştır";
                    var body = $@"<h3>İlanınız Yönetici Tarafından Askıya Alınmıştır</h3>
                        <p><strong>İlan No:</strong> #{listing.Id}</p>
                        <p><strong>İlan Başlığı:</strong> {listing.Title}</p>
                        <p><strong>Askıya Alma Nedeni:</strong> {reason}</p>
                        <p>İlanınız tekrar yayınlanana kadar görüntülenemez.</p>";
                    
                    try { await _emailSender.SendAsync(user.Email, subject, body); } catch { }
                }
            }

            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.suspend", "Listing", id.ToString(), reason);
        }
        return RedirectToAction("Listings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResumeListing(int id)
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            listing.IsClosed = false;
            listing.DealStatus = "open";
            _context.SaveChanges();
            
            // Listing sahibine email gönder
            if (!string.IsNullOrWhiteSpace(listing.UserId))
            {
                var user = await _userManager.FindByIdAsync(listing.UserId);
                if (user?.Email != null && user.EmailNotifications)
                {
                    var subject = "İlanınız Tekrar Yayınlanmıştır";
                    var body = $@"<h3>İlanınız Yönetici Tarafından Tekrar Yayınlanmıştır</h3>
                        <p><strong>İlan No:</strong> #{listing.Id}</p>
                        <p><strong>İlan Başlığı:</strong> {listing.Title}</p>
                        <p>İlanınız artık görünebilir durumdadır.</p>";
                    
                    try { await _emailSender.SendAsync(user.Email, subject, body); } catch { }
                }
            }

            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.resume", "Listing", id.ToString());
        }
        return RedirectToAction("Listings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePopular(int id, string status = "all")
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            listing.IsPopular = !listing.IsPopular;

            if (listing.IsPopular)
            {
                listing.PopularOrder ??= GetNextDisplayOrderForPopular(listing.Id);
            }
            else
            {
                listing.PopularOrder = null;
            }

            NormalizePopularOrderSequence();

            await _context.SaveChangesAsync();
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.popular.toggle", "Listing", id.ToString(), listing.IsPopular ? "enabled" : "disabled");
        }
        return RedirectToAction("Listings", new { status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(int id)
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            listing.IsFeatured = !listing.IsFeatured;
            if (listing.IsFeatured)
            {
                listing.FeaturedExpiryDate = DateTime.UtcNow.AddDays(30);
                if (!ParseDisplayOrder(listing.FeaturedPackage).HasValue)
                {
                    var nextOrder = GetNextDisplayOrderForFeatured(listing.Id);
                    listing.FeaturedPackage = UpsertDisplayOrder(listing.FeaturedPackage, nextOrder);
                }
            }
            else
            {
                listing.FeaturedExpiryDate = null;

                // Keep metadata clean when featured is disabled.
                listing.FeaturedPackage = UpsertDisplayOrder(listing.FeaturedPackage, null);
            }

            NormalizeFeaturedOrderSequence();

            await _context.SaveChangesAsync();
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.featured.toggle", "Listing", id.ToString(), listing.IsFeatured ? "enabled" : "disabled");
        }
        return RedirectToAction("Listings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVitrin(int id)
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            listing.IsVitrin = !listing.IsVitrin;
            if (listing.IsVitrin)
            {
                listing.VitrinExpiryDate = DateTime.UtcNow.AddDays(30);
                if (!ParseDisplayOrder(listing.VitrinPackage).HasValue)
                {
                    var nextOrder = GetNextDisplayOrderForVitrin(listing.Id);
                    listing.VitrinPackage = UpsertDisplayOrder(listing.VitrinPackage, nextOrder);
                }
            }
            else
            {
                listing.VitrinExpiryDate = null;

                // Keep metadata clean when showcase is disabled.
                listing.VitrinPackage = UpsertDisplayOrder(listing.VitrinPackage, null);
            }

            NormalizeVitrinOrderSequence();

            await _context.SaveChangesAsync();
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.vitrin.toggle", "Listing", id.ToString(), listing.IsVitrin ? "enabled" : "disabled");
        }
        return RedirectToAction("Listings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishToVitrin(int id)
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            var wasApproved = listing.IsApproved;
            listing.IsApproved = true;
            listing.IsVitrin = true;
            listing.VitrinExpiryDate = DateTime.UtcNow.AddDays(30);

            if (!ParseDisplayOrder(listing.VitrinPackage).HasValue)
            {
                var nextOrder = GetNextDisplayOrderForVitrin(listing.Id);
                listing.VitrinPackage = UpsertDisplayOrder(listing.VitrinPackage, nextOrder);
            }

            NormalizeVitrinOrderSequence();

            await _context.SaveChangesAsync();
            if (!wasApproved)
            {
                await _userMessageAutomationService.SendListingApprovedAsync(listing);
            }
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.publish-vitrin", "Listing", id.ToString());
        }
        return RedirectToAction("Listings");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishToStream(int id)
    {
        var listing = _context.Listings.FirstOrDefault(x => x.Id == id);
        if (listing != null)
        {
            var wasApproved = listing.IsApproved;
            listing.IsApproved = true;
            listing.IsFeatured = true;
            listing.FeaturedExpiryDate = DateTime.UtcNow.AddDays(30);

            if (!ParseDisplayOrder(listing.FeaturedPackage).HasValue)
            {
                var nextOrder = GetNextDisplayOrderForFeatured(listing.Id);
                listing.FeaturedPackage = UpsertDisplayOrder(listing.FeaturedPackage, nextOrder);
            }

            NormalizeFeaturedOrderSequence();

            await _context.SaveChangesAsync();
            if (!wasApproved)
            {
                await _userMessageAutomationService.SendListingApprovedAsync(listing);
            }
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "listing.publish-featured", "Listing", id.ToString());
        }
        return RedirectToAction("Listings");
    }

    private int GetNextDisplayOrderForVitrin(int currentListingId)
    {
        return _context.Listings
            .Where(x => x.IsVitrin && x.Id != currentListingId)
            .AsEnumerable()
            .Select(x => ParseDisplayOrder(x.VitrinPackage) ?? 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    private int GetNextDisplayOrderForFeatured(int currentListingId)
    {
        return _context.Listings
            .Where(x => x.IsFeatured && x.Id != currentListingId)
            .AsEnumerable()
            .Select(x => ParseDisplayOrder(x.FeaturedPackage) ?? 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    private int GetNextDisplayOrderForPopular(int currentListingId)
    {
        return (_context.Listings
            .Where(x => x.IsPopular && x.Id != currentListingId)
            .Max(x => (int?)x.PopularOrder) ?? 0) + 1;
    }

    private void NormalizeVitrinOrderSequence()
    {
        var vitrinListings = _context.Listings
            .Where(x => x.IsVitrin)
            .AsEnumerable()
            .OrderBy(x => ParseDisplayOrder(x.VitrinPackage) ?? int.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        for (var i = 0; i < vitrinListings.Count; i++)
        {
            var item = vitrinListings[i];
            item.VitrinPackage = UpsertDisplayOrder(item.VitrinPackage, i + 1);
        }
    }

    private void NormalizeFeaturedOrderSequence()
    {
        var featuredListings = _context.Listings
            .Where(x => x.IsFeatured)
            .AsEnumerable()
            .OrderBy(x => ParseDisplayOrder(x.FeaturedPackage) ?? int.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        for (var i = 0; i < featuredListings.Count; i++)
        {
            var item = featuredListings[i];
            item.FeaturedPackage = UpsertDisplayOrder(item.FeaturedPackage, i + 1);
        }
    }

    private void NormalizePopularOrderSequence()
    {
        var popularListings = _context.Listings
            .Where(x => x.IsPopular)
            .AsEnumerable()
            .OrderBy(x => x.PopularOrder ?? int.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        for (var i = 0; i < popularListings.Count; i++)
        {
            popularListings[i].PopularOrder = i + 1;
        }
    }

    private static int? NormalizePositiveOrder(int? value)
    {
        return value.HasValue && value.Value > 0 ? value.Value : null;
    }

    private static int? ParseDisplayOrder(string? packageMeta)
    {
        if (string.IsNullOrWhiteSpace(packageMeta))
        {
            return null;
        }

        var parts = packageMeta.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (!part.StartsWith("order=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(part[6..], out var order) && order > 0)
            {
                return order;
            }
        }

        return null;
    }

    private static string? UpsertDisplayOrder(string? packageMeta, int? order)
    {
        var parts = (packageMeta ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.StartsWith("order=", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (order.HasValue)
        {
            parts.Add($"order={order.Value}");
        }

        return parts.Count == 0 ? null : string.Join(';', parts);
    }

    public IActionResult Users()
    {
        var users = _context.Users
            .AsNoTracking()
            .OrderByDescending(x => x.EmailConfirmed)
            .ThenBy(x => x.Email)
            .ToList();

        var externalLoginUserIds = _context.UserLogins
            .Select(x => x.UserId)
            .Distinct()
            .ToHashSet();

        ViewBag.ExternalLoginUserIds = externalLoginUserIds;
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendUserMessage(string userId, string subject, string message)
    {
        userId = (userId ?? string.Empty).Trim();
        subject = (subject ?? string.Empty).Trim();
        message = (message ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            TempData["AdminError"] = T(
                "Mesaj göndermek için kullanıcı, konu ve mesaj zorunludur.",
                "User, subject and message are required to send a message.",
                "Для отправки сообщения нужны пользователь, тема и текст.",
                "المستخدم والموضوع والرسالة مطلوبة للإرسال.",
                "برای ارسال پیام، کاربر، موضوع و متن پیام الزامی است.");
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            TempData["AdminError"] = T(
                "Mesaj gönderilecek kullanıcı bulunamadı.",
                "The target user could not be found.",
                "Целевой пользователь не найден.",
                "لم يتم العثور على المستخدم المستهدف.",
                "کاربر موردنظر پیدا نشد.");
            return RedirectToAction(nameof(Users));
        }

        var admin = await _userManager.GetUserAsync(User);
        var conversationId = $"admin-user-{user.Id}-{admin?.Id ?? "admin"}";

        _context.VisitorMessages.Add(new VisitorMessage
        {
            ListingId = 0,
            ConversationId = conversationId,
            RecipientUserId = user.Id,
            RecipientEmail = user.Email,
            RecipientPhone = user.PhoneNumber,
            SenderUserId = admin?.Id,
            SenderName = admin?.FullName ?? admin?.UserName ?? "Admin",
            SenderEmail = admin?.Email ?? string.Empty,
            SenderPhone = admin?.PhoneNumber,
            SenderRole = "admin",
            Subject = subject,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow,
            IsRead = false
        });

        await _context.SaveChangesAsync();
        await _auditLogService.LogAdminActionAsync(HttpContext, admin, "user.message.send", "User", user.Id, subject);

        TempData["AdminSuccess"] = T(
            "Kullanıcıya mesaj gönderildi.",
            "Message sent to the user.",
            "Сообщение пользователю отправлено.",
            "تم إرسال الرسالة إلى المستخدم.",
            "پیام برای کاربر ارسال شد.");
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> TaxonomyAudit()
    {
        static string Clean(string? value) => value?.Trim() ?? string.Empty;

        var unknownCategoryIssue = T("Kategori tanınmıyor", "Unknown category", "Неизвестная категория", "فئة غير معروفة", "دسته ناشناخته");
        var normalizedCategoryIssue = T("Kategori normalize ediliyor", "Category is normalized", "Категория нормализуется", "يتم تطبيع الفئة", "دسته نرمال‌سازی می‌شود");
        var invalidSubCategoryIssue = T("Alt kategori geçersiz", "Invalid subcategory", "Недопустимая подкатегория", "فئة فرعية غير صالحة", "زیردسته نامعتبر است");
        var normalizedSubCategoryIssue = T("Alt kategori normalize ediliyor", "Subcategory is normalized", "Подкатегория нормализуется", "يتم تطبيع الفئة الفرعية", "زیردسته نرمال‌سازی می‌شود");
        var invalidTypeIssue = T("İlan tipi geçersiz", "Invalid listing type", "Недопустимый тип объявления", "نوع إعلان غير صالح", "نوع آگهی نامعتبر است");
        var normalizedTypeIssue = T("İlan tipi normalize ediliyor", "Listing type is normalized", "Тип объявления нормализуется", "يتم تطبيع نوع الإعلان", "نوع آگهی نرمال‌سازی می‌شود");

        var listings = await _context.Listings
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Category,
                x.SubCategory,
                x.Type
            })
            .ToListAsync();

        var listingIssues = listings
            .Select(listing =>
            {
                var rawCategory = Clean(listing.Category);
                var rawSubCategory = Clean(listing.SubCategory);
                var rawType = Clean(listing.Type);

                var normalizedCategory = ListingTaxonomy.NormalizePublishCategory(rawCategory);
                var normalizedSubCategory = ListingTaxonomy.NormalizeSubCategory(rawSubCategory) ?? string.Empty;
                var normalizedType = ListingTaxonomy.NormalizeListingType(rawType);

                var isKnownCategory = !string.Equals(ListingTaxonomy.GetCategoryTranslationKey(normalizedCategory), "allCategories", StringComparison.OrdinalIgnoreCase);
                var isValidSubCategory = ListingTaxonomy.IsValidSubCategory(rawCategory, rawSubCategory);
                var isValidType = ListingTaxonomy.IsValidListingType(rawCategory, rawType);

                var categoryDrift = !string.Equals(rawCategory, normalizedCategory, StringComparison.OrdinalIgnoreCase);
                var subCategoryDrift = !string.Equals(rawSubCategory, normalizedSubCategory, StringComparison.OrdinalIgnoreCase);
                var typeDrift = !string.Equals(rawType, normalizedType, StringComparison.OrdinalIgnoreCase);

                var issues = new List<string>();

                if (!isKnownCategory)
                {
                    issues.Add(unknownCategoryIssue);
                }
                else if (categoryDrift)
                {
                    issues.Add(normalizedCategoryIssue);
                }

                if (!isValidSubCategory)
                {
                    issues.Add(invalidSubCategoryIssue);
                }
                else if (subCategoryDrift)
                {
                    issues.Add(normalizedSubCategoryIssue);
                }

                if (!isValidType)
                {
                    issues.Add(invalidTypeIssue);
                }
                else if (typeDrift)
                {
                    issues.Add(normalizedTypeIssue);
                }

                return new AdminTaxonomyAuditListingIssueRow
                {
                    ListingId = listing.Id,
                    Title = listing.Title,
                    RawCategory = rawCategory,
                    RawSubCategory = rawSubCategory,
                    RawType = rawType,
                    NormalizedCategory = normalizedCategory,
                    NormalizedSubCategory = normalizedSubCategory,
                    NormalizedType = normalizedType,
                    HasCategoryIssue = !isKnownCategory || categoryDrift,
                    HasSubCategoryIssue = !isValidSubCategory || subCategoryDrift,
                    HasTypeIssue = !isValidType || typeDrift,
                    HasDrift = issues.Count > 0,
                    Issues = issues
                };
            })
            .ToList();

        var model = new AdminTaxonomyAuditViewModel
        {
            TotalListings = listings.Count,
            DriftedListingCount = listingIssues.Count(x => x.HasDrift),
            InvalidCategoryCount = listingIssues.Count(x => x.Issues.Contains(unknownCategoryIssue, StringComparer.OrdinalIgnoreCase)),
            InvalidSubCategoryCount = listingIssues.Count(x => x.Issues.Contains(invalidSubCategoryIssue, StringComparer.OrdinalIgnoreCase)),
            InvalidTypeCount = listingIssues.Count(x => x.Issues.Contains(invalidTypeIssue, StringComparer.OrdinalIgnoreCase)),
            Categories = listingIssues
                .GroupBy(x => new { x.RawCategory, x.NormalizedCategory })
                .Select(group => new AdminTaxonomyAuditCategoryRow
                {
                    RawCategory = group.Key.RawCategory,
                    NormalizedCategory = group.Key.NormalizedCategory,
                    Count = group.Count(),
                    IsKnown = group.All(item => !string.Equals(ListingTaxonomy.GetCategoryTranslationKey(item.NormalizedCategory), "allCategories", StringComparison.OrdinalIgnoreCase)),
                    HasDrift = group.Any(item => !string.Equals(item.RawCategory, item.NormalizedCategory, StringComparison.OrdinalIgnoreCase))
                })
                .OrderByDescending(x => !x.IsKnown)
                .ThenByDescending(x => x.HasDrift)
                .ThenByDescending(x => x.Count)
                .ToList(),
            SubCategories = listingIssues
                .GroupBy(x => new { x.RawCategory, x.RawSubCategory, x.NormalizedCategory, x.NormalizedSubCategory })
                .Select(group => new AdminTaxonomyAuditSubCategoryRow
                {
                    RawCategory = group.Key.RawCategory,
                    RawSubCategory = group.Key.RawSubCategory,
                    NormalizedCategory = group.Key.NormalizedCategory,
                    NormalizedSubCategory = group.Key.NormalizedSubCategory,
                    Count = group.Count(),
                    IsValid = group.All(item => !item.Issues.Contains(invalidSubCategoryIssue, StringComparer.OrdinalIgnoreCase)),
                    HasDrift = group.Any(item => !string.Equals(item.RawSubCategory, item.NormalizedSubCategory, StringComparison.OrdinalIgnoreCase))
                })
                .OrderByDescending(x => !x.IsValid)
                .ThenByDescending(x => x.HasDrift)
                .ThenByDescending(x => x.Count)
                .ToList(),
            ListingTypes = listingIssues
                .GroupBy(x => new { x.RawCategory, x.RawType, x.NormalizedCategory, x.NormalizedType })
                .Select(group => new AdminTaxonomyAuditTypeRow
                {
                    RawCategory = group.Key.RawCategory,
                    RawType = group.Key.RawType,
                    NormalizedCategory = group.Key.NormalizedCategory,
                    NormalizedType = group.Key.NormalizedType,
                    Count = group.Count(),
                    IsValid = group.All(item => !item.Issues.Contains(invalidTypeIssue, StringComparer.OrdinalIgnoreCase)),
                    HasDrift = group.Any(item => !string.Equals(item.RawType, item.NormalizedType, StringComparison.OrdinalIgnoreCase))
                })
                .OrderByDescending(x => !x.IsValid)
                .ThenByDescending(x => x.HasDrift)
                .ThenByDescending(x => x.Count)
                .ToList(),
            ListingIssues = listingIssues
                .Where(x => x.HasDrift)
                .OrderByDescending(x => x.HasCategoryIssue)
                .ThenByDescending(x => x.HasSubCategoryIssue)
                .ThenByDescending(x => x.HasTypeIssue)
                .ThenBy(x => x.ListingId)
                .Take(200)
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendUser(string id, string reason = "")
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == id);
        if (user != null)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(10));
            
            // Kullanıcıya email gönder
            if (user.Email != null && user.EmailNotifications)
            {
                var subject = "Hesabınız Askıya Alınmıştır";
                var body = $@"<h3>Hesabınız Yönetici Tarafından Askıya Alınmıştır</h3>
                    <p><strong>Hesap:</strong> {user.UserName}</p>
                    <p><strong>Askıya Alma Tarihi:</strong> {DateTime.UtcNow:dd.MM.yyyy HH:mm}</p>
                    <p><strong>Nedeni:</strong> {reason}</p>
                    <p>Daha fazla bilgi için destek ekibimizle iletişime geçiniz.</p>";
                
                try { await _emailSender.SendAsync(user.Email, subject, body); } catch { }
            }

            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "user.suspend", "User", user.Id, reason);
        }
        return RedirectToAction("Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResumeUser(string id)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == id);
        if (user != null)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            
            // Kullanıcıya email gönder
            if (user.Email != null && user.EmailNotifications)
            {
                var subject = "Hesabınız Yeniden Aktifleştirilmiştir";
                var body = $@"<h3>Hesabınız Yönetici Tarafından Yeniden Aktifleştirilmiştir</h3>
                    <p><strong>Hesap:</strong> {user.UserName}</p>
                    <p><strong>Reaktivasyon Tarihi:</strong> {DateTime.UtcNow:dd.MM.yyyy HH:mm}</p>
                    <p>Hesabınızı normal şekilde kullanmaya başlayabilirsiniz.</p>";
                
                try { await _emailSender.SendAsync(user.Email, subject, body); } catch { }
            }

            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "user.resume", "User", user.Id);
        }
        return RedirectToAction("Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id, string reason = "")
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == id);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
            
            // Kullanıcıya email gönder
            if (user.Email != null && user.EmailNotifications)
            {
                var subject = "Hesabınız Silinmiştir";
                var body = $@"<h3>Hesabınız Yönetici Tarafından Silinmiştir</h3>
                    <p><strong>Hesap:</strong> {user.UserName}</p>
                    <p><strong>Silme Tarihi:</strong> {DateTime.UtcNow:dd.MM.yyyy HH:mm}</p>
                    <p><strong>Nedeni:</strong> {reason}</p>
                    <p>Daha fazla bilgi için destek ekibimizle iletişime geçiniz.</p>";
                
                try { await _emailSender.SendAsync(user.Email, subject, body); } catch { }
            }

            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "user.delete", "User", user.Id, reason);
        }
        return RedirectToAction("Users");
    }

    public IActionResult CorporateApprovals()
    {
        var pending = _context.Users
            .Where(x => x.IsCorporateMember && !x.IsCorporateApproved)
            .ToList();
        return View(pending);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveCorporate(string id)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == id);
        if (user != null && user.IsCorporateMember)
        {
            user.IsCorporateApproved = true;
            user.IsSubscriptionActive = true;
            user.CorporateApprovalDate = DateTime.UtcNow;
            user.CorporateNote = "approved";
            await _context.SaveChangesAsync();
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "corporate.approve", "User", user.Id);
        }
        return RedirectToAction("CorporateApprovals");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectCorporate(string id)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == id);
        if (user != null)
        {
            user.IsCorporateApproved = false;
            user.IsSubscriptionActive = false;
            user.CorporateNote = "rejected";
            await _context.SaveChangesAsync();
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "corporate.reject", "User", user.Id);
        }
        return RedirectToAction("CorporateApprovals");
    }

    // ============================================================
    // ÖDEME YÖNETİMİ (Havale/EFT onay sistemi)
    // ============================================================

    public IActionResult Payments(string status = "pending")
    {
        var query = _context.Payments
            .Include(p => p.Package)
            .Include(p => p.User)
            .AsQueryable();

        query = status switch
        {
            "completed" => query.Where(p => p.PaymentStatus == "completed"),
            "failed" => query.Where(p => p.PaymentStatus == "failed"),
            _ => query.Where(p => p.PaymentStatus == "pending")
        };

        var payments = query.OrderByDescending(p => p.CreatedAt).ToList();
        foreach (var p in payments)
        {
            if (p.UserFullName == null && p.User != null)
            {
                p.UserFullName = p.User.FullName;
            }
        }
        ViewData["CurrentStatus"] = status;
        ViewBag.Packages = _context.PricingPackages.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToList();
        return View(payments);
    }


    private async Task<List<VisitorMessageThreadViewModel>> LoadVisitorMessageThreadsAsync()
    {
        var rows = await _context.VisitorMessages
            .AsNoTracking()
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ListingId,
                x.ConversationId,
                ListingTitle = _context.Listings.Where(l => l.Id == x.ListingId).Select(l => l.Title).FirstOrDefault(),
                x.SenderName,
                x.SenderEmail,
                x.SenderPhone,
                x.Subject,
                x.Message,
                x.CreatedAtUtc,
                x.IsRead,
                x.SenderRole,
                x.RecipientEmail,
                x.RecipientUserId,
                x.RecipientPhone,
                x.SenderUserId
            })
            .ToListAsync();

        return rows
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ConversationId) ? $"legacy-{x.Id}" : x.ConversationId)
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.CreatedAtUtc).ToList();
                var root = ordered.First();
                return new VisitorMessageThreadViewModel
                {
                    ConversationId = group.Key,
                    RootMessageId = root.Id,
                    Id = root.Id,
                    ListingId = root.ListingId,
                    ListingTitle = root.ListingId == 0 ? T("Sistem mesajı", "System message", "Системное сообщение", "رسالة نظامية", "پیام سیستمی") : root.ListingTitle ?? $"Ilan #{root.ListingId}",
                    RecipientEmail = root.RecipientEmail ?? string.Empty,
                    SenderName = root.SenderName,
                    SenderEmail = root.SenderEmail,
                    SenderPhone = root.SenderPhone,
                    Subject = root.Subject,
                    IsRead = ordered.All(x => x.IsRead),
                    CreatedAtUtc = ordered.Last().CreatedAtUtc,
                    Messages = ordered.Select(x => new VisitorMessageEntryViewModel
                    {
                        Id = x.Id,
                        SenderName = x.SenderName,
                        SenderRole = x.SenderRole,
                        Message = x.Message,
                        CreatedAtUtc = x.CreatedAtUtc,
                        IsRead = x.IsRead
                    }).ToList()
                };
            })
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();
    }

    private static string ResolveConversationId(VisitorMessage message)
    {
        return string.IsNullOrWhiteSpace(message.ConversationId)
            ? $"legacy-{message.Id}"
            : message.ConversationId;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(int id)
    {
        var payment = _context.Payments.Include(p => p.Package).FirstOrDefault(p => p.Id == id);
        if (payment != null && payment.PaymentStatus == "pending")
        {
            payment.PaymentStatus = "completed";
            payment.CompletedAt = DateTime.UtcNow;

            // UserPackage oluştur
            if (payment.PackageId.HasValue)
            {
                var userPackage = new UserPackage
                {
                    UserId = payment.UserId,
                    PackageId = payment.PackageId.Value,
                    TotalPurchased = 1,
                    UsedCount = 0,
                    IsActive = true,
                    PurchasedAt = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(payment.Package?.DurationDays ?? 30)
                };
                _context.UserPackages.Add(userPackage);
            }

            await _context.SaveChangesAsync();
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "payment.confirm", "Payment", payment.Id.ToString(), $"Amount={payment.Amount}");
            TempData["PaymentSuccess"] = string.Format(T("Ödeme #{0} onaylandı.", "Payment #{0} approved.", "Платеж #{0} подтвержден.", "تمت الموافقة على الدفعة رقم {0}."), id);
        }
        return RedirectToAction("Payments");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPayment(int id)
    {
        var payment = _context.Payments.FirstOrDefault(p => p.Id == id);
        if (payment != null)
        {
            payment.PaymentStatus = "failed";
            payment.Note = T("Yönetici tarafından reddedildi", "Rejected by administrator", "Отклонено администратором", "تم الرفض من قبل المسؤول");
            await _context.SaveChangesAsync();
            var admin = await _userManager.GetUserAsync(User);
            await _auditLogService.LogAdminActionAsync(HttpContext, admin, "payment.reject", "Payment", payment.Id.ToString());
            TempData["PaymentError"] = string.Format(T("Ödeme #{0} reddedildi.", "Payment #{0} was rejected.", "Платеж #{0} был отклонен.", "تم رفض الدفعة رقم {0}."), id);
        }
        return RedirectToAction("Payments");
    }

    public IActionResult ReviewModeration()
    {
        var pendingReviews = _context.Reviews
            .Where(r => r.ModerationStatus == ReviewModerationStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToList();

        return View(pendingReviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound();

        var userId = _userManager.GetUserId(User);
        review.ModerationStatus = ReviewModerationStatus.Approved;
        review.ModeratedByUserId = userId;
        review.ModeratedAt = DateTime.UtcNow;

        await UpdateListingRating(review.ListingId);
        await _context.SaveChangesAsync();

        TempData["ReviewSuccess"] = $"Yorum #{id} onaylandı.";
        return RedirectToAction(nameof(ReviewModeration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectReview(int id, string? note)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound();

        var userId = _userManager.GetUserId(User);
        review.ModerationStatus = ReviewModerationStatus.Rejected;
        review.ModerationNote = note;
        review.ModeratedByUserId = userId;
        review.ModeratedAt = DateTime.UtcNow;

        await UpdateListingRating(review.ListingId);
        await _context.SaveChangesAsync();

        TempData["ReviewSuccess"] = $"Yorum #{id} reddedildi.";
        return RedirectToAction(nameof(ReviewModeration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkModerateReviews(int[] ids, string action)
    {
        if (ids == null || ids.Length == 0)
            return RedirectToAction(nameof(ReviewModeration));

        var reviews = await _context.Reviews.Where(r => ids.Contains(r.Id)).ToListAsync();
        var userId = _userManager.GetUserId(User);
        var status = action == "approve" ? ReviewModerationStatus.Approved : ReviewModerationStatus.Rejected;
        var affectedListings = new HashSet<int>();

        foreach (var review in reviews)
        {
            review.ModerationStatus = status;
            review.ModeratedByUserId = userId;
            review.ModeratedAt = DateTime.UtcNow;
            affectedListings.Add(review.ListingId);
        }

        foreach (var listingId in affectedListings)
            await UpdateListingRating(listingId);

        await _context.SaveChangesAsync();

        TempData["ReviewSuccess"] = $"{reviews.Count} yorum işlendi.";
        return RedirectToAction(nameof(ReviewModeration));
    }

    private async Task UpdateListingRating(int listingId)
    {
        var approved = await _context.Reviews
            .Where(r => r.ListingId == listingId && r.ModerationStatus == ReviewModerationStatus.Approved)
            .ToListAsync();

        var listing = await _context.Listings.FindAsync(listingId);
        if (listing == null) return;

        listing.ReviewCount = approved.Count;
        listing.AverageRating = approved.Count > 0 ? (float)approved.Average(r => r.Rating) : 0;
    }
}
