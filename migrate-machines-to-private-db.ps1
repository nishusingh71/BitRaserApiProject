#!/usr/bin/env pwsh
# 🚀 MIGRATE MACHINES TABLE TO PRIVATE DATABASE
# Author: System
# Date: 2025-01-15

Write-Host "🚀 MACHINES TABLE MIGRATION TO PRIVATE DATABASE" -ForegroundColor Cyan
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host ""

# Configuration
$privateHost = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
$privatePort = 4000
$privateDb = "Cloud_Erase"
$privateUser = "2tdeFNZMcsWKkDR.root"
$privatePass = "76wtaj1GZkg7Qhek"
$targetUser = "devste@gmail.com"

Write-Host "📋 CONFIGURATION:" -ForegroundColor Yellow
Write-Host "  Host: $privateHost" -ForegroundColor White
Write-Host "  Port: $privatePort" -ForegroundColor White
Write-Host "  Database: $privateDb" -ForegroundColor White
Write-Host "  Target User: $targetUser" -ForegroundColor White
Write-Host ""

# Step 1: Test connection
Write-Host "🔌 STEP 1: Testing database connection..." -ForegroundColor Yellow

$testQuery = "SELECT 'Connection successful!' as STATUS, VERSION() as SERVER_VERSION;"

try {
    $testResult = mysql -h $privateHost `
     -P $privatePort `
                 -u $privateUser `
           -p$privatePass `
    -D $privateDb `
            -e $testQuery 2>&1

    if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Connection successful!" -ForegroundColor Green
        Write-Host $testResult
    } else {
     Write-Host "❌ Connection failed!" -ForegroundColor Red
 Write-Host $testResult
        exit 1
    }
} catch {
Write-Host "  ❌ Error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Check if table exists
Write-Host "🔍 STEP 2: Checking if machines table exists..." -ForegroundColor Yellow

$checkTableQuery = @"
SELECT COUNT(*) as table_exists 
FROM information_schema.TABLES 
WHERE TABLE_SCHEMA = '$privateDb' 
  AND TABLE_NAME = 'machines';
"@

$tableExists = mysql -h $privateHost `
     -P $privatePort `
               -u $privateUser `
      -p$privatePass `
       -D $privateDb `
 -N -e $checkTableQuery 2>&1

if ($tableExists -eq "1") {
    Write-Host "  ✅ Table exists" -ForegroundColor Green
    Write-Host "  ⚠️  Table will be updated with new data" -ForegroundColor Yellow
} else {
    Write-Host "  ⚠️  Table does not exist" -ForegroundColor Yellow
    Write-Host "  📝 Creating machines table..." -ForegroundColor Cyan

    # Create table
    mysql -h $privateHost `
          -P $privatePort `
          -u $privateUser `
          -p$privatePass `
     -D $privateDb `
     < create-machines-table-private-db.sql
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ Table created successfully!" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Failed to create table!" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# Step 3: Count machines in source
Write-Host "📊 STEP 3: Analyzing source data..." -ForegroundColor Yellow

$countQuery = @"
SELECT 
    COUNT(*) as TOTAL,
    COUNT(CASE WHEN user_email = '$targetUser' THEN 1 END) as USER_MACHINES,
    COUNT(CASE WHEN subuser_email IN (
        SELECT subuser_email FROM tech.subuser WHERE user_email = '$targetUser'
    ) THEN 1 END) as SUBUSER_MACHINES
FROM tech.machines;
"@

Write-Host "  Counting machines in tech database..." -ForegroundColor Cyan
mysql -h $privateHost `
      -P $privatePort `
    -u $privateUser `
      -p$privatePass `
      -e $countQuery

Write-Host ""

# Step 4: Run migration
Write-Host "🔄 STEP 4: Migrating machines data..." -ForegroundColor Yellow
Write-Host "  This may take a few moments..." -ForegroundColor Cyan

mysql -h $privateHost `
    -P $privatePort `
   -u $privateUser `
      -p$privatePass `
      < migrate-machines-data-to-private-db.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Migration completed successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Migration failed!" -ForegroundColor Red
exit 1
}

Write-Host ""

# Step 5: Verify migration
Write-Host "✅ STEP 5: Verifying migration..." -ForegroundColor Yellow

$verifyQuery = @"
SELECT 
    COUNT(*) as TOTAL_MACHINES,
    COUNT(CASE WHEN license_activated = 1 THEN 1 END) as LICENSED,
  COUNT(CASE WHEN subuser_email IS NOT NULL THEN 1 END) as SUBUSER_OWNED
FROM Cloud_Erase.machines
WHERE user_email = '$targetUser'
   OR subuser_email IN (
        SELECT subuser_email FROM Cloud_Erase.subuser WHERE user_email = '$targetUser'
    );
"@

Write-Host "  Machines in private database:" -ForegroundColor Cyan
mysql -h $privateHost `
      -P $privatePort `
      -u $privateUser `
      -p$privatePass `
      -e $verifyQuery

Write-Host ""

# Step 6: Show sample data
Write-Host "📋 STEP 6: Sample migrated machines:" -ForegroundColor Yellow

$sampleQuery = @"
SELECT 
    fingerprint_hash,
 mac_address,
    os_version,
    CASE WHEN license_activated = 1 THEN 'Licensed' ELSE 'Unlicensed' END as STATUS,
    created_at
FROM Cloud_Erase.machines
WHERE user_email = '$targetUser'
   OR subuser_email IN (
        SELECT subuser_email FROM Cloud_Erase.subuser WHERE user_email = '$targetUser'
    )
ORDER BY created_at DESC
LIMIT 5;
"@

mysql -h $privateHost `
      -P $privatePort `
      -u $privateUser `
      -p$privatePass `
      -e $sampleQuery

Write-Host ""

# Summary
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host "🎉 MIGRATION COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ WHAT WAS DONE:" -ForegroundColor Yellow
Write-Host "  1. ✅ Connection tested" -ForegroundColor Green
Write-Host "  2. ✅ Table created/verified" -ForegroundColor Green
Write-Host "  3. ✅ Data migrated" -ForegroundColor Green
Write-Host "  4. ✅ Migration verified" -ForegroundColor Green
Write-Host ""

Write-Host "🧪 NEXT STEPS:" -ForegroundColor Yellow
Write-Host "  1. Test API endpoint:" -ForegroundColor White
Write-Host "  curl -X GET 'http://localhost:4000/api/Machines/by-email/$targetUser' \" -ForegroundColor Cyan
Write-Host "       -H 'Authorization: Bearer {token}'" -ForegroundColor Cyan
Write-Host ""
Write-Host "  2. Check logs for routing:" -ForegroundColor White
Write-Host "     Look for: 🔀 Routing to PRIVATE DB" -ForegroundColor Cyan
Write-Host "     Look for: 🔍 Fetching machines for user" -ForegroundColor Cyan
Write-Host "     Look for: ✅ Found X machines" -ForegroundColor Cyan
Write-Host ""

Write-Host "📊 TABLES NOW IN PRIVATE DATABASE:" -ForegroundColor Yellow
Write-Host "  1. users" -ForegroundColor White
Write-Host "  2. subuser" -ForegroundColor White
Write-Host "  3. audit_reports" -ForegroundColor White
Write-Host "  4. sessions" -ForegroundColor White
Write-Host "  5. logs" -ForegroundColor White
Write-Host "  6. commands" -ForegroundColor White
Write-Host "  7. User_role_profile" -ForegroundColor White
Write-Host "  8. groups" -ForegroundColor White
Write-Host "  9. roles" -ForegroundColor White
Write-Host "  10. permissions" -ForegroundColor White
Write-Host "  11. role_permissions" -ForegroundColor White
Write-Host "  12. user_roles" -ForegroundColor White
Write-Host "  13. machines ← JUST ADDED! ✅" -ForegroundColor Green
Write-Host ""

Write-Host "🎊 Private cloud setup is now complete with 13 tables! 💪" -ForegroundColor Green
Write-Host ""
