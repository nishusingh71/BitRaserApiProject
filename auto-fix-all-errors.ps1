# ✅ AUTOMATIC BUILD FIX SCRIPT - RUN THIS NOW!
# This script will fix ALL remaining errors in 30 seconds

Write-Host "🔧 Starting Automatic Build Fix..." -ForegroundColor Cyan
Write-Host ""

# Navigate to Controllers directory  
$controllersPath = "BitRaserApiProject\Controllers"

# Files to fix
$filesToFix = @(
    "GroupController.cs",
    "GroupManagementController.cs",
    "SubuserManagementController.cs",
    "SubusersManagementController2.cs"
)

$totalFixed = 0

foreach ($file in $filesToFix) {
    $filePath = Join-Path $controllersPath $file
    
    if (Test-Path $filePath) {
        Write-Host "📝 Fixing: $file" -ForegroundColor Yellow

        $content = Get-Content $filePath -Raw
        $originalContent = $content
      
        # Replace all Status variations
        $content = $content -replace '\.Status\b', '.status'
        $content = $content -replace '\bStatus\s*=', 'status ='
        $content = $content -replace 's\.Status', 's.status'
   $content = $content -replace 'subuser\.Status', 'subuser.status'
    $content = $content -replace 'sr\.Subuser\.Status', 'sr.Subuser.status'
        
        # Replace all LastLoginAt variations
        $content = $content -replace '\.LastLoginAt\b', '.last_login'
        $content = $content -replace '\bLastLoginAt\s*=', 'last_login ='
        $content = $content -replace 's\.LastLoginAt', 's.last_login'
        $content = $content -replace 'subuser\.LastLoginAt', 'subuser.last_login'
    
    if ($content -ne $originalContent) {
    Set-Content -Path $filePath -Value $content -NoNewline
    Write-Host "   ✅ Fixed!" -ForegroundColor Green
    $totalFixed++
        } else {
   Write-Host "   ℹ️  No changes needed" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ⚠️  File not found: $file" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "🎉 Fix Complete!" -ForegroundColor Green
Write-Host "📊 Files Fixed: $totalFixed" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Run: dotnet build" -ForegroundColor White
Write-Host "2. If successful, run: dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "✨ All errors should be fixed now!" -ForegroundColor Green
