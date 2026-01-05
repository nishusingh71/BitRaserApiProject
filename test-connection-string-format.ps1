# 🔧 Connection String Validator & Fixer

## Test Your TiDB Connection String

$connectionString = "mysql://4WScT7meioLLU3B.root:89ayiOJGY2055G0g@gateway01.ap-southeast-1.prod.aws.tidbcloud.com:4000/Tech"

Write-Host "🔍 Validating TiDB Connection String..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Validate format
Write-Host "`n1️⃣ Checking format..." -ForegroundColor Yellow

if ($connectionString -match "^mysql://([^:]+):([^@]+)@([^:]+):(\d+)/(.+?)(\?.*)?$") {
    Write-Host "   ✅ Valid MySQL URI format" -ForegroundColor Green
    
$username = $Matches[1]
    $password = $Matches[2]
    $host = $Matches[3]
    $port = $Matches[4]
    $database = $Matches[5]
    
    Write-Host "`n📋 Parsed Components:" -ForegroundColor White
    Write-Host "   Username: $username" -ForegroundColor Gray
    Write-Host "   Password: $($password.Substring(0, 3))***" -ForegroundColor Gray
    Write-Host "   Host: $host" -ForegroundColor Gray
    Write-Host "   Port: $port" -ForegroundColor Gray
    Write-Host "   Database: $database" -ForegroundColor Gray

    # Step 2: Build standard connection string
    Write-Host "`n2️⃣ Building standard connection string..." -ForegroundColor Yellow
    
    $standardConnStr = "Server=$host;Port=$port;Database=$database;User=$username;Password=$password;AllowUserVariables=true;SslMode=Required;"
    
    Write-Host "   ✅ Standard format:" -ForegroundColor Green
    Write-Host "   $($standardConnStr -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor Gray
  
    # Step 3: Test direct MySQL connection
    Write-Host "`n3️⃣ Testing MySQL connection..." -ForegroundColor Yellow
    Write-Host "   Run this command to test:" -ForegroundColor White
    Write-Host @"
   
   mysql -h $host ``
         -P $port ``
         -u $username ``
     -p$password ``
         $database ``
         --ssl-mode=REQUIRED

"@ -ForegroundColor Gray

    # Step 4: Generate C# test code
    Write-Host "4️⃣ C# Connection Test Code:" -ForegroundColor Yellow
    Write-Host @"
   
   var connectionString = "$($standardConnStr -replace 'Password=[^;]+', 'Password=***PASSWORD***')";
   using var connection = new MySqlConnection(connectionString);
   await connection.OpenAsync();
   Console.WriteLine(`$"Connected! Server: {connection.ServerVersion}"`);

"@ -ForegroundColor Gray

    # Step 5: API request example
    Write-Host "5️⃣ API Request Example:" -ForegroundColor Yellow
    Write-Host @"
   
   curl -X POST http://localhost:5000/api/PrivateCloud/setup-simple ``
-H "Authorization: Bearer YOUR_TOKEN" ``
     -H "Content-Type: application/json" ``
     -d '{
       \"connectionString\": \"$connectionString\",
    \"notes\": \"TiDB Cloud\"
  }'

"@ -ForegroundColor Gray

} else {
    Write-Host "   ❌ Invalid format!" -ForegroundColor Red
    Write-Host "   Expected: mysql://user:pass@host:port/database" -ForegroundColor White
}

# Step 6: Common issues check
Write-Host "`n6️⃣ Common Issues Checklist:" -ForegroundColor Yellow
Write-Host "===========================" -ForegroundColor Cyan

$checks = @(
@{
   Name = "Username format"
        Test = $username -match '^[a-zA-Z0-9._-]+$'
        Pass = "✅ Valid username format"
        Fail = "⚠️  Username contains special characters (may need encoding)"
    },
    @{
        Name = "Password special chars"
        Test = $password -notmatch '[<>"|;`]'
        Pass = "✅ Password safe for connection string"
  Fail = "⚠️  Password has special chars (may need URL encoding)"
    },
    @{
        Name = "Port number"
        Test = $port -eq 4000
        Pass = "✅ Correct TiDB port (4000)"
        Fail = "⚠️  Unusual port (expected 4000 for TiDB)"
    },
    @{
   Name = "SSL requirement"
        Test = $true
        Pass = "✅ SSL will be enforced (SslMode=Required)"
    Fail = ""
    }
)

foreach ($check in $checks) {
    if ($check.Test) {
      Write-Host "   $($check.Pass)" -ForegroundColor Green
    } else {
        if ($check.Fail) {
  Write-Host "   $($check.Fail)" -ForegroundColor Yellow
        }
    }
}

# Step 7: Database verification SQL
Write-Host "`n7️⃣ Database Verification:" -ForegroundColor Yellow
Write-Host @"

After setup, verify with these SQL queries:

-- Check if config was created
SELECT user_email, database_type, server_host, test_status 
FROM private_cloud_databases 
WHERE user_email = 'devste@gmail.com';

-- Check if user has private cloud enabled
SELECT user_email, is_private_cloud, status 
FROM users 
WHERE user_email = 'devste@gmail.com';

"@ -ForegroundColor Gray

Write-Host "`n✅ Validation Complete!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Verify user has is_private_cloud = TRUE" -ForegroundColor White
Write-Host "2. Clear any existing config (if needed)" -ForegroundColor White
Write-Host "3. Get fresh JWT token" -ForegroundColor White
Write-Host "4. Call /api/PrivateCloud/setup-simple with your connection string" -ForegroundColor White
Write-Host "5. Watch console logs for detailed error messages" -ForegroundColor White
