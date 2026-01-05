# 🎛️ System Settings & Report Generation API - Complete Guide

## Overview
Complete System Settings and Report Generation APIs based on D-Secure UI screenshots.

---

## 📋 System Settings API

### Endpoints

#### Get All Settings
```http
GET /api/SystemSettings
Authorization: Bearer {token}
```

**Response:**
```json
{
  "general": {
    "siteName": "DSecureTech",
    "siteDescription": "Professional Data Erasure Solutions",
"defaultLanguage": "English",
    "timezone": "UTC",
    "enableMaintenanceMode": false
  },
  "security": {
    "passwordMinimumLength": 8,
    "sessionTimeoutMinutes": 30,
    "maxLoginAttempts": 5,
    "requireSpecialCharactersInPasswords": true,
    "enableTwoFactorAuthentication": false
  },
  "notifications": {
    "enableEmailNotifications": true,
"notifyOnNewUser": true,
    "notifyOnLicenseExpiry": true,
    "notifyOnSystemErrors": true
},
  "license": {
    "licenseType": "Enterprise",
    "totalLicenses": 1000,
    "usedLicenses": 750,
    "availableLicenses": 250,
    "licenseExpiryDate": "2025-12-31",
    "daysUntilExpiry": 367
  }
}
```

#### Update General Settings
```http
PUT /api/SystemSettings/general
Authorization: Bearer {token}
Content-Type: application/json

{
  "siteName": "DSecureTech Updated",
  "siteDescription": "Enterprise Data Erasure Solutions",
  "defaultLanguage": "English",
  "timezone": "America/New_York",
  "enableMaintenanceMode": false
}
```

#### Update Security Settings
```http
PUT /api/SystemSettings/security
Authorization: Bearer {token}
Content-Type: application/json

{
  "passwordMinimumLength": 10,
  "sessionTimeoutMinutes": 60,
  "maxLoginAttempts": 3,
  "requireSpecialCharactersInPasswords": true,
  "enableTwoFactorAuthentication": true
}
```

---

## 📊 Report Generation API

### Generate Report
```http
POST /api/ReportGeneration/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "reportTitle": "Monthly Erasure Report",
  "reportType": "Comprehensive Report",
  "fromDate": "2024-09-01",
  "toDate": "2024-09-30",
  "allDevices": true,
  "exportFormat": "PDF",
  "includeChartsAndGraphs": true,
  "includeComplianceCertificates": true,
  "includeDetailedStatistics": true,
  "erasurePersonName": "John Doe",
  "erasurePersonDepartment": "IT Security",
  "validatorPersonName": "Jane Smith",
  "validatorPersonDepartment": "Compliance",
  "headerText": "Data Erasure Report"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Report generated successfully",
  "reportId": "RPT-20241229-123456",
  "downloadUrl": "/api/ReportGeneration/download/RPT-20241229-123456",
  "fileName": "Monthly_Erasure_Report_20241229_103045.pdf",
  "fileSizeBytes": 2458624,
  "generatedAt": "2024-12-29T10:30:45Z",
  "format": "PDF"
}
```

### Download Report
```http
GET /api/ReportGeneration/download/{reportId}
Authorization: Bearer {token}
```

Returns PDF file for download.

### Get Report History
```http
GET /api/ReportGeneration/history?page=1&pageSize=10
Authorization: Bearer {token}
```

### Get Report Statistics
```http
GET /api/ReportGeneration/statistics
Authorization: Bearer {token}
```

---

## 🗄️ Database Tables

### SystemSettings
```sql
CREATE TABLE SystemSettings (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  SettingKey VARCHAR(100) NOT NULL,
  SettingValue TEXT NOT NULL,
  Category VARCHAR(50) NOT NULL,
  Description VARCHAR(500),
  CreatedAt DATETIME,
  UpdatedAt DATETIME,
  UpdatedBy VARCHAR(255),
  UNIQUE KEY (Category, SettingKey)
);
```

### GeneratedReports
```sql
CREATE TABLE GeneratedReports (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  ReportId VARCHAR(100) UNIQUE NOT NULL,
  ReportTitle VARCHAR(200) NOT NULL,
  ReportType VARCHAR(100) NOT NULL,
  FromDate DATETIME NOT NULL,
  ToDate DATETIME NOT NULL,
  Format VARCHAR(50) DEFAULT 'PDF',
  ConfigurationJson TEXT,
  FilePath VARCHAR(500),
  FileSizeBytes BIGINT,
  GeneratedBy VARCHAR(255) NOT NULL,
  GeneratedAt DATETIME,
  Status VARCHAR(50) DEFAULT 'completed',
  IsDeleted BOOLEAN DEFAULT FALSE
);
```

---

## 🔐 Required Permissions

- `SYSTEM_ADMIN` - Manage all settings
- `EXPORT_REPORTS` - Generate reports
- `READ_ALL_REPORTS` - View all reports

---

## ✅ Status: Complete and Production-Ready

**Files Created:**
- ✅ SystemSettingsModels.cs
- ✅ ReportGenerationModels.cs  
- ✅ SystemSettingsController.cs
- ✅ ReportGenerationController.cs
- ✅ Database entities configured
- ✅ Build successful

**Ready to use!** 🚀
