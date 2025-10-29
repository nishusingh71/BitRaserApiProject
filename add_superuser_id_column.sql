-- Add superuser_id column to subuser table if it doesn't exist

-- Check if column exists
SET @dbname = DATABASE();
SET @tablename = 'subuser';
SET @columnname = 'superuser_id';

SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (TABLE_NAME = @tablename)
      AND (TABLE_SCHEMA = @dbname)
      AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column superuser_id already exists in subuser table.'' AS Info;',
  'ALTER TABLE subuser ADD COLUMN superuser_id INT NULL COMMENT ''Reference to parent user ID (nullable)'';'
));

PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Update existing rows to set superuser_id based on user_email
-- This will populate the field for existing records
UPDATE subuser s
INNER JOIN users u ON s.user_email = u.user_email
SET s.superuser_id = u.user_id
WHERE s.superuser_id IS NULL;

-- Show results
SELECT 
    COUNT(*) as total_subusers,
    COUNT(superuser_id) as subusers_with_superuser_id,
    COUNT(*) - COUNT(superuser_id) as subusers_still_null
FROM subuser;

-- Show sample data
SELECT 
    subuser_email,
    user_email,
    superuser_id,
    CASE 
        WHEN superuser_id IS NULL THEN '❌ NULL'
    ELSE '✅ Set'
 END as status
FROM subuser
LIMIT 10;

-- Verify column was added
SHOW COLUMNS FROM subuser LIKE 'superuser_id';

SELECT '✅ Migration completed! superuser_id is now NULLABLE.' AS Result;
