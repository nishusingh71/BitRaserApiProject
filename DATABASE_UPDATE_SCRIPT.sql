-- ✅ Add Missing Columns to subuser Table
-- Run this SQL script manually in your MySQL database

USE bitraserdb;  -- Replace with your database name if different

-- ✅ 1. Add subuser_group column (string field)
ALTER TABLE `subuser` 
ADD COLUMN IF NOT EXISTS `subuser_group` VARCHAR(100) NULL 
AFTER `Role`;

-- ✅ 2. Add license_allocation column (integer field)
ALTER TABLE `subuser` 
ADD COLUMN IF NOT EXISTS `license_allocation` INT NULL DEFAULT 0 
AFTER `subuser_group`;

-- ✅ 3. Verify the columns were added
DESC `subuser`;

-- ✅ 4. Update existing records with default values (optional)
UPDATE `subuser` 
SET `license_allocation` = 0 
WHERE `license_allocation` IS NULL;

-- ✅ Done! Now your columns are added to the database
SELECT 
    subuser_id,
    subuser_email,
    Name,
    Department,
    Role,
    subuser_group,  -- ✅ New column
    license_allocation  -- ✅ New column
FROM `subuser`
LIMIT 5;
