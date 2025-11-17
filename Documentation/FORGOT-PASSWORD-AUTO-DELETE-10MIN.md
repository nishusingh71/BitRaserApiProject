# 🔄 Auto-Delete & 10-Minute Expiry - Implementation Guide

## ✅ **What's New**

### **1. Extended Expiry Time**
- **Before:** 5 minutes
- **After:** 10 minutes ✅

### **2. Automatic Cleanup**
- **Background Service** runs every 15 minutes
- **Auto-deletes** expired and used requests
- **No manual cleanup** needed

---

## 🎯 **How It Works**

### **Timeline:**

```
0:00  → User requests password reset
0:00  → OTP generated, expires at 0:10
0:10  → OTP expires (can't be used)
0:15  → Background service runs
0:15  → Expired record DELETED from database ✅
```

---

## 🔧 **Background Service Details**

### **Service Configuration:**

```csharp
// Runs automatically in background
public class ForgotPasswordCleanupBackgroundService : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(15);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
 while (!stoppingToken.IsCancellationRequested)
        {
     // Delete expired and used requests
            await CleanupExpiredRequestsAsync();
        
 // Wait 15 minutes
  await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
}
```

### **What Gets Deleted:**

1. ✅ **Expired requests** (`expires_at < NOW()`)
2. ✅ **Used requests** (`is_used = 1`)

### **What Gets Kept:**

1. ⏳ **Active requests** (not expired, not used)
2. ⏳ **Recent requests** (within 10 minutes)

---

## 📊 **Database Cleanup Logic**

### **SQL Query Executed:**

```sql
-- This runs every 15 minutes automatically
DELETE FROM forgot_password_requests
WHERE 
    is_used = 1-- Already used
    OR expires_at < UTC_TIMESTAMP()  -- Expired
;
```

### **Before Cleanup:**
```sql
SELECT * FROM forgot_password_requests;
```

```
+----+-------------------+--------+--------+-----------+---------------------+---------------------+
| id | email             | otp    | is_used| user_type | expires_at  | created_at          |
+----+-------------------+--------+--------+-----------+---------------------+---------------------+
|  1 | old@example.com | 123456 |   1    | user      | 2025-01-14 10:00:00 | 2025-01-14 09:50:00 |  ← Will DELETE (used)
|  2 | expired@test.com  | 789456 |   0    | user      | 2025-01-14 10:05:00 | 2025-01-14 09:55:00 |  ← Will DELETE (expired)
|  3 | active@test.com   | 456789 |   0  | subuser   | 2025-01-14 10:20:00 | 2025-01-14 10:10:00 |  ← Will KEEP (active)
+----+-------------------+--------+--------+-----------+---------------------+---------------------+
```

### **After Cleanup:**
```sql
SELECT * FROM forgot_password_requests;
```

```
+----+-------------------+--------+--------+-----------+---------------------+---------------------+
| id | email             | otp    | is_used| user_type | expires_at          | created_at          |
+----+-------------------+--------+--------+-----------+---------------------+---------------------+
|  3 | active@test.com   | 456789 |   0    | subuser   | 2025-01-14 10:20:00 | 2025-01-14 10:10:00 |  ✅ KEPT
+----+-------------------+--------+--------+-----------+---------------------+---------------------+
```

---

## 🚀 **API Response Changes**

### **Request Password Reset:**

**Request:**
```http
POST http://localhost:4000/api/forgot/request
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response (Updated):**
```json
{
  "success": true,
  "message": "Password reset code generated successfully for User. Use the OTP and reset link below.",
  "otp": "542183",
  "resetToken": "abc123xyz456...",
  "expiresAt": "2025-01-14T10:10:00Z",
  "expiryMinutes": 10  // ✅ Changed from 5 to 10
}
```

---

## 🧪 **Testing**

### **Test 1: Verify 10-Minute Expiry**

```sql
-- 1. Request password reset via API

-- 2. Check expiry time
SELECT 
    email,
  created_at,
    expires_at,
    TIMESTAMPDIFF(MINUTE, created_at, expires_at) as expiry_duration
FROM forgot_password_requests
WHERE email = 'test@example.com'
ORDER BY created_at DESC LIMIT 1;

-- Expected: expiry_duration = 10 minutes
```

### **Test 2: Verify Auto-Delete**

```sql
-- 1. Create expired request manually
INSERT INTO forgot_password_requests 
(user_id, email, user_type, otp, reset_token, is_used, expires_at, created_at)
VALUES 
(1, 'expired@test.com', 'user', '123456', 'token123', 0, 
 DATE_SUB(NOW(), INTERVAL 1 MINUTE),  -- Expired 1 minute ago
 DATE_SUB(NOW(), INTERVAL 11 MINUTE)); -- Created 11 minutes ago

-- 2. Check it exists
SELECT * FROM forgot_password_requests WHERE email = 'expired@test.com';
-- Should show the record

-- 3. Wait 15 minutes for background service to run
-- (Or manually trigger cleanup via Admin endpoint)

-- 4. Check again
SELECT * FROM forgot_password_requests WHERE email = 'expired@test.com';
-- Should be EMPTY (auto-deleted)
```

### **Test 3: Manual Cleanup Trigger**

```http
POST http://localhost:4000/api/forgot/cleanup
Authorization: Bearer <admin-token>
```

**Response:**
```json
{
  "success": true,
  "message": "Expired password reset requests cleaned up successfully."
}
```

---

## 📋 **Background Service Logs**

### **Startup Log:**
```
🧹 Forgot Password Cleanup Background Service started
```

### **Cleanup Cycle Logs:**
```
🧹 Starting automatic cleanup of expired password reset requests...
Deleted 5 expired forgot password requests
✅ Automatic cleanup completed
```

### **Error Log (if any):**
```
❌ Error during forgot password cleanup: [Error details]
```

### **Shutdown Log:**
```
🛑 Forgot Password Cleanup Background Service is stopping...
🛑 Forgot Password Cleanup Background Service stopped
```

---

## 🔍 **Monitoring**

### **Check Background Service Status:**

```sql
-- See what will be deleted in next cleanup
SELECT 
    COUNT(*) as will_be_deleted,
    SUM(CASE WHEN is_used = 1 THEN 1 ELSE 0 END) as used_count,
    SUM(CASE WHEN expires_at < NOW() THEN 1 ELSE 0 END) as expired_count
FROM forgot_password_requests
WHERE is_used = 1 OR expires_at < NOW();
```

### **Check Active Requests:**

```sql
-- See what will be kept
SELECT 
    email,
    user_type,
    created_at,
    expires_at,
    TIMESTAMPDIFF(MINUTE, NOW(), expires_at) as minutes_left
FROM forgot_password_requests
WHERE is_used = 0 AND expires_at > NOW()
ORDER BY created_at DESC;
```

---

## ⚙️ **Configuration Options**

### **Change Cleanup Interval:**

Edit `ForgotPasswordCleanupBackgroundService.cs`:

```csharp
// Current: Runs every 15 minutes
private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(15);

// Options:
// Every 5 minutes:  TimeSpan.FromMinutes(5)
// Every 30 minutes: TimeSpan.FromMinutes(30)
// Every hour:   TimeSpan.FromHours(1)
```

### **Change OTP Expiry Time:**

Edit `ForgotPasswordService.cs`:

```csharp
// Current: 10 minutes
DateTime expiresAt = DateTime.UtcNow.AddMinutes(10);

// Options:
// 5 minutes:  AddMinutes(5)
// 15 minutes: AddMinutes(15)
// 30 minutes: AddMinutes(30)
```

---

## 🎯 **Benefits**

### **✅ Auto-Cleanup:**
- No manual database maintenance needed
- Keeps database clean automatically
- Removes old data every 15 minutes

### **✅ Extended Time:**
- 10 minutes gives users more time
- Reduces "OTP expired" errors
- Better user experience

### **✅ Performance:**
- Less database bloat
- Faster queries (fewer records)
- Automatic garbage collection

---

## 📊 **Comparison**

| Feature | Before | After |
|---------|--------|-------|
| **OTP Expiry** | 5 minutes | 10 minutes ✅ |
| **Cleanup** | Manual (Admin only) | Automatic ✅ |
| **Cleanup Frequency** | On-demand | Every 15 minutes ✅ |
| **Database Growth** | Unlimited | Controlled ✅ |
| **Maintenance** | Required | None ✅ |

---

## 🐛 **Troubleshooting**

### **Issue: Background service not running**

**Check logs for:**
```
🧹 Forgot Password Cleanup Background Service started
```

**If missing, verify:**
```csharp
// In Program.cs
builder.Services.AddHostedService<ForgotPasswordCleanupBackgroundService>();
```

### **Issue: Records not being deleted**

**Check:**
```sql
-- Are there any records to delete?
SELECT * FROM forgot_password_requests
WHERE is_used = 1 OR expires_at < NOW();

-- If empty, cleanup is working!
```

### **Issue: Want to force cleanup now**

**Option 1: Admin Endpoint**
```http
POST http://localhost:4000/api/forgot/cleanup
Authorization: Bearer <admin-token>
```

**Option 2: Manual SQL**
```sql
DELETE FROM forgot_password_requests
WHERE is_used = 1 OR expires_at < NOW();
```

---

## ✅ **Summary**

### **Changes Made:**

1. ✅ **Expiry time increased** from 5 to 10 minutes
2. ✅ **Background service added** for auto-cleanup
3. ✅ **Cleanup runs every 15 minutes** automatically
4. ✅ **Expired and used records deleted** automatically
5. ✅ **Database stays clean** without manual intervention

### **Files Modified:**

```
✅ Services/ForgotPasswordService.cs - 10-minute expiry
✅ Models/DTOs/ForgotPasswordDTOs.cs - Updated default
✅ Models/ForgotPasswordRequest.cs - Updated comments
✅ Controllers/ForgotPasswordApiController.cs - Updated docs
✅ BackgroundServices/ForgotPasswordCleanupBackgroundService.cs - NEW
✅ Program.cs - Registered background service
```

---

## 🎊 **Result**

### **User Experience:**
```
User requests OTP → Gets 10 minutes to use it → Better UX ✅
```

### **Database:**
```
Records created → Auto-deleted after 15 minutes → Clean DB ✅
```

### **Admin:**
```
No manual cleanup needed → Service runs automatically → Less work ✅
```

---

**Perfect! Your Forgot Password system now has 10-minute expiry and automatic cleanup!** 🎉🚀
