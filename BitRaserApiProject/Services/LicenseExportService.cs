using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DSecureApi.Services
{
    /// <summary>
    /// License export data model
    /// </summary>
    public class LicenseExportData
    {
        public int Id { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public string? Hwid { get; set; }
        public string? ExpiryDate { get; set; }
        public string Edition { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    /// <summary>
    /// Service interface for license export
    /// </summary>
    public interface ILicenseExportService
    {
        byte[] ExportToExcel(List<LicenseExportData> licenses);
        byte[] ExportToPdf(List<LicenseExportData> licenses);
    }

    /// <summary>
    /// License export service - generates Excel and PDF exports
    /// </summary>
    public class LicenseExportService : ILicenseExportService
    {
        private readonly ILogger<LicenseExportService> _logger;

        public LicenseExportService(ILogger<LicenseExportService> logger)
        {
            _logger = logger;
            
            // Set QuestPDF license (community edition - free)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Export licenses to Excel using ClosedXML
        /// </summary>
        public byte[] ExportToExcel(List<LicenseExportData> licenses)
        {
            _logger.LogInformation("📊 Generating Excel export for {Count} licenses", licenses.Count);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Licenses");

            // Header row with styling
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#10b981"); // DSecure teal
            headerRow.Style.Font.FontColor = XLColor.White;

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "License Key";
            worksheet.Cell(1, 3).Value = "HWID";
            worksheet.Cell(1, 4).Value = "Expiry Date";
            worksheet.Cell(1, 5).Value = "Edition";
            worksheet.Cell(1, 6).Value = "Status";
            worksheet.Cell(1, 7).Value = "User Email";
            worksheet.Cell(1, 8).Value = "Created At";
            worksheet.Cell(1, 9).Value = "Last Seen";

            // Data rows
            for (int i = 0; i < licenses.Count; i++)
            {
                var license = licenses[i];
                var row = i + 2; // Start from row 2

                worksheet.Cell(row, 1).Value = license.Id;
                worksheet.Cell(row, 2).Value = license.LicenseKey;
                worksheet.Cell(row, 3).Value = license.Hwid ?? "N/A";
                worksheet.Cell(row, 4).Value = license.ExpiryDate ?? "N/A";
                worksheet.Cell(row, 5).Value = license.Edition;
                worksheet.Cell(row, 6).Value = license.Status;
                worksheet.Cell(row, 7).Value = license.UserEmail ?? "N/A";
                worksheet.Cell(row, 8).Value = license.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
                worksheet.Cell(row, 9).Value = license.LastSeen?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";

                // Color-code status
                var statusCell = worksheet.Cell(row, 6);
                switch (license.Status.ToUpperInvariant())
                {
                    case "ACTIVE":
                        statusCell.Style.Font.FontColor = XLColor.FromHtml("#10b981");
                        break;
                    case "EXPIRED":
                        statusCell.Style.Font.FontColor = XLColor.FromHtml("#f59e0b");
                        break;
                    case "REVOKED":
                        statusCell.Style.Font.FontColor = XLColor.FromHtml("#ef4444");
                        break;
                }
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add a summary sheet
            var summarySheet = workbook.Worksheets.Add("Summary");
            summarySheet.Cell(1, 1).Value = "DSecure License Report";
            summarySheet.Cell(1, 1).Style.Font.Bold = true;
            summarySheet.Cell(1, 1).Style.Font.FontSize = 16;
            
            summarySheet.Cell(3, 1).Value = "Generated:";
            summarySheet.Cell(3, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            
            summarySheet.Cell(4, 1).Value = "Total Licenses:";
            summarySheet.Cell(4, 2).Value = licenses.Count;
            
            summarySheet.Cell(5, 1).Value = "Active:";
            summarySheet.Cell(5, 2).Value = licenses.Count(l => l.Status == "ACTIVE");
            
            summarySheet.Cell(6, 1).Value = "Expired:";
            summarySheet.Cell(6, 2).Value = licenses.Count(l => l.Status == "EXPIRED");
            
            summarySheet.Cell(7, 1).Value = "Revoked:";
            summarySheet.Cell(7, 2).Value = licenses.Count(l => l.Status == "REVOKED");
            
            summarySheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            
            _logger.LogInformation("✅ Excel export generated successfully");
            return stream.ToArray();
        }

        /// <summary>
        /// Export licenses to PDF using QuestPDF
        /// </summary>
        public byte[] ExportToPdf(List<LicenseExportData> licenses)
        {
            _logger.LogInformation("📄 Generating PDF export for {Count} licenses", licenses.Count);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header().Element(ComposeHeader);

                    // Content
                    page.Content().Element(c => ComposeContent(c, licenses));

                    // Footer
                    page.Footer().Element(ComposeFooter);
                });
            });

            var pdfBytes = document.GeneratePdf();
            
            _logger.LogInformation("✅ PDF export generated successfully");
            return pdfBytes;
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("DSecure")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Teal.Medium);
                    
                    column.Item().Text("License Database Export")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(150).Column(column =>
                {
                    column.Item().AlignRight().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);
                    
                    column.Item().AlignRight().Text($"Time: {DateTime.UtcNow:HH:mm:ss} UTC")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);
                });
            });

            container.PaddingBottom(15);
        }

        private void ComposeContent(IContainer container, List<LicenseExportData> licenses)
        {
            container.Column(column =>
            {
                // Summary stats
                column.Item().Row(row =>
                {
                    row.ConstantItem(120).Background(Colors.Teal.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("Total").FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text(licenses.Count.ToString()).Bold().FontSize(18).FontColor(Colors.Teal.Darken2);
                    });

                    row.ConstantItem(10);

                    row.ConstantItem(100).Background(Colors.Green.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("Active").FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text(licenses.Count(l => l.Status == "ACTIVE").ToString()).Bold().FontSize(18).FontColor(Colors.Green.Darken2);
                    });

                    row.ConstantItem(10);

                    row.ConstantItem(100).Background(Colors.Orange.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("Expired").FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text(licenses.Count(l => l.Status == "EXPIRED").ToString()).Bold().FontSize(18).FontColor(Colors.Orange.Darken2);
                    });

                    row.ConstantItem(10);

                    row.ConstantItem(100).Background(Colors.Red.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("Revoked").FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text(licenses.Count(l => l.Status == "REVOKED").ToString()).Bold().FontSize(18).FontColor(Colors.Red.Darken2);
                    });
                });

                column.Item().PaddingVertical(15);

                // License table
                column.Item().Table(table =>
                {
                    // Define columns
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(160); // License Key
                        columns.ConstantColumn(130); // HWID
                        columns.ConstantColumn(80);  // Expiry Date
                        columns.ConstantColumn(70);  // Edition
                        columns.ConstantColumn(60);  // Status
                        columns.ConstantColumn(140); // User Email
                        columns.ConstantColumn(90);  // Created
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Teal.Medium).Padding(5)
                            .Text("License Key").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Teal.Medium).Padding(5)
                            .Text("HWID").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Teal.Medium).Padding(5)
                            .Text("Expiry").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Teal.Medium).Padding(5)
                            .Text("Edition").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Teal.Medium).Padding(5)
                            .Text("Status").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Teal.Medium).Padding(5)
                            .Text("User Email").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Teal.Medium).Padding(5)
                            .Text("Created").FontColor(Colors.White).Bold();
                    });

                    // Data rows
                    foreach (var license in licenses)
                    {
                        var bgColor = licenses.IndexOf(license) % 2 == 0 
                            ? Colors.White 
                            : Colors.Grey.Lighten4;

                        table.Cell().Background(bgColor).Padding(4)
                            .Text(license.LicenseKey).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(license.Hwid ?? "N/A").FontSize(8);
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(license.ExpiryDate ?? "N/A").FontSize(8);
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(license.Edition).FontSize(8);
                        
                        // Status with color
                        var statusColor = license.Status switch
                        {
                            "ACTIVE" => Colors.Green.Medium,
                            "EXPIRED" => Colors.Orange.Medium,
                            "REVOKED" => Colors.Red.Medium,
                            _ => Colors.Grey.Medium
                        };
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(license.Status).FontSize(8).FontColor(statusColor).Bold();
                        
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(license.UserEmail ?? "N/A").FontSize(8);
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(license.CreatedAt?.ToString("yyyy-MM-dd") ?? "N/A").FontSize(8);
                    }
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("© DSecure - Confidential License Report")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Medium);
                });

                row.ConstantItem(100).AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" of ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        }
    }
}
