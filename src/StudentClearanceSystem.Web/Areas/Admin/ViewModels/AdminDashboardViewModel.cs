using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Areas.Admin.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalStudents { get; set; }
    public int PendingClearanceRequests { get; set; }
    public int CompletedClearances { get; set; }
    public int RejectedClearances { get; set; }
    public int GraduationApproved { get; set; }
    public int GraduatedStudents { get; set; }
    public List<ClearanceRequest> RecentRequests { get; set; } = new();
}
