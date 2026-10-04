using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [StringLength(500)]
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; }
}
