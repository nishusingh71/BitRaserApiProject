# 🎯 LoginActivity Controller - README

## ⚠️ **CRITICAL: Field Separation**

This controller uses **ONLY `activity_status` field** and **NEVER touches `status` field**.

```
❌ status         → Account status (admin controlled)
✅ activity_status  → Online/Offline (automatic tracking)
```

---

## 📋 **Quick Start**

### **1. Database Migration Required:**
```bash
mysql -u root -p dsecure < Database/add_activity_status_columns.sql
```

### **2. Uncomment Controller Code:**
Search for `// TODO: Uncomment after migration` and uncomment those lines.

### **3. Rebuild & Test:**
```bash
dotnet build
curl -X POST http://localhost:4000/api/LoginActivity/user/login \
  -H "Authorization: Bearer TOKEN" \
  -d '{"email":"test@example.com"}'
```

---

## 🚀 **Endpoints**

| Method | Endpoint | Updates | Returns |
|--------|----------|---------|---------|
| POST | `/api/LoginActivity/user/login` | `activity_status` = "online" | activity details |
| POST | `/api/LoginActivity/user/logout` | `activity_status` = "offline" | activity details |
| GET | `/api/LoginActivity/user/{email}` | - | activity details |
| POST | `/api/LoginActivity/subuser/login` | `activity_status` = "online" | activity details |
| POST | `/api/LoginActivity/subuser/logout` | `activity_status` = "offline" | activity details |
| GET | `/api/LoginActivity/subuser/{email}` | - | activity details |
| GET | `/api/LoginActivity/users` | - | all users activity |
| GET | `/api/LoginActivity/subusers` | - | all subusers activity |
| GET | `/api/LoginActivity/parent/{email}/subusers` | - | parent's subusers |

---

## 📊 **Field Updates**

### **Login:**
```
✅ last_login       = server_time
✅ last_logout      = NULL
✅ last_login_ip    = client_ip
✅ activity_status  = "online"
❌ status           = UNCHANGED
```

### **Logout:**
```
✅ last_logout      = server_time
✅ activity_status  = "offline"
❌ status  = UNCHANGED
```

---

## 📝 **Example Usage**

### **Frontend Login:**
```javascript
// 1. Authenticate user
const authRes = await fetch('/api/Auth/login', {
  method: 'POST',
  body: JSON.stringify({ email, password })
});
const { token } = await authRes.json();

// 2. Record login activity
await fetch('/api/LoginActivity/user/login', {
  method: 'POST',
  headers: { 
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ email })
});
// ✅ activity_status = "online"
// ✅ status = unchanged
```

### **Frontend Logout:**
```javascript
// Record logout activity
await fetch('/api/LoginActivity/user/logout', {
  method: 'POST',
  headers: { 
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ email })
});
// ✅ activity_status = "offline"
// ✅ status = unchanged
```

---

## 🔍 **Status Calculation**

```
ONLINE  = last_login within 5 minutes + no logout after
OFFLINE = everything else
```

**Example:**
```
Current time: 15:00:00

User A:
  last_login: 14:58:00 (2 mins ago)
  last_logout: NULL
  → activity_status: "online" ✅

User B:
  last_login: 14:30:00
  last_logout: 14:35:00
  → activity_status: "offline" ✅

User C:
  last_login: 14:50:00 (10 mins ago)
  last_logout: NULL
  → activity_status: "offline" ✅
```

---

## ⚠️ **Important Notes**

### **1. Never Modifies status Field:**
```csharp
// ❌ WRONG:
user.status = "online";

// ✅ RIGHT:
user.activity_status = "online";
```

### **2. Separate Concerns:**
```
status:
  - Purpose: Account state
  - Values: active, inactive, suspended, banned
  - Updated by: Admin/System
  - Controller: UsersController, SubuserController

activity_status:
  - Purpose: Real-time presence
  - Values: online, offline
  - Updated by: LoginActivityController
  - Controller: This controller ONLY
```

### **3. Both Can Coexist:**
```
User Example:
  status: "active"
  activity_status: "offline"
  
Meaning:
  ✅ Account is active (can login)
  ❌ User is not currently logged in
```

---

## 📁 **Documentation**

| File | Purpose |
|------|---------|
| `LOGIN-ACTIVITY-CONTROLLER.md` | Complete documentation |
| `LOGIN-ACTIVITY-QUICK.md` | Quick reference |
| `LOGIN-ACTIVITY-MIGRATION-REQUIRED.md` | Migration guide |
| `LOGIN-ACTIVITY-SUMMARY.md` | Complete summary |
| `LOGIN-ACTIVITY-INTEGRATION.md` | Integration examples |
| `ACTIVITY-STATUS-MIGRATION-GUIDE.md` | Database migration |

---

## ✅ **Build Status**

```
✅ Build: Successful
✅ No errors
⏳ Migration: Required before use
⏳ Code: Commented out (uncomment after migration)
```

---

## 🎯 **Summary**

| Feature | Status | Note |
|---------|--------|------|
| **Field Used** | activity_status | NOT status ✅ |
| **Account Status** | Never touched | Preserved ✅ |
| **Migration** | Required | Run SQL first ⏳ |
| **Separation** | Clean | Independent fields ✅ |
| **Build** | Successful | Ready after migration ✅ |

---

**File:** `BitRaserApiProject/Controllers/LoginActivityController.cs`  
**Migration:** `Database/add_activity_status_columns.sql`  
**Status:** ⏳ **Migration Required**

**Sirf `activity_status` use hoga, `status` ko kabhi touch nahi karega!** ✅
