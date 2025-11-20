# 🔐 Hierarchical Role-Based Access Control System

## 📋 Overview

यह system comprehensive hierarchical access control provide करता है जहाँ users अपने से **नीचे के (lower privilege)** users को ही manage कर सकते हैं।

## 🎯 Core Principles (मुख्य सिद्धांत)

### 1. **Role Hierarchy** (भूमिका पदानुक्रम)

```
Level 1: SuperAdmin    ← Highest Authority (सर्वोच्च अधिकार)
  ↓
Level 2: Admin    ← Administrative Access
    ↓
Level 3: Manager       ← Departmental Management
    ↓
Level 4: Support   ← Support Operations
    ↓
Level 5: User          ← Basic End User
Level 6: SubUser   ← Subordinate User
```

### 2. **Access Rules** (पहुंच नियम)

#### ✅ **ALLOWED (अनुमति है)**
- **SuperAdmin**: पूरे system पर full access
- **Admin**: Manager, Support, User, SubUser को manage कर सकते हैं
- **Manager**: Support, User, SubUser को manage कर सकते हैं
- **Support**: User, SubUser को manage कर सकते हैं
- **User**: केवल अपने SubUsers को manage कर सकते हैं

#### ❌ **NOT ALLOWED (अनुमति नहीं है)**
- **Admin CANNOT**: SuperAdmin users create/read/update/delete
- **Manager CANNOT**: Admin या SuperAdmin users को manage
- **Users CANNOT**: Same या higher level के users को manage
- **User Role CANNOT**: नए subusers create करना

### 3. **Strict Hierarchy Enforcement**

```csharp
// ✅ Manager (level 3) managing Support (level 4) - ALLOWED
managerLevel (3) < supportLevel (4) = TRUE → Access Granted

// ❌ Admin (level 2) managing another Admin (level 2) - DENIED
adminLevel (2) < adminLevel (2) = FALSE → Access Denied

// ❌ Manager (level 3) managing Admin (level 2) - DENIED  
managerLevel (3) < adminLevel (2) = FALSE → Access Denied
```

## 🛡️ Implementation Details

### **New Service Methods**

#### 1. **CanAssignRoleAsync**
```csharp
// Check if user can assign a specific role
await _authService.CanAssignRoleAsync(assignerEmail, "Manager");
```

**Logic:**
- SuperAdmin can assign any role
- Others can only assign roles with **higher** hierarchy level (lower privilege)
- Example: Admin (level 2) can assign Manager (3), Support (4), User (5)
- Example: Admin (level 2) CANNOT assign Admin (2) or SuperAdmin (1)

#### 2. **CanCreateSubusersAsync**
```csharp
// Check if user can create subusers
await _authService.CanCreateSubusersAsync(userEmail);
```

**Logic:**
- "User" role CANNOT create subusers
- All other roles (SuperAdmin, Admin, Manager, Support) CAN create subusers

#### 3. **GetManagedUserEmailsAsync**
```csharp
// Get all users/subusers a manager can access
var managedEmails = await _authService.GetManagedUserEmailsAsync(managerEmail);
```

**Returns:**
- SuperAdmin: सभी users और subusers
- Others: केवल lower privilege users और उनके subusers

#### 4. **Enhanced CanManageUserAsync**
```csharp
// Check if manager can manage target user/subuser
await _authService.CanManageUserAsync(managerEmail, targetEmail, isTargetSubuser);
```

**Enhanced Logic:**
- ✅ Checks hierarchy level (manager < target)
- ✅ For subusers, validates parent user relationship
- ✅ SuperAdmin bypasses all checks

## 📊 Access Matrix (पहुंच मैट्रिक्स)

| Requester Role | Can Create | Can Read | Can Update | Can Delete |
|---------------|-----------|----------|------------|------------|
| **SuperAdmin** | All users/subusers | All users/subusers | All users/subusers | All users/subusers |
| **Admin** | Manager, Support, User, SubUser | Same + self | Same + self | Same |
| **Manager** | Support, User, SubUser | Same + self + managed | Same + self + managed | Same |
| **Support** | User, SubUser | Same + self + managed | Same + self + managed | Same |
| **User** | ❌ SubUser (CANNOT) | Own SubUsers + self | Own SubUsers + self | Own SubUsers |
| **SubUser** | ❌ None | Self only | Self only | ❌ None |

## 🚀 API Endpoint Updates

### **1. Create User**
```http
POST /api/EnhancedUsers
Authorization: Bearer {token}

{
  "UserEmail": "newuser@example.com",
  "UserName": "New User",
  "Password": "SecurePass@123",
  "DefaultRole": "Manager"
}
```

**Validation:**
- ✅ Checks if requester can assign "Manager" role
- ❌ Admin cannot create user with "SuperAdmin" role
- ❌ Manager cannot create user with "Admin" role

**Response:**
```json
{
  "success": false,
  "message": "You cannot create user with role 'SuperAdmin'",
  "detail": "You can only assign roles with lower privilege than your own"
}
```

### **2. Create Subuser**
```http
POST /api/RoleBasedAuth/create-subuser
Authorization: Bearer {token}

{
  "SubuserEmail": "subuser@example.com",
  "SubuserPassword": "password123",
  "RoleIds": [4, 5]
}
```

**Validation:**
- ✅ Checks if requester has "User" role
- ❌ Users with "User" role cannot create subusers
- ✅ Validates each role assignment

**Error Response:**
```json
{
  "success": false,
  "message": "You cannot create subusers",
  "detail": "Users with 'User' role are not allowed to create subusers"
}
```

### **3. Assign Role**
```http
POST /api/RoleBasedAuth/assign-role
Authorization: Bearer {token}

{
  "UserId": 123,
  "RoleId": 2
}
```

**Validation:**
- ✅ Checks if requester can assign this role
- ✅ Checks if requester can manage target user
- ❌ Prevents assigning equal or higher privilege roles

**Error Response:**
```json
{
  "message": "You cannot assign role 'Admin'",
  "detail": "You can only assign roles with lower privilege than your own"
}
```

### **4. Get Users (Filtered)**
```http
GET /api/EnhancedUsers
Authorization: Bearer {token}
```

**Filtering Logic:**
- **SuperAdmin**: सभी users दिखाई देंगे
- **Admin**: Manager, Support, User, SubUser दिखाई देंगे (SuperAdmin नहीं)
- **Manager**: Support, User, SubUser + own subusers दिखाई देंगे
- **User**: केवल own profile + own subusers

**Response:**
```json
[
  {
    "userEmail": "manager@example.com",
  "userName": "Manager User",
    "roles": ["Manager"],
"department": "Sales"
  }
]
```

### **5. Get Subusers (Filtered)**
```http
GET /api/EnhancedSubusers
Authorization: Bearer {token}
```

**Filtering Logic:**
- **SuperAdmin**: सभी subusers
- **Has READ_ALL_SUBUSERS**: Manageable subusers
- **Others**: केवल own subusers

## 🔍 Use Cases & Examples

### **Use Case 1: Admin Creating Users**

```csharp
// ✅ ALLOWED: Admin creating Manager
POST /api/EnhancedUsers
{
  "UserEmail": "manager@company.com",
  "DefaultRole": "Manager"  // ✅ Manager (level 3) < Admin (level 2)
}

// ❌ DENIED: Admin creating SuperAdmin
POST /api/EnhancedUsers
{
  "UserEmail": "admin2@company.com",
  "DefaultRole": "SuperAdmin"  // ❌ SuperAdmin (level 1) ≥ Admin (level 2)
}
→ Response: 403 Forbidden
```

### **Use Case 2: Manager Creating Subusers**

```csharp
// ✅ ALLOWED: Manager creating subuser with Support role
POST /api/EnhancedSubusers
{
  "Email": "support@company.com",
  "Role": "Support"  // ✅ Support (level 4) > Manager (level 3)
}

// ❌ DENIED: Manager creating subuser with Admin role
POST /api/EnhancedSubusers
{
  "Email": "admin@company.com",
  "Role": "Admin"  // ❌ Admin (level 2) < Manager (level 3)
}
→ Response: 403 Forbidden
```

### **Use Case 3: User Role Restrictions**

```csharp
// ❌ DENIED: User role trying to create subuser
POST /api/RoleBasedAuth/create-subuser
{
  "SubuserEmail": "subuser@company.com",
  "SubuserPassword": "password123"
}
→ Response: 403 Forbidden
→ Message: "Users with 'User' role cannot create subusers"
```

### **Use Case 4: Viewing Users**

```csharp
// SuperAdmin viewing users
GET /api/EnhancedUsers
→ Returns: ALL users (SuperAdmin, Admin, Manager, Support, User)

// Admin viewing users
GET /api/EnhancedUsers
→ Returns: Manager, Support, User, SubUser (NOT SuperAdmin)

// Manager viewing users
GET /api/EnhancedUsers
→ Returns: Support, User, SubUser (NOT SuperAdmin, Admin)

// User viewing users
GET /api/EnhancedUsers
→ Returns: Only own profile + own subusers
```

## 🔧 Configuration

### **Role Hierarchy Levels** (Database)

```sql
-- Roles table में HierarchyLevel field
INSERT INTO Roles (RoleName, Description, HierarchyLevel) VALUES
('SuperAdmin', 'Full system access', 1),
('Admin', 'Administrative access', 2),
('Manager', 'Departmental management', 3),
('Support', 'Support operations', 4),
('User', 'Basic end user', 5),
('SubUser', 'Subordinate user', 6);
```

### **Permission Assignment**

```csharp
// SuperAdmin: All permissions
FullAccess, UserManagement, CREATE_USER, DELETE_USER, ...

// Admin: All except FullAccess
UserManagement, CREATE_USER, UPDATE_USER, DELETE_USER, ...

// Manager: Management permissions
UserManagement, CREATE_SUBUSER, UPDATE_SUBUSER, ...

// Support: Limited management
READ_USER, READ_SUBUSER, UPDATE_SUBUSER, ...

// User: Basic permissions
READ_USER, VIEW_PROFILE, UPDATE_PROFILE

// SubUser: Minimal permissions
VIEW_PROFILE
```

## 📈 Benefits

### **1. Security**
- ✅ Users केवल अपने scope के data access कर सकते हैं
- ✅ Same-level या higher privilege users को manage नहीं कर सकते
- ✅ Prevents privilege escalation attacks

### **2. Data Isolation**
- ✅ Each role केवल appropriate data देख सकता है
- ✅ SuperAdmin visibility से बचाव
- ✅ Automatic filtering in API responses

### **3. Clear Responsibility**
- ✅ Clear hierarchy और responsibility chain
- ✅ Audit trail के लिए बेहतर
- ✅ Role-based operations easily trackable

## 🧪 Testing Scenarios

### **Test 1: Role Assignment Validation**
```csharp
// Login as Admin
POST /api/RoleBasedAuth/login
{ "email": "admin@company.com", "password": "admin123" }

// Try to create SuperAdmin user
POST /api/EnhancedUsers
{ "UserEmail": "superadmin2@company.com", "DefaultRole": "SuperAdmin" }
→ Expected: 403 Forbidden
→ Message: "You cannot create user with role 'SuperAdmin'"
```

### **Test 2: Subuser Creation by User Role**
```csharp
// Login as User
POST /api/RoleBasedAuth/login
{ "email": "user@company.com", "password": "user123" }

// Try to create subuser
POST /api/RoleBasedAuth/create-subuser
{ "SubuserEmail": "subuser@company.com", "SubuserPassword": "pass123" }
→ Expected: 403 Forbidden
→ Message: "Users with 'User' role cannot create subusers"
```

### **Test 3: Viewing Filtered Users**
```csharp
// Login as Manager
POST /api/RoleBasedAuth/login
{ "email": "manager@company.com", "password": "manager123" }

// Get all users
GET /api/EnhancedUsers
→ Expected: Only Support, User, SubUser visible (NOT SuperAdmin, Admin)
```

## 🔄 Migration from Old System

### **Before (Old System)**
```csharp
// No hierarchy checks - anyone could manage anyone
var users = await _context.Users.ToListAsync();
```

### **After (New System)**
```csharp
// Automatic hierarchy filtering
var managedEmails = await _authService.GetManagedUserEmailsAsync(currentUserEmail);
var users = await _context.Users
    .Where(u => managedEmails.Contains(u.user_email))
    .ToListAsync();
```

## 📚 Summary

### **Key Features**
1. ✅ **Strict Hierarchy**: Users can only manage lower privilege users
2. ✅ **Same-Level Protection**: Cannot manage users at same hierarchy level
3. ✅ **Role Validation**: Automatic validation before role assignment
4. ✅ **Filtered Queries**: API responses automatically filtered by hierarchy
5. ✅ **User Role Restriction**: "User" role cannot create subusers
6. ✅ **SuperAdmin Bypass**: SuperAdmin has full access everywhere

### **Access Pattern**
```
Manager → Can manage → [Support, User, SubUser]
Manager → Cannot manage → [SuperAdmin, Admin, Manager]
```

### **Error Messages**
- Clear और descriptive error messages
- Hindi explanation included where needed
- Proper HTTP status codes (403 Forbidden for permission issues)

---

## 🎉 Implementation Complete!

This hierarchical access control system ensures:
- ✅ Secure और scalable role management
- ✅ Clear hierarchy और responsibility
- ✅ Automatic access filtering
- ✅ Prevention of privilege escalation
- ✅ Comprehensive audit trail

**अब आपका system fully hierarchical access control के साथ secure है!** 🚀
