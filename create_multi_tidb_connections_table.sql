-- Multi-TiDB Connections Table
-- Stores multiple TiDB database connections for each user

CREATE TABLE IF NOT EXISTS `multi_tidb_connections` (
    `connection_id` VARCHAR(36) PRIMARY KEY,
    `user_email` VARCHAR(255) NOT NULL,
    `connection_name` VARCHAR(255) NOT NULL,
    `server_host` VARCHAR(255) NOT NULL,
    `server_port` INT NOT NULL DEFAULT 4000,
    `database_name` VARCHAR(255) NOT NULL,
    `username` VARCHAR(255) NOT NULL,
    `encrypted_password` TEXT NOT NULL,
    `encrypted_connection_string` TEXT NOT NULL,
    `is_active` BOOLEAN DEFAULT TRUE,
    `is_default` BOOLEAN DEFAULT FALSE,
    `description` VARCHAR(500),
    `tags` VARCHAR(500),
    `last_tested_at` DATETIME,
    `test_status` VARCHAR(50),
    `test_error` TEXT,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `created_by` VARCHAR(255),
    `updated_at` DATETIME ON UPDATE CURRENT_TIMESTAMP,
    `updated_by` VARCHAR(255),
  `deleted_at` DATETIME,
    `deleted_by` VARCHAR(255),
    `last_used_at` DATETIME,
    `usage_count` INT DEFAULT 0,
    `metadata_json` JSON,
    
    INDEX idx_user_email (`user_email`),
    INDEX idx_connection_name (`connection_name`),
    INDEX idx_is_default (`is_default`),
    INDEX idx_is_active (`is_active`),
    INDEX idx_created_at (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create index for faster lookups
CREATE INDEX idx_user_default ON `multi_tidb_connections` (`user_email`, `is_default`, `is_active`);
