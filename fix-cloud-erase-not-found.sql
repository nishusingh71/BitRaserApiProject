-- Fix Cloud_Erase Database Not Found Error
-- Quick diagnostic and solution

-- ========================================
-- STEP 1: Check Available Databases
-- ========================================

-- Connect using user 4WScT7meioLLU3B.root
-- mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com -P 4000 -u 4WScT7meioLLU3B.root -p89ayiOJGY2055G0g --ssl-mode=REQUIRED

SHOW DATABASES;

-- Expected output - check if Cloud_Erase exists

-- ========================================
-- STEP 2: Check User Permissions
-- ========================================

-- Check what databases this user can access
SELECT DISTINCT TABLE_SCHEMA 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys');

-- Check user grants
SHOW GRANTS FOR '4WScT7meioLLU3B.root'@'%';

-- ========================================
-- SOLUTION A: Create Cloud_Erase Database
-- ========================================

CREATE DATABASE IF NOT EXISTS Cloud_Erase
CHARACTER SET utf8mb4
COLLATE utf8mb4_bin;

-- Verify creation
SHOW DATABASES LIKE 'Cloud_Erase';

-- Use the database
USE Cloud_Erase;

-- Verify it's empty (should be)
SHOW TABLES;

-- ========================================
-- SOLUTION B: Use Test Database (Temporary)
-- ========================================

-- If you can't create databases, use 'test' database temporarily
USE test;

-- Connection string would be:
-- Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=test;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;

-- ========================================
-- SOLUTION C: Check Other User Credentials
-- ========================================

-- Try with the other user that has Cloud_Erase access
-- User: 2tdeFNZMcsWKkDR.root
-- This user has access to Cloud_Erase (from ApplicationDbContextConnection)

-- mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com -P 4000 -u 2tdeFNZMcsWKkDR.root -p76wtaj1GZkg7Qhek --ssl-mode=REQUIRED

-- Check if Cloud_Erase exists for this user
SHOW DATABASES;

-- ========================================
-- RECOMMENDED FIX: Update Connection String
-- ========================================

-- Use the user that already has access to Cloud_Erase
-- Connection String:
-- Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;
-- Port=4000;
-- Database=Cloud_Erase;
-- User=2tdeFNZMcsWKkDR.root;    ← Use this user
-- Password=76wtaj1GZkg7Qhek;   ← Use this password
-- SslMode=Required;

-- ========================================
-- VERIFICATION QUERIES
-- ========================================

-- After connecting, verify database access
SELECT DATABASE();

-- Check if you can create tables
CREATE TABLE IF NOT EXISTS test_access (
    id INT PRIMARY KEY,
    test_value VARCHAR(50)
);

-- If successful, drop test table
DROP TABLE IF EXISTS test_access;

-- ========================================
-- NOTES:
-- ========================================

/*
Error Analysis:
- User: 4WScT7meioLLU3B.root
- Database: Cloud_Erase
- Status: ❌ NOT FOUND

Available Users in appsettings.json:
1. User: 2tdeFNZMcsWKkDR.root
   - Has access to: Cloud_Erase__App
   - Probably has access to: Cloud_Erase

2. User: 4WScT7meioLLU3B.root
   - Was configured for: Tech (which doesn't exist)
   - Doesn't have access to: Cloud_Erase

SOLUTION:
Change connection string to use user 2tdeFNZMcsWKkDR.root
OR
Create Cloud_Erase database for user 4WScT7meioLLU3B.root
*/

-- ========================================
-- GRANT ACCESS (if you have admin rights)
-- ========================================

-- If you have admin access, grant permissions
GRANT ALL PRIVILEGES ON Cloud_Erase.* TO '4WScT7meioLLU3B.root'@'%';
FLUSH PRIVILEGES;

-- Verify grants
SHOW GRANTS FOR '4WScT7meioLLU3B.root'@'%';
