using StudentClearanceSystem.Web.Models.Enums;

namespace StudentClearanceSystem.Web.Models;

public class ClearanceRequest
{
    public int ClearanceRequestId { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public DateTime RequestDate { get; set; }

    public ClearanceRequestStatus Status { get; set; } = ClearanceRequestStatus.Pending;

    public DateTime? CompletedDate { get; set; }

    public string? CompletedByUserId { get; set; }
    public ApplicationUser? CompletedByUser { get; set; }

    public ICollection<ClearanceItem> ClearanceItems { get; set; } = new List<ClearanceItem>();

    public GraduationRecord? GraduationRecord { get; set; }
}
