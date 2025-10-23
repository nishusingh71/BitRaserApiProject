# 🧪 Comprehensive Testing Guide - Email-Based CRUD Operations

## 🎯 Complete Testing Checklist

Test all email-based CRUD operations with PATCH support across SubuserManagement and Group controllers.

---

## 🔐 Step 1: Authentication

### Login First
```http
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "admin@test.com",
  "password": "Admin@123"
}
```

**Expected Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "admin@test.com",
  "roles": ["SuperAdmin"],
  "expiresAt": "2024-02-01T12:00:00Z"
}
```

**Action**: Copy the `token` value for use in subsequent requests.

---

## 🧑 Section A: Subuser Management Tests

### Test A1: Get All Subusers
```http
GET http://localhost:4000/api/SubuserManagement
Authorization: Bearer <your-token>
```

**Expected**: 200 OK with list of subusers

---

### Test A2: Get Subuser by Email ✨
```http
GET http://localhost:4000/api/SubuserManagement/by-email/test@test.com
Authorization: Bearer <your-token>
```

**Expected**: 
- 200 OK if exists
- 404 Not Found if doesn't exist

---

### Test A3: Create Subuser (Minimal Fields)
```http
POST http://localhost:4000/api/SubuserManagement
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "email": "newuser@test.com",
  "password": "NewUser@123"
}
```

**Expected**: 201 Created with subuser details

**Verify**:
- Name should be auto-generated from email (`newuser`)
- Role should default to `"subuser"`
- MaxMachines should default to `5`

---

### Test A4: Create Subuser (Full Details)
```http
POST http://localhost:4000/api/SubuserManagement
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "subuserUsername": "johndoe123",
  "name": "John Doe",
  "email": "john.doe@test.com",
  "password": "JohnDoe@123",
  "phone": "+1-555-1234",
  "jobTitle": "Software Engineer",
  "department": "Engineering",
  "role": "team_member",
  "accessLevel": "full",
  "maxMachines": 10,
  "groupId": 1,
  "canViewReports": true,
"canManageMachines": true,
  "notes": "Senior engineer with full access"
}
```

**Expected**: 201 Created

---

### Test A5: Full Update by Email (PUT)
```http
PUT http://localhost:4000/api/SubuserManagement/by-email/john.doe@test.com
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "name": "John Doe Updated",
  "phone": "+1-555-5678",
  "department": "DevOps",
  "role": "team_member",
  "accessLevel": "full",
  "maxMachines": 15,
  "notes": "Promoted to DevOps team lead"
}
```

**Expected**: 200 OK with success message

---

### Test A6: Partial Update by Email (PATCH) ✨
```http
PATCH http://localhost:4000/api/SubuserManagement/by-email/john.doe@test.com
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "department": "Cloud Engineering",
  "phone": "+1-555-9999"
}
```

**Expected**: 
- 200 OK
- Only department and phone updated
- All other fields remain unchanged

---

### Test A7: Partial Update - Single Field (PATCH) ✨
```http
PATCH http://localhost:4000/api/SubuserManagement/by-email/john.doe@test.com
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "maxMachines": 20
}
```

**Expected**:
- 200 OK
- Only maxMachines updated to 20
- Everything else unchanged

---

### Test A8: Update by ID (Verify Backward Compatibility)
```http
PATCH http://localhost:4000/api/SubuserManagement/1
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "notes": "Updated via ID endpoint"
}
```

**Expected**: 200 OK (proves backward compatibility)

---

### Test A9: Change Password
```http
POST http://localhost:4000/api/SubuserManagement/1/change-password
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "currentPassword": "JohnDoe@123",
  "newPassword": "NewPassword@456",
  "confirmPassword": "NewPassword@456"
}
```

**Expected**: 200 OK

---

### Test A10: Assign Machines
```http
POST http://localhost:4000/api/SubuserManagement/assign-machines
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "subuserId": 1,
  "machineIds": [1, 2, 3]
}
```

**Expected**: 200 OK

---

### Test A11: Get Statistics
```http
GET http://localhost:4000/api/SubuserManagement/statistics
Authorization: Bearer <your-token>
```

**Expected**: 200 OK with comprehensive statistics

---

### Test A12: Delete by Email ✨
```http
DELETE http://localhost:4000/api/SubuserManagement/by-email/john.doe@test.com
Authorization: Bearer <your-token>
```

**Expected**: 200 OK

**Verify**: GET by email should now return 404

---

## 👥 Section B: Group Management Tests

### Test B1: Get All Groups
```http
GET http://localhost:4000/api/Group
Authorization: Bearer <your-token>
```

**Expected**: 200 OK with list of groups

---

### Test B2: Create Group
```http
POST http://localhost:4000/api/Group
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "groupName": "Engineering Team",
  "groupDescription": "Software Engineering Department",
  "groupLicenseAllocation": 50,
  "groupPermission": "{\"read\": true, \"write\": true, \"delete\": false}"
}
```

**Expected**: 201 Created

**Note**: Save the `groupId` from response for next tests

---

### Test B3: Get Group by ID
```http
GET http://localhost:4000/api/Group/1
Authorization: Bearer <your-token>
```

**Expected**: 200 OK with group details

---

### Test B4: Full Update Group (PUT)
```http
PUT http://localhost:4000/api/Group/1
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "groupName": "Engineering Team Updated",
  "groupDescription": "Updated description",
  "groupLicenseAllocation": 75,
  "groupPermission": "{\"read\": true, \"write\": true, \"delete\": true}",
  "status": "active"
}
```

**Expected**: 200 OK

---

### Test B5: Partial Update Group (PATCH) ✨
```http
PATCH http://localhost:4000/api/Group/1
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "groupLicenseAllocation": 100
}
```

**Expected**: 
- 200 OK
- Only license allocation updated
- Other fields unchanged

---

### Test B6: Get Group Members
```http
GET http://localhost:4000/api/Group/1/members
Authorization: Bearer <your-token>
```

**Expected**: 200 OK with member list (may be empty initially)

---

### Test B7: Add Member by Email ✨
```http
POST http://localhost:4000/api/Group/1/members/by-email
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "email": "newuser@test.com"
}
```

**Expected**: 
- 200 OK if subuser exists
- 404 Not Found if subuser doesn't exist

**Prerequisite**: Subuser must exist first (create using Test A3/A4)

---

### Test B8: Get Specific Member by Email ✨
```http
GET http://localhost:4000/api/Group/1/members/by-email/newuser@test.com
Authorization: Bearer <your-token>
```

**Expected**: 200 OK with member details

---

### Test B9: Get Group by Member Email ✨
```http
GET http://localhost:4000/api/Group/by-member-email/newuser@test.com
Authorization: Bearer <your-token>
```

**Expected**: 200 OK with group details

---

### Test B10: Remove Member by Email ✨
```http
DELETE http://localhost:4000/api/Group/1/members/by-email/newuser@test.com
Authorization: Bearer <your-token>
```

**Expected**: 200 OK

**Verify**: Member should no longer be in group (Test B6)

---

### Test B11: Get Group Statistics
```http
GET http://localhost:4000/api/Group/statistics
Authorization: Bearer <your-token>
```

**Expected**: 200 OK with statistics

---

### Test B12: Delete Group (Should Fail with Members)
```http
DELETE http://localhost:4000/api/Group/1
Authorization: Bearer <your-token>
```

**Expected**: 
- 400 Bad Request if group has members
- Suggests removing members first

---

### Test B13: Delete Group (After Removing Members)
1. First remove all members (Test B10)
2. Then delete group:

```http
DELETE http://localhost:4000/api/Group/1
Authorization: Bearer <your-token>
```

**Expected**: 200 OK

---

## 🔍 Section C: Error Handling Tests

### Test C1: Unauthorized Access (No Token)
```http
GET http://localhost:4000/api/SubuserManagement
```

**Expected**: 401 Unauthorized

---

### Test C2: Invalid Email Format
```http
GET http://localhost:4000/api/SubuserManagement/by-email/not-an-email
Authorization: Bearer <your-token>
```

**Expected**: 404 Not Found

---

### Test C3: Non-Existent Email
```http
GET http://localhost:4000/api/SubuserManagement/by-email/doesnotexist@test.com
Authorization: Bearer <your-token>
```

**Expected**: 404 Not Found

---

### Test C4: Duplicate Email
```http
POST http://localhost:4000/api/SubuserManagement
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "email": "newuser@test.com",
  "password": "Password@123"
}
```

Run twice. 

**Expected**: 
- First call: 201 Created
- Second call: 400 Bad Request (Email already exists)

---

### Test C5: Weak Password
```http
POST http://localhost:4000/api/SubuserManagement
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "email": "weakpass@test.com",
  "password": "123"
}
```

**Expected**: 400 Bad Request (Password too short)

---

### Test C6: Missing Required Fields
```http
POST http://localhost:4000/api/SubuserManagement
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "name": "No Email Or Password"
}
```

**Expected**: 400 Bad Request

---

### Test C7: Invalid Group ID
```http
PATCH http://localhost:4000/api/SubuserManagement/by-email/newuser@test.com
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "groupId": 99999
}
```

**Expected**: Should accept (foreign key constraint may fail later)

---

## 📊 Section D: PATCH vs PUT Comparison

### Test D1: PUT Requires All Fields
```http
PUT http://localhost:4000/api/SubuserManagement/by-email/newuser@test.com
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "department": "IT"
}
```

**Expected**: 200 OK (but may set other fields to null/default)

---

### Test D2: PATCH Updates Only Provided Fields ✨
```http
PATCH http://localhost:4000/api/SubuserManagement/by-email/newuser@test.com
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "department": "IT"
}
```

**Expected**: 
- 200 OK
- Only department updated
- All other fields preserved

**Verify**: GET the subuser and check other fields are unchanged

---

### Test D3: PATCH Multiple Fields ✨
```http
PATCH http://localhost:4000/api/SubuserManagement/by-email/newuser@test.com
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "department": "Engineering",
  "phone": "+1-555-1111",
  "maxMachines": 8,
  "notes": "Updated via PATCH"
}
```

**Expected**: 
- 200 OK
- Only these 4 fields updated
- Rest unchanged

---

## ✅ Section E: Integration Tests

### Test E1: Complete Workflow - Create & Update
1. Create subuser with minimal fields
2. PATCH to add department
3. PATCH to add phone
4. PATCH to update maxMachines
5. GET to verify all changes

```bash
# 1. Create
POST /api/SubuserManagement
{ "email": "workflow@test.com", "password": "Test@123" }

# 2. Add department
PATCH /api/SubuserManagement/by-email/workflow@test.com
{ "department": "IT" }

# 3. Add phone
PATCH /api/SubuserManagement/by-email/workflow@test.com
{ "phone": "+1-555-2222" }

# 4. Update maxMachines
PATCH /api/SubuserManagement/by-email/workflow@test.com
{ "maxMachines": 10 }

# 5. Verify
GET /api/SubuserManagement/by-email/workflow@test.com
```

**Expected**: Final GET should show all fields correctly set

---

### Test E2: Group Member Management Workflow
1. Create group
2. Create subuser
3. Add subuser to group
4. Verify member in group
5. Get group by member email
6. Remove member from group

```bash
# 1. Create group
POST /api/Group
{ "groupName": "Test Group", ... }

# 2. Create subuser
POST /api/SubuserManagement
{ "email": "member@test.com", "password": "Test@123" }

# 3. Add to group
POST /api/Group/1/members/by-email
{ "email": "member@test.com" }

# 4. Verify member
GET /api/Group/1/members/by-email/member@test.com

# 5. Get group by member
GET /api/Group/by-member-email/member@test.com

# 6. Remove member
DELETE /api/Group/1/members/by-email/member@test.com
```

**Expected**: All operations successful in sequence

---

## 📈 Section F: Performance Tests

### Test F1: Pagination
```http
GET http://localhost:4000/api/SubuserManagement?page=1&pageSize=10
Authorization: Bearer <your-token>
```

**Expected**: Max 10 results with pagination headers

---

### Test F2: Filtering
```http
GET http://localhost:4000/api/SubuserManagement?department=IT&status=active
Authorization: Bearer <your-token>
```

**Expected**: Only IT department active subusers

---

### Test F3: Search
```http
GET http://localhost:4000/api/SubuserManagement?search=john
Authorization: Bearer <your-token>
```

**Expected**: Subusers with "john" in name/email/department

---

## 🎯 Test Summary Checklist

### Subuser Management:
- [ ] Get all subusers
- [ ] Get by ID
- [ ] Get by email ✨
- [ ] Create with minimal fields
- [ ] Create with full details
- [ ] Full update by ID (PUT)
- [ ] Full update by email (PUT) ✨
- [ ] Partial update by ID (PATCH) ✨
- [ ] Partial update by email (PATCH) ✨
- [ ] Delete by ID
- [ ] Delete by email ✨
- [ ] Change password
- [ ] Assign machines
- [ ] Get statistics

### Group Management:
- [ ] Get all groups
- [ ] Get by ID
- [ ] Create group
- [ ] Full update (PUT)
- [ ] Partial update (PATCH) ✨
- [ ] Get members
- [ ] Get member by email ✨
- [ ] Add member by email ✨
- [ ] Remove member by email ✨
- [ ] Get group by member email ✨
- [ ] Delete group
- [ ] Get statistics

### Error Handling:
- [ ] Unauthorized access
- [ ] Invalid email format
- [ ] Non-existent email
- [ ] Duplicate email
- [ ] Weak password
- [ ] Missing required fields

### PATCH vs PUT:
- [ ] PATCH updates only provided fields ✨
- [ ] PUT updates all fields
- [ ] PATCH with single field ✨
- [ ] PATCH with multiple fields ✨

---

## 📝 Expected Results Summary

| Test Type | Total Tests | Expected Pass |
|-----------|-------------|---------------|
| Subuser CRUD | 14 | 14 |
| Group CRUD | 13 | 13 |
| Error Handling | 7 | 7 |
| PATCH vs PUT | 4 | 4 |
| Integration | 2 | 2 |
| Performance | 3 | 3 |
| **Total** | **43** | **43** |

---

## 🚀 Quick Test Script (Copy-Paste)

Save this in a file and run in sequence:

```bash
# Setup
TOKEN="your-token-here"
BASE_URL="http://localhost:4000"

# Test 1: Create Subuser
curl -X POST "$BASE_URL/api/SubuserManagement" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test@123"}'

# Test 2: Get by Email
curl -X GET "$BASE_URL/api/SubuserManagement/by-email/test@test.com" \
  -H "Authorization: Bearer $TOKEN"

# Test 3: PATCH Update
curl -X PATCH "$BASE_URL/api/SubuserManagement/by-email/test@test.com" \
-H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"department":"IT"}'

# Test 4: Create Group
curl -X POST "$BASE_URL/api/Group" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"groupName":"Test Group","groupLicenseAllocation":10}'

# Test 5: Add Member to Group
curl -X POST "$BASE_URL/api/Group/1/members/by-email" \
  -H "Authorization: Bearer $TOKEN" \
-H "Content-Type: application/json" \
  -d '{"email":"test@test.com"}'

# Test 6: Get Group by Member Email
curl -X GET "$BASE_URL/api/Group/by-member-email/test@test.com" \
  -H "Authorization: Bearer $TOKEN"
```

---

**All tests ready! Start testing systematically! 🧪✅**
