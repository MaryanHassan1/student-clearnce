using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;
using StudentClearanceSystem.Web.Models.Enums;
using StudentClearanceSystem.Web.Services;

namespace StudentClearanceSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ClearanceRequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLog;

    public ClearanceRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService, IAuditLogService auditLog)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
        _auditLog = auditLog;
    }

    public async Task<IActionResult> Index(string? search, ClearanceRequestStatus? status, int pageIndex = 1)
    {
        var query = _context.ClearanceRequests
            .Include(r => r.Student)
            .Include(r => r.GraduationRecord)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Student!.FullName.Contains(search) || r.Student!.RegistrationNumber.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        query = query.OrderByDescending(r => r.RequestDate);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.StatusList = new SelectList(Enum.GetValues<ClearanceRequestStatus>());

        var requests = await PaginatedList<ClearanceRequest>.CreateAsync(query, pageIndex, 10);
        return View(requests);
    }

    public async Task<IActionResult> Review(int id)
    {
        var request = await _context.ClearanceRequests
            .Include(r => r.Student)
            .Include(r => r.ClearanceItems)
                .ThenInclude(i => i.ClearanceDepartment)
            .Include(r => r.GraduationRecord)
            .FirstOrDefaultAsync(r => r.ClearanceRequestId == id);

        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveItem(int itemId)
    {
        var item = await _context.ClearanceItems
            .Include(i => i.ClearanceRequest)
                .ThenInclude(r => r!.Student)
            .Include(i => i.ClearanceDepartment)
            .FirstOrDefaultAsync(i => i.ClearanceItemId == itemId);

        if (item is null)
        {
            return NotFound();
        }

        item.Status = ClearanceItemStatus.Approved;
        item.RejectionReason = null;
        item.ReviewedByUserId = _userManager.GetUserId(User);
        item.ReviewedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Approve", "ClearanceItem", item.ClearanceItemId, $"{item.ClearanceDepartment!.Name} approved for {item.ClearanceRequest!.Student!.FullName}");
        await _notificationService.NotifyAsync(item.ClearanceRequest!.Student!.UserId, $"{item.ClearanceDepartment!.Name} has approved your clearance.");

        TempData["SuccessMessage"] = $"{item.ClearanceDepartment!.Name} approved.";
        return RedirectToAction(nameof(Review), new { id = item.ClearanceRequestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectItem(int itemId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "A rejection reason is required.";
            var itemForRedirect = await _context.ClearanceItems.FindAsync(itemId);
            return RedirectToAction(nameof(Review), new { id = itemForRedirect?.ClearanceRequestId });
        }

        var item = await _context.ClearanceItems
            .Include(i => i.ClearanceRequest)
                .ThenInclude(r => r!.Student)
            .Include(i => i.ClearanceDepartment)
            .FirstOrDefaultAsync(i => i.ClearanceItemId == itemId);

        if (item is null)
        {
            return NotFound();
        }

        item.Status = ClearanceItemStatus.Rejected;
        item.RejectionReason = reason;
        item.ReviewedByUserId = _userManager.GetUserId(User);
        item.ReviewedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Reject", "ClearanceItem", item.ClearanceItemId, $"{item.ClearanceDepartment!.Name} rejected for {item.ClearanceRequest!.Student!.FullName}: {reason}");
        await _notificationService.NotifyAsync(item.ClearanceRequest!.Student!.UserId, $"{item.ClearanceDepartment!.Name} has rejected your clearance. Reason: {reason}");

        TempData["SuccessMessage"] = $"{item.ClearanceDepartment!.Name} rejected.";
        return RedirectToAction(nameof(Review), new { id = item.ClearanceRequestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteClearance(int id)
    {
        var request = await _context.ClearanceRequests
            .Include(r => r.Student)
            .Include(r => r.ClearanceItems)
            .FirstOrDefaultAsync(r => r.ClearanceRequestId == id);

        if (request is null)
        {
            return NotFound();
        }

        if (request.ClearanceItems.Any(i => i.Status != ClearanceItemStatus.Approved))
        {
            TempData["ErrorMessage"] = "All departments must approve before clearance can be completed.";
            return RedirectToAction(nameof(Review), new { id });
        }

        request.Status = ClearanceRequestStatus.Completed;
        request.CompletedDate = DateTime.Now;
        request.CompletedByUserId = _userManager.GetUserId(User);

        _context.GraduationRecords.Add(new GraduationRecord
        {
            StudentId = request.StudentId,
            ClearanceRequestId = request.ClearanceRequestId,
            Status = GraduationStatus.NotApproved
        });

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Complete", "ClearanceRequest", request.ClearanceRequestId, request.Student!.FullName);
        await _notificationService.NotifyAsync(request.Student!.UserId, "Your clearance has been completed by all departments.");

        TempData["SuccessMessage"] = "Clearance completed.";
        return RedirectToAction(nameof(Review), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveGraduation(int id)
    {
        var request = await _context.ClearanceRequests
            .Include(r => r.Student)
            .Include(r => r.GraduationRecord)
            .FirstOrDefaultAsync(r => r.ClearanceRequestId == id);

        if (request?.GraduationRecord is null)
        {
            return NotFound();
        }

        request.GraduationRecord.Status = GraduationStatus.Approved;
        request.GraduationRecord.GraduationApprovedDate = DateTime.Now;
        request.GraduationRecord.ApprovedByUserId = _userManager.GetUserId(User);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "ApproveGraduation", "GraduationRecord", request.GraduationRecord.GraduationRecordId, request.Student!.FullName);
        await _notificationService.NotifyAsync(request.Student!.UserId, "Your graduation has been approved.");

        TempData["SuccessMessage"] = "Graduation approved.";
        return RedirectToAction(nameof(Review), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsGraduated(int id)
    {
        var request = await _context.ClearanceRequests
            .Include(r => r.Student)
            .Include(r => r.GraduationRecord)
            .FirstOrDefaultAsync(r => r.ClearanceRequestId == id);

        if (request?.GraduationRecord is null)
        {
            return NotFound();
        }

        request.GraduationRecord.Status = GraduationStatus.Graduated;
        request.GraduationRecord.GraduatedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "MarkGraduated", "GraduationRecord", request.GraduationRecord.GraduationRecordId, request.Student!.FullName);
        await _notificationService.NotifyAsync(request.Student!.UserId, "Congratulations! You have been marked as graduated.");

        TempData["SuccessMessage"] = "Student marked as graduated.";
        return RedirectToAction(nameof(Review), new { id });
    }
}
