using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Areas.Admin.ViewModels;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models.Enums;

namespace StudentClearanceSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            TotalStudents = await _context.Students.CountAsync(),
            PendingClearanceRequests = await _context.ClearanceRequests.CountAsync(r => r.Status == ClearanceRequestStatus.Pending),
            CompletedClearances = await _context.ClearanceRequests.CountAsync(r => r.Status == ClearanceRequestStatus.Completed),
            RejectedClearances = await _context.ClearanceRequests.CountAsync(r => r.ClearanceItems.Any(i => i.Status == ClearanceItemStatus.Rejected)),
            GraduationApproved = await _context.GraduationRecords.CountAsync(g => g.Status == GraduationStatus.Approved),
            GraduatedStudents = await _context.GraduationRecords.CountAsync(g => g.Status == GraduationStatus.Graduated),
            RecentRequests = await _context.ClearanceRequests
                .Include(r => r.Student)
                .Include(r => r.GraduationRecord)
                .OrderByDescending(r => r.RequestDate)
                .Take(10)
                .ToListAsync()
        };

        return View(model);
    }
}
