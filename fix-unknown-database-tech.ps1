# Fix Unknown Database 'Tech' Error
# Quick SQL commands to check and fix database

Write-Host "🔍 Fixing Unknown Database 'Tech' Error" -ForegroundColor Cyan
Write-Host ""

Write-Host "Database Error Details:" -ForegroundColor Yellow
Write-Host "❌ Unknown database 'Tech'" -ForegroundColor Red
Write-Host "   Server: gateway01.ap-southeast-1.prod.aws.tidbcloud.com:4000" -ForegroundColor Gray
Write-Host " User: 4WScT7meioLLU3B.root" -ForegroundColor Gray
Write-Host ""

Write-Host "===== SOLUTION =====" -ForegroundColor Cyan
Write-Host ""

Write-Host "Option 1: Use Existing Database" -ForegroundColor Yellow
Write-Host ""
Write-Host "Connect to TiDB and check available databases:" -ForegroundColor White
Write-Host ""
Write-Host "mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com \"
Write-Host "  -P 4000 \"
Write-Host "  -u 4WScT7meioLLU3B.root \"
Write-Host "  -p \"
Write-Host "  --ssl-mode=REQUIRED" -ForegroundColor Green
Write-Host ""
Write-Host "Then run:" -ForegroundColor White
Write-Host "SHOW DATABASES;" -ForegroundColor Green
Write-Host ""

Write-Host "Expected Output:" -ForegroundColor Gray
Write-Host "+--------------------+"
Write-Host "| Database    |"
Write-Host "+--------------------+"
Write-Host "| information_schema |"
Write-Host "| mysql|"
Write-Host "| test   |"
Write-Host "| Cloud_Erase        |"
Write-Host "| Cloud_Erase__App   |"
Write-Host "+--------------------+"
Write-Host ""

Write-Host "===== FIX OPTIONS =====" -ForegroundColor Cyan
Write-Host ""

Write-Host "Fix A: Create 'Tech' Database" -ForegroundColor Yellow
Write-Host ""
Write-Host "CREATE DATABASE IF NOT EXISTS Tech CHARACTER SET utf8mb4 COLLATE utf8mb4_bin;" -ForegroundColor Green
Write-Host ""

Write-Host "Fix B: Use Existing Database" -ForegroundColor Yellow
Write-Host ""
Write-Host "Update your connection string to use an existing database:" -ForegroundColor White
Write-Host ""
Write-Host "Option B1 - Use 'Cloud_Erase':" -ForegroundColor Cyan
Write-Host "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;" -ForegroundColor Green
Write-Host ""

Write-Host "Option B2 - Use 'Cloud_Erase__App':" -ForegroundColor Cyan
Write-Host "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase__App;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;" -ForegroundColor Green
Write-Host ""

Write-Host "Option B3 - Use 'test' (for testing):" -ForegroundColor Cyan
Write-Host "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=test;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;" -ForegroundColor Green
Write-Host ""

Write-Host "===== QUICK FIX COMMANDS =====" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Connect to TiDB:" -ForegroundColor Yellow
Write-Host @"
mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com \
  -P 4000 \
  -u 4WScT7meioLLU3B.root \
  -p89ayiOJGY2055G0g \
  --ssl-mode=REQUIRED
"@ -ForegroundColor Green
Write-Host ""

Write-Host "2. Create 'Tech' Database:" -ForegroundColor Yellow
Write-Host "CREATE DATABASE IF NOT EXISTS Tech;" -ForegroundColor Green
Write-Host ""

Write-Host "3. Verify Database Created:" -ForegroundColor Yellow
Write-Host "USE Tech;" -ForegroundColor Green
Write-Host "SHOW TABLES;" -ForegroundColor Green
Write-Host ""

Write-Host "===== RECOMMENDED APPROACH =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ RECOMMENDED: Use existing 'Cloud_Erase' database" -ForegroundColor Green
Write-Host ""
Write-Host "Why?" -ForegroundColor Yellow
Write-Host "- Database already exists" -ForegroundColor White
Write-Host "- No need to create new database" -ForegroundColor White
Write-Host "- Already has proper structure" -ForegroundColor White
Write-Host ""

Write-Host "Use this connection string:" -ForegroundColor Yellow
Write-Host "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;" -ForegroundColor Green
Write-Host ""

Write-Host "===== POWERSHELL TEST =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test the fixed connection string:" -ForegroundColor Yellow
Write-Host ""

$fixedConnectionString = "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;"

Write-Host "Connection String:" -ForegroundColor Cyan
Write-Host $fixedConnectionString -ForegroundColor Gray
Write-Host ""

Write-Host "To test this, run:" -ForegroundColor Yellow
Write-Host '.\test-connection-string-format.ps1 -ConnectionString "$fixedConnectionString"' -ForegroundColor Green
Write-Host ""

Write-Host "===== SUMMARY =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "Problem: Database 'Tech' does not exist" -ForegroundColor Red
Write-Host ""
Write-Host "Solutions:" -ForegroundColor Yellow
Write-Host "1. ✅ Use existing database: 'Cloud_Erase' (RECOMMENDED)" -ForegroundColor Green
Write-Host "2. ✅ Use existing database: 'Cloud_Erase__App'" -ForegroundColor Green
Write-Host "3. ⚠️  Create new database: 'Tech' (requires permission)" -ForegroundColor Yellow
Write-Host ""

Write-Host "Next Step:" -ForegroundColor Yellow
Write-Host "Update your connection string in the API request to use 'Cloud_Erase'" -ForegroundColor White
Write-Host ""
