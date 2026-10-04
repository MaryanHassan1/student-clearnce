using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentClearanceSystem.Web.Areas.Admin.ViewModels;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Models;
using StudentClearanceSystem.Web.Services;

namespace StudentClearanceSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLog;

    public UsersController(UserManager<ApplicationUser> userManager, IAuditLogService auditLog)
    {
        _userManager = userManager;
        _auditLog = auditLog;
    }

    public async Task<IActionResult> Index()
    {
        var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
        return View(admins.OrderBy(u => u.FullName).ToList());
    }

    public IActionResult Create()
    {
        return View(new AdminUserCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
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
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, Roles.Admin);
        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "Create", "AdminUser", null, user.Email);

        TempData["SuccessMessage"] = "Admin user created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new ResetPasswordViewModel { UserId = user.Id, FullName = user.FullName });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            return NotFound();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        user.MustChangePassword = true;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAsync(_userManager.GetUserId(User)!, "ResetPassword", "AdminUser", null, user.Email);
        TempData["SuccessMessage"] = "Password reset successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
        {
            TempData["ErrorMessage"] = "You cannot lock your own account.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var isLockedOut = await _userManager.IsLockedOutAsync(user);
        if (isLockedOut)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            user.LockoutEnabled = true;
            await _userManager.UpdateAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }

        await _auditLog.LogAsync(currentUserId!, isLockedOut ? "Unlock" : "Lock", "AdminUser", null, user.Email);
        TempData["SuccessMessage"] = $"{user.FullName} is now {(isLockedOut ? "unlocked" : "locked")}.";
        return RedirectToAction(nameof(Index));
    }
}
