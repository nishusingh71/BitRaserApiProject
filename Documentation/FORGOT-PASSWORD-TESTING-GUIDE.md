# 🧪 Forgot Password API - Complete Testing Guide

## ❌ Current Error Analysis

```json
{
  "success": false,
  "message": "Invalid email or OTP.",
  "resetAt": "0001-01-01T00:00:00"
}
```

### Root Cause:
The service is trying to validate `email + OTP + resetToken` but can't find a matching record. This could be due to:

1. **Table doesn't exist** (most likely)
2. **OTP/Token mismatch** (case sensitivity, whitespace)
3. **Record already used** (`IsUsed = true`)
4. **Record expired** (`ExpiresAt < DateTime.UtcNow`)
5. **Wrong email address**

---

## ✅ Step-by-Step Testing Process

### **STEP 1: Verify Database Table Exists**

Run this SQL to check:

```sql
-- Check if table exists
SHOW TABLES LIKE 'forgot_password_requests';

-- Check table structure
DESCRIBE forgot_password_requests;

-- Check current records
SELECT * FROM forgot_password_requests ORDER BY created_at DESC LIMIT 5;
```

**Expected Result:**
- Table should exist with columns: `id, user_id, email, otp, reset_token, is_used, expires_at, created_at, ip_address, user_agent`

**If table doesn't exist**, run this:

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

### **STEP 2: Complete API Flow Test**

#### **Test 2.1: Request Password Reset**

```http
POST http://localhost:4000/api/forgot/request
Content-Type: application/json

{
  "email": "test@example.com"
}
```

**✅ Expected Success Response:**
```json
{
  "success": true,
"message": "Password reset code generated successfully. Use the OTP and reset link below.",
  "otp": "542183",
  "resetLink": "http://localhost:4000/reset-password?token=abc123xyz456...",
  "resetToken": "abc123xyz456...",
  "expiresAt": "2025-01-14T11:05:00Z",
  "expiryMinutes": 5
}
```

**❌ Common Errors:**

1. **"An error occurred while processing your request"**
   - Table doesn't exist → Run CREATE TABLE SQL above

2. **"If this email exists, you will receive a password reset code"**
   - Email doesn't exist in `users` or `subuser` tables
   - Add test user:
   ```sql
   INSERT INTO users (user_email, user_password, hash_password, created_at) 
   VALUES ('test@example.com', 'Test@123', '$2a$11$hash...', NOW());
   ```

3. **"Too many active reset requests"**
   - Clean up old requests:
   ```sql
   DELETE FROM forgot_password_requests WHERE email = 'test@example.com';
   ```

---

#### **Test 2.2: Verify Database Record Created**

```sql
SELECT 
    id,
    email,
    otp,
    LEFT(reset_token, 20) as token_preview,
    is_used,
    expires_at,
    created_at,
    TIMESTAMPDIFF(MINUTE, NOW(), expires_at) as minutes_remaining
FROM forgot_password_requests 
WHERE email = 'test@example.com'
ORDER BY created_at DESC 
LIMIT 1;
```

**Expected Result:**
```
+----+-------------------+--------+----------------------+---------+---------------------+---------------------+-------------------+
| id | email          | otp    | token_preview        | is_used | expires_at          | created_at          | minutes_remaining |
+----+-------------------+--------+----------------------+---------+---------------------+---------------------+-------------------+
|  1 | test@example.com  | 542183 | abc123xyz456...|   0 | 2025-01-14 11:05:00 | 2025-01-14 11:00:00 |       5 |
+----+-------------------+--------+----------------------+---------+---------------------+---------------------+-------------------+
```

**⚠️ Important Checks:**
- `is_used` should be `0`
- `minutes_remaining` should be positive (not expired)

---

#### **Test 2.3: Verify OTP (Optional)**

```http
POST http://localhost:4000/api/forgot/verify-otp
Content-Type: application/json

{
  "email": "test@example.com",
  "otp": "542183"
}
```

**✅ Success Response:**
```json
{
  "success": true,
  "isValid": true,
  "message": "OTP verified successfully.",
  "email": "test@example.com"
}
```

**❌ Error Responses:**

```json
{
  "success": false,
  "isValid": false,
  "message": "Invalid or expired OTP."
}
```

**Common Issues:**
1. **OTP mismatch** - Check exact OTP from Step 2.1
2. **OTP expired** - Request new OTP (expires in 5 minutes)
3. **OTP already used** - Request new OTP

**Debug Query:**
```sql
SELECT email, otp, is_used, expires_at > NOW() as is_valid
FROM forgot_password_requests 
WHERE email = 'test@example.com'
ORDER BY created_at DESC LIMIT 1;
```

---

#### **Test 2.4: Reset Password**

```http
POST http://localhost:4000/api/forgot/reset
Content-Type: application/json

{
  "email": "test@example.com",
  "otp": "542183",
  "resetToken": "abc123xyz456...",
  "newPassword": "NewPassword@123"
}
```

**✅ Success Response:**
```json
{
  "success": true,
  "message": "Password reset successfully. You can now log in with your new password.",
  "resetAt": "2025-01-14T11:03:00Z"
}
```

**❌ Error Responses:**

1. **"Invalid email or OTP."**
   ```json
   {
     "success": false,
  "message": "Invalid email or OTP.",
"resetAt": "0001-01-01T00:00:00"
   }
   ```
   **Causes:**
   - Wrong email
   - Wrong OTP
   - OTP expired
   - OTP already used

2. **"Invalid reset token."**
   ```json
   {
     "success": false,
     "message": "Invalid reset token.",
     "resetAt": "0001-01-01T00:00:00"
   }
   ```
   **Causes:**
   - Token doesn't match
   - Token has whitespace or incorrect characters

3. **"User not found."**
   - Email exists in `forgot_password_requests` but not in `users` or `subuser` tables

---

### **STEP 3: Diagnostic Queries**

#### **Query 3.1: Check Active Requests**

```sql
SELECT 
    id,
    email,
    otp,
    LEFT(reset_token, 30) as token,
    is_used,
    expires_at,
    CASE 
        WHEN is_used = 1 THEN 'USED'
        WHEN expires_at < NOW() THEN 'EXPIRED'
        ELSE 'ACTIVE'
    END as status,
    TIMESTAMPDIFF(MINUTE, NOW(), expires_at) as minutes_left
FROM forgot_password_requests
WHERE email = 'test@example.com'
ORDER BY created_at DESC;
```

#### **Query 3.2: Simulate Service Query**

```sql
-- This is what the service runs to find the record
SELECT *
FROM forgot_password_requests
WHERE email = 'test@example.com'
  AND otp = '542183'  -- Replace with your OTP
  AND is_used = 0
  AND expires_at > NOW()
LIMIT 1;
```

**If this returns empty:**
- OTP is wrong
- Email is wrong
- Record is expired
- Record is already used

#### **Query 3.3: Check User Exists**

```sql
-- Check in users table
SELECT user_id, user_email, user_password, hash_password 
FROM users 
WHERE user_email = 'test@example.com';

-- Check in subuser table
SELECT subuser_id, subuser_email, subuser_password 
FROM subuser 
WHERE subuser_email = 'test@example.com';
```

---

### **STEP 4: Clean Slate Test**

If you're still having issues, start fresh:

```sql
-- 1. Delete all old requests
DELETE FROM forgot_password_requests WHERE email = 'test@example.com';

-- 2. Ensure user exists
INSERT IGNORE INTO users (user_email, user_password, hash_password, created_at) 
VALUES ('test@example.com', 'OldPassword@123', '$2a$11$oldHash', NOW());

-- 3. Verify user exists
SELECT * FROM users WHERE user_email = 'test@example.com';
```

Then run the complete flow again from Step 2.1.

---

## 🔧 Common Issues & Fixes

### **Issue 1: Table Doesn't Exist**
```
Table 'your_db.forgot_password_requests' doesn't exist
```

**Fix:**
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

### **Issue 2: OTP Expires Too Quickly**

**Current:** OTP expires in 5 minutes

**To extend to 15 minutes**, update `ForgotPasswordService.cs`:

```csharp
// Change this line:
DateTime expiresAt = DateTime.UtcNow.AddMinutes(15); // Changed from 5 to 15
```

---

### **Issue 3: Case Sensitivity**

Email and OTP are case-sensitive. Ensure:
- Email: `test@example.com` (lowercase)
- OTP: `542183` (exact digits)

---

### **Issue 4: Timezone Issues**

If `expires_at` uses local time but service uses `DateTime.UtcNow`:

```sql
-- Check timezone mismatch
SELECT 
    expires_at,
    NOW() as local_now,
    UTC_TIMESTAMP() as utc_now,
    expires_at > NOW() as valid_local,
    expires_at > UTC_TIMESTAMP() as valid_utc
FROM forgot_password_requests
ORDER BY created_at DESC LIMIT 1;
```

**Fix in `ApplicationDbContext.cs`:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<ForgotPasswordRequest>()
        .Property(f => f.ExpiresAt)
    .HasConversion(
      v => v.ToUniversalTime(),
      v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
```

---

## 📝 Complete Working Example

### **1. Create User:**
```sql
INSERT INTO users (user_email, user_password, hash_password, created_at) 
VALUES ('john@example.com', 'OldPass@123', '$2a$11$xyz', NOW());
```

### **2. Request Reset:**
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
  "otp": "789456",
  "resetToken": "a1b2c3d4e5f6...",
  "expiresAt": "2025-01-14T11:10:00Z"
}
```

### **3. Reset Password:**
```http
POST http://localhost:4000/api/forgot/reset
Content-Type: application/json

{
  "email": "john@example.com",
  "otp": "789456",
  "resetToken": "a1b2c3d4e5f6...",
  "newPassword": "NewSecure@456"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Password reset successfully. You can now log in with your new password.",
  "resetAt": "2025-01-14T11:05:00Z"
}
```

### **4. Verify Password Changed:**
```sql
SELECT user_email, user_password, hash_password, updated_at
FROM users 
WHERE user_email = 'john@example.com';
```

---

## ✅ Success Checklist

- [ ] Table `forgot_password_requests` exists
- [ ] User exists in `users` or `subuser` table
- [ ] Request returns `success: true` with OTP and token
- [ ] Database record created with correct values
- [ ] OTP verification works
- [ ] Password reset works
- [ ] Password actually changes in database
- [ ] Can login with new password

---

## 🚨 Emergency Debug Mode

Add this to `ForgotPasswordService.cs` in `ResetPasswordAsync()`:

```csharp
public async Task<ResetPasswordResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
{
    try
    {
     // ⚠️ DEBUG: Log input values
        _logger.LogInformation("🔍 DEBUG - Email: {Email}, OTP: {Otp}, Token: {Token}", 
 dto.Email, dto.Otp, dto.ResetToken?.Substring(0, Math.Min(20, dto.ResetToken.Length)));

        // Step 1: Validate OTP + Token
var request = await _repository.GetByEmailAndOtpAsync(dto.Email, dto.Otp);

        if (request == null)
        {
    // ⚠️ DEBUG: Check why query failed
          var allRequests = await _context.ForgotPasswordRequests
                .Where(f => f.Email == dto.Email)
 .ToListAsync();
     
            _logger.LogWarning("🔍 DEBUG - Found {Count} requests for {Email}", 
     allRequests.Count, dto.Email);
            
  foreach (var r in allRequests)
            {
                _logger.LogWarning("🔍 DEBUG - Request: OTP={Otp}, IsUsed={IsUsed}, Expired={Expired}", 
            r.Otp, r.IsUsed, r.ExpiresAt < DateTime.UtcNow);
         }

  return new ResetPasswordResponseDto
      {
         Success = false,
         Message = "Invalid email or OTP."
            };
    }

        // ... rest of the code
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error resetting password");
    return new ResetPasswordResponseDto
    {
        Success = false,
            Message = $"Error: {ex.Message}" // ⚠️ Only in development
        };
    }
}
```

---

**Once you follow these steps, the issue will be resolved! Let me know which step fails and I can help further.** 🚀
