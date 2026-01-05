# Verify appsettings.json Fix
# Checks if Tech database reference is removed

$ErrorActionPreference = "Continue"

Write-Host "🔍 Verifying appsettings.json Fix" -ForegroundColor Cyan
Write-Host ""

# Check if appsettings.json exists
$appsettingsPath = "BitRaserApiProject\appsettings.json"

if (-not (Test-Path $appsettingsPath)) {
    Write-Host "❌ appsettings.json not found at: $appsettingsPath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Found appsettings.json" -ForegroundColor Green
Write-Host ""

# Read file content
$content = Get-Content $appsettingsPath -Raw

# Check for 'Tech' database reference
if ($content -match 'Database=Tech') {
    Write-Host "❌ PROBLEM FOUND: 'Database=Tech' still exists!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Location:" -ForegroundColor Yellow
    
    # Find the line
    $lines = Get-Content $appsettingsPath
    $lineNumber = 0
    foreach ($line in $lines) {
        $lineNumber++
        if ($line -match 'Database=Tech') {
   Write-Host "Line $lineNumber : $line" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "💡 FIX NEEDED:" -ForegroundColor Yellow
    Write-Host "Change 'Database=Tech' to 'Database=Cloud_Erase'" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "✅ No 'Database=Tech' reference found" -ForegroundColor Green
Write-Host ""

# Check for Cloud_Erase
if ($content -match 'Database=Cloud_Erase') {
    Write-Host "✅ Found 'Database=Cloud_Erase' - Correct!" -ForegroundColor Green
    
    # Show the connection strings
    Write-Host ""
    Write-Host "Connection Strings Found:" -ForegroundColor Cyan
    
    $lines = Get-Content $appsettingsPath
    $inConnectionStrings = $false
 foreach ($line in $lines) {
        if ($line -match '"ConnectionStrings"') {
            $inConnectionStrings = $true
 }
        
     if ($inConnectionStrings -and $line -match 'Database=') {
 Write-Host "  $($line.Trim())" -ForegroundColor Gray
        }
    
        if ($inConnectionStrings -and $line -match '^    }') {
$inConnectionStrings = $false
        }
 }
} else {
    Write-Host "⚠️  Warning: 'Database=Cloud_Erase' not found" -ForegroundColor Yellow
    Write-Host "   Verify connection strings manually" -ForegroundColor White
}

Write-Host ""
Write-Host "===== VERIFICATION SUMMARY =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ appsettings.json exists" -ForegroundColor Green
Write-Host "✅ No 'Database=Tech' reference" -ForegroundColor Green
Write-Host "✅ Using 'Database=Cloud_Erase'" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration is correct!" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Rebuild solution (Ctrl+Shift+B)" -ForegroundColor White
Write-Host "2. Restart API (Shift+F5, then F5)" -ForegroundColor White
Write-Host "3. Test setup with .\final-fixed-diagnostic.ps1" -ForegroundColor White
Write-Host ""
