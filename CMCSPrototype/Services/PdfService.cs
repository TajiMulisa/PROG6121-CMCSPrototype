using CMCSPrototype.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using Document = QuestPDF.Fluent.Document;

namespace CMCSPrototype.Services
{
    public class PdfService : IPdfService
    {
        public byte[] GenerateReportPdf(List<Claim> claims, Dictionary<string, decimal> summary)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Header().Element(header => ComposeHeader(header));
                    page.Content().Element(content => ComposeContent(content, claims, summary));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf();
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Claims Report").Bold().FontSize(20);
                    column.Item().Text($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm}");
                });
            });
        }

        private void ComposeContent(IContainer container, List<Claim> claims, Dictionary<string, decimal> summary)
        {
            container.Column(column =>
            {
                column.Spacing(20);

                column.Item().Element(table => ComposeClaimsTable(table, claims));
                column.Item().Element(table => ComposeSummaryTable(table, summary));
            });
        }

        private void ComposeClaimsTable(IContainer container, List<Claim> claims)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("ID");
                    header.Cell().Text("Lecturer");
                    header.Cell().Text("Hours");
                    header.Cell().Text("Rate");
                    header.Cell().Text("Total");
                });

                foreach (var claim in claims)
                {
                    table.Cell().Text($"#{claim.Id}");
                    table.Cell().Text(claim.LecturerName);
                    table.Cell().Text($"{claim.HoursWorked} hrs");
                    table.Cell().Text($"{claim.HourlyRate:C}");
                    table.Cell().Text($"{claim.TotalAmount:C}");
                }
            });
        }

        private void ComposeSummaryTable(IContainer container, Dictionary<string, decimal> summary)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Lecturer Payment Summary").Bold();
                    header.Cell().Text("");
                });
                
                foreach (var item in summary.OrderByDescending(x => x.Value))
                {
                    table.Cell().Text(item.Key);
                    table.Cell().Text($"{item.Value:C}");
                }

                table.Footer(footer =>
                {
                    footer.Cell().Text("Grand Total").Bold();
                    footer.Cell().Text($"{summary.Values.Sum():C}").Bold();
                });
            });
        }
    }
}
