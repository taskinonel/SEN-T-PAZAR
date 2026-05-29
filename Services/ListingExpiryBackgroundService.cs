using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Services;

public sealed class ListingExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ListingExpiryBackgroundService> _logger;

    public ListingExpiryBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ListingExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Listing expiry background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userMsg = scope.ServiceProvider.GetRequiredService<IUserMessageAutomationService>();

                var now = DateTime.UtcNow;

                // Send reminders for listings expiring within 7 days
                var reminderThreshold = now.AddDays(7);
                var toRemind = await db.Listings
                    .Where(l => !l.IsClosed && l.PublishUntil != null && l.PublishUntil <= reminderThreshold && !l.ExpiryReminderSent)
                    .ToListAsync(stoppingToken);

                foreach (var listing in toRemind)
                {
                    try
                    {
                        await userMsg.SendListingExpiryReminderAsync(listing, stoppingToken);
                        listing.ExpiryReminderSent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send expiry reminder for listing {ListingId}", listing.Id);
                    }
                }

                // Unpublish listings past their PublishUntil
                var toExpire = await db.Listings
                    .Where(l => !l.IsClosed && l.PublishUntil != null && l.PublishUntil <= now)
                    .ToListAsync(stoppingToken);

                foreach (var listing in toExpire)
                {
                    try
                    {
                        listing.IsClosed = true;
                        await userMsg.SendListingSuspendedAsync(listing, "İlanın yayın süresi doldu.", stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to suspend expired listing {ListingId}", listing.Id);
                    }
                }

                if (toRemind.Any() || toExpire.Any())
                {
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ListingExpiryBackgroundService");
            }

            // Wait 1 hour between checks
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("Listing expiry background service stopping.");
    }
}
