-- ============================================
-- TiDB Private Cloud Databases Table Creation
-- ============================================
-- Run this script in your TiDB database (Cloud_Erase)
-- This table stores private cloud configuration for users

-- Drop table if exists (CAUTION: This will delete all data)
-- DROP TABLE IF EXISTS `private_cloud_databases`;

-- ============================================
-- Create private_cloud_databases table
-- ============================================
CREATE TABLE IF NOT EXISTS `private_cloud_databases` (
    -- Primary Key
    `config_id` INT AUTO_INCREMENT PRIMARY KEY,
    
    -- User Reference (Foreign Key)
    `user_id` INT NOT NULL,
    `user_email` VARCHAR(255) NOT NULL,
    
    -- Database Connection Details
    `connection_string` TEXT NOT NULL,
    `database_type` VARCHAR(50) NOT NULL DEFAULT 'mysql',
    `server_host` VARCHAR(255) NULL,
    `server_port` INT NULL DEFAULT 3306,
    `database_name` VARCHAR(255) NOT NULL,
    `database_username` VARCHAR(255) NOT NULL,
    
    -- Status and Testing
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
    `last_tested_at` DATETIME NULL,
    `test_status` VARCHAR(50) NULL DEFAULT 'pending',
    `test_error` TEXT NULL,
    
    -- Schema Information
    `schema_initialized` BOOLEAN NOT NULL DEFAULT FALSE,
    `schema_initialized_at` DATETIME NULL,
    `schema_version` VARCHAR(50) NULL DEFAULT '1.0.0',
    
    -- Timestamps
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    -- Metadata
    `created_by` VARCHAR(255) NULL,
    `settings_json` TEXT NULL,
    `notes` VARCHAR(500) NULL,
    
    -- Indexes
    UNIQUE KEY `idx_user_email_unique` (`user_email`),
    KEY `idx_user_id` (`user_id`),
 KEY `idx_is_active` (`is_active`),
    KEY `idx_test_status` (`test_status`),
    KEY `idx_schema_initialized` (`schema_initialized`)
    
    -- Note: TiDB doesn't support foreign key constraints in the same way as MySQL
    -- FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- ============================================
-- Table Comments (for documentation)
-- ============================================
ALTER TABLE `private_cloud_databases` 
    COMMENT 'Stores private cloud database configurations for users';

-- ============================================
-- Verify Table Creation
-- ============================================
-- Show table structure
DESC `private_cloud_databases`;

-- Show indexes
SHOW INDEX FROM `private_cloud_databases`;

-- Count records (should be 0 initially)
SELECT COUNT(*) AS total_records FROM `private_cloud_databases`;

-- ============================================
-- Sample Insert Statement (for testing)
-- ============================================
/*
INSERT INTO `private_cloud_databases` (
    `user_id`,
    `user_email`,
    `connection_string`,
    `database_type`,
    `server_host`,
    `server_port`,
 `database_name`,
    `database_username`,
    `is_active`,
    `test_status`,
    `schema_initialized`,
    `created_by`,
    `notes`
) VALUES (
    1,    -- user_id
    'devste@gmail.com',       -- user_email
    'ENCRYPTED_CONNECTION_STRING_HERE',       -- connection_string (will be encrypted by API)
    'mysql',                -- database_type
    'gateway01.ap-southeast-1.prod.aws.tidbcloud.com',  -- server_host
    4000,           -- server_port
    'Cloud_Erase',   -- database_name
    '2tdeFNZMcsWKkDR.root',  -- database_username
  TRUE,      -- is_active
    'success',          -- test_status
    TRUE,   -- schema_initialized
    'devste@gmail.com',     -- created_by
    'TiDB Cloud Production Database'    -- notes
);
*/

-- ============================================
-- Sample Select Queries
-- ============================================

-- Get all active configurations
-- SELECT * FROM `private_cloud_databases` WHERE `is_active` = TRUE;

-- Get configuration for a specific user
-- SELECT * FROM `private_cloud_databases` WHERE `user_email` = 'devste@gmail.com';

-- Get configurations with schema not initialized
-- SELECT * FROM `private_cloud_databases` WHERE `schema_initialized` = FALSE;

-- Get configurations with failed tests
-- SELECT * FROM `private_cloud_databases` WHERE `test_status` = 'failed';

-- ============================================
-- Sample Update Queries
-- ============================================

-- Update test status after successful connection
/*
UPDATE `private_cloud_databases`
SET 
    `test_status` = 'success',
    `last_tested_at` = NOW(),
    `test_error` = NULL
WHERE `user_email` = 'devste@gmail.com';
*/

-- Mark schema as initialized
/*
UPDATE `private_cloud_databases`
SET 
    `schema_initialized` = TRUE,
    `schema_initialized_at` = NOW(),
    `schema_version` = '1.0.0'
WHERE `user_email` = 'devste@gmail.com';
*/

-- Deactivate configuration
/*
UPDATE `private_cloud_databases`
SET 
    `is_active` = FALSE,
    `updated_at` = NOW()
WHERE `user_email` = 'devste@gmail.com';
*/

-- ============================================
-- Sample Delete Queries
-- ============================================

-- Delete configuration for a user
-- DELETE FROM `private_cloud_databases` WHERE `user_email` = 'devste@gmail.com';

-- Delete all inactive configurations
-- DELETE FROM `private_cloud_databases` WHERE `is_active` = FALSE;

-- ============================================
-- Cleanup Old Test Data (Optional)
-- ============================================

-- Delete configurations older than 30 days that are not active
/*
DELETE FROM `private_cloud_databases`
WHERE 
    `is_active` = FALSE 
    AND `created_at` < DATE_SUB(NOW(), INTERVAL 30 DAY);
*/

-- ============================================
-- Performance Statistics
-- ============================================

-- Count by database type
-- SELECT `database_type`, COUNT(*) as count 
-- FROM `private_cloud_databases` 
-- GROUP BY `database_type`;

-- Count by test status
-- SELECT `test_status`, COUNT(*) as count 
-- FROM `private_cloud_databases` 
-- GROUP BY `test_status`;

-- Count by schema initialization
-- SELECT `schema_initialized`, COUNT(*) as count 
-- FROM `private_cloud_databases` 
-- GROUP BY `schema_initialized`;

-- ============================================
-- Table Maintenance
-- ============================================

-- Analyze table for better query performance
-- ANALYZE TABLE `private_cloud_databases`;

-- Check table status
-- SHOW TABLE STATUS LIKE 'private_cloud_databases';

-- ============================================
-- Backup and Restore (Important!)
-- ============================================

-- Backup table structure
-- SHOW CREATE TABLE `private_cloud_databases`;

-- Backup table data
-- SELECT * FROM `private_cloud_databases` INTO OUTFILE '/tmp/private_cloud_backup.csv';

-- ============================================
-- NOTES AND WARNINGS
-- ============================================

/*
IMPORTANT NOTES:

1. CONNECTION STRING SECURITY:
   - Connection strings are stored ENCRYPTED in the application
   - Never store plain text passwords in this table
   - The API handles encryption/decryption automatically

2. TIDB SPECIFIC:
   - TiDB is MySQL compatible but has some differences
   - Foreign keys are not enforced in TiDB
   - ON UPDATE CURRENT_TIMESTAMP works in TiDB
   - AUTO_INCREMENT works normally

3. DATA INTEGRITY:
   - Always backup before running DELETE or DROP commands
   - Use transactions for critical updates
   - Test queries on non-production data first

4. PERFORMANCE:
   - Indexes are created on commonly queried columns
   - Keep settings_json and notes reasonably sized
   - Regularly analyze table statistics

5. SCHEMA VERSION:
   - schema_version tracks database schema changes
   - Increment this when you modify the private database schema
   - Used for migration tracking

6. USER REFERENCE:
   - user_id should match users.user_id in main database
   - user_email should match users.user_email in main database
   - Both are stored for redundancy and faster lookups

7. TESTING:
   - Always test connection before marking schema_initialized = TRUE
   - Store test errors in test_error column for debugging
   - Use last_tested_at to track connection health

8. CLEANUP:
   - Regularly clean up old test configurations
   - Archive inactive configurations after 30 days
   - Monitor table size and optimize as needed
*/

-- ============================================
-- SUCCESS MESSAGE
-- ============================================

SELECT 
    '✅ Table created successfully!' AS status,
    'private_cloud_databases' AS table_name,
    (SELECT COUNT(*) FROM information_schema.tables 
     WHERE table_schema = DATABASE() 
     AND table_name = 'private_cloud_databases') AS table_exists;

-- ============================================
-- END OF SCRIPT
-- ============================================
