using System.ComponentModel.DataAnnotations;
using StudentClearanceSystem.Web.Models.Enums;

namespace StudentClearanceSystem.Web.Models;

public class ClearanceItem
{
    public int ClearanceItemId { get; set; }

    public int ClearanceRequestId { get; set; }
    public ClearanceRequest? ClearanceRequest { get; set; }

    public int ClearanceDepartmentId { get; set; }
    public ClearanceDepartment? ClearanceDepartment { get; set; }

    public ClearanceItemStatus Status { get; set; } = ClearanceItemStatus.Pending;

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }

    public DateTime? ReviewedDate { get; set; }
}
