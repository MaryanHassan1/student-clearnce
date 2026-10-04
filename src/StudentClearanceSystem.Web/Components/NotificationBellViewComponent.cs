using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Components;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationBellViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        if (userId is null)
        {
            return View(new List<Notification>());
        }

        var notifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .Take(10)
            .ToListAsync();

        return View(notifications);
    }
}
