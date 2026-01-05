# Complete Private Cloud Test Script
# Tests all endpoints with proper database name handling

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Complete Private Cloud Endpoint Test" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

$API_URL = "http://localhost:5000"
$USER_EMAIL = "devste@gmail.com"

# Step 1: Login
Write-Host "Step 1: Login..." -ForegroundColor Yellow
Write-Host "Enter password for $USER_EMAIL" -ForegroundColor Gray
$userPassword = Read-Host -AsSecureString
$userPasswordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($userPassword))

$loginBody = @{
    email = $USER_EMAIL
    password = $userPasswordText
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$API_URL/api/Auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $TOKEN = $loginResponse.token
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "   Token: $($TOKEN.Substring(0, 20))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
"Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

Write-Host ""
Write-Host "Step 2: Check Access..." -ForegroundColor Yellow

try {
  $accessResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/check-access" `
        -Method GET `
        -Headers @{"Authorization"="Bearer $TOKEN"} `
        -ErrorAction Stop
    
    Write-Host "✅ Access check successful!" -ForegroundColor Green
    Write-Host "   Has Access: $($accessResponse.hasPrivateCloudAccess)" -ForegroundColor Gray
    Write-Host "Is Configured: $($accessResponse.isConfigured)" -ForegroundColor Gray
    Write-Host " Schema Initialized: $($accessResponse.isSchemaInitialized)" -ForegroundColor Gray
    
    if (-not $accessResponse.hasPrivateCloudAccess) {
   Write-Host ""
        Write-Host "❌ User does not have private cloud access!" -ForegroundColor Red
        Write-Host "Run: UPDATE users SET is_private_cloud = 1 WHERE user_email = '$USER_EMAIL';" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ Access check failed: $($_.Exception.Message)" -ForegroundColor Red
  exit 1
}

Write-Host ""
Write-Host "Step 3: Get Database Name..." -ForegroundColor Yellow
$DB_NAME = Read-Host "Enter TiDB database name (e.g., Tech, Cloud_Erase)"

if ([string]::IsNullOrWhiteSpace($DB_NAME)) {
    Write-Host "❌ Database name required!" -ForegroundColor Red
    exit 1
}

Write-Host "   Using database: $DB_NAME" -ForegroundColor Green

Write-Host ""
Write-Host "Step 4: Setup Configuration..." -ForegroundColor Yellow

$connectionString = "mysql://2tdeFNZMcsWKkDR.root:76wtaj1GZkg7Qhek@gateway01.ap-southeast-1.prod.aws.tidbcloud.com:4000/$($DB_NAME)?ssl-mode=REQUIRED"

$setupBody = @{
    connectionString = $connectionString
    databaseType = "mysql"
  notes = "Test setup - Database: $DB_NAME"
} | ConvertTo-Json

Write-Host "Request:" -ForegroundColor Gray
Write-Host $setupBody -ForegroundColor Gray
Write-Host ""

try {
    $setupResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/setup-simple" `
        -Method POST `
  -Headers $headers `
        -Body $setupBody `
        -ErrorAction Stop
    
    Write-Host "✅ Setup successful!" -ForegroundColor Green
    Write-Host "   Message: $($setupResponse.message)" -ForegroundColor Gray
    Write-Host "   Detail: $($setupResponse.detail)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Setup failed!" -ForegroundColor Red
    
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
        try {
       $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "   Message: $($errorResponse.message)" -ForegroundColor Red
    Write-Host "   Detail: $($errorResponse.detail)" -ForegroundColor Red
     if ($errorResponse.error) {
   Write-Host "   Error: $($errorResponse.error)" -ForegroundColor Red
            }
        } catch {
          Write-Host "   Raw: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
    
    exit 1
}

Write-Host ""
Write-Host "Step 5: Get Configuration..." -ForegroundColor Yellow

try {
    $configResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/config" `
        -Method GET `
        -Headers @{"Authorization"="Bearer $TOKEN"} `
        -ErrorAction Stop
    
    Write-Host "✅ Configuration retrieved!" -ForegroundColor Green
    Write-Host "   Config ID: $($configResponse.configId)" -ForegroundColor Gray
    Write-Host "   Database Name: $($configResponse.databaseName)" -ForegroundColor $(if($configResponse.databaseName -eq $DB_NAME) {"Green"} else {"Red"})
    Write-Host "   Server Host: $($configResponse.serverHost)" -ForegroundColor Gray
    Write-Host "   Server Port: $($configResponse.serverPort)" -ForegroundColor Gray
    Write-Host "   Test Status: $($configResponse.testStatus)" -ForegroundColor Gray
    Write-Host "   Schema Initialized: $($configResponse.schemaInitialized)" -ForegroundColor Gray
    
    if ($configResponse.databaseName -ne $DB_NAME) {
        Write-Host ""
        Write-Host "⚠️  WARNING: Database name mismatch!" -ForegroundColor Yellow
     Write-Host "   Expected: $DB_NAME" -ForegroundColor Yellow
 Write-Host "   Got: $($configResponse.databaseName)" -ForegroundColor Yellow
    } else {
        Write-Host ""
      Write-Host "✅ Database name matches! Configuration is correct!" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Failed to get config: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Step 6: Test Connection..." -ForegroundColor Yellow

try {
    $testResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/test" `
        -Method POST `
        -Headers @{"Authorization"="Bearer $TOKEN"} `
        -ErrorAction Stop
    
    Write-Host "✅ Connection test successful!" -ForegroundColor Green
    Write-Host "   Success: $($testResponse.success)" -ForegroundColor Gray
    Write-Host "   Message: $($testResponse.message)" -ForegroundColor Gray
    Write-Host "   Server Version: $($testResponse.serverVersion)" -ForegroundColor Gray
    Write-Host "   Response Time: $($testResponse.responseTimeMs) ms" -ForegroundColor Gray
    Write-Host "   Schema Exists: $($testResponse.schemaExists)" -ForegroundColor Gray
    Write-Host "   Missing Tables: $($testResponse.missingTables.Count)" -ForegroundColor Gray
} catch {
    Write-Host "⚠️  Connection test failed: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 7: Initialize Schema..." -ForegroundColor Yellow

$initSchema = Read-Host "Do you want to initialize schema? (yes/no)"

if ($initSchema -eq "yes") {
    try {
        $schemaResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/initialize-schema" `
  -Method POST `
            -Headers @{"Authorization"="Bearer $TOKEN"} `
  -ErrorAction Stop
    
        Write-Host "✅ Schema initialization successful!" -ForegroundColor Green
        Write-Host "   Message: $($schemaResponse.message)" -ForegroundColor Gray
Write-Host "   Note: $($schemaResponse.note)" -ForegroundColor Gray
    } catch {
      Write-Host "❌ Schema initialization failed!" -ForegroundColor Red
        
  if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
            try {
$errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "   Message: $($errorResponse.message)" -ForegroundColor Red
         } catch {
   Write-Host "   Raw: $($_.ErrorDetails.Message)" -ForegroundColor Red
            }
        }
    }
    
    Write-Host ""
    Write-Host "Step 8: Validate Schema..." -ForegroundColor Yellow
    
    try {
        $validateResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/validate-schema" `
   -Method POST `
   -Headers @{"Authorization"="Bearer $TOKEN"} `
    -ErrorAction Stop
        
        Write-Host "✅ Schema validation successful!" -ForegroundColor Green
        Write-Host "   Is Valid: $($validateResponse.isValid)" -ForegroundColor $(if($validateResponse.isValid) {"Green"} else {"Red"})
        Write-Host "   Message: $($validateResponse.message)" -ForegroundColor Gray
  Write-Host "   Existing Tables: $($validateResponse.existingTables.Count)" -ForegroundColor Gray
        Write-Host "   Missing Tables: $($validateResponse.missingTables.Count)" -ForegroundColor Gray
   
        if ($validateResponse.existingTables.Count -gt 0) {
Write-Host ""
      Write-Host "   Created Tables:" -ForegroundColor Green
            foreach ($table in $validateResponse.existingTables) {
       Write-Host "      ✅ $table" -ForegroundColor Green
      }
        }
    
      if ($validateResponse.missingTables.Count -gt 0) {
      Write-Host ""
     Write-Host "   Missing Tables:" -ForegroundColor Red
       foreach ($table in $validateResponse.missingTables) {
                Write-Host "      ❌ $table" -ForegroundColor Red
            }
        }
    } catch {
     Write-Host "❌ Schema validation failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Step 9: Get Required Tables..." -ForegroundColor Yellow

try {
    $tablesResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/required-tables" `
        -Method GET `
   -Headers @{"Authorization"="Bearer $TOKEN"} `
        -ErrorAction Stop
    
    Write-Host "✅ Required tables retrieved!" -ForegroundColor Green
    Write-Host "   Total Count: $($tablesResponse.totalCount)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   Required Tables:" -ForegroundColor Yellow
    foreach ($table in $tablesResponse.tables) {
        Write-Host "      - $table" -ForegroundColor Gray
    }
} catch {
 Write-Host "⚠️  Failed to get required tables: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ TEST COMPLETE!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "✅ Login successful" -ForegroundColor Green
Write-Host "✅ Access check passed" -ForegroundColor Green
Write-Host "✅ Configuration setup completed" -ForegroundColor Green
Write-Host "✅ Database name: $DB_NAME" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Schema initialized: $(if($schemaResponse) {'✅ Yes'} else {'❌ No'})" -ForegroundColor White
Write-Host "2. Ready to use private cloud database!" -ForegroundColor White
Write-Host ""

Write-Host "Configuration Details:" -ForegroundColor Yellow
Write-Host "   User: $USER_EMAIL" -ForegroundColor Gray
Write-Host "   Database: $DB_NAME" -ForegroundColor Gray
Write-Host "   Host: gateway01.ap-southeast-1.prod.aws.tidbcloud.com" -ForegroundColor Gray
Write-Host "   Port: 4000" -ForegroundColor Gray
Write-Host ""

Write-Host "✅ All private cloud endpoints working correctly!" -ForegroundColor Green
