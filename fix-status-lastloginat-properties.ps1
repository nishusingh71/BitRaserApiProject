# Fix Status and LastLoginAt properties in all controllers
# This script replaces Status with status and LastLoginAt with last_login

Write-Host "🔧 Fixing Status and LastLoginAt properties..." -ForegroundColor Cyan

$files = @(
    "BitRaserApiProject\Controllers\SubuserManagementController.cs",
    "BitRaserApiProject\Controllers\GroupController.cs",
    "BitRaserApiProject\Controllers\GroupManagementController.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
 Write-Host "📝 Processing: $file" -ForegroundColor Yellow
        
        $content = Get-Content $file -Raw
        
    # Replace Status property references
        $content = $content -replace '\.Status(?=\s*==)', '.status'
     $content = $content -replace '\.Status(?=\s*!=)', '.status'
        $content = $content -replace '\.Status(?=\s*,)', '.status'
 $content = $content -replace '\.Status(?=\s*;)', '.status'
        $content = $content -replace 'Status\s*=\s*s\.Status', 'Status = s.status'
        $content = $content -replace 'Status\s*=\s*subuser\.Status', 'Status = subuser.status'
        $content = $content -replace 'Status\s*=\s*sr\.Subuser\.Status', 'Status = sr.Subuser.status'
      $content = $content -replace 'subuser\.Status\s*=', 'subuser.status ='
    $content = $content -replace 'Status\s*=\s*"', 'status = "'
        
        # Replace LastLoginAt property references
   $content = $content -replace '\.LastLoginAt', '.last_login'
        $content = $content -replace 'LastLoginAt\s*=\s*subuser\.last_login', 'LastLoginAt = subuser.last_login'
        
      Set-Content $file -Value $content -NoNewline
        Write-Host "✅ Fixed: $file" -ForegroundColor Green
    } else {
        Write-Host "❌ File not found: $file" -ForegroundColor Red
    }
}

Write-Host "`n🎉 All files processed!" -ForegroundColor Green
Write-Host "Now run: dotnet build" -ForegroundColor Cyan
