using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            if (User.IsInRole(Roles.Admin))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if (User.IsInRole(Roles.Student))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Student" });
            }
        }

        return RedirectToAction("Login", "Account");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
