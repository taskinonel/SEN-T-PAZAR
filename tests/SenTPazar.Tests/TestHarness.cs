using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SEN_T_PAZAR.Models;
using SEN_T_PAZAR.Services;

namespace SenTPazar.Tests;

internal sealed class TestHarness : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Db => _provider.GetRequiredService<ApplicationDbContext>();
    public UserManager<ApplicationUser> UserManager => _provider.GetRequiredService<UserManager<ApplicationUser>>();
    public IConfiguration Configuration { get; }

    public TestHarness()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-super-secret-key-12345",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Configuration);
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlite(_connection));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        _provider = services.BuildServiceProvider();

        Db.Database.EnsureCreated();
    }

    public T WithUser<T>(T controller, string userId) where T : ControllerBase
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal,
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("sen-t.com")
                }
            }
        };

        return controller;
    }

    public void Dispose()
    {
        Db.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }
}

internal sealed class NoOpUserMessageAutomationService : IUserMessageAutomationService
{
    public Task SendWelcomeAsync(ApplicationUser user, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendListingSubmittedAsync(Listing listing, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendListingApprovedAsync(Listing listing, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendListingRejectedAsync(Listing listing, string? reason = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendListingSuspendedAsync(Listing listing, string? reason = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendListingUpdatedAsync(Listing listing, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendListingExpiryReminderAsync(Listing listing, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class TestUploadStorageService : IUploadStorageService
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "sent-pazar-tests", Guid.NewGuid().ToString("N"));

    public string RootPath => _rootPath;

    public string RequestPath => "/uploads";

    public string EnsureDirectory(params string[] segments)
    {
        var fullPath = Path.Combine(new[] { RootPath }.Concat(segments.Where(x => !string.IsNullOrWhiteSpace(x))).ToArray());
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public string GetPublicDirectory(params string[] segments)
    {
        var normalized = segments
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().Trim('/', '\\'))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return normalized.Length == 0
            ? RequestPath
            : RequestPath + "/" + string.Join('/', normalized);
    }

    public string? TryGetPhysicalPath(string? publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath))
        {
            return null;
        }

        var relative = publicPath.Trim().TrimStart('/', '\\');
        return Path.Combine(RootPath, relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
