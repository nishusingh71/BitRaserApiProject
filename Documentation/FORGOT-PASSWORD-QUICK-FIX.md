# 🔥 QUICK FIX: Forgot Password API Setup

## ❌ **Current Error:**
```json
{
  "success": false,
  "message": "An error occurred while processing your request."
}
```

## ✅ **Root Cause:**
Database table `forgot_password_requests` doesn't exist yet!

---

## 🚀 **SOLUTION - 2 Steps:**

### **Step 1: Create Database Table**

**Option A: Run SQL Manually in TiDB**
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
    KEY `idx_email_expiry` (`email`, `expires_at`),
    KEY `idx_user_id` (`user_id`),
    
    CONSTRAINT `fk_forgot_password_user` 
      FOREIGN KEY (`user_id`) 
        REFERENCES `users` (`user_id`) 
        ON DELETE CASCADE
        
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

**Option B: Run SQL File**
```bash
# Navigate to Database folder
cd Database

# Execute SQL file in TiDB
mysql -h your-tidb-host -P 4000 -u your-user -p your-database < create_forgot_password_table.sql
```

**Option C: Without Foreign Key (if foreign key fails)**
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

### **Step 2: Test API**

**1. Request Password Reset:**
```http
POST http://localhost:5000/api/forgot/request
Content-Type: application/json

{
  "email": "test@example.com"
}
```

**Expected Success Response:**
```json
{
  "success": true,
  "message": "Password reset code generated successfully. Use the OTP and reset link below.",
  "otp": "542183",
  "resetLink": "http://localhost:5000/reset-password?token=abc123...",
  "resetToken": "abc123...",
  "expiresAt": "2025-01-14T10:35:00Z",
  "expiryMinutes": 5
}
```

---

## 📊 **Verify Table Creation**

```sql
SHOW TABLES LIKE 'forgot_password_requests';

DESCRIBE forgot_password_requests;

SELECT COUNT(*) FROM forgot_password_requests;
```

---

## ✅ **Success Checklist**

- [ ] Database table created
- [ ] POST `/api/forgot/request` returns success with OTP
- [ ] OTP and reset token received in response
- [ ] Table has data: `SELECT * FROM forgot_password_requests;`

---

**After creating the table, your API will work perfectly!** ✅🚀
