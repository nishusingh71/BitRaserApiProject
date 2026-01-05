# Live Monitor - Private Cloud Setup
# Watches for changes in real-time

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "🔴 LIVE: Private Cloud Setup Monitor" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$API_URL = Read-Host "API URL (default: https://localhost:44316)"
if ([string]::IsNullOrWhiteSpace($API_URL)) {
    $API_URL = "https://localhost:44316"
}

$USER_EMAIL = Read-Host "User Email (default: devste@gmail.com)"
if ([string]::IsNullOrWhiteSpace($USER_EMAIL)) {
    $USER_EMAIL = "devste@gmail.com"
}

Write-Host "Enter password for $USER_EMAIL:" -NoNewline
$userPassword = Read-Host -AsSecureString
$userPasswordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($userPassword))

# Login
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
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $TOKEN"
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "🔴 LIVE MONITORING STARTED" -ForegroundColor Red
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Monitoring for changes every 3 seconds..." -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

$lastState = $null
$iteration = 0

while ($true) {
    $iteration++
    Clear-Host

    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "🔴 LIVE: Private Cloud Setup Monitor" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Iteration: $iteration | Time: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Gray
    Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
  Write-Host ""
    
    try {
      # Check access
        $accessResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/check-access" `
            -Method GET `
          -Headers $headers `
            -ErrorAction SilentlyContinue
      
        if ($accessResponse) {
            Write-Host "📊 Access Status:" -ForegroundColor Yellow
       Write-Host "   Has Access: $($accessResponse.hasPrivateCloudAccess)" -ForegroundColor $(if($accessResponse.hasPrivateCloudAccess) {"Green"} else {"Red"})
   Write-Host "   Is Configured: $($accessResponse.isConfigured)" -ForegroundColor $(if($accessResponse.isConfigured) {"Green"} else {"Red"})
    Write-Host "   Schema Initialized: $($accessResponse.isSchemaInitialized)" -ForegroundColor $(if($accessResponse.isSchemaInitialized) {"Green"} else {"Red"})
        
          # Detect changes
      $currentState = "$($accessResponse.isConfigured)|$($accessResponse.isSchemaInitialized)"
            
            if ($lastState -ne $null -and $lastState -ne $currentState) {
    Write-Host ""
       Write-Host "🔔 CHANGE DETECTED!" -ForegroundColor Magenta
       
             if ($accessResponse.isConfigured -and $lastState -notmatch "True\|") {
      Write-Host "   ✅ Configuration has been set up!" -ForegroundColor Green
  [System.Media.SystemSounds]::Beep.Play()
           }
             
                if ($accessResponse.isSchemaInitialized -and $lastState -notmatch "\|True$") {
                Write-Host "   ✅ Schema has been initialized!" -ForegroundColor Green
          [System.Media.SystemSounds]::Beep.Play()
      }
            }
            
            $lastState = $currentState
        }
        
      Write-Host ""
     
        # Check configuration details
        try {
       $configResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/config" `
                -Method GET `
       -Headers $headers `
             -ErrorAction SilentlyContinue
     
            if ($configResponse) {
        Write-Host "⚙️  Configuration Details:" -ForegroundColor Yellow
     Write-Host "   Config ID: $($configResponse.configId)" -ForegroundColor Gray
  Write-Host "   Database Type: $($configResponse.databaseType)" -ForegroundColor Gray
   Write-Host "Database Name: $($configResponse.databaseName)" -ForegroundColor Cyan
   Write-Host "   Server Host: $($configResponse.serverHost)" -ForegroundColor Gray
         Write-Host "   Server Port: $($configResponse.serverPort)" -ForegroundColor Gray
     Write-Host "   Test Status: $($configResponse.testStatus)" -ForegroundColor $(if($configResponse.testStatus -eq "success") {"Green"} else {"Red"})
   Write-Host "   Schema Initialized: $($configResponse.schemaInitialized)" -ForegroundColor $(if($configResponse.schemaInitialized) {"Green"} else {"Red"})
                Write-Host "   Last Tested: $($configResponse.lastTestedAt)" -ForegroundColor Gray
            } else {
     Write-Host "⚙️  Configuration: Not yet configured" -ForegroundColor Yellow
          }
        } catch {
            Write-Host "⚙️  Configuration: Not yet configured" -ForegroundColor Yellow
        }
        
        Write-Host ""
        
        # Show required tables
        try {
 $tablesResponse = Invoke-RestMethod -Uri "$API_URL/api/PrivateCloud/required-tables" `
     -Method GET `
    -Headers $headers `
    -ErrorAction SilentlyContinue
  
            if ($tablesResponse) {
    Write-Host "📋 Required Tables ($($tablesResponse.totalCount)):" -ForegroundColor Yellow
       foreach ($table in $tablesResponse.tables) {
       Write-Host "   - $table" -ForegroundColor Gray
      }
  }
        } catch {
     # Ignore error
        }
        
    } catch {
   Write-Host "❌ Error checking status: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Waiting 3 seconds..." -ForegroundColor DarkGray
    Start-Sleep -Seconds 3
}
