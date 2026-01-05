-- QUICK FIX FOR devste@gmail.com
-- Run these queries in order

-- ===========================================
-- STEP 1: Verify user exists
-- ===========================================
SELECT 
    'User Check' as step,
    user_id,
    user_email,
    is_private_cloud,
    status
FROM users 
WHERE user_email = 'devste@gmail.com';

-- Expected: 1 row returned
-- If no rows: User doesn't exist - CREATE USER FIRST!
-- If is_private_cloud = 0: Continue to Step 2


-- ===========================================
-- STEP 2: Enable private cloud
-- ===========================================
UPDATE users 
SET 
is_private_cloud = TRUE,
    status = 'active'
WHERE user_email = 'devste@gmail.com';

-- Expected: Query OK, 1 row affected


-- ===========================================
-- STEP 3: Delete old config (FRESH START)
-- ===========================================
DELETE FROM private_cloud_databases 
WHERE user_email = 'devste@gmail.com';

-- Expected: Query OK, 0 or 1 rows affected
-- This clears any broken configuration


-- ===========================================
-- STEP 4: Verify table exists
-- ===========================================
SHOW TABLES LIKE 'private_cloud_databases';

-- Expected: 1 row showing 'private_cloud_databases'
-- If empty: Run create_private_cloud_table.sql first!


-- ===========================================
-- STEP 5: Final verification
-- ===========================================
SELECT 
    'Final Check' as step,
  u.user_id,
    u.user_email,
u.is_private_cloud,
    u.status,
    CASE 
        WHEN pc.config_id IS NULL THEN 'Ready for setup ✅'
        ELSE 'Config exists (delete first) ❌'
    END as config_status
FROM users u
LEFT JOIN private_cloud_databases pc ON u.user_email = pc.user_email
WHERE u.user_email = 'devste@gmail.com';

-- Expected results:
-- is_private_cloud = 1 ✅
-- status = active ✅
-- config_status = 'Ready for setup ✅'


-- ===========================================
-- TROUBLESHOOTING QUERIES
-- ===========================================

-- If Step 1 returns no rows:
-- SELECT COUNT(*) FROM users; -- Check if ANY users exist

-- If table doesn't exist in Step 4:
-- SOURCE create_private_cloud_table.sql;

-- Check for any errors:
-- SHOW WARNINGS;
