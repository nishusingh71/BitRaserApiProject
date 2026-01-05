# 🚨 EMERGENCY FILE RESTORE SCRIPT

Write-Host "🔧 Restoring PrivateCloudService.cs from Git..." -ForegroundColor Yellow

# Navigate to project directory
cd "C:\Users\nishu\Downloads\BitRaserApiProject\BitRaserApiProject\BitRaserApiProject"

# Check if file exists in Git history
$fileExists = git ls-files "BitRaserApiProject\Services\PrivateCloudService.cs"

if ($fileExists) {
    Write-Host "✅ File found in Git history" -ForegroundColor Green
    
    # Show file status
    Write-Host "`n📊 Current file status:" -ForegroundColor Cyan
    git status "BitRaserApiProject\Services\PrivateCloudService.cs"
    
    # Restore from last commit
    Write-Host "`n🔄 Restoring from last commit..." -ForegroundColor Yellow
    git checkout HEAD -- "BitRaserApiProject\Services\PrivateCloudService.cs"
    
    Write-Host "✅ File restored successfully!" -ForegroundColor Green
    
    # Verify restore
    Write-Host "`n✔️ Verification:" -ForegroundColor Cyan
    if (Test-Path "BitRaserApiProject\Services\PrivateCloudService.cs") {
        $fileSize = (Get-Item "BitRaserApiProject\Services\PrivateCloudService.cs").Length
        Write-Host "   File size: $fileSize bytes" -ForegroundColor Green
        Write-Host "   File exists: YES" -ForegroundColor Green
        
        # Try to build
        Write-Host "`n🔨 Testing build..." -ForegroundColor Yellow
        dotnet build --no-incremental
        
        if ($LASTEXITCODE -eq 0) {
       Write-Host "`n✅ BUILD SUCCESS! File is working." -ForegroundColor Green
        } else {
    Write-Host "`n⚠️ Build failed, but file was restored." -ForegroundColor Yellow
    Write-Host "   You may need to apply the Base-64 fix again." -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ❌ File still missing!" -ForegroundColor Red
    }
} else {
    Write-Host "❌ File not found in Git history" -ForegroundColor Red
  Write-Host "💡 Alternative: Manually copy from backup or recreate" -ForegroundColor Yellow
}

Write-Host "`n📋 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. If restore worked: Apply Base-64 fix from BASE64-ENCRYPTION-ERROR-QUICK-FIX.md"
Write-Host "   2. Test with: dotnet build"
Write-Host "   3. If still errors: Check COMPLETE-SETUP-SELECTED-TABLES-ADDED.md for reference"

Write-Host "`n✅ Script complete!" -ForegroundColor Green
