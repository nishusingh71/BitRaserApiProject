-- Quick Debug Queries for devste@gmail.com Private Cloud Setup

-- ============================================
-- Step 1: Check if user exists
-- ============================================
SELECT 
    user_id,
    user_email,
    user_name,
    is_private_cloud,
    status,
    created_at
FROM users 
WHERE user_email = 'devste@gmail.com';

-- Expected: Should return 1 row with user details
-- If no rows: User doesn't exist - create user first
-- If is_private_cloud = 0 or NULL: Run Step 2


-- ============================================
-- Step 2: Enable private cloud for user
-- ============================================
UPDATE users 
SET is_private_cloud = TRUE,
    status = 'active'
WHERE user_email = 'devste@gmail.com';

-- Expected: Query OK, 1 row affected


-- ============================================
-- Step 3: Verify private cloud is enabled
-- ============================================
SELECT 
    user_email,
    is_private_cloud,
    status
FROM users 
WHERE user_email = 'devste@gmail.com';

-- Expected: is_private_cloud should be 1 (TRUE)


-- ============================================
-- Step 4: Check existing private cloud config
-- ============================================
SELECT 
    config_id,
    user_email,
    database_type,
    server_host,
    server_port,
    database_name,
    is_active,
    test_status,
    schema_initialized,
    last_tested_at,
    created_at
FROM private_cloud_databases
WHERE user_email = 'devste@gmail.com';

-- Expected: Either 0 rows (no config yet) or existing config details
-- If config exists but you want to recreate: Run Step 5


-- ============================================
-- Step 5: Delete existing config (if needed)
-- ============================================
DELETE FROM private_cloud_databases 
WHERE user_email = 'devste@gmail.com';

-- Expected: Query OK, 1 row affected (if config existed)
-- Use this ONLY if you want to start fresh


-- ============================================
-- Step 6: Check table structure
-- ============================================
DESCRIBE private_cloud_databases;

-- Expected: Should show all columns including:
-- - config_id
-- - user_email
-- - connection_string (encrypted)
-- - database_type
-- - is_active
-- - test_status
-- - schema_initialized


-- ============================================
-- Step 7: Verify user_id exists (for foreign key)
-- ============================================
SELECT 
    u.user_id,
    u.user_email,
    u.is_private_cloud,
    COUNT(pc.config_id) as has_private_config
FROM users u
LEFT JOIN private_cloud_databases pc ON u.user_email = pc.user_email
WHERE u.user_email = 'devste@gmail.com'
GROUP BY u.user_id, u.user_email, u.is_private_cloud;

-- Expected: Shows user and whether they have a private cloud config


-- ============================================
-- Step 8: Check for any errors in test_error column
-- ============================================
SELECT 
    user_email,
    test_status,
    test_error,
    last_tested_at
FROM private_cloud_databases
WHERE user_email = 'devste@gmail.com'
AND test_error IS NOT NULL;

-- Expected: Shows any previous connection test errors


-- ============================================
-- COMPLETE RESET (use if needed)
-- ============================================
-- Uncomment and run if you want to completely reset

-- DELETE FROM private_cloud_databases WHERE user_email = 'devste@gmail.com';
-- UPDATE users SET is_private_cloud = FALSE WHERE user_email = 'devste@gmail.com';
-- UPDATE users SET is_private_cloud = TRUE WHERE user_email = 'devste@gmail.com';


-- ============================================
-- VERIFICATION CHECKLIST
-- ============================================
/*
✅ User exists in users table
✅ is_private_cloud = TRUE
✅ status = 'active'
✅ No existing config OR old config deleted
✅ private_cloud_databases table exists
✅ Main database connection works
✅ API application is running
✅ JWT token is valid
*/
