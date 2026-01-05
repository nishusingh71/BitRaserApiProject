-- ================================================
-- LICENSE ACTIVATION SYSTEM - DATABASE MIGRATION
-- ================================================
-- Run this script to create the required tables
-- ================================================

-- 1. Create licenses table (for activation system)
CREATE TABLE IF NOT EXISTS `licenses` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `license_key` VARCHAR(64) NOT NULL UNIQUE,
  `hwid` VARCHAR(128) NULL,
  `expiry_days` INT NOT NULL DEFAULT 365,
  `edition` VARCHAR(32) NOT NULL DEFAULT 'BASIC',
  `status` VARCHAR(16) NOT NULL DEFAULT 'ACTIVE',
  `server_revision` INT NOT NULL DEFAULT 1,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_seen` DATETIME NULL,
  `user_email` VARCHAR(255) NULL,
  `notes` VARCHAR(500) NULL,
  
  INDEX `idx_license_key` (`license_key`),
  INDEX `idx_hwid` (`hwid`),
  INDEX `idx_status` (`status`),
  INDEX `idx_edition` (`edition`),
  INDEX `idx_user_email` (`user_email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Create license_usage_logs table
CREATE TABLE IF NOT EXISTS `license_usage_logs` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `license_key` VARCHAR(64) NOT NULL,
  `hwid` VARCHAR(128) NULL,
  `action` VARCHAR(50) NOT NULL,
  `user_email` VARCHAR(255) NULL,
  `old_edition` VARCHAR(32) NULL,
  `new_edition` VARCHAR(32) NULL,
  `old_expiry_days` INT NULL,
  `new_expiry_days` INT NULL,
  `ip_address` VARCHAR(45) NULL,
  `user_agent` VARCHAR(500) NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `notes` VARCHAR(500) NULL,
  
  INDEX `idx_license_key` (`license_key`),
  INDEX `idx_action` (`action`),
  INDEX `idx_created_at` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ================================================
-- SAMPLE DATA (Optional - for testing)
-- ================================================

-- Insert sample licenses
INSERT INTO `licenses` (`license_key`, `expiry_days`, `edition`, `status`, `notes`)
VALUES 
  ('TEST-1234-ABCD-5678', 365, 'BASIC', 'ACTIVE', 'Test license - Basic edition'),
  ('DEMO-9876-WXYZ-4321', 90, 'PRO', 'ACTIVE', 'Demo license - Pro edition'),
  ('CORP-AAAA-BBBB-CCCC', 730, 'ENTERPRISE', 'ACTIVE', 'Corporate license - Enterprise edition');

-- ================================================
-- VERIFICATION QUERIES
-- ================================================

-- Check if tables were created
SELECT 'licenses table created' AS status, COUNT(*) AS record_count FROM `licenses`;
SELECT 'license_usage_logs table created' AS status, COUNT(*) AS record_count FROM `license_usage_logs`;

-- View all licenses
SELECT 
    id,
    license_key,
    hwid,
    expiry_days,
    DATE_ADD(created_at, INTERVAL expiry_days DAY) AS expiry_date,
    DATEDIFF(DATE_ADD(created_at, INTERVAL expiry_days DAY), CURDATE()) AS remaining_days,
edition,
    status,
    server_revision,
    created_at,
    last_seen,
    user_email
FROM `licenses`
ORDER BY created_at DESC;

-- ================================================
-- USEFUL QUERIES FOR ADMIN
-- ================================================

-- Get license statistics
SELECT 
    status,
    edition,
    COUNT(*) AS count
FROM `licenses`
GROUP BY status, edition
ORDER BY status, edition;

-- Find expiring licenses (next 30 days)
SELECT 
    license_key,
    edition,
    DATE_ADD(created_at, INTERVAL expiry_days DAY) AS expiry_date,
    DATEDIFF(DATE_ADD(created_at, INTERVAL expiry_days DAY), CURDATE()) AS days_remaining
FROM `licenses`
WHERE status = 'ACTIVE'
  AND DATE_ADD(created_at, INTERVAL expiry_days DAY) <= DATE_ADD(CURDATE(), INTERVAL 30 DAY)
  AND DATE_ADD(created_at, INTERVAL expiry_days DAY) >= CURDATE()
ORDER BY days_remaining ASC;

-- Find expired licenses
SELECT 
    license_key,
    edition,
    DATE_ADD(created_at, INTERVAL expiry_days DAY) AS expiry_date,
    DATEDIFF(CURDATE(), DATE_ADD(created_at, INTERVAL expiry_days DAY)) AS days_expired
FROM `licenses`
WHERE DATE_ADD(created_at, INTERVAL expiry_days DAY) < CURDATE()
ORDER BY days_expired DESC;

-- Recent license activity
SELECT 
    license_key,
    action,
    old_edition,
    new_edition,
    old_expiry_days,
  new_expiry_days,
    ip_address,
    created_at
FROM `license_usage_logs`
ORDER BY created_at DESC
LIMIT 50;

-- ================================================
-- CLEANUP QUERIES (Use with caution!)
-- ================================================

-- Delete all test licenses (uncomment to use)
-- DELETE FROM `licenses` WHERE license_key LIKE 'TEST-%';

-- Clear all usage logs (uncomment to use)
-- DELETE FROM `license_usage_logs`;

-- Drop tables (uncomment to use - WARNING: This deletes all data!)
-- DROP TABLE IF EXISTS `license_usage_logs`;
-- DROP TABLE IF EXISTS `licenses`;
