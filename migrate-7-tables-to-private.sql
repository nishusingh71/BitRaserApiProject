-- ========================================
-- MIGRATE 7 TABLES TO PRIVATE CLOUD DB
-- ========================================
-- Author: GitHub Copilot
-- Date: 2025-01-15
-- Purpose: Create 7 tables in Private Cloud DB and copy system data

USE cloud_erase__private; -- Your Private Cloud DB

-- ========================================
-- STEP 1: CREATE ALL 7 TABLES
-- ========================================

-- 1. AuditReports Table (✅ FIXED: Changed from audit_reports to AuditReports)
CREATE TABLE IF NOT EXISTS `AuditReports` (
    `report_id` INT AUTO_INCREMENT PRIMARY KEY,
 `client_email` VARCHAR(255) NOT NULL,
 `report_name` VARCHAR(255) NOT NULL,
    `erasure_method` VARCHAR(255) NOT NULL,
    `report_datetime` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `report_details_json` JSON NOT NULL,
    `synced` BOOLEAN DEFAULT FALSE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_client_email (`client_email`),
    INDEX idx_report_date (`report_datetime`),
    INDEX idx_synced (`synced`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Subuser Table
CREATE TABLE IF NOT EXISTS `subuser` (
    `subuser_id` INT AUTO_INCREMENT PRIMARY KEY,
    `subuser_email` VARCHAR(255) NOT NULL UNIQUE,
 `subuser_password` VARCHAR(255) NOT NULL,
    `subuser_username` VARCHAR(100),
 `user_email` VARCHAR(255) NOT NULL,
    `superuser_id` INT,
    `Name` VARCHAR(100),
    `Phone` VARCHAR(20),
    `Department` VARCHAR(100),
    `Role` VARCHAR(50) NOT NULL DEFAULT 'subuser',
    `PermissionsJson` JSON,
    `AssignedMachines` INT DEFAULT 0,
    `MaxMachines` INT DEFAULT 5,
    `MachineIdsJson` JSON,
  `LicenseIdsJson` JSON,
    `GroupId` INT,
    `subuser_group` VARCHAR(100),
    `license_allocation` INT DEFAULT 0,
    `status` VARCHAR(50) DEFAULT 'active',
    `activity_status` VARCHAR(50) DEFAULT 'active',
    `timezone` VARCHAR(100),
    `domain` VARCHAR(255),
 `organization_name` VARCHAR(255),
    `IsEmailVerified` BOOLEAN DEFAULT FALSE,
    `CanCreateSubusers` BOOLEAN DEFAULT FALSE,
    `CanViewReports` BOOLEAN DEFAULT TRUE,
    `CanManageMachines` BOOLEAN DEFAULT FALSE,
    `CanAssignLicenses` BOOLEAN DEFAULT FALSE,
 `EmailNotifications` BOOLEAN DEFAULT TRUE,
    `SystemAlerts` BOOLEAN DEFAULT TRUE,
    `LastLoginIp` VARCHAR(500),
    `last_login` TIMESTAMP NULL,
    `last_logout` TIMESTAMP NULL,
    `FailedLoginAttempts` INT DEFAULT 0,
    `LockedUntil` TIMESTAMP NULL,
    `CreatedBy` INT NOT NULL,
    `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `UpdatedBy` INT,
    `Notes` VARCHAR(500),
    INDEX idx_subuser_email (`subuser_email`),
    INDEX idx_user_email (`user_email`),
    INDEX idx_superuser (`superuser_id`),
    INDEX idx_status (`status`),
    INDEX idx_activity_status (`activity_status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. Roles Table
CREATE TABLE IF NOT EXISTS `Roles` (
    `RoleId` INT AUTO_INCREMENT PRIMARY KEY,
    `RoleName` VARCHAR(100) NOT NULL UNIQUE,
    `Description` VARCHAR(500),
    `HierarchyLevel` INT NOT NULL DEFAULT 5,
    `IsSystemRole` BOOLEAN DEFAULT TRUE,
    `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_role_name (`RoleName`),
    INDEX idx_hierarchy (`HierarchyLevel`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4. Permissions Table
CREATE TABLE IF NOT EXISTS `Permissions` (
    `PermissionId` INT AUTO_INCREMENT PRIMARY KEY,
    `PermissionName` VARCHAR(100) NOT NULL UNIQUE,
    `Description` VARCHAR(500),
    `Category` VARCHAR(50),
    `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_permission_name (`PermissionName`),
    INDEX idx_category (`Category`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 5. RolePermissions Table (Junction Table)
CREATE TABLE IF NOT EXISTS `RolePermissions` (
    `RoleId` INT NOT NULL,
    `PermissionId` INT NOT NULL,
    PRIMARY KEY (`RoleId`, `PermissionId`),
    INDEX idx_role (`RoleId`),
    INDEX idx_permission (`PermissionId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 6. SubuserRoles Table (Junction Table)
CREATE TABLE IF NOT EXISTS `SubuserRoles` (
    `SubuserId` INT NOT NULL,
    `RoleId` INT NOT NULL,
    `AssignedByEmail` VARCHAR(255),
    `AssignedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`SubuserId`, `RoleId`),
    INDEX idx_subuser (`SubuserId`),
    INDEX idx_role (`RoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 7. Routes Table
CREATE TABLE IF NOT EXISTS `Routes` (
    `RouteId` INT AUTO_INCREMENT PRIMARY KEY,
    `RoutePath` VARCHAR(255) NOT NULL,
    `HttpMethod` VARCHAR(10) NOT NULL,
    `Description` VARCHAR(500),
    `RequiresAuthentication` BOOLEAN DEFAULT TRUE,
    `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE INDEX idx_route_method (`RoutePath`, `HttpMethod`),
    INDEX idx_path (`RoutePath`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================
-- STEP 2: COPY SYSTEM TABLES DATA
-- (Roles, Permissions, RolePermissions, Routes)
-- ========================================

-- Copy Roles (ALL system roles)
INSERT INTO cloud_erase__private.Roles (RoleId, RoleName, Description, HierarchyLevel, IsSystemRole, CreatedAt)
SELECT RoleId, RoleName, Description, HierarchyLevel, IsSystemRole, CreatedAt
FROM tech.Roles
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
 HierarchyLevel = VALUES(HierarchyLevel),
    IsSystemRole = VALUES(IsSystemRole);

-- Copy Permissions (ALL system permissions)
INSERT INTO cloud_erase__private.Permissions (PermissionId, PermissionName, Description, Category, CreatedAt)
SELECT PermissionId, PermissionName, Description, Category, CreatedAt
FROM tech.Permissions
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    Category = VALUES(Category);

-- Copy RolePermissions (ALL role-permission mappings)
INSERT INTO cloud_erase__private.RolePermissions (RoleId, PermissionId)
SELECT RoleId, PermissionId
FROM tech.RolePermissions
ON DUPLICATE KEY UPDATE RoleId = VALUES(RoleId);

-- Copy Routes (ALL API routes)
INSERT INTO cloud_erase__private.Routes (RouteId, RoutePath, HttpMethod, Description, RequiresAuthentication, CreatedAt)
SELECT RouteId, RoutePath, HttpMethod, Description, RequiresAuthentication, CreatedAt
FROM tech.Routes
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    RequiresAuthentication = VALUES(RequiresAuthentication);

-- ========================================
-- STEP 3: VERIFY MIGRATION
-- ========================================

-- Check table creation
SELECT 
  TABLE_NAME,
    TABLE_ROWS,
    CREATE_TIME
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'cloud_erase__private'
AND TABLE_NAME IN ('AuditReports', 'subuser', 'Roles', 'Permissions', 'RolePermissions', 'SubuserRoles', 'Routes')
ORDER BY TABLE_NAME;

-- Check data counts
SELECT 'AuditReports' AS TableName, COUNT(*) AS RecordCount FROM AuditReports
UNION ALL
SELECT 'subuser', COUNT(*) FROM subuser
UNION ALL
SELECT 'Roles', COUNT(*) FROM Roles
UNION ALL
SELECT 'Permissions', COUNT(*) FROM Permissions
UNION ALL
SELECT 'RolePermissions', COUNT(*) FROM RolePermissions
UNION ALL
SELECT 'SubuserRoles', COUNT(*) FROM SubuserRoles
UNION ALL
SELECT 'Routes', COUNT(*) FROM Routes;

-- ========================================
-- STEP 4: USER-SPECIFIC DATA MIGRATION
-- ========================================
-- NOTE: User-specific data (AuditReports, subuser, SubuserRoles) 
-- should be migrated using the API endpoint:
-- POST /api/PrivateCloud/migrate-all-tables
-- This ensures only the current user's data is migrated.

-- Example: Manual migration for specific user (OPTIONAL)
-- Replace 'user@example.com' with actual user email

-- Migrate AuditReports for specific user
-- INSERT INTO cloud_erase__private.AuditReports
-- SELECT * FROM tech.AuditReports
-- WHERE client_email = 'user@example.com'
-- ON DUPLICATE KEY UPDATE report_name = VALUES(report_name);

-- Migrate subusers for specific user
-- INSERT INTO cloud_erase__private.subuser
-- SELECT * FROM tech.subuser
-- WHERE user_email = 'user@example.com'
-- ON DUPLICATE KEY UPDATE Name = VALUES(Name);

-- Migrate SubuserRoles for user's subusers
-- INSERT INTO cloud_erase__private.SubuserRoles
-- SELECT sr.* FROM tech.SubuserRoles sr
-- INNER JOIN tech.subuser s ON sr.SubuserId = s.subuser_id
-- WHERE s.user_email = 'user@example.com'
-- ON DUPLICATE KEY UPDATE AssignedByEmail = VALUES(AssignedByEmail);

-- ========================================
-- MIGRATION COMPLETE
-- ========================================

SELECT 'Migration script completed!' AS Status,
 'System tables copied. Use API endpoint for user-specific data.' AS Note;

-- ========================================
-- TROUBLESHOOTING
-- ========================================

-- If errors occur, check:
-- 1. Database exists: SELECT DATABASE();
-- 2. Tables created: SHOW TABLES;
-- 3. Data copied: SELECT COUNT(*) FROM Roles;
-- 4. Indexes created: SHOW INDEX FROM subuser;

-- To rollback (DROP all tables - USE WITH CAUTION):
-- DROP TABLE IF EXISTS AuditReports;
-- DROP TABLE IF EXISTS subuser;
-- DROP TABLE IF EXISTS SubuserRoles;
-- DROP TABLE IF EXISTS RolePermissions;
-- DROP TABLE IF EXISTS Permissions;
-- DROP TABLE IF EXISTS Roles;
-- DROP TABLE IF EXISTS Routes;
