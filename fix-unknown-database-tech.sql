-- Fix Unknown Database 'Tech' Error
-- Quick diagnostic and fix SQL script

-- ========================================
-- STEP 1: Check Available Databases
-- ========================================

SHOW DATABASES;

-- Expected output:
-- +--------------------+
-- | Database   |
-- +--------------------+
-- | information_schema |
-- | mysql         |
-- | test             |
-- | Cloud_Erase        |
-- | Cloud_Erase__App|
-- +--------------------+

-- ========================================
-- STEP 2: Check if 'Tech' database exists
-- ========================================

SELECT SCHEMA_NAME 
FROM INFORMATION_SCHEMA.SCHEMATA 
WHERE SCHEMA_NAME = 'Tech';

-- If empty result, database doesn't exist

-- ========================================
-- SOLUTION A: Create 'Tech' Database
-- ========================================

CREATE DATABASE IF NOT EXISTS Tech 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_bin;

-- Verify creation
SHOW DATABASES LIKE 'Tech';

-- Switch to Tech database
USE Tech;

-- Check tables (should be empty initially)
SHOW TABLES;

-- ========================================
-- SOLUTION B: Use Existing Database
-- ========================================

-- Option B1: Use Cloud_Erase
USE Cloud_Erase;
SHOW TABLES;

-- Option B2: Use Cloud_Erase__App
USE Cloud_Erase__App;
SHOW TABLES;

-- Option B3: Use test database
USE test;
SHOW TABLES;

-- ========================================
-- STEP 3: Verify Database Access
-- ========================================

-- Check your user permissions
SHOW GRANTS FOR '4WScT7meioLLU3B.root'@'%';

-- ========================================
-- RECOMMENDED: Use Cloud_Erase
-- ========================================

-- Switch to Cloud_Erase
USE Cloud_Erase;

-- Check existing tables
SHOW TABLES;

-- If you want to use this database, update your connection string to:
-- Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;
-- Port=4000;
-- Database=Cloud_Erase;
-- User=4WScT7meioLLU3B.root;
-- Password=89ayiOJGY2055G0g;
-- SslMode=Required;

-- ========================================
-- NOTES:
-- ========================================

/*
Error was:
"Unknown database 'Tech'"

This means:
1. Database 'Tech' doesn't exist
2. OR user doesn't have access to it

Solutions:
1. Create 'Tech' database (above)
2. Use existing database like 'Cloud_Erase'
3. Check user permissions

Recommended:
Use 'Cloud_Erase' database - it already exists and is configured
*/
