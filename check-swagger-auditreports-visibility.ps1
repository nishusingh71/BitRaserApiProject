# 🔍 Check Swagger Visibility for EnhancedAuditReports Controller
# Run this script to diagnose why controller is not visible in Swagger

Write-Host ""
Write-Host "🔍 SWAGGER VISIBILITY DIAGNOSTIC TOOL" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if controller file exists
Write-Host "📂 Step 1: Checking controller file..." -ForegroundColor Yellow
$controllerPath = "BitRaserApiProject\Controllers\EnhancedAuditReportsController.cs"

if (Test-Path $controllerPath) {
    Write-Host "✅ Controller file found at: $controllerPath" -ForegroundColor Green
    
    # Check file size
    $fileSize = (Get-Item $controllerPath).Length
    Write-Host "   File size: $fileSize bytes" -ForegroundColor Gray
    
    # Check if file contains key attributes
    $content = Get-Content $controllerPath -Raw
    
    if ($content -match '\[ApiController\]') {
        Write-Host "✅ [ApiController] attribute found" -ForegroundColor Green
    } else {
     Write-Host "❌ [ApiController] attribute MISSING" -ForegroundColor Red
    }
    
    if ($content -match '\[Route\(') {
        Write-Host "✅ [Route] attribute found" -ForegroundColor Green
    } else {
        Write-Host "❌ [Route] attribute MISSING" -ForegroundColor Red
    }
    
    if ($content -match 'class\s+EnhancedAuditReportsController\s*:\s*ControllerBase') {
        Write-Host "✅ Inherits from ControllerBase" -ForegroundColor Green
    } else {
        Write-Host "⚠️ May not inherit from ControllerBase correctly" -ForegroundColor Yellow
    }
    
} else {
    Write-Host "❌ Controller file NOT FOUND at: $controllerPath" -ForegroundColor Red
    Write-Host "   Please check if file exists at correct location" -ForegroundColor Yellow
exit 1
}

Write-Host ""

# Step 2: Build project
Write-Host "🔨 Step 2: Building project..." -ForegroundColor Yellow
$buildOutput = dotnet build --no-incremental 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build SUCCESSFUL" -ForegroundColor Green
} else {
  Write-Host "❌ Build FAILED!" -ForegroundColor Red
    Write-Host "   Build output:" -ForegroundColor Yellow
    Write-Host $buildOutput -ForegroundColor Gray
    Write-Host ""
    Write-Host "   Fix compilation errors and run this script again" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Step 3: Check if API is running
Write-Host "🌐 Step 3: Checking if API is running..." -ForegroundColor Yellow

try {
    # Try to connect to Swagger JSON endpoint
    $swaggerUrl = "http://localhost:4000/swagger/v1/swagger.json"
    Write-Host "   Connecting to: $swaggerUrl" -ForegroundColor Gray
    
    $response = Invoke-WebRequest -Uri $swaggerUrl -Method Get -TimeoutSec 5
    Write-Host "✅ API is running and accessible" -ForegroundColor Green
    
    # Parse JSON and search for EnhancedAuditReports
    Write-Host ""
    Write-Host "🔍 Step 4: Searching for EnhancedAuditReports in Swagger..." -ForegroundColor Yellow

$json = $response.Content | ConvertFrom-Json
 $foundPaths = @()
    
    foreach ($path in $json.paths.PSObject.Properties.Name) {
        if ($path -like "*EnhancedAuditReports*") {
            $foundPaths += $path
        }
    }
 
    if ($foundPaths.Count -gt 0) {
   Write-Host "✅ EnhancedAuditReports FOUND in Swagger JSON!" -ForegroundColor Green
        Write-Host "   Found $($foundPaths.Count) endpoints:" -ForegroundColor Gray
  foreach ($path in $foundPaths) {
    Write-Host "   ✓ $path" -ForegroundColor Green
 }
        
        Write-Host ""
        Write-Host "📊 DIAGNOSIS:" -ForegroundColor Cyan
        Write-Host "   Controller IS registered and visible in Swagger JSON" -ForegroundColor White
        Write-Host "   If not visible in browser, it's a BROWSER CACHE issue" -ForegroundColor White
        Write-Host ""
    Write-Host "🔧 SOLUTION:" -ForegroundColor Yellow
    Write-Host "   1. Hard refresh browser: Ctrl + Shift + R" -ForegroundColor White
        Write-Host "   2. Or open in Incognito/Private mode" -ForegroundColor White
        Write-Host "   3. Clear browser cache completely" -ForegroundColor White
        
    } else {
        Write-Host "❌ EnhancedAuditReports NOT FOUND in Swagger JSON" -ForegroundColor Red
        Write-Host ""
        Write-Host "📋 Available controllers in Swagger:" -ForegroundColor Yellow
        
    $allControllers = @{}
        foreach ($path in $json.paths.PSObject.Properties.Name) {
       if ($path -match '/api/([^/]+)') {
                $controller = $Matches[1]
         if (-not $allControllers.ContainsKey($controller)) {
          $allControllers[$controller] = @()
      }
        $allControllers[$controller] += $path
         }
  }
        
        foreach ($controller in $allControllers.Keys | Sort-Object) {
Write-Host "   📌 $controller ($($allControllers[$controller].Count) endpoints)" -ForegroundColor Gray
        }

  Write-Host ""
    Write-Host "🔧 TROUBLESHOOTING:" -ForegroundColor Yellow
        Write-Host "   1. Check if controller class name matches file name" -ForegroundColor White
  Write-Host "   2. Verify [ApiController] and [Route] attributes" -ForegroundColor White
    Write-Host "   3. Ensure controller inherits from ControllerBase" -ForegroundColor White
    Write-Host "   4. Check for compilation errors in controller" -ForegroundColor White
      Write-Host "   5. Try: dotnet clean && dotnet build" -ForegroundColor White
    }
    
} catch {
    Write-Host "❌ API is NOT running or not accessible" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔧 SOLUTION:" -ForegroundColor Yellow
    Write-Host "   1. Start the API: dotnet run" -ForegroundColor White
    Write-Host "   2. Wait for 'Application started' message" -ForegroundColor White
    Write-Host "   3. Run this script again" -ForegroundColor White
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "🏁 Diagnostic Complete!" -ForegroundColor Cyan
Write-Host ""
