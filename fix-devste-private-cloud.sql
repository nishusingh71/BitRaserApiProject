-- COMPLETE FIX SCRIPT for devste@gmail.com Private Cloud Setup
-- Run this script to fix all common issues

-- ============================================
-- STEP 1: CREATE TABLE IF NOT EXISTS
-- ============================================
CREATE TABLE IF NOT EXISTS `private_cloud_databases` (
  `config_id` INT AUTO_INCREMENT PRIMARY KEY,
  `user_id` INT NOT NULL,
  `user_email` VARCHAR(255) NOT NULL,
  `connection_string` TEXT NOT NULL,
  `database_type` VARCHAR(50) NOT NULL DEFAULT 'mysql',
  `server_host` VARCHAR(255),
  `server_port` INT DEFAULT 3306,
  `database_name` VARCHAR(255) NOT NULL,
  `database_username` VARCHAR(255) NOT NULL,
  `is_active` BOOLEAN DEFAULT TRUE,
  `last_tested_at` DATETIME,
  `test_status` VARCHAR(50) DEFAULT 'pending',
  `test_error` TEXT,
  `schema_initialized` BOOLEAN DEFAULT FALSE,
  `schema_initialized_at` DATETIME,
  `schema_version` VARCHAR(50) DEFAULT '1.0.0',
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `created_by` VARCHAR(255),
  `settings_json` JSON,
  `notes` VARCHAR(500),
UNIQUE KEY `user_email_unique` (`user_email`),
  CONSTRAINT `fk_private_cloud_user` FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Expected: Query OK, 0 rows affected (table already exists)
-- OR: Query OK (table created)


-- ============================================
-- STEP 2: ENSURE USER EXISTS
-- ============================================
-- Check if user exists
SELECT COUNT(*) as user_exists 
FROM users 
WHERE user_email = 'devste@gmail.com';

-- If user doesn't exist, uncomment and modify this:
/*
INSERT INTO users (
    user_name, 
    user_email, 
    user_password, 
    user_role,
    is_private_cloud,
    status,
    created_at
) VALUES (
    'Dev Ste',
    'devste@gmail.com',
    '$2a$11$YourHashedPasswordHere', -- Replace with actual hashed password
'admin',
    TRUE,
 'active',
    NOW()
);
*/


-- ============================================
-- STEP 3: ENABLE PRIVATE CLOUD
-- ============================================
UPDATE users 
SET 
    is_private_cloud = TRUE,
    status = 'active',
    updated_at = NOW()
WHERE user_email = 'devste@gmail.com';

-- Expected: Query OK, 1 row affected


-- ============================================
-- STEP 4: CLEAN OLD CONFIG (FRESH START)
-- ============================================
DELETE FROM private_cloud_databases 
WHERE user_email = 'devste@gmail.com';

-- Expected: Query OK, 0-1 rows affected


-- ============================================
-- STEP 5: VERIFY SETUP
-- ============================================
SELECT 
    'User Check' as check_type,
    user_id,
    user_email,
    is_private_cloud,
    status
FROM users 
WHERE user_email = 'devste@gmail.com'

UNION ALL

SELECT 
    'Config Check' as check_type,
    config_id as user_id,
    user_email,
    CAST(is_active AS CHAR) as is_private_cloud,
    test_status as status
FROM private_cloud_databases
WHERE user_email = 'devste@gmail.com';

-- Expected Results:
-- Row 1: User Check | user_id | devste@gmail.com | 1 (TRUE) | active
-- Row 2: Config Check | (should be empty for fresh start)


-- ============================================
-- STEP 6: GET USER ID FOR REFERENCE
-- ============================================
SELECT 
user_id,
    user_email,
    user_name,
    is_private_cloud
FROM users 
WHERE user_email = 'devste@gmail.com';

-- Note the user_id for manual config creation if needed


-- ============================================
-- OPTIONAL: MANUAL CONFIG INSERT (if API fails)
-- ============================================
/*
-- Get user_id first from Step 6, then uncomment and modify:

INSERT INTO private_cloud_databases (
    user_id,
    user_email,
    connection_string,
    database_type,
    server_host,
    server_port,
    database_name,
    database_username,
    is_active,
    test_status,
    created_by,
    notes
) VALUES (
    999, -- Replace with actual user_id from Step 6
    'devste@gmail.com',
    'ENCRYPTED_CONNECTION_STRING_HERE', -- API will encrypt this
    'mysql',
    'gateway01.ap-southeast-1.prod.aws.tidbcloud.com',
    4000,
    'Tech',
    '4WScT7meioLLU3B.root',
    TRUE,
  'pending',
    'devste@gmail.com',
    'TiDB Test Database'
);
*/


-- ============================================
-- STEP 7: FINAL VERIFICATION
-- ============================================
SELECT 
    u.user_id,
    u.user_email,
    u.user_name,
    u.is_private_cloud,
    u.status as user_status,
    CASE 
      WHEN pc.config_id IS NULL THEN '❌ No Config'
     ELSE '✅ Config Exists'
    END as config_status,
    pc.database_type,
    pc.test_status,
    pc.schema_initialized
FROM users u
LEFT JOIN private_cloud_databases pc ON u.user_email = pc.user_email
WHERE u.user_email = 'devste@gmail.com';

-- Expected:
-- ✅ is_private_cloud = 1
-- ✅ user_status = active
-- ❌ config_status = No Config (ready for API setup)
-- OR
-- ✅ config_status = Config Exists (if manual insert done)


-- ============================================
-- TROUBLESHOOTING QUERIES
-- ============================================

-- Check for foreign key issues
SELECT 
  CONSTRAINT_NAME,
    TABLE_NAME,
    REFERENCED_TABLE_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_NAME = 'private_cloud_databases'
AND TABLE_SCHEMA = DATABASE();


-- Check table structure
SHOW CREATE TABLE private_cloud_databases;


-- Check for any orphaned configs
SELECT pc.* 
FROM private_cloud_databases pc
LEFT JOIN users u ON pc.user_id = u.user_id
WHERE u.user_id IS NULL;


-- ============================================
-- SUCCESS CHECKLIST
-- ============================================
/*
After running this script, verify:

✅ private_cloud_databases table exists
✅ User 'devste@gmail.com' exists in users table
✅ is_private_cloud = TRUE for the user
✅ User status = 'active'
✅ No existing config in private_cloud_databases (fresh start)
✅ Foreign key constraint exists and is valid

Now you can:
1. Start the API: dotnet run
2. Get JWT token by logging in
3. Call POST /api/PrivateCloud/setup-simple with your TiDB connection string
4. Check application console for detailed logs
*/


-- ============================================
-- ROLLBACK (if something goes wrong)
-- ============================================
/*
-- Uncomment to rollback changes:

DELETE FROM private_cloud_databases WHERE user_email = 'devste@gmail.com';
UPDATE users SET is_private_cloud = FALSE WHERE user_email = 'devste@gmail.com';
*/
