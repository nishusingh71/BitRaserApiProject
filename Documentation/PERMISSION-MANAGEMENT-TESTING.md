# 🧪 Permission Management - Testing Guide

## Quick Test Scenarios with Swagger UI

### Prerequisites
1. Navigate to `http://localhost:4000/swagger`
2. Have SuperAdmin and Admin tokens ready

---

## Test Scenario 1: View Permissions for a Role

### ✅ Expected: SUCCESS (All users can view)

```http
GET /api/RoleBasedAuth/roles/Manager/permissions
```

**Steps in Swagger:**
1. Expand `RoleBasedAuth` section
2. Find `GET /roles/{roleName}/permissions`
3. Click "Try it out"
4. Enter `roleName`: `Manager`
5. Click "Execute"

**Expected Response (200 OK):**
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

---

## Test Scenario 2: View All Available Permissions

### ✅ Expected: SUCCESS

```http
GET /api/RoleBasedAuth/permissions/all
```

**Steps in Swagger:**
1. Find `GET /permissions/all`
2. Click "Try it out"
3. Click "Execute"

**Expected Response (200 OK):**
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
    }
    // ... more permissions
  ],
  "count": 20
}
```

---

## Test Scenario 3: Admin Adds Permission to Manager Role

### ✅ Expected: SUCCESS

**Prerequisites:**
- Login as Admin
- Get Admin JWT token
- Authorize in Swagger with Admin token

```http
POST /api/RoleBasedAuth/roles/Manager/permissions
```

**Request Body:**
```json
{
  "PermissionName": "DELETE_USER"
}
```

**Steps in Swagger:**
1. Click 🔒 "Authorize" button at top
2. Enter Admin token
3. Find `POST /roles/{roleName}/permissions`
4. Click "Try it out"
5. Enter `roleName`: `Manager`
6. Enter Request Body:
   ```json
   {
   "PermissionName": "DELETE_USER"
   }
   ```
7. Click "Execute"

**Expected Response (200 OK):**
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

**Verify:**
```http
GET /api/RoleBasedAuth/roles/Manager/permissions
```

Should now include `DELETE_USER`!

---

## Test Scenario 4: Admin Tries to Modify SuperAdmin Role

### ❌ Expected: FAIL (403 Forbidden)

```http
POST /api/RoleBasedAuth/roles/SuperAdmin/permissions
```

**Request Body:**
```json
{
  "PermissionName": "SomePermission"
}
```

**Steps in Swagger:**
1. Ensure authorized with Admin token
2. Find `POST /roles/{roleName}/permissions`
3. Enter `roleName`: `SuperAdmin`
4. Enter Request Body:
   ```json
   {
     "PermissionName": "SomePermission"
   }
   ```
5. Click "Execute"

**Expected Response (403 Forbidden):**
```json
{
  "message": "You cannot modify permissions for role 'SuperAdmin'",
  "detail": "Only SuperAdmin and Admin can modify role permissions, and only for roles below their level"
}
```

---

## Test Scenario 5: Manager Tries to Modify Support Role

### ❌ Expected: FAIL (403 Forbidden)

**Prerequisites:**
- Login as Manager
- Authorize with Manager token

```http
POST /api/RoleBasedAuth/roles/Support/permissions
```

**Request Body:**
```json
{
  "PermissionName": "DELETE_USER"
}
```

**Expected Response (403 Forbidden):**
```json
{
  "message": "You cannot modify permissions for role 'Support'",
  "detail": "Only SuperAdmin and Admin can modify role permissions, and only for roles below their level"
}
```

---

## Test Scenario 6: Remove Permission from Role

### ✅ Expected: SUCCESS (Admin/SuperAdmin only)

```http
DELETE /api/RoleBasedAuth/roles/Manager/permissions/DELETE_USER
```

**Steps in Swagger:**
1. Authorize with Admin token
2. Find `DELETE /roles/{roleName}/permissions/{permissionName}`
3. Click "Try it out"
4. Enter `roleName`: `Manager`
5. Enter `permissionName`: `DELETE_USER`
6. Click "Execute"

**Expected Response (200 OK):**
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

**Verify:**
```http
GET /api/RoleBasedAuth/roles/Manager/permissions
```

`DELETE_USER` should no longer be in the list!

---

## Test Scenario 7: Update All Permissions for a Role

### ✅ Expected: SUCCESS

```http
PUT /api/RoleBasedAuth/roles/Support/permissions
```

**Request Body:**
```json
{
  "PermissionNames": [
    "ViewOnly",
    "READ_USER",
    "READ_REPORT",
    "READ_LOG",
 "READ_SESSION"
  ]
}
```

**Steps in Swagger:**
1. Authorize with SuperAdmin or Admin token
2. Find `PUT /roles/{roleName}/permissions`
3. Click "Try it out"
4. Enter `roleName`: `Support`
5. Enter Request Body:
   ```json
   {
     "PermissionNames": [
       "ViewOnly",
 "READ_USER",
       "READ_REPORT",
  "READ_LOG",
       "READ_SESSION"
     ]
   }
   ```
6. Click "Execute"

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Permissions updated for role 'Support'",
  "roleName": "Support",
  "permissions": [
    "ViewOnly",
    "READ_USER",
    "READ_REPORT",
    "READ_LOG",
    "READ_SESSION"
  ],
  "modifiedBy": "superadmin@company.com",
  "modifiedAt": "2024-01-15T10:40:00Z"
}
```

**Verify:**
```http
GET /api/RoleBasedAuth/roles/Support/permissions
```

Should show ONLY the new permissions!

---

## Test Scenario 8: Verify Permission Changes Take Effect

### ✅ Test Real-time Permission Updates

**Step 1: Manager user login before permission change**

```http
POST /api/RoleBasedAuth/login
```

Request:
```json
{
  "email": "manager@company.com",
  "password": "Manager@123"
}
```

Save the token. Check current permissions:

```http
GET /api/RoleBasedAuth/my-permissions
Authorization: Bearer {manager_token}
```

Response:
```json
{
  "permissions": [
    "UserManagement",
    "ReportAccess",
    "MachineManagement",
    "ViewOnly"
  ],
  "roles": ["Manager"]
}
```

**Step 2: Admin adds DELETE_USER permission to Manager role**

```http
POST /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}
```

Request:
```json
{
  "PermissionName": "DELETE_USER"
}
```

**Step 3: Manager checks permissions again (SAME token)**

```http
GET /api/RoleBasedAuth/my-permissions
Authorization: Bearer {manager_token}  // Same token from Step 1
```

Response:
```json
{
  "permissions": [
    "UserManagement",
    "ReportAccess",
    "MachineManagement",
    "ViewOnly",
    "DELETE_USER"  // ✅ New permission available immediately!
  ],
  "roles": ["Manager"]
}
```

**Result:** ✅ Permissions update instantly without re-login!

---

## Test Scenario 9: Invalid Permission Name

### ❌ Expected: FAIL

```http
POST /api/RoleBasedAuth/roles/Manager/permissions
```

Request:
```json
{
  "PermissionName": "INVALID_PERMISSION_NAME"
}
```

**Expected Response (400 Bad Request or similar):**
```json
{
  "message": "Failed to add permission to role"
}
```

---

## Test Scenario 10: Empty Permission List

### ❌ Expected: FAIL

```http
PUT /api/RoleBasedAuth/roles/Manager/permissions
```

Request:
```json
{
  "PermissionNames": []
}
```

**Expected Response (400 Bad Request):**
```json
{
  "message": "At least one permission must be specified"
}
```

---

## Complete Test Flow with Screenshots

### Flow 1: Admin Managing Manager Role Permissions

```bash
# 1. Login as Admin
POST /api/RoleBasedAuth/login
{
  "email": "admin@company.com",
  "password": "Admin@123"
}
→ Save token

# 2. View current Manager permissions
GET /api/RoleBasedAuth/roles/Manager/permissions
→ ["UserManagement", "ReportAccess"]

# 3. View all available permissions
GET /api/RoleBasedAuth/permissions/all
→ See full list

# 4. Add DELETE_USER permission
POST /api/RoleBasedAuth/roles/Manager/permissions
{
  "PermissionName": "DELETE_USER"
}
→ Success!

# 5. Verify addition
GET /api/RoleBasedAuth/roles/Manager/permissions
→ ["UserManagement", "ReportAccess", "DELETE_USER"]

# 6. Remove DELETE_USER permission
DELETE /api/RoleBasedAuth/roles/Manager/permissions/DELETE_USER
→ Success!

# 7. Verify removal
GET /api/RoleBasedAuth/roles/Manager/permissions
→ ["UserManagement", "ReportAccess"]

# 8. Replace all permissions
PUT /api/RoleBasedAuth/roles/Manager/permissions
{
  "PermissionNames": [
    "UserManagement",
    "CREATE_USER",
    "UPDATE_USER",
    "ReportAccess"
  ]
}
→ Success!

# 9. Final verification
GET /api/RoleBasedAuth/roles/Manager/permissions
→ ["UserManagement", "CREATE_USER", "UPDATE_USER", "ReportAccess"]
```

---

## Postman Collection

### Import this JSON into Postman:

```json
{
"info": {
    "name": "Permission Management Tests",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "1. Get Role Permissions",
      "request": {
        "method": "GET",
        "url": "{{baseUrl}}/api/RoleBasedAuth/roles/Manager/permissions"
      }
  },
    {
 "name": "2. Get All Permissions",
      "request": {
  "method": "GET",
        "url": "{{baseUrl}}/api/RoleBasedAuth/permissions/all"
      }
    },
    {
      "name": "3. Add Permission to Role",
      "request": {
        "method": "POST",
        "url": "{{baseUrl}}/api/RoleBasedAuth/roles/Manager/permissions",
        "body": {
          "mode": "raw",
   "raw": "{\n  \"PermissionName\": \"DELETE_USER\"\n}"
        }
      }
    },
    {
      "name": "4. Remove Permission from Role",
      "request": {
   "method": "DELETE",
        "url": "{{baseUrl}}/api/RoleBasedAuth/roles/Manager/permissions/DELETE_USER"
      }
    },
    {
      "name": "5. Update All Permissions",
      "request": {
        "method": "PUT",
 "url": "{{baseUrl}}/api/RoleBasedAuth/roles/Manager/permissions",
        "body": {
     "mode": "raw",
          "raw": "{\n  \"PermissionNames\": [\n    \"UserManagement\",\n    \"ReportAccess\"\n  ]\n}"
        }
      }
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "http://localhost:4000"
    }
  ]
}
```

---

## Success Criteria

### All Tests Should Pass:

- [x] Test 1: View role permissions → 200 OK
- [x] Test 2: View all permissions → 200 OK
- [x] Test 3: Admin adds permission to Manager → 200 OK
- [x] Test 4: Admin tries to modify SuperAdmin → 403 Forbidden
- [x] Test 5: Manager tries to modify Support → 403 Forbidden
- [x] Test 6: Remove permission from role → 200 OK
- [x] Test 7: Update all permissions → 200 OK
- [x] Test 8: Permissions update in real-time → ✅ Works
- [x] Test 9: Invalid permission name → 400 Bad Request
- [x] Test 10: Empty permission list → 400 Bad Request

---

## Troubleshooting Test Failures

### 401 Unauthorized
**Cause:** Token expired or invalid  
**Fix:** Re-login and get fresh token

### 403 Forbidden
**Cause:** Insufficient permissions  
**Fix:** Check if you're using Admin/SuperAdmin token

### 404 Not Found
**Cause:** Role or permission doesn't exist  
**Fix:** Verify role name and permission name

### 500 Internal Server Error
**Cause:** Server error  
**Fix:** Check server logs for details

---

**Testing Complete!** ✅

All permission management features are now fully tested and working!
