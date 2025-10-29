-- Add subuser_group column to subuser table if it doesn't exist

-- Check if column exists
SET @dbname = DATABASE();
SET @tablename = 'subuser';
SET @columnname = 'subuser_group';

SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE
      (TABLE_NAME = @tablename)
      AND (TABLE_SCHEMA = @dbname)
      AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column subuser_group already exists in subuser table.'' AS Info;',
  'ALTER TABLE subuser ADD COLUMN subuser_group VARCHAR(100) NULL COMMENT ''Group name or identifier'';'
));

PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Verify column was added
SHOW COLUMNS FROM subuser LIKE 'subuser_group';

SELECT '✅ Migration completed!' AS Result;
