using StudentClearanceSystem.Web.Models.Enums;

namespace StudentClearanceSystem.Web.Models;

public class GraduationRecord
{
    public int GraduationRecordId { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int ClearanceRequestId { get; set; }
    public ClearanceRequest? ClearanceRequest { get; set; }

    public GraduationStatus Status { get; set; } = GraduationStatus.NotApproved;

    public DateTime? GraduationApprovedDate { get; set; }
    public DateTime? GraduatedDate { get; set; }

    public string? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedByUser { get; set; }
}
