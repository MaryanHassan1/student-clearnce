using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Areas.Admin.ViewModels;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;
using StudentClearanceSystem.Web.Models.Enums;
using StudentClearanceSystem.Web.Services;

namespace StudentClearanceSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class StudentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLog;
    private readonly IWebHostEnvironment _environment;

    public StudentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogService auditLog, IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _auditLog = auditLog;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string? search, int? facultyId, int? departmentId, int pageIndex = 1)
    {
        var query = _context.Students
            .Include(s => s.Faculty)
            .Include(s => s.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.FullName.Contains(search) || s.RegistrationNumber.Contains(search) || s.Email.Contains(search));
        }

        if (facultyId.HasValue)
        {
            query = query.Where(s => s.FacultyId == facultyId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(s => s.DepartmentId == departmentId.Value);
        }

        query = query.OrderBy(s => s.FullName);

        ViewBag.Search = search;
        ViewBag.FacultyId = facultyId;
        ViewBag.DepartmentId = departmentId;
        ViewBag.Faculties = new SelectList(await _context.Faculties.OrderBy(f => f.Name).ToListAsync(), "FacultyId", "Name", facultyId);
        ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DepartmentId", "Name", departmentId);

        var students = await PaginatedList<StudentEntity>.CreateAsync(query, pageIndex, 10);
        return View(students);
    }

    public async Task<IActionResult> Details(int id)
    {
        var student = await _context.Students
            .Include(s => s.Faculty)
            .Include(s => s.Department)
            .Include(s => s.ClearanceRequests)
                .ThenInclude(r => r.ClearanceItems)
                    .ThenInclude(i => i.ClearanceDepartment)
            .Include(s => s.ClearanceRequests)
                .ThenInclude(r => r.GraduationRecord)
            .FirstOrDefaultAsync(s => s.StudentId == id);

        if (student is null)
        {
            return NotFound();
        }

        return View(student);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new StudentCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentCreateViewModel model)
    {
        if (await _context.Students.AnyAsync(s => s.RegistrationNumber == model.RegistrationNumber))
        {
            ModelState.AddModelError(nameof(model.RegistrationNumber), "This registration number is already in use.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true,
            MustChangePassword = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            await PopulateLookupsAsync();
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, Roles.Student);

        var student = new StudentEntity
        {
            UserId = user.Id,
            RegistrationNumber = model.RegistrationNumber,
            FullName = model.FullName,
            Gender = model.Gender,
            Phone = model.Phone,
            Email = model.Email,
            FacultyId = model.FacultyId,
            DepartmentId = model.DepartmentId,
            Program = model.Program,
            AcademicYear = model.AcademicYear,
            AccountStatus = AccountStatus.Active,
            ProfilePicturePath = await SaveProfilePictureAsync(model.ProfilePicture)
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Create", "Student", student.StudentId, student.FullName);
        TempData["SuccessMessage"] = $"Student created successfully. Initial password: {model.Password}";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        await PopulateLookupsAsync();
        return View(new StudentEditViewModel
        {
            StudentId = student.StudentId,
            RegistrationNumber = student.RegistrationNumber,
            FullName = student.FullName,
            Gender = student.Gender,
            Phone = student.Phone,
            Email = student.Email,
            FacultyId = student.FacultyId,
            DepartmentId = student.DepartmentId,
            Program = student.Program,
            AcademicYear = student.AcademicYear,
            ExistingProfilePicturePath = student.ProfilePicturePath,
            AccountStatus = student.AccountStatus
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentEditViewModel model)
    {
        if (id != model.StudentId)
        {
            return NotFound();
        }

        if (await _context.Students.AnyAsync(s => s.RegistrationNumber == model.RegistrationNumber && s.StudentId != id))
        {
            ModelState.AddModelError(nameof(model.RegistrationNumber), "This registration number is already in use.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync();
            return View(model);
        }

        var student = await _context.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        student.RegistrationNumber = model.RegistrationNumber;
        student.FullName = model.FullName;
        student.Gender = model.Gender;
        student.Phone = model.Phone;
        student.Email = model.Email;
        student.FacultyId = model.FacultyId;
        student.DepartmentId = model.DepartmentId;
        student.Program = model.Program;
        student.AcademicYear = model.AcademicYear;
        student.AccountStatus = model.AccountStatus;

        if (model.ProfilePicture is not null)
        {
            student.ProfilePicturePath = await SaveProfilePictureAsync(model.ProfilePicture);
        }

        var user = await _userManager.FindByIdAsync(student.UserId);
        if (user is not null)
        {
            user.FullName = model.FullName;
            if (user.Email != model.Email)
            {
                user.Email = model.Email;
                user.UserName = model.Email;
                await _userManager.UpdateNormalizedEmailAsync(user);
                await _userManager.UpdateNormalizedUserNameAsync(user);
            }
            await _userManager.UpdateAsync(user);
        }

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Update", "Student", student.StudentId, student.FullName);
        TempData["SuccessMessage"] = "Student updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        student.AccountStatus = student.AccountStatus == AccountStatus.Active ? AccountStatus.Inactive : AccountStatus.Active;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, student.AccountStatus == AccountStatus.Active ? "Activate" : "Deactivate", "Student", student.StudentId, student.FullName);
        TempData["SuccessMessage"] = $"{student.FullName} is now {student.AccountStatus}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync()
    {
        var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
        var departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();

        ViewBag.Faculties = new SelectList(faculties, "FacultyId", "Name");
        ViewBag.Departments = new SelectList(departments, "DepartmentId", "Name");
        ViewBag.HasFaculties = faculties.Any();
        ViewBag.HasDepartments = departments.Any();
    }

    private async Task<string?> SaveProfilePictureAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profile-pictures");
        Directory.CreateDirectory(uploadsFolder);
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/profile-pictures/{fileName}";
    }
}
