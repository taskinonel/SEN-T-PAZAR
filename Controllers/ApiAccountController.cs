using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SEN_T_PAZAR.Models;
using SEN_T_PAZAR.Services;

namespace SEN_T_PAZAR.Controllers;

[ApiController]
[Route("api/Account")]
[Route("api/v1/Account")]
public sealed class AccountControllerApi : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _db;
    private readonly IUserMessageAutomationService _userMessageAutomationService;
    private readonly ILogger<AccountControllerApi> _logger;

    public AccountControllerApi(UserManager<ApplicationUser> userManager, IConfiguration configuration, ApplicationDbContext db, IUserMessageAutomationService userMessageAutomationService, ILogger<AccountControllerApi> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _db = db;
        _userMessageAutomationService = userMessageAutomationService;
        _logger = logger;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var input = request.UserNameOrEmail.Trim();
        ApplicationUser? user;
        if (input.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(input);
        }
        else
        {
            user = await _userManager.FindByNameAsync(input);
        }

        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { error = "Kullanıcı adı/e-posta veya şifre hatalı." });
        }

        var tokenResult = BuildToken(user);
        if (tokenResult == null)
        {
            return StatusCode(500, new { error = "JWT ayarları eksik. Jwt:Key, Jwt:Issuer, Jwt:Audience yapılandırılmalı." });
        }

        return Ok(new MobileAuthResponse
        {
            Token = tokenResult.Value.Token,
            ExpiresAtUtc = tokenResult.Value.ExpiresAtUtc,
            User = new MobileUserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty
            }
        });
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] MobileRegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var email = request.Email.Trim();
        var userName = request.UserName.Trim();

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            return Conflict(new { error = "Bu e-posta adresi zaten kayıtlı." });
        }

        if (await _userManager.FindByNameAsync(userName) != null)
        {
            return Conflict(new { error = "Bu kullanıcı adı zaten kullanılıyor." });
        }

        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName,
            FullName = request.FullName.Trim(),
            EmailConfirmed = true
        };

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            return BadRequest(new
            {
                errors = create.Errors.Select(x => x.Description).ToList()
            });
        }

        await _userMessageAutomationService.SendWelcomeAsync(user);

        var tokenResult = BuildToken(user);
        if (tokenResult == null)
        {
            return StatusCode(500, new { error = "JWT ayarları eksik. Jwt:Key, Jwt:Issuer, Jwt:Audience yapılandırılmalı." });
        }

        return Ok(new MobileAuthResponse
        {
            Token = tokenResult.Value.Token,
            ExpiresAtUtc = tokenResult.Value.ExpiresAtUtc,
            User = new MobileUserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty
            }
        });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("DeviceToken")]
    public async Task<IActionResult> SetDeviceToken([FromBody] MobileDeviceTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Kullanıcı bilgisi bulunamadı." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { error = "Kullanıcı bulunamadı." });
        }

        user.FcmToken = request.FcmToken.Trim();
        user.FcmTokenUpdatedAtUtc = DateTime.UtcNow;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return BadRequest(new { errors = update.Errors.Select(x => x.Description).ToList() });
        }

        return Ok(new { success = true });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("Profile")]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Kullanıcı bilgisi bulunamadı." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { error = "Kullanıcı bulunamadı." });
        }

        return Ok(new MobileProfileDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            City = user.City,
            EmailNotifications = user.EmailNotifications
        });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("MyAds")]
    public async Task<IActionResult> MyAds()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Kullanıcı bilgisi bulunamadı." });
        }

        var items = await _db.Listings
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new MobileMyAdDto
            {
                Id = x.Id,
                Title = x.Title,
                Price = x.PriceAmount,
                PriceCurrency = x.PriceCurrency,
                Category = x.Category,
                ListingType = x.Type,
                Status = x.IsApproved ? (x.IsClosed ? "closed" : "active") : "pending",
                CreatedAtUtc = x.CreatedAt,
                ImageUrl = AbsoluteImageUrl(_db.ListingImages
                    .Where(i => i.ListingId == x.Id)
                    .OrderBy(i => i.Id)
                    .Select(i => i.FilePath)
                    .FirstOrDefault())
            })
            .ToListAsync();

        return Ok(items);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("Messages")]
    public async Task<IActionResult> Messages()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Kullanıcı bilgisi bulunamadı." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { error = "Kullanıcı bulunamadı." });
        }

        var rows = await _db.VisitorMessages
            .AsNoTracking()
            .Where(x =>
                (!string.IsNullOrWhiteSpace(x.RecipientUserId) && x.RecipientUserId == user.Id) ||
                (!string.IsNullOrWhiteSpace(x.RecipientPhone) && x.RecipientPhone == user.PhoneNumber) ||
                (!string.IsNullOrWhiteSpace(x.RecipientEmail) && x.RecipientEmail == user.Email) ||
                (!string.IsNullOrWhiteSpace(x.SenderUserId) && x.SenderUserId == user.Id))
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ListingId,
                x.ConversationId,
                ListingTitle = _db.Listings.Where(l => l.Id == x.ListingId).Select(l => l.Title).FirstOrDefault(),
                x.SenderName,
                x.SenderEmail,
                x.SenderPhone,
                x.Subject,
                x.Message,
                x.CreatedAtUtc,
                x.IsRead,
                x.SenderRole
            })
            .ToListAsync();

        var threads = rows
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ConversationId) ? $"legacy-{x.Id}" : x.ConversationId)
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.CreatedAtUtc).ToList();
                var root = ordered.First();
                return new MobileMessageThreadDto
                {
                    ConversationId = group.Key,
                    RootMessageId = root.Id,
                    ListingId = root.ListingId,
                    ListingTitle = root.ListingId == 0 ? "Sistem mesajı" : root.ListingTitle ?? $"Ilan #{root.ListingId}",
                    Subject = root.Subject,
                    SenderName = root.SenderName,
                    SenderEmail = root.SenderEmail,
                    SenderPhone = root.SenderPhone,
                    IsRead = ordered.All(x => x.IsRead),
                    CreatedAtUtc = ordered.Last().CreatedAtUtc,
                    Messages = ordered.Select(x => new MobileMessageEntryDto
                    {
                        Id = x.Id,
                        SenderName = x.SenderName,
                        SenderRole = x.SenderRole,
                        Message = x.Message,
                        CreatedAtUtc = x.CreatedAtUtc
                    }).ToList()
                };
            })
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        _logger.LogInformation("Mobile messages fetched for user {UserId}. Thread count: {ThreadCount}", user.Id, threads.Count);
        return Ok(threads);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("Messages/Reply")]
    public async Task<IActionResult> ReplyMessage([FromBody] MobileReplyMessageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Kullanıcı bilgisi bulunamadı." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { error = "Kullanıcı bulunamadı." });
        }

        var rootMessage = await _db.VisitorMessages.FirstOrDefaultAsync(x => x.Id == request.MessageId);
        if (rootMessage == null)
        {
            return NotFound(new { error = "Mesaj bulunamadı." });
        }

        var canAccess = (!string.IsNullOrWhiteSpace(rootMessage.RecipientUserId) && rootMessage.RecipientUserId == user.Id)
            || (!string.IsNullOrWhiteSpace(rootMessage.RecipientPhone) && rootMessage.RecipientPhone == user.PhoneNumber)
            || (!string.IsNullOrWhiteSpace(rootMessage.RecipientEmail) && rootMessage.RecipientEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(rootMessage.SenderUserId) && rootMessage.SenderUserId == user.Id);
        if (!canAccess)
        {
            _logger.LogWarning("Reply denied for user {UserId} on message {MessageId}", user.Id, request.MessageId);
            return Forbid();
        }

        var conversationId = string.IsNullOrWhiteSpace(rootMessage.ConversationId) ? $"legacy-{rootMessage.Id}" : rootMessage.ConversationId;
        var reply = new VisitorMessage
        {
            ListingId = rootMessage.ListingId,
            ConversationId = conversationId,
            RecipientUserId = rootMessage.SenderUserId,
            RecipientPhone = rootMessage.SenderPhone,
            RecipientEmail = rootMessage.SenderEmail,
            SenderUserId = user.Id,
            SenderName = user.FullName ?? user.UserName ?? string.Empty,
            SenderEmail = user.Email ?? string.Empty,
            SenderPhone = user.PhoneNumber,
            SenderRole = "owner",
            Subject = rootMessage.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? rootMessage.Subject : $"Re: {rootMessage.Subject}",
            Message = request.Message.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            IsRead = false
        };

        _db.VisitorMessages.Add(reply);
        rootMessage.IsRead = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Reply created by user {UserId} in conversation {ConversationId}", user.Id, reply.ConversationId);

        return Ok(new { success = true, conversationId = reply.ConversationId, id = reply.Id });
    }

    private (string Token, DateTime ExpiresAtUtc)? BuildToken(ApplicationUser user)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 16)
        {
            return null;
        }

        var issuer = _configuration["Jwt:Issuer"] ?? "sentpazar.com";
        var audience = _configuration["Jwt:Audience"] ?? "sen-t-mobile";
        var expiresMinutes = Math.Clamp(_configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60 * 24, 15, 60 * 24 * 30);

        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private string AbsoluteImageUrl(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return $"{Request.Scheme}://{Request.Host}/img/logo.png";
        }

        if (Uri.TryCreate(rawPath, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        var normalized = rawPath.StartsWith('/') ? rawPath : "/" + rawPath;
        return $"{Request.Scheme}://{Request.Host}{normalized}";
    }
}
