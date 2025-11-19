# ⚠️ MIGRATION REQUIRED - LoginActivity Controller

## 🚨 **IMPORTANT NOTICE**

The `LoginActivityController` uses **ONLY `activity_status` field** and **NEVER touches `status` field**.

**Before using this controller, you MUST run the database migration.**

---

## 📋 **Why Migration is Required**

### **Field Separation:**

| Field | Purpose | Updated By | Controller |
|-------|---------|------------|------------|
| **`status`** | Account status | Admin/System | UsersController, SubuserController |
| **`activity_status`** | Online/Offline | LoginActivity API | **LoginActivityController** ✅ |

### **Problem Without Migration:**

```
❌ Database doesn't have activity_status column
❌ Controller code is commented out
❌ Endpoints will work but won't update activity_status
```

### **Solution:**

```
✅ Run SQL migration to add columns
✅ Uncomment controller code
✅ Rebuild project
✅ Test endpoints
```

---

## 🔧 **Step-by-Step Migration**

### **Step 1: Run SQL Migration**

```bash
# Connect to MySQL
mysql -u root -p

# Use your database
USE dsecure;

# Run migration script
SOURCE Database/add_activity_status_columns.sql;

# OR directly:
mysql -u root -p dsecure < Database/add_activity_status_columns.sql
```

### **Step 2: Verify Columns**

```sql
-- Check users table
DESCRIBE users;
-- Should show: activity_status, last_login_ip

-- Check subuser table
DESCRIBE subuser;
-- Should show: activity_status

-- Verify data
SELECT user_email, status, activity_status, last_login, last_logout 
FROM users 
LIMIT 5;
```

**Expected Output:**
```
+------------------+--------+-----------------+---------------------+---------------------+
| user_email       | status | activity_status | last_login          | last_logout       |
+------------------+--------+-----------------+---------------------+---------------------+
| admin@example.com| active | offline         | NULL           | NULL |
+------------------+--------+-----------------+---------------------+---------------------+
```

---

### **Step 3: Uncomment Controller Code**

**File:** `BitRaserApiProject/Controllers/LoginActivityController.cs`

#### **Location 1: User Login (Line ~93)**
```csharp
// BEFORE:
// user.last_login_ip = ipAddress;
// user.activity_status = "online";

// AFTER:
user.last_login_ip = ipAddress;
user.activity_status = "online";
```

#### **Location 2: User Logout (Line ~145)**
```csharp
// BEFORE:
// user.activity_status = "offline";

// AFTER:
user.activity_status = "offline";
```

#### **Location 3: Subuser Login (Line ~242)**
```csharp
// BEFORE:
// subuser.activity_status = "online";

// AFTER:
subuser.activity_status = "online";
```

#### **Location 4: Subuser Logout (Line ~290)**
```csharp
// BEFORE:
// subuser.activity_status = "offline";

// AFTER:
subuser.activity_status = "offline";
```

#### **Location 5: Get User Activity (Line ~193)**
```csharp
// BEFORE:
// last_login_ip = user.last_login_ip, // TODO: Uncomment after migration

// AFTER:
last_login_ip = user.last_login_ip,
```

#### **Location 6: Get All Users (Line ~369)**
```csharp
// BEFORE:
// last_login_ip = u.last_login_ip, // TODO: Uncomment after migration

// AFTER:
last_login_ip = u.last_login_ip,
```

---

### **Step 4: Rebuild Project**

```bash
# Clean build
dotnet clean
dotnet build

# Should show:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

---

### **Step 5: Test Endpoints**

#### **Test 1: User Login**
```bash
curl -X POST "http://localhost:4000/api/LoginActivity/user/login" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com"}'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "User login recorded successfully",
  "data": {
  "email": "admin@example.com",
    "last_login": "2025-01-26T12:30:45Z",
"last_logout": null,
    "activity_status": "online",
    "server_time": "2025-01-26T12:30:45Z"
  }
}
```

#### **Test 2: Verify Database**
```sql
SELECT user_email, status, activity_status, last_login, last_logout, last_login_ip
FROM users 
WHERE user_email = 'admin@example.com';
```

**Expected:**
```
status: "active" (unchanged)
activity_status: "online" (updated ✅)
last_login: "2025-01-26 12:30:45" (updated ✅)
last_logout: NULL (cleared ✅)
last_login_ip: "192.168.1.100" (updated ✅)
```

#### **Test 3: User Logout**
```bash
curl -X POST "http://localhost:4000/api/LoginActivity/user/logout" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com"}'
```

#### **Test 4: Verify Logout**
```sql
SELECT user_email, status, activity_status, last_logout
FROM users 
WHERE user_email = 'admin@example.com';
```

**Expected:**
```
status: "active" (still unchanged ✅)
activity_status: "offline" (updated ✅)
last_logout: "2025-01-26 14:30:45" (updated ✅)
```

---

## ✅ **Migration Checklist**

- [ ] **SQL Migration:** Ran `add_activity_status_columns.sql`
- [ ] **Verify Columns:** Checked `users` and `subuser` tables
- [ ] **Uncomment Code:** All 6 locations uncommented
- [ ] **Rebuild:** `dotnet build` successful
- [ ] **Test Login:** User login works
- [ ] **Test Logout:** User logout works
- [ ] **Verify DB:** `activity_status` updated, `status` unchanged
- [ ] **Test Subuser:** Subuser login/logout works
- [ ] **Test Get:** Get activity endpoints work

---

## 🚨 **Common Issues**

### **Issue 1: Column doesn't exist**
```
Error: Unknown column 'activity_status' in 'field list'
```

**Solution:**
```bash
# Run migration again
mysql -u root -p dsecure < Database/add_activity_status_columns.sql

# Verify
mysql -u root -p dsecure -e "DESCRIBE users;"
```

---

### **Issue 2: Still using status field**
```
Error: status field is being modified by LoginActivity
```

**Solution:**
```
Check controller code - should ONLY use activity_status
Search for: user.status = 
Should find: 0 results in LoginActivityController
```

---

### **Issue 3: Build errors**
```
Error: 'users' does not contain a definition for 'activity_status'
```

**Solution:**
```
Code is still commented out
Uncomment all activity_status lines
Rebuild
```

---

## 📊 **Before vs After Migration**

### **Before Migration:**

```csharp
// Controller code:
// user.activity_status = "online"; // Commented out
user.status = "online"; // ❌ WRONG - Old way

// Database:
status: "active" → Changes to "online" ❌
activity_status: (doesn't exist)
```

### **After Migration:**

```csharp
// Controller code:
user.activity_status = "online"; // ✅ Uncommented
// user.status is never touched

// Database:
status: "active" → Stays "active" ✅
activity_status: "offline" → Changes to "online" ✅
```

---

## 🎯 **Summary**

| Step | Action | Status |
|------|--------|--------|
| 1 | Run SQL migration | ⏳ Required |
| 2 | Verify columns | ⏳ Required |
| 3 | Uncomment code | ⏳ Required |
| 4 | Rebuild project | ⏳ Required |
| 5 | Test endpoints | ⏳ Required |
| 6 | Verify separation | ⏳ Required |

---

**Status:** ⏳ **MIGRATION PENDING**  
**File:** `Database/add_activity_status_columns.sql`  
**Documentation:** `Documentation/ACTIVITY-STATUS-MIGRATION-GUIDE.md`

**Run migration first, then uncomment code!** 🚀
