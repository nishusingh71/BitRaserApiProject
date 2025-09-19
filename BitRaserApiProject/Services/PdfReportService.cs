using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

public class PdfReportService
{
    public bool GeneratePdf(Dictionary<string, object> reportData, string outputPath, bool preview = false)
    {
        try
        {
            // Check if reportsetting.json exists
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string settingsPath = Path.Combine(baseDir, "reportsetting.json");
            if (!File.Exists(settingsPath))
            {
                Console.Error.WriteLine($"Error: reportsetting.json not found at {settingsPath}");
                return false;
            }

            var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(settingsPath));

            int fontSize = 10;
            string watermarkImagePath = settings.GetValueOrDefault("watermark", "");
            string topLeftLogoPath = settings.GetValueOrDefault("top_logo", "");
            string topRightLogoPath = settings.GetValueOrDefault("top_logo", "");

            // Check output directory is writable
            string outputDir = Path.GetDirectoryName(outputPath) ?? "";
            if (!Directory.Exists(outputDir))
            {
                Console.Error.WriteLine($"Error: Output directory does not exist: {outputDir}");
                return false;
            }
            try
            {
                string testFile = Path.Combine(outputDir, "test_write.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception dirEx)
            {
                Console.Error.WriteLine($"Error: Cannot write to output directory: {dirEx.Message}");
                return false;
            }

            using var document = new PdfDocument();
            var page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.Letter;
            var gfx = XGraphics.FromPdfPage(page);

            // Draw header logos and text (handle missing images gracefully)
            if (!string.IsNullOrEmpty(topLeftLogoPath) && File.Exists(topLeftLogoPath))
                gfx.DrawImage(XImage.FromFile(topLeftLogoPath), 40, 40, 108, 43);
            if (!string.IsNullOrEmpty(topRightLogoPath) && File.Exists(topRightLogoPath))
                gfx.DrawImage(XImage.FromFile(topRightLogoPath), page.Width - 148, 40, 108, 43);

            var headerFont = new XFont("Arial", 14, XFontStyle.Bold);
            gfx.DrawString(settings.GetValueOrDefault("header_text", "D-SecureErase Report"), headerFont, XBrushes.Black, new XRect(0, 40, page.Width, 43), XStringFormats.Center);

            var titleFont = new XFont("Arial", 12, XFontStyle.Bold);
            gfx.DrawString(settings.GetValueOrDefault("report_title", "D-SecureErase Report"), titleFont, XBrushes.Black, new XRect(0, 90, page.Width, 20), XStringFormats.Center);

            var bodyFont = new XFont("Arial", fontSize, XFontStyle.Regular);
            gfx.DrawString($"Process Status: {reportData.GetValueOrDefault("status", "")}", bodyFont, XBrushes.Black, 40, 130);
            gfx.DrawString($"Process Mode: {reportData.GetValueOrDefault("process_mode", "")}", bodyFont, XBrushes.Black, 40, 150);

            // Draw watermark if exists and valid
            if (!string.IsNullOrEmpty(watermarkImagePath) && File.Exists(watermarkImagePath))
            {
                gfx.DrawImage(XImage.FromFile(watermarkImagePath), (int)page.Width / 4, (int)page.Height / 4, (int)page.Width / 2, (int)page.Height / 2);
            }

            document.Save(outputPath);
            return true;
        }
        catch (Exception ex)
        {
            // More detailed error logging
            Console.Error.WriteLine($"Error generating report: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
}