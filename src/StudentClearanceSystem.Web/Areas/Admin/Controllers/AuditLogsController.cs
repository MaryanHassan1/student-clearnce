using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int pageIndex = 1)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .AsQueryable();

        var logs = await PaginatedList<AuditLog>.CreateAsync(query, pageIndex, 25);
        return View(logs);
    }
}
