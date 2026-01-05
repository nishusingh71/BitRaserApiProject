-- ============================================
-- Private Cloud Setup - Quick Fix Script
-- ============================================

-- Step 1: Check current user status
SELECT 
    user_id,
    user_email,
    user_name,
    is_private_cloud,
    private_api,
    created_at
FROM users 
WHERE user_email = 'devste@gmail.com';

-- Step 2: Enable private cloud for user
UPDATE users 
SET 
    is_private_cloud = TRUE,
    updated_at = NOW()
WHERE user_email = 'devste@gmail.com';

-- Step 3: Verify the change
SELECT 
    user_email,
    is_private_cloud,
    updated_at
FROM users 
WHERE user_email = 'devste@gmail.com';

-- Step 4: Check if there's any existing private cloud config
SELECT 
    config_id,
    user_email,
    database_type,
    server_host,
    is_active,
    test_status,
    last_tested_at
FROM private_cloud_databases
WHERE user_email = 'devste@gmail.com';

-- Step 5: If old config exists and you want to delete it
-- DELETE FROM private_cloud_databases WHERE user_email = 'devste@gmail.com';

-- ============================================
-- Expected Results:
-- ============================================
-- After Step 3, you should see:
-- user_email        | is_private_cloud
-- --------------------------------
-- devste@gmail.com  | 1 (TRUE)
-- ============================================
