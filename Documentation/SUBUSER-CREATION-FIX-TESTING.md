# 🧪 Subuser Creation Fix - Quick Testing Guide

## ✅ Fix Summary

**Issue:** Subusers with Manager/Support roles could NOT create subusers  
**Root Cause:** `CanCreateSubusersAsync()` always checked User table instead of Subuser table  
**Solution:** Auto-detect user type and check correct table for roles

---

## 🚀 Quick Test Cases

### **Test 1: Subuser with Manager Role ✅ (SHOULD WORK)**

```bash
# Login as Manager Subuser
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "manager-subuser@company.com",
  "password": "Manager@123"
}

# Expected Response:
{
  "token": "eyJ...",
  "roles": ["Manager"],
  "permissions": ["CREATE_SUBUSER", "ReportAccess", "MachineManagement"]
}

# Create Subuser
POST http://localhost:4000/api/EnhancedSubusers
Authorization: Bearer {token_from_above}
Content-Type: application/json

{
  "Email": "test-subuser@company.com",
  "Password": "Test@123",
  "Name": "Test User",
  "Role": "Support"
}

# Expected Response: ✅ SUCCESS
{
  "subuser_id": 10,
  "subuser_email": "test-subuser@company.com",
  "message": "Subuser created successfully"
}
```

---

### **Test 2: Subuser with User Role ❌ (SHOULD FAIL)**

```bash
# Login as User Role Subuser
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "basic-subuser@company.com",
  "password": "User@123"
}

# Expected Response:
{
  "token": "eyJ...",
  "roles": ["User"],
  "permissions": ["ViewOnly"]
}

# Try to Create Subuser
POST http://localhost:4000/api/EnhancedSubusers
Authorization: Bearer {token_from_above}
Content-Type: application/json

{
  "Email": "test@company.com",
  "Password": "Test@123",
  "Name": "Test"
}

# Expected Response: ❌ 403 FORBIDDEN
{
  "success": false,
  "message": "You cannot create subusers",
  "detail": "Users with 'User' role are not allowed to create subusers"
}
```

---

### **Test 3: Regular User with Manager Role ✅ (SHOULD WORK)**

```bash
# Login as Manager User
POST http://localhost:4000/api/RoleBasedAuth/login
{
  "email": "manager@company.com",
  "password": "Manager@123"
}

# Create Subuser
POST http://localhost:4000/api/EnhancedSubusers
Authorization: Bearer {manager_token}
{
  "Email": "manager-new-subuser@company.com",
  "Password": "Test@123",
  "Name": "Manager's Subuser"
}

# Expected Response: ✅ SUCCESS
{
  "message": "Subuser created successfully"
}
```

---

## 📋 Testing Checklist

### **Setup (One-time)**

- [ ] Database has CREATE_SUBUSER permission (PermissionId = 32)
- [ ] Manager role has CREATE_SUBUSER permission in RolePermissions table
- [ ] Support role has CREATE_SUBUSER permission in RolePermissions table
- [ ] User role does NOT have CREATE_SUBUSER permission

**Verify with SQL:**
```sql
-- Check permission exists
SELECT * FROM Permissions WHERE PermissionName = 'CREATE_SUBUSER';
-- Expected: PermissionId = 32

-- Check role-permission mappings
SELECT r.RoleName, p.PermissionName
FROM RolePermissions rp
JOIN Roles r ON rp.RoleId = r.RoleId
JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE p.PermissionName = 'CREATE_SUBUSER';
-- Expected: Admin, Manager, Support
```

---

### **Test Scenarios**

| # | User Type | Role | Action | Expected Result | Status |
|---|-----------|------|--------|----------------|--------|
| 1 | User | SuperAdmin | Create Subuser | ✅ Success | |
| 2 | User | Admin | Create Subuser | ✅ Success | |
| 3 | User | Manager | Create Subuser | ✅ Success | |
| 4 | User | Support | Create Subuser | ✅ Success | |
| 5 | User | User | Create Subuser | ❌ 403 Forbidden | |
| 6 | Subuser | SuperAdmin | Create Subuser | ✅ Success | |
| 7 | Subuser | Admin | Create Subuser | ✅ Success | |
| 8 | Subuser | Manager | Create Subuser | ✅ Success | ✅ **FIXED!** |
| 9 | Subuser | Support | Create Subuser | ✅ Success | ✅ **FIXED!** |
| 10 | Subuser | User | Create Subuser | ❌ 403 Forbidden | ✅ **FIXED!** |

---

## 🔍 Debugging Steps

### **If Test Fails:**

#### **1. Check User's Permissions**

```bash
GET http://localhost:4000/api/RoleBasedAuth/my-permissions
Authorization: Bearer {user_token}
```

Expected response should include `CREATE_SUBUSER`:
```json
{
  "permissions": [
    "CREATE_SUBUSER",
    "ReportAccess",
    "MachineManagement"
  ],
  "roles": ["Manager"],
  "userType": "subuser"
}
```

#### **2. Check Database**

```sql
-- For Subuser
SELECT s.subuser_email, r.RoleName, p.PermissionName
FROM subuser s
JOIN SubuserRoles sr ON s.subuser_id = sr.SubuserId
JOIN Roles r ON sr.RoleId = r.RoleId
JOIN RolePermissions rp ON r.RoleId = rp.RoleId
JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE s.subuser_email = 'manager-subuser@company.com';

-- Expected to see CREATE_SUBUSER permission
```

#### **3. Check Code Flow**

Add breakpoints/logs in:
- `EnhancedSubusersController.CreateSubuser()` - Line: `CanCreateSubusersAsync()`
- `RoleBasedAuthService.CanCreateSubusersAsync()` - Check `isSubuser` value
- `RoleBasedAuthService.GetUserRolesAsync()` - Verify roles returned

---

## 🎯 Success Criteria

✅ **All 10 test scenarios pass**  
✅ **Subusers with Manager/Support roles can create subusers**  
✅ **Subusers with User role correctly blocked**  
✅ **Users with Manager/Support roles still work**  
✅ **Users with User role correctly blocked**

---

## 📊 Quick Validation Commands

### **Swagger UI Test:**

1. Navigate to `http://localhost:4000/swagger`
2. Authorize with Manager Subuser token
3. Try `POST /api/EnhancedSubusers` endpoint
4. Expected: **200 OK** with created subuser

### **Postman Collection:**

Import this JSON:
```json
{
  "info": { "name": "Subuser Creation Fix Tests" },
  "item": [
    {
      "name": "Manager Subuser Creates Subuser",
      "request": {
     "method": "POST",
        "url": "{{baseUrl}}/api/EnhancedSubusers",
        "header": [{"key": "Authorization", "value": "Bearer {{manager_subuser_token}}"}],
        "body": {
          "mode": "raw",
     "raw": "{\"Email\":\"test@company.com\",\"Password\":\"Test@123\",\"Name\":\"Test User\"}"
        }
    }
    },
    {
      "name": "User Role Subuser Tries to Create (Should Fail)",
   "request": {
        "method": "POST",
   "url": "{{baseUrl}}/api/EnhancedSubusers",
        "header": [{"key": "Authorization", "value": "Bearer {{user_subuser_token}}"}],
    "body": {
   "mode": "raw",
  "raw": "{\"Email\":\"test@company.com\",\"Password\":\"Test@123\",\"Name\":\"Test\"}"
        }
      }
    }
  ]
}
```

---

## 🚨 Troubleshooting

### **Issue: Still getting "Subusers cannot create subusers"**

**Check:**
1. Code changes applied correctly?
2. Application restarted?
3. `isSubuser` parameter being set correctly?

**Fix:**
```bash
# Restart application
dotnet build
dotnet run

# Verify code change
git diff BitRaserApiProject/Services/RoleBasedAuthService.cs
```

---

### **Issue: "You cannot create subusers" for Manager Subuser**

**Check:**
1. Database has CREATE_SUBUSER permission mapped to Manager role?
2. Subuser has Manager role assigned in SubuserRoles table?

**Fix:**
```sql
-- Add permission to Manager role
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 3, 32
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermissions
    WHERE RoleId = 3 AND PermissionId = 32
);

-- Check subuser's role
SELECT * FROM SubuserRoles WHERE SubuserId = (
    SELECT subuser_id FROM subuser WHERE subuser_email = 'manager-subuser@company.com'
);
```

---

## ✅ Final Checklist

Before marking as complete:

- [ ] All 10 test scenarios pass
- [ ] Code changes deployed
- [ ] Database seeded with permissions
- [ ] Documentation updated
- [ ] Team notified

---

**Testing Complete!** 🎉

Subusers अब अपने role के according subusers create कर सकते हैं!
