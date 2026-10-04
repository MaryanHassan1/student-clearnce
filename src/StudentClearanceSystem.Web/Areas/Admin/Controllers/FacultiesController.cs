using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Areas.Admin.ViewModels;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;
using StudentClearanceSystem.Web.Services;

namespace StudentClearanceSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class FacultiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly UserManager<ApplicationUser> _userManager;

    public FacultiesController(ApplicationDbContext context, IAuditLogService auditLog, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _auditLog = auditLog;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
        return View(faculties);
    }

    public IActionResult Create()
    {
        return View(new FacultyViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacultyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var faculty = new Faculty { Name = model.Name };
        _context.Faculties.Add(faculty);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Create", "Faculty", faculty.FacultyId, faculty.Name);
        TempData["SuccessMessage"] = "Faculty created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty is null)
        {
            return NotFound();
        }

        return View(new FacultyViewModel { FacultyId = faculty.FacultyId, Name = faculty.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FacultyViewModel model)
    {
        if (id != model.FacultyId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty is null)
        {
            return NotFound();
        }

        faculty.Name = model.Name;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Update", "Faculty", faculty.FacultyId, faculty.Name);
        TempData["SuccessMessage"] = "Faculty updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty is null)
        {
            return NotFound();
        }

        var inUse = await _context.Departments.AnyAsync(d => d.FacultyId == id) ||
                    await _context.Students.AnyAsync(s => s.FacultyId == id);
        if (inUse)
        {
            TempData["ErrorMessage"] = "This faculty cannot be deleted because it is in use by departments or students.";
            return RedirectToAction(nameof(Index));
        }

        _context.Faculties.Remove(faculty);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Delete", "Faculty", id, faculty.Name);
        TempData["SuccessMessage"] = "Faculty deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
