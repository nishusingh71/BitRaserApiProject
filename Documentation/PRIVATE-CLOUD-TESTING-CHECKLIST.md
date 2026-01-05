# ✅ Private Cloud API - Testing Checklist

## 🎯 Complete Testing Guide

### **Prerequisites**
- [ ] ✅ Build successful
- [ ] ✅ Database running
- [ ] ✅ User exists in database
- [ ] ✅ User has `is_private_cloud = TRUE`
- [ ] ✅ JWT secret configured in appsettings.json
- [ ] ✅ API running on localhost:5000 (or your port)

---

## 📝 Step-by-Step Testing

### **1. Login & Get Token** ✅

**Request:**
```http
POST http://localhost:5000/api/RoleBasedAuth/login
Content-Type: application/json

{
"email": "user@example.com",
  "password": "YourPassword123"
}
```

**Expected Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userType": "user",
  "email": "user@example.com",
  "roles": ["User"],
  "permissions": [],
  "expiresAt": "2025-01-15T10:00:00Z"
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ Token is present
- [ ] ✅ Token is not empty
- [ ] ✅ Email matches request
- [ ] ✅ ExpiresAt is in future

---

### **2. Check Private Cloud Access** ✅

**Request:**
```http
GET http://localhost:5000/api/PrivateCloud/check-access
Authorization: Bearer YOUR_TOKEN_HERE
```

**Expected Response (200 OK):**
```json
{
  "hasPrivateCloudAccess": true,
  "isConfigured": false,
  "isSchemaInitialized": false,
  "lastTested": null,
  "testStatus": null,
  "databaseType": null,
  "currentUser": "user@example.com"
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ `hasPrivateCloudAccess` is `true`
- [ ] ✅ `currentUser` matches your email
- [ ] ✅ Not getting 401 error

**If you get 400 "Private cloud feature not enabled":**
```sql
UPDATE users SET is_private_cloud = TRUE WHERE user_email = 'user@example.com';
```

---

### **3. Get Setup Wizard** ✅

**Request:**
```http
GET http://localhost:5000/api/PrivateCloud/setup-wizard
Authorization: Bearer YOUR_TOKEN
```

**Expected Response (200 OK):**
```json
{
  "steps": [
    {
      "step": 1,
"title": "Database Type",
      "description": "Select your database type (MySQL, PostgreSQL, SQL Server)",
      "fields": [...]
    },
    ...
  ],
  "currentStep": 1,
  "totalSteps": 5
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ 5 steps returned
- [ ] ✅ Each step has title and description

---

### **4. Get Required Tables** ✅

**Request:**
```http
GET http://localhost:5000/api/PrivateCloud/required-tables
Authorization: Bearer YOUR_TOKEN
```

**Expected Response (200 OK):**
```json
{
  "tables": [
    "users",
  "groups",
    "subuser",
    "machines",
"audit_reports",
    "sessions",
    "logs",
    "commands"
  ],
  "totalCount": 8,
  "description": "These tables will be created in your private database"
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ 8 tables listed
- [ ] ✅ All table names are strings

---

### **5. Setup Database Configuration** ✅

**Request:**
```http
POST http://localhost:5000/api/PrivateCloud/setup
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "databaseType": "mysql",
  "serverHost": "gateway01.ap-southeast-1.prod.aws.tidbcloud.com",
  "serverPort": 4000,
  "databaseName": "Cloud_Erase",
  "databaseUsername": "2tdeFNZMcsWKkDR.root",
  "databasePassword": "76wtaj1GZkg7Qhek",
  "storageLimitMb": 1024,
  "notes": "Test TiDB setup"
}
```

**Expected Response (200 OK):**
```json
{
  "message": "Private database configured successfully",
  "nextStep": "Test the connection using /test endpoint"
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ Success message received
- [ ] ✅ Configuration saved in database

**Verify in database:**
```sql
SELECT * FROM private_cloud_databases WHERE user_email = 'user@example.com';
```

---

### **6. Test Database Connection** ✅

**Request:**
```http
POST http://localhost:5000/api/PrivateCloud/test
Authorization: Bearer YOUR_TOKEN
```

**Expected Response (200 OK) - Success:**
```json
{
  "success": true,
  "message": "Connection successful",
  "serverVersion": "8.0.11-TiDB-v7.5.0",
  "responseTimeMs": 245,
  "schemaExists": false,
  "missingTables": [
    "users",
    "subuser",
    "machines",
    "audit_reports",
    "sessions",
    "logs",
    "commands",
    "groups"
  ],
  "testedAt": "2025-01-14T10:30:00Z"
}
```

**Expected Response (200 OK) - Failure:**
```json
{
  "success": false,
  "message": "Connection failed",
  "error": "Access denied for user '...'",
  "testedAt": "2025-01-14T10:30:00Z"
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ `success` field is present
- [ ] ✅ Response time is reasonable (< 5 seconds)
- [ ] ✅ If success=true, serverVersion is returned
- [ ] ✅ If success=false, error message is clear

---

### **7. Initialize Database Schema** ✅

**Request:**
```http
POST http://localhost:5000/api/PrivateCloud/initialize-schema
Authorization: Bearer YOUR_TOKEN
```

**Expected Response (200 OK):**
```json
{
  "message": "Database schema initialized successfully",
  "note": "All required tables have been created in your private database"
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ Success message received
- [ ] ✅ Schema marked as initialized in database

**Verify in your TiDB/MySQL:**
```sql
SHOW TABLES;
```

**Expected tables:**
- users
- groups
- subuser
- machines
- audit_reports
- sessions
- logs
- commands

---

### **8. Validate Schema** ✅

**Request:**
```http
POST http://localhost:5000/api/PrivateCloud/validate-schema
Authorization: Bearer YOUR_TOKEN
```

**Expected Response (200 OK) - All Tables Exist:**
```json
{
  "isValid": true,
  "message": "All tables exist",
  "existingTables": [
    "users",
    "groups",
    "subuser",
    "machines",
    "audit_reports",
 "sessions",
    "logs",
    "commands"
  ],
  "missingTables": [],
"requiredTables": [
    "users",
    "groups",
    "subuser",
    "machines",
    "audit_reports",
    "sessions",
    "logs",
    "commands"
  ]
}
```

**Expected Response (200 OK) - Missing Tables:**
```json
{
  "isValid": false,
  "message": "Missing tables: machines, audit_reports",
  "existingTables": ["users", "groups", "subuser", "sessions", "logs", "commands"],
  "missingTables": ["machines", "audit_reports"],
  "requiredTables": [...]
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ `isValid` is `true`
- [ ] ✅ All 8 tables in `existingTables`
- [ ] ✅ `missingTables` is empty array

---

### **9. Get Configuration** ✅

**Request:**
```http
GET http://localhost:5000/api/PrivateCloud/config
Authorization: Bearer YOUR_TOKEN
```

**Expected Response (200 OK):**
```json
{
  "configId": 1,
  "userId": 123,
  "userEmail": "user@example.com",
  "connectionString": "***ENCRYPTED***",
  "databaseType": "mysql",
  "serverHost": "gateway01.ap-southeast-1.prod.aws.tidbcloud.com",
  "serverPort": 4000,
  "databaseName": "Cloud_Erase",
  "databaseUsername": "2tdeFNZMcsWKkDR.root",
  "isActive": true,
  "lastTestedAt": "2025-01-14T10:30:00Z",
  "testStatus": "success",
  "testError": null,
  "schemaInitialized": true,
  "schemaInitializedAt": "2025-01-14T10:35:00Z",
  "schemaVersion": "1.0.0",
  "storageUsedMb": 0,
  "storageLimitMb": 1024,
  "createdAt": "2025-01-14T10:00:00Z",
  "updatedAt": "2025-01-14T10:35:00Z",
  "createdBy": "user@example.com",
  "notes": "Test TiDB setup"
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ All configuration fields returned
- [ ] ✅ Connection string is masked (`***ENCRYPTED***`)
- [ ] ✅ `isActive` is `true`
- [ ] ✅ `schemaInitialized` is `true`

---

### **10. Delete Configuration (Optional)** ✅

**Request:**
```http
DELETE http://localhost:5000/api/PrivateCloud/config
Authorization: Bearer YOUR_TOKEN
```

**Expected Response (200 OK):**
```json
{
  "message": "Private database configuration removed successfully"
}
```

**Checklist:**
- [ ] ✅ Status code is 200
- [ ] ✅ Success message received
- [ ] ✅ Configuration removed from database
- [ ] ⚠️ **Note:** Data in private database is NOT deleted

---

## 🔍 Error Scenarios to Test

### **401 Unauthorized**
- [ ] ✅ Request without Authorization header
- [ ] ✅ Request with invalid/expired token
- [ ] ✅ Request with malformed token

### **400 Bad Request**
- [ ] ✅ Setup without `is_private_cloud = TRUE`
- [ ] ✅ Test without configuration
- [ ] ✅ Invalid database credentials

### **404 Not Found**
- [ ] ✅ Get config when none exists
- [ ] ✅ Delete config when none exists

### **500 Internal Server Error**
- [ ] ✅ Database connection error
- [ ] ✅ Invalid database host

---

## 📊 Complete Flow Test

**Test the entire user journey:**

1. ✅ Login → Get token
2. ✅ Check access → Verify `is_private_cloud = TRUE`
3. ✅ Get setup wizard → Display 5 steps
4. ✅ Get required tables → Show 8 tables
5. ✅ Setup database → Save configuration
6. ✅ Test connection → Verify connectivity
7. ✅ Initialize schema → Create 8 tables
8. ✅ Validate schema → Confirm all tables exist
9. ✅ Get config → Show configuration (masked password)

---

## 🎯 Success Criteria

**All endpoints should return:**
- ✅ Correct HTTP status codes (200, 400, 401, 404, 500)
- ✅ Consistent JSON response format
- ✅ Clear error messages
- ✅ User email extracted from JWT correctly
- ✅ No 401 errors with valid token
- ✅ Encrypted connection strings
- ✅ Proper logging

---

## 📝 Test Report Template

```
## Private Cloud API Test Report

**Date:** 2025-01-14
**Tester:** Your Name
**Environment:** Development

### Login & Authentication
- [x] Login successful
- [x] Token received
- [x] Token valid for 24 hours

### Access Check
- [x] Check access endpoint works
- [x] User email extracted correctly
- [x] Private cloud access confirmed

### Database Setup
- [x] Setup wizard returns 5 steps
- [x] Required tables list shows 8 tables
- [x] Database configuration saved
- [x] Connection test successful
- [x] Schema initialization successful
- [x] Schema validation successful

### Error Handling
- [x] 401 for missing token
- [x] 400 for missing private cloud flag
- [x] 404 for non-existent config
- [x] 500 for database errors

### Overall Result: ✅ PASS / ❌ FAIL

**Issues Found:** None / [List issues]

**Notes:** [Any additional comments]
```

---

**Happy Testing! 🚀**

If you encounter any issues, check:
1. JWT token is valid
2. User has `is_private_cloud = TRUE`
3. Database credentials are correct
4. Network connectivity to database
5. All migrations applied

