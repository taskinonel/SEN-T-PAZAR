
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using SEN_T_PAZAR.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;



var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<IListingCatalogService, ListingCatalogService>();

// Identity ve DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var resolvedConnectionString = string.IsNullOrWhiteSpace(connectionString)
    ? "Data Source=sent-pazar.db"
    : connectionString;
var useSqlite = resolvedConnectionString.Contains(".db", StringComparison.OrdinalIgnoreCase) ||
    resolvedConnectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<SEN_T_PAZAR.Models.ApplicationDbContext>(options =>
{
    if (useSqlite)
    {
        options.UseSqlite(resolvedConnectionString);
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


var app = builder.Build();

// Otomatik admin rolü ve kullanıcıya atama
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SEN_T_PAZAR.Models.ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<SEN_T_PAZAR.Models.ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (dbContext.Database.IsSqlite())
        await dbContext.Database.EnsureCreatedAsync();
    else
        dbContext.Database.Migrate();

    // Admin rolü yoksa oluştur
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    // Admin yapmak istediğiniz e-posta adresini buraya yazın
    var adminEmail = "taskinonel@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
        await userManager.AddToRoleAsync(adminUser, "Admin");
}

var supportedCultures = new[]
{
    new CultureInfo("tr"),
    new CultureInfo("en"),
    new CultureInfo("ru"),
    new CultureInfo("ar")
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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization(localizationOptions);
app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "category-tr",
    pattern: "kategori/{slug}",
    defaults: new { controller = "Home", action = "Category" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "category-en",
    pattern: "category/{slug}",
    defaults: new { controller = "Home", action = "Category" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "category-ru",
    pattern: "kategoriya/{slug}",
    defaults: new { controller = "Home", action = "Category" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "category-ar",
    pattern: "tasnif/{slug}",
	defaults: new { controller = "Home", action = "Category" })
    .WithStaticAssets();

app.UseAuthentication();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
