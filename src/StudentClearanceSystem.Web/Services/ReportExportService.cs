using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace StudentClearanceSystem.Web.Services;

public class ReportExportService : IReportExportService
{
    public byte[] ExportToExcel(string title, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(title);

        for (var col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var col = 0; col < row.Count; col++)
            {
                sheet.Cell(rowIndex, col + 1).Value = row[col];
            }
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf(string title, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var rowList = rows.ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Text(title).SemiBold().FontSize(16);

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in headers)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(4).Text(h).Bold();
                        }
                    });

                    foreach (var row in rowList)
                    {
                        foreach (var value in row)
                        {
                            table.Cell().Border(1).Padding(4).Text(value);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
