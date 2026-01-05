-- ============================================
-- USER ACTIVITY TRACKING - COMPLETE MIGRATION
-- ============================================
-- This script adds login/logout tracking, status, and timezone support
-- for both Users and Subusers
--
-- Fields to be added:
-- USERS TABLE:
--   - last_login (DATETIME) - Login timestamp tracking
--   - last_logout (DATETIME) - Logout timestamp tracking
--   - status (VARCHAR) - User status (active/inactive)
--   - timezone (VARCHAR) - User's timezone preference
--
-- SUBUSER TABLE:
--   - last_login (DATETIME) - Login timestamp tracking (renamed from LastLoginAt for consistency)
--   - last_logout (DATETIME) - Logout timestamp tracking (renamed from LastLogoutAt for consistency)
--   - status (VARCHAR) - Subuser status (active/inactive/suspended)
--   - timezone (VARCHAR) - Subuser's timezone preference (renamed from Timezone for consistency)
-- ============================================

-- Add missing columns to users table
-- Add last_login, last_logout, status and timezone columns

-- Check if last_login column exists, if not add it
SET @dbname = DATABASE;
SET @tablename = 'users';
SET @columnname = 'last_login';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE
 (TABLE_NAME = @tablename)
      AND (TABLE_SCHEMA = @dbname)
      AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column last_login already exists in users table.'' AS Info;',
  'ALTER TABLE users ADD COLUMN last_login DATETIME NULL COMMENT ''Last login timestamp'';'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Check if last_logout column exists, if not add it
SET @columnname = 'last_logout';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE
 (TABLE_NAME = @tablename)
      AND (TABLE_SCHEMA = @dbname)
    AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column last_logout already exists in users table.'' AS Info;',
  'ALTER TABLE users ADD COLUMN last_logout DATETIME NULL COMMENT ''Last logout timestamp'';'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Check if status column exists, if not add it
SET @columnname = 'status';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
   (TABLE_NAME = @tablename)
 AND (TABLE_SCHEMA = @dbname)
 AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column status already exists in users table.'' AS Info;',
  'ALTER TABLE users ADD COLUMN status VARCHAR(20) NULL DEFAULT ''active'' COMMENT ''User status (active/inactive)'';'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Check if timezone column exists, if not add it
SET @columnname = 'timezone';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
   (TABLE_NAME = @tablename)
 AND (TABLE_SCHEMA = @dbname)
 AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column timezone already exists in users table.'' AS Info;',
  'ALTER TABLE users ADD COLUMN timezone VARCHAR(100) NULL COMMENT ''User timezone (e.g., Asia/Kolkata)'';'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Add missing columns to subuser table
SET @tablename = 'subuser';

-- Check if last_login column exists, if not add it
SET @columnname = 'last_login';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
   (TABLE_NAME = @tablename)
      AND (TABLE_SCHEMA = @dbname)
   AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column last_login already exists in subuser table.'' AS Info;',
  'ALTER TABLE subuser ADD COLUMN last_login DATETIME NULL COMMENT ''Last login timestamp'';'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Check if last_logout column exists, if not add it
SET @columnname = 'last_logout';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (TABLE_NAME = @tablename)
      AND (TABLE_SCHEMA = @dbname)
   AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column last_logout already exists in subuser table.'' AS Info;',
  'ALTER TABLE subuser ADD COLUMN last_logout DATETIME NULL COMMENT ''Last logout timestamp'';'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Check if status column exists, if not add it
SET @columnname = 'status';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (TABLE_NAME = @tablename)
 AND (TABLE_SCHEMA = @dbname)
      AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column status already exists in subuser table.'' AS Info;',
  'ALTER TABLE subuser ADD COLUMN status VARCHAR(50) NULL DEFAULT ''active'' COMMENT ''Subuser status (active/inactive/suspended)'';'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Check if timezone column exists, if not add it
SET @columnname = 'timezone';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
    (TABLE_NAME = @tablename)
      AND (TABLE_SCHEMA = @dbname)
      AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column timezone already exists in subuser table.'' AS Info;',
  'ALTER TABLE subuser ADD COLUMN timezone VARCHAR(100) NULL COMMENT ''Subuser timezone (e.g., Asia/Kolkata)'';'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Verify columns were added
SELECT '========================================' AS Info;
SELECT 'USERS TABLE - Columns Added:' AS Info;
SELECT '========================================' AS Info;
SHOW COLUMNS FROM users LIKE '%login%';
SHOW COLUMNS FROM users LIKE '%logout%';
SHOW COLUMNS FROM users LIKE '%status%';
SHOW COLUMNS FROM users LIKE '%timezone%';

SELECT '========================================' AS Info;
SELECT 'SUBUSER TABLE - Columns Added (Lowercase Only):' AS Info;
SELECT '========================================' AS Info;
SHOW COLUMNS FROM subuser LIKE '%login%';
SHOW COLUMNS FROM subuser LIKE '%logout%';
SHOW COLUMNS FROM subuser LIKE '%status%';
SHOW COLUMNS FROM subuser LIKE '%timezone%';

-- Set default values for existing records (optional)
-- Uncomment if you want to set defaults
-- UPDATE users SET status = 'active' WHERE status IS NULL;
-- UPDATE users SET timezone = 'Asia/Kolkata' WHERE timezone IS NULL;
-- UPDATE subuser SET status = 'active' WHERE status IS NULL;
-- UPDATE subuser SET timezone = 'Asia/Kolkata' WHERE timezone IS NULL;

SELECT '✅ Migration completed successfully!' AS Result;
SELECT 'Added columns to USERS table:' AS Summary;
SELECT '  - last_login (DATETIME)' AS Detail;
SELECT '  - last_logout (DATETIME)' AS Detail;
SELECT '  - status (VARCHAR)' AS Detail;
SELECT '  - timezone (VARCHAR)' AS Detail;

SELECT '' AS Spacer;
SELECT 'Added columns to SUBUSER table (lowercase only):' AS Summary;
SELECT '  - last_login (DATETIME)' AS Detail;
SELECT '  - last_logout (DATETIME)' AS Detail;
SELECT '  - status (VARCHAR)' AS Detail;
SELECT '  - timezone (VARCHAR)' AS Detail;
