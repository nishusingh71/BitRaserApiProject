# 🚀 Enhanced Controllers with Role-Based Access Control

## 📋 **Overview**

आपके original routes में अब comprehensive role-based access control, dynamic permissions, और hierarchical user management की सुविधा add की गई है।

## 🔧 **Enhanced Controllers Created**

### 1. **EnhancedMachinesController** 
- **Path**: `api/EnhancedMachines`
- **Features**: Machine ownership validation, license management, hierarchical access

### 2. **EnhancedUsersController**
- **Path**: `api/EnhancedUsers` 
- **Features**: User management, role assignment, profile updates, statistics

### 3. **EnhancedAuditReportsController**
- **Path**: `api/EnhancedAuditReports`
- **Features**: Report management, export functionality, statistics, client validation

## 🛡️ **Role-Based Permissions Structure**

### **Machine Management Permissions**
```
READ_ALL_MACHINES          - View all machines (Admin/Manager)
READ_MACHINE               - View own machines (User+)
READ_USER_MACHINES         - View managed users' machines (Manager+)
READ_LICENSE_STATUS        - Check license status (User+)
CREATE_MACHINE             - Create new machines (User+)
UPDATE_MACHINE             - Update own machines (User+)
DELETE_MACHINE             - Delete machines (Manager+)
MANAGE_ALL_MACHINES        - Full machine management (Admin)
ACTIVATE_LICENSE           - Activate licenses (Manager+)
DEACTIVATE_LICENSE         - Deactivate licenses (Admin)
RENEW_LICENSE              - Renew licenses (Manager+)
MANAGE_ALL_LICENSES        - Full license management (Admin)
```

### **User Management Permissions**
```
READ_ALL_USERS             - View all users (Admin)
READ_USER                  - View user profiles (User+)
READ_USER_STATISTICS       - View user stats (Manager+)
UPDATE_USER                - Update user profiles (User+)
UPDATE_USER_LICENSE        - Update licenses (Manager+)
UPDATE_PAYMENT_DETAILS     - Update payment info (User+)
CHANGE_PASSWORD            - Change passwords (User+)
DELETE_USER                - Delete users (Admin)
MANAGE_ALL_USERS           - Full user management (Admin)
ASSIGN_ROLES               - Assign roles (Admin)
REMOVE_ROLES               - Remove roles (Admin)
CREATE_REPORTS_FOR_OTHERS  - Create reports for others (Manager+)
```

### **Report Management Permissions**
```
READ_ALL_REPORTS           - View all reports (Admin/Manager)
READ_REPORT                - View own reports (User+)
READ_USER_REPORTS          - View managed users' reports (Manager+)
READ_REPORT_STATISTICS     - View report statistics (Manager+)
READ_ALL_REPORT_STATISTICS - View all report statistics (Admin)
UPDATE_REPORT              - Update own reports (User+)
UPDATE_ALL_REPORTS         - Update all reports (Admin)
DELETE_REPORT              - Delete own reports (User+)
DELETE_ALL_REPORTS         - Delete all reports (Admin)
EXPORT_REPORTS             - Export own reports (User+)
EXPORT_ALL_REPORTS         - Export all reports (Admin)
```

## 🎯 **API Endpoints with Role Requirements**

### **Enhanced Machines API**

#### **Get All Machines**
```http
GET /api/EnhancedMachines
Authorization: Bearer <token>
Permission Required: READ_ALL_MACHINES (Admin/Manager) | Shows own machines for Users
```

#### **Get Machine by Hash**
```http
GET /api/EnhancedMachines/by-hash/{hash}
Authorization: Bearer <token>
Permission Required: READ_MACHINE + ownership validation
```

#### **Get Machine by MAC (Anonymous)**
```http
GET /api/EnhancedMachines/by-mac/{mac}
No Authorization Required
Returns: Limited machine information for client validation
```

#### **License Operations**
```http
GET /api/EnhancedMachines/license-status/{mac}
POST /api/EnhancedMachines/activate-license/{mac}
POST /api/EnhancedMachines/deactivate-license/{mac}
PATCH /api/EnhancedMachines/renew-license/{mac}?additionalDays=30
```

### **Enhanced Users API**

#### **User Profile Management**
```http
GET /api/EnhancedUsers/{email}
PUT /api/EnhancedUsers/{email}
DELETE /api/EnhancedUsers/{email}
```

#### **User Statistics**
```http
GET /api/EnhancedUsers/{email}/statistics
Returns: Machines, subusers, sessions, logs, reports count
```

#### **Role Management**
```http
POST /api/EnhancedUsers/{email}/assign-role
DELETE /api/EnhancedUsers/{email}/remove-role/{roleId}
```

#### **Security Operations**
```http
PATCH /api/EnhancedUsers/change-password/{email}
PATCH /api/EnhancedUsers/update-license/{email}
PATCH /api/EnhancedUsers/update-payment/{email}
```

### **Enhanced Audit Reports API**

#### **Report Management**
```http
GET /api/EnhancedAuditReports
POST /api/EnhancedAuditReports
PUT /api/EnhancedAuditReports/{id}
DELETE /api/EnhancedAuditReports/{id}
```

#### **Advanced Features**
```http
GET /api/EnhancedAuditReports/statistics
GET /api/EnhancedAuditReports/export
POST /api/EnhancedAuditReports/reserve-id
PUT /api/EnhancedAuditReports/upload-report/{id}
PATCH /api/EnhancedAuditReports/mark-synced/{id}
```

## 🔐 **Authentication & Authorization Flow**

### **1. JWT Token Validation**
```csharp
var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

### **2. Permission Check**
```csharp
if (!await _authService.HasPermissionAsync(userEmail!, "PERMISSION_NAME"))
    return statusCode(403,new {error ="Insufficient permissions");
```

### **3. Ownership Validation**
```csharp
if (resource.user_email != userEmail && 
    !await _authService.HasPermissionAsync(userEmail!, "ADMIN_PERMISSION"))
    return statusCode(403,new {error ="You can only access your own resources");
```

### **4. Hierarchical Management Check**
```csharp
if (!await _authService.CanManageUserAsync(currentUserEmail!, targetUserEmail))
    return statusCode(403,new {error ="You can only manage users assigned to you");
```

## 📊 **Usage Examples**

### **1. Create Machine with Auto-Assignment**
```http
POST /api/EnhancedMachines
Authorization: Bearer <token>
Content-Type: application/json

{
  "fingerprint_hash": "ABC123",
  "mac_address": "00:11:22:33:44:55",
  "cpu_id": "CPU123",
  "bios_serial": "BIOS456",
  "os_version": "Windows 11"
}
```

### **2. Get User Statistics**
```http
GET /api/EnhancedUsers/user@example.com/statistics
Authorization: Bearer <token>

Response:
{
  "totalMachines": 5,
  "activeMachines": 3,
  "totalSubusers": 2,
  "totalSessions": 15,
  "recentLogs": 8,
  "totalReports": 12
}
```

### **3. Filter Reports with Pagination**
```http
GET /api/EnhancedAuditReports?clientEmail=user@example.com&dateFrom=2024-01-01&page=0&pageSize=50
Authorization: Bearer <token>
```

### **4. Export Reports to CSV**
```http
GET /api/EnhancedAuditReports/export?dateFrom=2024-01-01&dateTo=2024-12-31
Authorization: Bearer <token>
```

## 🎛️ **Configuration Setup**

### **1. Update Program.cs**
```csharp
builder.Services.AddScoped<IRoleBasedAuthService, RoleBasedAuthService>();
```

### **2. Database Migration**
Run the existing migrations to set up role tables:
```bash
dotnet ef database update
```

### **3. Initialize Default Roles**
The system will automatically create default roles and permissions on startup.

## 🔄 **Migration from Original Controllers**

### **Replace Original Endpoints**
1. **MachinesController** → **EnhancedMachinesController**
2. **UsersController** → **EnhancedUsersController**  
3. **AuditReportsController** → **EnhancedAuditReportsController**

### **Update Client Applications**
- Change API endpoints to use new paths
- Add proper JWT tokens to requests
- Handle new permission-based error responses
- Use new request/response models

## 🛠️ **Testing the Enhanced Controllers**

### **1. Test with Different Roles**
```bash
# Login as Admin
POST /api/Auth/login
{
  "email": "admin@example.com",
  "password": "password"
}

# Use token to access admin-only endpoints
GET /api/EnhancedMachines
Authorization: Bearer <admin_token>
```

### **2. Test Ownership Validation**
```bash
# Login as regular user
POST /api/Auth/login
{
  "email": "user@example.com", 
  "password": "password"
}

# Try to access another user's data (should fail)
GET /api/EnhancedUsers/otheruser@example.com
Authorization: Bearer <user_token>
```

### **3. Test Hierarchical Access**
```bash
# Manager accessing subordinate user's data
GET /api/EnhancedUsers/subordinate@example.com/statistics
Authorization: Bearer <manager_token>
```

## 🎉 **Benefits of Enhanced Controllers**

### **✅ Security Features**
- **JWT-based authentication** on all endpoints
- **Fine-grained permissions** for each operation
- **Ownership validation** for user resources
- **Hierarchical access control** for managers
- **Audit trails** for all operations

### **✅ User Experience**
- **Automatic user assignment** for new resources
- **Role-based UI adaptation** possibilities
- **Comprehensive error messages** with clear reasons
- **Bulk operations** for administrators
- **Export functionality** for reports

### **✅ Administration**
- **Real-time permission changes** without restart
- **Dynamic role assignments** 
- **User hierarchy management**
- **Comprehensive statistics** and reporting
- **CSV export** for data analysis

## 🚨 **Important Notes**

1. **Use Enhanced Controllers**: Replace calls to original controllers with enhanced versions
2. **JWT Required**: All requests (except anonymous ones) need valid JWT tokens
3. **Permission Errors**: Handle 403 Forbidden responses gracefully
4. **Ownership Validation**: Users can only access their own resources unless they have admin permissions
5. **Role Assignment**: Only admins can assign/remove roles

Your original functionality is now enhanced with comprehensive security and role management! 🔒✨