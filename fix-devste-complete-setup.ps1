# Fix devste@gmail.com Private Cloud Setup
# Comprehensive debugging and fix script

$ErrorActionPreference = "Continue"

Write-Host "🔧 ===== FIX devste@gmail.com PRIVATE CLOUD SETUP =====" -ForegroundColor Cyan
Write-Host ""

# Configuration
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkZXZzdGVAZ21haWwuY29tIiwianRpIjoiZmE3NDExZDEtMTVkZC00ZGIxLTg4NmMtNWQ5MjAyNjA1MWUwIiwiZXhwIjoxNzY0MDYxMjA3LCJpc3MiOiJEaHJ1dkFwaUlzc3VlciIsImF1ZCI6IkRocnV2QXBpQXVkaWVuY2UifQ.EHp_Aun2L1iuwCjXYw5Fmfwcdr5A_msFzqtKOm-YMbg"
$baseUrl = "https://localhost:44316"
$headers = @{
    "Authorization" = "Bearer $token"
 "Content-Type" = "application/json"
}

# Test connection string
$connectionString = "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;"

Write-Host "📋 Configuration:" -ForegroundColor Yellow
Write-Host "   User: devste@gmail.com"
Write-Host "   Base URL: $baseUrl"
Write-Host "   Token: Present (${token.Length} chars)"
Write-Host "   Connection String: Present (${connectionString.Length} chars)"
Write-Host ""

# Step 1: Check API health
Write-Host "1️⃣ Checking API Health..." -ForegroundColor Cyan
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method Get -SkipCertificateCheck -ErrorAction Stop 2>$null
Write-Host "   ✅ API is healthy" -ForegroundColor Green
} catch {
    Write-Host "   ⚠️  API health check failed: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# Step 2: Check user's private cloud access
Write-Host "2️⃣ Checking Private Cloud Access..." -ForegroundColor Cyan
try {
    $access = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/check-access" `
        -Method Get `
        -Headers @{ "Authorization" = "Bearer $token" } `
 -SkipCertificateCheck `
      -ErrorAction Stop
    
    Write-Host "   ✅ Has Access: $($access.hasPrivateCloudAccess)" -ForegroundColor Green
    Write-Host "   Is Configured: $($access.isConfigured)"
    Write-Host "   Schema Initialized: $($access.isSchemaInitialized)"
    Write-Host "   Current User: $($access.currentUser)"
    
    if (-not $access.hasPrivateCloudAccess) {
        Write-Host "   ❌ User does not have private cloud access!" -ForegroundColor Red
        Write-Host "   💡 Run this SQL in Main DB:" -ForegroundColor Yellow
   Write-Host "   UPDATE users SET is_private_cloud = TRUE WHERE user_email = 'devste@gmail.com';"
 exit 1
  }
} catch {
    Write-Host "   ❌ Error checking access: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Response: $($_.ErrorDetails.Message)"
}
Write-Host ""

# Step 3: Attempt complete setup with detailed error tracking
Write-Host "3️⃣ Attempting Complete Setup..." -ForegroundColor Cyan
Write-Host "   📦 Preparing request body..."

$setupBody = @{
    connectionString = $connectionString
    databaseType = "mysql"
    notes = "Cloud_Erase database - Fixed setup"
    migrateExistingData = $false  # Don't migrate on first attempt
} | ConvertTo-Json

Write-Host "   ✅ Request body prepared"
Write-Host ""

try {
    Write-Host "   🚀 Sending POST request to /complete-setup..."
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/complete-setup" `
        -Method Post `
      -Headers $headers `
     -Body $setupBody `
        -SkipCertificateCheck `
        -ErrorAction Stop `
-Verbose
    
    Write-Host ""
Write-Host "   ✅ Setup Response Received:" -ForegroundColor Green
    Write-Host "   Success: $($response.success)"
    Write-Host "   Message: $($response.message)"
    Write-Host "   User: $($response.userEmail)"
    Write-Host ""
    
    Write-Host "   📊 Steps Completed:" -ForegroundColor Cyan
    foreach ($step in $response.steps) {
        $statusColor = if ($step.success) { "Green" } else { "Red" }
     Write-Host "   $($step.step). $($step.name): $($step.status)" -ForegroundColor $statusColor
     
        if ($step.details) {
   Write-Host " Details: $($step.details)" -ForegroundColor Gray
        }
        
        if ($step.error) {
          Write-Host "  ❌ Error: $($step.error)" -ForegroundColor Red
   }
    }
    
    Write-Host ""
    Write-Host "   📈 Summary:" -ForegroundColor Cyan
    Write-Host "   Total Steps: $($response.summary.totalSteps)"
    Write-Host "   Successful Steps: $($response.summary.successfulSteps)"
    Write-Host "   Tenant Routing: $($response.summary.tenantRoutingEnabled)"
    
    if ($response.success) {
Write-Host ""
      Write-Host "   🎉 SETUP COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "   ⚠️  Setup completed with warnings" -ForegroundColor Yellow
        Write-Host "   Failed at: $($response.summary.failedAt)" -ForegroundColor Yellow
        if ($response.summary.error) {
         Write-Host "   Error: $($response.summary.error)" -ForegroundColor Yellow
        }
    }
    
} catch {
    Write-Host ""
    Write-Host "   ❌ SETUP FAILED!" -ForegroundColor Red
    Write-Host "   Exception: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host ""
 Write-Host "   📝 Error Details:" -ForegroundColor Yellow
        try {
          $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
       $errorJson | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Yellow
  
         # Check for specific error in steps
  if ($errorJson.steps) {
         Write-Host ""
  Write-Host "   🔍 Failed Steps:" -ForegroundColor Yellow
    foreach ($step in $errorJson.steps) {
        if (-not $step.success) {
                Write-Host "   Step $($step.step): $($step.name) - FAILED" -ForegroundColor Red
          if ($step.error) {
             Write-Host "      Error: $($step.error)" -ForegroundColor Red
            }
            }
 }
            }
        } catch {
  Write-Host $_.ErrorDetails.Message -ForegroundColor Yellow
        }
    }
    
    if ($_.Exception.Response) {
   Write-Host ""
        Write-Host "   🌐 HTTP Response Details:" -ForegroundColor Yellow
        Write-Host "   Status Code: $($_.Exception.Response.StatusCode)"
        Write-Host "   Status Description: $($_.Exception.Response.StatusDescription)"
 }
}

Write-Host ""
Write-Host "===== DIAGNOSTIC CHECKS =====" -ForegroundColor Cyan

# Step 4: Verify configuration was saved (if setup succeeded partially)
Write-Host ""
Write-Host "4️⃣ Checking if configuration was saved..." -ForegroundColor Cyan
try {
    $config = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/config" `
        -Method Get `
     -Headers @{ "Authorization" = "Bearer $token" } `
        -SkipCertificateCheck `
  -ErrorAction Stop
    
    Write-Host "   ✅ Configuration exists:" -ForegroundColor Green
    Write-Host "   Config ID: $($config.configId)"
    Write-Host "   Database Type: $($config.databaseType)"
    Write-Host "   Server Host: $($config.serverHost)"
    Write-Host "   Server Port: $($config.serverPort)"
    Write-Host "   Database Name: $($config.databaseName)"
 Write-Host " Is Active: $($config.isActive)"
    Write-Host "   Schema Initialized: $($config.schemaInitialized)"
    Write-Host "   Test Status: $($config.testStatus)"
    Write-Host "   Last Tested: $($config.lastTestedAt)"
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "   ℹ️  No configuration found yet" -ForegroundColor Yellow
    } else {
        Write-Host "   ⚠️  Error checking configuration: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Step 5: Test routing (if configured)
Write-Host ""
Write-Host "5️⃣ Testing Tenant Routing..." -ForegroundColor Cyan
try {
    $routing = Invoke-RestMethod -Uri "$baseUrl/api/PrivateCloud/test-routing" `
        -Method Get `
        -Headers @{ "Authorization" = "Bearer $token" } `
-SkipCertificateCheck `
        -ErrorAction Stop
    
  Write-Host "   ✅ Routing test result:" -ForegroundColor Green
    Write-Host "   Status: $($routing.routingStatus)"
    Write-Host "   Is Private Cloud: $($routing.isPrivateCloud)"
    Write-Host "   Can Connect: $($routing.canConnect)"
    Write-Host "   Database: $($routing.database)"
    Write-Host "   Message: $($routing.message)"
    
    if ($routing.statistics) {
 Write-Host "   📊 Statistics:"
        Write-Host "      Audit Reports: $($routing.statistics.auditReports)"
   Write-Host "      Subusers: $($routing.statistics.subusers)"
    }
} catch {
    Write-Host "   ⚠️  Routing test not available yet: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "===== SUMMARY & RECOMMENDATIONS =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Check application logs for detailed error messages:"
Write-Host "   - Look for 'PrivateCloudService' logs"
Write-Host "   - Look for 'SetupPrivateDatabaseFromConnectionStringAsync' logs"
Write-Host ""
Write-Host "2. Verify database connection manually:"
Write-Host "   mysql -h gateway01.ap-southeast-1.prod.aws.tidbcloud.com -P 4000 -u 4WScT7meioLLU3B.root -p -D Cloud_Erase"
Write-Host ""
Write-Host "3. Check Main DB for user configuration:"
Write-Host "   SELECT user_id, user_email, is_private_cloud FROM users WHERE user_email = 'devste@gmail.com';"
Write-Host "   SELECT * FROM PrivateCloudDatabases WHERE UserEmail = 'devste@gmail.com';"
Write-Host ""
Write-Host "4. If Step 1 failed, check:"
Write-Host "   - Connection string format"
Write-Host "   - Database credentials"
Write-Host "   - Network connectivity"
Write-Host "   - SSL/TLS requirements"
Write-Host ""
Write-Host "===== SCRIPT COMPLETE =====" -ForegroundColor Cyan
