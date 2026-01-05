-- ============================================================================
-- ADD ACTIVITY_STATUS AND LAST_LOGIN_IP COLUMNS
-- For Users and Subusers tables
-- Database: MySQL/MariaDB
-- ============================================================================

-- ============================================================================
-- PART 1: ADD COLUMNS TO USERS TABLE
-- ============================================================================

-- Check if activity_status column exists in users table
SET @activity_status_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND COLUMN_NAME = 'activity_status'
);

-- Add activity_status column if it doesn't exist
SET @sql_add_activity_status = IF(
    @activity_status_exists = 0,
    'ALTER TABLE users ADD COLUMN activity_status VARCHAR(50) DEFAULT ''offline'' AFTER status',
    'SELECT ''activity_status column already exists in users table'' AS message'
);

PREPARE stmt FROM @sql_add_activity_status;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if last_login_ip column exists in users table
SET @last_login_ip_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND COLUMN_NAME = 'last_login_ip'
);

-- Add last_login_ip column if it doesn't exist
SET @sql_add_last_login_ip = IF(
    @last_login_ip_exists = 0,
    'ALTER TABLE users ADD COLUMN last_login_ip VARCHAR(500) NULL AFTER last_logout',
    'SELECT ''last_login_ip column already exists in users table'' AS message'
);

PREPARE stmt FROM @sql_add_last_login_ip;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PART 2: ADD COLUMNS TO SUBUSER TABLE
-- ============================================================================

-- Check if activity_status column exists in subuser table
SET @subuser_activity_status_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'subuser'
      AND COLUMN_NAME = 'activity_status'
);

-- Add activity_status column if it doesn't exist
SET @sql_add_subuser_activity_status = IF(
    @subuser_activity_status_exists = 0,
    'ALTER TABLE subuser ADD COLUMN activity_status VARCHAR(50) DEFAULT ''offline'' AFTER status',
    'SELECT ''activity_status column already exists in subuser table'' AS message'
);

PREPARE stmt FROM @sql_add_subuser_activity_status;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- NOTE: subuser.LastLoginIp already exists, no need to add

-- ============================================================================
-- PART 3: MIGRATE EXISTING DATA
-- ============================================================================

-- Copy existing status values to activity_status for users
UPDATE users
SET activity_status = CASE
    WHEN status IS NULL OR status = '' THEN 'offline'
    WHEN status = 'active' THEN 'offline' -- Default to offline, will update on next login
    WHEN status IN ('inactive', 'suspended', 'banned') THEN 'offline'
    ELSE 'offline'
END
WHERE activity_status IS NULL OR activity_status = '';

-- Copy existing status values to activity_status for subusers
UPDATE subuser
SET activity_status = CASE
    WHEN status IS NULL OR status = '' THEN 'offline'
    WHEN status = 'active' THEN 'offline' -- Default to offline, will update on next login
    WHEN status IN ('inactive', 'suspended', 'banned') THEN 'offline'
    ELSE 'offline'
END
WHERE activity_status IS NULL OR activity_status = '';

-- ============================================================================
-- PART 4: ADD INDEXES FOR PERFORMANCE
-- ============================================================================

-- Add index on users.activity_status for faster queries
SET @idx_users_activity_status_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND INDEX_NAME = 'idx_users_activity_status'
);

SET @sql_add_idx_users_activity_status = IF(
    @idx_users_activity_status_exists = 0,
    'CREATE INDEX idx_users_activity_status ON users(activity_status)',
    'SELECT ''idx_users_activity_status index already exists'' AS message'
);

PREPARE stmt FROM @sql_add_idx_users_activity_status;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index on subuser.activity_status for faster queries
SET @idx_subuser_activity_status_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'subuser'
   AND INDEX_NAME = 'idx_subuser_activity_status'
);

SET @sql_add_idx_subuser_activity_status = IF(
    @idx_subuser_activity_status_exists = 0,
    'CREATE INDEX idx_subuser_activity_status ON subuser(activity_status)',
  'SELECT ''idx_subuser_activity_status index already exists'' AS message'
);

PREPARE stmt FROM @sql_add_idx_subuser_activity_status;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PART 5: VERIFICATION QUERIES
-- ============================================================================

-- Verify users table structure
SELECT 
    'users' AS table_name,
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
 COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'users'
  AND COLUMN_NAME IN ('status', 'activity_status', 'last_login', 'last_logout', 'last_login_ip')
ORDER BY ORDINAL_POSITION;

-- Verify subuser table structure
SELECT 
    'subuser' AS table_name,
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'subuser'
  AND COLUMN_NAME IN ('status', 'activity_status', 'last_login', 'last_logout', 'LastLoginIp')
ORDER BY ORDINAL_POSITION;

-- ============================================================================
-- PART 6: DATA VERIFICATION
-- ============================================================================

-- Count users by activity_status
SELECT 
    'users' AS table_name,
    activity_status,
    COUNT(*) AS count
FROM users
GROUP BY activity_status
ORDER BY count DESC;

-- Count subusers by activity_status
SELECT 
    'subuser' AS table_name,
    activity_status,
    COUNT(*) AS count
FROM subuser
GROUP BY activity_status
ORDER BY count DESC;

-- Show recent users with their status fields
SELECT 
    user_id,
    user_email,
    user_name,
    status,
    activity_status,
    last_login,
    last_logout,
    last_login_ip
FROM users
ORDER BY user_id DESC
LIMIT 10;

-- Show recent subusers with their status fields
SELECT 
    subuser_id,
  subuser_email,
    Name,
    status,
    activity_status,
    last_login,
    last_logout,
    LastLoginIp
FROM subuser
ORDER BY subuser_id DESC
LIMIT 10;

-- ============================================================================
-- NOTES:
-- ============================================================================
-- 
-- 1. activity_status vs status:
--    - status: User account status (active, inactive, suspended, banned)
--    - activity_status: Real-time online/offline tracking (online, offline)
--
-- 2. Activity status is calculated based on:
--    - online: last_login within last 5 minutes AND (no logout OR logout before login)
--    - offline: Everything else
--
-- 3. The status field remains unchanged and continues to track account status
--
-- 4. activity_status will be updated by the UserActivity API endpoints:
--    - POST /api/UserActivity/record-login
--    - POST /api/UserActivity/record-logout
--    - POST /api/UserActivity/update-all-status
--
-- ============================================================================

-- ✅ Migration complete!
SELECT '✅ Migration complete! activity_status and last_login_ip columns added.' AS result;
