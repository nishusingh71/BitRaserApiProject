-- ✅ UPDATE EXISTING FORGOT PASSWORD TABLE TO SUPPORT SUBUSERS
-- Run this if you already have the table created

-- Step 1: Add user_type column if it doesn't exist
ALTER TABLE `forgot_password_requests`
ADD COLUMN IF NOT EXISTS `user_type` VARCHAR(20) DEFAULT 'user' 
COMMENT 'Type: user or subuser'
AFTER `email`;

-- Step 2: Add index for better performance
ALTER TABLE `forgot_password_requests`
ADD INDEX IF NOT EXISTS `idx_user_id_type` (`user_id`, `user_type`);

-- Step 3: Update existing records to set user_type
-- This assumes all existing records are for 'user' type
UPDATE `forgot_password_requests`
SET `user_type` = 'user'
WHERE `user_type` IS NULL OR `user_type` = '';

-- Step 4: Verify the changes
DESCRIBE `forgot_password_requests`;

-- Step 5: Check updated structure
SELECT 
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'forgot_password_requests'
ORDER BY ORDINAL_POSITION;

-- Step 6: Test query
SELECT 
  id,
    email,
    user_type,
    otp,
    LEFT(reset_token, 30) as token_preview,
is_used,
    expires_at,
    created_at
FROM `forgot_password_requests`
ORDER BY created_at DESC
LIMIT 5;

-- Success message
SELECT '✅ Table successfully updated to support both Users and Subusers!' as status;
