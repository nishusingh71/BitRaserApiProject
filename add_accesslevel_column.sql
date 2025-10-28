-- Add AccessLevel column to subuser table if it doesn't exist
-- Run this script to fix the "Unknown column" error

SET @dbname = DATABASE();
SET @tablename = 'subuser';
SET @columnname = 'accesslevel';

-- Check if accesslevel column exists (lowercase)
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (TABLE_NAME = @tablename)
      AND (TABLE_SCHEMA = @dbname)
      AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column accesslevel already exists in subuser table.'' AS Info;',
  'ALTER TABLE subuser ADD COLUMN accesslevel VARCHAR(50) NULL DEFAULT ''limited'' COMMENT ''Access level: full, limited, read_only'';'
));

PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Also check for uppercase version
SET @columnname_upper = 'AccessLevel';
SET @preparedStatement2 = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (TABLE_NAME = @tablename)
   AND (TABLE_SCHEMA = @dbname)
AND (COLUMN_NAME = @columnname_upper)
  ) > 0,
  'SELECT ''Column AccessLevel (uppercase) exists in subuser table.'' AS Info;',
  'SELECT ''No AccessLevel column found.'' AS Info;'
));

PREPARE checkUpper FROM @preparedStatement2;
EXECUTE checkUpper;
DEALLOCATE PREPARE checkUpper;

-- Verify column was added
SELECT '========================================' AS Info;
SELECT 'SUBUSER TABLE - AccessLevel Column:' AS Info;
SELECT '========================================' AS Info;
SHOW COLUMNS FROM subuser LIKE '%access%';

SELECT '✅ Migration completed!' AS Result;
