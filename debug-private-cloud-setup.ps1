# Debug Private Cloud Setup for devste@gmail.com

Write-Host "🔍 Private Cloud Setup Debugger" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

$userEmail = "devste@gmail.com"

# Step 1: Check SQL files exist
Write-Host "`n📋 Step 1: Checking SQL files..." -ForegroundColor Yellow

$sqlFiles = @(
    "check_private_cloud_access.sql",
    "enable_private_cloud_user.sql",
  "create_private_cloud_table.sql"
)

foreach ($file in $sqlFiles) {
    if (Test-Path $file) {
  Write-Host "  ✅ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing: $file" -ForegroundColor Red
    }
}

# Step 2: Show SQL commands to run manually
Write-Host "`n📋 Step 2: SQL Commands to Run Manually" -ForegroundColor Yellow
Write-Host "=======================================" -ForegroundColor Cyan

Write-Host "`n1️⃣ Check if user exists:" -ForegroundColor White
Write-Host @"
SELECT user_id, user_email, is_private_cloud, status 
FROM users 
WHERE user_email = '$userEmail';
"@ -ForegroundColor Gray

Write-Host "`n2️⃣ Enable private cloud (if not enabled):" -ForegroundColor White
Write-Host @"
UPDATE users 
SET is_private_cloud = TRUE 
WHERE user_email = '$userEmail';
"@ -ForegroundColor Gray

Write-Host "`n3️⃣ Verify update:" -ForegroundColor White
Write-Host @"
SELECT user_email, is_private_cloud 
FROM users 
WHERE user_email = '$userEmail';
"@ -ForegroundColor Gray

Write-Host "`n4️⃣ Check existing private cloud config:" -ForegroundColor White
Write-Host @"
SELECT config_id, user_email, database_type, server_host, is_active, test_status 
FROM private_cloud_databases 
WHERE user_email = '$userEmail';
"@ -ForegroundColor Gray

Write-Host "`n5️⃣ Delete existing config (if needed):" -ForegroundColor White
Write-Host @"
DELETE FROM private_cloud_databases 
WHERE user_email = '$userEmail';
"@ -ForegroundColor Gray

# Step 3: Check appsettings.json
Write-Host "`n📋 Step 3: Checking appsettings.json..." -ForegroundColor Yellow

if (Test-Path "BitRaserApiProject/appsettings.json") {
    Write-Host "  ✅ Found appsettings.json" -ForegroundColor Green
    
    $appSettings = Get-Content "BitRaserApiProject/appsettings.json" | ConvertFrom-Json
    
    if ($appSettings.ConnectionStrings.DefaultConnection) {
        Write-Host "  ✅ Main DB connection string exists" -ForegroundColor Green
        $connStr = $appSettings.ConnectionStrings.DefaultConnection
        
        # Mask password
    $maskedConnStr = $connStr -replace '(password|pwd)=([^;]*)', '$1=***'
        Write-Host "     Connection: $maskedConnStr" -ForegroundColor Gray
    } else {
        Write-Host "  ❌ Main DB connection string missing" -ForegroundColor Red
    }
} else {
    Write-Host "  ❌ appsettings.json not found" -ForegroundColor Red
}

# Step 4: Test connection string format
Write-Host "`n📋 Step 4: Test Your Connection String Format" -ForegroundColor Yellow

$sampleUri = "mysql://4WScT7meioLLU3B.root:89ayiOJGY2055G0g@gateway01.ap-southeast-1.prod.aws.tidbcloud.com:4000/Tech"

Write-Host "`nValid MySQL URI format:" -ForegroundColor White
Write-Host "  mysql://username:password@host:port/database" -ForegroundColor Gray

Write-Host "`nExample (TiDB):" -ForegroundColor White
Write-Host "  $sampleUri" -ForegroundColor Gray

Write-Host "`nYour connection string should match this pattern:" -ForegroundColor White
if ($sampleUri -match "^mysql://([^:]+):([^@]+)@([^:]+):(\d+)/(.+)$") {
    Write-Host "  ✅ Username: $($Matches[1])" -ForegroundColor Green
    Write-Host "  ✅ Password: ***" -ForegroundColor Green
    Write-Host "  ✅ Host: $($Matches[3])" -ForegroundColor Green
    Write-Host "  ✅ Port: $($Matches[4])" -ForegroundColor Green
    Write-Host "  ✅ Database: $($Matches[5])" -ForegroundColor Green
}

# Step 5: Show API test command
Write-Host "`n📋 Step 5: API Test Command" -ForegroundColor Yellow

Write-Host @"

# First, login to get token:
curl -X POST http://localhost:5000/api/RoleBasedAuth/login \
  -H "Content-Type: application/json" \
  -d '{\"email\":\"$userEmail\",\"password\":\"YOUR_PASSWORD\"}'

# Then, use the token to setup:
curl -X POST http://localhost:5000/api/PrivateCloud/setup-simple \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{\"connectionString\":\"YOUR_CONNECTION_STRING_HERE\",\"notes\":\"TiDB Test\"}'
"@ -ForegroundColor Gray

# Step 6: Common issues
Write-Host "`n📋 Step 6: Common Issues & Fixes" -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Cyan

Write-Host "`n❌ Issue 1: User not found" -ForegroundColor Red
Write-Host "   Fix: Create user in database first" -ForegroundColor White

Write-Host "`n❌ Issue 2: is_private_cloud = FALSE" -ForegroundColor Red
Write-Host "   Fix: Run UPDATE query from Step 2" -ForegroundColor White

Write-Host "`n❌ Issue 3: Duplicate entry" -ForegroundColor Red
Write-Host "   Fix: Run DELETE query from Step 5" -ForegroundColor White

Write-Host "`n❌ Issue 4: Invalid connection string format" -ForegroundColor Red
Write-Host "   Fix: Use format from Step 4" -ForegroundColor White

Write-Host "`n❌ Issue 5: Connection test failed" -ForegroundColor Red
Write-Host "   Fix: Verify credentials with direct MySQL connection" -ForegroundColor White

# Step 7: Next steps
Write-Host "`n📋 Step 7: Next Steps" -ForegroundColor Yellow
Write-Host "====================" -ForegroundColor Cyan

Write-Host @"

1. Run the SQL queries manually in your database client
2. Verify user exists and is_private_cloud = TRUE
3. Delete any existing config if present
4. Start the application with: dotnet run
5. Watch console logs for detailed error messages
6. Try the setup API call again

"@ -ForegroundColor White

Write-Host "🔍 For detailed logs, run:" -ForegroundColor Cyan
Write-Host " dotnet run | Tee-Object -FilePath debug-log.txt" -ForegroundColor Gray

Write-Host "`n✅ Debug script complete!" -ForegroundColor Green
