-- ✅ Diagnostic Script: Check Login/Logout Times in Database
-- Run this to verify if times are actually being updated

-- ==================================================
-- 1. Check Users Table - Last Login/Logout Times
-- ==================================================
SELECT 
    user_email,
    user_name,
    last_login,
    last_logout,
    CASE 
  WHEN last_login IS NULL THEN 'Never Logged In'
        WHEN last_logout IS NULL THEN 'Currently Logged In (No Logout Yet)'
        WHEN last_login > last_logout THEN 'Currently Logged In'
        ELSE 'Logged Out'
  END AS login_status,
    TIMESTAMPDIFF(MINUTE, last_logout, last_login) as session_duration_minutes,
  created_at,
    updated_at
FROM users
ORDER BY last_login DESC
LIMIT 20;

-- ==================================================
-- 2. Check Subusers Table - Last Login/Logout Times
-- ==================================================
SELECT 
    subuser_email,
    Name as subuser_name,
    user_email as parent_email,
    last_login,
    last_logout,
    LastLoginIp,
    CASE 
        WHEN last_login IS NULL THEN 'Never Logged In'
   WHEN last_logout IS NULL THEN 'Currently Logged In (No Logout Yet)'
  WHEN last_login > last_logout THEN 'Currently Logged In'
        ELSE 'Logged Out'
    END AS login_status,
TIMESTAMPDIFF(MINUTE, last_logout, last_login) as session_duration_minutes,
    CreatedAt,
    UpdatedAt
FROM subuser
ORDER BY last_login DESC
LIMIT 20;

-- ==================================================
-- 3. Check Sessions Table - Login/Logout Times
-- ==================================================
SELECT 
    session_id,
    user_email,
    login_time,
    logout_time,
    session_status,
    ip_address,
    device_info,
    CASE 
        WHEN session_status = 'active' THEN 'Active Session'
        WHEN session_status = 'closed' THEN 'Closed Session'
        ELSE 'Unknown Status'
    END AS status_description,
    TIMESTAMPDIFF(MINUTE, login_time, COALESCE(logout_time, NOW())) as session_duration_minutes
FROM Sessions
ORDER BY login_time DESC
LIMIT 20;

-- ==================================================
-- 4. Compare Login Times (Users vs Sessions)
-- ==================================================
SELECT 
    u.user_email,
    u.user_name,
u.last_login as users_table_last_login,
    s.login_time as sessions_table_login_time,
    CASE 
   WHEN u.last_login = s.login_time THEN '✅ MATCH'
        WHEN u.last_login IS NULL THEN '❌ NULL in users table'
        WHEN s.login_time IS NULL THEN '❌ NULL in sessions table'
  ELSE '⚠️  MISMATCH'
    END AS comparison,
    TIMESTAMPDIFF(SECOND, u.last_login, s.login_time) as time_difference_seconds
FROM users u
LEFT JOIN Sessions s ON u.user_email = s.user_email
WHERE s.session_id = (
    SELECT session_id 
 FROM Sessions 
    WHERE user_email = u.user_email 
 ORDER BY login_time DESC 
    LIMIT 1
)
ORDER BY u.last_login DESC
LIMIT 20;

-- ==================================================
-- 5. Compare Logout Times (Users vs Sessions)
-- ==================================================
SELECT 
    u.user_email,
    u.user_name,
    u.last_logout as users_table_last_logout,
    s.logout_time as sessions_table_logout_time,
    s.session_status,
    CASE 
 WHEN u.last_logout = s.logout_time THEN '✅ MATCH'
  WHEN u.last_logout IS NULL AND s.logout_time IS NULL THEN '✅ Both NULL (Active)'
        WHEN u.last_logout IS NULL THEN '❌ NULL in users table'
        WHEN s.logout_time IS NULL THEN '❌ NULL in sessions table'
        ELSE '⚠️  MISMATCH'
END AS comparison,
  TIMESTAMPDIFF(SECOND, u.last_logout, s.logout_time) as time_difference_seconds
FROM users u
LEFT JOIN Sessions s ON u.user_email = s.user_email
WHERE s.session_id = (
    SELECT session_id 
    FROM Sessions 
  WHERE user_email = u.user_email 
    ORDER BY logout_time DESC 
    LIMIT 1
)
ORDER BY u.last_logout DESC
LIMIT 20;

-- ==================================================
-- 6. Check Active Sessions
-- ==================================================
SELECT 
    user_email,
    login_time,
    session_status,
    ip_address,
    TIMESTAMPDIFF(MINUTE, login_time, NOW()) as minutes_since_login,
    CASE 
        WHEN TIMESTAMPDIFF(MINUTE, login_time, NOW()) > 60 THEN '⚠️  Session older than 1 hour'
        WHEN TIMESTAMPDIFF(MINUTE, login_time, NOW()) > 480 THEN '❌ Session older than 8 hours (token expired)'
        ELSE '✅ Active session'
  END AS session_health
FROM Sessions
WHERE session_status = 'active'
ORDER BY login_time DESC;

-- ==================================================
-- 7. Check NULL Values
-- ==================================================
SELECT 
    'Users with NULL last_login' as check_type,
    COUNT(*) as count
FROM users
WHERE last_login IS NULL

UNION ALL

SELECT 
    'Users with NULL last_logout' as check_type,
    COUNT(*) as count
FROM users
WHERE last_logout IS NULL

UNION ALL

SELECT 
    'Subusers with NULL last_login' as check_type,
    COUNT(*) as count
FROM subuser
WHERE last_login IS NULL

UNION ALL

SELECT 
    'Subusers with NULL last_logout' as check_type,
    COUNT(*) as count
FROM subuser
WHERE last_logout IS NULL

UNION ALL

SELECT 
    'Sessions with NULL logout_time' as check_type,
    COUNT(*) as count
FROM Sessions
WHERE logout_time IS NULL;

-- ==================================================
-- 8. Recent Login/Logout Activity (Last 24 hours)
-- ==================================================
SELECT 
    u.user_email,
    u.user_name,
'user' as user_type,
    u.last_login,
    u.last_logout,
    TIMESTAMPDIFF(HOUR, u.last_login, NOW()) as hours_since_login
FROM users u
WHERE u.last_login >= DATE_SUB(NOW(), INTERVAL 24 HOUR)

UNION ALL

SELECT 
  s.subuser_email,
  s.Name as user_name,
    'subuser' as user_type,
    s.last_login,
    s.last_logout,
  TIMESTAMPDIFF(HOUR, s.last_login, NOW()) as hours_since_login
FROM subuser s
WHERE s.last_login >= DATE_SUB(NOW(), INTERVAL 24 HOUR)

ORDER BY last_login DESC;

-- ==================================================
-- 9. Check Database Server Time
-- ==================================================
SELECT 
    NOW() as current_server_time,
    UTC_TIMESTAMP() as current_utc_time,
    @@global.time_zone as global_timezone,
    @@session.time_zone as session_timezone;

-- ==================================================
-- 10. Verify Column Types
-- ==================================================
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME IN ('users', 'subuser', 'Sessions')
AND COLUMN_NAME IN ('last_login', 'last_logout', 'login_time', 'logout_time', 'created_at', 'updated_at')
ORDER BY TABLE_NAME, COLUMN_NAME;
