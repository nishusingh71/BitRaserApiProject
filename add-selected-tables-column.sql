-- ================================================================
-- Add SelectedTables Column to private_cloud_databases Table
-- For MySQL/TiDB/MariaDB
-- ================================================================

USE Cloud_Erase__App;

-- Add the selected_tables column
ALTER TABLE private_cloud_databases 
ADD COLUMN selected_tables JSON 
COMMENT 'JSON object storing which tables user wants in private cloud'
AFTER database_username;

-- Set default selection for existing users
UPDATE private_cloud_databases 
SET selected_tables = JSON_OBJECT(
    'AuditReports', true,
    'subuser', true,
    'Roles', true,
    'SubuserRoles', true,
    'machines', false,
    'sessions', false,
    'logs', false,
    'commands', false,
    'groups', false
)
WHERE selected_tables IS NULL;

-- Verify the column was added
SELECT 
    user_email,
    selected_tables,
    schema_initialized,
    is_active
FROM private_cloud_databases;

-- Check column structure
DESCRIBE private_cloud_databases;

PRINT '✅ Column added successfully!';
PRINT '✅ Default table selection set for existing users';
PRINT '';
PRINT 'Default Selection:';
PRINT '  ✓ AuditReports';
PRINT '  ✓ subuser';
PRINT '  ✓ Roles';
PRINT '  ✓ SubuserRoles';
PRINT '  ✗ machines (not selected)';
PRINT '  ✗ sessions (not selected)';
PRINT '  ✗ logs (not selected)';
PRINT '  ✗ commands (not selected)';
PRINT '  ✗ groups (not selected)';
