# Rename AllTableController.cs to avoid conflicts
# Since enhanced versions of controllers already exist

Write-Host "🔧 Fixing duplicate controller errors..." -ForegroundColor Cyan

$oldFile = "BitRaserApiProject\Controllers\AllTableController.cs"
$newFile = "BitRaserApiProject\Controllers\AllTableController.cs.backup"

if (Test-Path $oldFile) {
    Write-Host "📝 Renaming AllTableController.cs to .backup..." -ForegroundColor Yellow
    Move-Item -Path $oldFile -Destination $newFile -Force
    Write-Host "✅ File renamed successfully!" -ForegroundColor Green
    Write-Host "ℹ️  Original file backed up as AllTableController.cs.backup" -ForegroundColor Cyan
} else {
    Write-Host "❌ File not found: $oldFile" -ForegroundColor Red
}

Write-Host "`n🎉 Fix complete! Now run: dotnet run" -ForegroundColor Green
