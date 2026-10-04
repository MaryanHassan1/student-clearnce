using StudentClearanceSystem.Web.Common;

namespace StudentClearanceSystem.Web.Areas.Admin.ViewModels;

public class ReportViewModel
{
    public string Title { get; set; } = string.Empty;
    public string ReportKey { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public PaginatedList<string[]> Rows { get; set; } = null!;
    public string? Search { get; set; }
}
