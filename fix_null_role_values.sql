-- Fix NULL Role values in subuser table

-- Update all NULL Role values to default 'subuser'
UPDATE subuser 
SET Role = 'subuser' 
WHERE Role IS NULL OR Role = '';

-- Verify the update
SELECT 
    COUNT(*) as total_subusers,
    COUNT(Role) as subusers_with_role,
    COUNT(*) - COUNT(Role) as subusers_without_role
FROM subuser;

-- Show role distribution
SELECT 
COALESCE(Role, 'NULL') as role_name,
    COUNT(*) as count
FROM subuser
GROUP BY Role
ORDER BY count DESC;
