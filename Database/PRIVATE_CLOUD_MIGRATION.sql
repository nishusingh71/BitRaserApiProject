-- ================================================================
-- PRIVATE CLOUD / MULTI-TENANT DATABASE MIGRATION
-- ================================================================
-- Run this script to add private database support to users table
-- ================================================================

USE bitraser_main;

-- 1. Add private cloud columns to users table
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS is_private_cloud BOOLEAN DEFAULT FALSE COMMENT 'Whether user has private database',
ADD COLUMN IF NOT EXISTS private_db_connection_string TEXT NULL COMMENT 'Encrypted connection string for private DB',
ADD COLUMN IF NOT EXISTS private_db_created_at DATETIME NULL COMMENT 'When private DB was created',
ADD COLUMN IF NOT EXISTS private_db_status VARCHAR(20) DEFAULT 'inactive' COMMENT 'active/inactive/error',
ADD COLUMN IF NOT EXISTS private_db_last_validated DATETIME NULL COMMENT 'Last successful connection test',
ADD COLUMN IF NOT EXISTS private_db_schema_version VARCHAR(20) NULL COMMENT 'Schema version of private DB';

-- 2. Create index for faster private cloud user lookups
CREATE INDEX IF NOT EXISTS idx_is_private_cloud ON users(is_private_cloud);
CREATE INDEX IF NOT EXISTS idx_private_db_status ON users(private_db_status);

-- 3. Create table for tracking private database configurations
CREATE TABLE IF NOT EXISTS private_cloud_databases (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_email VARCHAR(255) NOT NULL,
    database_name VARCHAR(100) NOT NULL,
  server_host VARCHAR(255) NOT NULL,
    server_port INT DEFAULT 3306,
    connection_status VARCHAR(20) DEFAULT 'unknown',
    last_health_check DATETIME NULL,
    schema_version VARCHAR(20) NULL,
    total_records INT DEFAULT 0,
    storage_size_mb DECIMAL(10,2) DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    UNIQUE KEY unique_user_database (user_email),
    FOREIGN KEY (user_email) REFERENCES users(user_email) ON DELETE CASCADE,
    INDEX idx_connection_status (connection_status),
    INDEX idx_last_health_check (last_health_check)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Tracks private cloud database configurations';

-- 4. Create table for database routing cache
CREATE TABLE IF NOT EXISTS database_routing_cache (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_email VARCHAR(255) NOT NULL,
    parent_email VARCHAR(255) NULL COMMENT 'For subusers - points to parent user',
  target_database VARCHAR(20) NOT NULL COMMENT 'main or private',
    connection_string_hash VARCHAR(64) NULL COMMENT 'SHA256 hash for validation',
    cached_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    expires_at DATETIME NULL,
    
    UNIQUE KEY unique_user_cache (user_email),
    INDEX idx_parent_email (parent_email),
    INDEX idx_expires_at (expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Cache for database routing decisions';

-- 5. Create audit log for private database operations
CREATE TABLE IF NOT EXISTS private_db_audit_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_email VARCHAR(255) NOT NULL,
operation VARCHAR(50) NOT NULL COMMENT 'create/update/delete/validate/migrate',
    operation_status VARCHAR(20) NOT NULL COMMENT 'success/failed/pending',
    details TEXT NULL,
    error_message TEXT NULL,
    performed_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    performed_by VARCHAR(255) NULL,
    
    INDEX idx_user_email (user_email),
    INDEX idx_operation (operation),
    INDEX idx_performed_at (performed_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Audit log for private database operations';

-- 6. Verify changes
SELECT 
    'Users table updated successfully' AS status,
  COUNT(*) AS total_users,
    SUM(CASE WHEN is_private_cloud = TRUE THEN 1 ELSE 0 END) AS private_cloud_users,
    SUM(CASE WHEN is_private_cloud = FALSE THEN 1 ELSE 0 END) AS main_db_users
FROM users;

SELECT 'Private cloud tables created successfully' AS status;

-- 7. Sample data check
SELECT 
    user_email,
    is_private_cloud,
    private_db_status,
    private_db_created_at
FROM users
LIMIT 5;
