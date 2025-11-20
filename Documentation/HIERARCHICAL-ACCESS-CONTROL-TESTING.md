# 🧪 Hierarchical Access Control - API Testing Guide

## Quick Test Scenarios

### Setup: Create Test Users

```bash
# 1. Create first user (auto SuperAdmin)
POST http://localhost:4000/api/Users
Content-Type: application/json

{
  "user_name": "Super Admin",
  "user_email": "superadmin@test.com",
  "user_password": "SuperAdmin@123",
  "phone_number": "+1234567890"
}

# 2. Login as SuperAdmin
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "superadmin@test.com",
  "password": "SuperAdmin@123"
}
# Save the token from response

# 3. Create Admin user
POST http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "UserEmail": "admin@test.com",
  "UserName": "Admin User",
  "Password": "Admin@123",
  "DefaultRole": "Admin"
}

# 4. Create Manager user
POST http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "UserEmail": "manager@test.com",
  "UserName": "Manager User",
  "Password": "Manager@123",
  "DefaultRole": "Manager"
}

# 5. Create User role user
POST http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "UserEmail": "user@test.com",
  "UserName": "Regular User",
  "Password": "User@123",
  "DefaultRole": "User"
}
```

---

## Test 1: Admin Cannot Create SuperAdmin

### Expected: ❌ 403 Forbidden

```bash
# 1. Login as Admin
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "admin@test.com",
  "password": "Admin@123"
}
# Save admin token

# 2. Try to create SuperAdmin user
POST http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "UserEmail": "superadmin2@test.com",
  "UserName": "Second SuperAdmin",
  "Password": "SuperAdmin@123",
  "DefaultRole": "SuperAdmin"
}
```

**Expected Response:**
```json
{
  "success": false,
  "message": "You cannot create user with role 'SuperAdmin'",
  "detail": "You can only assign roles with lower privilege than your own. Admins cannot create SuperAdmin users."
}
```

---

## Test 2: Admin CAN Create Manager

### Expected: ✅ 201 Created

```bash
# Using admin token from Test 1
POST http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "UserEmail": "manager2@test.com",
  "UserName": "Second Manager",
  "Password": "Manager@123",
  "DefaultRole": "Manager"
}
```

**Expected Response:**
```json
{
  "userEmail": "manager2@test.com",
  "userName": "Second Manager",
  "assignedRole": "Manager",
  "roleAssignedToRBAC": true,
  "message": "User created successfully with Manager role assigned to RBAC"
}
```

---

## Test 3: User Role Cannot Create Subusers

### Expected: ❌ 403 Forbidden

```bash
# 1. Login as User
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "user@test.com",
  "password": "User@123"
}
# Save user token

# 2. Try to create subuser
POST http://localhost:4000/api/RoleBasedAuth/create-subuser
Authorization: Bearer {user_token}
Content-Type: application/json

{
  "SubuserEmail": "subuser@test.com",
  "SubuserPassword": "Subuser@123",
  "RoleIds": [6]
}
```

**Expected Response:**
```json
{
  "success": false,
  "message": "You cannot create subusers",
  "detail": "Users with 'User' role are not allowed to create subusers"
}
```

---

## Test 4: Manager CAN Create Subusers

### Expected: ✅ 200 OK

```bash
# 1. Login as Manager
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "manager@test.com",
  "password": "Manager@123"
}
# Save manager token

# 2. Create subuser with Support role
POST http://localhost:4000/api/EnhancedSubusers
Authorization: Bearer {manager_token}
Content-Type: application/json

{
  "Email": "support@test.com",
  "Password": "Support@123",
  "Name": "Support User",
  "Role": "Support"
}
```

**Expected Response:**
```json
{
  "subuser_id": 1,
  "subuser_email": "support@test.com",
  "name": "Support User",
  "role": "Support",
  "message": "Subuser created successfully"
}
```

---

## Test 5: Manager Cannot Assign Admin Role to Subuser

### Expected: ❌ 403 Forbidden

```bash
# Using manager token
POST http://localhost:4000/api/EnhancedSubusers
Authorization: Bearer {manager_token}
Content-Type: application/json

{
  "Email": "admin-subuser@test.com",
  "Password": "Admin@123",
  "Name": "Admin Subuser",
  "Role": "Admin"
}
```

**Expected Response:**
```json
{
  "success": false,
  "message": "You cannot assign role 'Admin' to subuser",
  "detail": "You can only assign roles with lower privilege than your own"
}
```

---

## Test 6: View Users - Hierarchical Filtering

### Test 6a: SuperAdmin Sees All Users

```bash
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "superadmin@test.com",
  "password": "SuperAdmin@123"
}
# Save superadmin token

GET http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {superadmin_token}
```

**Expected Response:** All users including SuperAdmin, Admin, Manager, etc.

---

### Test 6b: Admin Does NOT See SuperAdmin Users

```bash
GET http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {admin_token}
```

**Expected Response:** Manager, Support, User (NO SuperAdmin users)

---

### Test 6c: Manager Sees Limited Users

```bash
GET http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {manager_token}
```

**Expected Response:** Support, User (NO SuperAdmin, Admin, or Manager users)

---

### Test 6d: User Sees Only Own Profile

```bash
GET http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {user_token}
```

**Expected Response:** Only own profile (user@test.com)

---

## Test 7: View Subusers - Hierarchical Filtering

### Test 7a: Manager Sees Own Subusers

```bash
GET http://localhost:4000/api/EnhancedSubusers
Authorization: Bearer {manager_token}
```

**Expected Response:** Only subusers created by manager@test.com

---

### Test 7b: Admin Sees All Manageable Subusers

```bash
GET http://localhost:4000/api/EnhancedSubusers
Authorization: Bearer {admin_token}
```

**Expected Response:** Subusers belonging to Manager, Support, User (not SuperAdmin's subusers)

---

## Test 8: Assign Role - Hierarchy Validation

### Test 8a: Admin Cannot Assign SuperAdmin Role

```bash
# Get user ID first
GET http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {admin_token}

# Try to assign SuperAdmin role to Manager
POST http://localhost:4000/api/RoleBasedAuth/assign-role
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "UserId": 3,  # manager user ID
  "RoleId": 1   # SuperAdmin role ID
}
```

**Expected Response:**
```json
{
  "message": "You cannot assign role 'SuperAdmin'",
  "detail": "You can only assign roles with lower privilege than your own"
}
```

---

### Test 8b: Admin CAN Assign Support Role

```bash
POST http://localhost:4000/api/RoleBasedAuth/assign-role
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "UserId": 4,  # user ID
  "RoleId": 4   # Support role ID
}
```

**Expected Response:**
```json
{
  "message": "Role assigned successfully"
}
```

---

## Test 9: Same-Level User Management Restriction

### Expected: ❌ 403 Forbidden

```bash
# 1. Create second admin user (using superadmin)
POST http://localhost:4000/api/EnhancedUsers
Authorization: Bearer {superadmin_token}
Content-Type: application/json

{
  "UserEmail": "admin2@test.com",
  "UserName": "Second Admin",
  "Password": "Admin@123",
  "DefaultRole": "Admin"
}

# 2. Login as first admin
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "admin@test.com",
  "password": "Admin@123"
}

# 3. Try to update second admin (same level)
PUT http://localhost:4000/api/EnhancedUsers/admin2@test.com
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "UserEmail": "admin2@test.com",
  "UserName": "Updated Admin Name"
}
```

**Expected Response:**
```json
{
  "error": "You can only update your own profile or profiles you manage"
}
```

---

## Test 10: Update Subuser - Parent Validation

### Test 10a: Manager CAN Update Own Subuser

```bash
PATCH http://localhost:4000/api/EnhancedSubusers/support@test.com
Authorization: Bearer {manager_token}
Content-Type: application/json

{
  "Name": "Updated Support Name",
  "Department": "IT"
}
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Subuser updated successfully",
  "subuser_email": "support@test.com"
}
```

---

### Test 10b: Admin Cannot Update Other Manager's Subuser

**Note:** This should be tested if strict parent-child relationship is enforced

```bash
# Create subuser under second manager
POST http://localhost:4000/api/EnhancedSubusers
Authorization: Bearer {manager2_token}
Content-Type: application/json

{
  "Email": "manager2-subuser@test.com",
  "Password": "Subuser@123",
  "Name": "Manager 2 Subuser"
}

# Try to update it using first manager token
PATCH http://localhost:4000/api/EnhancedSubusers/manager2-subuser@test.com
Authorization: Bearer {manager_token}
Content-Type: application/json

{
  "Name": "Hacked Name"
}
```

**Expected Response:**
```json
{
  "success": false,
  "error": "You can only update your own subusers"
}
```

---

## Summary of Expected Results

| Test | Scenario | Expected Result |
|------|----------|----------------|
| 1 | Admin creates SuperAdmin | ❌ 403 Forbidden |
| 2 | Admin creates Manager | ✅ 201 Created |
| 3 | User creates Subuser | ❌ 403 Forbidden |
| 4 | Manager creates Subuser | ✅ 200 OK |
| 5 | Manager assigns Admin role | ❌ 403 Forbidden |
| 6a | SuperAdmin views users | ✅ All users |
| 6b | Admin views users | ✅ Manager, Support, User (NO SuperAdmin) |
| 6c | Manager views users | ✅ Support, User only |
| 6d | User views users | ✅ Own profile only |
| 7 | View subusers filtered | ✅ Hierarchical filtering |
| 8a | Admin assigns SuperAdmin role | ❌ 403 Forbidden |
| 8b | Admin assigns Support role | ✅ 200 OK |
| 9 | Admin updates another Admin | ❌ 403 Forbidden |
| 10 | Subuser management | ✅ Parent validation working |

---

## Running All Tests

### Using Swagger UI
1. Navigate to `http://localhost:4000/swagger`
2. Execute each test in order
3. Verify expected responses

### Using Postman
1. Import the collection (create from above endpoints)
2. Set up environment variables for tokens
3. Run collection with Postman Runner

### Using curl or HTTP files
1. Copy each test to `.http` file
2. Use VS Code REST Client extension
3. Execute tests sequentially

---

## Troubleshooting

### Common Issues

**Issue:** All tests return 401 Unauthorized
- **Solution:** Check if JWT token is valid and not expired
- **Fix:** Re-login and get fresh token

**Issue:** Tests pass but shouldn't
- **Solution:** Check role hierarchy levels in database
- **Fix:** Run database seeding script

**Issue:** SuperAdmin cannot be created
- **Solution:** Check if roles table is populated
- **Fix:** Run database initializer

---

## Database Validation Queries

```sql
-- Check role hierarchy
SELECT RoleId, RoleName, HierarchyLevel 
FROM Roles 
ORDER BY HierarchyLevel;

-- Check user roles
SELECT u.user_email, r.RoleName, r.HierarchyLevel
FROM Users u
JOIN UserRoles ur ON u.user_id = ur.UserId
JOIN Roles r ON ur.RoleId = r.RoleId
ORDER BY r.HierarchyLevel;

-- Check subuser roles
SELECT s.subuser_email, s.user_email, r.RoleName
FROM subuser s
JOIN SubuserRoles sr ON s.subuser_id = sr.SubuserId
JOIN Roles r ON sr.RoleId = r.RoleId;
```

---

**Testing Complete! ✅**

All tests should validate the hierarchical access control system is working correctly.
