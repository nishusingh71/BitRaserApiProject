# 🔥 URGENT FIX: "Invalid email or OTP" Error

## Current Error:
```json
{
  "success": false,
  "message": "Invalid email or OTP.",
  "resetAt": "0001-01-01T00:00:00"
}
```

---

## 🎯 Most Likely Cause: **Table Doesn't Exist**

### Quick Test:
```sql
SHOW TABLES LIKE 'forgot_password_requests';
```

**If empty** → Table doesn't exist → Run this:

```sql
CREATE TABLE IF NOT EXISTS `forgot_password_requests` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_id` INT NOT NULL,
    `email` VARCHAR(255) NOT NULL,
    `otp` VARCHAR(6) NOT NULL,
    `reset_token` VARCHAR(500) NOT NULL,
    `is_used` TINYINT(1) DEFAULT 0,
`expires_at` DATETIME NOT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `ip_address` VARCHAR(50) NULL,
    `user_agent` VARCHAR(500) NULL,
    
    UNIQUE KEY `idx_reset_token` (`reset_token`),
    KEY `idx_email_expiry` (`email`, `expires_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## ✅ Complete Test Flow

### 1️⃣ Request Password Reset
```http
POST http://localhost:4000/api/forgot/request
Content-Type: application/json

{
  "email": "test@example.com"
}
```

**Copy the response:**
- `otp`: "542183"
- `resetToken`: "abc123..."

### 2️⃣ Verify Request in Database
```sql
SELECT email, otp, reset_token, is_used, expires_at 
FROM forgot_password_requests 
WHERE email = 'test@example.com'
ORDER BY created_at DESC LIMIT 1;
```

### 3️⃣ Reset Password
```http
POST http://localhost:4000/api/forgot/reset
Content-Type: application/json

{
  "email": "test@example.com",
  "otp": "542183",
  "resetToken": "abc123...",
  "newPassword": "NewPassword@123"
}
```

---

## 🐛 If Still Failing:

### Check These:
1. **OTP matches exactly** (case-sensitive)
2. **Email matches exactly** (case-sensitive)
3. **Token matches exactly**
4. **Request not expired** (5 minutes)
5. **Request not already used**

### Debug Query:
```sql
-- Find your exact record
SELECT 
    email,
    otp,
    LEFT(reset_token, 30) as token,
    is_used,
    expires_at,
    CASE 
        WHEN is_used = 1 THEN 'ALREADY USED'
        WHEN expires_at < NOW() THEN 'EXPIRED'
  ELSE 'VALID'
    END as status
FROM forgot_password_requests
WHERE email = 'test@example.com'
ORDER BY created_at DESC LIMIT 1;
```

---

## 🚀 Start Fresh:

```sql
-- 1. Clean up old requests
DELETE FROM forgot_password_requests WHERE email = 'test@example.com';

-- 2. Ensure test user exists
INSERT IGNORE INTO users (user_email, user_password, hash_password, created_at) 
VALUES ('test@example.com', 'Old@123', '$2a$11$hash', NOW());

-- 3. Request new OTP via API (Step 1 above)

-- 4. Use exact OTP/token from response (Step 3 above)
```

---

## 📞 Need More Help?

Run complete diagnostics:
```bash
# Open Database folder
cd Database

# Run diagnostic script
mysql -h your-host -u your-user -p your-database < diagnostic_forgot_password.sql
```

Or check the complete testing guide:
- `Documentation/FORGOT-PASSWORD-TESTING-GUIDE.md`

---

**TL;DR:** Create the table, request OTP, use exact values from response! ✅
