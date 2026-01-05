# Fix Cloud_Erase Database Not Found Error
# Tests both users and recommends solution

$ErrorActionPreference = "Continue"

Write-Host "🔍 Diagnosing Cloud_Erase Database Access Issue" -ForegroundColor Cyan
Write-Host ""

# Test configurations
$user1 = @{
    Host = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
    Port = 4000
    Database = "Cloud_Erase"
    User = "4WScT7meioLLU3B.root"
 Password = "89ayiOJGY2055G0g"
    Name = "User 1 (Current - Failing)"
}

$user2 = @{
    Host = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
    Port = 4000
    Database = "Cloud_Erase"
    User = "2tdeFNZMcsWKkDR.root"
    Password = "76wtaj1GZkg7Qhek"
    Name = "User 2 (From ApplicationDbContextConnection)"
}

Write-Host "===== TESTING USER 1 =====" -ForegroundColor Yellow
Write-Host "User: $($user1.User)" -ForegroundColor White
Write-Host "Database: $($user1.Database)" -ForegroundColor White
Write-Host ""

# Test User 1
$connStr1 = "Server=$($user1.Host);Port=$($user1.Port);Database=$($user1.Database);User=$($user1.User);Password=$($user1.Password);SslMode=Required;"

Write-Host "Testing connection..." -ForegroundColor Gray
try {
  # Try to connect using mysql command if available
    $testCommand = "mysql -h $($user1.Host) -P $($user1.Port) -u $($user1.User) -p$($user1.Password) -D $($user1.Database) --ssl-mode=REQUIRED -e 'SELECT DATABASE();' 2>&1"
    
    $result = Invoke-Expression $testCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ User 1 CAN access Cloud_Erase database" -ForegroundColor Green
        $user1Access = $true
    } else {
     Write-Host "❌ User 1 CANNOT access Cloud_Erase database" -ForegroundColor Red
        Write-Host "   Error: $result" -ForegroundColor Red
     $user1Access = $false
    }
} catch {
    Write-Host "❌ User 1 connection failed" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    $user1Access = $false
}

Write-Host ""
Write-Host "===== TESTING USER 2 =====" -ForegroundColor Yellow
Write-Host "User: $($user2.User)" -ForegroundColor White
Write-Host "Database: $($user2.Database)" -ForegroundColor White
Write-Host ""

# Test User 2
$connStr2 = "Server=$($user2.Host);Port=$($user2.Port);Database=$($user2.Database);User=$($user2.User);Password=$($user2.Password);SslMode=Required;"

Write-Host "Testing connection..." -ForegroundColor Gray
try {
    $testCommand = "mysql -h $($user2.Host) -P $($user2.Port) -u $($user2.User) -p$($user2.Password) -D $($user2.Database) --ssl-mode=REQUIRED -e 'SELECT DATABASE();' 2>&1"
 
    $result = Invoke-Expression $testCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ User 2 CAN access Cloud_Erase database" -ForegroundColor Green
        $user2Access = $true
    } else {
      Write-Host "❌ User 2 CANNOT access Cloud_Erase database" -ForegroundColor Red
    Write-Host "   Error: $result" -ForegroundColor Red
        $user2Access = $false
    }
} catch {
    Write-Host "❌ User 2 connection failed" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    $user2Access = $false
}

Write-Host ""
Write-Host "===== ANALYSIS =====" -ForegroundColor Cyan
Write-Host ""

if ($user1Access) {
    Write-Host "✅ User 1 works - No changes needed" -ForegroundColor Green
    Write-Host ""
    Write-Host "Use this connection string:" -ForegroundColor Yellow
    Write-Host $connStr1 -ForegroundColor Green
} elseif ($user2Access) {
    Write-Host "⚠️  User 1 doesn't work, but User 2 does!" -ForegroundColor Yellow
 Write-Host ""
    Write-Host "💡 SOLUTION: Use User 2 credentials" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Updated Connection String:" -ForegroundColor Green
    Write-Host $connStr2 -ForegroundColor White
    Write-Host ""
    Write-Host "API Request Body:" -ForegroundColor Yellow
    Write-Host @"
{
  "connectionString": "$connStr2",
  "databaseType": "mysql"
}
"@ -ForegroundColor White
} else {
    Write-Host "❌ Neither user can access Cloud_Erase database" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possible causes:" -ForegroundColor Yellow
 Write-Host "1. Database 'Cloud_Erase' doesn't exist" -ForegroundColor White
    Write-Host "2. Both users don't have access" -ForegroundColor White
    Write-Host "3. Network/SSL issues" -ForegroundColor White
    Write-Host ""
    Write-Host "Solutions:" -ForegroundColor Yellow
  Write-Host "1. Create Cloud_Erase database" -ForegroundColor White
    Write-Host "2. Use 'test' database instead" -ForegroundColor White
    Write-Host "3. Grant access to users" -ForegroundColor White
}

Write-Host ""
Write-Host "===== QUICK FIXES =====" -ForegroundColor Cyan
Write-Host ""

Write-Host "Option 1: Use User 2 (Recommended)" -ForegroundColor Yellow
Write-Host "Connection String:" -ForegroundColor Gray
Write-Host $connStr2 -ForegroundColor Green
Write-Host ""

Write-Host "Option 2: Use test database" -ForegroundColor Yellow
$testConnStr = "Server=$($user1.Host);Port=$($user1.Port);Database=test;User=$($user1.User);Password=$($user1.Password);SslMode=Required;"
Write-Host "Connection String:" -ForegroundColor Gray
Write-Host $testConnStr -ForegroundColor Green
Write-Host ""

Write-Host "Option 3: Create Cloud_Erase database" -ForegroundColor Yellow
Write-Host "SQL Command:" -ForegroundColor Gray
Write-Host "CREATE DATABASE IF NOT EXISTS Cloud_Erase;" -ForegroundColor Green
Write-Host ""

Write-Host "===== NEXT STEPS =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Try Option 1 (Use User 2)" -ForegroundColor White
Write-Host "2. If that fails, try Option 2 (Use test database)" -ForegroundColor White
Write-Host "3. If you need Cloud_Erase, create it using Option 3" -ForegroundColor White
Write-Host ""
