using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;
using StudentClearanceSystem.Web.Models.Enums;
using StudentClearanceSystem.Web.Services;

namespace StudentClearanceSystem.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class ClearanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public ClearanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var student = await GetCurrentStudentAsync();
        if (student is null)
        {
            return NotFound();
        }

        var request = await _context.ClearanceRequests
            .Include(r => r.ClearanceItems)
                .ThenInclude(i => i.ClearanceDepartment)
            .Include(r => r.GraduationRecord)
            .Where(r => r.StudentId == student.StudentId)
            .OrderByDescending(r => r.RequestDate)
            .FirstOrDefaultAsync();

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestClearance()
    {
        var student = await GetCurrentStudentAsync();
        if (student is null)
        {
            return NotFound();
        }

        var existing = await _context.ClearanceRequests.AnyAsync(r => r.StudentId == student.StudentId);
        if (existing)
        {
            TempData["ErrorMessage"] = "A clearance request already exists for your account.";
            return RedirectToAction(nameof(Index));
        }

        var activeDepartments = await _context.ClearanceDepartments.Where(d => d.IsActive).ToListAsync();
        if (!activeDepartments.Any())
        {
            TempData["ErrorMessage"] = "No active clearance departments are configured. Please contact the administrator.";
            return RedirectToAction(nameof(Index));
        }

        var request = new ClearanceRequest
        {
            StudentId = student.StudentId,
            RequestDate = DateTime.Now,
            Status = ClearanceRequestStatus.Pending
        };

        foreach (var department in activeDepartments)
        {
            request.ClearanceItems.Add(new ClearanceItem
            {
                ClearanceDepartmentId = department.ClearanceDepartmentId,
                Status = ClearanceItemStatus.Pending
            });
        }

        _context.ClearanceRequests.Add(request);
        await _context.SaveChangesAsync();

        await _notificationService.NotifyAsync(student.UserId, "Your clearance request has been submitted and is now pending department review.");
        TempData["SuccessMessage"] = "Clearance request submitted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resubmit(int itemId)
    {
        var student = await GetCurrentStudentAsync();
        if (student is null)
        {
            return NotFound();
        }

        var item = await _context.ClearanceItems
            .Include(i => i.ClearanceRequest)
            .Include(i => i.ClearanceDepartment)
            .FirstOrDefaultAsync(i => i.ClearanceItemId == itemId && i.ClearanceRequest!.StudentId == student.StudentId);

        if (item is null)
        {
            return NotFound();
        }

        if (item.Status != ClearanceItemStatus.Rejected)
        {
            TempData["ErrorMessage"] = "Only rejected items can be resubmitted.";
            return RedirectToAction(nameof(Index));
        }

        item.Status = ClearanceItemStatus.Pending;
        item.RejectionReason = null;
        item.ReviewedByUserId = null;
        item.ReviewedDate = null;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Your {item.ClearanceDepartment!.Name} item has been resubmitted for review.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<StudentEntity?> GetCurrentStudentAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
    }
}
