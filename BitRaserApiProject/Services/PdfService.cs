using DSecureApi.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DSecureApi.Services;

public class PdfService
{
    private const int MaxPathLength = 180;
    private const int LogoMaxHeight = 40;
    private const int SignatureMaxHeight = 40;
    private const int WatermarkMaxSize = 350;

    private static string SafePath(string? value, int max = MaxPathLength)
        => string.IsNullOrEmpty(value)
 ? string.Empty
            : (value.Length <= max ? value : value.Substring(0, max - 3) + "...");

    /// <summary>
    /// Validates if the byte array is a valid image
    /// </summary>
    private static bool IsValidImage(byte[]? imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return false;

        try
        {
            // Try to create a test image to validate
            using var stream = new MemoryStream(imageBytes);
            // Basic validation - check for common image headers
            if (imageBytes.Length < 4)
                return false;

            // Check for PNG, JPEG, GIF, BMP headers
            bool isPng = imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47;
            bool isJpeg = imageBytes[0] == 0xFF && imageBytes[1] == 0xD8;
            bool isGif = imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46;
            bool isBmp = imageBytes[0] == 0x42 && imageBytes[1] == 0x4D;

            return isPng || isJpeg || isGif || isBmp;
        }
        catch
        {
            return false;
        }
    }

    public byte[] GenerateReport(ReportRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var reportData = request.ReportData ?? new ReportData();
        reportData.ErasureLog ??= new List<ErasureLogEntry>();

        // Validate images before use
        var hasValidWatermark = IsValidImage(request.WatermarkImage);
        var hasValidLeftLogo = IsValidImage(request.HeaderLeftLogo);
        var hasValidRightLogo = IsValidImage(request.HeaderRightLogo);
        var hasValidTechSignature = IsValidImage(request.TechnicianSignature);
        var hasValidValidatorSignature = IsValidImage(request.ValidatorSignature);

        var pdf = Document.Create(container =>
    {
        container.Page(page =>
{
            page.Size(PageSizes.Letter);
            page.Margin(40);

    // Watermark (only if valid image exists)
            if (hasValidWatermark)
            {
                page.Background().Element(bg =>
            {
                   bg.AlignCenter()
          .AlignMiddle()
          .Width(WatermarkMaxSize)
         .Height(WatermarkMaxSize)
          .Image(request.WatermarkImage!)
          .FitWidth();
               });
            }

    // Header
            page.Header().Column(col =>
     {
                col.Item().Table(table =>
      {
             table.ColumnsDefinition(columns =>
                {
                   columns.RelativeColumn();
                   columns.RelativeColumn();
                   columns.RelativeColumn();
               });

          // Left logo
             if (hasValidLeftLogo)
                 table.Cell().AlignLeft().Height(LogoMaxHeight).Image(request.HeaderLeftLogo!).FitHeight();
             else
                 table.Cell().AlignLeft().Height(LogoMaxHeight).Image(Placeholders.Image(100, 40)).FitHeight();

          // Header text
             table.Cell().AlignCenter().Text(request.HeaderText ?? string.Empty)
                 .FontSize(10)
             .SemiBold();

          // Right logo
             if (hasValidRightLogo)
                 table.Cell().AlignRight().Height(LogoMaxHeight).Image(request.HeaderRightLogo!).FitHeight();
             else
                 table.Cell().AlignRight().Height(LogoMaxHeight).Image(Placeholders.Image(100, 40)).FitHeight();
         });

                col.Item()
                          .AlignCenter()
             .PaddingTop(5)
              .Text(request.ReportTitle ?? "BitRaserErase Report")
            .FontSize(16)
         .Bold();
            });

    // Content
            page.Content().PaddingVertical(10).Column(col =>
          {
               void Section(string title, Action<IContainer> content)
               {
                   col.Item()
            .Text(title)
                   .FontSize(12)
        .Bold()
             .FontColor(Colors.Grey.Darken3);

                   content(col.Item().PaddingBottom(10));
               }

               Section("Process Status", section =>
 {
             section.Table(t =>
               {
                t.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); });
                t.Cell().Text("Process Status:").FontSize(10).Bold();
                t.Cell().Text(reportData.Status ?? "N/A").FontSize(10);
                t.Cell().Text("Process Mode:").FontSize(10).Bold();
                t.Cell().Text(reportData.ProcessMode ?? "N/A").FontSize(10);
            });
         });

               Section("Report Info", section =>
    {
              section.Table(t =>
         {
                t.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); });
                t.Cell().Text("Report ID:").FontSize(10).Bold();
                t.Cell().Text(reportData.ReportId ?? "N/A").FontSize(10);
                t.Cell().Text("Report Date:").FontSize(10).Bold();
                t.Cell().Text(reportData.ReportDate ?? "N/A").FontSize(10);
                t.Cell().Text("Software:").FontSize(10).Bold();
                t.Cell().Text($"{reportData.SoftwareName ?? "N/A"} {reportData.ProductVersion ?? ""}").FontSize(10);
                t.Cell().Text("Digital Identifier:").FontSize(10).Bold();
                t.Cell().Text(reportData.DigitalSignature ?? "N/A").FontSize(10);
            });
          });

               Section("System Info", section =>
 {
            section.Table(t =>
               {
                   t.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); });
                   t.Cell().Text("OS:").FontSize(10).Bold();
                   // ✅ FIXED: Smart OS display - use OSVersion if available, fallback to OS, then "N/A"
                   var osDisplay = !string.IsNullOrWhiteSpace(reportData.OSVersion) 
                       ? reportData.OSVersion.Trim()
                       : !string.IsNullOrWhiteSpace(reportData.OS)
                           ? reportData.OS.Trim()
                           : "N/A";
                   t.Cell().Text(osDisplay).FontSize(10);
                   t.Cell().Text("Computer:").FontSize(10).Bold();
                   t.Cell().Text(reportData.ComputerName ?? "N/A").FontSize(10);
                   t.Cell().Text("MAC:").FontSize(10).Bold();
                   t.Cell().Text(reportData.MacAddress ?? "N/A").FontSize(10);
                   t.Cell().Text("Manufacturer:").FontSize(10).Bold();
                   t.Cell().Text(reportData.Manufacturer ?? "N/A").FontSize(10);
               });
        });

               Section("Process Summary", section =>
                       {
                     section.Table(t =>
                 {
              t.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); });
              t.Cell().Text("Start:").FontSize(10).Bold();
              t.Cell().Text(reportData.EraserStartTime ?? "N/A").FontSize(10);
              t.Cell().Text("End:").FontSize(10).Bold();
              t.Cell().Text(reportData.EraserEndTime ?? "N/A").FontSize(10);
              t.Cell().Text("Method:").FontSize(10).Bold();
              t.Cell().Text(reportData.EraserMethod ?? "N/A").FontSize(10);
              t.Cell().Text("Verification:").FontSize(10).Bold();
              t.Cell().Text(reportData.ValidationMethod ?? "N/A").FontSize(10);
          });
                 });

               Section("Erasure Details", section =>
       {
             section.Table(t =>
{
         t.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); });
         t.Cell().Text("Erase Type:").FontSize(10).Bold();
         t.Cell().Text(reportData.ErasureType ?? "N/A").FontSize(10);
         t.Cell().Text("Total Files:").FontSize(10).Bold();
         t.Cell().Text(reportData.TotalFiles.ToString()).FontSize(10);
         t.Cell().Text("Successful Erasures:").FontSize(10).Bold();
         t.Cell().Text(reportData.ErasedFiles.ToString()).FontSize(10);
         t.Cell().Text("Failed Erasures:").FontSize(10).Bold();
         t.Cell().Text(reportData.FailedFiles.ToString()).FontSize(10);
     });
         });

               Section("Personnel", section =>
                 {
                    section.Table(t =>
                  {
                   t.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); });
                   t.Cell().Text("Erased By:").FontSize(10).Bold();
                   // ✅ FIXED: Show user department or blank string instead of "N/A"
                   var technicianInfo = !string.IsNullOrWhiteSpace(request.TechnicianName) 
                       ? request.TechnicianName 
                       : "Unknown";
                   var technicianDept = !string.IsNullOrWhiteSpace(request.TechnicianDept) 
                       ? $" ({request.TechnicianDept})" 
                       : string.Empty; // ✅ Blank instead of "(N/A)"
                   t.Cell().Text($"{technicianInfo}{technicianDept}").FontSize(10);
                   
                   t.Cell().Text("Validated By:").FontSize(10).Bold();
                   // ✅ FIXED: Show user department or blank string instead of "N/A"
                   var validatorInfo = !string.IsNullOrWhiteSpace(request.ValidatorName) 
                       ? request.ValidatorName 
                       : "Unknown";
                   var validatorDept = !string.IsNullOrWhiteSpace(request.ValidatorDept) 
                       ? $" ({request.ValidatorDept})" 
                       : string.Empty; // ✅ Blank instead of "(N/A)"
                   t.Cell().Text($"{validatorInfo}{validatorDept}").FontSize(10);
               });

                    col.Item().PaddingTop(30).Table(sigTable =>
                       {
                  sigTable.ColumnsDefinition(c =>
                       {
                   c.RelativeColumn();
                   c.ConstantColumn(50);
                   c.RelativeColumn();
               });

                  sigTable.Cell().AlignCenter().Text("Technician").FontSize(10).Bold();
                  sigTable.Cell();
                  sigTable.Cell().AlignCenter().Text("Validator").FontSize(10).Bold();

                           // Technician signature
                  if (hasValidTechSignature)
                      sigTable.Cell().AlignCenter().Height(SignatureMaxHeight).Image(request.TechnicianSignature!).FitHeight();
                  else
                      sigTable.Cell().AlignCenter().Height(SignatureMaxHeight).Image(Placeholders.Image(120, 40)).FitHeight();

                  sigTable.Cell();

                           // Validator signature
                  if (hasValidValidatorSignature)
                      sigTable.Cell().AlignCenter().Height(SignatureMaxHeight).Image(request.ValidatorSignature!).FitHeight();
                  else
                      sigTable.Cell().AlignCenter().Height(SignatureMaxHeight).Image(Placeholders.Image(120, 40)).FitHeight();
              });
                });

              // PAGE BREAK BEFORE ANNEXURE - This forces new page
               col.Item().PageBreak();

              // Annexure section on new page
               Section("Annexure: Erasure Log", section =>
               {
                    if (reportData.ErasureLog.Count == 0)
                    {
                        section.Text("No erasure log entries available.")
                      .FontSize(10)
                   .Italic();
                        return;
                    }

                    section.Table(t =>
               {
                 if ((reportData.ReportId ?? "").Contains("erasevolume", StringComparison.OrdinalIgnoreCase))
                 {
                       // Volume erasure table with proper formatting
                     t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);     // Volume
                c.RelativeColumn(2);     // Capacity
                c.RelativeColumn(2);   // Total Sectors
                c.RelativeColumn(2);     // Sectors Erased
                c.RelativeColumn(2);     // Status
            });

                       // Header row with borders
                     t.Header(h =>
                {
              h.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
              .Text("Volume").FontSize(10).Bold();
              h.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
         .Text("Capacity").FontSize(10).Bold();
              h.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
 .Text("Total Sectors").FontSize(10).Bold();
              h.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
     .Text("Sectors Erased").FontSize(10).Bold();
              h.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
         .Text("Status").FontSize(10).Bold();
          });

                       // Data rows with borders
                     foreach (var log in reportData.ErasureLog)
                     {
                         t.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
                            .Text(log.Target ?? "N/A").FontSize(10);
                         t.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
                         .Text(log.Capacity ?? "N/A").FontSize(10);
                         t.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
         .Text(log.TotalSectors ?? "N/A").FontSize(10);
                         t.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
                    .Text(log.SectorsErased ?? "N/A").FontSize(10);
                         t.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
                 .Text(log.Status ?? "N/A").FontSize(10);
                     }
                 }
                 else
                 {
                       // File erasure table
                     t.ColumnsDefinition(c =>
                {
                  c.RelativeColumn(5);     // File Path
                  c.RelativeColumn(2);     // Size
                  c.RelativeColumn(2);     // Status
              });

                     t.Header(h =>
                   {
             h.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
         .Text("File Path").FontSize(10).Bold();
             h.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
                   .Text("Size").FontSize(10).Bold();
             h.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
          .Text("Status").FontSize(10).Bold();
         });

                     foreach (var log in reportData.ErasureLog)
                     {
                         t.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
                           .Text(SafePath(log.Target)).FontSize(10);
                         t.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
                   .Text(log.Size ?? "N/A").FontSize(10);
                         t.Cell().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5)
         .Text(log.Status ?? "N/A").FontSize(10);
                     }
                 }
             });
                });
           });

    // Footer
            page.Footer().Row(row =>
         {
             // Footer logo (reuse right logo)
              if (hasValidRightLogo)
                  row.RelativeItem().AlignLeft().Height(LogoMaxHeight).Image(request.HeaderRightLogo!).FitHeight();
              else
                  row.RelativeItem().AlignLeft().Height(LogoMaxHeight).Image(Placeholders.Image(100, 40)).FitHeight();

              row.RelativeItem().AlignCenter()
              .Text(reportData.DigitalSignature ?? "N/A")
           .FontSize(8);

              row.RelativeItem().AlignRight().Text(t => t.CurrentPageNumber());
          });
        });
    });

        return pdf.GeneratePdf();
    }
}
