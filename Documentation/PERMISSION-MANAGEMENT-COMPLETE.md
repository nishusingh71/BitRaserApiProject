# 🔐 Permission Management System - Complete Guide

## 📋 Overview

यह system SuperAdmin और Admin को authority देता है कि वे अपने नीचे के roles की permissions को modify कर सकें।

---

## 🎯 Key Features

### **1. Hierarchical Permission Control**
- ✅ **SuperAdmin**: सभी roles की permissions modify कर सकते हैं
- ✅ **Admin**: Manager, Support, User, SubUser की permissions modify कर सकते हैं (SuperAdmin की नहीं)
- ❌ **Others**: Permission modification नहीं कर सकते

### **2. Permission Operations**
- ✅ View permissions for any role
- ✅ Add permission to role
- ✅ Remove permission from role
- ✅ Replace all permissions for a role
- ✅ View all available permissions

### **3. Real-time Updates**
- Changes immediately reflect on next login
- No system restart required
- Automatic validation

---

## 🚀 API Endpoints

### **1. Get Permissions for a Role**

```http
GET /api/RoleBasedAuth/roles/{roleName}/permissions
Authorization: Bearer {jwt_token}
```

**Example:**
```http
GET /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "roleName": "Manager",
  "permissions": [
    "UserManagement",
    "ReportAccess",
"MachineManagement",
    "ViewOnly"
  ],
  "count": 4
}
```

**Access:** ✅ All authenticated users can view

---

### **2. Get All Available Permissions**

```http
GET /api/RoleBasedAuth/permissions/all
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "permissions": [
    {
      "permissionId": 1,
      "permissionName": "FullAccess",
      "description": "Complete system access"
    },
    {
      "permissionId": 2,
      "permissionName": "UserManagement",
      "description": "Manage users and subusers"
    },
    {
      "permissionId": 3,
"permissionName": "ReportAccess",
   "description": "Access and manage reports"
    }
  ],
  "count": 3
}
```

**Access:** ✅ All authenticated users can view

---

### **3. Add Permission to Role**

```http
POST /api/RoleBasedAuth/roles/{roleName}/permissions
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "PermissionName": "DELETE_USER"
}
```

**Example - Add DELETE_USER permission to Manager role:**
```http
POST /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "PermissionName": "DELETE_USER"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Permission 'DELETE_USER' added to role 'Manager'",
  "roleName": "Manager",
  "permissionName": "DELETE_USER",
  "modifiedBy": "admin@company.com",
  "modifiedAt": "2024-01-15T10:30:00Z"
}
```

**Error Response (403 Forbidden):**
```json
{
  "message": "You cannot modify permissions for role 'SuperAdmin'",
  "detail": "Only SuperAdmin and Admin can modify role permissions, and only for roles below their level"
}
```

**Access:** ✅ SuperAdmin, Admin only (for lower-level roles)

---

### **4. Remove Permission from Role**

```http
DELETE /api/RoleBasedAuth/roles/{roleName}/permissions/{permissionName}
Authorization: Bearer {admin_token}
```

**Example - Remove DELETE_USER permission from Manager role:**
```http
DELETE /api/RoleBasedAuth/roles/Manager/permissions/DELETE_USER
Authorization: Bearer {admin_token}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Permission 'DELETE_USER' removed from role 'Manager'",
  "roleName": "Manager",
  "permissionName": "DELETE_USER",
  "modifiedBy": "admin@company.com",
  "modifiedAt": "2024-01-15T10:35:00Z"
}
```

**Access:** ✅ SuperAdmin, Admin only

---

### **5. Update All Permissions for a Role (Replace)**

```http
PUT /api/RoleBasedAuth/roles/{roleName}/permissions
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "PermissionNames": [
    "UserManagement",
    "ReportAccess",
    "MachineManagement",
    "DELETE_USER"
  ]
}
```

**Example - Update Manager role permissions:**
```http
PUT /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "PermissionNames": [
    "UserManagement",
"CREATE_USER",
    "UPDATE_USER",
    "DELETE_USER",
    "ReportAccess",
    "MachineManagement"
  ]
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Permissions updated for role 'Manager'",
  "roleName": "Manager",
  "permissions": [
    "UserManagement",
    "CREATE_USER",
    "UPDATE_USER",
    "DELETE_USER",
    "ReportAccess",
    "MachineManagement"
  ],
  "modifiedBy": "superadmin@company.com",
  "modifiedAt": "2024-01-15T10:40:00Z"
}
```

**Access:** ✅ SuperAdmin, Admin only

---

## 🔍 Access Control Matrix

| Requester Role | Can Modify Permissions For |
|----------------|---------------------------|
| **SuperAdmin** | All roles (SuperAdmin, Admin, Manager, Support, User, SubUser) |
| **Admin** | Manager, Support, User, SubUser (NOT SuperAdmin) |
| **Manager** | ❌ Cannot modify any role permissions |
| **Support** | ❌ Cannot modify any role permissions |
| **User** | ❌ Cannot modify any role permissions |

### **Validation Rules:**

```csharp
// ✅ SuperAdmin can modify any role
SuperAdmin → Can modify → [SuperAdmin, Admin, Manager, Support, User, SubUser]

// ✅ Admin can modify lower-level roles only
Admin → Can modify → [Manager, Support, User, SubUser]
Admin → Cannot modify → [SuperAdmin, Admin]

// ❌ Others cannot modify permissions
Manager/Support/User → Cannot modify → Any role
```

---

## 🎯 Use Cases & Examples

### **Use Case 1: Admin Adding Permission to Manager Role**

**Scenario:** Admin wants to give DELETE_USER permission to Manager role

```bash
# Step 1: Login as Admin
POST /api/RoleBasedAuth/login
{
  "email": "admin@company.com",
  "password": "admin123"
}
# Save token

# Step 2: View current Manager permissions
GET /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}

# Response:
{
  "permissions": ["UserManagement", "ReportAccess", "MachineManagement"]
}

# Step 3: Add DELETE_USER permission
POST /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}
{
"PermissionName": "DELETE_USER"
}

# Response:
{
  "success": true,
  "message": "Permission 'DELETE_USER' added to role 'Manager'"
}

# Step 4: Verify updated permissions
GET /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}

# Response:
{
  "permissions": [
    "UserManagement",
    "ReportAccess", 
    "MachineManagement",
    "DELETE_USER"  // ✅ New permission added
  ]
}
```

**Result:** अब सभी Manager role वाले users को DELETE_USER permission मिल जाएगी!

---

### **Use Case 2: SuperAdmin Updating All Permissions for Support Role**

**Scenario:** SuperAdmin wants to completely replace Support role permissions

```bash
# Step 1: Login as SuperAdmin
POST /api/RoleBasedAuth/login
{
  "email": "superadmin@company.com",
  "password": "superadmin123"
}

# Step 2: Get all available permissions
GET /api/RoleBasedAuth/permissions/all
Authorization: Bearer {superadmin_token}

# Response: List of all permissions

# Step 3: Update Support role with new permission set
PUT /api/RoleBasedAuth/roles/Support/permissions
Authorization: Bearer {superadmin_token}
{
  "PermissionNames": [
    "ViewOnly",
    "READ_USER",
    "READ_REPORT",
    "UPDATE_REPORT",
    "READ_SESSION",
    "READ_LOG"
  ]
}

# Response:
{
  "success": true,
  "message": "Permissions updated for role 'Support'",
  "permissions": [
    "ViewOnly",
    "READ_USER",
    "READ_REPORT",
    "UPDATE_REPORT",
    "READ_SESSION",
    "READ_LOG"
  ]
}
```

**Result:** Support role की सभी पुरानी permissions हट गईं और नई permissions assign हो गईं!

---

### **Use Case 3: Admin Trying to Modify SuperAdmin Role (Will Fail)**

```bash
# Step 1: Login as Admin
POST /api/RoleBasedAuth/login
{
  "email": "admin@company.com",
  "password": "admin123"
}

# Step 2: Try to add permission to SuperAdmin role
POST /api/RoleBasedAuth/roles/SuperAdmin/permissions
Authorization: Bearer {admin_token}
{
  "PermissionName": "SomePermission"
}

# Response: 403 Forbidden
{
  "message": "You cannot modify permissions for role 'SuperAdmin'",
  "detail": "Only SuperAdmin and Admin can modify role permissions, and only for roles below their level"
}
```

**Result:** ❌ Admin SuperAdmin role की permissions modify नहीं कर सकते!

---

## 🔄 How Permissions are Reflected

### **Immediate Effect:**
Permission changes **immediately reflect** on:
1. ✅ Next user login
2. ✅ GET `/api/RoleBasedAuth/my-permissions` endpoint
3. ✅ Any permission-protected endpoint

### **Example Flow:**

```bash
# 1. Manager user currently has these permissions
GET /api/RoleBasedAuth/my-permissions
Authorization: Bearer {manager_token}

Response:
{
  "permissions": ["UserManagement", "ReportAccess"],
  "roles": ["Manager"]
}

# 2. Admin adds DELETE_USER permission to Manager role
POST /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}
{
  "PermissionName": "DELETE_USER"
}

# 3. Manager checks permissions again (same token)
GET /api/RoleBasedAuth/my-permissions
Authorization: Bearer {manager_token}

Response:
{
  "permissions": [
    "UserManagement",
  "ReportAccess",
    "DELETE_USER"  // ✅ New permission immediately available!
  ],
  "roles": ["Manager"]
}

# 4. Manager can now delete users (if endpoint requires DELETE_USER)
DELETE /api/EnhancedUsers/user@example.com
Authorization: Bearer {manager_token}
# ✅ Access granted!
```

---

## 🧪 Testing Scenarios

### **Test 1: Admin Adding Permission to Manager**

```bash
# Login as Admin
POST /api/RoleBasedAuth/login
{
  "email": "admin@company.com",
  "password": "Admin@123"
}
# Expected: 200 OK with token

# Add CREATE_SUBUSER permission to Manager role
POST /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}
{
  "PermissionName": "CREATE_SUBUSER"
}
# Expected: 200 OK
```

---

### **Test 2: Admin Trying to Modify SuperAdmin (Should Fail)**

```bash
# Try to add permission to SuperAdmin role
POST /api/RoleBasedAuth/roles/SuperAdmin/permissions
Authorization: Bearer {admin_token}
{
  "PermissionName": "SomePermission"
}
# Expected: 403 Forbidden
# Message: "You cannot modify permissions for role 'SuperAdmin'"
```

---

### **Test 3: Manager Trying to Modify Support (Should Fail)**

```bash
# Login as Manager
POST /api/RoleBasedAuth/login
{
  "email": "manager@company.com",
  "password": "Manager@123"
}

# Try to add permission to Support role
POST /api/RoleBasedAuth/roles/Support/permissions
Authorization: Bearer {manager_token}
{
  "PermissionName": "DELETE_USER"
}
# Expected: 403 Forbidden
# Message: "You cannot modify permissions for role 'Support'"
```

---

### **Test 4: SuperAdmin Updating All Permissions**

```bash
# Update User role with complete permission set
PUT /api/RoleBasedAuth/roles/User/permissions
Authorization: Bearer {superadmin_token}
{
  "PermissionNames": [
    "ViewOnly",
    "READ_USER",
    "VIEW_PROFILE",
    "UPDATE_PROFILE"
  ]
}
# Expected: 200 OK with updated permissions list
```

---

### **Test 5: View All Available Permissions**

```bash
GET /api/RoleBasedAuth/permissions/all
Authorization: Bearer {any_token}
# Expected: 200 OK with all permissions
```

---

## 📊 Common Permissions List

### **System Administration**
- `FullAccess` - Complete system control (SuperAdmin only)
- `SystemConfiguration` - System settings management
- `GlobalSettings` - Global configuration

### **User Management**
- `UserManagement` - Complete user management
- `CREATE_USER` - Create new users
- `READ_USER` - View user details
- `UPDATE_USER` - Update user information
- `DELETE_USER` - Delete users
- `CREATE_SUBUSER` - Create subusers
- `UPDATE_SUBUSER` - Update subuser details
- `DELETE_SUBUSER` - Delete subusers
- `READ_ALL_USERS` - View all users
- `UPDATE_ALL_USERS` - Update any user
- `DELETE_ALL_USERS` - Delete any user

### **Report Management**
- `ReportAccess` - General report access
- `CREATE_REPORT` - Create reports
- `READ_REPORT` - View reports
- `UPDATE_REPORT` - Update reports
- `DELETE_REPORT` - Delete reports
- `EXPORT_REPORTS` - Export report data

### **Machine & License**
- `MachineManagement` - Machine operations
- `ADD_MACHINE` - Add machines
- `EDIT_MACHINE` - Edit machine details
- `DELETE_MACHINE` - Delete machines
- `VIEW_MACHINES` - View machines
- `LicenseManagement` - License operations
- `ValidateLicense` - Validate licenses
- `AssignLicense` - Assign licenses
- `RevokeLicense` - Revoke licenses

### **System Monitoring**
- `SystemLogs` - Access system logs
- `ViewLogs` - View log entries
- `ViewSessions` - View user sessions
- `MonitorSystem` - System monitoring
- `END_SESSION` - End user sessions
- `EXTEND_SESSION` - Extend session duration

### **Basic Access**
- `ViewOnly` - Read-only access
- `VIEW_PROFILE` - View own profile
- `UPDATE_PROFILE` - Update own profile
- `AccessDashboard` - Dashboard access

---

## 🔧 Database Structure

### **Tables Involved:**

1. **Roles** - Role definitions
```sql
RoleId, RoleName, Description, HierarchyLevel
```

2. **Permissions** - Permission definitions
```sql
PermissionId, PermissionName, Description
```

3. **RolePermissions** - Role-Permission mapping
```sql
RoleId, PermissionId
```

### **Key Queries:**

```sql
-- View all permissions for Manager role
SELECT p.PermissionName
FROM Roles r
JOIN RolePermissions rp ON r.RoleId = rp.RoleId
JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE r.RoleName = 'Manager';

-- Add permission to role
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 
    (SELECT RoleId FROM Roles WHERE RoleName = 'Manager'),
    (SELECT PermissionId FROM Permissions WHERE PermissionName = 'DELETE_USER');

-- Remove permission from role
DELETE FROM RolePermissions
WHERE RoleId = (SELECT RoleId FROM Roles WHERE RoleName = 'Manager')
  AND PermissionId = (SELECT PermissionId FROM Permissions WHERE PermissionName = 'DELETE_USER');
```

---

## 🎓 Best Practices

### **1. Principle of Least Privilege**
```bash
# ❌ Don't give too many permissions
{
  "PermissionNames": [
    "FullAccess",  # Too broad!
    "DELETE_ALL_USERS",
    "SystemConfiguration"
  ]
}

# ✅ Give only required permissions
{
  "PermissionNames": [
    "READ_USER",
    "UPDATE_USER",
    "VIEW_PROFILE"
  ]
}
```

### **2. Regular Audits**
- Review role permissions monthly
- Remove unused permissions
- Check for permission creep

### **3. Test Before Deploying**
- Test permission changes in development first
- Verify user access after changes
- Have rollback plan ready

### **4. Document Changes**
- Keep track of permission modifications
- Document why changes were made
- Maintain change log

---

## 🚨 Troubleshooting

### **Issue 1: Permission Added but User Still Can't Access**

**Solution:**
```bash
# User needs to re-login to get updated permissions
POST /api/RoleBasedAuth/login
{
  "email": "user@example.com",
  "password": "password"
}

# Or check current permissions without re-login
GET /api/RoleBasedAuth/my-permissions
Authorization: Bearer {current_token}
```

### **Issue 2: Cannot Add Permission to Role**

**Check:**
1. ✅ Are you SuperAdmin or Admin?
2. ✅ Is target role below your level?
3. ✅ Does permission exist in system?

**Verify:**
```bash
# Check available permissions
GET /api/RoleBasedAuth/permissions/all

# Check your role
GET /api/RoleBasedAuth/my-permissions
```

### **Issue 3: 403 Forbidden when Modifying Permissions**

**Cause:** Trying to modify role at same or higher level

**Solution:** Only SuperAdmin and Admin can modify permissions, and only for lower-level roles

---

## 📝 Summary

### **Key Points:**

1. ✅ **SuperAdmin** - सभी roles की permissions modify कर सकते हैं
2. ✅ **Admin** - Manager, Support, User, SubUser की permissions modify कर सकते हैं
3. ❌ **Others** - Permission modification नहीं कर सकते
4. ✅ **Changes immediate** - Re-login के बाद तुरंत effect होता है
5. ✅ **Audit trail** - सभी changes log होते हैं

### **Quick Commands:**

```bash
# View role permissions
GET /api/RoleBasedAuth/roles/{roleName}/permissions

# Add permission
POST /api/RoleBasedAuth/roles/{roleName}/permissions
{"PermissionName": "DELETE_USER"}

# Remove permission
DELETE /api/RoleBasedAuth/roles/{roleName}/permissions/{permissionName}

# Replace all permissions
PUT /api/RoleBasedAuth/roles/{roleName}/permissions
{"PermissionNames": ["Permission1", "Permission2"]}

# View all permissions
GET /api/RoleBasedAuth/permissions/all
```

---

**Permission Management System अब पूरी तरह से implemented है!** 🚀

SuperAdmin और Admin अब आसानी से अपने नीचे के roles की permissions को manage कर सकते हैं!
