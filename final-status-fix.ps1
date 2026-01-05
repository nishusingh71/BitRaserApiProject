# Final fix for all Status-related errors
# This script fixes remaining Status property issues

Write-Host "🔧 Final fixing of Status properties..." -ForegroundColor Cyan

$files = @(
    "BitRaserApiProject\Controllers\SubuserManagementController.cs",
    "BitRaserApiProject\Controllers\GroupController.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "📝 Processing: $file" -ForegroundColor Yellow
        
        $content = Get-Content $file -Raw
        
    # Fix DTO access - dto.status should be dto.Status
      $content = $content -replace 'subuser\.status\s*=\s*dto\.status', 'subuser.status = dto.Status'
        $content = $content -replace 'group\.status\s*=\s*dto\.status', 'group.status = dto.Status'
      
   # Fix initialization - Status = "active" should be status = "active"
        $content = $content -replace '(\s+)Status\s*=\s*"active"(?=,)', '$1status = "active"'

        Set-Content $file -Value $content -NoNewline
   Write-Host "✅ Fixed: $file" -ForegroundColor Green
    } else {
        Write-Host "❌ File not found: $file" -ForegroundColor Red
    }
}

Write-Host "`n🎉 All errors fixed!" -ForegroundColor Green
Write-Host "Now run: dotnet build" -ForegroundColor Cyan
