-- =============================================
-- COMPLETE DATA MIGRATION SCRIPT
-- From Main DB (tech) to Private Cloud (cloud_erase__private)
-- =============================================

USE cloud_erase__private;

-- =============================================
-- STEP 1: Create Missing Tables
-- =============================================

-- 1A: Permissions Table
CREATE TABLE IF NOT EXISTS `Permissions` (
    `PermissionId` INT AUTO_INCREMENT PRIMARY KEY,
    `PermissionName` VARCHAR(100) NOT NULL UNIQUE,
    `Description` VARCHAR(500),
    `Category` VARCHAR(100),
    `IsActive` BOOLEAN DEFAULT TRUE,
    `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_permission_name (`PermissionName`),
    INDEX idx_category (`Category`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 1B: RolePermissions Table
CREATE TABLE IF NOT EXISTS `RolePermissions` (
    `RolePermissionId` INT AUTO_INCREMENT PRIMARY KEY,
    `RoleId` INT NOT NULL,
    `PermissionId` INT NOT NULL,
    `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_role (`RoleId`),
    INDEX idx_permission (`PermissionId`),
    UNIQUE KEY unique_role_permission (`RoleId`, `PermissionId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 1C: UserRoles Table
CREATE TABLE IF NOT EXISTS `UserRoles` (
    `UserRoleId` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    `RoleId` INT NOT NULL,
    `AssignedByEmail` VARCHAR(255),
    `AssignedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user (`UserId`),
INDEX idx_role (`RoleId`),
  UNIQUE KEY unique_user_role (`UserId`, `RoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SELECT '✅ Step 1: Missing tables created' as Status;

-- =============================================
-- STEP 2: Copy Roles
-- =============================================
INSERT INTO Roles (RoleId, RoleName, Description, HierarchyLevel, IsActive, CreatedAt, UpdatedAt)
SELECT RoleId, RoleName, Description, HierarchyLevel, IsActive, CreatedAt, UpdatedAt
FROM tech.Roles
WHERE RoleId IN (1, 2, 3, 4, 5)
ON DUPLICATE KEY UPDATE
    RoleName = VALUES(RoleName),
    Description = VALUES(Description);

SELECT '✅ Step 2: Roles copied' as Status, COUNT(*) as Total FROM Roles;

-- =============================================
-- STEP 3: Copy Permissions
-- =============================================
INSERT INTO Permissions (PermissionId, PermissionName, Description, Category, IsActive, CreatedAt, UpdatedAt)
SELECT PermissionId, PermissionName, Description, Category, IsActive, CreatedAt, UpdatedAt
FROM tech.Permissions
ON DUPLICATE KEY UPDATE
    PermissionName = VALUES(PermissionName);

SELECT '✅ Step 3: Permissions copied' as Status, COUNT(*) as Total FROM Permissions;

-- =============================================
-- STEP 4: Copy RolePermissions
-- =============================================
INSERT INTO RolePermissions (RolePermissionId, RoleId, PermissionId, CreatedAt)
SELECT RolePermissionId, RoleId, PermissionId, CreatedAt
FROM tech.RolePermissions
WHERE RoleId IN (1, 2, 3, 4, 5)
ON DUPLICATE KEY UPDATE
    RoleId = VALUES(RoleId);

SELECT '✅ Step 4: RolePermissions copied' as Status, COUNT(*) as Total FROM RolePermissions;

-- =============================================
-- STEP 5: Copy User (devste@gmail.com)
-- =============================================
INSERT INTO users (
    user_id, user_name, user_email, user_password, hash_password, 
    phone_number, department, user_group, user_role, license_allocation,
    status, timezone, domain, organization_name, is_domain_admin,
    is_private_cloud, private_api, payment_details_json, license_details_json,
    last_login, last_logout, created_at, updated_at
)
SELECT 
    user_id, user_name, user_email, user_password, hash_password,
    phone_number, department, user_group, user_role, license_allocation,
  status, timezone, domain, organization_name, is_domain_admin,
 is_private_cloud, private_api, payment_details_json, license_details_json,
    last_login, last_logout, created_at, updated_at
FROM tech.users
WHERE user_email = 'devste@gmail.com'
ON DUPLICATE KEY UPDATE
 user_name = VALUES(user_name),
    is_private_cloud = VALUES(is_private_cloud);

SELECT '✅ Step 5: User copied' as Status, COUNT(*) as Total FROM users;

-- =============================================
-- STEP 6: Copy UserRoles
-- =============================================
INSERT INTO UserRoles (UserRoleId, UserId, RoleId, AssignedByEmail, AssignedAt)
SELECT ur.UserRoleId, ur.UserId, ur.RoleId, ur.AssignedByEmail, ur.AssignedAt
FROM tech.UserRoles ur
INNER JOIN tech.users u ON ur.UserId = u.user_id
WHERE u.user_email = 'devste@gmail.com'
ON DUPLICATE KEY UPDATE
    RoleId = VALUES(RoleId);

SELECT '✅ Step 6: UserRoles copied' as Status, COUNT(*) as Total FROM UserRoles;

-- =============================================
-- STEP 7: Copy Subusers (if any)
-- =============================================
INSERT INTO subuser (
    subuser_id, subuser_email, subuser_password, subuser_username,
    user_email, superuser_id, Name, Phone, Department, Role,
    PermissionsJson, AssignedMachines, MaxMachines, MachineIdsJson,
    LicenseIdsJson, GroupId, subuser_group, license_allocation,
    status, activity_status, timezone, domain, organization_name,
    IsEmailVerified, CanCreateSubusers, CanViewReports, CanManageMachines,
    CanAssignLicenses, EmailNotifications, SystemAlerts, LastLoginIp,
    last_login, last_logout, FailedLoginAttempts, LockedUntil,
    CreatedBy, CreatedAt, UpdatedAt, UpdatedBy, Notes
)
SELECT 
    subuser_id, subuser_email, subuser_password, subuser_username,
    user_email, superuser_id, Name, Phone, Department, Role,
    PermissionsJson, AssignedMachines, MaxMachines, MachineIdsJson,
    LicenseIdsJson, GroupId, subuser_group, license_allocation,
    status, activity_status, timezone, domain, organization_name,
    IsEmailVerified, CanCreateSubusers, CanViewReports, CanManageMachines,
    CanAssignLicenses, EmailNotifications, SystemAlerts, LastLoginIp,
    last_login, last_logout, FailedLoginAttempts, LockedUntil,
    CreatedBy, CreatedAt, UpdatedAt, UpdatedBy, Notes
FROM tech.subuser
WHERE user_email = 'devste@gmail.com'
ON DUPLICATE KEY UPDATE
    Name = VALUES(Name);

SELECT '✅ Step 7: Subusers copied' as Status, COUNT(*) as Total FROM subuser;

-- =============================================
-- STEP 8: Copy SubuserRoles
-- =============================================
INSERT INTO SubuserRoles (SubuserRoleId, SubuserId, RoleId, AssignedByEmail, AssignedAt)
SELECT sr.SubuserRoleId, sr.SubuserId, sr.RoleId, sr.AssignedByEmail, sr.AssignedAt
FROM tech.SubuserRoles sr
INNER JOIN tech.subuser s ON sr.SubuserId = s.subuser_id
WHERE s.user_email = 'devste@gmail.com'
ON DUPLICATE KEY UPDATE
  RoleId = VALUES(RoleId);

SELECT '✅ Step 8: SubuserRoles copied' as Status, COUNT(*) as Total FROM SubuserRoles;

-- =============================================
-- FINAL VERIFICATION
-- =============================================
SELECT '========== FINAL SUMMARY ==========' as Title;

SELECT 'Roles' as Table_Name, COUNT(*) as Row_Count FROM Roles
UNION ALL
SELECT 'Permissions', COUNT(*) FROM Permissions
UNION ALL
SELECT 'RolePermissions', COUNT(*) FROM RolePermissions
UNION ALL
SELECT 'users', COUNT(*) FROM users
UNION ALL
SELECT 'UserRoles', COUNT(*) FROM UserRoles
UNION ALL
SELECT 'subuser', COUNT(*) FROM subuser
UNION ALL
SELECT 'SubuserRoles', COUNT(*) FROM SubuserRoles;

-- Check User's Complete Details
SELECT 
    u.user_email,
    u.user_name,
    u.is_private_cloud,
    r.RoleName,
    COUNT(DISTINCT p.PermissionId) as TotalPermissions
FROM users u
LEFT JOIN UserRoles ur ON u.user_id = ur.UserId
LEFT JOIN Roles r ON ur.RoleId = r.RoleId
LEFT JOIN RolePermissions rp ON r.RoleId = rp.RoleId
LEFT JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE u.user_email = 'devste@gmail.com'
GROUP BY u.user_email, u.user_name, u.is_private_cloud, r.RoleName;

SELECT '✅ Migration Complete!' as Status;
