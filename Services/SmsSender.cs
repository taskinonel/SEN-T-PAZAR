using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SEN_T_PAZAR.Services;

public class SmsSender : ISmsSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<SmsSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string? ErrorMessage)> SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        var provider = (_configuration["Sms:Provider"] ?? string.Empty).Trim().ToLowerInvariant();
        var isWhatsAppMode = provider == "whatsapp";
        if (provider != "twilio" && !isWhatsAppMode)
        {
            return (false, "Mesaj sağlayıcısı yapılandırılmadı. 'Sms:Provider=whatsapp' veya 'Sms:Provider=twilio' ayarlayın.");
        }

        var accountSid = _configuration["Sms:Twilio:AccountSid"];
        var authToken = _configuration["Sms:Twilio:AuthToken"];
        var fromNumber = _configuration["Sms:Twilio:FromNumber"];

        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(fromNumber))
        {
            return (false, "Twilio ayarları eksik. AccountSid/AuthToken/FromNumber zorunludur.");
        }

        var toTarget = isWhatsAppMode ? EnsureWhatsAppAddress(phoneNumber) : phoneNumber;
        var fromTarget = isWhatsAppMode ? EnsureWhatsAppAddress(fromNumber) : fromNumber;
        var messageText = isWhatsAppMode
            ? $"SEN-T PAZAR WhatsApp doğrulama kodunuz: {code}. Kod 10 dakika geçerlidir."
            : $"SEN-T PAZAR doğrulama kodunuz: {code}. Kod 10 dakika geçerlidir.";

        var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = toTarget,
            ["From"] = fromTarget,
            ["Body"] = messageText
        });

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };

        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);

        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("Twilio mesaj gönderimi başarısız. Status: {StatusCode}, Body: {Body}", (int)response.StatusCode, body);
        return (false, isWhatsAppMode
            ? "WhatsApp doğrulama kodu gönderilemedi. Twilio WhatsApp ayarlarınızı kontrol edin."
            : "SMS gönderimi başarısız oldu. Lütfen sağlayıcı ayarlarını kontrol edin.");
    }

    private static string EnsureWhatsAppAddress(string value)
    {
        var normalized = value.Trim();
        if (normalized.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return $"whatsapp:{normalized}";
    }
}
