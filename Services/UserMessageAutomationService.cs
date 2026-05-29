using System.Net;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Services;

public interface IUserMessageAutomationService
{
    Task SendWelcomeAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task SendListingSubmittedAsync(Listing listing, CancellationToken cancellationToken = default);

    Task SendListingApprovedAsync(Listing listing, CancellationToken cancellationToken = default);

    Task SendListingRejectedAsync(Listing listing, string? reason = null, CancellationToken cancellationToken = default);

    Task SendListingSuspendedAsync(Listing listing, string? reason = null, CancellationToken cancellationToken = default);
    Task SendListingUpdatedAsync(Listing listing, CancellationToken cancellationToken = default);
    Task SendListingExpiryReminderAsync(Listing listing, CancellationToken cancellationToken = default);
}

public sealed class UserMessageAutomationService : IUserMessageAutomationService
{
    private const string SystemSenderName = "SEN-T PAZAR";
    private const string DefaultSenderEmail = "noreply@sentpazar.com";

    private readonly ApplicationDbContext _db;
    private readonly EmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserMessageAutomationService> _logger;

    public UserMessageAutomationService(
        ApplicationDbContext db,
        EmailSender emailSender,
        IConfiguration configuration,
        ILogger<UserMessageAutomationService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendWelcomeAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var displayName = ResolveDisplayName(user.FullName, user.UserName, user.Email);
        var subject = "SEN-T PAZAR'a hoş geldiniz";
        var dashboardUrl = BuildDashboardUrl();
        var message = $"Merhaba {displayName}, üyeliğiniz başarıyla oluşturuldu. Profilinizi tamamlayabilir, ilan verip süreç mesajlarını Mesajlar alanından takip edebilirsiniz. Yardıma ihtiyaç duyarsanız destek ekibimiz yanınızda.";
        var emailBody = BuildEmailBody(
            subject,
            $"Merhaba {displayName}, hesabınız başarıyla oluşturuldu.",
            new[]
            {
                "Profil bilgilerinizi tamamlayarak daha hızlı geri dönüş alabilirsiniz.",
                "İlan verdiğinizde onay ve yayın süreciyle ilgili otomatik bilgilendirme mesajları alırsınız.",
                "Mesajlar alanından tüm sistem bildirimlerinizi tek yerde takip edebilirsiniz."
            },
            dashboardUrl,
            "Mesajları aç");

        return SendAsync(user.Id, user.Email, user.PhoneNumber, 0, $"system-welcome-{user.Id}", subject, message, emailBody, user.EmailNotifications, cancellationToken);
    }

    public async Task SendListingSubmittedAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        var recipient = await ResolveListingRecipientAsync(listing, cancellationToken);
        var subject = BuildListingSubject(listing);
        var dashboardUrl = BuildDashboardUrl();
        var message = $"{SafeListingTitle(listing)} başlıklı ilanınız alınmıştır. Şu an ön inceleme aşamasındadır. İçerik, kategori ve görseller kontrol edildikten sonra yayına alınır veya sizden düzenleme istenir.";
        var emailBody = BuildEmailBody(
            subject,
            $"#{listing.Id} numaralı ilanınız yayına hazırlık kuyruğuna alındı.",
            new[]
            {
                "İlan başlığınız, açıklamanız ve görselleriniz editör kontrolünden geçer.",
                "Uygun bulunursa ilanınız otomatik olarak yayına açılır.",
                "Düzeltme gerekirse mesaj kutunuzda bilgilendirme görürsünüz."
            },
            dashboardUrl,
            "Süreç mesajlarını görüntüle");

        await SendAsync(recipient.UserId, recipient.Email, recipient.Phone, listing.Id, BuildListingConversationId(listing.Id), subject, message, emailBody, recipient.SendEmail, cancellationToken);
    }

    public async Task SendListingApprovedAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        var recipient = await ResolveListingRecipientAsync(listing, cancellationToken);
        var subject = BuildListingSubject(listing);
        var listingUrl = BuildListingUrl(listing.Id);
        var message = $"{SafeListingTitle(listing)} başlıklı ilanınız onaylandı ve yayına açıldı. Artık ziyaretçiler ilanınızı görüntüleyebilir, favorilere ekleyebilir ve sizinle iletişime geçebilir.";
        var emailBody = BuildEmailBody(
            subject,
            $"#{listing.Id} numaralı ilanınız onaylandı.",
            new[]
            {
                "İlanınız artık yayında ve arama sonuçlarında görünür durumdadır.",
                "Gelen soruları ve sistem bildirimlerini mesaj kutunuzdan takip edebilirsiniz.",
                "İlan performansını artırmak için dilerseniz vitrin veya öne çıkan paketlerini kullanabilirsiniz."
            },
            listingUrl,
            "İlanı aç");

        await SendAsync(recipient.UserId, recipient.Email, recipient.Phone, listing.Id, BuildListingConversationId(listing.Id), subject, message, emailBody, recipient.SendEmail, cancellationToken);
    }

    public async Task SendListingRejectedAsync(Listing listing, string? reason = null, CancellationToken cancellationToken = default)
    {
        var recipient = await ResolveListingRecipientAsync(listing, cancellationToken);
        var subject = BuildListingSubject(listing);
        var dashboardUrl = BuildDashboardUrl();
        var detail = string.IsNullOrWhiteSpace(reason)
            ? "İçeriği gözden geçirip gerekli alanları güncelledikten sonra yeniden değerlendirmeye gönderebilirsiniz."
            : $"Dikkat edilmesi gereken nokta: {reason.Trim()}";
        var message = $"{SafeListingTitle(listing)} başlıklı ilanınız şu anda yayına alınamadı. {detail}";
        var emailBody = BuildEmailBody(
            subject,
            $"#{listing.Id} numaralı ilanınız için düzenleme gerekiyor.",
            new[]
            {
                "Başlık, açıklama, fiyat ve kategori alanlarının tutarlı olduğundan emin olun.",
                string.IsNullOrWhiteSpace(reason) ? "Görselleri ve iletişim bilgilerini kontrol ederek ilanı tekrar düzenleyin." : $"Yönetici notu: {reason.Trim()}",
                "Düzenleme sonrası ilanınızı yeniden kaydedebilir veya destek ekibimizden yardım alabilirsiniz."
            },
            dashboardUrl,
            "Mesaj kutusunu aç");

        await SendAsync(recipient.UserId, recipient.Email, recipient.Phone, listing.Id, BuildListingConversationId(listing.Id), subject, message, emailBody, recipient.SendEmail, cancellationToken);
    }

    public async Task SendListingSuspendedAsync(Listing listing, string? reason = null, CancellationToken cancellationToken = default)
    {
        var recipient = await ResolveListingRecipientAsync(listing, cancellationToken);
        var subject = BuildListingSubject(listing);
        var dashboardUrl = BuildDashboardUrl();
        var reasonText = string.IsNullOrWhiteSpace(reason)
            ? "İlanınız geçici olarak yayından kaldırıldı."
            : $"Askıya alma nedeni: {reason.Trim()}";
        var message = $"{SafeListingTitle(listing)} başlıklı ilanınız askıya alındı. {reasonText} Düzenleme yaptıktan sonra yeniden değerlendirilmesi için bizimle iletişime geçebilirsiniz.";
        var emailBody = BuildEmailBody(
            subject,
            $"#{listing.Id} numaralı ilanınız geçici olarak askıya alındı.",
            new[]
            {
                string.IsNullOrWhiteSpace(reason) ? "İlanınız şu an ziyaretçilere gösterilmiyor." : $"Yönetici notu: {reason.Trim()}",
                "Düzenleme sonrası ilanınızı tekrar yayına almak için destek ekibimizle iletişime geçebilirsiniz.",
                "Tüm süreç mesajlarını hesap panelinizdeki Mesajlar alanında bulabilirsiniz."
            },
            dashboardUrl,
            "Mesajları görüntüle");

        await SendAsync(recipient.UserId, recipient.Email, recipient.Phone, listing.Id, BuildListingConversationId(listing.Id), subject, message, emailBody, recipient.SendEmail, cancellationToken);
    }

    private async Task<ListingRecipient> ResolveListingRecipientAsync(Listing listing, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(listing.UserId))
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == listing.UserId, cancellationToken);

            if (user != null)
            {
                return new ListingRecipient(
                    user.Id,
                    user.Email,
                    string.IsNullOrWhiteSpace(user.PhoneNumber) ? listing.Phone : user.PhoneNumber,
                    user.EmailNotifications);
            }
        }

        return new ListingRecipient(null, null, listing.Phone, false);
    }

    private async Task SendAsync(
        string? recipientUserId,
        string? recipientEmail,
        string? recipientPhone,
        int listingId,
        string conversationId,
        string subject,
        string message,
        string? emailBody,
        bool sendEmail,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId) && string.IsNullOrWhiteSpace(recipientEmail) && string.IsNullOrWhiteSpace(recipientPhone))
        {
            return;
        }

        try
        {
            _db.VisitorMessages.Add(new VisitorMessage
            {
                ListingId = listingId,
                ConversationId = conversationId,
                RecipientUserId = recipientUserId,
                RecipientEmail = recipientEmail,
                RecipientPhone = recipientPhone,
                SenderName = SystemSenderName,
                SenderEmail = ResolveSenderEmail(),
                SenderRole = "admin",
                Subject = subject,
                Message = message,
                CreatedAtUtc = DateTime.UtcNow,
                IsRead = false
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Automatic visitor message could not be stored. ConversationId={ConversationId}", conversationId);
        }

        // Send email to user if enabled and valid
        if (sendEmail && !string.IsNullOrWhiteSpace(recipientEmail) && !string.IsNullOrWhiteSpace(emailBody))
        {
            try
            {
                await _emailSender.SendAsync(recipientEmail, subject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Automatic email could not be sent to {RecipientEmail}. ConversationId={ConversationId}", recipientEmail, conversationId);
            }
        }

        // Always send notification to admin for monitoring (independent of user's notification preferences)
        await SendAdminNotificationAsync(listingId, conversationId, subject, message, emailBody, recipientEmail, cancellationToken);
    }

    private async Task SendAdminNotificationAsync(
        int listingId,
        string conversationId,
        string subject,
        string message,
        string? emailBody,
        string? recipientEmail,
        CancellationToken cancellationToken)
    {
        try
        {
            var admin = (_configuration["Notifications:AdminEmail"] ?? string.Empty).Trim();

            // Skip admin notification if user is the admin (avoid duplicate)
            if (string.Equals(admin, recipientEmail, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // store as visitor message for admin
            try
            {
                _db.VisitorMessages.Add(new VisitorMessage
                {
                    ListingId = listingId,
                    ConversationId = conversationId,
                    RecipientUserId = null,
                    RecipientEmail = admin,
                    RecipientPhone = null,
                    SenderName = SystemSenderName,
                    SenderEmail = ResolveSenderEmail(),
                    SenderRole = "system",
                    Subject = subject,
                    Message = message,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsRead = false
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not store admin visitor message for {AdminEmail}", admin);
            }

            // send email to admin
            if (!string.IsNullOrWhiteSpace(emailBody))
            {
                try
                {
                    await _emailSender.SendAsync(admin, subject, emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Automatic admin email could not be sent to {AdminEmail}", admin);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin notification failure");
        }
    }

    public Task SendListingUpdatedAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        var recipient = ResolveListingRecipientAsync(listing, cancellationToken);
        var subject = BuildListingSubject(listing);
        var listingUrl = BuildListingUrl(listing.Id);
        var message = $"{SafeListingTitle(listing)} başlıklı ilanınız güncellendi. Yayın süresi gerekli şartlarda uzatıldı.";
        var emailBody = BuildEmailBody(
            subject,
            $"#{listing.Id} numaralı ilanınız güncellendi.",
            new[] { "İlanınızda yapılan değişiklikler kaydedildi.", "Yayın süresi güncellenmişse ilgili bildirim size iletilir.", "Mesajlar alanından süreci takip edebilirsiniz." },
            listingUrl,
            "İlanı görüntüle");

        // resolve recipient then send
        return Task.Run(async () =>
        {
            var r = await recipient;
            await SendAsync(r.UserId, r.Email, r.Phone, listing.Id, BuildListingConversationId(listing.Id), subject, message, emailBody, r.SendEmail, cancellationToken);
        }, cancellationToken);
    }

    public Task SendListingExpiryReminderAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        var recipientTask = ResolveListingRecipientAsync(listing, cancellationToken);
        var subject = BuildListingSubject(listing);
        var listingUrl = BuildListingUrl(listing.Id);
        var message = $"{SafeListingTitle(listing)} başlıklı ilanınız için yayın süresi yaklaşıyor. Lütfen ilanın bilgilerini kontrol edip gerekirse güncelleyin.";
        var emailBody = BuildEmailBody(
            subject,
            $"#{listing.Id} numaralı ilanınızın yayın süresi yaklaşıyor.",
            new[] { "Yayın süreniz kalan 7 gün içinde sona erecek.", "Güncelleme yaparsanız yayın süresi 60 gün daha uzatılacaktır.", "Mesajlar alanından ilgili bildirimleri takip edebilirsiniz." },
            listingUrl,
            "İlanı düzenle");

        return Task.Run(async () =>
        {
            var recipient = await recipientTask;
            await SendAsync(recipient.UserId, recipient.Email, recipient.Phone, listing.Id, BuildListingConversationId(listing.Id), subject, message, emailBody, recipient.SendEmail, cancellationToken);
        }, cancellationToken);
    }

    private string BuildEmailBody(string title, string intro, IReadOnlyList<string> items, string actionUrl, string actionText)
    {
        var encodedItems = string.Join(string.Empty, items.Select(x => $"<li style='margin:0 0 8px'>{WebUtility.HtmlEncode(x)}</li>"));
        return $@"<div style='font-family:Arial,sans-serif;line-height:1.65;color:#17324d'>
<h2 style='margin:0 0 16px'>{WebUtility.HtmlEncode(title)}</h2>
<p>{WebUtility.HtmlEncode(intro)}</p>
<ul style='padding-left:18px;margin:16px 0'>{encodedItems}</ul>
<p style='margin:24px 0'>
    <a href='{WebUtility.HtmlEncode(actionUrl)}' style='display:inline-block;background:#0f5caa;color:#fff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:700'>
        {WebUtility.HtmlEncode(actionText)}
    </a>
</p>
<p style='font-size:13px;color:#6b7280'>Bu mesaj SEN-T PAZAR sistem bildirimi olarak otomatik gönderildi.</p>
</div>";
    }

    private string BuildDashboardUrl()
    {
        return BuildAbsoluteUrl("/Account/Dashboard?tab=messages");
    }

    private string BuildListingUrl(int listingId)
    {
        return BuildAbsoluteUrl($"/Home/Details/{listingId}");
    }

    private string BuildAbsoluteUrl(string relativePath)
    {
        var origin = (_configuration["App:PublicOrigin"]
                ?? _configuration["Authentication:Google:PublicOrigin"]
                ?? "https://www.sentpazar.com")
            .TrimEnd('/');

        return $"{origin}{relativePath}";
    }

    private string ResolveSenderEmail()
    {
        var configured = (_configuration["Smtp:From"] ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(configured) ? DefaultSenderEmail : configured;
    }

    private static string BuildListingSubject(Listing listing)
    {
        return $"İlan süreci güncellemesi: {SafeListingTitle(listing)}";
    }

    private static string BuildListingConversationId(int listingId)
    {
        return $"system-listing-{listingId}";
    }

    private static string SafeListingTitle(Listing listing)
    {
        return string.IsNullOrWhiteSpace(listing.Title) ? $"İlan #{listing.Id}" : listing.Title.Trim();
    }

    private static string ResolveDisplayName(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "Değerli Kullanıcımız";
    }

    private sealed record ListingRecipient(string? UserId, string? Email, string? Phone, bool SendEmail);
}