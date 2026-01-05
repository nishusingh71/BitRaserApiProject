# PowerShell Script to Run Application with Detailed Logging
# Save this file as: run-with-logs.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BitRaser API - Private Cloud Setup  " -ForegroundColor Cyan
Write-Host "    Detailed Logging Enabled  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to project directory
$projectPath = "BitRaserApiProject"
if (Test-Path $projectPath) {
    Set-Location $projectPath
    Write-Host "✅ Project directory found" -ForegroundColor Green
} else {
    Write-Host "❌ Project directory not found. Please run this from solution root." -ForegroundColor Red
    exit 1
}

# Clean build
Write-Host "`n🔨 Cleaning previous build..." -ForegroundColor Yellow
Remove-Item -Path "bin", "obj" -Recurse -Force -ErrorAction SilentlyContinue

# Build project
Write-Host "🔧 Building project..." -ForegroundColor Yellow
dotnet build --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful`n" -ForegroundColor Green

# Set environment variable for detailed logging
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Logging__LogLevel__Default = "Debug"
$env:Logging__LogLevel__BitRaserApiProject = "Debug"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Application Starting...              " -ForegroundColor Cyan
Write-Host "  Watch for Private Cloud Setup logs   " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Run application with output capture
Write-Host "📝 Application Logs:" -ForegroundColor Yellow
Write-Host "---" -ForegroundColor Gray

# Run and capture output
dotnet run --no-build | Tee-Object -FilePath "application-logs.txt"
