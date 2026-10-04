using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;
using StudentClearanceSystem.Web.Models.Enums;
using StudentClearanceSystem.Web.Models.ViewModels;

namespace StudentClearanceSystem.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToLocalRole();
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var studentAccountStatus = await _context.Students
            .Where(s => s.UserId == user.Id)
            .Select(s => (AccountStatus?)s.AccountStatus)
            .FirstOrDefaultAsync();

        if (studentAccountStatus == AccountStatus.Inactive)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        if (user.MustChangePassword)
        {
            return RedirectToAction(nameof(Settings));
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToLocalRole();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new SettingsViewModel
        {
            FullName = user.FullName,
            UpdateEmail = new UpdateEmailViewModel { Email = user.Email! }
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmail([Bind(Prefix = "UpdateEmail")] UpdateEmailViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Settings), new SettingsViewModel { FullName = user.FullName, UpdateEmail = model });
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing is not null && existing.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.Email), "This email address is already in use.");
            return View(nameof(Settings), new SettingsViewModel { FullName = user.FullName, UpdateEmail = model });
        }

        user.Email = model.Email;
        user.UserName = model.Email;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(nameof(Settings), new SettingsViewModel { FullName = user.FullName, UpdateEmail = model });
        }

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student is not null)
        {
            student.Email = model.Email;
            await _context.SaveChangesAsync();
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Your email address has been updated.";
        return RedirectToAction(nameof(Settings));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Settings), new SettingsViewModel { FullName = user.FullName, UpdateEmail = new UpdateEmailViewModel { Email = user.Email! }, ChangePassword = model });
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(nameof(Settings), new SettingsViewModel { FullName = user.FullName, UpdateEmail = new UpdateEmailViewModel { Email = user.Email! }, ChangePassword = model });
        }

        var wasForced = user.MustChangePassword;
        if (wasForced)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Your password has been changed.";

        return wasForced ? RedirectToLocalRole() : RedirectToAction(nameof(Settings));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocalRole()
    {
        if (User.IsInRole(Roles.Admin))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Student" });
    }
}
