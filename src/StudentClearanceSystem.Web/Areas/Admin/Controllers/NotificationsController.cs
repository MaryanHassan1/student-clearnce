using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int pageIndex = 1)
    {
        var query = _context.Notifications
            .Include(n => n.RecipientUser)
            .OrderByDescending(n => n.CreatedDate)
            .AsQueryable();

        var notifications = await PaginatedList<Notification>.CreateAsync(query, pageIndex, 20);
        return View(notifications);
    }
}
