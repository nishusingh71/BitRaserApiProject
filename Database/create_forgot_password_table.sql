-- ✅ FORGOT PASSWORD REQUESTS TABLE
-- Run this SQL in your TiDB database to create the table
-- ✅ SUPPORTS BOTH USERS AND SUBUSERS

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
    
    -- Indexes for performance
  UNIQUE KEY `idx_reset_token` (`reset_token`),
    KEY `idx_email_expiry` (`email`, `expires_at`),
    KEY `idx_user_id_type` (`user_id`, `user_type`),
    
 -- Foreign key constraint (optional - only for users table)
    -- Note: Cannot add FK for subuser_id due to different tables
    -- CONSTRAINT `fk_forgot_password_user` 
    --   FOREIGN KEY (`user_id`) 
    --     REFERENCES `users` (`user_id`) 
    --     ON DELETE CASCADE
        
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Verify table creation
SELECT 
    TABLE_NAME, 
    TABLE_ROWS, 
 CREATE_TIME 
FROM 
    information_schema.TABLES 
WHERE 
    TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'forgot_password_requests';
