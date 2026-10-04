using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Areas.Admin.ViewModels;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;
using StudentClearanceSystem.Web.Services;

namespace StudentClearanceSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly UserManager<ApplicationUser> _userManager;

    public DepartmentsController(ApplicationDbContext context, IAuditLogService auditLog, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _auditLog = auditLog;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var departments = await _context.Departments
            .Include(d => d.Faculty)
            .OrderBy(d => d.Name)
            .ToListAsync();
        return View(departments);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateFacultiesAsync();
        return View(new DepartmentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFacultiesAsync();
            return View(model);
        }

        var department = new Department { Name = model.Name, FacultyId = model.FacultyId };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Create", "Department", department.DepartmentId, department.Name);
        TempData["SuccessMessage"] = "Department created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        await PopulateFacultiesAsync();
        return View(new DepartmentViewModel { DepartmentId = department.DepartmentId, Name = department.Name, FacultyId = department.FacultyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentViewModel model)
    {
        if (id != model.DepartmentId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateFacultiesAsync();
            return View(model);
        }

        var department = await _context.Departments.FindAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        department.Name = model.Name;
        department.FacultyId = model.FacultyId;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Update", "Department", department.DepartmentId, department.Name);
        TempData["SuccessMessage"] = "Department updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        var inUse = await _context.Students.AnyAsync(s => s.DepartmentId == id);
        if (inUse)
        {
            TempData["ErrorMessage"] = "This department cannot be deleted because it is in use by students.";
            return RedirectToAction(nameof(Index));
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Delete", "Department", id, department.Name);
        TempData["SuccessMessage"] = "Department deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateFacultiesAsync()
    {
        ViewBag.Faculties = new SelectList(await _context.Faculties.OrderBy(f => f.Name).ToListAsync(), "FacultyId", "Name");
    }
}
