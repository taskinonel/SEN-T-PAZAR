using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Controllers;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Services;

public sealed class SavedSearchNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SavedSearchNotificationService> _logger;

    public SavedSearchNotificationService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SavedSearchNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saved search notifications failed.");
            }

            var intervalMinutes = Math.Clamp(_configuration.GetValue<int?>("SavedSearchNotifications:IntervalMinutes") ?? 30, 5, 180);
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var enabled = _configuration.GetValue<bool?>("SavedSearchNotifications:Enabled") ?? true;
        if (!enabled)
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var emailSender = scope.ServiceProvider.GetRequiredService<EmailSender>();

        var publicOrigin = (_configuration["App:PublicOrigin"] ?? _configuration["Authentication:Google:PublicOrigin"] ?? "https://www.sentpazar.com").TrimEnd('/');
        var users = await db.Users
            .Where(x => x.EmailConfirmed && x.EmailNotifications && x.Email != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            var serialized = await userManager.GetAuthenticationTokenAsync(user, "SEN-TPAZAR", "saved-searches");
            if (string.IsNullOrWhiteSpace(serialized))
            {
                continue;
            }

            List<HomeController.SavedSearchItem>? searches;
            try
            {
                searches = JsonSerializer.Deserialize<List<HomeController.SavedSearchItem>>(serialized);
            }
            catch
            {
                continue;
            }

            if (searches == null || searches.Count == 0)
            {
                continue;
            }

            var changed = false;
            foreach (var search in searches)
            {
                var since = search.LastNotifiedAtUtc ?? search.CreatedAtUtc;
                var matches = await FindMatchesAsync(db, search.Query, since, cancellationToken);
                if (matches.Count == 0)
                {
                    continue;
                }

                var body = BuildEmailBody(matches, publicOrigin, search);
                await emailSender.SendAsync(user.Email!, "Kayitli aramaniza uygun yeni ilanlar bulundu", body);
                search.LastNotifiedAtUtc = DateTime.UtcNow;
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            await userManager.SetAuthenticationTokenAsync(
                user,
                "SEN-TPAZAR",
                "saved-searches",
                JsonSerializer.Serialize(searches));
        }
    }

    private static async Task<List<Listing>> FindMatchesAsync(
        ApplicationDbContext db,
        Dictionary<string, string> query,
        DateTime since,
        CancellationToken cancellationToken)
    {
        var q = db.Listings
            .AsNoTracking()
            .Where(x => x.IsApproved && !x.IsClosed && x.CreatedAt > since);

        if (query.TryGetValue("category", out var category) && !string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.Category.ToLower() == category.ToLower());
        }

        if (query.TryGetValue("city", out var city) && !string.IsNullOrWhiteSpace(city) && !city.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var cityFilter = city.Trim();
            if (cityFilter.Contains("::", StringComparison.Ordinal))
            {
                var parts = cityFilter.Split("::", 2, StringSplitOptions.TrimEntries);
                var cityPart = parts[0];
                var districtPart = parts.Length > 1 ? parts[1] : string.Empty;
                q = q.Where(x => x.City == cityPart && (x.District == districtPart || (x.Neighborhood != null && x.Neighborhood.Contains(districtPart))));
            }
            else
            {
                q = q.Where(x => x.City == cityFilter);
            }
        }

        if (query.TryGetValue("listingType", out var listingType) && !string.IsNullOrWhiteSpace(listingType) && !listingType.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var loweredType = listingType.Trim().ToLowerInvariant();
            q = loweredType switch
            {
                "sale" => q.Where(x => x.Type.ToLower().Contains("sat")),
                "rent" => q.Where(x => x.Type.ToLower().Contains("kira")),
                "daily" => q.Where(x => x.Type.ToLower().Contains("g\u00fcn") || x.Type.ToLower().Contains("daily")),
                _ => q
            };
        }

        if (query.TryGetValue("keyword", out var keyword) && !string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim().ToLower();
            q = q.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.Description.ToLower().Contains(term) ||
                x.City.ToLower().Contains(term) ||
                x.District.ToLower().Contains(term));
        }

        if (query.TryGetValue("priceRange", out var priceRange) && !string.IsNullOrWhiteSpace(priceRange) && !priceRange.Equals("any", StringComparison.OrdinalIgnoreCase))
        {
            q = priceRange.Trim().ToLowerInvariant() switch
            {
                "low" => q.Where(x => x.PriceAmount <= 1_000_000m),
                "mid" => q.Where(x => x.PriceAmount > 1_000_000m && x.PriceAmount <= 5_000_000m),
                "high" => q.Where(x => x.PriceAmount > 5_000_000m),
                _ => q
            };
        }

        return await q
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);
    }

    private static string BuildEmailBody(List<Listing> matches, string publicOrigin, HomeController.SavedSearchItem search)
    {
        var sb = new StringBuilder();
        sb.Append("<h3>Kayitli aramaniza uygun yeni ilanlar bulundu</h3>");
        sb.Append("<p>Asagidaki ilanlar son bildiriminizden sonra eklendi:</p><ul>");

        foreach (var item in matches)
        {
            sb.Append("<li>");
            sb.Append($"<a href=\"{publicOrigin}/Home/Details/{item.Id}\">{System.Net.WebUtility.HtmlEncode(item.Title)}</a>");
            sb.Append($" - {item.PriceAmount:n0} {System.Net.WebUtility.HtmlEncode(item.PriceCurrency)}");
            sb.Append("</li>");
        }

        sb.Append("</ul>");
        sb.Append($"<p>Arama kaydi: <code>{System.Net.WebUtility.HtmlEncode(JsonSerializer.Serialize(search.Query))}</code></p>");
        return sb.ToString();
    }
}
