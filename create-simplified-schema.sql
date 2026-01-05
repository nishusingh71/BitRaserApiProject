-- Simplified Private Cloud Schema - Only 2 Tables
-- Run this if automatic schema initialization fails
-- Connect: mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com -P 4000 -u 4WScT7meioLLU3B.root -p89ayiOJGY2055G0g -D Cloud_Erase_Private --ssl-mode=REQUIRED

USE Cloud_Erase_Private;

-- ========================================
-- Table 1: audit_reports (No dependencies)
-- ========================================
CREATE TABLE IF NOT EXISTS `audit_reports` (
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

-- Verify
SELECT 'audit_reports' AS table_name;
DESCRIBE audit_reports;

-- ========================================
-- Table 2: subuser (No dependencies)
-- ========================================
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
    INDEX idx_status (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Verify
SELECT 'subuser' AS table_name;
DESCRIBE subuser;

-- ========================================
-- VERIFICATION
-- ========================================

-- Show all tables
SHOW TABLES;

-- Expected output:
-- +----------------------------+
-- | Tables_in_Cloud_Erase_Private |
-- +----------------------------+
-- | audit_reports    |
-- | subuser       |
-- +----------------------------+

-- Count rows in tables
SELECT 'audit_reports' AS table_name, COUNT(*) AS row_count FROM audit_reports
UNION ALL
SELECT 'subuser' AS table_name, COUNT(*) AS row_count FROM subuser;

SELECT '✅ Simplified schema created successfully!' AS status;
SELECT 'Created 2 tables: audit_reports, subuser' AS info;
