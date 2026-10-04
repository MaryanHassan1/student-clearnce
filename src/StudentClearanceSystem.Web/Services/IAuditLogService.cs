namespace StudentClearanceSystem.Web.Services;

public interface IAuditLogService
{
    Task LogAsync(string userId, string action, string entityName, int? entityId = null, string? details = null);
}
