# 📄 **PDF Export Feature - Enhanced Audit Reports with File Uploads**

## 🎯 **Overview**

आपके `EnhancedAuditReportsController` में अब comprehensive PDF export functionality add कर दी गई है जो आपके existing PDF service का use करती है, साथ ही **signature upload**, **header logo upload**, और **watermark upload** की सुविधा भी है।

---

## 🚀 **New PDF Export Endpoints**

### **1. Basic PDF Export (No File Uploads)**

#### **Multiple Reports PDF Export**
```http
GET /api/EnhancedAuditReports/export-pdf
Authorization: Bearer <token>
Permission Required: EXPORT_REPORTS
```

#### **Single Report PDF Export**
```http
GET /api/EnhancedAuditReports/{id}/export-pdf
Authorization: Bearer <token>
Permission Required: EXPORT_REPORTS
```

### **2. Advanced PDF Export (With File Uploads)**

#### **Multiple Reports PDF Export with Files**
```http
POST /api/EnhancedAuditReports/export-pdf-with-files
Authorization: Bearer <token>
Content-Type: multipart/form-data
Permission Required: EXPORT_REPORTS
```

#### **Single Report PDF Export with Files**
```http
POST /api/EnhancedAuditReports/{id}/export-pdf-with-files
Authorization: Bearer <token>
Content-Type: multipart/form-data
Permission Required: EXPORT_REPORTS
```

---

## 📋 **File Upload Parameters**

### **Form Fields Supported:**

#### **Basic Information**
- `clientEmail` (string) - Filter by client email
- `dateFrom` (DateTime) - Start date filter
- `dateTo` (DateTime) - End date filter
- `reportTitle` (string) - Custom report title
- `headerText` (string) - Custom header text

#### **Personnel Information**
- `technicianName` (string) - Technician name
- `technicianDept` (string) - Technician department
- `validatorName` (string) - Validator name
- `validatorDept` (string) - Validator department

#### **File Uploads (IFormFile)**
- `headerLeftLogo` - Left header logo image
- `headerRightLogo` - Right header logo image
- `watermarkImage` - Background watermark image
- `technicianSignature` - Technician signature image
- `validatorSignature` - Validator signature image

---

## 🔧 **Features Implemented**

### **✅ Multiple Report PDF Export**
- **Summary Report**: Creates a consolidated PDF with all selected reports
- **Role-based Filtering**: Users see only their own reports unless admin
- **Date Range Filtering**: Filter reports by date range
- **Client Email Filtering**: Filter by specific client email
- **Professional Layout**: Uses your existing PDF service design

### **✅ Single Report PDF Export**
- **Detailed Report**: Full report with all erasure log details
- **JSON Parsing**: Automatically parses `report_details_json` field
- **Fallback Handling**: Creates basic report if JSON parsing fails
- **Custom Options**: Allows customization of technician/validator info
- **Same Design**: Uses your existing PDF service for consistency

### **✅ Security Features**
- **Permission-based Access**: Requires `EXPORT_REPORTS` permission
- **Ownership Validation**: Users can only export their own reports
- **Admin Override**: Admins can export any reports
- **Role-based Filtering**: Automatic filtering based on user role

---

## 📊 **PDF Content Structure**

### **Summary Report (Multiple Reports)**
```
📄 Audit Reports Summary (X reports)
├── Report Info
│   ├── Report ID: SUMMARY_YYYYMMDDHHMMSS
│   ├── Report Date: Current timestamp
│   ├── Software: BitRaser API v1.0
│   └── Status: Completed
├── Process Summary
│   ├── Process Mode: Summary Export
│   ├── Technician: System (API Service)
│   └── Validator: System (Automated Export)
└── Annexure: Reports List
    ├── Report Name | Status | Method | Date
    ├── Report 1    | Synced | DoD    | 2024-01-15
    └── Report 2    | Pending| NSA    | 2024-01-16
```

### **Single Report PDF**
```
📄 Individual Report (Full Detail)
├── Process Status
├── Report Info (from database + JSON)
├── System Info (from JSON)
├── Process Summary
├── Erasure Details
├── Personnel Info
└── Annexure: Erasure Log (from JSON)
```

---

## 🧪 **Testing the Enhanced PDF Export**

### **1. Basic PDF Export (GET Request)**
```bash
# Login first
curl -X POST http://localhost:4000/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"password"}'

# Export all reports as PDF (basic)
curl -X GET "http://localhost:4000/api/EnhancedAuditReports/export-pdf" \
  -H "Authorization: Bearer <token>" \
  --output "basic_reports.pdf"

# Export single report (basic)
curl -X GET "http://localhost:4000/api/EnhancedAuditReports/123/export-pdf" \
  -H "Authorization: Bearer <token>" \
  --output "basic_report_123.pdf"
```

### **2. Advanced PDF Export with File Uploads**
```bash
# Export multiple reports with custom signatures and logos
curl -X POST "http://localhost:4000/api/EnhancedAuditReports/export-pdf-with-files" \
  -H "Authorization: Bearer <token>" \
  -F "reportTitle=Custom Report Title" \
  -F "headerText=My Company - Audit Report" \
  -F "technicianName=John Doe" \
  -F "technicianDept=IT Security" \
  -F "validatorName=Jane Smith" \
  -F "validatorDept=Quality Assurance" \
  -F "headerLeftLogo=@/path/to/left_logo.png" \
  -F "headerRightLogo=@/path/to/right_logo.png" \
  -F "watermarkImage=@/path/to/watermark.png" \
  -F "technicianSignature=@/path/to/tech_signature.png" \
  -F "validatorSignature=@/path/to/validator_signature.png" \
  -F "dateFrom=2024-01-01" \
  -F "dateTo=2024-12-31" \
  --output "custom_reports.pdf"

# Export single report with files
curl -X POST "http://localhost:4000/api/EnhancedAuditReports/123/export-pdf-with-files" \
  -H "Authorization: Bearer <token>" \
  -F "reportTitle=Custom Single Report" \
  -F "headerText=Detailed Audit Report" \
  -F "technicianName=John Doe" \
  -F "technicianSignature=@/path/to/signature.png" \
  -F "headerLeftLogo=@/path/to/logo.png" \
  --output "custom_report_123.pdf"
```

---

## 🎯 **HTML Form Examples**

### **Multiple Reports Export Form**
```html
<form action="/api/EnhancedAuditReports/export-pdf-with-files" method="post" enctype="multipart/form-data">
  <input type="hidden" name="Authorization" value="Bearer <token>" />
  
  <!-- Basic Info -->
  <input type="text" name="reportTitle" placeholder="Report Title" />
  <input type="text" name="headerText" placeholder="Header Text" />
  <input type="email" name="clientEmail" placeholder="Client Email Filter" />
  <input type="date" name="dateFrom" />
  <input type="date" name="dateTo" />
  
  <!-- Personnel Info -->
  <input type="text" name="technicianName" placeholder="Technician Name" />
  <input type="text" name="technicianDept" placeholder="Technician Department" />
  <input type="text" name="validatorName" placeholder="Validator Name" />
  <input type="text" name="validatorDept" placeholder="Validator Department" />
  
  <!-- File Uploads -->
  <label>Left Header Logo:</label>
  <input type="file" name="headerLeftLogo" accept="image/*" />
  
  <label>Right Header Logo:</label>
  <input type="file" name="headerRightLogo" accept="image/*" />
  
  <label>Watermark Image:</label>
  <input type="file" name="watermarkImage" accept="image/*" />
  
  <label>Technician Signature:</label>
  <input type="file" name="technicianSignature" accept="image/*" />
  
  <label>Validator Signature:</label>
  <input type="file" name="validatorSignature" accept="image/*" />
  
  <button type="submit">Export PDF with Files</button>
</form>
```

### **Single Report Export Form**
```html
<form action="/api/EnhancedAuditReports/123/export-pdf-with-files" method="post" enctype="multipart/form-data">
  <input type="hidden" name="Authorization" value="Bearer <token>" />
  
  <!-- Basic Info -->
  <input type="text" name="reportTitle" placeholder="Report Title" />
  <input type="text" name="headerText" placeholder="Header Text" />
  
  <!-- Personnel Info -->
  <input type="text" name="technicianName" placeholder="Technician Name" />
  <input type="text" name="technicianDept" placeholder="Technician Department" />
  
  <!-- File Uploads -->
  <label>Header Logo:</label>
  <input type="file" name="headerLeftLogo" accept="image/*" />
  
  <label>Technician Signature:</label>
  <input type="file" name="technicianSignature" accept="image/*" />
  
  <label>Validator Signature:</label>
  <input type="file" name="validatorSignature" accept="image/*" />
  
  <button type="submit">Export Single Report PDF</button>
</form>
```

---

## 🎯 **JavaScript/Frontend Integration**

### **Basic PDF Export (No Files)**
```javascript
async function exportBasicPDF(reportId = null, filters = {}) {
    const token = localStorage.getItem('authToken');
    
    let url = '/api/EnhancedAuditReports/export-pdf';
    if (reportId) {
        url = `/api/EnhancedAuditReports/${reportId}/export-pdf`;
    }
    
    // Build query string for basic export
    const params = new URLSearchParams();
    if (filters.dateFrom) params.append('dateFrom', filters.dateFrom);
    if (filters.dateTo) params.append('dateTo', filters.dateTo);
    if (filters.clientEmail) params.append('clientEmail', filters.clientEmail);
    
    if (params.toString()) {
        url += '?' + params.toString();
    }
    
    try {
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (response.ok) {
            const blob = await response.blob();
            downloadBlob(blob, `report_${Date.now()}.pdf`);
        } else {
            console.error('Export failed:', response.statusText);
        }
    } catch (error) {
        console.error('Export error:', error);
    }
}
```

### **Advanced PDF Export with Files**
```javascript
async function exportPDFWithFiles(reportId = null, formData) {
    const token = localStorage.getItem('authToken');
    
    let url = '/api/EnhancedAuditReports/export-pdf-with-files';
    if (reportId) {
        url = `/api/EnhancedAuditReports/${reportId}/export-pdf-with-files`;
    }
    
    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`
                // Don't set Content-Type for FormData - browser will set it with boundary
            },
            body: formData
        });
        
        if (response.ok) {
            const blob = await response.blob();
            downloadBlob(blob, `custom_report_${Date.now()}.pdf`);
        } else {
            console.error('Export failed:', response.statusText);
        }
    } catch (error) {
        console.error('Export error:', error);
    }
}

// Helper function to create FormData
function createReportFormData(options = {}) {
    const formData = new FormData();
    
    // Add text fields
    if (options.reportTitle) formData.append('reportTitle', options.reportTitle);
    if (options.headerText) formData.append('headerText', options.headerText);
    if (options.technicianName) formData.append('technicianName', options.technicianName);
    if (options.technicianDept) formData.append('technicianDept', options.technicianDept);
    if (options.validatorName) formData.append('validatorName', options.validatorName);
    if (options.validatorDept) formData.append('validatorDept', options.validatorDept);
    if (options.clientEmail) formData.append('clientEmail', options.clientEmail);
    if (options.dateFrom) formData.append('dateFrom', options.dateFrom);
    if (options.dateTo) formData.append('dateTo', options.dateTo);
    
    // Add file fields
    if (options.headerLeftLogo) formData.append('headerLeftLogo', options.headerLeftLogo);
    if (options.headerRightLogo) formData.append('headerRightLogo', options.headerRightLogo);
    if (options.watermarkImage) formData.append('watermarkImage', options.watermarkImage);
    if (options.technicianSignature) formData.append('technicianSignature', options.technicianSignature);
    if (options.validatorSignature) formData.append('validatorSignature', options.validatorSignature);
    
    return formData;
}

// Helper function to download blob
function downloadBlob(blob, filename) {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
}

// Usage examples
// Basic export
exportBasicPDF(null, {
    dateFrom: '2024-01-01',
    dateTo: '2024-12-31'
});

// Advanced export with files
const formData = createReportFormData({
    reportTitle: 'Custom Report',
    headerText: 'My Company - Audit Report',
    technicianName: 'John Doe',
    technicianDept: 'IT Security',
    headerLeftLogo: document.getElementById('leftLogo').files[0],
    technicianSignature: document.getElementById('techSignature').files[0],
    dateFrom: '2024-01-01',
    dateTo: '2024-12-31'
});

exportPDFWithFiles(null, formData);
```

---

## 🔧 **C# Client Integration**

```csharp
public class EnhancedReportExportService
{
    private readonly HttpClient _httpClient;
    
    public EnhancedReportExportService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    // Basic PDF export
    public async Task<byte[]> ExportBasicPDFAsync(int? reportId = null, string? clientEmail = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(clientEmail)) queryParams.Add($"clientEmail={Uri.EscapeDataString(clientEmail)}");
        if (dateFrom.HasValue) queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue) queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
        
        var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        
        string endpoint = reportId.HasValue 
            ? $"/api/EnhancedAuditReports/{reportId}/export-pdf{query}"
            : $"/api/EnhancedAuditReports/export-pdf{query}";
            
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
    
    // Advanced PDF export with files
    public async Task<byte[]> ExportPDFWithFilesAsync(int? reportId = null, ReportExportWithFilesOptions options = null)
    {
        options ??= new ReportExportWithFilesOptions();
        
        using var formData = new MultipartFormDataContent();
        
        // Add text fields
        if (!string.IsNullOrEmpty(options.ReportTitle))
            formData.Add(new StringContent(options.ReportTitle), "reportTitle");
        if (!string.IsNullOrEmpty(options.HeaderText))
            formData.Add(new StringContent(options.HeaderText), "headerText");
        if (!string.IsNullOrEmpty(options.TechnicianName))
            formData.Add(new StringContent(options.TechnicianName), "technicianName");
        if (!string.IsNullOrEmpty(options.TechnicianDept))
            formData.Add(new StringContent(options.TechnicianDept), "technicianDept");
        if (!string.IsNullOrEmpty(options.ValidatorName))
            formData.Add(new StringContent(options.ValidatorName), "validatorName");
        if (!string.IsNullOrEmpty(options.ValidatorDept))
            formData.Add(new StringContent(options.ValidatorDept), "validatorDept");
        if (!string.IsNullOrEmpty(options.ClientEmail))
            formData.Add(new StringContent(options.ClientEmail), "clientEmail");
        if (options.DateFrom.HasValue)
            formData.Add(new StringContent(options.DateFrom.Value.ToString("yyyy-MM-dd")), "dateFrom");
        if (options.DateTo.HasValue)
            formData.Add(new StringContent(options.DateTo.Value.ToString("yyyy-MM-dd")), "dateTo");
        
        // Add file fields
        if (options.HeaderLeftLogo != null)
            formData.Add(new ByteArrayContent(options.HeaderLeftLogo), "headerLeftLogo", "left_logo.png");
        if (options.HeaderRightLogo != null)
            formData.Add(new ByteArrayContent(options.HeaderRightLogo), "headerRightLogo", "right_logo.png");
        if (options.WatermarkImage != null)
            formData.Add(new ByteArrayContent(options.WatermarkImage), "watermarkImage", "watermark.png");
        if (options.TechnicianSignature != null)
            formData.Add(new ByteArrayContent(options.TechnicianSignature), "technicianSignature", "tech_signature.png");
        if (options.ValidatorSignature != null)
            formData.Add(new ByteArrayContent(options.ValidatorSignature), "validatorSignature", "validator_signature.png");
        
        string endpoint = reportId.HasValue 
            ? $"/api/EnhancedAuditReports/{reportId}/export-pdf-with-files"
            : $"/api/EnhancedAuditReports/export-pdf-with-files";
            
        var response = await _httpClient.PostAsync(endpoint, formData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}

public class ReportExportWithFilesOptions
{
    public string? ReportTitle { get; set; }
    public string? HeaderText { get; set; }
    public string? TechnicianName { get; set; }
    public string? TechnicianDept { get; set; }
    public string? ValidatorName { get; set; }
    public string? ValidatorDept { get; set; }
    public string? ClientEmail { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    
    public byte[]? HeaderLeftLogo { get; set; }
    public byte[]? HeaderRightLogo { get; set; }
    public byte[]? WatermarkImage { get; set; }
    public byte[]? TechnicianSignature { get; set; }
    public byte[]? ValidatorSignature { get; set; }
}

// Usage
var exportService = new EnhancedReportExportService(httpClient);

// Basic export
var basicPdf = await exportService.ExportBasicPDFAsync(
    reportId: 123,
    clientEmail: "user@example.com",
    dateFrom: new DateTime(2024, 1, 1)
);

// Advanced export with files
var options = new ReportExportWithFilesOptions
{
    ReportTitle = "Custom Report",
    HeaderText = "My Company - Audit Report",
    TechnicianName = "John Doe",
    TechnicianDept = "IT Security",
    HeaderLeftLogo = File.ReadAllBytes("left_logo.png"),
    TechnicianSignature = File.ReadAllBytes("signature.png"),
    DateFrom = new DateTime(2024, 1, 1),
    DateTo = new DateTime(2024, 12, 31)
};

var customPdf = await exportService.ExportPDFWithFilesAsync(null, options);
File.WriteAllBytes("custom_report.pdf", customPdf);
```

---

## 📊 **Supported File Formats**

### **Image File Types (Headers, Signatures, Watermarks):**
- ✅ **PNG** - Recommended for logos and signatures
- ✅ **JPG/JPEG** - Good for photos and watermarks
- ✅ **GIF** - Basic support
- ✅ **BMP** - Basic support

### **File Size Limits:**
- **Maximum file size**: 10MB per file (configurable)
- **Recommended sizes**:
  - Headers: 200x60px (300 DPI)
  - Signatures: 150x50px (300 DPI)
  - Watermarks: 400x400px (300 DPI)

---

## 🛡️ **Security & Permissions**

### **Required Permissions:**
- `EXPORT_REPORTS` - Basic export permission for own reports
- `EXPORT_ALL_REPORTS` - Admin permission to export any reports

### **File Upload Security:**
- ✅ **File type validation** - Only image files allowed
- ✅ **File size limits** - Prevents abuse
- ✅ **Memory stream handling** - Secure file processing
- ✅ **Automatic cleanup** - Files are not stored permanently

### **Access Control:**
- **Users**: Can only export their own reports with files
- **Managers**: Can export reports of users they manage with files
- **Admins**: Can export all reports in the system with files

---

## 🎉 **Benefits of Enhanced PDF Export**

### **✅ Professional Customization**
- Upload custom company logos for headers
- Add personal signatures for technicians and validators
- Include watermarks for document security
- Full control over report appearance

### **✅ Role-Based Security**
- Automatic data filtering based on user permissions
- Secure file upload handling
- Ownership validation for all operations

### **✅ Flexible Usage**
- Basic export for simple needs (GET requests)
- Advanced export with files for full customization (POST with multipart/form-data)
- Both single report and multiple reports support

### **✅ Easy Integration**
- RESTful API endpoints
- Standard multipart/form-data support
- Compatible with HTML forms, JavaScript, and C# clients

---

## 📋 **Complete Endpoint Summary**

| **Method** | **Endpoint** | **Content-Type** | **Purpose** |
|------------|-------------|------------------|-------------|
| GET | `/api/EnhancedAuditReports/export-pdf` | - | Basic multiple reports PDF |
| GET | `/api/EnhancedAuditReports/{id}/export-pdf` | - | Basic single report PDF |
| POST | `/api/EnhancedAuditReports/export-pdf-with-files` | multipart/form-data | Advanced multiple reports PDF with files |
| POST | `/api/EnhancedAuditReports/{id}/export-pdf-with-files` | multipart/form-data | Advanced single report PDF with files |
| GET | `/api/EnhancedAuditReports/export-csv` | - | CSV export |

---

Your Enhanced Audit Reports Controller now has **complete PDF export functionality** with signature upload, header upload, and watermark support - exactly like your original PDF service! 🚀📄✨