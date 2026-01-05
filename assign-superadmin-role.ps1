# ✅ ASSIGN SUPERADMIN ROLE - POWERSHELL SCRIPT
# Quick execution script for assigning SuperAdmin role

Write-Host "🔐 ASSIGNING SUPERADMIN ROLE TO USER" -ForegroundColor Cyan
Write-Host "=" * 60

$userEmail = "devste@gmail.com"
$mainDbName = "cloud_erase"

Write-Host "`n📧 Target User: $userEmail" -ForegroundColor Yellow
Write-Host "🗄️ Database: $mainDbName" -ForegroundColor Yellow

# MySQL connection details
$mysqlPath = "mysql"  # Update if needed
$mysqlUser = "root"
$mysqlPassword = "root"  # Update with your password

Write-Host "`n⚙️ Executing SQL script..." -ForegroundColor Cyan

# Execute SQL script
& $mysqlPath -u $mysqlUser -p$mysqlPassword -e @"
USE $mainDbName;

-- Remove existing roles
DELETE FROM UserRoles
WHERE UserId = (SELECT user_id FROM users WHERE user_email = '$userEmail');

-- Assign SuperAdmin role
INSERT INTO UserRoles (UserId, RoleId, AssignedByEmail, AssignedAt)
SELECT 
    user_id,
    1,  -- SuperAdmin
 'system',
    NOW()
FROM users
WHERE user_email = '$userEmail';

-- Verify
SELECT 
    u.user_email,
    r.RoleName,
    r.HierarchyLevel,
    ur.AssignedAt
FROM users u
JOIN UserRoles ur ON u.user_id = ur.UserId
JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.user_email = '$userEmail';
"@

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ SUCCESS! SuperAdmin role assigned" -ForegroundColor Green
    Write-Host "User $userEmail now has full system access" -ForegroundColor Green
} else {
    Write-Host "`n❌ ERROR! Failed to assign role" -ForegroundColor Red
    Write-Host "Check MySQL connection and credentials" -ForegroundColor Red
}

Write-Host "`n" + ("=" * 60)
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
