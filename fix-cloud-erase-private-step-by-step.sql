-- Step-by-Step Fix for Cloud_Erase_Private Database Error
-- Error: Unknown database 'Cloud_Erase_Private'
-- User: 4WScT7meioLLU3B.root

-- ========================================
-- STEP 1: Connect to TiDB Cluster
-- ========================================

-- Connect using this user
-- mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com -P 4000 -u 4WScT7meioLLU3B.root -p89ayiOJGY2055G0g --ssl-mode=REQUIRED

-- ========================================
-- STEP 2: Check Available Databases
-- ========================================

SHOW DATABASES;

-- Expected output - check if Cloud_Erase_Private exists
-- If NOT in list, proceed to create it

-- ========================================
-- STEP 3: Check User Permissions
-- ========================================

-- Check what databases this user can access
SELECT DISTINCT TABLE_SCHEMA 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys');

-- Check user grants
SHOW GRANTS FOR CURRENT_USER();

-- ========================================
-- SOLUTION 1: Create Cloud_Erase_Private Database
-- ========================================

-- Create the database
CREATE DATABASE IF NOT EXISTS Cloud_Erase_Private
CHARACTER SET utf8mb4
COLLATE utf8mb4_bin;

-- Verify creation
SHOW DATABASES LIKE 'Cloud_Erase_Private';

-- Switch to the new database
USE Cloud_Erase_Private;

-- Verify you're in the right database
SELECT DATABASE();

-- ========================================
-- STEP 4: Create Required Tables
-- ========================================

-- Create audit_reports table
CREATE TABLE IF NOT EXISTS audit_reports (
    report_id INT PRIMARY KEY AUTO_INCREMENT,
  report_name VARCHAR(255) NOT NULL,
    client_email VARCHAR(255) NOT NULL,
    erasure_method VARCHAR(100),
    report_details JSON,
    report_date_time DATETIME DEFAULT CURRENT_TIMESTAMP,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_client_email (client_email),
    INDEX idx_report_date (report_date_time)
);

-- Create subuser table
CREATE TABLE IF NOT EXISTS subuser (
    subuser_id INT PRIMARY KEY AUTO_INCREMENT,
  subuser_email VARCHAR(255) NOT NULL UNIQUE,
    subuser_username VARCHAR(255) NOT NULL UNIQUE,
    subuser_password VARCHAR(255) NOT NULL,
    user_email VARCHAR(255) NOT NULL,
    name VARCHAR(255),
    phone VARCHAR(20),
    department VARCHAR(100),
    role VARCHAR(50) DEFAULT 'operator',
    is_active BOOLEAN DEFAULT TRUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_user_email (user_email),
    INDEX idx_subuser_email (subuser_email)
);

-- Verify tables created
SHOW TABLES;

-- Check table structures
DESCRIBE audit_reports;
DESCRIBE subuser;

-- ========================================
-- STEP 5: Test Database Access
-- ========================================

-- Test insert into audit_reports
INSERT INTO audit_reports (report_name, client_email, erasure_method)
VALUES ('Test Report', 'test@example.com', 'DoD 5220.22-M');

-- Verify insert
SELECT * FROM audit_reports;

-- Clean up test data
DELETE FROM audit_reports WHERE report_name = 'Test Report';

-- ========================================
-- SOLUTION 2: Use Existing Database Instead
-- ========================================

-- If you can't create Cloud_Erase_Private, check if Cloud_Erase exists
SHOW DATABASES LIKE 'Cloud_Erase';

-- If Cloud_Erase exists, use it
USE Cloud_Erase;

-- Update your connection string to use Cloud_Erase instead:
-- Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;
-- Port=4000;
-- Database=Cloud_Erase;  ← Change from Cloud_Erase_Private
-- User=4WScT7meioLLU3B.root;
-- Password=89ayiOJGY2055G0g;
-- SslMode=Required;

-- ========================================
-- SOLUTION 3: Use Different User
-- ========================================

-- Try with the other user that has access
-- User: 2tdeFNZMcsWKkDR.root
-- Password: 76wtaj1GZkg7Qhek

-- Connect with this user:
-- mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com -P 4000 -u 2tdeFNZMcsWKkDR.root -p76wtaj1GZkg7Qhek --ssl-mode=REQUIRED

-- Check databases for this user
SHOW DATABASES;

-- Create Cloud_Erase_Private with this user
CREATE DATABASE IF NOT EXISTS Cloud_Erase_Private
CHARACTER SET utf8mb4
COLLATE utf8mb4_bin;

-- Grant access to first user if needed
GRANT ALL PRIVILEGES ON Cloud_Erase_Private.* TO '4WScT7meioLLU3B.root'@'%';
FLUSH PRIVILEGES;

-- ========================================
-- VERIFICATION SCRIPT
-- ========================================

-- Run these commands to verify everything works
USE Cloud_Erase_Private;

-- Check current user
SELECT CURRENT_USER();

-- Check current database
SELECT DATABASE();

-- List tables
SHOW TABLES;

-- Check table count
SELECT 
    'audit_reports' as table_name,
    COUNT(*) as row_count
FROM audit_reports
UNION ALL
SELECT 
 'subuser' as table_name,
    COUNT(*) as row_count
FROM subuser;

-- ========================================
-- RECOMMENDED CONNECTION STRING
-- ========================================

/*
After fixing, use this connection string:

Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;
Port=4000;
Database=Cloud_Erase_Private;
User=4WScT7meioLLU3B.root;
Password=89ayiOJGY2055G0g;
SslMode=Required;

OR if Cloud_Erase_Private can't be created, use:

Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;
Port=4000;
Database=Cloud_Erase;
User=2tdeFNZMcsWKkDR.root;
Password=76wtaj1GZkg7Qhek;
SslMode=Required;
*/

-- ========================================
-- TROUBLESHOOTING
-- ========================================

-- If CREATE DATABASE fails with permission error:
-- Error: Access denied; you need the CREATE privilege

-- Solution: Use admin user or ask DBA to create database

-- If GRANT fails:
-- Solution: You need GRANT OPTION privilege

-- ========================================
-- NOTES
-- ========================================

/*
Issue: User 4WScT7meioLLU3B.root trying to access Cloud_Erase_Private
Status: Database doesn't exist OR user doesn't have access

Options:
1. Create Cloud_Erase_Private database (if you have permission)
2. Use Cloud_Erase database instead
3. Use user 2tdeFNZMcsWKkDR.root who has more access
4. Ask admin to create database and grant access

Recommended: Use option 2 or 3 for quickest fix
*/
