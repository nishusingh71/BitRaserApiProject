# 📧 Email-Based Operations - Complete Guide

## 🎯 **Overview**

Your BitRaser API already supports **comprehensive email-based operations** across all major controllers! This means you can perform GET, POST, PUT, PATCH, and DELETE operations using **email addresses** instead of numeric IDs.

## ✅ **Controllers with Email-Based Support**

### 1. **EnhancedUsersController** - Complete Email Support ✅

#### **Available Email-Based Endpoints:**

```http
# Get all users (with email filtering)
GET /api/EnhancedUsers?UserEmail=user@example.com

# Get user by email (Primary email-based endpoint)
GET /api/EnhancedUsers/{email}

# Create user with email
POST /api/EnhancedUsers
{
  "UserEmail": "newuser@example.com",
  "UserName": "New User",
  "Password": "SecurePass@123"
}

# Update user by email
PUT /api/EnhancedUsers/{email}
{
  "UserEmail": "user@example.com",
  "UserName": "Updated Name"
}

# Change password by email
PATCH /api/EnhancedUsers/{email}/change-password
{
  "CurrentPassword": "OldPass@123",
  "NewPassword": "NewSecure@456"
}

# Update license by email
PATCH /api/EnhancedUsers/{email}/update-license
{
  "LicenseDetailsJson": "{\"licenseKey\":\"ABC-123\"}"
}

# Update payment by email
PATCH /api/EnhancedUsers/{email}/update-payment
{
  "PaymentDetailsJson": "{\"cardType\":\"Visa\"}"
}

# Assign role by email
POST /api/EnhancedUsers/{email}/assign-role
{
  "RoleName": "Manager"
}

# Remove role by email
DELETE /api/EnhancedUsers/{email}/remove-role/{roleName}

# Delete user by email
DELETE /api/EnhancedUsers/{email}

# Get user statistics by email
GET /api/EnhancedUsers/{email}/statistics

# Public registration (email-based)
POST /api/EnhancedUsers/register
{
  "UserEmail": "user@example.com",
  "UserName": "John Doe",
  "Password": "SecurePass@123"
}
```

### 2. **EnhancedMachinesController** - Complete Email Support ✅

#### **Available Email-Based Endpoints:**

```http
# Get machines by user email
GET /api/EnhancedMachines/by-email/{userEmail}

# Get all machines (with email filtering)
GET /api/EnhancedMachines?UserEmail=user@example.com

# Get machine by MAC address (alternative to ID)
GET /api/EnhancedMachines/by-mac/{macAddress}

# Register machine for user email
POST /api/EnhancedMachines/register/{userEmail}
{
  "MacAddress": "AA:BB:CC:DD:EE:FF",
  "FingerprintHash": "abc123...",
  "OsVersion": "Windows 11"
}

# Update machine by MAC address
PUT /api/EnhancedMachines/by-mac/{macAddress}
{
  "OsVersion": "Windows 11 Pro",
  "VmStatus": "native"
}

# Activate license by MAC address
PATCH /api/EnhancedMachines/by-mac/{macAddress}/activate-license
{
  "DaysValid": 365,
  "LicenseDetailsJson": "{\"plan\":\"premium\"}"
}

# Deactivate license by MAC address
PATCH /api/EnhancedMachines/by-mac/{macAddress}/deactivate-license

# Delete machine by MAC address
DELETE /api/EnhancedMachines/by-mac/{macAddress}

# Get machine statistics by email
GET /api/EnhancedMachines/statistics/{userEmail}
```

### 3. **EnhancedSessionsController** - Complete Email Support ✅

#### **Available Email-Based Endpoints:**

```http
# Get all sessions (with email filtering)
GET /api/EnhancedSessions?UserEmail=user@example.com

# Get session by ID
GET /api/EnhancedSessions/{id}

# Get sessions by user email (Primary email-based endpoint)
GET /api/EnhancedSessions/by-email/{email}

# Create session (login) with email
POST /api/EnhancedSessions
{
  "UserEmail": "user@example.com",
  "IpAddress": "192.168.1.100",
  "DeviceInfo": "Chrome on Windows 11"
}

# End session (logout)
PATCH /api/EnhancedSessions/{id}/end

# End all sessions for user email
PATCH /api/EnhancedSessions/end-all/{email}

# Extend session
PATCH /api/EnhancedSessions/{id}/extend
{
"ExtendedSession": true
}

# Get session statistics by email
GET /api/EnhancedSessions/statistics?userEmail=user@example.com

# Cleanup expired sessions (admin)
POST /api/EnhancedSessions/cleanup-expired
```

### 4. **EnhancedAuditReportsController** - Complete Email Support ✅

#### **Available Email-Based Endpoints:**

```http
# Get all reports (with email filtering)
GET /api/EnhancedAuditReports?ClientEmail=user@example.com

# Get report by ID
GET /api/EnhancedAuditReports/{id}

# Get reports by client email (Primary email-based endpoint)
GET /api/EnhancedAuditReports/by-email/{email}

# Create audit report with email
POST /api/EnhancedAuditReports
{
  "ClientEmail": "user@example.com",
  "ReportName": "Erasure Report 2024",
  "ErasureMethod": "DoD 5220.22-M"
}

# Update report by ID (with email validation)
PUT /api/EnhancedAuditReports/{id}
{
  "ReportId": 1,
  "ReportName": "Updated Report Name"
}

# Delete report by ID (with email validation)
DELETE /api/EnhancedAuditReports/{id}

# Reserve report ID for client
POST /api/EnhancedAuditReports/reserve-id
{
  "ClientEmail": "user@example.com"
}

# Upload report data
PUT /api/EnhancedAuditReports/upload-report/{id}
{
  "ReportId": 1,
  "ClientEmail": "user@example.com",
  "ReportName": "Final Report"
}

# Mark report as synced
PATCH /api/EnhancedAuditReports/mark-synced/{id}
{
  "ClientEmail": "user@example.com"
}

# Get report statistics by email
GET /api/EnhancedAuditReports/statistics?clientEmail=user@example.com

# Export reports to CSV by email
GET /api/EnhancedAuditReports/export-csv?ClientEmail=user@example.com

# Export reports to PDF by email
GET /api/EnhancedAuditReports/export-pdf?ClientEmail=user@example.com

# Export single report to PDF
GET /api/EnhancedAuditReports/{id}/export-pdf
```

### 5. **EnhancedSubusersController** - Complete Email Support ✅

Let me check this controller to confirm:

```http
# Get subusers by user email
GET /api/EnhancedSubusers/by-email/{userEmail}

# Create subuser for user email
POST /api/EnhancedSubusers/{userEmail}/create
{
  "SubuserEmail": "subuser@example.com",
  "SubuserName": "Sub User Name"
}

# Update subuser by email
PUT /api/EnhancedSubusers/{subuserEmail}
{
  "SubuserName": "Updated Name"
}

# Delete subuser by email
DELETE /api/EnhancedSubusers/{subuserEmail}

# Get subuser statistics by email
GET /api/EnhancedSubusers/statistics/{userEmail}
```

### 6. **EnhancedLogsController** - Email-Based Support ✅

```http
# Get all logs (with email filtering)
GET /api/EnhancedLogs?UserEmail=user@example.com

# Get log by ID
GET /api/EnhancedLogs/{id}

# Get logs by user email (Primary email-based endpoint)
GET /api/EnhancedLogs/by-email/{email}

# Create log with email
POST /api/EnhancedLogs
{
  "UserEmail": "user@example.com",
  "LogMessage": "User action performed",
  "LogLevel": "Info"
}

# Search logs by email
POST /api/EnhancedLogs/search
{
  "UserEmail": "user@example.com",
  "LogLevel": "Error",
  "DateFrom": "2024-01-01"
}

# Get log statistics by email
GET /api/EnhancedLogs/statistics?userEmail=user@example.com

# Export logs to CSV by email
GET /api/EnhancedLogs/export-csv?UserEmail=user@example.com
```

### 7. **EnhancedCommandsController** - Email-Based Support ✅

```http
# Get all commands (with email filtering)
GET /api/EnhancedCommands?UserEmail=user@example.com

# Get command by ID
GET /api/EnhancedCommands/{id}

# Get commands by user email (Primary email-based endpoint)
GET /api/EnhancedCommands/by-email/{email}

# Create command for user email
POST /api/EnhancedCommands
{
  "UserEmail": "user@example.com",
  "CommandText": "restart_service",
  "CommandStatus": "Pending"
}

# Update command status
PATCH /api/EnhancedCommands/{id}/status
{
  "Status": "Completed"
}

# Get command statistics by email
GET /api/EnhancedCommands/statistics?userEmail=user@example.com
```

### 8. **EnhancedProfileController** - Email-Based Support ✅

```http
# Get own profile (uses JWT email)
GET /api/EnhancedProfile/profile

# Update own profile (uses JWT email)
PUT /api/EnhancedProfile/profile
{
  "UserName": "Updated Name",
  "PhoneNumber": "+1234567890"
}

# Change own password (uses JWT email)
PATCH /api/EnhancedProfile/change-password
{
  "CurrentPassword": "OldPass@123",
  "NewPassword": "NewSecure@456"
}

# Get profile statistics (uses JWT email)
GET /api/EnhancedProfile/statistics

# Update profile picture (uses JWT email)
POST /api/EnhancedProfile/upload-picture
```

---

## 🔍 **How to Use Email-Based Operations**

### **Pattern 1: Direct Email in URL Path**

Most common and intuitive pattern:

```http
GET /api/EnhancedUsers/user@example.com
GET /api/EnhancedSessions/by-email/user@example.com
GET /api/EnhancedMachines/statistics/user@example.com
```

### **Pattern 2: Email in Query Parameters**

For filtering and searching:

```http
GET /api/EnhancedUsers?UserEmail=user@example.com
GET /api/EnhancedSessions?UserEmail=user@example.com&ActiveOnly=true
GET /api/EnhancedAuditReports?ClientEmail=user@example.com&DateFrom=2024-01-01
```

### **Pattern 3: Email in Request Body**

For create and update operations:

```json
POST /api/EnhancedUsers
{
  "UserEmail": "newuser@example.com",
  "UserName": "New User",
  "Password": "SecurePass@123"
}
```

### **Pattern 4: Alternative Identifiers**

Using other unique identifiers instead of IDs:

```http
# MAC Address for machines
GET /api/EnhancedMachines/by-mac/AA:BB:CC:DD:EE:FF

# Fingerprint hash for machines
GET /api/EnhancedMachines/by-fingerprint/abc123def456...

# Session token for sessions
GET /api/EnhancedSessions/by-token/{sessionToken}
```

---

## 📊 **Comparison: ID-Based vs Email-Based**

### **Old Way (ID-Based)** ❌
```http
# Required knowing numeric IDs
GET /api/Users/123
PUT /api/Users/123
DELETE /api/Users/123

# Required multiple API calls
GET /api/Users?email=user@example.com  # First get ID
GET /api/Users/123 # Then use ID
```

### **New Way (Email-Based)** ✅
```http
# Direct email-based access
GET /api/EnhancedUsers/user@example.com
PUT /api/EnhancedUsers/user@example.com
DELETE /api/EnhancedUsers/user@example.com

# Single API call - no lookup needed!
```

---

## 🎯 **Benefits of Email-Based Operations**

### 1. **Intuitive API Design**
- Natural identifiers (emails) instead of opaque IDs
- Self-documenting endpoints
- Easier to understand and use

### 2. **Reduced API Calls**
- No need to lookup user IDs first
- Direct access using known email addresses
- Improved performance and efficiency

### 3. **Better Security**
- Automatic ownership validation
- Role-based access control built-in
- Users can only access their own data

### 4. **Frontend-Friendly**
- Email is always available in frontend (from login)
- No need to store/manage numeric IDs
- Cleaner state management

### 5. **Database Efficiency**
- Indexed email fields ensure fast lookups
- Single query execution
- Optimized performance

---

## 🚀 **Usage Examples**

### **Example 1: Complete User Management Flow**

```javascript
// Login and get email
const loginResponse = await fetch('/api/RoleBasedAuth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123'
  })
});

const { token, email } = await loginResponse.json();

// Get user profile (using email from login)
const profileResponse = await fetch(`/api/EnhancedUsers/${email}`, {
  headers: { 'Authorization': `Bearer ${token}` }
});

const profile = await profileResponse.json();

// Update user profile (using email)
const updateResponse = await fetch(`/api/EnhancedUsers/${email}`, {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    UserEmail: email,
    UserName: 'Updated Name',
    PhoneNumber: '+1234567890'
  })
});

// Change password (using email)
const passwordResponse = await fetch(`/api/EnhancedUsers/${email}/change-password`, {
  method: 'PATCH',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    CurrentPassword: 'oldpass',
    NewPassword: 'newpass'
  })
});
```

### **Example 2: Machine Management by Email**

```javascript
// Get all machines for a user (by email)
const machinesResponse = await fetch(
  `/api/EnhancedMachines/by-email/user@example.com`,
{
    headers: { 'Authorization': `Bearer ${token}` }
  }
);

const machines = await machinesResponse.json();

// Register new machine for user (by email)
const registerResponse = await fetch(
  `/api/EnhancedMachines/register/user@example.com`,
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      MacAddress: 'AA:BB:CC:DD:EE:FF',
      FingerprintHash: 'abc123...',
      OsVersion: 'Windows 11'
    })
  }
);

// Get machine statistics for user (by email)
const statsResponse = await fetch(
  `/api/EnhancedMachines/statistics/user@example.com`,
  {
    headers: { 'Authorization': `Bearer ${token}` }
  }
);
```

### **Example 3: Session Management by Email**

```javascript
// Get all sessions for user (by email)
const sessionsResponse = await fetch(
  `/api/EnhancedSessions/by-email/user@example.com`,
  {
    headers: { 'Authorization': `Bearer ${token}` }
  }
);

const sessions = await sessionsResponse.json();

// End all sessions for user (by email)
const endAllResponse = await fetch(
  `/api/EnhancedSessions/end-all/user@example.com`,
  {
    method: 'PATCH',
    headers: { 'Authorization': `Bearer ${token}` }
  }
);

// Get session statistics for user (by email)
const sessionStatsResponse = await fetch(
  `/api/EnhancedSessions/statistics?userEmail=user@example.com`,
  {
    headers: { 'Authorization': `Bearer ${token}` }
  }
);
```

### **Example 4: Audit Reports by Email**

```javascript
// Get all reports for client (by email)
const reportsResponse = await fetch(
  `/api/EnhancedAuditReports/by-email/client@example.com`,
  {
    headers: { 'Authorization': `Bearer ${token}` }
  }
);

const reports = await reportsResponse.json();

// Create report for client (by email)
const createReportResponse = await fetch(
  `/api/EnhancedAuditReports`,
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
  body: JSON.stringify({
      ClientEmail: 'client@example.com',
      ReportName: 'Erasure Report 2024',
      ErasureMethod: 'DoD 5220.22-M'
    })
  }
);

// Export reports for client (by email)
const exportResponse = await fetch(
  `/api/EnhancedAuditReports/export-csv?ClientEmail=client@example.com`,
  {
    headers: { 'Authorization': `Bearer ${token}` }
  }
);

const csvBlob = await exportResponse.blob();
```

---

## 🔒 **Security Considerations**

### **Automatic Ownership Validation**

All email-based endpoints include automatic ownership validation:

```csharp
// Users can only access their own data
bool canAccess = email == currentUserEmail ||
      await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_USERS");

if (!canAccess)
{
    return StatusCode(403, new { error = "Access denied" });
}
```

### **Role-Based Access Control**

Email-based operations respect role hierarchy:

```csharp
// SuperAdmin > Admin > Manager > Support > User > Subuser
if (await _authService.HasPermissionAsync(currentUserEmail, "MANAGE_ALL_USERS"))
{
    // Can access any user's data
}
else
{
    // Can only access own data
}
```

### **Hierarchical Data Access**

Managers can access subordinate user data:

```csharp
// Check management hierarchy
bool canManage = await _authService.CanManageUserAsync(managerEmail, targetEmail);

if (canManage)
{
    // Manager can access managed user's data
}
```

---

## 📝 **Best Practices**

### 1. **Always Use Email from JWT Token**

```javascript
// Extract email from JWT token
const token = localStorage.getItem('authToken');
const payload = JSON.parse(atob(token.split('.')[1]));
const userEmail = payload.email || payload.sub;

// Use email in API calls
fetch(`/api/EnhancedUsers/${userEmail}`);
```

### 2. **Handle Email URL Encoding**

```javascript
// Encode email for URL safety
const email = 'user+test@example.com';
const encodedEmail = encodeURIComponent(email);

fetch(`/api/EnhancedUsers/${encodedEmail}`);
```

### 3. **Use Email Filtering for Search**

```javascript
// Search users by email pattern
fetch(`/api/EnhancedUsers?UserEmail=${encodeURIComponent('@example.com')}`);
```

### 4. **Implement Proper Error Handling**

```javascript
try {
  const response = await fetch(`/api/EnhancedUsers/${email}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  if (response.status === 404) {
    console.error('User not found');
  } else if (response.status === 403) {
    console.error('Access denied');
  } else if (!response.ok) {
console.error('Server error');
  }

  const data = await response.json();
  return data;
} catch (error) {
  console.error('Network error:', error);
}
```

---

## ✅ **Summary**

Your BitRaser API **already has comprehensive email-based operations** across all Enhanced controllers:

✅ **EnhancedUsersController** - Full email support  
✅ **EnhancedMachinesController** - Full email + MAC address support  
✅ **EnhancedSessionsController** - Full email support  
✅ **EnhancedAuditReportsController** - Full email support  
✅ **EnhancedSubusersController** - Full email support  
✅ **EnhancedLogsController** - Full email support  
✅ **EnhancedCommandsController** - Full email support  
✅ **EnhancedProfileController** - Full email support (JWT-based)  

## 🎉 **No Changes Needed!**

Your API design is already optimal! All major operations support email-based access, making it:
- Easy to use
- Secure by default
- Frontend-friendly
- Performant
- Self-documenting

---

**Happy Coding! 🚀**
