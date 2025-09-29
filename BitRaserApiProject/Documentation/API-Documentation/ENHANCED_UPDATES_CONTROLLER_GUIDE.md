# Enhanced Updates Controller Guide

## Overview

The **Enhanced Updates Controller** provides comprehensive software update management capabilities with full support for both users and subusers. It includes role-based access control, version management, checksum validation, and download tracking.

## Key Features

### ✅ **Role-Based Access Control**
- **SuperAdmin/Admin**: Full update management (create, edit, delete, publish)
- **Manager**: Can create and publish updates, view statistics
- **Support**: Read-only access to updates and statistics
- **User**: Can view active updates only
- **SubUser**: Very limited access to current updates

### ✅ **Comprehensive Update Management**
- Version lifecycle management (active, deprecated, recalled)
- Automatic checksum generation (MD5, SHA256)
- Platform-specific update filtering
- Mandatory vs optional update classification
- Security and installation notes

### ✅ **Client Integration Support**
- Anonymous endpoints for client update checks
- Version comparison logic
- Download tracking and statistics
- Platform-specific filtering

### ✅ **Enhanced Security**
- File integrity verification with checksums
- Role-based download permissions
- Audit logging for all operations
- Secure version validation

## API Endpoints

### 1. Get All Updates (Role-Filtered)
```http
GET /api/EnhancedUpdates
Authorization: Bearer <token>
```

**Query Parameters:**
- `updateType`: Filter by type (major, minor, patch, hotfix)
- `updateStatus`: Filter by status (active, deprecated, recalled)
- `isMandatory`: Filter mandatory updates (true/false)
- `releaseDateFrom`: Filter by release date from
- `releaseDateTo`: Filter by release date to
- `platform`: Filter by platform
- `page`: Page number (default: 0)
- `pageSize`: Items per page (default: 50)

**Response:**
```json
{
  "updates": [
    {
      "version_id": 1,
      "version_number": "2.1.0",
      "changelog": "Bug fixes and performance improvements",
      "download_link": "https://updates.example.com/v2.1.0.exe",
      "release_date": "2024-01-15T10:00:00Z",
      "is_mandatory_update": false,
      "update_type": "minor",
      "update_status": "active",
      "file_size_bytes": 15728640,
      "requires_restart": true,
      "can_edit": false
    }
  ],
  "pagination": {
    "page": 0,
    "pageSize": 50,
    "totalCount": 25
  },
  "userContext": {
    "email": "user@example.com",
    "userType": "User",
    "canCreateUpdates": false,
    "canManageAllUpdates": false
  }
}
```

### 2. Get Latest Update (Anonymous/Authenticated)
```http
GET /api/EnhancedUpdates/latest?platform=windows&currentVersion=2.0.5
```

**Query Parameters:**
- `platform`: Target platform (optional)
- `currentVersion`: Current version for comparison (optional)

**Response:**
```json
{
  "version_id": 1,
  "version_number": "2.1.0",
  "changelog": "Bug fixes and performance improvements",
  "download_link": "https://updates.example.com/v2.1.0.exe",
  "release_date": "2024-01-15T10:00:00Z",
  "is_mandatory_update": false,
  "update_type": "minor",
  "file_size_bytes": 15728640,
  "checksum_sha256": "abc123def456...",
  "requires_restart": true,
  "auto_download_enabled": true,
  "installation_notes": "Close all applications before installing",
  "security_notes": "Fixes critical security vulnerability CVE-2024-001",
  "is_newer_version": true,
  "current_version_provided": "2.0.5"
}
```

### 3. Check for Updates (Anonymous)
```http
GET /api/EnhancedUpdates/check/15?platform=windows
```

**Response:**
```json
{
  "current_version_id": 15,
  "platform_filter": "windows",
  "updates_available": true,
  "total_updates": 3,
  "mandatory_updates": 1,
  "security_updates": 2,
  "updates": [
    {
      "version_id": 16,
      "version_number": "2.0.6",
      "changelog": "Security patch",
      "is_mandatory_update": true,
      "update_type": "patch",
      "security_notes": "Critical security fix"
    }
  ],
  "checked_at": "2024-01-20T14:30:00Z"
}
```

### 4. Get Specific Update
```http
GET /api/EnhancedUpdates/1
Authorization: Bearer <token>
```

**Response:**
```json
{
  "version_id": 1,
  "version_number": "2.1.0",
  "changelog": "Bug fixes and performance improvements",
  "download_link": "https://updates.example.com/v2.1.0.exe",
  "release_date": "2024-01-15T10:00:00Z",
  "is_mandatory_update": false,
  "update_type": "minor",
  "update_status": "active",
  "file_size_bytes": 15728640,
  "checksum_md5": "d41d8cd98f00b204e9800998ecf8427e",
  "checksum_sha256": "abc123def456...",
  "minimum_os_version": "Windows 10",
  "supported_platforms": "windows,linux,macos",
  "requires_restart": true,
  "auto_download_enabled": true,
  "security_notes": "Fixes critical security vulnerability",
  "installation_notes": "Close all applications before installing",
  "created_by_email": "admin@example.com",
  "created_at": "2024-01-10T09:00:00Z",
  "is_deprecated": false,
  "can_edit": false,
  "can_delete": false
}
```

### 5. Create New Update (Admin/Manager Only)
```http
POST /api/EnhancedUpdates
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "versionNumber": "2.2.0",
  "changelog": "Major feature release with new UI and performance improvements",
  "downloadLink": "https://updates.example.com/v2.2.0.exe",
  "releaseDate": "2024-02-01T10:00:00Z",
  "isMandatoryUpdate": false,
  "updateType": "major",
  "updateStatus": "active",
  "fileSizeBytes": 25165824,
  "minimumOSVersion": "Windows 10",
  "supportedPlatforms": "windows,linux,macos",
  "securityNotes": "No security issues addressed in this release",
  "installationNotes": "Restart required after installation. Allow 10 minutes for installation.",
  "requiresRestart": true,
  "autoDownloadEnabled": true,
  "rollbackVersion": "2.1.0"
}
```

**Response:**
```json
{
  "version_id": 25,
  "version_number": "2.2.0",
  "created_by": "admin@example.com",
  "created_at": "2024-01-20T15:00:00Z",
  "message": "Update created successfully",
  "checksums_generated": {
    "md5": false,
    "sha256": false
  }
}
```

### 6. Update Existing Update
```http
PUT /api/EnhancedUpdates/25
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "changelog": "Updated changelog with additional bug fixes",
  "isMandatoryUpdate": true,
  "securityNotes": "Critical security patch included",
  "requiresRestart": true
}
```

### 7. Delete Update (Admin Only)
```http
DELETE /api/EnhancedUpdates/25
Authorization: Bearer <admin-token>
```

**Response:**
```json
{
  "message": "Update deleted successfully",
  "version_number": "2.2.0",
  "deleted_by": "admin@example.com",
  "deleted_at": "2024-01-20T16:00:00Z"
}
```

### 8. Get Update Statistics
```http
GET /api/EnhancedUpdates/statistics
Authorization: Bearer <admin-token>
```

**Response:**
```json
{
  "overview": {
    "totalUpdates": 25,
    "activeUpdates": 20,
    "mandatoryUpdates": 5,
    "deprecatedUpdates": 3,
    "securityUpdates": 8
  },
  "distribution": {
    "byType": [
      { "updateType": "major", "count": 5 },
      { "updateType": "minor", "count": 12 },
      { "updateType": "patch", "count": 8 }
    ],
    "byStatus": [
      { "status": "active", "count": 20 },
      { "status": "deprecated", "count": 3 },
      { "status": "recalled", "count": 2 }
    ]
  },
  "recentActivity": {
    "updatesLast30Days": 3,
    "updatesLast7Days": 1,
    "recentUpdates": [
      {
        "version_number": "2.1.5",
        "update_type": "patch",
        "release_date": "2024-01-18T10:00:00Z",
        "is_mandatory_update": true
      }
    ]
  },
  "creators": [
    { "createdBy": "admin@example.com", "count": 15 },
    { "createdBy": "manager@example.com", "count": 10 }
  ],
  "averageFileSize": 18874368.5,
  "generatedAt": "2024-01-20T16:30:00Z",
  "generatedBy": "admin@example.com"
}
```

### 9. Download Update (Anonymous)
```http
GET /api/EnhancedUpdates/25/download?userAgent=BitRaserClient/2.1.0
```

**Response:**
```json
{
  "version_number": "2.2.0",
  "download_link": "https://updates.example.com/v2.2.0.exe",
  "file_size_bytes": 25165824,
  "checksum_sha256": "abc123def456...",
  "installation_notes": "Restart required after installation",
  "requires_restart": true,
  "download_initiated_at": "2024-01-20T17:00:00Z"
}
```

## Permission Requirements

### Create Updates
- **Required Permission**: `CREATE_UPDATES`
- **Roles**: SuperAdmin, Admin, Manager

### View All Updates  
- **Required Permission**: `READ_ALL_UPDATES`
- **Roles**: SuperAdmin, Admin, Manager, Support

### Manage All Updates
- **Required Permission**: `MANAGE_ALL_UPDATES`
- **Roles**: SuperAdmin, Admin

### Delete Updates
- **Required Permission**: `DELETE_UPDATES`
- **Roles**: SuperAdmin, Admin

### View Statistics
- **Required Permission**: `VIEW_UPDATE_STATISTICS`
- **Roles**: SuperAdmin, Admin, Manager, Support

## Update Lifecycle Management

### Update Types
- **major**: Major version releases (e.g., 1.0.0 → 2.0.0)
- **minor**: Minor feature releases (e.g., 1.0.0 → 1.1.0)
- **patch**: Bug fixes and patches (e.g., 1.0.0 → 1.0.1)
- **hotfix**: Critical emergency fixes

### Update Status
- **active**: Available for download and installation
- **deprecated**: Still available but no longer recommended
- **recalled**: Removed from distribution due to critical issues

### Version Validation
- Uses semantic versioning (e.g., 2.1.5)
- Automatic version comparison logic
- Prevents duplicate version numbers

## Security Features

### Checksum Validation
- **MD5**: Basic integrity verification
- **SHA256**: Strong cryptographic verification
- Automatic generation when file content provided

### Access Control
- Role-based download permissions
- Audit logging for all operations
- Secure file validation

### Platform Security
- Platform-specific update filtering
- Minimum OS version requirements
- Rollback version specification

## Client Integration Examples

### Check for Updates in Client Application
```csharp
// Anonymous check - no authentication required
var response = await httpClient.GetAsync(
    $"https://api.example.com/api/EnhancedUpdates/check/{currentVersionId}?platform=windows");

if (response.IsSuccessStatusCode)
{
    var updateInfo = await response.Content.ReadFromJsonAsync<UpdateCheckResponse>();
    
    if (updateInfo.UpdatesAvailable)
    {
        // Handle available updates
        foreach (var update in updateInfo.Updates)
        {
            if (update.IsMandatoryUpdate)
            {
                // Force update
            }
            else
            {
                // Optional update - prompt user
            }
        }
    }
}
```

### Download and Verify Update
```csharp
// Get download information
var downloadInfo = await httpClient.GetAsync(
    $"https://api.example.com/api/EnhancedUpdates/{versionId}/download");

// Download file from provided link
var fileBytes = await httpClient.GetByteArrayAsync(downloadInfo.DownloadLink);

// Verify checksum
var calculatedHash = ComputeSHA256Hash(fileBytes);
if (calculatedHash.Equals(downloadInfo.ChecksumSHA256, StringComparison.OrdinalIgnoreCase))
{
    // File integrity verified - proceed with installation
}
```

## Best Practices

### For Administrators
1. **Always provide detailed changelogs** for better user experience
2. **Use semantic versioning** consistently
3. **Set appropriate mandatory flags** only for critical updates
4. **Include security notes** for security-related updates
5. **Test updates** before marking them as active
6. **Plan deprecation schedules** for older versions

### For Developers
1. **Implement checksum verification** in client applications
2. **Handle mandatory updates** appropriately in UI
3. **Cache update information** to reduce API calls
4. **Implement proper error handling** for network issues
5. **Log update activities** for troubleshooting

### For Users
1. **Review changelogs** before installing updates
2. **Backup critical data** before major updates
3. **Install security updates** promptly
4. **Follow installation notes** for smooth updates

## Troubleshooting

### Common Issues

#### Permission Denied (403)
- **Cause**: Insufficient permissions for the operation
- **Solution**: Check user roles and permissions

#### Update Not Found (404)
- **Cause**: Invalid version ID or update has been deleted
- **Solution**: Verify version ID and check if update exists

#### Version Already Exists (409)
- **Cause**: Attempting to create update with existing version number
- **Solution**: Use a different version number

#### Invalid Version Format (400)
- **Cause**: Version number doesn't follow semantic versioning
- **Solution**: Use format like "2.1.0" (major.minor.patch)

### Monitoring and Logging

The Enhanced Updates Controller provides comprehensive logging:
- **Update creation/modification** events
- **Download requests** with user agent tracking
- **Permission violations** and security events
- **Error conditions** and troubleshooting information

Monitor these logs to:
- Track update adoption rates
- Identify problematic updates
- Monitor security and access patterns
- Troubleshoot client integration issues

## Integration with Other Controllers

The Enhanced Updates Controller integrates seamlessly with:
- **Enhanced Profile Controller**: Update creator profiles and permissions
- **Enhanced Logs Controller**: Update operation logging
- **Dynamic System Controller**: Update system health monitoring
- **Role-Based Auth Service**: Permission validation and user authentication

This provides a complete, enterprise-ready update management solution for your BitRaser application.