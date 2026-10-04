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
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var student = await _context.Students
            .Include(s => s.Faculty)
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student is null)
        {
            return NotFound();
        }

        return View(student);
    }
}
