-- 🧪 FORGOT PASSWORD API - DIAGNOSTIC QUERIES
-- Run these queries to diagnose the "Invalid email or OTP" error

-- ===================================================
-- STEP 1: Check if table exists
-- ===================================================
SHOW TABLES LIKE 'forgot_password_requests';

-- Expected: Table should exist
-- If empty, table doesn't exist - run create table script


-- ===================================================
-- STEP 2: Check table structure
-- ===================================================
DESCRIBE forgot_password_requests;

-- Expected columns:
-- id, user_id, email, otp, reset_token, is_used, expires_at, created_at, ip_address, user_agent


-- ===================================================
-- STEP 3: Check recent password reset requests
-- ===================================================
SELECT 
    id,
    email,
    otp,
    LEFT(reset_token, 30) as token_preview,
    is_used,
    expires_at,
    created_at,
    CASE 
        WHEN is_used = 1 THEN '❌ USED'
        WHEN expires_at < NOW() THEN '⏰ EXPIRED'
        ELSE '✅ ACTIVE'
    END as status,
    TIMESTAMPDIFF(MINUTE, NOW(), expires_at) as minutes_remaining
FROM forgot_password_requests
ORDER BY created_at DESC
LIMIT 10;

-- Check status column:
-- ✅ ACTIVE = Can be used
-- ❌ USED = Already used
-- ⏰ EXPIRED = Past expiry time


-- ===================================================
-- STEP 4: Find specific email's requests
-- ===================================================
-- Replace 'your_email@example.com' with actual email
SELECT 
    id,
    email,
    otp,
    LEFT(reset_token, 40) as token,
    is_used,
    expires_at,
    created_at,
    TIMESTAMPDIFF(MINUTE, NOW(), expires_at) as mins_left
FROM forgot_password_requests
WHERE email = 'your_email@example.com'
ORDER BY created_at DESC;


-- ===================================================
-- STEP 5: Simulate service query (what API checks)
-- ===================================================
-- Replace with your actual values
SELECT 
    'FOUND' as result,
    email,
  otp,
    LEFT(reset_token, 30) as token
FROM forgot_password_requests
WHERE email = 'your_email@example.com'      -- Replace
  AND otp = '123456'       -- Replace with actual OTP
  AND is_used = 0
  AND expires_at > NOW()
LIMIT 1;

-- If this returns empty, the API will say "Invalid email or OTP"


-- ===================================================
-- STEP 6: Check if user exists in users table
-- ===================================================
SELECT user_id, user_email, user_password, hash_password, created_at
FROM users 
WHERE user_email = 'your_email@example.com';  -- Replace

-- If empty, user doesn't exist


-- ===================================================
-- STEP 7: Check if user exists in subuser table
-- ===================================================
SELECT subuser_id, subuser_email, subuser_password, CreatedAt
FROM subuser 
WHERE subuser_email = 'your_email@example.com';  -- Replace


-- ===================================================
-- STEP 8: Check timezone issues
-- ===================================================
SELECT 
    email,
    expires_at,
    NOW() as server_local_time,
    UTC_TIMESTAMP() as server_utc_time,
  expires_at > NOW() as valid_local_time,
    expires_at > UTC_TIMESTAMP() as valid_utc_time,
    CASE
        WHEN expires_at > NOW() THEN '✅ Valid (Local)'
   WHEN expires_at > UTC_TIMESTAMP() THEN '✅ Valid (UTC)'
        ELSE '❌ Expired'
    END as timezone_check
FROM forgot_password_requests
ORDER BY created_at DESC
LIMIT 5;


-- ===================================================
-- CLEANUP COMMANDS
-- ===================================================

-- Delete all expired requests
DELETE FROM forgot_password_requests 
WHERE expires_at < NOW();

-- Delete all used requests
DELETE FROM forgot_password_requests 
WHERE is_used = 1;

-- Delete requests for specific email (start fresh)
DELETE FROM forgot_password_requests 
WHERE email = 'your_email@example.com';  -- Replace

-- Delete all requests (nuclear option)
-- DELETE FROM forgot_password_requests;


-- ===================================================
-- CREATE TEST USER (if needed)
-- ===================================================
INSERT INTO users (user_email, user_password, hash_password, created_at, updated_at) 
VALUES (
    'test@example.com',          -- Email
    'TestPassword@123',          -- Plain password
    '$2a$11$abcdefghijklmnopqrstuvwxyz',  -- BCrypt hash
    NOW(),
    NOW()
) ON DUPLICATE KEY UPDATE user_email = user_email;


-- ===================================================
-- VERIFY PASSWORD RESET WORKED
-- ===================================================
SELECT 
    user_email,
    user_password,
    hash_password,
    updated_at,
    CASE 
        WHEN updated_at > DATE_SUB(NOW(), INTERVAL 5 MINUTE) THEN '✅ Recently Updated'
        ELSE '⏰ Old Update'
    END as update_status
FROM users
WHERE user_email = 'your_email@example.com'  -- Replace
ORDER BY updated_at DESC;


-- ===================================================
-- SUMMARY REPORT
-- ===================================================
SELECT 
    '=== FORGOT PASSWORD REQUESTS SUMMARY ===' as report,
    '' as spacer;

SELECT 
    'Total Requests' as metric,
    COUNT(*) as count
FROM forgot_password_requests
UNION ALL
SELECT 
 'Active Requests',
    COUNT(*)
FROM forgot_password_requests
WHERE is_used = 0 AND expires_at > NOW()
UNION ALL
SELECT 
    'Used Requests',
    COUNT(*)
FROM forgot_password_requests
WHERE is_used = 1
UNION ALL
SELECT 
    'Expired Requests',
    COUNT(*)
FROM forgot_password_requests
WHERE is_used = 0 AND expires_at < NOW();


-- ===================================================
-- MOST COMMON ISSUES DIAGNOSTIC
-- ===================================================

-- Issue 1: Check for case sensitivity problems
SELECT 
    email,
    LOWER(email) as lowercase_email,
  otp,
    'Check if email case matches exactly' as note
FROM forgot_password_requests
ORDER BY created_at DESC
LIMIT 5;

-- Issue 2: Check for whitespace in OTP or email
SELECT 
  id,
    CONCAT('[', email, ']') as email_with_brackets,
    CONCAT('[', otp, ']') as otp_with_brackets,
    LENGTH(email) as email_length,
    LENGTH(otp) as otp_length,
    'OTP should be exactly 6 characters' as note
FROM forgot_password_requests
ORDER BY created_at DESC
LIMIT 5;

-- Issue 3: Check for duplicate active requests
SELECT 
    email,
    COUNT(*) as active_count,
    'Should have max 3 active per email' as note
FROM forgot_password_requests
WHERE is_used = 0 AND expires_at > NOW()
GROUP BY email
HAVING COUNT(*) > 3;

