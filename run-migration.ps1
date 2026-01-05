# ====================================
# PRIVATE CLOUD DATA MIGRATION
# Quick Execution Script
# ====================================

Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  PRIVATE CLOUD DATA MIGRATION TOOL" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if SQL file exists
Write-Host "[1/3] Checking SQL script..." -ForegroundColor Yellow
if (Test-Path "migrate-main-to-private-complete.sql") {
    Write-Host "   ✅ SQL script found!" -ForegroundColor Green
} else {
    Write-Host "   ❌ SQL script NOT FOUND!" -ForegroundColor Red
    Write-Host "   Please ensure 'migrate-main-to-private-complete.sql' exists in current directory" -ForegroundColor Red
exit 1
}

# Step 2: Display what will be migrated
Write-Host "`n[2/3] Migration Plan:" -ForegroundColor Yellow
Write-Host "   📋 Tables to Create:" -ForegroundColor Gray
Write-Host "      - Permissions (NEW)" -ForegroundColor White
Write-Host "      - RolePermissions (NEW)" -ForegroundColor White
Write-Host "- UserRoles (NEW)" -ForegroundColor White
Write-Host ""
Write-Host "   📊 Data to Migrate:" -ForegroundColor Gray
Write-Host "      - Roles (5 roles)" -ForegroundColor White
Write-Host "      - Permissions (~32 permissions)" -ForegroundColor White
Write-Host "      - RolePermissions (~80 mappings)" -ForegroundColor White
Write-Host "      - User: devste@gmail.com" -ForegroundColor White
Write-Host "      - UserRoles for devste@gmail.com" -ForegroundColor White
Write-Host "      - Subusers (if any)" -ForegroundColor White
Write-Host "      - SubuserRoles (if any)" -ForegroundColor White

# Step 3: Instructions
Write-Host "`n[3/3] Execution Instructions:" -ForegroundColor Yellow
Write-Host "   1. Open MySQL Workbench or your MySQL client" -ForegroundColor White
Write-Host "   2. Connect to your TiDB Cloud database" -ForegroundColor White
Write-Host "   3. Select database: cloud_erase__private" -ForegroundColor White
Write-Host "   4. Run the script: migrate-main-to-private-complete.sql" -ForegroundColor White
Write-Host ""
Write-Host "   OR use MySQL command line:" -ForegroundColor White
Write-Host "   mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com \" -ForegroundColor Cyan
Write-Host "     -P 4000 \" -ForegroundColor Cyan
Write-Host "         -u 2tdeFNZMcsWKkDR.root \" -ForegroundColor Cyan
Write-Host "         -p \" -ForegroundColor Cyan
Write-Host "         cloud_erase__private < migrate-main-to-private-complete.sql" -ForegroundColor Cyan

Write-Host "`n═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  EXPECTED RESULTS AFTER MIGRATION" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Roles: 5" -ForegroundColor Green
Write-Host "✅ Permissions: 32" -ForegroundColor Green
Write-Host "✅ RolePermissions: ~80" -ForegroundColor Green
Write-Host "✅ users: 1 (devste@gmail.com)" -ForegroundColor Green
Write-Host "✅ UserRoles: 1+ (devste@gmail.com's roles)" -ForegroundColor Green
Write-Host "✅ subuser: 0+ (depends on existing data)" -ForegroundColor Green
Write-Host "✅ SubuserRoles: 0+ (depends on existing data)" -ForegroundColor Green

Write-Host "`n═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  AFTER MIGRATION - FIX THE ERRORS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "After successful migration, your current errors will be fixed:" -ForegroundColor Yellow
Write-Host ""
Write-Host "❌ BEFORE:" -ForegroundColor Red
Write-Host '   "currentRoles": []' -ForegroundColor Red
Write-Host '   "Table cloud_erase__private.RolePermissions doesn not exist"' -ForegroundColor Red
Write-Host ""
Write-Host "✅ AFTER:" -ForegroundColor Green
Write-Host '   "currentRoles": ["Admin"]  // or whatever role devste has' -ForegroundColor Green
Write-Host '   Table RolePermissions will exist with data' -ForegroundColor Green

Write-Host "`n═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  READY TO MIGRATE!" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Run the SQL script now? This is a manual step." -ForegroundColor Yellow
Write-Host "Press any key to open the SQL file in default editor..." -ForegroundColor White
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Open SQL file in default editor
Start-Process "migrate-main-to-private-complete.sql"

Write-Host "`n✅ SQL file opened. Please execute it in your MySQL client." -ForegroundColor Green
Write-Host "After execution, test your APIs again!" -ForegroundColor Cyan
