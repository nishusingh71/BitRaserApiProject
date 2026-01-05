# ✅ Auto-Fix EnhancedAuditReportsController DTOs
# This script automatically adds missing DTOs and helper methods

Write-Host "🔧 Auto-Fix Starting..." -ForegroundColor Cyan
Write-Host ""

$filePath = "BitRaserApiProject\Controllers\EnhancedAuditReportsController.cs"

# Check if file exists
if (-not (Test-Path $filePath)) {
    Write-Host "❌ ERROR: File not found: $filePath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ File found: $filePath" -ForegroundColor Green
Write-Host ""

# Read current content
$content = Get-Content $filePath -Raw

# Check if DTOs already exist
if ($content -match "class ReportFilterRequest") {
    Write-Host "⚠️  DTOs already exist in file!" -ForegroundColor Yellow
    Write-Host "   File may already be fixed." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Would you like to rebuild?" -ForegroundColor Cyan
    $rebuild = Read-Host "Enter 'yes' to rebuild, or press Enter to skip"
    
    if ($rebuild -ne "yes") {
    Write-Host "Skipping fix. Running build check..." -ForegroundColor Cyan
   Write-Host ""
        
      Push-Location "BitRaserApiProject"
     dotnet build
        Pop-Location
        
        exit 0
    }
}

Write-Host "📝 Creating backup..." -ForegroundColor Cyan
$backupPath = "$filePath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Copy-Item $filePath $backupPath
Write-Host "✅ Backup created: $backupPath" -ForegroundColor Green
Write-Host ""

# The missing code to add
$missingCode = @'

      /// <summary>
        /// Parse D-Secure report_details_json with proper field mapping
        /// </summary>
        private async Task<ReportData> ParseDSecureReportData(audit_reports auditReport)
        {
    var reportData = new ReportData();

            try
            {
            if (string.IsNullOrEmpty(auditReport.report_details_json) ||
             auditReport.report_details_json == "{}")
         {
       return CreateDefaultReportData(auditReport);
         }

                using var doc = JsonDocument.Parse(auditReport.report_details_json);
     var root = doc.RootElement;

            reportData.ReportId = GetJsonString(root, "report_id") ?? auditReport.report_id.ToString();
                reportData.ReportDate = GetJsonString(root, "datetime") ?? auditReport.report_datetime.ToString("yyyy-MM-dd HH:mm:ss");
       reportData.DigitalSignature = GetJsonString(root, "digital_signature") ?? $"DSE-{auditReport.report_id}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

   reportData.SoftwareName = GetJsonString(root, "software_name") ?? "D-SecureErase";
     reportData.ProductVersion = GetJsonString(root, "product_version") ?? "1.0";
                reportData.ComputerName = GetJsonString(root, "computer_name") ?? "Unknown";
       reportData.MacAddress = GetJsonString(root, "mac_address") ?? "Unknown";
      reportData.Manufacturer = GetJsonString(root, "manufacturer") ?? "Unknown";

      var os = GetJsonString(root, "os");
           var osVersion = GetJsonString(root, "os_version");

             if (string.IsNullOrWhiteSpace(os) || os.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase))
       {
   os = string.Empty;
 }
                else
              {
               os = os.Trim();
         }

         if (string.IsNullOrWhiteSpace(osVersion) || osVersion.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase))
     {
osVersion = string.Empty;
        }
 else
          {
           osVersion = osVersion.Trim();
    }

   if (!string.IsNullOrEmpty(os) && !string.IsNullOrEmpty(osVersion))
             {
          reportData.OSVersion = $"{os} {osVersion}";
   }
         else if (!string.IsNullOrEmpty(osVersion))
  {
           reportData.OSVersion = osVersion;
      }
     else if (!string.IsNullOrEmpty(os))
           {
                    reportData.OSVersion = os;
             }
    else
                {
   reportData.OSVersion = string.Empty;
         }

      reportData.EraserMethod = GetJsonString(root, "eraser_method") ?? auditReport.erasure_method ?? "Unknown";
    reportData.Status = GetJsonString(root, "status") ?? (auditReport.synced ? "Completed" : "Pending");
         reportData.ProcessMode = GetJsonString(root, "process_mode") ?? "Standard Erasure";
      reportData.ValidationMethod = GetJsonString(root, "validation_method") ?? "Not Specified";
         reportData.EraserStartTime = GetJsonString(root, "Eraser_Start_Time");
        reportData.EraserEndTime = GetJsonString(root, "Eraser_End_Time");
       reportData.TotalFiles = GetJsonInt(root, "total_files");
     reportData.ErasedFiles = GetJsonInt(root, "erased_files");
    reportData.FailedFiles = GetJsonInt(root, "failed_files");

       if (root.TryGetProperty("erasure_log", out var logArray) && logArray.ValueKind == JsonValueKind.Array)
         {
  reportData.ErasureLog = new List<ErasureLogEntry>();

        foreach (var logEntry in logArray.EnumerateArray())
  {
          var entry = new ErasureLogEntry
       {
    Target = GetJsonString(logEntry, "target") ?? "Unknown",
       Status = GetJsonString(logEntry, "status") ?? "Unknown",
            Capacity = GetJsonString(logEntry, "free_space") ?? GetJsonString(logEntry, "dummy_file_size") ?? "Unknown",
          Size = GetJsonString(logEntry, "dummy_file_size") ?? "Unknown",
 TotalSectors = GetJsonString(logEntry, "sectors_erased") ?? "Unknown",
              SectorsErased = GetJsonString(logEntry, "sectors_erased") ?? "Unknown"
           };

            reportData.ErasureLog.Add(entry);
           }
        }

return reportData;
            }
       catch (JsonException ex)
            {
        return CreateDefaultReportData(auditReport);
            }
        }

      private ReportData CreateDefaultReportData(audit_reports auditReport)
   {
         return new ReportData
         {
              ReportId = auditReport.report_id.ToString(),
            ReportDate = auditReport.report_datetime.ToString("yyyy-MM-dd HH:mm:ss"),
      DigitalSignature = $"DSE-{auditReport.report_id}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
         SoftwareName = "D-SecureErase",
      ProductVersion = "1.0",
                ComputerName = auditReport.client_email,
    MacAddress = "Unknown",
    Manufacturer = "Unknown",
              OSVersion = string.Empty,
     EraserMethod = auditReport.erasure_method ?? "Unknown",
              Status = auditReport.synced ? "Completed" : "Pending",
     ProcessMode = "Standard Erasure",
   ValidationMethod = "Not Specified",
      ErasureLog = new List<ErasureLogEntry>
      {
        new ErasureLogEntry
   {
      Target = $"Report #{auditReport.report_id}",
   Status = auditReport.synced ? "Completed" : "Pending",
                 Capacity = "See database for details",
        Size = "Unknown",
           TotalSectors = "Unknown",
   SectorsErased = "Unknown"
            }
                }
       };
   }

        private string? GetJsonString(JsonElement element, string propertyName)
        {
 if (element.TryGetProperty(propertyName, out var prop))
        {
                return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
            }
return null;
   }

        private int GetJsonInt(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
        {
 if (prop.ValueKind == JsonValueKind.Number)
  {
          return prop.GetInt32();
    }
         if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var result))
   {
    return result;
       }
  }
   return 0;
   }

        #endregion
  }

    /// <summary>
    /// User details for PDF generation
    /// </summary>
    internal class UserDetailsForPDF
    {
        public string? UserName { get; set; }
        public string? Department { get; set; }
    }

    /// <summary>
    /// Report filter request
    /// </summary>
    public class ReportFilterRequest
  {
        public string? ClientEmail { get; set; }
        public string? ErasureMethod { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool? SyncedOnly { get; set; }
 public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 100;
    }

    /// <summary>
    /// Audit report creation request
    /// </summary>
    public class AuditReportCreateRequest
    {
    public string ClientEmail { get; set; } = string.Empty;
  public string? ReportName { get; set; }
        public string? ErasureMethod { get; set; }
        public string? ReportDetailsJson { get; set; }
    }

    /// <summary>
  /// Audit report update request
    /// </summary>
    public class AuditReportUpdateRequest
    {
    public int ReportId { get; set; }
        public string? ClientEmail { get; set; }
        public string? ReportName { get; set; }
public string? ErasureMethod { get; set; }
        public string? ReportDetailsJson { get; set; }
    }

    /// <summary>
    /// Report reservation request
    /// </summary>
    public class ReportReservationRequest
    {
        public string ClientEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Report upload request
    /// </summary>
    public class ReportUploadRequest
    {
    public int ReportId { get; set; }
        public string ClientEmail { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
     public string ErasureMethod { get; set; } = string.Empty;
        public string ReportDetailsJson { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sync confirmation request
    /// </summary>
    public class SyncConfirmationRequest
    {
    public string ClientEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Report export request
 /// </summary>
    public class ReportExportRequest
    {
      public string? ClientEmail { get; set; }
   public DateTime? DateFrom { get; set; }
      public DateTime? DateTo { get; set; }
    }

    /// <summary>
    /// PDF export options
    /// </summary>
  public class PdfExportOptions
    {
        public string? HeaderText { get; set; }
     public string? TechnicianName { get; set; }
   public string? TechnicianDept { get; set; }
        public string? ValidatorName { get; set; }
        public string? ValidatorDept { get; set; }
    }

    /// <summary>
    /// Report export with files request
    /// </summary>
    public class ReportExportWithFilesRequest
    {
        [FromForm]
        public string? ClientEmail { get; set; }

        [FromForm]
        public DateTime? DateFrom { get; set; }

        [FromForm]
        public DateTime? DateTo { get; set; }

  [FromForm]
        public string? ReportTitle { get; set; }

        [FromForm]
        public string? HeaderText { get; set; }

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

    /// <summary>
    /// Single report export with files request
    /// </summary>
    public class SingleReportExportWithFilesRequest
 {
        [FromForm]
        public string? ReportTitle { get; set; }

        [FromForm]
    public string? HeaderText { get; set; }

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
}
'@

Write-Host "✏️  Adding missing DTOs and helper methods..." -ForegroundColor Cyan

# Find the last closing brace
$lastBraceIndex = $content.LastIndexOf("}")

if ($lastBraceIndex -eq -1) {
    Write-Host "❌ ERROR: Could not find closing brace in file!" -ForegroundColor Red
    exit 1
}

# Insert the missing code before the last closing brace
$newContent = $content.Substring(0, $lastBraceIndex) + $missingCode + "`n" + $content.Substring($lastBraceIndex)

# Write the updated content
Set-Content $filePath $newContent -NoNewline

Write-Host "✅ DTOs and helper methods added successfully!" -ForegroundColor Green
Write-Host ""

Write-Host "🔨 Building project..." -ForegroundColor Cyan
Write-Host ""

Push-Location "BitRaserApiProject"
$buildOutput = dotnet build 2>&1
$buildSuccess = $LASTEXITCODE -eq 0
Pop-Location

if ($buildSuccess) {
    Write-Host "✅ BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎉 All 35 errors fixed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Summary:" -ForegroundColor Cyan
    Write-Host "  ✅ 11 DTO classes added" -ForegroundColor Green
    Write-Host "  ✅ 4 helper methods added" -ForegroundColor Green
    Write-Host "  ✅ Build: SUCCESS" -ForegroundColor Green
 Write-Host ""
} else {
    Write-Host "❌ BUILD FAILED!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Build Output:" -ForegroundColor Yellow
    Write-Host $buildOutput
    Write-Host ""
    Write-Host "💡 You can restore backup: $backupPath" -ForegroundColor Cyan
}

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
