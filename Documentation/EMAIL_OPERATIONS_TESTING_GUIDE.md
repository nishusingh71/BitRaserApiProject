# 🧪 Email-Based Operations - Testing Guide

## 🎯 **Testing in Swagger UI**

Complete guide for testing all email-based endpoints in Swagger.

---

## 🚀 **Setup: Get Authentication Token**

### **Step 1: Login to Get JWT Token**

```http
POST /api/RoleBasedAuth/login
```

**Request Body:**
```json
{
  "email": "admin@dsecuretech.com",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userType": "user",
  "email": "admin@dsecuretech.com",
  "roles": ["SuperAdmin"],
  "permissions": ["FullAccess"],
  "expiresAt": "2024-02-01T10:00:00Z"
}
```

### **Step 2: Authorize in Swagger**

1. Click the **"Authorize"** button (🔒 icon) at the top right
2. Enter: `Bearer <your-token>`
3. Click **"Authorize"**
4. Click **"Close"**

---

## 📧 **Test 1: User Management by Email**

### **1.1 Get User by Email**

```http
GET /api/EnhancedUsers/{email}
```

**Parameters:**
- `email`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
{
  "userEmail": "admin@dsecuretech.com",
  "userName": "Admin User",
  "phoneNumber": "+1234567890",
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": [
    {
      "roleName": "SuperAdmin",
  "description": "Full system access",
      "hierarchyLevel": 1
    }
  ],
  "permissions": ["FullAccess"],
  "hasLicenses": true
}
```

### **1.2 Update User by Email**

```http
PUT /api/EnhancedUsers/{email}
```

**Parameters:**
- `email`: `admin@dsecuretech.com`

**Request Body:**
```json
{
  "userEmail": "admin@dsecuretech.com",
  "userName": "Updated Admin Name",
  "phoneNumber": "+9876543210"
}
```

**Expected Response: 200 OK**
```json
{
  "message": "User updated successfully",
  "userEmail": "admin@dsecuretech.com",
  "updatedAt": "2024-01-20T15:30:00Z"
}
```

### **1.3 Change Password by Email**

```http
PATCH /api/EnhancedUsers/{email}/change-password
```

**Parameters:**
- `email`: `admin@dsecuretech.com`

**Request Body:**
```json
{
  "currentPassword": "Admin@123",
  "newPassword": "NewSecure@456"
}
```

**Expected Response: 200 OK**
```json
{
  "message": "Password changed successfully",
  "userEmail": "admin@dsecuretech.com",
  "updatedAt": "2024-01-20T15:35:00Z",
  "passwordUpdated": true,
  "hashUpdated": true
}
```

### **1.4 Update License by Email**

```http
PATCH /api/EnhancedUsers/{email}/update-license
```

**Parameters:**
- `email`: `admin@dsecuretech.com`

**Request Body:**
```json
{
  "licenseDetailsJson": "{\"licenseKey\":\"ABC-123-DEF-456\",\"plan\":\"premium\",\"expiryDate\":\"2025-12-31\"}"
}
```

**Expected Response: 200 OK**
```json
{
  "message": "License details updated successfully",
  "userEmail": "admin@dsecuretech.com",
  "updatedAt": "2024-01-20T15:40:00Z"
}
```

### **1.5 Get User Statistics by Email**

```http
GET /api/EnhancedUsers/{email}/statistics
```

**Parameters:**
- `email`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
{
  "userEmail": "admin@dsecuretech.com",
  "userName": "Admin User",
  "accountAge": "365.00:00:00",
  "totalMachines": 5,
  "activeLicenses": 3,
  "totalReports": 12,
  "totalSessions": 45,
  "activeSessions": 2,
  "totalSubusers": 3,
  "lastActivity": "2024-01-20T15:00:00Z"
}
```

### **1.6 Assign Role by Email**

```http
POST /api/EnhancedUsers/{email}/assign-role
```

**Parameters:**
- `email`: `testuser@example.com`

**Request Body:**
```json
{
  "roleName": "Manager"
}
```

**Expected Response: 200 OK**
```json
{
  "message": "Role Manager assigned to user testuser@example.com",
  "userEmail": "testuser@example.com",
  "roleName": "Manager",
  "assignedBy": "admin@dsecuretech.com",
  "assignedAt": "2024-01-20T15:45:00Z"
}
```

---

## 🖥️ **Test 2: Machine Management by Email**

### **2.1 Get Machines by User Email**

```http
GET /api/EnhancedMachines/by-email/{userEmail}
```

**Parameters:**
- `userEmail`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
[
  {
    "fingerprintHash": "abc123def456",
    "userEmail": "admin@dsecuretech.com",
    "subuserEmail": null,
    "macAddress": "AA:BB:CC:DD:EE:FF",
    "osVersion": "Windows 11 Pro",
    "licenseActivated": true,
    "licenseActivationDate": "2024-01-01T00:00:00Z",
    "licenseDaysValid": 365,
    "vmStatus": "native",
    "createdAt": "2024-01-01T10:00:00Z"
  }
]
```

### **2.2 Register Machine for User Email**

```http
POST /api/EnhancedMachines/register/{userEmail}
```

**Parameters:**
- `userEmail`: `admin@dsecuretech.com`

**Request Body:**
```json
{
  "macAddress": "11:22:33:44:55:66",
  "fingerprintHash": "newhash123456",
  "physicalDriveId": "DISK001",
  "cpuId": "CPU-12345",
  "biosSerial": "BIOS-67890",
  "osVersion": "Windows 11 Pro",
  "licenseActivated": false,
  "vmStatus": "native"
}
```

**Expected Response: 201 Created**
```json
{
  "fingerprintHash": "newhash123456",
  "userEmail": "admin@dsecuretech.com",
  "subuserEmail": null,
  "macAddress": "11:22:33:44:55:66",
  "licenseActivated": false,
  "createdAt": "2024-01-20T16:00:00Z",
  "message": "Machine registered successfully"
}
```

### **2.3 Get Machine by MAC Address**

```http
GET /api/EnhancedMachines/by-mac/{macAddress}
```

**Parameters:**
- `macAddress`: `AA:BB:CC:DD:EE:FF`

**Expected Response: 200 OK**
```json
{
  "fingerprintHash": "abc123def456",
  "userEmail": "admin@dsecuretech.com",
  "macAddress": "AA:BB:CC:DD:EE:FF",
  "osVersion": "Windows 11 Pro",
  "licenseActivated": true,
  "licenseDetailsJson": "{\"plan\":\"premium\"}"
}
```

### **2.4 Activate License by MAC Address**

```http
PATCH /api/EnhancedMachines/by-mac/{macAddress}/activate-license
```

**Parameters:**
- `macAddress`: `11:22:33:44:55:66`

**Request Body:**
```json
{
  "daysValid": 365,
  "licenseDetailsJson": "{\"plan\":\"premium\",\"licenseKey\":\"PREM-789\"}"
}
```

**Expected Response: 200 OK**
```json
{
  "message": "License activated successfully",
  "macAddress": "11:22:33:44:55:66",
  "userEmail": "admin@dsecuretech.com",
  "licenseActivated": true,
  "activationDate": "2024-01-20T16:05:00Z",
  "daysValid": 365
}
```

### **2.5 Get Machine Statistics by Email**

```http
GET /api/EnhancedMachines/statistics/{userEmail}
```

**Parameters:**
- `userEmail`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
{
  "userEmail": "admin@dsecuretech.com",
  "totalMachines": 6,
  "activeLicenses": 4,
  "inactiveLicenses": 2,
  "expiredLicenses": 0,
  "expiringInNext30Days": 1,
  "machinesRegisteredToday": 1,
  "machinesRegisteredThisWeek": 2,
  "osVersionDistribution": [
    { "osVersion": "Windows 11 Pro", "count": 4 },
    { "osVersion": "Windows 10", "count": 2 }
]
}
```

---

## 🔄 **Test 3: Session Management by Email**

### **3.1 Get Sessions by Email**

```http
GET /api/EnhancedSessions/by-email/{email}
```

**Parameters:**
- `email`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
[
  {
    "session_id": 1,
    "user_email": "admin@dsecuretech.com",
    "login_time": "2024-01-20T10:00:00Z",
  "logout_time": null,
    "ip_address": "192.168.1.100",
    "device_info": "Chrome 120.0 on Windows 11",
    "session_status": "active",
    "expiresAt": "2024-01-21T10:00:00Z",
    "isExpired": false,
    "timeRemaining": "23h 45m"
  }
]
```

### **3.2 Create Session (Login)**

```http
POST /api/EnhancedSessions
```

**Request Body:**
```json
{
  "userEmail": "admin@dsecuretech.com",
  "ipAddress": "192.168.1.101",
  "deviceInfo": "Firefox 121.0 on Windows 11"
}
```

**Expected Response: 201 Created**
```json
{
  "session_id": 2,
  "user_email": "admin@dsecuretech.com",
  "login_time": "2024-01-20T16:10:00Z",
  "ip_address": "192.168.1.101",
  "device_info": "Firefox 121.0 on Windows 11",
  "session_status": "active",
  "expiresAt": "2024-01-21T16:10:00Z",
  "timeRemaining": "23h 59m"
}
```

### **3.3 End All Sessions for User**

```http
PATCH /api/EnhancedSessions/end-all/{email}
```

**Parameters:**
- `email`: `testuser@example.com`

**Expected Response: 200 OK**
```json
{
  "message": "Ended 3 active sessions for user testuser@example.com",
  "sessionIds": [5, 6, 7]
}
```

### **3.4 Get Session Statistics**

```http
GET /api/EnhancedSessions/statistics?userEmail={email}
```

**Query Parameters:**
- `userEmail`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
{
  "totalSessions": 45,
  "activeSessions": 2,
  "closedSessions": 40,
  "expiredSessions": 3,
  "sessionsToday": 5,
  "sessionsThisWeek": 12,
  "averageSessionDuration": "4.5 hours",
  "topDevices": [
    { "device": "Chrome 120.0 on Windows 11", "count": 20 },
    { "device": "Firefox 121.0 on Windows 11", "count": 15 }
  ]
}
```

---

## 📄 **Test 4: Audit Reports by Email**

### **4.1 Get Reports by Client Email**

```http
GET /api/EnhancedAuditReports/by-email/{email}
```

**Parameters:**
- `email`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
[
  {
    "report_id": 1,
    "client_email": "admin@dsecuretech.com",
    "report_name": "Erasure Report 2024-01",
    "erasure_method": "DoD 5220.22-M",
    "report_datetime": "2024-01-15T14:30:00Z",
    "synced": true
  }
]
```

### **4.2 Create Audit Report**

```http
POST /api/EnhancedAuditReports
```

**Request Body:**
```json
{
  "clientEmail": "admin@dsecuretech.com",
  "reportName": "Erasure Report 2024-02",
  "erasureMethod": "DoD 5220.22-M (7 passes)",
  "reportDetailsJson": "{\"totalDrives\":5,\"successfulErasures\":5}"
}
```

**Expected Response: 201 Created**
```json
{
  "report_id": 2,
  "client_email": "admin@dsecuretech.com",
  "report_name": "Erasure Report 2024-02",
  "erasure_method": "DoD 5220.22-M (7 passes)",
  "report_datetime": "2024-01-20T16:20:00Z",
  "synced": false
}
```

### **4.3 Get Report Statistics by Email**

```http
GET /api/EnhancedAuditReports/statistics?clientEmail={email}
```

**Query Parameters:**
- `clientEmail`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
{
  "totalReports": 12,
  "syncedReports": 10,
  "pendingReports": 2,
  "reportsThisMonth": 5,
  "reportsThisWeek": 2,
  "reportsToday": 1,
  "erasureMethods": [
    { "method": "DoD 5220.22-M", "count": 8 },
    { "method": "NIST 800-88", "count": 4 }
  ]
}
```

### **4.4 Export Reports to CSV by Email**

```http
GET /api/EnhancedAuditReports/export-csv?ClientEmail={email}
```

**Query Parameters:**
- `ClientEmail`: `admin@dsecuretech.com`
- `DateFrom`: `2024-01-01`
- `DateTo`: `2024-01-31`

**Expected Response: 200 OK**
- **Content-Type:** `text/csv`
- **File Download:** `audit_reports_20240120_162500.csv`

### **4.5 Export Reports to PDF by Email**

```http
GET /api/EnhancedAuditReports/export-pdf?ClientEmail={email}
```

**Query Parameters:**
- `ClientEmail`: `admin@dsecuretech.com`
- `DateFrom`: `2024-01-01`
- `DateTo`: `2024-01-31`

**Expected Response: 200 OK**
- **Content-Type:** `application/pdf`
- **File Download:** `audit_reports_20240120_162530.pdf`

---

## 📝 **Test 5: Logs by Email**

### **5.1 Get Logs by Email**

```http
GET /api/EnhancedLogs/by-email/{email}
```

**Parameters:**
- `email`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
[
  {
    "log_id": 1,
  "user_email": "admin@dsecuretech.com",
 "log_message": "User logged in successfully",
    "log_level": "Info",
    "log_timestamp": "2024-01-20T10:00:00Z",
    "log_details_json": "{\"ipAddress\":\"192.168.1.100\"}"
  }
]
```

### **5.2 Create Log for User**

```http
POST /api/EnhancedLogs
```

**Request Body:**
```json
{
  "userEmail": "admin@dsecuretech.com",
  "logMessage": "User performed password change",
  "logLevel": "Info",
  "logDetailsJson": "{\"action\":\"password_change\",\"success\":true}"
}
```

**Expected Response: 201 Created**

### **5.3 Search Logs by Email**

```http
POST /api/EnhancedLogs/search
```

**Request Body:**
```json
{
  "userEmail": "admin@dsecuretech.com",
  "logLevel": "Error",
  "dateFrom": "2024-01-01T00:00:00Z",
  "dateTo": "2024-01-31T23:59:59Z"
}
```

**Expected Response: 200 OK**

### **5.4 Get Log Statistics by Email**

```http
GET /api/EnhancedLogs/statistics?userEmail={email}
```

**Query Parameters:**
- `userEmail`: `admin@dsecuretech.com`

**Expected Response: 200 OK**
```json
{
  "totalLogs": 150,
  "infoLogs": 120,
  "warningLogs": 20,
  "errorLogs": 10,
  "logsToday": 15,
  "logsThisWeek": 45,
  "logLevelDistribution": [
    { "logLevel": "Info", "count": 120 },
    { "logLevel": "Warning", "count": 20 },
    { "logLevel": "Error", "count": 10 }
  ]
}
```

---

## ✅ **Expected Results Summary**

| Test | Endpoint | Expected Status | Email Used |
|------|----------|----------------|------------|
| Get User | `GET /api/EnhancedUsers/{email}` | 200 OK | ✅ |
| Update User | `PUT /api/EnhancedUsers/{email}` | 200 OK | ✅ |
| Change Password | `PATCH /api/EnhancedUsers/{email}/change-password` | 200 OK | ✅ |
| Get Machines | `GET /api/EnhancedMachines/by-email/{email}` | 200 OK | ✅ |
| Register Machine | `POST /api/EnhancedMachines/register/{email}` | 201 Created | ✅ |
| Get Sessions | `GET /api/EnhancedSessions/by-email/{email}` | 200 OK | ✅ |
| End Sessions | `PATCH /api/EnhancedSessions/end-all/{email}` | 200 OK | ✅ |
| Get Reports | `GET /api/EnhancedAuditReports/by-email/{email}` | 200 OK | ✅ |
| Create Report | `POST /api/EnhancedAuditReports` | 201 Created | ✅ |
| Get Logs | `GET /api/EnhancedLogs/by-email/{email}` | 200 OK | ✅ |

---

## 🚨 **Common Errors and Solutions**

### **Error 1: 401 Unauthorized**
```json
{
  "error": "Unauthorized"
}
```

**Solution:**
- Click the "Authorize" button in Swagger
- Enter your JWT token: `Bearer <token>`
- Make sure token is not expired

### **Error 2: 403 Forbidden**
```json
{
  "error": "You can only view your own data"
}
```

**Solution:**
- You're trying to access another user's data
- Login with admin account or access your own data

### **Error 3: 404 Not Found**
```json
{
  "error": "User with email test@example.com not found"
}
```

**Solution:**
- Verify the email address is correct
- Check if the user/resource exists
- Use correct email format

### **Error 4: 400 Bad Request**
```json
{
  "message": "Invalid email format"
}
```

**Solution:**
- Ensure email is properly formatted
- Check all required fields are provided
- Validate JSON syntax in request body

---

## 🎉 **All Tests Passed?**

Congratulations! Your email-based API operations are working perfectly! 🚀

---

**Happy Testing! 🧪**
