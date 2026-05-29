using System.Globalization;
using Serilog;
using Microsoft.AspNetCore.Localization;
using SEN_T_PAZAR.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.HttpOverrides;
using SEN_T_PAZAR.Hubs;
using Microsoft.AspNetCore.Authentication.Google;

// Configure Serilog early so startup logs are captured.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });
builder.Host.UseSerilog();

// Allow large file uploads (IIS / Kestrel)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB
});

// Configure form options for multipart uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
    options.ValueLengthLimit = int.MaxValue;
});

// Email ayarları
var smtpHost = builder.Configuration["Smtp:Host"] ?? "smtp.example.com";
var smtpPort = int.TryParse(builder.Configuration["Smtp:Port"], out var port) ? port : 587;
var smtpUser = builder.Configuration["Smtp:User"] ?? "";
var smtpPass = builder.Configuration["Smtp:Pass"] ?? "";
var smtpFrom = builder.Configuration["Smtp:From"] ?? "noreply@example.com";
builder.Services.AddSingleton(new SEN_T_PAZAR.Services.EmailSender(smtpHost, smtpPort, smtpUser, smtpPass, smtpFrom));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SiteLocalizer>();
builder.Services.AddScoped<IListingCatalogService, ListingCatalogService>();
builder.Services.AddHttpClient<ITextTranslationService, TextTranslationService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddSingleton<IUploadStorageService, UploadStorageService>();
builder.Services.AddScoped<IUserMessageAutomationService, UserMessageAutomationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSignalR();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
// Response compression for performance
builder.Services.AddResponseCompression();

// Identity ve DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var resolvedConnectionString = string.IsNullOrWhiteSpace(connectionString)
    ? "Data Source=sent-pazar.db"
    : connectionString;
var useSqlite = resolvedConnectionString.Contains(".db", StringComparison.OrdinalIgnoreCase) ||
    resolvedConnectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
    resolvedConnectionString.Contains("sent-pazar.db", StringComparison.OrdinalIgnoreCase);
var usePostgres = !useSqlite && (
    resolvedConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
    resolvedConnectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
    resolvedConnectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase));

builder.Services.AddDbContext<SEN_T_PAZAR.Models.ApplicationDbContext>(options =>
{
    if (useSqlite)
    {
        options.UseSqlite(resolvedConnectionString);
        return;
    }

    if (usePostgres)
    {
        options.UseNpgsql(resolvedConnectionString);
        return;
    }

    options.UseSqlServer(resolvedConnectionString);
});
builder.Services.AddIdentity<SEN_T_PAZAR.Models.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<SEN_T_PAZAR.Models.ApplicationDbContext>()
    .AddDefaultTokenProviders();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

var authenticationBuilder = builder.Services.AddAuthentication();
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authenticationBuilder.AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = googleClientId;
        googleOptions.ClientSecret = googleClientSecret;
    });
}

var app = builder.Build();

// Startup checks: ensure critical secrets are not left to defaults in production
if (!app.Environment.IsDevelopment())
{
    var missing = new List<string>();
    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]) || builder.Configuration["Jwt:Key"].Contains("CHANGE_ME"))
        missing.Add("Jwt:Key");
    if (string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Pass"]))
        missing.Add("Smtp:Pass");
    if (string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]))
        missing.Add("Authentication:Google:ClientSecret");

    if (missing.Count > 0)
    {
        var msg = "Missing required configuration for production: " + string.Join(", ", missing);
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("StartupChecks");
        if (missing.Contains("Jwt:Key"))
        {
            // JWT is required for auth token integrity; stop only for this case.
            logger.LogCritical(msg);
            throw new InvalidOperationException(msg);
        }

        // Non-critical production features can stay disabled while the site remains online.
        logger.LogWarning(msg);
    }
}

// Otomatik admin rolü ve kullanıcıya atama
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SEN_T_PAZAR.Models.ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<SEN_T_PAZAR.Models.ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (dbContext.Database.IsSqlite())
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        // Tables already created by fix-db tool
        // dbContext.Database.Migrate();
    }

    // Admin rolü yoksa oluştur
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    // Admin yapmak istediğiniz e-posta adresini buraya yazın
    var adminEmail = "taskinonel@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new SEN_T_PAZAR.Models.ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Halo9506!");
    }
    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        await userManager.AddToRoleAsync(adminUser, "Admin");

    // Fiyatlandırma paketlerini seed et
    if (!dbContext.PricingPackages.Any())
    {
        var packages = new List<SEN_T_PAZAR.Models.PricingPackage>
        {
            // Öne Çıkan Paketler
            new() { Name = "Gold Öne Çıkan", PackageType = "featured", Tier = "gold", Price = 499, Currency = "TL", DurationDays = 30, ListingsIncluded = 1, Description = "30 gün boyunca ilanınızı ana sayfada öne çıkanlar arasında gösterin", DisplayOrder = 1 },
            new() { Name = "Silver Öne Çıkan", PackageType = "featured", Tier = "silver", Price = 299, Currency = "TL", DurationDays = 14, ListingsIncluded = 1, Description = "14 gün boyunca ilanınızı öne çıkanlar arasında gösterin", DisplayOrder = 2 },
            new() { Name = "Bronze Öne Çıkan", PackageType = "featured", Tier = "bronze", Price = 149, Currency = "TL", DurationDays = 7, ListingsIncluded = 1, Description = "7 gün boyunca ilanınızı öne çıkanlar arasında gösterin", DisplayOrder = 3 },
            
            // Vitrin Paketleri
            new() { Name = "VIP Vitrin", PackageType = "vitrin", Tier = "vip", Price = 999, Currency = "TL", DurationDays = 30, ListingsIncluded = 1, Description = "30 gün boyunca ilanınızı vitrinde üst sıralarda gösterin", DisplayOrder = 4 },
            new() { Name = "Super Vitrin", PackageType = "vitrin", Tier = "super", Price = 599, Currency = "TL", DurationDays = 14, ListingsIncluded = 1, Description = "14 gün boyunca ilanınızı vitrinde gösterin", DisplayOrder = 5 },
            new() { Name = "Standart Vitrin", PackageType = "vitrin", Tier = "standart", Price = 249, Currency = "TL", DurationDays = 7, ListingsIncluded = 1, Description = "7 gün boyunca ilanınızı vitrinde gösterin", DisplayOrder = 6 },
            
            // Kombo Paketler
            new() { Name = "Gold Kombo", PackageType = "combo", Tier = "gold", Price = 799, Currency = "TL", DurationDays = 30, ListingsIncluded = 1, Description = "30 gün boyunca hem öne çıkan hem de vitrinde gösterin", DisplayOrder = 7 },
            new() { Name = "Silver Kombo", PackageType = "combo", Tier = "silver", Price = 449, Currency = "TL", DurationDays = 14, ListingsIncluded = 1, Description = "14 gün boyunca hem öne çıkan hem de vitrinde gösterin", DisplayOrder = 8 },
        };

        dbContext.PricingPackages.AddRange(packages);
        await dbContext.SaveChangesAsync();
    }
}

var supportedCultures = new[]
{
    new CultureInfo("tr"),
    new CultureInfo("en"),
    new CultureInfo("ru"),
    new CultureInfo("ar"),
    new CultureInfo("fa")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

localizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());

app.Use(async (context, next) =>
{
    if (context.Request.Query.TryGetValue("culture", out var cultureValue))
    {
        var requested = cultureValue.ToString();
        var isSupported = supportedCultures.Any(x => x.Name.Equals(requested, StringComparison.OrdinalIgnoreCase));

        if (isSupported)
        {
            var requestCulture = new RequestCulture(requested);
            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(requestCulture));
        }
    }

    await next();
});

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRequestLocalization(localizationOptions);
app.UseRouting();

// Simple request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestLogger");
    var cid = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? "-";
    logger.LogInformation("{Method} {Path} CID={CID}", context.Request.Method, context.Request.Path, cid);
    await next();
});

app.UseResponseCompression();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer-when-downgrade";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Permissions-Policy"] = "geolocation=()";
    // Basic CSP - adjust as needed for inline scripts/styles
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: https:; script-src 'self' 'unsafe-inline' https:; style-src 'self' 'unsafe-inline' https:;";
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

// Correlation ID for requests (helps tracing logs and errors)
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("X-Correlation-ID"))
    {
        var id = Guid.NewGuid().ToString("N");
        context.Request.Headers["X-Correlation-ID"] = id;
        context.Response.Headers["X-Correlation-ID"] = id;
    }
    else
    {
        context.Response.Headers["X-Correlation-ID"] = context.Request.Headers["X-Correlation-ID"].ToString();
    }
    await next();
});

// Serve uploaded files from configured external uploads root under the request path (e.g. /uploads)
var uploadStorage = app.Services.GetRequiredService<IUploadStorageService>();
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
// Allow only image types we expect
provider.Mappings.Clear();
provider.Mappings[".jpg"] = "image/jpeg";
provider.Mappings[".jpeg"] = "image/jpeg";
provider.Mappings[".png"] = "image/png";
provider.Mappings[".webp"] = "image/webp";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadStorage.RootPath),
    RequestPath = new PathString(uploadStorage.RequestPath),
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        // security headers for served files
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Context.Response.Headers["Referrer-Policy"] = "no-referrer-when-downgrade";
        // Cache images aggressively
        ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=2592000"; // 30 days
    }
});

app.MapControllerRoute(
    name: "category-tr",
    pattern: "kategori/{slug}",
    defaults: new { controller = "Home", action = "Category" });

app.MapControllerRoute(
    name: "category-en",
    pattern: "category/{slug}",
    defaults: new { controller = "Home", action = "Category" });

app.MapControllerRoute(
    name: "category-ru",
    pattern: "kategoriya/{slug}",
    defaults: new { controller = "Home", action = "Category" });

app.MapControllerRoute(
    name: "category-ar",
    pattern: "tasnif/{slug}",
    defaults: new { controller = "Home", action = "Category" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chatHub");

// Simple health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.Run();