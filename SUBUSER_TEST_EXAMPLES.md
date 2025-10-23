# 🧪 Subuser Creation - Quick Test Examples

## Swagger UI Test Karne Ke Liye

### 1. **Minimum Details (Sirf Email aur Password)**

```json
POST /api/SubuserManagement
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "email": "test1@example.com",
  "password": "Test@12345"
}
```

**Expected Result:**
```json
{
  "id": 1,
  "parentUserId": 2,
  "parentUserName": "Admin User",
  "parentUserEmail": "admin@test.com",
  "subuserUsername": null,
  "name": "test1",  // Auto-generated from email
  "email": "test1@example.com",
  "phone": null,
  "jobTitle": null,
  "department": null,
  "role": "subuser",  // Default
  "accessLevel": "limited",  // Default
  "maxMachines": 5,  // Default
  "groupId": null,
  "status": "active",
  "isEmailVerified": false,
  "canCreateSubusers": false,
  "canViewReports": true,
  "canManageMachines": false,
  "canAssignLicenses": false,
  "createdAt": "2024-01-15T10:00:00Z"
}
```

---

### 2. **With Name Only**

```json
POST /api/SubuserManagement

{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass@123"
}
```

**Result:**
- Name: "John Doe" (provided)
- All other fields: defaults

---

### 3. **With Basic Info**

```json
POST /api/SubuserManagement

{
  "name": "Sarah Johnson",
  "email": "sarah@example.com",
  "password": "Sarah@123",
  "phone": "+1234567890",
  "department": "IT"
}
```

**Result:**
- Name: "Sarah Johnson"
- Phone: "+1234567890"
- Department: "IT"
- Other fields: defaults

---

### 4. **With Role Change**

```json
POST /api/SubuserManagement

{
  "name": "Mike Manager",
  "email": "mike@example.com",
  "password": "Mike@123",
  "role": "team_member",
  "accessLevel": "full",
  "maxMachines": 10
}
```

**Result:**
- Role: "team_member" (changed from default)
- AccessLevel: "full" (changed from default)
- MaxMachines: 10 (changed from default)

---

### 5. **Complete Details**

```json
POST /api/SubuserManagement

{
  "subuserUsername": "admin_john",
  "name": "John Admin",
  "email": "john.admin@example.com",
  "password": "JohnAdmin@123",
  "phone": "+1-555-1234",
  "jobTitle": "IT Administrator",
  "department": "Information Technology",
  "role": "team_member",
  "accessLevel": "full",
  "maxMachines": 15,
  "groupId": 1,
  "canCreateSubusers": false,
  "canViewReports": true,
  "canManageMachines": true,
  "canAssignLicenses": true,
  "emailNotifications": true,
  "systemAlerts": true,
  "notes": "Senior IT team member with full access"
}
```

---

## 🧪 Postman/Insomnia Test Collection

### Environment Variables:
```
BASE_URL: http://localhost:4000
TOKEN: <your-jwt-token>
```

### 1. Login First
```http
POST {{BASE_URL}}/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "admin@test.com",
  "password": "Admin@123"
}
```

Save the `token` from response.

---

### 2. Create Minimum Subuser
```http
POST {{BASE_URL}}/api/SubuserManagement
Authorization: Bearer {{TOKEN}}
Content-Type: application/json

{
  "email": "minimal@test.com",
  "password": "Test@12345"
}
```

---

### 3. Create with Name
```http
POST {{BASE_URL}}/api/SubuserManagement
Authorization: Bearer {{TOKEN}}
Content-Type: application/json

{
  "name": "Quick Test",
"email": "quick@test.com",
  "password": "Quick@123"
}
```

---

### 4. Create with Department
```http
POST {{BASE_URL}}/api/SubuserManagement
Authorization: Bearer {{TOKEN}}
Content-Type: application/json

{
  "name": "IT User",
  "email": "it.user@test.com",
  "password": "ITUser@123",
  "department": "IT Department"
}
```

---

### 5. Create Team Member
```http
POST {{BASE_URL}}/api/SubuserManagement
Authorization: Bearer {{TOKEN}}
Content-Type: application/json

{
  "name": "Team Member",
  "email": "team@test.com",
  "password": "Team@123",
  "role": "team_member",
  "accessLevel": "full"
}
```

---

## 🎯 Testing Scenarios

### Scenario 1: Quick Demo
**Goal**: Quickly create 5 test subusers

```bash
# User 1 - Minimal
{
  "email": "demo1@test.com",
  "password": "Demo@12345"
}

# User 2 - With name
{
  "name": "Demo Two",
  "email": "demo2@test.com",
  "password": "Demo@12345"
}

# User 3 - IT dept
{
  "name": "Demo Three",
  "email": "demo3@test.com",
  "password": "Demo@12345",
  "department": "IT"
}

# User 4 - Sales dept
{
  "name": "Demo Four",
  "email": "demo4@test.com",
  "password": "Demo@12345",
  "department": "Sales"
}

# User 5 - Manager
{
  "name": "Demo Manager",
  "email": "demo5@test.com",
  "password": "Demo@12345",
  "role": "team_member",
  "accessLevel": "full"
}
```

---

### Scenario 2: Real Production
**Goal**: Create actual team members

```json
// Developer
{
  "name": "Alice Developer",
  "email": "alice@company.com",
  "password": "AliceDev@123",
  "jobTitle": "Senior Developer",
  "department": "Engineering",
  "role": "team_member",
  "accessLevel": "full",
  "maxMachines": 10,
  "canManageMachines": true
}

// Designer
{
  "name": "Bob Designer",
  "email": "bob@company.com",
  "password": "BobDes@123",
  "jobTitle": "UI/UX Designer",
  "department": "Design",
  "role": "subuser",
  "accessLevel": "limited",
  "maxMachines": 3
}

// Support
{
  "name": "Charlie Support",
  "email": "charlie@company.com",
  "password": "CharlieS@123",
  "jobTitle": "Support Engineer",
  "department": "Customer Support",
  "role": "subuser",
  "accessLevel": "read_only",
  "canViewReports": true
}
```

---

## ✅ Validation Tests

### Test 1: Missing Email
```json
{
  "password": "Test@123"
}
```
**Expected**: ❌ 400 Bad Request - "Email is required"

---

### Test 2: Missing Password
```json
{
  "email": "test@test.com"
}
```
**Expected**: ❌ 400 Bad Request - "Password is required"

---

### Test 3: Invalid Email
```json
{
  "email": "not-an-email",
  "password": "Test@123"
}
```
**Expected**: ❌ 400 Bad Request - "Invalid email format"

---

### Test 4: Short Password
```json
{
  "email": "test@test.com",
  "password": "123"
}
```
**Expected**: ❌ 400 Bad Request - "Password must be at least 8 characters"

---

### Test 5: Duplicate Email
```json
// First user
{
  "email": "duplicate@test.com",
  "password": "Test@123"
}

// Try same email again
{
  "email": "duplicate@test.com",
  "password": "Test@123"
}
```
**Expected**: 
- First: ✅ 201 Created
- Second: ❌ 400 Bad Request - "Email already exists"

---

## 🔍 Verification Tests

### After Creating Subuser:

#### 1. Get by ID
```http
GET {{BASE_URL}}/api/SubuserManagement/1
Authorization: Bearer {{TOKEN}}
```

#### 2. Get All Subusers
```http
GET {{BASE_URL}}/api/SubuserManagement?page=1&pageSize=10
Authorization: Bearer {{TOKEN}}
```

#### 3. Check Defaults
Verify that these fields have correct defaults:
- ✅ role = "subuser"
- ✅ accessLevel = "limited"
- ✅ maxMachines = 5
- ✅ status = "active"
- ✅ canViewReports = true
- ✅ canCreateSubusers = false
- ✅ canManageMachines = false
- ✅ canAssignLicenses = false

---

## 📊 Expected Responses

### Success (201 Created):
```json
{
  "id": 1,
  "parentUserId": 2,
  "parentUserName": "Admin",
  "parentUserEmail": "admin@test.com",
  "name": "Test User",
  "email": "test@test.com",
  "role": "subuser",
  "status": "active",
  "createdAt": "2024-01-15T10:00:00Z",
  ...
}
```

### Error (400 Bad Request):
```json
{
  "message": "Email already exists"
}
```

### Error (401 Unauthorized):
```json
{
  "message": "User not authenticated"
}
```

---

## 🎊 Quick Command Reference

### cURL Commands:

```bash
# Login
curl -X POST http://localhost:4000/api/RoleBasedAuth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Admin@123"}'

# Create Minimal Subuser
curl -X POST http://localhost:4000/api/SubuserManagement \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test@123"}'

# Get All Subusers
curl -X GET http://localhost:4000/api/SubuserManagement \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

**All test cases ready! Start testing! 🚀**
