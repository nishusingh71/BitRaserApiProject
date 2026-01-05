# Fix Cloud_Erase_Private Database Error - Step by Step
# Diagnostic and fix script

$ErrorActionPreference = "Continue"

Write-Host "🔍 ===== CLOUD_ERASE_PRIVATE DATABASE FIX =====" -ForegroundColor Cyan
Write-Host ""

# Configuration
$user1 = @{
    Host = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
    Port = 4000
    User = "4WScT7meioLLU3B.root"
    Password = "89ayiOJGY2055G0g"
    Name = "User 1 (Current)"
}

$user2 = @{
    Host = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
    Port = 4000
    User = "2tdeFNZMcsWKkDR.root"
    Password = "76wtaj1GZkg7Qhek"
    Name = "User 2 (Alternative)"
}

Write-Host "Error Details:" -ForegroundColor Yellow
Write-Host "❌ Unknown database 'Cloud_Erase_Private'" -ForegroundColor Red
Write-Host "   User: $($user1.User)" -ForegroundColor Gray
Write-Host "   Host: $($user1.Host):$($user1.Port)" -ForegroundColor Gray
Write-Host ""

Write-Host "===== STEP 1: Check Available Databases =====" -ForegroundColor Cyan
Write-Host ""

Write-Host "Testing User 1..." -ForegroundColor Yellow
$cmd1 = "mysql -h $($user1.Host) -P $($user1.Port) -u $($user1.User) -p$($user1.Password) --ssl-mode=REQUIRED -e `"SHOW DATABASES;`" 2>&1"

try {
    Write-Host "Executing: SHOW DATABASES" -ForegroundColor Gray
    $result1 = Invoke-Expression $cmd1
    
    if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Connection successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Available databases:" -ForegroundColor Cyan
        Write-Host $result1 -ForegroundColor White
        
        # Check if Cloud_Erase_Private exists
        if ($result1 -match "Cloud_Erase_Private") {
      Write-Host ""
    Write-Host "✅ Cloud_Erase_Private EXISTS!" -ForegroundColor Green
       Write-Host "   Database is available but connection string might be wrong" -ForegroundColor Yellow
      } else {
       Write-Host ""
            Write-Host "❌ Cloud_Erase_Private DOES NOT EXIST" -ForegroundColor Red
         $needsCreate = $true
        }
     
        # Check if Cloud_Erase exists
      if ($result1 -match "Cloud_Erase") {
   Write-Host "✅ Cloud_Erase EXISTS - Can use this instead!" -ForegroundColor Green
         $canUseCloudErase = $true
        }
    } else {
        Write-Host "❌ Connection failed" -ForegroundColor Red
        Write-Host $result1 -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "===== STEP 2: Recommended Solutions =====" -ForegroundColor Cyan
Write-Host ""

if ($canUseCloudErase) {
    Write-Host "💡 SOLUTION 1: Use Cloud_Erase Database (Recommended)" -ForegroundColor Yellow
    Write-Host ""
    $cloudEraseConn = "Server=$($user1.Host);Port=$($user1.Port);Database=Cloud_Erase;User=$($user1.User);Password=$($user1.Password);SslMode=Required;"
    Write-Host "Connection String:" -ForegroundColor Cyan
    Write-Host $cloudEraseConn -ForegroundColor Green
    Write-Host ""
  Write-Host "API Request Body:" -ForegroundColor Cyan
    Write-Host @"
{
  "connectionString": "$cloudEraseConn",
  "databaseType": "mysql"
}
"@ -ForegroundColor White
    Write-Host ""
}

if ($needsCreate) {
    Write-Host "💡 SOLUTION 2: Create Cloud_Erase_Private Database" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "SQL Command:" -ForegroundColor Cyan
    Write-Host @"
mysql -h $($user1.Host) -P $($user1.Port) -u $($user1.User) -p$($user1.Password) --ssl-mode=REQUIRED -e "CREATE DATABASE IF NOT EXISTS Cloud_Erase_Private CHARACTER SET utf8mb4 COLLATE utf8mb4_bin;"
"@ -ForegroundColor Green
    Write-Host ""
}

Write-Host "💡 SOLUTION 3: Use Alternative User (User 2)" -ForegroundColor Yellow
Write-Host ""
$user2Conn = "Server=$($user2.Host);Port=$($user2.Port);Database=Cloud_Erase;User=$($user2.User);Password=$($user2.Password);SslMode=Required;"
Write-Host "Connection String:" -ForegroundColor Cyan
Write-Host $user2Conn -ForegroundColor Green
Write-Host ""

Write-Host "===== STEP 3: Test Connection =====" -ForegroundColor Cyan
Write-Host ""

Write-Host "Test Cloud_Erase database:" -ForegroundColor Yellow
$testCmd = "mysql -h $($user1.Host) -P $($user1.Port) -u $($user1.User) -p$($user1.Password) -D Cloud_Erase --ssl-mode=REQUIRED -e `"SELECT DATABASE();`" 2>&1"

try {
    $testResult = Invoke-Expression $testCmd
    
    if ($LASTEXITCODE -eq 0) {
     Write-Host "✅ Can connect to Cloud_Erase!" -ForegroundColor Green
    Write-Host $testResult -ForegroundColor White
    Write-Host ""
   Write-Host "🎯 RECOMMENDED: Use Cloud_Erase database" -ForegroundColor Green
    } else {
        Write-Host "❌ Cannot connect to Cloud_Erase" -ForegroundColor Red
    Write-Host $testResult -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Connection test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "===== STEP 4: Create Database (If Needed) =====" -ForegroundColor Cyan
Write-Host ""

if ($needsCreate) {
    Write-Host "Attempting to create Cloud_Erase_Private..." -ForegroundColor Yellow
    
    $createCmd = "mysql -h $($user1.Host) -P $($user1.Port) -u $($user1.User) -p$($user1.Password) --ssl-mode=REQUIRED -e `"CREATE DATABASE IF NOT EXISTS Cloud_Erase_Private CHARACTER SET utf8mb4 COLLATE utf8mb4_bin;`" 2>&1"
    
    try {
        $createResult = Invoke-Expression $createCmd
        
        if ($LASTEXITCODE -eq 0) {
     Write-Host "✅ Database created successfully!" -ForegroundColor Green
            
       # Verify
       $verifyCmd = "mysql -h $($user1.Host) -P $($user1.Port) -u $($user1.User) -p$($user1.Password) --ssl-mode=REQUIRED -e `"SHOW DATABASES LIKE 'Cloud_Erase_Private';`" 2>&1"
$verifyResult = Invoke-Expression $verifyCmd
    
       if ($verifyResult -match "Cloud_Erase_Private") {
   Write-Host "✅ Verified: Cloud_Erase_Private exists!" -ForegroundColor Green
         
     $privateConn = "Server=$($user1.Host);Port=$($user1.Port);Database=Cloud_Erase_Private;User=$($user1.User);Password=$($user1.Password);SslMode=Required;"
          Write-Host ""
    Write-Host "Use this connection string:" -ForegroundColor Cyan
  Write-Host $privateConn -ForegroundColor Green
         }
        } else {
Write-Host "❌ Failed to create database" -ForegroundColor Red
       Write-Host $createResult -ForegroundColor Red
   Write-Host ""
  Write-Host "Possible reasons:" -ForegroundColor Yellow
            Write-Host "- User doesn't have CREATE DATABASE permission" -ForegroundColor White
      Write-Host "- Database name already exists but user can't see it" -ForegroundColor White
            Write-Host ""
            Write-Host "💡 Use Solution 1 or 3 instead" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ Error creating database: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "===== SUMMARY =====" -ForegroundColor Cyan
Write-Host ""

Write-Host "Problem: Database 'Cloud_Erase_Private' not found" -ForegroundColor Yellow
Write-Host ""
Write-Host "Quick Fixes (in order of preference):" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. ✅ Use Cloud_Erase database" -ForegroundColor Green
Write-Host "   Connection: ...Database=Cloud_Erase;..." -ForegroundColor Gray
Write-Host ""
Write-Host "2. ✅ Use User 2 with Cloud_Erase" -ForegroundColor Green
Write-Host "   User: 2tdeFNZMcsWKkDR.root" -ForegroundColor Gray
Write-Host ""
Write-Host "3. ⚠️  Create Cloud_Erase_Private (if you have permission)" -ForegroundColor Yellow
Write-Host "   SQL: CREATE DATABASE Cloud_Erase_Private;" -ForegroundColor Gray
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Choose a solution above" -ForegroundColor White
Write-Host "2. Update connection string in API request" -ForegroundColor White
Write-Host "3. Retry setup" -ForegroundColor White
Write-Host ""
