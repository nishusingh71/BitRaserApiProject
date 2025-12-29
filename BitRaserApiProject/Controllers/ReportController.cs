using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Controllers;

//[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly PdfService _pdfService;
    private readonly ICacheService _cacheService;

    public ReportController(PdfService pdfService, ICacheService cacheService)
    {
        _pdfService = pdfService;
        _cacheService = cacheService;
    }

    [HttpPost("generate")] // Accepts JSON body
    public IActionResult Generate([FromBody] ReportRequest request)
    {
        // Skip ModelState validation for now to avoid Required field issues
        var pdfBytes = _pdfService.GenerateReport(request);
        return File(pdfBytes, "application/pdf", (request.ReportTitle ?? "report") + ".pdf");
    }

    [HttpPost("generate-with-images")] // Accepts multipart/form-data with images
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> GenerateWithImages([FromForm] ReportRequestWithFiles request)
    {
        // Parse ReportData from JSON string if provided
        ReportData? reportData = null;
        if (!string.IsNullOrEmpty(request.ReportDataJson))
        {
            try
            {
                reportData = JsonSerializer.Deserialize<ReportData>(request.ReportDataJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                return BadRequest("Invalid ReportData JSON format");
            }
        }

        var reportRequest = new ReportRequest
        {
            ReportData = reportData ?? new ReportData(),
            ReportTitle = request.ReportTitle,
            HeaderText = request.HeaderText,
            TechnicianName = request.TechnicianName,
            TechnicianDept = request.TechnicianDept,
            ValidatorName = request.ValidatorName,
            ValidatorDept = request.ValidatorDept
        };

        // Convert uploaded files to byte arrays
        if (request.HeaderLeftLogo != null && request.HeaderLeftLogo.Length > 0)
        {
            using var ms = new MemoryStream();
            await request.HeaderLeftLogo.CopyToAsync(ms);
            reportRequest.HeaderLeftLogo = ms.ToArray();
        }

        if (request.HeaderRightLogo != null && request.HeaderRightLogo.Length > 0)
        {
            using var ms = new MemoryStream();
            await request.HeaderRightLogo.CopyToAsync(ms);
            reportRequest.HeaderRightLogo = ms.ToArray();
        }

        if (request.WatermarkImage != null && request.WatermarkImage.Length > 0)
        {
            using var ms = new MemoryStream();
            await request.WatermarkImage.CopyToAsync(ms);
            reportRequest.WatermarkImage = ms.ToArray();
        }

        if (request.TechnicianSignature != null && request.TechnicianSignature.Length > 0)
        {
            using var ms = new MemoryStream();
            await request.TechnicianSignature.CopyToAsync(ms);
            reportRequest.TechnicianSignature = ms.ToArray();
        }

        if (request.ValidatorSignature != null && request.ValidatorSignature.Length > 0)
        {
            using var ms = new MemoryStream();
            await request.ValidatorSignature.CopyToAsync(ms);
            reportRequest.ValidatorSignature = ms.ToArray();
        }

        var pdfBytes = _pdfService.GenerateReport(reportRequest);
        return File(pdfBytes, "application/pdf", (request.ReportTitle ?? "report") + ".pdf");
    }
}