using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
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

        ViewBag.StudentName = student.FullName;
        return View(request);
    }
}
