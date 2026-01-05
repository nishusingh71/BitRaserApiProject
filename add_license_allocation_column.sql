-- Add license_allocation column to subuser table if it doesn't exist

-- Check if column exists
SET @dbname = DATABASE();
SET @tablename = 'subuser';
SET @columnname = 'license_allocation';

SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (TABLE_NAME = @tablename)
      AND (TABLE_SCHEMA = @dbname)
    AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column license_allocation already exists in subuser table.'' AS Info;',
  'ALTER TABLE subuser ADD COLUMN license_allocation INT NULL DEFAULT 0 COMMENT ''Number of licenses allocated to subuser'';'
));

PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Set default value for existing NULL records
UPDATE subuser 
SET license_allocation = 0 
WHERE license_allocation IS NULL;

-- Show results
SELECT 
    COUNT(*) as total_subusers,
    COUNT(license_allocation) as subusers_with_license_allocation,
    AVG(license_allocation) as average_license_allocation,
    MAX(license_allocation) as max_license_allocation
FROM subuser;

-- Show sample data
SELECT 
    subuser_email,
  user_email,
    license_allocation,
    CASE 
        WHEN license_allocation IS NULL THEN '❌ NULL'
        WHEN license_allocation = 0 THEN '⚠️ Zero'
 ELSE '✅ Set'
    END as status
FROM subuser
LIMIT 10;

-- Verify column was added
SHOW COLUMNS FROM subuser LIKE 'license_allocation';

SELECT '✅ Migration completed! license_allocation column is now available.' AS Result;
