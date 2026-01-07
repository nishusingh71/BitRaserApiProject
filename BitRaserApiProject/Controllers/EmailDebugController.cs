using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BitRaserApiProject.Utilities;
using System.Security.Claims;
using BitRaserApiProject.Factories;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Services.Email;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Debug controller for testing email encoding/decoding
    /// Use this to verify Base64 email functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EmailDebugController : ControllerBase
    {
        private readonly ILogger<EmailDebugController> _logger;
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly ICacheService _cacheService;
        private readonly IEmailOrchestrator? _emailOrchestrator;

        public EmailDebugController(
            ILogger<EmailDebugController> logger,
            DynamicDbContextFactory contextFactory,
            ICacheService cacheService,
            IEmailOrchestrator? emailOrchestrator = null)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _cacheService = cacheService;
            _emailOrchestrator = emailOrchestrator;
        }

        /// <summary>
        /// 🧪 TEST: Send email via MS Graph API
        /// POST /api/EmailDebug/test-graph-email
        /// </summary>
        [HttpPost("test-graph-email")]
        [AllowAnonymous]
        public async Task<IActionResult> TestGraphEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ToEmail))
                {
                    return BadRequest(new { success = false, message = "toEmail is required" });
                }

                if (_emailOrchestrator == null)
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Email orchestrator not configured",
                        hint = "Check if IEmailOrchestrator is registered in Program.cs"
                    });
                }

                _logger.LogInformation("🧪 Testing MS Graph email to: {Email}", request.ToEmail);

                var emailRequest = new EmailSendRequest
                {
                    ToEmail = request.ToEmail,
                    ToName = request.ToName ?? request.ToEmail,
                    Subject = request.Subject ?? $"🧪 Test Email from DSecure - {DateTime.UtcNow:HH:mm:ss}",
                    HtmlBody = request.HtmlBody ?? GenerateTestEmailHtml(request.ToEmail),
                    Type = EmailType.Transactional
                };

                var result = await _emailOrchestrator.SendEmailAsync(emailRequest);

                if (result.Success)
                {
                    _logger.LogInformation("✅ Test email sent successfully via {Provider}", result.ProviderUsed);
                    return Ok(new
                    {
                        success = true,
                        message = "Test email sent successfully!",
                        provider = result.ProviderUsed,
                        durationMs = result.DurationMs,
                        toEmail = request.ToEmail,
                        sentAt = DateTime.UtcNow.ToString("o")
                    });
                }
                else
                {
                    _logger.LogWarning("⚠️ Test email failed: {Message}", result.Message);
                    return StatusCode(500, new
                    {
                        success = false,
                        message = result.Message,
                        provider = result.ProviderUsed,
                        hint = "Check Render logs for detailed error"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in test email endpoint");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    exceptionType = ex.GetType().Name
                });
            }
        }

        /// <summary>
        /// 🔍 GET: Check MS Graph provider status
        /// GET /api/EmailDebug/graph-status
        /// </summary>
        [HttpGet("graph-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGraphStatus()
        {
            try
            {
                if (_emailOrchestrator == null)
                {
                    return Ok(new { 
                        success = false, 
                        message = "Email orchestrator not configured",
                        providers = new List<object>()
                    });
                }

                var providers = await _emailOrchestrator.GetAvailableProvidersAsync(EmailType.Transactional);
                
                var status = providers.Select(p => new
                {
                    name = p.ProviderName,
                    priority = p.Priority,
                    isAvailable = true
                }).ToList();

                return Ok(new
                {
                    success = true,
                    totalProviders = status.Count,
                    providers = status,
                    primaryProvider = status.OrderBy(p => p.priority).FirstOrDefault()?.name ?? "none",
                    checkedAt = DateTime.UtcNow.ToString("o")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting graph status");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        private string GenerateTestEmailHtml(string toEmail)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial, sans-serif; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: #1a1a2e; color: #fff; padding: 25px; text-align: center;'>
<h1 style='margin: 0; font-size: 20px;'>🧪 Test Email</h1>
</div>
<div style='padding: 25px;'>
<p>Hi there,</p>
<p>This is a <strong>test email</strong> sent via <strong>Microsoft Graph API</strong>.</p>
<p style='background: #f0f9ff; padding: 15px; border-radius: 8px;'>
<strong>📧 Recipient:</strong> {toEmail}<br>
<strong>📅 Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
</p>
<p>If you received this email, MS Graph is working correctly! 🎉</p>
</div>
<div style='background: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #888;'>
DSecure Technologies • Test Email
</div>
</div>
</body>
</html>";
        }

        public class TestEmailRequest
        {
            public string ToEmail { get; set; } = string.Empty;
            public string? ToName { get; set; }
            public string? Subject { get; set; }
            public string? HtmlBody { get; set; }
        }

        /// <summary>
        /// 🧪 FULL TEST: Send order email with Excel attachment (simulates checkout)
        /// POST /api/EmailDebug/test-full-hybrid
        /// </summary>
        [HttpPost("test-full-hybrid")]
        [AllowAnonymous]
        public async Task<IActionResult> TestFullHybridEmail([FromBody] FullHybridTestRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ToEmail))
                {
                    return BadRequest(new { success = false, message = "toEmail is required" });
                }

                if (_emailOrchestrator == null)
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Email orchestrator not configured",
                        hint = "IEmailOrchestrator not registered in DI"
                    });
                }

                _logger.LogInformation("🧪 FULL HYBRID TEST: Sending to {Email}", request.ToEmail);

                // Simulate order data
                var orderId = request.TestOrderId ?? new Random().Next(10000, 99999);
                var productName = request.ProductName ?? "BitRaser Professional";
                var quantity = request.Quantity ?? 1;
                var amount = request.Amount ?? 99.00m;
                var licenseKey = $"TEST-{Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper()}";

                // Generate test Excel attachment
                byte[]? excelBytes = null;
                try
                {
                    excelBytes = GenerateTestExcelAttachment(orderId, productName, quantity, amount, licenseKey);
                    _logger.LogInformation("📎 Generated test Excel: {Size} bytes", excelBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Could not generate Excel, proceeding without attachment");
                }

                // Build email request
                var emailRequest = new EmailSendRequest
                {
                    ToEmail = request.ToEmail,
                    ToName = request.ToName ?? request.ToEmail.Split('@')[0],
                    Subject = $"🧪 TEST - Order #{orderId} Confirmation",
                    HtmlBody = GenerateFullTestEmailHtml(orderId, productName, quantity, amount, licenseKey, request.ToName ?? "Customer"),
                    Type = EmailType.Transactional,
                    OrderId = orderId
                };

                // Add Excel attachment if generated successfully
                if (excelBytes != null && excelBytes.Length > 0)
                {
                    emailRequest.Attachments = new List<EmailAttachment>
                    {
                        new EmailAttachment(
                            $"DSecure_Order_{orderId}.xlsx",
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            excelBytes
                        )
                    };
                }

                // Send via hybrid orchestrator
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await _emailOrchestrator.SendEmailAsync(emailRequest);
                stopwatch.Stop();

                if (result.Success)
                {
                    _logger.LogInformation("✅ FULL HYBRID TEST: Success via {Provider} in {Ms}ms", 
                        result.ProviderUsed, stopwatch.ElapsedMilliseconds);
                    
                    return Ok(new
                    {
                        success = true,
                        message = "Full hybrid email sent successfully!",
                        provider = result.ProviderUsed,
                        durationMs = stopwatch.ElapsedMilliseconds,
                        toEmail = request.ToEmail,
                        testData = new
                        {
                            orderId = orderId,
                            productName = productName,
                            quantity = quantity,
                            amount = amount,
                            licenseKey = licenseKey,
                            hasExcelAttachment = excelBytes != null
                        },
                        sentAt = DateTime.UtcNow.ToString("o")
                    });
                }
                else
                {
                    _logger.LogWarning("⚠️ FULL HYBRID TEST: Failed - {Message}", result.Message);
                    return StatusCode(500, new
                    {
                        success = false,
                        message = result.Message,
                        provider = result.ProviderUsed,
                        hint = "Check Render logs for detailed error"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ FULL HYBRID TEST: Exception");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    exceptionType = ex.GetType().Name
                });
            }
        }

        public class FullHybridTestRequest
        {
            public string ToEmail { get; set; } = string.Empty;
            public string? ToName { get; set; }
            public string? ProductName { get; set; }
            public int? Quantity { get; set; }
            public decimal? Amount { get; set; }
            public int? TestOrderId { get; set; }
        }

        private byte[] GenerateTestExcelAttachment(int orderId, string productName, int quantity, decimal amount, string licenseKey)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Order Details");

            // Header
            ws.Cell("A1").Value = "Order Details";
            ws.Range("A1:C1").Merge();
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            // Order info
            ws.Cell("A3").Value = "Order ID:";
            ws.Cell("B3").Value = orderId;
            ws.Cell("A4").Value = "Product:";
            ws.Cell("B4").Value = productName;
            ws.Cell("A5").Value = "Quantity:";
            ws.Cell("B5").Value = quantity;
            ws.Cell("A6").Value = "Amount:";
            ws.Cell("B6").Value = $"${amount:N2}";
            ws.Cell("A7").Value = "Date:";
            ws.Cell("B7").Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // License Keys section
            ws.Cell("A9").Value = "License Keys";
            ws.Cell("A9").Style.Font.Bold = true;
            ws.Cell("A10").Value = "#1";
            ws.Cell("B10").Value = licenseKey;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private string GenerateFullTestEmailHtml(int orderId, string productName, int quantity, decimal amount, string licenseKey, string customerName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 600px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); color: #fff; padding: 30px; text-align: center;'>
<h1 style='margin: 0; font-size: 24px;'>🧪 TEST ORDER CONFIRMATION</h1>
<p style='margin: 10px 0 0 0; opacity: 0.8;'>Order #{orderId}</p>
</div>
<div style='padding: 30px;'>
<p>Hi {customerName},</p>
<p>This is a <strong>TEST EMAIL</strong> from the hybrid email system.</p>

<div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
<h3 style='margin: 0 0 15px 0;'>📦 Order Summary</h3>
<p style='margin: 5px 0;'><strong>Product:</strong> {productName}</p>
<p style='margin: 5px 0;'><strong>Quantity:</strong> {quantity}</p>
<p style='margin: 5px 0;'><strong>Amount:</strong> ${amount:N2}</p>
</div>

<div style='background: #e8f5e9; padding: 20px; border-radius: 8px; margin: 20px 0;'>
<h3 style='margin: 0 0 15px 0;'>🔑 Test License Key</h3>
<p style='font-family: monospace; font-size: 16px; background: #fff; padding: 10px; border-radius: 4px;'>{licenseKey}</p>
</div>

<p>📎 <strong>Attachment:</strong> Excel file with order details is attached.</p>

<p style='margin-top: 30px; color: #888; font-size: 12px;'>
This is a test email sent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC<br>
Sent via DSecure Hybrid Email System
</p>
</div>
<div style='background: #1a1a2e; color: #fff; padding: 15px; text-align: center; font-size: 12px;'>
DSecure Technologies • Support@dsecuretech.com
</div>
</div>
</body>
</html>";
        }

        /// <summary>
        /// 🎯 COMPREHENSIVE TEST: Test ALL hybrid email conditions with just sender email
        /// POST /api/EmailDebug/test-all-hybrid
        /// Tests: MS Graph → SendGrid → Quota → Provider availability
        /// </summary>
        [HttpPost("test-all-hybrid")]
        [AllowAnonymous]
        public async Task<IActionResult> TestAllHybridConditions([FromBody] SimpleEmailTestRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var testResults = new List<object>();
            var testNumber = 0;

            try
            {
                if (string.IsNullOrEmpty(request.ToEmail))
                {
                    return BadRequest(new { success = false, message = "toEmail is required" });
                }

                if (_emailOrchestrator == null)
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Email orchestrator not configured",
                        hint = "IEmailOrchestrator not registered in DI"
                    });
                }

                _logger.LogInformation("🎯 Starting FULL HYBRID EMAIL TEST to: {Email}", request.ToEmail);

                // ═══════════════════════════════════════════════════════════════
                // TEST 1: Check Provider Availability
                // ═══════════════════════════════════════════════════════════════
                testNumber++;
                var test1Start = DateTime.UtcNow;
                try
                {
                    var providers = await _emailOrchestrator.GetAvailableProvidersAsync(EmailType.Transactional);
                    var providerList = providers.ToList();
                    var providerStatus = providerList.Select(p => new { 
                        name = p.ProviderName, 
                        priority = p.Priority,
                        available = true 
                    }).ToList();

                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "Provider Availability Check",
                        success = true,
                        durationMs = (DateTime.UtcNow - test1Start).TotalMilliseconds,
                        data = new
                        {
                            totalProviders = providerList.Count,
                            providers = providerStatus,
                            primaryProvider = providerList.FirstOrDefault()?.ProviderName ?? "None"
                        }
                    });
                }
                catch (Exception ex)
                {
                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "Provider Availability Check",
                        success = false,
                        durationMs = (DateTime.UtcNow - test1Start).TotalMilliseconds,
                        error = ex.Message
                    });
                }

                // ═══════════════════════════════════════════════════════════════
                // TEST 2: Simple Transactional Email (MS Graph Priority)
                // ═══════════════════════════════════════════════════════════════
                testNumber++;
                var test2Start = DateTime.UtcNow;
                try
                {
                    var result = await _emailOrchestrator.SendEmailAsync(new EmailSendRequest
                    {
                        ToEmail = request.ToEmail,
                        ToName = request.ToEmail.Split('@')[0],
                        Subject = $"🧪 Test #{testNumber}: Simple Email - {DateTime.UtcNow:HH:mm:ss}",
                        HtmlBody = GenerateTestHtml("Simple Transactional Email", 
                            "This tests the primary email provider (MS Graph).", testNumber),
                        Type = EmailType.Transactional
                    });

                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "Simple Transactional Email",
                        success = result.Success,
                        provider = result.ProviderUsed,
                        durationMs = (DateTime.UtcNow - test2Start).TotalMilliseconds,
                        message = result.Message
                    });
                }
                catch (Exception ex)
                {
                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "Simple Transactional Email",
                        success = false,
                        durationMs = (DateTime.UtcNow - test2Start).TotalMilliseconds,
                        error = ex.Message
                    });
                }

                // ═══════════════════════════════════════════════════════════════
                // TEST 3: Email with Excel Attachment
                // ═══════════════════════════════════════════════════════════════
                testNumber++;
                var test3Start = DateTime.UtcNow;
                try
                {
                    // Generate test Excel
                    byte[] excelBytes;
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Test Data");
                        ws.Cell("A1").Value = "Hybrid Email Test";
                        ws.Cell("A2").Value = "Timestamp:";
                        ws.Cell("B2").Value = DateTime.UtcNow.ToString("o");
                        ws.Cell("A3").Value = "Recipient:";
                        ws.Cell("B3").Value = request.ToEmail;
                        ws.Columns().AdjustToContents();
                        
                        using var stream = new MemoryStream();
                        workbook.SaveAs(stream);
                        excelBytes = stream.ToArray();
                    }

                    var result = await _emailOrchestrator.SendEmailAsync(new EmailSendRequest
                    {
                        ToEmail = request.ToEmail,
                        ToName = request.ToEmail.Split('@')[0],
                        Subject = $"🧪 Test #{testNumber}: Excel Attachment - {DateTime.UtcNow:HH:mm:ss}",
                        HtmlBody = GenerateTestHtml("Email with Excel Attachment", 
                            "This tests attachment handling. Check for attached Excel file!", testNumber),
                        Type = EmailType.Transactional,
                        Attachments = new List<EmailAttachment>
                        {
                            new EmailAttachment("TestData.xlsx", 
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                                excelBytes)
                        }
                    });

                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "Email with Excel Attachment",
                        success = result.Success,
                        provider = result.ProviderUsed,
                        durationMs = (DateTime.UtcNow - test3Start).TotalMilliseconds,
                        attachmentSize = $"{excelBytes.Length} bytes",
                        message = result.Message
                    });
                }
                catch (Exception ex)
                {
                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "Email with Excel Attachment",
                        success = false,
                        durationMs = (DateTime.UtcNow - test3Start).TotalMilliseconds,
                        error = ex.Message
                    });
                }

                // ═══════════════════════════════════════════════════════════════
                // TEST 4: OTP Email Type
                // ═══════════════════════════════════════════════════════════════
                testNumber++;
                var test4Start = DateTime.UtcNow;
                try
                {
                    var testOtp = new Random().Next(100000, 999999).ToString();
                    var result = await _emailOrchestrator.SendEmailAsync(new EmailSendRequest
                    {
                        ToEmail = request.ToEmail,
                        ToName = request.ToEmail.Split('@')[0],
                        Subject = $"🧪 Test #{testNumber}: OTP Email - {DateTime.UtcNow:HH:mm:ss}",
                        HtmlBody = GenerateOtpTestHtml(testOtp, testNumber),
                        Type = EmailType.OTP
                    });

                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "OTP Email Type",
                        success = result.Success,
                        provider = result.ProviderUsed,
                        durationMs = (DateTime.UtcNow - test4Start).TotalMilliseconds,
                        testOtp = testOtp,
                        message = result.Message
                    });
                }
                catch (Exception ex)
                {
                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "OTP Email Type",
                        success = false,
                        durationMs = (DateTime.UtcNow - test4Start).TotalMilliseconds,
                        error = ex.Message
                    });
                }

                // ═══════════════════════════════════════════════════════════════
                // TEST 5: Order Confirmation Simulation
                // ═══════════════════════════════════════════════════════════════
                testNumber++;
                var test5Start = DateTime.UtcNow;
                try
                {
                    var orderId = new Random().Next(10000, 99999);
                    var licenseKey = $"TEST-{Guid.NewGuid():N}".Substring(0, 20).ToUpper();

                    var result = await _emailOrchestrator.SendEmailAsync(new EmailSendRequest
                    {
                        ToEmail = request.ToEmail,
                        ToName = request.ToEmail.Split('@')[0],
                        Subject = $"🧪 Test #{testNumber}: Order #{orderId} - {DateTime.UtcNow:HH:mm:ss}",
                        HtmlBody = GenerateOrderTestHtml(orderId, licenseKey, testNumber),
                        Type = EmailType.Transactional,
                        OrderId = orderId
                    });

                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "Order Confirmation Simulation",
                        success = result.Success,
                        provider = result.ProviderUsed,
                        durationMs = (DateTime.UtcNow - test5Start).TotalMilliseconds,
                        testData = new { orderId, licenseKey },
                        message = result.Message
                    });
                }
                catch (Exception ex)
                {
                    testResults.Add(new
                    {
                        test = testNumber,
                        name = "Order Confirmation Simulation",
                        success = false,
                        durationMs = (DateTime.UtcNow - test5Start).TotalMilliseconds,
                        error = ex.Message
                    });
                }

                stopwatch.Stop();

                // Calculate summary
                var successCount = testResults.Count(t => ((dynamic)t).success == true);
                var failCount = testResults.Count - successCount;

                _logger.LogInformation("🎯 FULL HYBRID TEST COMPLETE: {Success}/{Total} passed in {Ms}ms", 
                    successCount, testResults.Count, stopwatch.ElapsedMilliseconds);

                return Ok(new
                {
                    success = failCount == 0,
                    message = $"Hybrid email test complete: {successCount}/{testResults.Count} tests passed",
                    recipientEmail = request.ToEmail,
                    totalDurationMs = stopwatch.ElapsedMilliseconds,
                    summary = new
                    {
                        totalTests = testResults.Count,
                        passed = successCount,
                        failed = failCount,
                        emailsSent = successCount - 1 // Minus the provider check
                    },
                    testResults = testResults,
                    timestamp = DateTime.UtcNow.ToString("o")
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "❌ FULL HYBRID TEST FAILED");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    recipientEmail = request.ToEmail,
                    totalDurationMs = stopwatch.ElapsedMilliseconds,
                    testResults = testResults,
                    exceptionType = ex.GetType().Name
                });
            }
        }

        public class SimpleEmailTestRequest
        {
            public string ToEmail { get; set; } = string.Empty;
        }

        private string GenerateTestHtml(string title, string description, int testNumber)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #1a1a2e, #16213e); color: #fff; padding: 25px; text-align: center;'>
<h1 style='margin: 0; font-size: 18px;'>🧪 Hybrid Test #{testNumber}</h1>
<p style='margin: 5px 0 0 0; opacity: 0.8; font-size: 14px;'>{title}</p>
</div>
<div style='padding: 25px;'>
<p>{description}</p>
<p style='color: #888; font-size: 12px;'>Sent: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
</div>
<div style='background: #f8f9fa; padding: 15px; text-align: center; font-size: 11px; color: #888;'>
DSecure Technologies • Hybrid Email System Test
</div>
</div>
</body></html>";
        }

        private string GenerateOtpTestHtml(string otp, int testNumber)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #059669, #047857); color: #fff; padding: 25px; text-align: center;'>
<h1 style='margin: 0; font-size: 18px;'>🔐 Test #{testNumber}: OTP Email</h1>
</div>
<div style='padding: 25px; text-align: center;'>
<p>Your test verification code is:</p>
<div style='background: #f0fdf4; padding: 20px; border-radius: 8px; margin: 20px 0;'>
<span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #059669;'>{otp}</span>
</div>
<p style='color: #888; font-size: 12px;'>This is a TEST OTP - do not use for actual verification</p>
</div>
<div style='background: #f8f9fa; padding: 15px; text-align: center; font-size: 11px; color: #888;'>
DSecure Technologies • OTP Email Test
</div>
</div>
</body></html>";
        }

        private string GenerateOrderTestHtml(int orderId, string licenseKey, int testNumber)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #1a1a2e, #16213e); color: #fff; padding: 25px; text-align: center;'>
<h1 style='margin: 0; font-size: 18px;'>📦 Test #{testNumber}: Order Confirmation</h1>
<p style='margin: 5px 0 0 0; opacity: 0.8;'>Order #{orderId}</p>
</div>
<div style='padding: 25px;'>
<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin-bottom: 15px;'>
<p style='margin: 5px 0;'><strong>Product:</strong> BitRaser Professional (Test)</p>
<p style='margin: 5px 0;'><strong>Quantity:</strong> 1</p>
<p style='margin: 5px 0;'><strong>Amount:</strong> $99.00</p>
</div>
<div style='background: #e8f5e9; padding: 15px; border-radius: 8px;'>
<p style='margin: 0 0 10px 0;'><strong>🔑 Test License Key:</strong></p>
<code style='background: #fff; padding: 8px 12px; display: block; border-radius: 4px;'>{licenseKey}</code>
</div>
</div>
<div style='background: #f8f9fa; padding: 15px; text-align: center; font-size: 11px; color: #888;'>
DSecure Technologies • Order Email Test
</div>
</div>
</body></html>";
        }        /// <summary>
        /// Test endpoint: Encode email to Base64
        /// GET /api/EmailDebug/encode/{email}
        /// </summary>
        [HttpGet("encode/{email}")]
        [AllowAnonymous]
        public IActionResult EncodeEmail(string email)
        {
            try
            {
                if (!Base64EmailEncoder.IsValidEmail(email))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid email format",
                        providedEmail = email
                    });
                }

                var encoded = Base64EmailEncoder.Encode(email);

                return Ok(new
                {
                    success = true,
                    plainEmail = email,
                    base64Email = encoded,
                    usage = new
                    {
                        swagger = $"Use this in Swagger: {encoded}",
                        javascript = $"const encoded = btoa('{email}').replace(/\\+/g, '-').replace(/\\//g, '_').replace(/=+$/, '');",
                        csharp = $"var encoded = Base64EmailEncoder.Encode(\"{email}\");"
                    },
                    testEndpoints = new
                    {
                        getUser = $"/api/Users/{encoded}",
                        getSubusers = $"/api/EnhancedSubuser/by-parent/{encoded}",
                        getSessions = $"/api/Sessions/by-email/{encoded}"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error encoding email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test endpoint: Decode Base64 email
        /// GET /api/EmailDebug/decode/{encodedEmail}
        /// </summary>
        [HttpGet("decode/{encodedEmail}")]
        [AllowAnonymous]
        public IActionResult DecodeEmail(string encodedEmail)
        {
            try
            {
                var decoded = Base64EmailEncoder.Decode(encodedEmail);

                if (!Base64EmailEncoder.IsValidEmail(decoded))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Decoded value is not a valid email",
                        decodedValue = decoded
                    });
                }

                return Ok(new
                {
                    success = true,
                    base64Email = encodedEmail,
                    plainEmail = decoded,
                    isValid = Base64EmailEncoder.IsValidEmail(decoded)
                });
            }
            catch (FormatException)
            {
                // Check if it's already a plain email
                if (Base64EmailEncoder.IsValidEmail(encodedEmail))
                {
                    return Ok(new
                    {
                        success = true,
                        message = "This is already a plain email (not Base64)",
                        plainEmail = encodedEmail,
                        shouldBe = Base64EmailEncoder.Encode(encodedEmail),
                        note = "For API calls, encode this first"
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Base64 encoding",
                    providedValue = encodedEmail,
                    hint = "Use /api/EmailDebug/encode/{email} to get correct encoding"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error decoding email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test endpoint: Check if email parameter will work
        /// GET /api/EmailDebug/test/{emailOrEncoded}
        /// </summary>
        [HttpGet("test/{emailOrEncoded}")]
        [AllowAnonymous]
        public IActionResult TestEmailParameter(string emailOrEncoded)
        {
            var result = new
            {
                inputValue = emailOrEncoded,
                isPlainEmail = Base64EmailEncoder.IsValidEmail(emailOrEncoded),
                tests = new List<object>()
            };

            // Test 1: Is it a plain email?
            if (Base64EmailEncoder.IsValidEmail(emailOrEncoded))
            {
                result.tests.Add(new
                {
                    test = "Plain Email Detection",
                    status = "✅ PASS",
                    message = "This is a valid plain email",
                    recommendation = "Encode before using in API calls",
                    encoded = Base64EmailEncoder.Encode(emailOrEncoded)
                });
            }
            else
            {
                result.tests.Add(new
                {
                    test = "Plain Email Detection",
                    status = "❌ FAIL",
                    message = "This is not a plain email"
                });

                // Test 2: Can it be decoded as Base64?
                try
                {
                    var decoded = Base64EmailEncoder.Decode(emailOrEncoded);
                    
                    if (Base64EmailEncoder.IsValidEmail(decoded))
                    {
                        result.tests.Add(new
                        {
                            test = "Base64 Decoding",
                            status = "✅ PASS",
                            message = "Successfully decoded to valid email",
                            decodedEmail = decoded,
                            recommendation = "This value is ready for API use"
                        });
                    }
                    else
                    {
                        result.tests.Add(new
                        {
                            test = "Base64 Decoding",
                            status = "⚠️ WARNING",
                            message = "Decoded but not a valid email",
                            decodedValue = decoded
                        });
                    }
                }
                catch (FormatException)
                {
                    result.tests.Add(new
                    {
                        test = "Base64 Decoding",
                        status = "❌ FAIL",
                        message = "Not valid Base64 encoding",
                        recommendation = "Provide either plain email or Base64-encoded email"
                    });
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Get current authenticated user's email
        /// Useful for debugging JWT claims
        /// </summary>
        [HttpGet("current-user")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                success = true,
                currentUser = new
                {
                    email = email,
                    emailClaim = emailClaim,
                    role = role,
                    allClaims = User.Claims.Select(c => new
                    {
                        type = c.Type,
                        value = c.Value
                    }).ToList()
                },
                encodedEmail = email != null ? Base64EmailEncoder.Encode(email) : null,
                testEndpoints = email != null ? new
                {
                    getSubusers = $"/api/EnhancedSubuser/by-parent/{Base64EmailEncoder.Encode(email)}",
                    getUserSessions = $"/api/Sessions/by-email/{Base64EmailEncoder.Encode(email)}",
                    getMachines = $"/api/Machines/by-email/{Base64EmailEncoder.Encode(email)}"
                } : null
            });
        }

        /// <summary>
        /// Comprehensive test of all email endpoints
        /// Tests if email parameter works in different scenarios
        /// </summary>
        [HttpPost("test-all")]
        [AllowAnonymous]
        public IActionResult TestAllScenarios([FromBody] EmailTestRequest request)
        {
            var results = new List<object>();

            // Test 1: Plain email encoding
            try
            {
                var encoded = Base64EmailEncoder.Encode(request.Email);
                results.Add(new
                {
                    test = "Encoding Plain Email",
                    status = "✅ PASS",
                    input = request.Email,
                    output = encoded
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    test = "Encoding Plain Email",
                    status = "❌ FAIL",
                    error = ex.Message
                });
            }

            // Test 2: Decode and re-encode (round trip)
            try
            {
                var encoded = Base64EmailEncoder.Encode(request.Email);
                var decoded = Base64EmailEncoder.Decode(encoded);
                var matches = decoded == request.Email;

                results.Add(new
                {
                    test = "Round Trip (Encode → Decode)",
                    status = matches ? "✅ PASS" : "❌ FAIL",
                    original = request.Email,
                    encoded = encoded,
                    decoded = decoded,
                    matches = matches
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    test = "Round Trip Test",
                    status = "❌ FAIL",
                    error = ex.Message
                });
            }

            // Test 3: Email validation
            try
            {
                var isValid = Base64EmailEncoder.IsValidEmail(request.Email);
                results.Add(new
                {
                    test = "Email Validation",
                    status = isValid ? "✅ PASS" : "❌ FAIL",
                    email = request.Email,
                    isValid = isValid
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    test = "Email Validation",
                    status = "❌ FAIL",
                    error = ex.Message
                });
            }

            return Ok(new
            {
                success = true,
                testedEmail = request.Email,
                totalTests = results.Count,
                passed = results.Count(r => r.ToString()!.Contains("✅")),
                results = results
            });
        }

        public class EmailTestRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        /// <summary>
        /// Check database for subusers - DEBUGGING ONLY
        /// GET /api/EmailDebug/check-database/{parentEmail}
        /// </summary>
        [HttpGet("check-database/{parentEmail}")]
        [Authorize]
        public async Task<IActionResult> CheckDatabase(string parentEmail)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("🔍 Checking database for parent email: {ParentEmail}", parentEmail);

                // Get all subusers for this parent
                var subusers = await context.subuser
                    .Where(s => s.user_email == parentEmail)
                    .Select(s => new
                    {
                        s.subuser_id,
                        s.subuser_email,
                        s.user_email,
                        s.Name
                    })
                    .ToListAsync();

                // Get all unique parent emails
                var allParentEmails = await context.subuser
                    .Select(s => s.user_email)
                    .Distinct()
                    .ToListAsync();

                // Get total subusers count
                var totalSubusers = await context.subuser.CountAsync();

                // Check if exact match exists
                var exactMatch = allParentEmails.Any(e => e == parentEmail);

                // Check case-insensitive match
                var caseInsensitiveMatch = allParentEmails
                    .FirstOrDefault(e => e.Equals(parentEmail, StringComparison.OrdinalIgnoreCase));

                return Ok(new
                {
                    success = true,
                    searchedParentEmail = parentEmail,
                    foundSubusers = subusers.Count,
                    subusers = subusers,
                    databaseInfo = new
                    {
                        totalSubusersInDb = totalSubusers,
                        totalUniqueParents = allParentEmails.Count,
                        allParentEmails = allParentEmails,
                        exactMatch = exactMatch,
                        caseInsensitiveMatch = caseInsensitiveMatch != null,
                        matchedEmail = caseInsensitiveMatch
                    },
                    debugging = new
                    {
                        searchQuery = $"WHERE user_email = '{parentEmail}'",
                        hint = exactMatch ? 
                            "✅ Exact match found" : 
                            caseInsensitiveMatch != null ? 
                                $"⚠️ Case mismatch: Database has '{caseInsensitiveMatch}', you searched '{parentEmail}'" :
                                $"❌ No match found. Available parent emails: {string.Join(", ", allParentEmails)}"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error checking database",
                    error = ex.Message
                });
            }
        }
    }
}
