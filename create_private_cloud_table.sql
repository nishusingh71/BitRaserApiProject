-- Create private_cloud_databases table if it doesn't exist
-- Run this in your MAIN database (where users table exists)

CREATE TABLE IF NOT EXISTS `private_cloud_databases` (
  `config_id` INT AUTO_INCREMENT PRIMARY KEY,
  `user_id` INT NOT NULL,
  `user_email` VARCHAR(255) NOT NULL,
  `connection_string` TEXT NOT NULL COMMENT 'Encrypted connection string',
  `database_type` VARCHAR(50) NOT NULL DEFAULT 'mysql',
  `server_host` VARCHAR(255),
  `server_port` INT DEFAULT 3306,
  `database_name` VARCHAR(255) NOT NULL,
  `database_username` VARCHAR(255) NOT NULL,
  `is_active` BOOLEAN DEFAULT TRUE,
  `last_tested_at` DATETIME,
  `test_status` VARCHAR(50) DEFAULT 'pending',
  `test_error` TEXT,
  `schema_initialized` BOOLEAN DEFAULT FALSE,
  `schema_initialized_at` DATETIME,
  `schema_version` VARCHAR(50) DEFAULT '1.0.0',
  `storage_used_mb` DECIMAL(10,2) DEFAULT 0,
  `storage_limit_mb` DECIMAL(10,2),
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `created_by` VARCHAR(255),
  `settings_json` JSON,
  `notes` VARCHAR(500),
  CONSTRAINT fk_private_cloud_user FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`) ON DELETE CASCADE,
  UNIQUE KEY `user_email_unique` (`user_email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Private Cloud Database Configurations';

-- Verify table created
SELECT TABLE_NAME, TABLE_COMMENT 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() 
AND TABLE_NAME = 'private_cloud_databases';

-- Check existing configurations
SELECT 
    config_id,
    user_email,
    database_type,
    server_host,
    server_port,
    database_name,
    is_active,
    schema_initialized,
    created_at
FROM private_cloud_databases
ORDER BY created_at DESC;
