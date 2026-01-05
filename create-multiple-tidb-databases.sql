-- Create Multiple Databases in TiDB for Testing

-- ============================================
-- CREATE MULTIPLE DATABASES
-- ============================================

-- Main Application Database
CREATE DATABASE IF NOT EXISTS `Cloud_Erase__App` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_bin;

-- Cloud Erase Database
CREATE DATABASE IF NOT EXISTS `Cloud_Erase` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_bin;

-- Analytics Database
CREATE DATABASE IF NOT EXISTS `Analytics_DB` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_bin;

-- Reporting Database
CREATE DATABASE IF NOT EXISTS `Reporting_DB` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_bin;

-- Logs Database
CREATE DATABASE IF NOT EXISTS `Logs_DB` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_bin;

-- ============================================
-- VERIFY DATABASES
-- ============================================

SHOW DATABASES LIKE '%Cloud_Erase%';
SHOW DATABASES LIKE '%Analytics%';
SHOW DATABASES LIKE '%Reporting%';
SHOW DATABASES LIKE '%Logs%';

-- ============================================
-- CREATE TEST TABLES IN EACH DATABASE
-- ============================================

-- Cloud_Erase__App Database
USE `Cloud_Erase__App`;

CREATE TABLE IF NOT EXISTS `test_app_table` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(255) NOT NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `test_app_table` (`name`) VALUES ('Test from App DB');

-- Cloud_Erase Database
USE `Cloud_Erase`;

CREATE TABLE IF NOT EXISTS `test_cloud_table` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
  `description` VARCHAR(255) NOT NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `test_cloud_table` (`description`) VALUES ('Test from Cloud_Erase DB');

-- Analytics Database
USE `Analytics_DB`;

CREATE TABLE IF NOT EXISTS `test_analytics_table` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `metric_name` VARCHAR(255) NOT NULL,
    `metric_value` DECIMAL(10,2),
    `recorded_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `test_analytics_table` (`metric_name`, `metric_value`) 
VALUES ('User Count', 100.00);

-- Reporting Database
USE `Reporting_DB`;

CREATE TABLE IF NOT EXISTS `test_reports_table` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `report_name` VARCHAR(255) NOT NULL,
    `report_type` VARCHAR(100),
    `generated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `test_reports_table` (`report_name`, `report_type`) 
VALUES ('Monthly Report', 'Summary');

-- Logs Database
USE `Logs_DB`;

CREATE TABLE IF NOT EXISTS `test_logs_table` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `log_level` VARCHAR(50) NOT NULL,
    `log_message` VARCHAR(1000) NOT NULL,
    `logged_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `test_logs_table` (`log_level`, `log_message`) 
VALUES ('INFO', 'Test log entry');

-- ============================================
-- VERIFICATION QUERIES
-- ============================================

-- Check data in each database
SELECT 'Cloud_Erase__App' as Database, COUNT(*) as RecordCount FROM `Cloud_Erase__App`.`test_app_table`
UNION ALL
SELECT 'Cloud_Erase', COUNT(*) FROM `Cloud_Erase`.`test_cloud_table`
UNION ALL
SELECT 'Analytics_DB', COUNT(*) FROM `Analytics_DB`.`test_analytics_table`
UNION ALL
SELECT 'Reporting_DB', COUNT(*) FROM `Reporting_DB`.`test_reports_table`
UNION ALL
SELECT 'Logs_DB', COUNT(*) FROM `Logs_DB`.`test_logs_table`;

-- ============================================
-- GRANT PERMISSIONS (if needed)
-- ============================================

-- GRANT ALL PRIVILEGES ON `Cloud_Erase__App`.* TO '2tdeFNZMcsWKkDR.root'@'%';
-- GRANT ALL PRIVILEGES ON `Cloud_Erase`.* TO '2tdeFNZMcsWKkDR.root'@'%';
-- GRANT ALL PRIVILEGES ON `Analytics_DB`.* TO '2tdeFNZMcsWKkDR.root'@'%';
-- GRANT ALL PRIVILEGES ON `Reporting_DB`.* TO '2tdeFNZMcsWKkDR.root'@'%';
-- GRANT ALL PRIVILEGES ON `Logs_DB`.* TO '2tdeFNZMcsWKkDR.root'@'%';
-- FLUSH PRIVILEGES;

-- ============================================
-- CLEANUP (if needed)
-- ============================================

-- DROP DATABASE IF EXISTS `Cloud_Erase__App`;
-- DROP DATABASE IF EXISTS `Cloud_Erase`;
-- DROP DATABASE IF EXISTS `Analytics_DB`;
-- DROP DATABASE IF EXISTS `Reporting_DB`;
-- DROP DATABASE IF EXISTS `Logs_DB`;
