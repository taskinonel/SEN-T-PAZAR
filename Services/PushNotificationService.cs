using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Services;

public interface IPushNotificationService
{
    Task SendToUserAsync(ApplicationUser user, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
}

public sealed class PushNotificationService : IPushNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PushNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendToUserAsync(ApplicationUser user, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.FcmToken))
        {
            return;
        }

        var serverKey = _configuration["Fcm:ServerKey"];
        if (string.IsNullOrWhiteSpace(serverKey))
        {
            return;
        }

        var endpoint = _configuration["Fcm:Endpoint"] ?? "https://fcm.googleapis.com/fcm/send";
        var payload = new
        {
            to = user.FcmToken,
            notification = new
            {
                title,
                body
            },
            data = data ?? new Dictionary<string, string>()
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("key", "=" + serverKey);

        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("FCM push failed. Status={StatusCode}, Body={Body}", response.StatusCode, responseBody);
        }
    }
}
