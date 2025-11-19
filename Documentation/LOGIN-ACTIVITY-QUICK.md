# ⚡ LoginActivity Controller - Quick Reference

## 🎯 **Purpose**
Auto-fill login/logout details using **ONLY `activity_status`** field (NOT `status` field)

⚠️ **MIGRATION REQUIRED:** Run `Database/add_activity_status_columns.sql` first

---

## ⚠️ **CRITICAL: Field Separation**

```
❌ status         → Account status (NEVER TOUCHED)
✅ activity_status  → Online/Offline (Used by this controller)
```

**Example:**
```
User A:
  status: "active" → Account is active
  activity_status: "offline" → Not logged in
```

---

## 🚀 **Endpoints**

### **USER ENDPOINTS**

#### 1. User Login
```http
POST /api/LoginActivity/user/login
Content-Type: application/json

{"email": "user@example.com"}
```
**Updates:** `activity_status = "online"` (NOT status)

#### 2. User Logout
```http
POST /api/LoginActivity/user/logout
Content-Type: application/json

{"email": "user@example.com"}
```
**Updates:** `activity_status = "offline"` (NOT status)

#### 3. Get User Activity
```http
GET /api/LoginActivity/user/{email}
```
**Returns:** `activity_status` (NOT status)

---

### **SUBUSER ENDPOINTS**

#### 4. Subuser Login
```http
POST /api/LoginActivity/subuser/login
Content-Type: application/json

{"email": "subuser@example.com"}
```
**Updates:** `activity_status = "online"` (NOT status)

#### 5. Subuser Logout
```http
POST /api/LoginActivity/subuser/logout
Content-Type: application/json

{"email": "subuser@example.com"}
```
**Updates:** `activity_status = "offline"` (NOT status)

#### 6. Get Subuser Activity
```http
GET /api/LoginActivity/subuser/{email}
```
**Returns:** `activity_status` (NOT status)

---

### **BULK ENDPOINTS**

#### 7. All Users Activity
```http
GET /api/LoginActivity/users
```

#### 8. All Subusers Activity
```http
GET /api/LoginActivity/subusers
```

#### 9. Parent's Subusers Activity
```http
GET /api/LoginActivity/parent/{parentEmail}/subusers
```

---

## 📊 **Fields Updated (After Migration)**

### **On Login:**
```
✅ last_login = server_time
✅ last_logout = null
✅ last_login_ip = client_ip
✅ activity_status = "online"
❌ status = UNCHANGED
```

### **On Logout:**
```
✅ last_logout = server_time
✅ activity_status = "offline"
❌ status = UNCHANGED
```

---

## 🔧 **Migration Steps**

```bash
# 1. Run SQL migration
mysql -u root -p dsecure < Database/add_activity_status_columns.sql

# 2. Verify columns
DESCRIBE users;  # Check for activity_status, last_login_ip
DESCRIBE subuser;  # Check for activity_status

# 3. Uncomment in controller
# - user.activity_status = "online";
# - user.last_login_ip = ipAddress;
# - subuser.activity_status = "online";

# 4. Rebuild
dotnet build

# 5. Test
curl -X POST http://localhost:4000/api/LoginActivity/user/login \
  -H "Authorization: Bearer TOKEN" \
  -d '{"email":"admin@example.com"}'
```

---

## 📝 **Response Example (After Migration)**

```json
{
  "success": true,
  "message": "User login recorded successfully",
  "data": {
 "email": "admin@example.com",
    "user_name": "Admin User",
    "last_login": "2025-01-26T12:30:45Z",
    "last_logout": null,
    "activity_status": "online",
    "server_time": "2025-01-26T12:30:45Z"
  }
}
```

**Note:** `status` field is **NOT** in response.

---

## ✅ **Status Calculation**

```
ONLINE = last_login within 5 mins + no logout after
OFFLINE = everything else
```

---

## ⚠️ **Important**

1. **NEVER modifies `status` field**
2. **ONLY uses `activity_status` field**
3. **Requires database migration first**
4. **`status` = account state (admin controlled)**
5. **`activity_status` = presence (automatic)**

---

**File:** `BitRaserApiProject/Controllers/LoginActivityController.cs`  
**Status:** ✅ **Ready After Migration**  
**Build:** ✅ **Successful**  
**Field:** ✅ **activity_status ONLY**
