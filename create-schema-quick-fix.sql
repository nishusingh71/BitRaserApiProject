-- Quick Fix: Create All Tables for Cloud_Erase_Private
-- Run this script if automatic schema initialization fails

-- Connect first:
-- mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com -P 4000 -u 4WScT7meioLLU3B.root -p89ayiOJGY2055G0g -D Cloud_Erase_Private --ssl-mode=REQUIRED

USE Cloud_Erase_Private;

-- Drop foreign key constraints if recreating
SET FOREIGN_KEY_CHECKS=0;

-- Create all tables
CREATE TABLE IF NOT EXISTS `users` (
    `user_id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_name` VARCHAR(255) NOT NULL,
    `user_email` VARCHAR(255) NOT NULL UNIQUE,
  `user_password` VARCHAR(255) NOT NULL,
    `hash_password` VARCHAR(255),
    `phone_number` VARCHAR(20),
    `department` VARCHAR(100),
    `user_group` VARCHAR(100),
    `user_role` VARCHAR(50),
    `license_allocation` INT DEFAULT 0,
    `status` VARCHAR(50) DEFAULT 'active',
    `timezone` VARCHAR(100),
    `domain` VARCHAR(255),
    `organization_name` VARCHAR(255),
    `is_domain_admin` BOOLEAN DEFAULT FALSE,
    `is_private_cloud` BOOLEAN DEFAULT FALSE,
    `private_api` BOOLEAN DEFAULT FALSE,
    `payment_details_json` JSON,
    `license_details_json` JSON,
    `last_login` TIMESTAMP NULL,
    `last_logout` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_user_email (`user_email`),
    INDEX idx_status (`status`),
    INDEX idx_organization (`organization_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `groups` (
    `group_id` INT AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(100) NOT NULL,
    `description` VARCHAR(500),
    `total_users` INT DEFAULT 0,
    `total_licenses` INT DEFAULT 0,
    `status` VARCHAR(50) DEFAULT 'active',
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_name (`name`),
  INDEX idx_status (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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
    INDEX idx_status (`status`),
    CONSTRAINT fk_subuser_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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
    INDEX idx_synced (`synced`),
    CONSTRAINT fk_report_user FOREIGN KEY (`client_email`) REFERENCES `users`(`user_email`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `machines` (
    `fingerprint_hash` VARCHAR(255) PRIMARY KEY,
    `mac_address` VARCHAR(255) NOT NULL,
    `physical_drive_id` VARCHAR(255) NOT NULL,
    `cpu_id` VARCHAR(255) NOT NULL,
  `bios_serial` VARCHAR(255) NOT NULL,
    `os_version` VARCHAR(255),
    `user_email` VARCHAR(255),
    `subuser_email` VARCHAR(255),
    `license_details_json` JSON,
    `machine_details_json` JSON,
    `license_activation_date` TIMESTAMP NULL,
 `license_days_valid` INT DEFAULT 0,
    `license_activated` BOOLEAN DEFAULT FALSE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_mac_address (`mac_address`),
    INDEX idx_user_email (`user_email`),
    INDEX idx_subuser_email (`subuser_email`),
    CONSTRAINT fk_machine_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE SET NULL,
    CONSTRAINT fk_machine_subuser FOREIGN KEY (`subuser_email`) REFERENCES `subuser`(`subuser_email`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `sessions` (
    `session_id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_email` VARCHAR(255) NOT NULL,
    `ip_address` VARCHAR(45),
    `device_info` VARCHAR(1000),
    `session_status` VARCHAR(50) NOT NULL DEFAULT 'active',
    `login_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `logout_time` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_user_email (`user_email`),
    INDEX idx_status (`session_status`),
    INDEX idx_login_time (`login_time`),
    CONSTRAINT fk_session_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `logs` (
    `log_id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_email` VARCHAR(255),
    `log_level` VARCHAR(50) NOT NULL,
    `log_message` VARCHAR(2000) NOT NULL,
    `log_details_json` JSON,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user_email (`user_email`),
INDEX idx_log_level (`log_level`),
    INDEX idx_created_at (`created_at`),
    CONSTRAINT fk_log_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `commands` (
    `Command_id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_email` VARCHAR(255),
    `command_text` VARCHAR(2000) NOT NULL,
    `command_json` JSON,
    `command_status` VARCHAR(100) DEFAULT 'pending',
    `issued_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `executed_at` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user_email (`user_email`),
    INDEX idx_status (`command_status`),
    CONSTRAINT fk_command_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS=1;

-- Verify tables created
SHOW TABLES;

-- Show table structures
SELECT 'users' AS table_name;
DESCRIBE users;

SELECT 'groups' AS table_name;
DESCRIBE groups;

SELECT 'subuser' AS table_name;
DESCRIBE subuser;

SELECT 'machines' AS table_name;
DESCRIBE machines;

SELECT 'audit_reports' AS table_name;
DESCRIBE audit_reports;

SELECT 'sessions' AS table_name;
DESCRIBE sessions;

SELECT 'logs' AS table_name;
DESCRIBE logs;

SELECT 'commands' AS table_name;
DESCRIBE commands;

-- Show foreign key relationships
SELECT 
    TABLE_NAME,
    CONSTRAINT_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'Cloud_Erase_Private'
  AND REFERENCED_TABLE_NAME IS NOT NULL;

SELECT '✅ Schema creation complete!' AS status;
