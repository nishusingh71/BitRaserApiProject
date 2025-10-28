# Fix DTO Status references in controllers
# This script replaces dto.status with dto.Status in controller files

Write-Host "🔧 Fixing DTO Status references..." -ForegroundColor Cyan

$files = @(
    "BitRaserApiProject\Controllers\SubuserManagementController.cs",
    "BitRaserApiProject\Controllers\GroupController.cs",
    "BitRaserApiProject\Controllers\GroupManagementController.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "📝 Processing: $file" -ForegroundColor Yellow
        
        $content = Get-Content $file -Raw
        
  # Replace dto.status with dto.Status (for DTO access)
        $content = $content -replace 'dto\.status(?=\s*!=)', 'dto.Status'
        $content = $content -replace 'dto\.status(?=\s*\))', 'dto.Status'
     
   # Fix GroupMemberItemDto status property
      $content = $content -replace '(\s+)status\s*=\s*"active"', '$1Status = "active"'
        
  Set-Content $file -Value $content -NoNewline
 Write-Host "✅ Fixed: $file" -ForegroundColor Green
  } else {
        Write-Host "❌ File not found: $file" -ForegroundColor Red
    }
}

Write-Host "`n🎉 All DTO references fixed!" -ForegroundColor Green
Write-Host "Now run: dotnet build" -ForegroundColor Cyan
