# PowerShell Script to Test Private Cloud Setup
# Usage: ./test-private-cloud.ps1

param(
    [string]$Email = "devste@gmail.com",
    [string]$Password = "your_password_here",
    [string]$ApiUrl = "http://localhost:5000"
)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Private Cloud Setup Test" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Login
Write-Host "Step 1: Getting JWT Token..." -ForegroundColor Yellow
$loginBody = @{
    email = $Email
    password = $Password
} | ConvertTo-Json

try {
  $loginResponse = Invoke-RestMethod -Uri "$ApiUrl/api/RoleBasedAuth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody
    
    $token = $loginResponse.token
    Write-Host "✅ Login successful" -ForegroundColor Green
    Write-Host "   User: $($loginResponse.email)" -ForegroundColor Gray
    Write-Host "   Type: $($loginResponse.userType)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Check Access
Write-Host "`nStep 2: Checking Private Cloud Access..." -ForegroundColor Yellow
try {
    $headers = @{
      "Authorization" = "Bearer $token"
    }
    
    $accessResponse = Invoke-RestMethod -Uri "$ApiUrl/api/PrivateCloud/check-access" `
    -Method Get `
    -Headers $headers
    
  Write-Host "✅ Access check successful" -ForegroundColor Green
    Write-Host "   Has Access: $($accessResponse.hasPrivateCloudAccess)" -ForegroundColor Gray
    Write-Host "   Is Configured: $($accessResponse.isConfigured)" -ForegroundColor Gray
    
    if (-not $accessResponse.hasPrivateCloudAccess) {
        Write-Host "`n❌ ERROR: User does not have private cloud access!" -ForegroundColor Red
     Write-Host "   Run this SQL query:" -ForegroundColor Yellow
   Write-Host "   UPDATE users SET is_private_cloud = TRUE WHERE user_email = '$Email';" -ForegroundColor White
        exit 1
    }
} catch {
    Write-Host "❌ Access check failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Setup Private Database
Write-Host "`nStep 3: Setting up Private Database..." -ForegroundColor Yellow
$setupBody = @{
    databaseType = "mysql"
    serverHost = "gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
    serverPort = 4000
    databaseName = "Cloud_Erase"
    databaseUsername = "2tdeFNZMcsWKkDR.root"
    databasePassword = "76wtaj1GZkg7Qhek"
    storageLimitMb = 1024
    notes = "TiDB Production Database"
} | ConvertTo-Json

Write-Host "   Credentials:" -ForegroundColor Gray
Write-Host "   Host: $($setupBody | ConvertFrom-Json | Select-Object -ExpandProperty serverHost)" -ForegroundColor Gray
Write-Host "   Port: $($setupBody | ConvertFrom-Json | Select-Object -ExpandProperty serverPort)" -ForegroundColor Gray
Write-Host "   Database: $($setupBody | ConvertFrom-Json | Select-Object -ExpandProperty databaseName)" -ForegroundColor Gray

try {
    $setupResponse = Invoke-RestMethod -Uri "$ApiUrl/api/PrivateCloud/setup" `
 -Method Post `
        -Headers $headers `
    -ContentType "application/json" `
        -Body $setupBody
    
    Write-Host "`n✅ ✅ ✅ SETUP SUCCESSFUL! ✅ ✅ ✅" -ForegroundColor Green
    Write-Host "   Message: $($setupResponse.message)" -ForegroundColor Gray
    Write-Host "   Next Step: $($setupResponse.nextStep)" -ForegroundColor Gray
} catch {
    Write-Host "`n❌ SETUP FAILED!" -ForegroundColor Red
    
 if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd() | ConvertFrom-Json
        
        Write-Host "`n📋 Error Details:" -ForegroundColor Yellow
        Write-Host "   Message: $($errorBody.message)" -ForegroundColor White
     Write-Host "   Detail: $($errorBody.detail)" -ForegroundColor White
        Write-Host "   User: $($errorBody.userEmail)" -ForegroundColor Gray
        Write-Host "   DB Type: $($errorBody.databaseType)" -ForegroundColor Gray
  Write-Host "   Host: $($errorBody.serverHost)" -ForegroundColor Gray
        
 if ($errorBody.error) {
    Write-Host "`n   Technical Error: $($errorBody.error)" -ForegroundColor Red
    }
  
        if ($errorBody.stackTrace) {
  Write-Host "`n   Stack Trace: $($errorBody.stackTrace)" -ForegroundColor DarkGray
    }
    } else {
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor White
    }
    
  Write-Host "`n🔍 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "   1. Check application console logs for detailed error" -ForegroundColor White
    Write-Host "   2. Look for lines starting with '===' or '❌'" -ForegroundColor White
    Write-Host "   3. Verify SQL: SELECT is_private_cloud FROM users WHERE user_email='$Email';" -ForegroundColor White
    Write-Host "   4. Test TiDB connection manually" -ForegroundColor White
    
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Test Completed Successfully!" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
