namespace StudentClearanceSystem.Web.Services;

public interface IReportExportService
{
    byte[] ExportToExcel(string title, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows);
    byte[] ExportToPdf(string title, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows);
}
