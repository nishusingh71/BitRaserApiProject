using DSecureApi.Models;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace DSecureApi.Services.Email
{
    /// <summary>
    /// Excel export service for generating order detail attachments
    /// Uses ClosedXML for .xlsx generation
    /// </summary>
    public class ExcelExportService
    {
        private readonly ILogger<ExcelExportService> _logger;

        public ExcelExportService(ILogger<ExcelExportService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generate Excel file with order details and license keys
        /// </summary>
        public byte[] GenerateOrderDetailsExcel(Order order, List<string>? licenseKeys = null)
        {
            try
            {
                _logger.LogInformation("📊 Generating Excel for Order #{OrderId}", order.OrderId);

                using var workbook = new XLWorkbook();
                
                // Order Details Sheet
                var orderSheet = workbook.AddWorksheet("Order Details");
                CreateOrderDetailsSheet(orderSheet, order);
                
                // License Keys Sheet (if applicable)
                if (licenseKeys != null && licenseKeys.Count > 0)
                {
                    var licenseSheet = workbook.AddWorksheet("License Keys");
                    CreateLicenseKeysSheet(licenseSheet, order, licenseKeys);
                }

                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                
                var bytes = stream.ToArray();
                _logger.LogInformation("✅ Excel generated: {Size} bytes", bytes.Length);
                
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate Excel for Order #{OrderId}", order.OrderId);
                throw;
            }
        }

        private void CreateOrderDetailsSheet(IXLWorksheet sheet, Order order)
        {
            // Title
            sheet.Cell("A1").Value = "DSecure - Order Confirmation";
            sheet.Range("A1:C1").Merge();
            sheet.Cell("A1").Style.Font.Bold = true;
            sheet.Cell("A1").Style.Font.FontSize = 16;
            sheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1a1a2e");
            sheet.Cell("A1").Style.Font.FontColor = XLColor.White;

            // Order Info Header
            var row = 3;
            AddHeaderRow(sheet, row, "ORDER INFORMATION");
            row++;

            AddDataRow(sheet, ref row, "Order ID", order.OrderId.ToString());
            AddDataRow(sheet, ref row, "Order Date", order.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            AddDataRow(sheet, ref row, "Status", order.Status);
            AddDataRow(sheet, ref row, "Payment ID", order.DodoPaymentId ?? "N/A");
            
            row++;

            // Customer Info
            AddHeaderRow(sheet, row, "CUSTOMER INFORMATION");
            row++;

            AddDataRow(sheet, ref row, "Name", $"{order.FirstName ?? ""} {order.LastName ?? ""}".Trim());
            AddDataRow(sheet, ref row, "Email", order.UserEmail ?? "N/A");
            AddDataRow(sheet, ref row, "Phone", order.PhoneNumber ?? "N/A");
            
            row++;

            // Billing Address
            AddHeaderRow(sheet, row, "BILLING ADDRESS");
            row++;

            if (!string.IsNullOrEmpty(order.BillingAddress))
                AddDataRow(sheet, ref row, "Address", order.BillingAddress);
            if (!string.IsNullOrEmpty(order.BillingCity))
                AddDataRow(sheet, ref row, "City", order.BillingCity);
            if (!string.IsNullOrEmpty(order.BillingState))
                AddDataRow(sheet, ref row, "State", order.BillingState);
            if (!string.IsNullOrEmpty(order.BillingCountry))
                AddDataRow(sheet, ref row, "Country", order.BillingCountry);
            if (!string.IsNullOrEmpty(order.BillingZip))
                AddDataRow(sheet, ref row, "ZIP Code", order.BillingZip);
            
            row++;

            // Product Info
            AddHeaderRow(sheet, row, "PRODUCT INFORMATION");
            row++;

            AddDataRow(sheet, ref row, "Product", order.ProductName ?? "DSecure Product");
            AddDataRow(sheet, ref row, "Quantity", order.LicenseCount.ToString());
            AddDataRow(sheet, ref row, "License Count", order.LicenseCount.ToString());
            AddDataRow(sheet, ref row, "License Duration", $"{order.LicenseYears} Year(s)");
            if (order.LicenseExpiresAt.HasValue)
                AddDataRow(sheet, ref row, "Expires At", order.LicenseExpiresAt.Value.ToString("yyyy-MM-dd"));
            
            row++;

            // Payment Info
            AddHeaderRow(sheet, row, "PAYMENT INFORMATION");
            row++;

            var amount = order.AmountCents / 100m;
            var tax = order.TaxAmountCents / 100m;
            var total = amount;

            AddDataRow(sheet, ref row, "Subtotal", $"{order.Currency ?? "USD"} {amount:N2}");
            if (tax > 0)
            {
                AddDataRow(sheet, ref row, "Tax", $"{order.Currency ?? "USD"} {tax:N2}");
                total = amount + tax;
            }
            AddDataRow(sheet, ref row, "Total", $"{order.Currency ?? "USD"} {total:N2}");
            AddDataRow(sheet, ref row, "Payment Method", order.PaymentMethod ?? "Card");
            if (!string.IsNullOrEmpty(order.CardLastFour))
                AddDataRow(sheet, ref row, "Card", $"**** **** **** {order.CardLastFour}");

            // Auto-fit columns
            sheet.Columns().AdjustToContents();
            sheet.Column("A").Width = 20;
            sheet.Column("B").Width = 40;
        }

        private void CreateLicenseKeysSheet(IXLWorksheet sheet, Order order, List<string> licenseKeys)
        {
            // Title
            sheet.Cell("A1").Value = "License Keys";
            sheet.Range("A1:C1").Merge();
            sheet.Cell("A1").Style.Font.Bold = true;
            sheet.Cell("A1").Style.Font.FontSize = 14;
            sheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1a1a2e");
            sheet.Cell("A1").Style.Font.FontColor = XLColor.White;

            // Headers
            sheet.Cell("A3").Value = "#";
            sheet.Cell("B3").Value = "License Key";
            sheet.Cell("C3").Value = "Status";
            
            var headerRange = sheet.Range("A3:C3");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // License keys
            var row = 4;
            for (int i = 0; i < licenseKeys.Count; i++)
            {
                sheet.Cell($"A{row}").Value = i + 1;
                sheet.Cell($"B{row}").Value = licenseKeys[i];
                sheet.Cell($"C{row}").Value = "Active";
                
                // Alternate row colors
                if (i % 2 == 1)
                {
                    sheet.Range($"A{row}:C{row}").Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f5f5");
                }
                
                row++;
            }

            // Summary
            row += 2;
            sheet.Cell($"A{row}").Value = "Total Licenses:";
            sheet.Cell($"A{row}").Style.Font.Bold = true;
            sheet.Cell($"B{row}").Value = licenseKeys.Count;

            row++;
            if (order.LicenseExpiresAt.HasValue)
            {
                sheet.Cell($"A{row}").Value = "Valid Until:";
                sheet.Cell($"A{row}").Style.Font.Bold = true;
                sheet.Cell($"B{row}").Value = order.LicenseExpiresAt.Value.ToString("yyyy-MM-dd");
            }

            // Auto-fit
            sheet.Columns().AdjustToContents();
            sheet.Column("B").Width = 40;
        }

        private void AddHeaderRow(IXLWorksheet sheet, int row, string text)
        {
            sheet.Cell($"A{row}").Value = text;
            sheet.Range($"A{row}:B{row}").Merge();
            sheet.Cell($"A{row}").Style.Font.Bold = true;
            sheet.Cell($"A{row}").Style.Font.FontSize = 12;
            sheet.Cell($"A{row}").Style.Fill.BackgroundColor = XLColor.FromHtml("#e8e8e8");
        }

        private void AddDataRow(IXLWorksheet sheet, ref int row, string label, string value)
        {
            sheet.Cell($"A{row}").Value = label;
            sheet.Cell($"B{row}").Value = value;
            sheet.Cell($"A{row}").Style.Font.Bold = true;
            row++;
        }
    }
}
