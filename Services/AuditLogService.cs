using Microsoft.AspNetCore.Identity;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Services;

public interface IAuditLogService
{
    Task LogAdminActionAsync(HttpContext httpContext, ApplicationUser? actor, string action, string targetType, string? targetId = null, string? details = null, CancellationToken cancellationToken = default);
}

public sealed class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;

    public AuditLogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogAdminActionAsync(HttpContext httpContext, ApplicationUser? actor, string action, string targetType, string? targetId = null, string? details = null, CancellationToken cancellationToken = default)
    {
        if (actor == null)
        {
            return;
        }

        var audit = new AdminAuditLog
        {
            ActorUserId = actor.Id,
            ActorEmail = actor.Email ?? actor.UserName ?? actor.Id,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Set<AdminAuditLog>().Add(audit);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
