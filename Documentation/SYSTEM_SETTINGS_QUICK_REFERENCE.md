# 🚀 System Settings & Report Generation - Quick Reference

## 📍 System Settings Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/SystemSettings` | GET | Get all settings |
| `/api/SystemSettings/general` | GET/PUT | General settings |
| `/api/SystemSettings/security` | GET/PUT | Security settings |
| `/api/SystemSettings/notifications` | GET/PUT | Notification settings |
| `/api/SystemSettings/license` | GET | License info |
| `/api/SystemSettings/options` | GET | Dropdown options |

## 📊 Report Generation Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/ReportGeneration/generate` | POST | Generate report |
| `/api/ReportGeneration/download/{id}` | GET | Download report |
| `/api/ReportGeneration/history` | GET | Report history |
| `/api/ReportGeneration/statistics` | GET | Statistics |
| `/api/ReportGeneration/{id}` | DELETE | Delete report |

## 🔐 Required Permissions

- `SYSTEM_ADMIN` - Manage all settings
- `EXPORT_REPORTS` - Generate reports
- `READ_ALL_REPORTS` - View all reports

## 📝 Quick Example - Update General Settings

```bash
curl -X PUT http://localhost:4000/api/SystemSettings/general \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "siteName": "DSecureTech",
    "siteDescription": "Professional Data Erasure Solutions",
    "defaultLanguage": "English",
    "timezone": "UTC",
    "enableMaintenanceMode": false
  }'
```

## 📝 Quick Example - Generate Report

```bash
curl -X POST http://localhost:4000/api/ReportGeneration/generate \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "reportTitle": "Monthly Report",
    "reportType": "Comprehensive Report",
    "fromDate": "2024-09-01",
    "toDate": "2024-09-30",
    "allDevices": true,
    "exportFormat": "PDF",
    "includeChartsAndGraphs": true,
    "includeComplianceCertificates": true,
    "includeDetailedStatistics": true
  }'
```

## ✅ Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 500 | Server Error |

## 🗄️ Database Tables

- `SystemSettings` - System configuration
- `GeneratedReports` - Report metadata
- `ReportTemplates` - Report templates
- `ScheduledReports` - Scheduled reports

## 🔍 Test in Swagger

```
http://localhost:4000/swagger
```

## 📚 Full Documentation

- Complete Guide: `SYSTEM_SETTINGS_REPORT_GENERATION_COMPLETE_GUIDE.md`
- Implementation Summary: `SYSTEM_SETTINGS_IMPLEMENTATION_SUMMARY.md`

---

**Status:** ✅ Production Ready  
**Build:** ✅ Successful  
**Endpoints:** 15 Total  

**Ready to use!** 🎉
