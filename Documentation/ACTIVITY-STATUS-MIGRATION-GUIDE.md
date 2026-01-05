# ✅ Activity Status Column Addition - Migration Guide

## 🎯 Purpose

Add `activity_status` and `last_login_ip` columns to `users` and `subuser` tables for real-time online/offline tracking.

---

## 📋 Changes Summary

### **Users Table:**
| Column | Type | Default | Purpose |
|--------|------|---------|---------|
| `activity_status` | VARCHAR(50) | 'offline' | Real-time online/offline status |
| `last_login_ip` | VARCHAR(500) | NULL | Last login IP address |

### **Subuser Table:**
| Column | Type | Default | Purpose |
|--------|------|---------|---------|
| `activity_status` | VARCHAR(50) | 'offline' | Real-time online/offline status |
| `LastLoginIp` | VARCHAR(500) | NULL | ✅ Already exists |

---

## 🔧 Migration Steps

### **Step 1: Run SQL Migration**
```sql
-- Execute the migration script
mysql -u root -p dsecure < Database/add_activity_status_columns.sql
```

Or manually:
```sql
-- Connect to database
USE dsecure;

-- Add activity_status to users table
ALTER TABLE users 
ADD COLUMN activity_status VARCHAR(50) DEFAULT 'offline' AFTER status;

-- Add last_login_ip to users table
ALTER TABLE users 
ADD COLUMN last_login_ip VARCHAR(500) NULL AFTER last_logout;

-- Add activity_status to subuser table
ALTER TABLE subuser 
ADD COLUMN activity_status VARCHAR(50) DEFAULT 'offline' AFTER status;

-- Verify changes
DESCRIBE users;
DESCRIBE subuser;
```

---

### **Step 2: Update Code (Already Done)**

The controller code has been updated with fallback logic.

---

### **Step 3: Uncomment Activity Status Code**

After running the SQL migration, update `UserActivityController.cs` by removing TODO comments.

---

## 📊 Field Comparison

### **status vs activity_status**

| Field | Purpose | Values | Updated By |
|-------|---------|--------|------------|
| `status` | Account status | active, inactive, suspended, banned | Admin manually |
| `activity_status` | Real-time presence | online, offline | UserActivity API automatically |

---

## ✅ Verification

### **Check Tables:**
```sql
-- Verify users table
DESCRIBE users;

-- Verify subuser table
DESCRIBE subuser;

-- Check activity_status distribution
SELECT activity_status, COUNT(*) FROM users GROUP BY activity_status;
SELECT activity_status, COUNT(*) FROM subuser GROUP BY activity_status;
```

---

## 🧪 Testing

### **Test 1: Record Login**
```bash
curl -X POST "http://localhost:4000/api/UserActivity/record-login?email=test@example.com" \
  -H "Authorization: Bearer TOKEN"
```

---

## 📝 Summary

| Step | Status | Description |
|------|--------|-------------|
| 1. SQL Migration | ⏳ Pending | Run add_activity_status_columns.sql |
| 2. Code Update | ✅ Done | Fallback logic added |
| 3. Uncomment Code | ⏳ After Migration | Remove TODO comments |
| 4. Testing | ⏳ After Update | Test all endpoints |

---

**Status:** ⏳ **READY FOR MIGRATION**  
**Next Step:** Run the SQL migration script  
**Then:** Uncomment activity_status code in controller
