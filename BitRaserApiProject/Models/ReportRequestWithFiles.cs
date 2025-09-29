using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace BitRaserApiProject.Models;

// Model for handling multipart form data with file uploads in Swagger
public class ReportRequestWithFiles
{
    // Use FromForm attribute for complex object binding in multipart requests
    [FromForm]
    public string? ReportDataJson { get; set; }
    
    // This will be populated from ReportDataJson in the controller
    public ReportData? ReportData { get; set; }

    [FromForm]
    public string? ReportTitle { get; set; }
    
    [FromForm]
    public string? HeaderText { get; set; }

    // Image files for upload via multipart/form-data
    [FromForm]
    public IFormFile? HeaderLeftLogo { get; set; }
    
    [FromForm]
    public IFormFile? HeaderRightLogo { get; set; }
    
    [FromForm]
    public IFormFile? WatermarkImage { get; set; }

    [FromForm]
    public string? TechnicianName { get; set; }
    
    [FromForm]
    public string? TechnicianDept { get; set; }
    
    [FromForm]
    public string? ValidatorName { get; set; }
    
    [FromForm]
    public string? ValidatorDept { get; set; }

    [FromForm]
    public IFormFile? TechnicianSignature { get; set; }
    
    [FromForm]
    public IFormFile? ValidatorSignature { get; set; }
}