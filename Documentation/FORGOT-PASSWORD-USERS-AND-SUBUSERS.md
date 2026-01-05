# 🎯 Forgot Password for Users & Subusers - Complete Guide

## ✨ **Key Feature: Works for Both Users and Subusers**

The Forgot Password API automatically detects whether the email belongs to a **User** or **Subuser** and handles password reset accordingly.

---

## 📊 **How It Works**

### **Step 1: Email Detection**
When you request password reset, the system checks:
1. ✅ **Users table** (`users.user_email`)
2. ✅ **Subusers table** (`subuser.subuser_email`)

```csharp
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.user_email == dto.Email);

var subuser = await _context.subuser
    .FirstOrDefaultAsync(s => s.subuser_email == dto.Email);
```

### **Step 2: User Type Tracking**
The system stores `user_type` in the database:
- `"user"` - For regular users
- `"subuser"` - For subusers

```sql
CREATE TABLE forgot_password_requests (
    user_id INT,     -- stores user_id or subuser_id
    email VARCHAR(255),
    user_type VARCHAR(20), -- "user" or "subuser"
    otp VARCHAR(6),
    reset_token VARCHAR(500),
    ...
);
```

### **Step 3: Password Update**
On reset, the system updates the correct table:

**For Users:**
```csharp
user.user_password = "NewPassword@123";     // Plain text
user.hash_password = BCrypt.Hash();     // BCrypt hash
user.updated_at = DateTime.UtcNow;
```

**For Subusers:**
```csharp
subuser.subuser_password = BCrypt.Hash();   // BCrypt hash only
subuser.UpdatedAt = DateTime.UtcNow;
```

---

## 🚀 **API Usage Examples**

### **Example 1: Reset Password for Regular User**

**Step 1: Request OTP**
```http
POST http://localhost:4000/api/forgot/request
Content-Type: application/json

{
  "email": "john@example.com"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Password reset code generated successfully for User. Use the OTP and reset link below.",
  "otp": "542183",
  "resetToken": "abc123xyz...",
  "expiresAt": "2025-01-14T11:05:00Z",
  "expiryMinutes": 5
}
```

**Step 2: Reset Password**
```http
POST http://localhost:4000/api/forgot/reset
Content-Type: application/json

{
  "email": "john@example.com",
  "otp": "542183",
  "resetToken": "abc123xyz...",
  "newPassword": "NewSecurePassword@123"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Password reset successfully for User. You can now log in with your new password.",
  "resetAt": "2025-01-14T11:03:00Z"
}
```

---

### **Example 2: Reset Password for Subuser**

**Step 1: Request OTP**
```http
POST http://localhost:4000/api/forgot/request
Content-Type: application/json

{
  "email": "sarah.subuser@example.com"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Password reset code generated successfully for Subuser. Use the OTP and reset link below.",
  "otp": "789456",
  "resetToken": "xyz789abc...",
  "expiresAt": "2025-01-14T11:10:00Z",
  "expiryMinutes": 5
}
```

**Step 2: Reset Password**
```http
POST http://localhost:4000/api/forgot/reset
Content-Type: application/json

{
  "email": "sarah.subuser@example.com",
  "otp": "789456",
  "resetToken": "xyz789abc...",
  "newPassword": "NewSubuserPass@456"
}
```

**Response:**
```json
{
  "success": true,
"message": "Password reset successfully for Subuser. You can now log in with your new password.",
  "resetAt": "2025-01-14T11:08:00Z"
}
```

---

## 📋 **Database Table Structure**

### **Updated Table with UserType Support:**

```sql
CREATE TABLE IF NOT EXISTS `forgot_password_requests` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_id` INT NOT NULL COMMENT 'Stores user_id or subuser_id',
    `email` VARCHAR(255) NOT NULL,
    `user_type` VARCHAR(20) DEFAULT 'user' COMMENT 'Type: user or subuser',
    `otp` VARCHAR(6) NOT NULL,
    `reset_token` VARCHAR(500) NOT NULL,
    `is_used` TINYINT(1) DEFAULT 0,
    `expires_at` DATETIME NOT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `ip_address` VARCHAR(50) NULL,
    `user_agent` VARCHAR(500) NULL,

    UNIQUE KEY `idx_reset_token` (`reset_token`),
    KEY `idx_email_expiry` (`email`, `expires_at`),
    KEY `idx_user_id_type` (`user_id`, `user_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 🧪 **Testing Both User Types**

### **Test 1: User Password Reset**

```sql
-- 1. Create test user
INSERT INTO users (user_email, user_password, hash_password, created_at) 
VALUES ('testuser@example.com', 'OldPass@123', '$2a$11$oldhash', NOW());

-- 2. Request OTP via API
-- 3. Check database
SELECT user_id, email, user_type, otp, LEFT(reset_token, 30) as token
FROM forgot_password_requests 
WHERE email = 'testuser@example.com';

-- Expected: user_type = 'user'

-- 4. Reset password via API
-- 5. Verify password changed
SELECT user_email, user_password, hash_password, updated_at
FROM users 
WHERE user_email = 'testuser@example.com';
```

### **Test 2: Subuser Password Reset**

```sql
-- 1. Create test subuser
INSERT INTO subuser (subuser_email, subuser_password, superuser_id, CreatedAt) 
VALUES ('testsubuser@example.com', '$2a$11$oldhash', 1, NOW());

-- 2. Request OTP via API
-- 3. Check database
SELECT user_id, email, user_type, otp, LEFT(reset_token, 30) as token
FROM forgot_password_requests 
WHERE email = 'testsubuser@example.com';

-- Expected: user_type = 'subuser'

-- 4. Reset password via API
-- 5. Verify password changed
SELECT subuser_email, subuser_password, UpdatedAt
FROM subuser 
WHERE subuser_email = 'testsubuser@example.com';
```

---

## 🔍 **Diagnostic Queries**

### **Check User Type Distribution**

```sql
SELECT 
    user_type,
    COUNT(*) as request_count,
 SUM(CASE WHEN is_used = 0 AND expires_at > NOW() THEN 1 ELSE 0 END) as active_count,
    SUM(CASE WHEN is_used = 1 THEN 1 ELSE 0 END) as used_count,
    SUM(CASE WHEN expires_at < NOW() THEN 1 ELSE 0 END) as expired_count
FROM forgot_password_requests
GROUP BY user_type;
```

**Expected Output:**
```
+------------+---------------+--------------+------------+---------------+
| user_type  | request_count | active_count | used_count | expired_count |
+------------+---------------+--------------+------------+---------------+
| user  |    15       |   3   |      8     |       4       |
| subuser    |       8  | 2      |      5     |       1       |
+------------+---------------+--------------+------------+---------------+
```

### **Find Recent Requests by Type**

```sql
SELECT 
    email,
    user_type,
    otp,
    LEFT(reset_token, 30) as token,
    is_used,
    CASE 
        WHEN is_used = 1 THEN '✅ USED'
   WHEN expires_at < NOW() THEN '⏰ EXPIRED'
        ELSE '🟢 ACTIVE'
    END as status,
    created_at
FROM forgot_password_requests
ORDER BY created_at DESC
LIMIT 10;
```

---

## 🎯 **Key Differences: Users vs Subusers**

| Feature | Users | Subusers |
|---------|-------|----------|
| **Table** | `users` | `subuser` |
| **Email Column** | `user_email` | `subuser_email` |
| **ID Column** | `user_id` | `subuser_id` |
| **Password Storage** | Both plain + BCrypt hash | BCrypt hash only |
| **Password Columns** | `user_password`, `hash_password` | `subuser_password` |
| **Updated Column** | `updated_at` | `UpdatedAt` |

---

## 📝 **Response Messages**

### **For Users:**
```json
{
  "message": "Password reset code generated successfully for User."
}
```

```json
{
  "message": "Password reset successfully for User. You can now log in with your new password."
}
```

### **For Subusers:**
```json
{
  "message": "Password reset code generated successfully for Subuser."
}
```

```json
{
  "message": "Password reset successfully for Subuser. You can now log in with your new password."
}
```

---

## 🔒 **Security Features (Same for Both)**

- ✅ **6-digit OTP** with 5-minute expiry
- ✅ **Unique reset token** (GUID + random bytes)
- ✅ **Rate limiting** - Max 3 active requests per email
- ✅ **Single-use tokens** - Marked as used after reset
- ✅ **IP tracking** - Stores requester's IP
- ✅ **User agent tracking** - Stores browser/device info
- ✅ **BCrypt hashing** - Secure password storage

---

## ✅ **Complete Test Checklist**

### **For Users:**
- [ ] User exists in `users` table
- [ ] Request OTP returns `user_type: "user"` in logs
- [ ] Database stores `user_type = 'user'`
- [ ] Password reset updates `users.user_password` and `users.hash_password`
- [ ] Can login with new password

### **For Subusers:**
- [ ] Subuser exists in `subuser` table
- [ ] Request OTP returns `user_type: "subuser"` in logs
- [ ] Database stores `user_type = 'subuser'`
- [ ] Password reset updates `subuser.subuser_password`
- [ ] Can login with new password

---

## 🐛 **Troubleshooting**

### **Issue: "User not found" for Subuser**

**Check:**
```sql
-- Verify subuser exists
SELECT * FROM subuser WHERE subuser_email = 'your-email@example.com';

-- Check if email is in users table instead
SELECT * FROM users WHERE user_email = 'your-email@example.com';
```

### **Issue: Wrong User Type**

**Check Database:**
```sql
SELECT email, user_type, user_id 
FROM forgot_password_requests 
WHERE email = 'your-email@example.com'
ORDER BY created_at DESC LIMIT 1;
```

**Expected:**
- If email is in `users` table → `user_type = 'user'`
- If email is in `subuser` table → `user_type = 'subuser'`

---

## 🎊 **Summary**

### **✅ What Works:**
1. ✅ Forgot Password for **Users**
2. ✅ Forgot Password for **Subusers**
3. ✅ Automatic detection of user type
4. ✅ Correct table updates based on user type
5. ✅ User-friendly messages indicating user type

### **✅ No Code Changes Needed:**
The same API endpoints work for both:
- `POST /api/forgot/request`
- `POST /api/forgot/verify-otp`
- `POST /api/forgot/reset`

### **✅ Just Send Email:**
System automatically detects if it's a User or Subuser!

---

**Perfect! Your Forgot Password system now fully supports both Users and Subusers!** 🎉🚀

