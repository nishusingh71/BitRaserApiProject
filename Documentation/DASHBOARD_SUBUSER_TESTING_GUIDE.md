# Dashboard & Subuser API Testing Guide

## ✅ Fixes Applied Summary

### 1. **Dashboard Controllers Fixed**
- ✅ Removed hard-coded `[Authorize(Roles = "Admin")]`
- ✅ Added dynamic permission-based authorization
- ✅ Fixed JWT token claims (added `ClaimTypes.NameIdentifier` and `ClaimTypes.Name`)
- ✅ Fixed `User.Identity?.Name` to `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- ✅ Added proper null checks
- ✅ Enhanced error messages with exception details

### 2. **Subuser Controller Fixed**
- ✅ Improved permission logic - users can manage their own subusers without special permissions
- ✅ Added null checks for authentication
- ✅ Safe role assignment with fallback to default "User" role
- ✅ Better error handling with try-catch blocks
- ✅ Added logging for debugging

### 3. **Build Status**
```
✅ Build Successful
✅ No Compilation Errors
✅ All Controllers Updated
✅ Ready for Testing
```

---

## 🧪 Testing Steps

### Step 1: Test Login & Token Generation

```http
POST http://localhost:5000/api/DashboardAuth/login
Content-Type: application/json

{
  "Email": "test@example.com",
  "Password": "YourPassword"
}
```

**Expected Response:**
```json
{
  "token": "eyJhbGci...",
  "refreshToken": "guid...",
  "user": {
    "id": "1",
    "name": "Test User",
    "email": "test@example.com",
    "role": "SuperAdmin, Admin",
    "timeZone": "UTC",
    "department": "",
    "lastLogin": "2025-01-26T..."
  },
  "expiresAt": "2025-01-27T..."
}
```

**Verify Token Claims:**
- Decode JWT token at https://jwt.io
- Check for these claims:
  - ✅ `nameid` (NameIdentifier)
  - ✅ `unique_name` (Name)
  - ✅ `sub` (Subject)
  - ✅ `email`
  - ✅ `user_type` (user/subuser)
  - ✅ `role` (role names)
  - ✅ `permission` (permission names)

---

### Step 2: Test Dashboard Overview

```http
GET http://localhost:5000/api/AdminDashboard/overview
Authorization: Bearer YOUR_TOKEN_HERE
```

**Expected Response:**
```json
{
  "totalUsers": 10,
  "activeUsers": 5,
  "totalLicenses": 20,
  "usedLicenses": 15,
  "totalMachines": 20,
  "activeMachines": 15,
  "recentActivities": [
    {
      "id": "123",
      "type": "INFO",
      "description": "User login successful",
      "user": "test@example.com",
      "timestamp": "2025-01-26T...",
      "status": "INFO"
    }
  ]
}
```

**Error Cases to Test:**

#### No Token (401):
```http
GET http://localhost:5000/api/AdminDashboard/overview
```
**Expected**: `401 Unauthorized - User not authenticated`

#### Insufficient Permissions (403):
```http
GET http://localhost:5000/api/AdminDashboard/overview
Authorization: Bearer <token_without_VIEW_ORGANIZATION_HIERARCHY_permission>
```
**Expected**: `403 Forbidden - Insufficient permissions to view dashboard`

---

### Step 3: Test Subuser Management

#### Get All Subusers (Own Subusers)
```http
GET http://localhost:5000/api/EnhancedSubuser
Authorization: Bearer YOUR_TOKEN_HERE
```

**Expected for Regular User:**
```json
[
  {
    "subuser_email": "subuser1@example.com",
    "user_email": "test@example.com",
    "subuser_id": 1,
    "roles": ["User"],
    "hasPassword": true
  }
]
```

**Expected for Subuser (no permissions):**
```json
[]
```

**Expected for Admin:**
```json
[
  {
    "subuser_email": "subuser1@example.com",
    "user_email": "user1@example.com",
    "subuser_id": 1,
    "roles": ["User"],
    "hasPassword": true
  },
  {
    "subuser_email": "subuser2@example.com",
    "user_email": "user2@example.com",
    "subuser_id": 2,
    "roles": ["Manager"],
    "hasPassword": true
  }
]
```

---

#### Create Subuser
```http
POST http://localhost:5000/api/EnhancedSubuser
Authorization: Bearer YOUR_TOKEN_HERE
Content-Type: application/json

{
  "SubuserEmail": "newsubuser@example.com",
  "SubuserPassword": "SecurePass@123",
  "DefaultRole": "User"
}
```

**Expected Response:**
```json
{
  "subuserEmail": "newsubuser@example.com",
  "parentUserEmail": "test@example.com",
  "subuserID": 5,
  "message": "Subuser created successfully"
}
```

**Error Cases:**

##### Duplicate Email (409):
```json
{
  "error": "Subuser with email newsubuser@example.com already exists"
}
```

##### Email Used as Main User (409):
```json
{
  "error": "Email newsubuser@example.com is already used as a main user account"
}
```

##### Subuser Creating Subuser (403):
```http
# Login as subuser first
POST http://localhost:5000/api/DashboardAuth/login
{
  "Email": "subuser@example.com",
  "Password": "Pass@123"
}

# Try to create subuser with subuser token
POST http://localhost:5000/api/EnhancedSubuser
Authorization: Bearer <subuser_token>
{
  "SubuserEmail": "another@example.com",
  "SubuserPassword": "Pass@123"
}
```
**Expected**: `403 Forbidden - Subusers cannot create subusers`

---

#### Get Subuser by Email
```http
GET http://localhost:5000/api/EnhancedSubuser/subuser1@example.com
Authorization: Bearer YOUR_TOKEN_HERE
```

**Expected Response:**
```json
{
  "subuser_email": "subuser1@example.com",
  "user_email": "test@example.com",
  "subuser_id": 1,
  "roles": [
    {
      "roleName": "User",
      "description": "Basic user access",
      "assignedAt": "2025-01-25T...",
      "assignedByEmail": "test@example.com"
    }
  ],
  "permissions": [
    "ViewOnly",
    "VIEW_PROFILE",
    "UPDATE_PROFILE"
  ],
  "hasPassword": true
}
```

---

#### Update Subuser
```http
PUT http://localhost:5000/api/EnhancedSubuser/subuser1@example.com
Authorization: Bearer YOUR_TOKEN_HERE
Content-Type: application/json

{
  "SubuserEmail": "subuser1@example.com",
  "NewPassword": "NewSecurePass@456"
}
```

**Expected Response:**
```json
{
  "message": "Subuser updated successfully",
  "subuserEmail": "subuser1@example.com",
  "updatedAt": "2025-01-26T..."
}
```

---

#### Assign Role to Subuser
```http
POST http://localhost:5000/api/EnhancedSubuser/subuser1@example.com/assign-role
Authorization: Bearer YOUR_TOKEN_HERE
Content-Type: application/json

{
  "RoleName": "Manager"
}
```

**Expected Response:**
```json
{
  "message": "Role Manager assigned to subuser subuser1@example.com",
  "subuserEmail": "subuser1@example.com",
  "roleName": "Manager",
  "assignedBy": "test@example.com",
  "assignedAt": "2025-01-26T..."
}
```

**Note**: If "Manager" role doesn't exist, system automatically falls back to "User" role!

---

#### Delete Subuser
```http
DELETE http://localhost:5000/api/EnhancedSubuser/subuser1@example.com
Authorization: Bearer YOUR_TOKEN_HERE
```

**Expected Response:**
```json
{
  "message": "Subuser deleted successfully",
  "subuserEmail": "subuser1@example.com",
  "deletedAt": "2025-01-26T..."
}
```

---

### Step 4: Test Dashboard Users

#### Get All Users
```http
GET http://localhost:5000/api/DashboardUsers?page=1&pageSize=10
Authorization: Bearer YOUR_TOKEN_HERE
```

**Expected Response:**
```json
{
  "items": [
    {
      "id": "1",
      "name": "Test User",
      "email": "test@example.com",
      "department": "",
      "role": "User",
      "status": "Active",
      "lastLogin": "2025-01-26T...",
      "createdAt": "2025-01-20T...",
      "groups": [],
      "licenseCount": 0
    }
  ],
  "totalCount": 10,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

### Step 5: Test Dashboard Licenses

```http
GET http://localhost:5000/api/DashboardLicenses
Authorization: Bearer YOUR_TOKEN_HERE
```

**Expected Response:**
```json
[
  {
    "id": "abc123xyz",
    "type": "Paid",
    "assignedTo": "test@example.com",
    "status": "Active",
    "expiryDate": "2026-01-26T...",
    "createdAt": "2025-01-01T...",
    "features": "{\"feature1\": true}"
  }
]
```

---

## 🐛 Common Errors & Solutions

### Error 401: "User not authenticated"
**Cause**: Missing or invalid token  
**Solution**: 
1. Check if Authorization header is present
2. Verify token is not expired
3. Re-login to get new token

### Error 403: "Insufficient permissions"
**Cause**: User doesn't have required permission  
**Solution**:
1. Check user's roles: `SELECT * FROM UserRoles WHERE UserId = X`
2. Check role permissions: `SELECT * FROM RolePermissions WHERE RoleId = X`
3. Assign required role or permission

### Error 500: "Error retrieving data"
**Cause**: Backend exception  
**Solution**:
1. Check logs for detailed error
2. Verify database connectivity
3. Check if required tables/data exist

---

## 📊 Debugging Commands

### Check Token Claims
```bash
# Decode JWT token
echo "YOUR_TOKEN" | base64 -d
```

### Check User Permissions in Database
```sql
-- Get user's roles
SELECT u.user_email, r.RoleName
FROM users u
JOIN UserRoles ur ON u.user_id = ur.UserId
JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.user_email = 'test@example.com';

-- Get role permissions
SELECT r.RoleName, p.PermissionName
FROM Roles r
JOIN RolePermissions rp ON r.RoleId = rp.RoleId
JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE r.RoleName IN ('SuperAdmin', 'Admin', 'Manager');

-- Get subuser roles
SELECT s.subuser_email, r.RoleName
FROM subuser s
JOIN SubuserRoles sr ON s.subuser_id = sr.SubuserId
JOIN Roles r ON sr.RoleId = r.RoleId;
```

### Check Logs
```sql
SELECT * FROM logs 
WHERE user_email = 'test@example.com' 
ORDER BY created_at DESC 
LIMIT 50;
```

---

## ✅ Success Criteria

- [ ] Login returns valid JWT token with all required claims
- [ ] Dashboard overview accessible with valid token
- [ ] Regular users can view their own subusers
- [ ] Admins can view all subusers
- [ ] Users can create subusers without special permissions
- [ ] Subusers cannot create other subusers
- [ ] Role assignment works with fallback to default role
- [ ] Proper error messages for 401, 403, 500 errors
- [ ] All CRUD operations work for authorized users
- [ ] Unauthorized access returns appropriate error codes

---

## 🎯 Next Steps

1. ✅ Test all endpoints with Postman/Swagger
2. ✅ Verify permissions in database
3. ✅ Check logs for any errors
4. ✅ Test with different user roles
5. ✅ Test error scenarios

---

**Last Updated**: 2025-01-26  
**Status**: ✅ Ready for Testing  
**Build**: ✅ Successful
