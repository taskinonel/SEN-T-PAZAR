using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;
using SEN_T_PAZAR.Services;

namespace SEN_T_PAZAR.Controllers;

public class PricingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PricingController> _logger;
    private readonly SiteLocalizer _localizer;

    public PricingController(
        ApplicationDbContext db,
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PricingController> logger,
        SiteLocalizer localizer)
    {
        _db = db;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _localizer = localizer;
    }

    private string T(string tr, string en, string ru, string ar, string? fa = null) => _localizer.CultureCode switch
    {
        "en" => en,
        "ru" => ru,
        "ar" => ar,
        "fa" => fa ?? en ?? tr,
        _ => tr
    };

    [HttpGet]
    public async Task<IActionResult> Index(string? type = null)
    {
        var query = _db.PricingPackages.Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(p => p.PackageType == type);
        }

        var packages = await query
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        List<UserPackageInfo> userPackages = new();
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var up = await _db.UserPackages
                    .Where(p => p.UserId == user.Id && p.IsActive && (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow))
                    .Include(p => p.Package)
                    .ToListAsync();

                userPackages = up.Select(p => new UserPackageInfo
                {
                    PackageName = p.Package?.Name ?? "Bilinmiyor",
                    PackageType = p.Package?.PackageType ?? string.Empty,
                    RemainingUses = p.RemainingCount,
                    ExpiryDate = p.ExpiryDate
                }).ToList();
            }
        }

        ViewData["UserPackages"] = userPackages;
        ViewData["SelectedType"] = type;

        return View(packages);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Purchase(int id)
    {
        var package = await _db.PricingPackages.FindAsync(id);
        if (package == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        UserPackage? userPackage = null;
        if (user != null)
        {
            userPackage = await _db.UserPackages
                .Where(p => p.UserId == user.Id && p.PackageId == package.Id && p.IsActive)
                .FirstOrDefaultAsync();
        }

        var model = new PurchaseViewModel
        {
            PackageId = package.Id,
            PackageName = package.Name,
            PackageType = package.PackageType,
            Price = package.Price,
            Currency = package.Currency,
            DurationDays = package.DurationDays,
            Description = package.Description,
            AvailableCount = userPackage?.RemainingCount ?? 0
        };

        PopulatePaymentOptions(model);

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("payment-post")]
    public async Task<IActionResult> Purchase(PurchaseViewModel model)
    {
        PopulatePaymentOptions(model);

        if (!IsSupportedPaymentMethod(model.PaymentMethod))
        {
            ModelState.AddModelError(nameof(model.PaymentMethod), T("Geçersiz ödeme yöntemi seçildi.", "An invalid payment method was selected.", "Выбран недопустимый способ оплаты.", "تم اختيار طريقة دفع غير صالحة."));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        var package = await _db.PricingPackages.FindAsync(model.PackageId);

        if (user == null)
        {
            return Unauthorized();
        }

        if (package == null)
        {
            return NotFound();
        }

        if (string.Equals(model.PaymentMethod, "credit_card", StringComparison.OrdinalIgnoreCase))
        {
            var stripeSecret = (_configuration["Payments:Stripe:SecretKey"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(stripeSecret) || !IsLiveStripeSecret(stripeSecret))
            {
                ModelState.AddModelError(string.Empty, T("Kredi kartı ödemesi sadece canlı Stripe anahtarı ile açılır. Lütfen canlı anahtar tanımlayın veya havale/EFT seçin.", "Credit card payment is enabled only with a live Stripe key. Configure a live key or use bank transfer/EFT.", "Оплата картой доступна только с live-ключом Stripe. Настройте live-ключ или используйте банковский перевод/EFT.", "الدفع بالبطاقة متاح فقط مع مفتاح Stripe المباشر. قم بإعداد المفتاح المباشر أو استخدم التحويل البنكي/EFT."));
                return View(model);
            }

            var payment = new Payment
            {
                UserId = user.Id,
                PackageId = package.Id,
                PaymentMethod = "credit_card",
                PaymentStatus = "pending",
                Amount = package.Price,
                Currency = package.Currency,
                TransactionId = null,
                ExternalPaymentId = null,
                CompletedAt = null,
                Note = T("Kredi kartı (Stripe Checkout) başlatıldı", "Credit card payment (Stripe Checkout) started", "Оплата картой (Stripe Checkout) запущена", "تم بدء الدفع بالبطاقة (Stripe Checkout)")
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            var sessionResult = await CreateStripeCheckoutSessionAsync(user, package, payment.Id, stripeSecret);
            if (!sessionResult.Success || string.IsNullOrWhiteSpace(sessionResult.CheckoutUrl))
            {
                payment.PaymentStatus = "failed";
                payment.Note = T("Stripe oturumu açılamadı: ", "Unable to open Stripe session: ", "Не удалось открыть сессию Stripe: ", "تعذر فتح جلسة Stripe: ") + (sessionResult.ErrorMessage ?? T("Bilinmeyen hata", "Unknown error", "Неизвестная ошибка", "خطأ غير معروف"));
                await _db.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, sessionResult.ErrorMessage ?? T("Ödeme oturumu oluşturulamadı.", "Payment session could not be created.", "Не удалось создать платежную сессию.", "تعذر إنشاء جلسة الدفع."));
                return View(model);
            }

            payment.ExternalPaymentId = sessionResult.SessionId;
            payment.TransactionId = sessionResult.SessionId;
            payment.PaymentDetails = JsonSerializer.Serialize(new
            {
                CheckoutUrl = sessionResult.CheckoutUrl,
                Provider = "Stripe"
            });
            await _db.SaveChangesAsync();

            return Redirect(sessionResult.CheckoutUrl);
        }

        var bankInfo = GetBankPaymentInfo();
        var paymentRecord = new Payment
        {
            UserId = user.Id,
            PackageId = package.Id,
            PaymentMethod = model.PaymentMethod.ToLowerInvariant(),
            PaymentStatus = "pending",
            Amount = package.Price,
            Currency = package.Currency,
            TransactionId = null,
            ExternalPaymentId = null,
            CompletedAt = null,
            Note = T("Banka transferi bildirimi alındı. Yönetici onayı bekleniyor.", "Bank transfer request received. Awaiting admin approval.", "Запрос на банковский перевод получен. Ожидается подтверждение администратора.", "تم استلام طلب التحويل البنكي. بانتظار موافقة المسؤول.")
        };

        _db.Payments.Add(paymentRecord);
        await _db.SaveChangesAsync();

        var referenceCode = $"SNT-{paymentRecord.Id}-{DateTime.UtcNow:yyyyMMdd}";
        paymentRecord.TransactionId = referenceCode;
        paymentRecord.PaymentDetails = JsonSerializer.Serialize(new
        {
            Method = paymentRecord.PaymentMethod,
            ReferenceCode = referenceCode,
            BankAccountName = bankInfo.AccountName,
            BankName = bankInfo.BankName,
            Iban = bankInfo.Iban,
            SwiftCode = bankInfo.SwiftCode,
            Note = T("Dekont açıklamasına referans kodunu yazınız.", "Please include the reference code in your transfer note.", "Укажите референс-код в назначении перевода.", "يرجى كتابة رمز المرجع في وصف التحويل.")
        });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(PaymentPending), new { id = paymentRecord.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> PaymentPending(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var payment = await _db.Payments
            .Include(x => x.Package)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

        if (payment == null)
        {
            return NotFound();
        }

        var bankInfo = GetBankPaymentInfo();
        ViewData["BankAccountName"] = bankInfo.AccountName;
        ViewData["BankName"] = bankInfo.BankName;
        ViewData["Iban"] = bankInfo.Iban;
        ViewData["SwiftCode"] = bankInfo.SwiftCode;

        return View(payment);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> PaymentReturn(string status = "cancel", string? session_id = null)
    {
        if (string.IsNullOrWhiteSpace(session_id))
        {
            TempData["SuccessMessage"] = status == "success"
                ? T("Ödeme sonucu doğrulanıyor. Birkaç saniye sonra tekrar kontrol edin.", "Payment result is being verified. Please check again in a few seconds.", "Результат платежа проверяется. Повторите попытку через несколько секунд.", "يتم التحقق من نتيجة الدفع. يرجى المحاولة مرة أخرى بعد بضع ثوانٍ.")
                : T("Ödeme işlemi iptal edildi.", "Payment was cancelled.", "Платеж был отменен.", "تم إلغاء الدفع.");
            return RedirectToAction(nameof(Index));
        }

        var payment = await _db.Payments
            .Include(x => x.Package)
            .FirstOrDefaultAsync(x => x.ExternalPaymentId == session_id);

        if (payment == null)
        {
            TempData["SuccessMessage"] = T("Ödeme kaydı bulunamadı.", "Payment record not found.", "Платежная запись не найдена.", "لم يتم العثور على سجل الدفع.");
            return RedirectToAction(nameof(Index));
        }

        if (payment.PaymentStatus == "completed")
        {
            TempData["SuccessMessage"] = T("Ödeme başarıyla tamamlandı.", "Payment completed successfully.", "Платеж успешно завершен.", "اكتملت عملية الدفع بنجاح.");
            return RedirectToAction(nameof(Index));
        }

        var stripeSecret = (_configuration["Payments:Stripe:SecretKey"] ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(stripeSecret))
        {
            TempData["SuccessMessage"] = T("Ödeme sonucu doğrulanamadı. Lütfen destekle iletişime geçin.", "Payment result could not be verified. Please contact support.", "Не удалось подтвердить результат платежа. Свяжитесь с поддержкой.", "تعذر التحقق من نتيجة الدفع. يرجى التواصل مع الدعم.");
            return RedirectToAction(nameof(Index));
        }

        var verify = await GetStripeSessionStatusAsync(session_id, stripeSecret);
        if (verify.IsPaid && payment.Package != null)
        {
            payment.PaymentStatus = "completed";
            payment.CompletedAt = DateTime.UtcNow;
            payment.Note = T("Stripe ödeme dönüşü ile doğrulandı.", "Verified by Stripe payment return.", "Подтверждено возвратом платежа Stripe.", "تم التحقق عبر عودة دفع Stripe.");
            await ActivateUserPackageAsync(payment.UserId, payment.Package);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = T("Ödeme başarıyla tamamlandı.", "Payment completed successfully.", "Платеж успешно завершен.", "اكتملت عملية الدفع بنجاح.");
            return RedirectToAction(nameof(Index));
        }

        payment.PaymentStatus = status == "cancel" ? "failed" : payment.PaymentStatus;
        payment.Note = verify.ErrorMessage ?? payment.Note;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = status == "cancel"
            ? T("Ödeme işlemi iptal edildi.", "Payment was cancelled.", "Платеж был отменен.", "تم إلغاء الدفع.")
            : T("Ödeme henüz tamamlanmadı.", "Payment is not completed yet.", "Платеж еще не завершен.", "لم تكتمل عملية الدفع بعد.");
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("stripe-webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var webhookSecret = (_configuration["Payments:Stripe:WebhookSecret"] ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            _logger.LogWarning("Stripe webhook çağrıldı ancak WebhookSecret yapılandırılmamış.");
            return BadRequest();
        }

        Request.EnableBuffering();
        string payload;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            payload = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
        }

        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (!ValidateStripeSignature(payload, signature, webhookSecret))
        {
            _logger.LogWarning("Stripe webhook imza doğrulaması başarısız.");
            return Unauthorized();
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("type", out var typeElement))
        {
            return Ok();
        }

        var eventType = typeElement.GetString() ?? string.Empty;
        if (!string.Equals(eventType, "checkout.session.completed", StringComparison.OrdinalIgnoreCase))
        {
            return Ok();
        }

        if (!root.TryGetProperty("data", out var dataElement) ||
            !dataElement.TryGetProperty("object", out var sessionObject))
        {
            return Ok();
        }

        var sessionId = sessionObject.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        var paymentStatus = sessionObject.TryGetProperty("payment_status", out var psProp) ? psProp.GetString() : null;

        if (string.IsNullOrWhiteSpace(sessionId) || !string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            return Ok();
        }

        var payment = await _db.Payments.Include(x => x.Package).FirstOrDefaultAsync(x => x.ExternalPaymentId == sessionId);
        if (payment == null || payment.Package == null)
        {
            return Ok();
        }

        if (!string.Equals(payment.PaymentStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            payment.PaymentStatus = "completed";
            payment.CompletedAt = DateTime.UtcNow;
            payment.Note = "Stripe webhook doğrulaması ile tamamlandı.";
            await ActivateUserPackageAsync(payment.UserId, payment.Package);
            await _db.SaveChangesAsync();
        }

        return Ok();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> PromoteListing(int listingId)
    {
        var user = await _userManager.GetUserAsync(User);

        var listing = await _db.Listings.FindAsync(listingId);
        if (user == null || listing == null || listing.UserId != user.Id)
        {
            return NotFound();
        }

        var model = await BuildPromoteListingViewModelAsync(listing, user.Id);

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("payment-post")]
    public async Task<IActionResult> PromoteListing(PromoteListingViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        var listing = await _db.Listings.FindAsync(model.ListingId);
        if (user == null || listing == null || listing.UserId != user.Id)
        {
            return NotFound();
        }

        if (model.SelectedPackageId <= 0)
        {
            ModelState.AddModelError(nameof(model.SelectedPackageId), "Lütfen bir paket seçiniz");
            return View(await BuildPromoteListingViewModelAsync(listing, user.Id));
        }

        var package = await _db.PricingPackages.FindAsync(model.SelectedPackageId);
        if (package == null)
        {
            ModelState.AddModelError("", "Geçersiz paket seçimi");
            return View(await BuildPromoteListingViewModelAsync(listing, user.Id));
        }

        var userPackage = await _db.UserPackages
            .Where(p => p.UserId == user.Id && p.PackageId == package.Id && p.IsActive && p.TotalPurchased > p.UsedCount)
            .FirstOrDefaultAsync();

        if (userPackage == null)
        {
            ModelState.AddModelError("", "Bu paket için yeterli kullanım hakkınız yok");
            return View(await BuildPromoteListingViewModelAsync(listing, user.Id, model.SelectedPackageId));
        }

        userPackage.UsedCount++;

        if (package.PackageType == "featured" || package.PackageType == "combo")
        {
            listing.IsFeatured = true;
            listing.FeaturedExpiryDate = DateTime.UtcNow.AddDays(package.DurationDays);
            listing.FeaturedPackage = package.Tier;
        }

        if (package.PackageType == "vitrin" || package.PackageType == "combo")
        {
            listing.IsVitrin = true;
            listing.VitrinExpiryDate = DateTime.UtcNow.AddDays(package.DurationDays);
            listing.VitrinPackage = package.Tier;
        }

        if (package.PackageType == "popular" || package.PackageType == "combo")
        {
            listing.IsPopular = true;
            listing.PopularOrder = 0; // öne çıkarmak için
        }

        _db.ListingPromotions.Add(new ListingPromotion
        {
            ListingId = listing.Id,
            UserId = user.Id,
            PromotionType = package.PackageType,
            PackageName = package.Name,
            DurationDays = package.DurationDays,
            ExpiresAt = DateTime.UtcNow.AddDays(package.DurationDays),
            PaymentId = null
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "İlanınız başarıyla " + package.Name + " paketi ile güçlendirildi!";
        return RedirectToAction("Dashboard", "Account", new { tab = "listings" });
    }

    private async Task<PromoteListingViewModel> BuildPromoteListingViewModelAsync(Listing listing, string userId, int? selectedPackageId = null)
    {
        var userPackages = await _db.UserPackages
            .Where(p => p.UserId == userId && p.IsActive && p.TotalPurchased > p.UsedCount && (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow))
            .Include(p => p.Package)
            .ToListAsync();

        return new PromoteListingViewModel
        {
            ListingId = listing.Id,
            ListingTitle = listing.Title,
            CurrentFeatured = listing.IsFeatured,
            CurrentVitrin = listing.IsVitrin,
            FeaturedExpiryDate = listing.FeaturedExpiryDate,
            VitrinExpiryDate = listing.VitrinExpiryDate,
            SelectedPackageId = selectedPackageId.GetValueOrDefault(),
            AvailablePackages = userPackages
                .Select(p => new PackageOptionViewModel
                {
                    PackageId = p.PackageId,
                    PackageName = p.Package != null ? p.Package.Name : "Bilinmiyor",
                    PackageType = p.Package != null ? p.Package.PackageType : "",
                    RemainingUses = p.RemainingCount,
                    ExpiryDate = p.ExpiryDate
                })
                .ToList()
        };
    }

    private async Task<(bool Success, string? CheckoutUrl, string? SessionId, string? ErrorMessage)> CreateStripeCheckoutSessionAsync(
        ApplicationUser user,
        PricingPackage package,
        int paymentId,
        string secretKey)
    {
        var successUrl = (_configuration["Payments:Stripe:SuccessUrl"] ?? string.Empty).Trim();
        var cancelUrl = (_configuration["Payments:Stripe:CancelUrl"] ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(successUrl))
        {
            var template = Url.Action(nameof(PaymentReturn), "Pricing", new { status = "success", session_id = "__SESSION_ID__" }, Request.Scheme) ?? string.Empty;
            successUrl = template.Replace("__SESSION_ID__", "{CHECKOUT_SESSION_ID}", StringComparison.Ordinal);
        }

        if (string.IsNullOrWhiteSpace(cancelUrl))
        {
            cancelUrl = Url.Action(nameof(PaymentReturn), "Pricing", new { status = "cancel" }, Request.Scheme) ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(successUrl) || string.IsNullOrWhiteSpace(cancelUrl))
        {
            return (false, null, null, "Ödeme yönlendirme URL'leri oluşturulamadı.");
        }

        var currencyRaw = (package.Currency ?? "TRY").Trim().ToUpperInvariant();
        var currency = currencyRaw switch
        {
            "TL" => "try",
            "TRY" => "try",
            "USD" => "usd",
            "EUR" => "eur",
            "GBP" => "gbp",
            _ => "try"
        };
        var unitAmount = (int)Math.Round(package.Price * 100m, MidpointRounding.AwayFromZero);

        var formData = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["success_url"] = successUrl,
            ["cancel_url"] = cancelUrl,
            ["customer_email"] = user.Email ?? string.Empty,
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = currency,
            ["line_items[0][price_data][unit_amount]"] = unitAmount.ToString(),
            ["line_items[0][price_data][product_data][name]"] = package.Name,
            ["line_items[0][price_data][product_data][description]"] = package.Description ?? "SEN-T PAZAR paket satın alma",
            ["metadata[paymentId]"] = paymentId.ToString(),
            ["metadata[userId]"] = user.Id,
            ["metadata[packageId]"] = package.Id.ToString()
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions")
        {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stripe checkout oluşturulamadı. Status={Status} Body={Body}", (int)response.StatusCode, body);
            return (false, null, null, "Gerçek ödeme oturumu başlatılamadı. Stripe ayarlarını kontrol edin.");
        }

        using var jsonDoc = JsonDocument.Parse(body);
        var root = jsonDoc.RootElement;
        var checkoutUrl = root.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
        var sessionId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        return string.IsNullOrWhiteSpace(checkoutUrl)
            ? (false, null, null, "Stripe oturum bağlantısı alınamadı.")
            : (true, checkoutUrl, sessionId, null);
    }

    private async Task<(bool IsPaid, string? ErrorMessage)> GetStripeSessionStatusAsync(string sessionId, string secretKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.stripe.com/v1/checkout/sessions/{sessionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stripe session durum sorgusu başarısız. Status={Status} Body={Body}", (int)response.StatusCode, body);
            return (false, "Ödeme sonucu doğrulanamadı.");
        }

        using var jsonDoc = JsonDocument.Parse(body);
        var root = jsonDoc.RootElement;
        var paymentStatus = root.TryGetProperty("payment_status", out var ps) ? ps.GetString() : null;
        var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;

        var paid = string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase);

        return (paid, paid ? null : "Ödeme henüz tamamlanmamış görünüyor.");
    }

    private static bool ValidateStripeSignature(string payload, string signatureHeader, string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(webhookSecret))
        {
            return false;
        }

        var parts = signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var timestamp = parts.FirstOrDefault(x => x.StartsWith("t=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
        var signature = parts.FirstOrDefault(x => x.StartsWith("v1=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];

        if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        var secretBytes = Encoding.UTF8.GetBytes(webhookSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var expected = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    private async Task ActivateUserPackageAsync(string userId, PricingPackage package)
    {
        var userPackage = await _db.UserPackages
            .Where(p => p.UserId == userId && p.PackageId == package.Id && p.IsActive)
            .FirstOrDefaultAsync();

        if (userPackage != null)
        {
            userPackage.TotalPurchased += package.ListingsIncluded;
            userPackage.UsedCount = 0;
            userPackage.ExpiryDate = DateTime.UtcNow.AddDays(package.DurationDays);
            return;
        }

        _db.UserPackages.Add(new UserPackage
        {
            UserId = userId,
            PackageId = package.Id,
            TotalPurchased = package.ListingsIncluded,
            ExpiryDate = DateTime.UtcNow.AddDays(package.DurationDays)
        });
    }

    private static bool IsSupportedPaymentMethod(string? paymentMethod)
    {
        return paymentMethod is "bank_transfer" or "eft";
    }

    private void PopulatePaymentOptions(PurchaseViewModel model)
    {
        model.CreditCardEnabled = false;
        model.BankTransferEnabled = true;
        model.EftEnabled = true;

        var bankInfo = GetBankPaymentInfo();
        model.BankAccountName = bankInfo.AccountName;
        model.BankName = bankInfo.BankName;
        model.Iban = bankInfo.Iban;
        model.SwiftCode = bankInfo.SwiftCode;

        if (string.IsNullOrWhiteSpace(model.PaymentMethod) || string.Equals(model.PaymentMethod, "credit_card", StringComparison.OrdinalIgnoreCase))
        {
            model.PaymentMethod = "bank_transfer";
        }
    }

    private static bool IsLiveStripeSecret(string secretKey)
    {
        return secretKey.StartsWith("sk_live_", StringComparison.Ordinal);
    }

    private (string AccountName, string BankName, string Iban, string SwiftCode) GetBankPaymentInfo()
    {
        var config = _configuration.GetSection("Payments:BankTransfer");
        var accountName = config["AccountName"]?.Trim();
        var bankName = config["BankName"]?.Trim();
        var iban = config["Iban"]?.Trim();
        var swiftCode = config["SwiftCode"]?.Trim();

        if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(iban))
        {
            _logger.LogError("Bank transfer payment info is not configured. Set Payments:BankTransfer:AccountName, BankName, and Iban in configuration.");
            throw new InvalidOperationException("Banka bilgileri yapılandırılmamış. Lütfen yöneticiyle iletişime geçin.");
        }

        return (accountName, bankName, iban, swiftCode ?? "-");
    }
}

public class PurchaseViewModel
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string PackageType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TL";
    public int DurationDays { get; set; }
    public string? Description { get; set; }
    public int AvailableCount { get; set; }

    [Required(ErrorMessage = "Ödeme yöntemi seçiniz")]
    public string PaymentMethod { get; set; } = "bank_transfer";

    public bool CreditCardEnabled { get; set; }
    public bool BankTransferEnabled { get; set; } = true;
    public bool EftEnabled { get; set; } = true;

    public string BankAccountName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string SwiftCode { get; set; } = string.Empty;
}

public class PromoteListingViewModel
{
    public int ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public bool CurrentFeatured { get; set; }
    public bool CurrentVitrin { get; set; }
    public DateTime? FeaturedExpiryDate { get; set; }
    public DateTime? VitrinExpiryDate { get; set; }
    public List<PackageOptionViewModel> AvailablePackages { get; set; } = new();

    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir paket seçiniz")]
    public int SelectedPackageId { get; set; }
}

public class PackageOptionViewModel
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string PackageType { get; set; } = string.Empty;
    public int RemainingUses { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
