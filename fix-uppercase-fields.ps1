# ✅ AUTOMATIC FIX SCRIPT - Replace ALL Uppercase Fields
# Run this in PowerShell from project root directory

Write-Host "🔧 Starting Automatic Field Replacement..." -ForegroundColor Cyan
Write-Host "📂 Project Directory: BitRaserApiProject" -ForegroundColor Yellow

$projectPath = "BitRaserApiProject"

# Define replacements
$replacements = @{
    # Status field
    '\.Status'        = '.status'
    'Status\s*='      = 'status ='
    's\.Status'         = 's.status'
    'subuser\.Status'   = 'subuser.status'
    'sr\.Subuser\.Status' = 'sr.Subuser.status'
    
    # LastLoginAt field
    '\.LastLoginAt'     = '.last_login'
    'LastLoginAt\s*='   = 'last_login ='
  's\.LastLoginAt'    = 's.last_login'
    'subuser\.LastLoginAt' = 'subuser.last_login'
    
    # LastLogoutAt field
    '\.LastLogoutAt'    = '.last_logout'
    'LastLogoutAt\s*='  = 'last_login ='
 
    # Timezone field  
    '\.Timezone'        = '.timezone'
    'Timezone\s*='      = 'timezone ='
}

# Files to process
$filesToProcess = @(
    "$projectPath\ApplicationDbContext.cs",
    "$projectPath\Controllers\EnhancedSubusersController.cs",
    "$projectPath\Controllers\GroupController.cs",
    "$projectPath\Controllers\GroupManagementController.cs",
  "$projectPath\Controllers\SubuserManagementController.cs",
    "$projectPath\Controllers\SubusersManagementController2.cs"
)

$totalReplacements = 0

foreach ($file in $filesToProcess) {
    if (Test-Path $file) {
    Write-Host "📝 Processing: $file" -ForegroundColor Green
        
      $content = Get-Content $file -Raw
        $originalContent = $content
     
        foreach ($pattern in $replacements.Keys) {
       $replacement = $replacements[$pattern]
            $content = $content -replace $pattern, $replacement
        }
        
        if ($content -ne $originalContent) {
       Set-Content -Path $file -Value $content -NoNewline
     $totalReplacements++
       Write-Host "   ✅ Updated successfully" -ForegroundColor Cyan
        } else {
   Write-Host "   ℹ️  No changes needed" -ForegroundColor Gray
        }
    } else {
    Write-Host "   ⚠️  File not found: $file" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎉 Replacement Complete!" -ForegroundColor Green
Write-Host "📊 Files Modified: $totalReplacements" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Run: dotnet build" -ForegroundColor White
Write-Host "2. Check for any remaining errors" -ForegroundColor White
Write-Host "3. Run: dotnet run" -ForegroundColor White
