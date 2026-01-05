-- ✅ CREATE MACHINES TABLE IN PRIVATE DATABASE (TiDB Cloud)
-- Database: Cloud_Erase
-- User: devste@gmail.com

USE Cloud_Erase;

-- Drop table if exists (for clean setup)
DROP TABLE IF EXISTS `machines`;

-- Create machines table with all required fields
CREATE TABLE `machines` (
  -- Primary Key
  `fingerprint_hash` VARCHAR(255) NOT NULL COMMENT 'Unique machine fingerprint',
  
  -- Machine Identification
  `mac_address` VARCHAR(255) NOT NULL COMMENT 'MAC address',
  `physical_drive_id` VARCHAR(255) NOT NULL COMMENT 'Physical drive ID',
  `cpu_id` VARCHAR(255) NOT NULL COMMENT 'CPU ID',
  `bios_serial` VARCHAR(255) NOT NULL COMMENT 'BIOS serial number',
  
  -- System Information
  `os_version` VARCHAR(255) NOT NULL COMMENT 'Operating system version',
  `vm_status` VARCHAR(50) DEFAULT 'unknown' COMMENT 'Virtual machine status',
  
  -- Ownership
  `user_email` VARCHAR(255) DEFAULT NULL COMMENT 'Owner user email',
  `subuser_email` VARCHAR(255) DEFAULT NULL COMMENT 'Subuser email (if owned by subuser)',
  
  -- License Information
  `license_activated` TINYINT(1) DEFAULT 0 COMMENT 'License activation status',
  `license_activation_date` DATETIME DEFAULT NULL COMMENT 'License activation date',
  `license_days_valid` INT DEFAULT 0 COMMENT 'License validity duration in days',
  `license_details_json` TEXT COMMENT 'License details in JSON format',
  
  -- Additional Details
  `machine_details_json` TEXT COMMENT 'Machine hardware details in JSON format',
  `demo_usage_count` INT DEFAULT 0 COMMENT 'Demo usage counter',
  
  -- Timestamps
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation time',
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Last update time',
  
  -- Constraints
  PRIMARY KEY (`fingerprint_hash`),
  UNIQUE KEY `unique_mac_address` (`mac_address`),
  KEY `idx_user_email` (`user_email`),
  KEY `idx_subuser_email` (`subuser_email`),
  KEY `idx_license_activated` (`license_activated`),
  KEY `idx_created_at` (`created_at`),
  KEY `idx_license_activation_date` (`license_activation_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci
COMMENT='Machines/devices registered by users';

-- Verify table creation
SELECT 
    TABLE_NAME,
    TABLE_TYPE,
    ENGINE,
    TABLE_ROWS,
    CREATE_TIME,
    TABLE_COMMENT
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'Cloud_Erase'
  AND TABLE_NAME = 'machines';

-- Show table structure
DESCRIBE machines;

-- Show indexes
SHOW INDEX FROM machines;

SELECT 
    '✅ Machines table created successfully in Cloud_Erase database!' as STATUS;
