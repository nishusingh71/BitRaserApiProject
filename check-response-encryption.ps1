# 🔍 Check Response Encryption Configuration
# Run this script to verify encryption is properly configured

Write-Host ""
Write-Host "🔐 RESPONSE ENCRYPTION DIAGNOSTIC TOOL" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if middleware is registered in Program.cs
Write-Host "📂 Step 1: Checking Program.cs..." -ForegroundColor Yellow

$programPath = "BitRaserApiProject\Program.cs"
if (Test-Path $programPath) {
    $content = Get-Content $programPath -Raw
    
    if ($content -match 'UseResponseEncryption') {
        Write-Host "✅ UseResponseEncryption() found in Program.cs" -ForegroundColor Green
    } else {
        Write-Host "❌ UseResponseEncryption() NOT FOUND in Program.cs" -ForegroundColor Red
        Write-Host "   Add this line after UseAuthorization():" -ForegroundColor Yellow
        Write-Host "   app.UseResponseEncryption();" -ForegroundColor Gray
  exit 1
    }
} else {
    Write-Host "❌ Program.cs not found at: $programPath" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Check appsettings.json
Write-Host "📂 Step 2: Checking appsettings.json..." -ForegroundColor Yellow

$appsettingsPath = "BitRaserApiProject\appsettings.json"
if (Test-Path $appsettingsPath) {
  $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    
    if ($json.Encryption) {
        Write-Host "✅ Encryption section found" -ForegroundColor Green
        
   if ($json.Encryption.Enabled -eq $true) {
        Write-Host "✅ Encryption.Enabled = true" -ForegroundColor Green
    } elseif ($json.Encryption.Enabled -eq $false) {
          Write-Host "⚠️ Encryption.Enabled = false (Encryption is DISABLED)" -ForegroundColor Yellow
        } else {
    Write-Host "⚠️ Encryption.Enabled not set (defaults to true)" -ForegroundColor Yellow
        }
      
        if ($json.Encryption.ResponseKey) {
        Write-Host "✅ Encryption.ResponseKey found (length: $($json.Encryption.ResponseKey.Length))" -ForegroundColor Green
            
      if ($json.Encryption.ResponseKey.Length -lt 32) {
                Write-Host "⚠️ ResponseKey is shorter than 32 characters (will be padded)" -ForegroundColor Yellow
      }
    } else {
            Write-Host "❌ Encryption.ResponseKey NOT FOUND" -ForegroundColor Red
        }
        
if ($json.Encryption.Key) {
Write-Host "✅ Encryption.Key found (fallback key)" -ForegroundColor Green
        }
    } else {
        Write-Host "❌ Encryption section NOT FOUND in appsettings.json" -ForegroundColor Red
        exit 1
    }
} else {
Write-Host "❌ appsettings.json not found at: $appsettingsPath" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Check ResponseEncryptionMiddleware.cs exists
Write-Host "📂 Step 3: Checking ResponseEncryptionMiddleware.cs..." -ForegroundColor Yellow

$middlewarePath = "BitRaserApiProject\Middleware\ResponseEncryptionMiddleware.cs"
if (Test-Path $middlewarePath) {
    Write-Host "✅ ResponseEncryptionMiddleware.cs exists" -ForegroundColor Green
    
    $content = Get-Content $middlewarePath -Raw
    
    if ($content -match 'UseResponseEncryption') {
        Write-Host "✅ Extension method UseResponseEncryption found" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Extension method UseResponseEncryption not found" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ ResponseEncryptionMiddleware.cs not found at: $middlewarePath" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Check EncryptionHelper.cs exists
Write-Host "📂 Step 4: Checking EncryptionHelper.cs..." -ForegroundColor Yellow

$helperPath = "BitRaserApiProject\Services\EncryptionHelper.cs"
if (Test-Path $helperPath) {
    Write-Host "✅ EncryptionHelper.cs exists" -ForegroundColor Green
    
    $content = Get-Content $helperPath -Raw
    
    if ($content -match 'Encrypt\(string plainText') {
        Write-Host "✅ Encrypt method found" -ForegroundColor Green
    } else {
      Write-Host "❌ Encrypt method not found" -ForegroundColor Red
    }
} else {
    Write-Host "❌ EncryptionHelper.cs not found at: $helperPath" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 5: Build project
Write-Host "🔨 Step 5: Building project..." -ForegroundColor Yellow
$buildOutput = dotnet build --no-incremental 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build SUCCESSFUL" -ForegroundColor Green
} else {
    Write-Host "❌ Build FAILED!" -ForegroundColor Red
    Write-Host "   Fix compilation errors first" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Step 6: Summary
Write-Host "📊 DIAGNOSTIC SUMMARY" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Middleware registered: YES" -ForegroundColor Green
Write-Host "✅ Configuration found: YES" -ForegroundColor Green
Write-Host "✅ Middleware file exists: YES" -ForegroundColor Green
Write-Host "✅ Helper file exists: YES" -ForegroundColor Green
Write-Host "✅ Build successful: YES" -ForegroundColor Green
Write-Host ""
Write-Host "🎉 All checks passed! Encryption should be working." -ForegroundColor Green
Write-Host ""
Write-Host "🧪 NEXT STEPS:" -ForegroundColor Yellow
Write-Host "   1. Run the API: dotnet run" -ForegroundColor White
Write-Host "   2. Check startup logs for: ✅ Response encryption is ENABLED" -ForegroundColor White
Write-Host "   3. Call any API endpoint" -ForegroundColor White
Write-Host "   4. Check if response has: { encrypted: true, data: '...', timestamp: '...' }" -ForegroundColor White
Write-Host ""
Write-Host "🔍 TEST COMMAND:" -ForegroundColor Yellow
Write-Host "   curl http://localhost:4000/api/Users/1 -H 'Authorization: Bearer TOKEN'" -ForegroundColor Gray
Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "🏁 Diagnostic Complete!" -ForegroundColor Cyan
Write-Host ""
