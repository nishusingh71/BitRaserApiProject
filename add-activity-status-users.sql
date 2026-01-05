-- ========================================
-- ADD activity_status TO users TABLE
-- ========================================
-- Author: GitHub Copilot
-- Date: 2025-01-15
-- Purpose: Add activity_status column to users table in Private Cloud DB

USE cloud_erase__private; -- Your Private Cloud DB

-- ========================================
-- STEP 1: CHECK IF COLUMN EXISTS
-- ========================================

SELECT 
    'Checking if activity_status column exists...' AS Status;

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    COLUMN_DEFAULT,
    IS_NULLABLE
FROM information_schema.COLUMNS 
WHERE 
    TABLE_SCHEMA = 'cloud_erase__private'
AND TABLE_NAME = 'users'
    AND COLUMN_NAME = 'activity_status';

-- ========================================
-- STEP 2: ADD COLUMN IF NOT EXISTS
-- ========================================

SELECT 'Adding activity_status column...' AS Status;

-- Add activity_status column (MySQL 8.0+ supports IF NOT EXISTS)
ALTER TABLE `users`
ADD COLUMN IF NOT EXISTS `activity_status` VARCHAR(50) DEFAULT 'active' 
COMMENT 'User activity status: active, inactive, suspended, locked, pending, deleted'
AFTER `status`;

SELECT '✅ Column added successfully!' AS Status;

-- ========================================
-- STEP 3: ADD INDEX FOR PERFORMANCE
-- ========================================

SELECT 'Adding index on activity_status...' AS Status;

-- Create index if not exists
CREATE INDEX IF NOT EXISTS `idx_activity_status` ON `users`(`activity_status`);

SELECT '✅ Index created successfully!' AS Status;

-- ========================================
-- STEP 4: SET DEFAULT VALUE FOR EXISTING ROWS
-- ========================================

SELECT 'Updating existing rows with default value...' AS Status;

-- Update NULL values to 'active'
UPDATE `users`
SET `activity_status` = 'active'
WHERE `activity_status` IS NULL;

SELECT CONCAT('✅ Updated ', ROW_COUNT(), ' rows') AS Status;

-- ========================================
-- STEP 5: VERIFY SCHEMA
-- ========================================

SELECT 'Verifying schema...' AS Status;

-- Show table structure
DESC users;

-- Show all columns
SELECT 
    COLUMN_NAME AS 'Column Name',
 DATA_TYPE AS 'Data Type',
    COLUMN_DEFAULT AS 'Default Value',
 IS_NULLABLE AS 'Nullable',
    COLUMN_KEY AS 'Key',
    COLUMN_COMMENT AS 'Comment'
FROM information_schema.COLUMNS 
WHERE 
    TABLE_SCHEMA = 'cloud_erase__private'
    AND TABLE_NAME = 'users'
ORDER BY ORDINAL_POSITION;

-- ========================================
-- STEP 6: VERIFY INDEXES
-- ========================================

SELECT 'Verifying indexes...' AS Status;

SHOW INDEX FROM users WHERE Key_name = 'idx_activity_status';

-- ========================================
-- STEP 7: TEST QUERIES
-- ========================================

SELECT 'Running test queries...' AS Status;

-- Count users by activity status
SELECT 
    activity_status,
    COUNT(*) AS user_count
FROM users
GROUP BY activity_status;

-- ========================================
-- STEP 8: SAMPLE DATA VERIFICATION
-- ========================================

SELECT 'Sample user data:' AS Status;

SELECT 
    user_id,
    user_email,
    status,
    activity_status,
    is_private_cloud,
    created_at
FROM users
LIMIT 5;

-- ========================================
-- MIGRATION COMPLETE
-- ========================================

SELECT '🎉 MIGRATION COMPLETE!' AS Status,
       'activity_status column added to users table' AS Details,
       'Users table now has activity tracking' AS Note;

-- ========================================
-- ROLLBACK (IF NEEDED - USE WITH CAUTION)
-- ========================================

-- Uncomment to remove the column
-- ALTER TABLE `users` DROP COLUMN `activity_status`;
-- DROP INDEX `idx_activity_status` ON `users`;

-- ========================================
-- TROUBLESHOOTING
-- ========================================

-- If errors occur, check:
-- 1. Database exists: SELECT DATABASE();
-- 2. Table exists: SHOW TABLES LIKE 'users';
-- 3. Current schema: DESC users;
-- 4. Permissions: SHOW GRANTS;

-- Check for duplicate columns
SELECT 
    COUNT(*) AS column_count,
    COLUMN_NAME
FROM information_schema.COLUMNS 
WHERE 
    TABLE_SCHEMA = 'cloud_erase__private'
    AND TABLE_NAME = 'users'
GROUP BY COLUMN_NAME
HAVING COUNT(*) > 1;
