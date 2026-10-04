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
public class ClearanceDepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClearanceDepartmentsController(ApplicationDbContext context, IAuditLogService auditLog, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _auditLog = auditLog;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var departments = await _context.ClearanceDepartments.OrderBy(d => d.Name).ToListAsync();
        return View(departments);
    }

    public IActionResult Create()
    {
        return View(new ClearanceDepartmentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClearanceDepartmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var department = new ClearanceDepartment
        {
            Name = model.Name,
            Description = model.Description,
            IsActive = model.IsActive
        };
        _context.ClearanceDepartments.Add(department);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Create", "ClearanceDepartment", department.ClearanceDepartmentId, department.Name);
        TempData["SuccessMessage"] = "Clearance department created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var department = await _context.ClearanceDepartments.FindAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        return View(new ClearanceDepartmentViewModel
        {
            ClearanceDepartmentId = department.ClearanceDepartmentId,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClearanceDepartmentViewModel model)
    {
        if (id != model.ClearanceDepartmentId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var department = await _context.ClearanceDepartments.FindAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        department.Name = model.Name;
        department.Description = model.Description;
        department.IsActive = model.IsActive;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Update", "ClearanceDepartment", department.ClearanceDepartmentId, department.Name);
        TempData["SuccessMessage"] = "Clearance department updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var department = await _context.ClearanceDepartments.FindAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        department.IsActive = !department.IsActive;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, department.IsActive ? "Activate" : "Deactivate", "ClearanceDepartment", department.ClearanceDepartmentId, department.Name);
        TempData["SuccessMessage"] = $"{department.Name} is now {(department.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }
}
