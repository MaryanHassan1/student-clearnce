using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string userId, string action, string entityName, int? entityId = null, string? details = null)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }
}
