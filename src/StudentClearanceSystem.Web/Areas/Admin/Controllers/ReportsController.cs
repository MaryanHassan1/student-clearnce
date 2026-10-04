using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Areas.Admin.ViewModels;
using StudentClearanceSystem.Web.Common;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models.Enums;
using StudentClearanceSystem.Web.Services;

namespace StudentClearanceSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ReportsController : Controller
{
    private const int PageSize = 15;

    private readonly ApplicationDbContext _context;
    private readonly IReportExportService _exportService;

    public ReportsController(ApplicationDbContext context, IReportExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Report(string key, string? search, int pageIndex = 1)
    {
        pageIndex = pageIndex < 1 ? 1 : pageIndex;
        var data = await RunReportAsync(key, search, pageIndex);
        if (data is null)
        {
            return NotFound();
        }

        var model = new ReportViewModel
        {
            Title = data.Value.Title,
            ReportKey = key,
            Headers = data.Value.Headers,
            Rows = new PaginatedList<string[]>(data.Value.Rows, data.Value.TotalCount, pageIndex, PageSize),
            Search = search
        };

        return View(model);
    }

    public async Task<IActionResult> Export(string key, string? search, string format)
    {
        var data = await RunReportAsync(key, search, pageIndex: null);
        if (data is null)
        {
            return NotFound();
        }

        if (format == "excel")
        {
            var content = _exportService.ExportToExcel(data.Value.Title, data.Value.Headers, data.Value.Rows);
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{key}.xlsx");
        }

        if (format == "pdf")
        {
            var content = _exportService.ExportToPdf(data.Value.Title, data.Value.Headers, data.Value.Rows);
            return File(content, "application/pdf", $"{key}.pdf");
        }

        return BadRequest("Unknown export format.");
    }

    private async Task<(string Title, List<string> Headers, List<string[]> Rows, int TotalCount)?> RunReportAsync(string key, string? search, int? pageIndex)
    {
        switch (key)
        {
            case "students":
            {
                var query = _context.Students
                    .Include(s => s.Faculty)
                    .Include(s => s.Department)
                    .Where(s => string.IsNullOrEmpty(search) || s.FullName.Contains(search!) || s.RegistrationNumber.Contains(search!))
                    .OrderBy(s => s.FullName);

                var total = await query.CountAsync();
                var entities = await MaterializeAsync(query, pageIndex);
                var headers = new List<string> { "Registration No.", "Full Name", "Faculty", "Department", "Program", "Academic Year", "Account Status" };
                var rows = entities.Select(s => new[]
                {
                    s.RegistrationNumber, s.FullName, s.Faculty?.Name ?? "", s.Department?.Name ?? "",
                    s.Program, s.AcademicYear, s.AccountStatus.ToString()
                }).ToList();

                return ("All Students", headers, rows, total);
            }
            case "pending":
            {
                var query = _context.ClearanceRequests
                    .Include(r => r.Student)
                    .Include(r => r.ClearanceItems)
                    .Where(r => r.Status == ClearanceRequestStatus.Pending)
                    .Where(r => string.IsNullOrEmpty(search) || r.Student!.FullName.Contains(search!) || r.Student!.RegistrationNumber.Contains(search!))
                    .OrderByDescending(r => r.RequestDate);

                var total = await query.CountAsync();
                var entities = await MaterializeAsync(query, pageIndex);
                var headers = new List<string> { "Registration No.", "Full Name", "Request Date", "Departments Approved" };
                var rows = entities.Select(r => new[]
                {
                    r.Student!.RegistrationNumber, r.Student!.FullName, r.RequestDate.ToString("d"),
                    $"{r.ClearanceItems.Count(i => i.Status == ClearanceItemStatus.Approved)} / {r.ClearanceItems.Count}"
                }).ToList();

                return ("Pending Clearance", headers, rows, total);
            }
            case "completed":
            {
                var query = _context.ClearanceRequests
                    .Include(r => r.Student)
                    .Where(r => r.Status == ClearanceRequestStatus.Completed)
                    .Where(r => string.IsNullOrEmpty(search) || r.Student!.FullName.Contains(search!) || r.Student!.RegistrationNumber.Contains(search!))
                    .OrderByDescending(r => r.CompletedDate);

                var total = await query.CountAsync();
                var entities = await MaterializeAsync(query, pageIndex);
                var headers = new List<string> { "Registration No.", "Full Name", "Request Date", "Completed Date" };
                var rows = entities.Select(r => new[]
                {
                    r.Student!.RegistrationNumber, r.Student!.FullName, r.RequestDate.ToString("d"),
                    r.CompletedDate.HasValue ? r.CompletedDate.Value.ToString("d") : "-"
                }).ToList();

                return ("Completed Clearance", headers, rows, total);
            }
            case "rejected":
            {
                var query = _context.ClearanceItems
                    .Include(i => i.ClearanceRequest)
                        .ThenInclude(r => r!.Student)
                    .Include(i => i.ClearanceDepartment)
                    .Where(i => i.Status == ClearanceItemStatus.Rejected)
                    .Where(i => string.IsNullOrEmpty(search) || i.ClearanceRequest!.Student!.FullName.Contains(search!) || i.ClearanceRequest!.Student!.RegistrationNumber.Contains(search!))
                    .OrderByDescending(i => i.ReviewedDate);

                var total = await query.CountAsync();
                var entities = await MaterializeAsync(query, pageIndex);
                var headers = new List<string> { "Registration No.", "Full Name", "Department", "Rejection Reason" };
                var rows = entities.Select(i => new[]
                {
                    i.ClearanceRequest!.Student!.RegistrationNumber, i.ClearanceRequest!.Student!.FullName,
                    i.ClearanceDepartment?.Name ?? "", i.RejectionReason ?? ""
                }).ToList();

                return ("Rejected Clearance", headers, rows, total);
            }
            case "graduation-approved":
            {
                var query = _context.GraduationRecords
                    .Include(g => g.Student)
                    .Where(g => g.Status == GraduationStatus.Approved)
                    .Where(g => string.IsNullOrEmpty(search) || g.Student!.FullName.Contains(search!) || g.Student!.RegistrationNumber.Contains(search!))
                    .OrderByDescending(g => g.GraduationApprovedDate);

                var total = await query.CountAsync();
                var entities = await MaterializeAsync(query, pageIndex);
                var headers = new List<string> { "Registration No.", "Full Name", "Graduation Approved Date" };
                var rows = entities.Select(g => new[]
                {
                    g.Student!.RegistrationNumber, g.Student!.FullName,
                    g.GraduationApprovedDate.HasValue ? g.GraduationApprovedDate.Value.ToString("d") : "-"
                }).ToList();

                return ("Graduation Approved Students", headers, rows, total);
            }
            case "graduated":
            {
                var query = _context.GraduationRecords
                    .Include(g => g.Student)
                    .Where(g => g.Status == GraduationStatus.Graduated)
                    .Where(g => string.IsNullOrEmpty(search) || g.Student!.FullName.Contains(search!) || g.Student!.RegistrationNumber.Contains(search!))
                    .OrderByDescending(g => g.GraduatedDate);

                var total = await query.CountAsync();
                var entities = await MaterializeAsync(query, pageIndex);
                var headers = new List<string> { "Registration No.", "Full Name", "Graduated Date" };
                var rows = entities.Select(g => new[]
                {
                    g.Student!.RegistrationNumber, g.Student!.FullName,
                    g.GraduatedDate.HasValue ? g.GraduatedDate.Value.ToString("d") : "-"
                }).ToList();

                return ("Graduated Students", headers, rows, total);
            }
            default:
                return null;
        }
    }

    private async Task<List<T>> MaterializeAsync<T>(IQueryable<T> query, int? pageIndex)
    {
        if (!pageIndex.HasValue)
        {
            return await query.ToListAsync();
        }

        return await query.Skip((pageIndex.Value - 1) * PageSize).Take(PageSize).ToListAsync();
    }
}
