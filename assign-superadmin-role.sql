-- ✅ ASSIGN SUPERADMIN ROLE TO USER IN MAIN DB
-- This will give user full system access

USE cloud_erase;

-- Step 1: Get user details
SELECT 
    user_id,
    user_email,
    user_name,
    status
FROM users
WHERE user_email = 'devste@gmail.com';

-- Step 2: Check current roles
SELECT 
    u.user_id,
    u.user_email,
    ur.RoleId,
    r.RoleName,
    ur.AssignedByEmail,
  ur.AssignedAt
FROM users u
LEFT JOIN UserRoles ur ON u.user_id = ur.UserId
LEFT JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.user_email = 'devste@gmail.com';

-- Step 3: Remove existing roles (if any)
DELETE FROM UserRoles
WHERE UserId = (SELECT user_id FROM users WHERE user_email = 'devste@gmail.com');

-- Step 4: Assign SuperAdmin role (RoleId = 1)
INSERT INTO UserRoles (UserId, RoleId, AssignedByEmail, AssignedAt)
SELECT 
    user_id,
    1,  -- SuperAdmin role
    'system',
    NOW()
FROM users
WHERE user_email = 'devste@gmail.com';

-- Step 5: Verify assignment
SELECT 
    u.user_id,
    u.user_email,
    u.user_name,
    ur.RoleId,
    r.RoleName,
    r.HierarchyLevel,
    ur.AssignedByEmail,
    ur.AssignedAt
FROM users u
JOIN UserRoles ur ON u.user_id = ur.UserId
JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.user_email = 'devste@gmail.com';

-- Expected Result:
-- user_id | user_email       | RoleId | RoleName   | HierarchyLevel
-- 2       | devste@gmail.com | 1 | SuperAdmin | 1

-- ✅ SUCCESS! User now has SuperAdmin role with full system access
