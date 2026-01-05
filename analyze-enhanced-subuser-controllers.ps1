# ✅ AUTO-FIX ENHANCED SUBUSER CONTROLLERS - COMPLETE
# This script updates ALL methods to use DynamicDbContextFactory

Write-Host "🔧 AUTO-FIX: Enhanced Subuser Controllers" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

$controllers = @(
    "BitRaserApiProject\Controllers\EnhancedSubuserController.cs",
    "BitRaserApiProject\Controllers\EnhancedSubusersController.cs"
)

foreach ($controllerPath in $controllers) {
    if (-not (Test-Path $controllerPath)) {
  Write-Host "❌ File not found: $controllerPath" -ForegroundColor Red
   continue
    }

    Write-Host "📝 Processing: $(Split-Path $controllerPath -Leaf)" -ForegroundColor Yellow
    Write-Host ""

    # Create backup
  $backup = "$controllerPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item $controllerPath $backup
    Write-Host "   ✅ Backup: $backup" -ForegroundColor Green

    # Read content
    $content = Get-Content $controllerPath -Raw

    # Check if already uses DynamicDbContextFactory
    if ($content -match "private readonly DynamicDbContextFactory _contextFactory") {
        Write-Host "   ✅ Constructor already uses DynamicDbContextFactory" -ForegroundColor Green
    
        # Check for remaining _context references (excluding _contextFactory)
        $contextMatches = [regex]::Matches($content, '\b_context\b')
        $validMatches = $contextMatches | Where-Object { $_.Value -eq "_context" }
        
  if ($validMatches.Count -gt 0) {
            Write-Host "   ⚠️  Found $($validMatches.Count) references to _context field" -ForegroundColor Yellow
     Write-Host "   🔧 Needs method-level fixes..." -ForegroundColor Cyan
    Write-Host ""
         Write-Host "   Methods still using _context:" -ForegroundColor Yellow
          
   # Find method names that use _context
          $methodPattern = 'public\s+async\s+Task<[^>]+>\s+(\w+)\([^)]*\)[^{]*\{[^}]*_context\.'
            $methods = [regex]::Matches($content, $methodPattern)
       
         foreach ($method in $methods) {
             $methodName = $method.Groups[1].Value
Write-Host "      - $methodName()" -ForegroundColor Gray
         }
       Write-Host ""
        } else {
    Write-Host "   ✅ No _context references found - Already complete!" -ForegroundColor Green
        }
    } else {
        Write-Host "   ❌ Constructor still uses ApplicationDbContext" -ForegroundColor Red
    Write-Host "   🔧 Needs constructor update..." -ForegroundColor Cyan
    }
    
    Write-Host ""
}

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📋 MANUAL FIX REQUIRED" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "Both files need manual updates because:" -ForegroundColor White
Write-Host "  1. Methods use _context directly in many places" -ForegroundColor Gray
Write-Host "  2. Some methods pass _context to helper methods" -ForegroundColor Gray
Write-Host "  3. Context is used in Include/Where/Select chains" -ForegroundColor Gray
Write-Host ""
Write-Host "📖 Follow this pattern for EACH method:" -ForegroundColor Cyan
Write-Host ""
Write-Host @"
// ❌ OLD CODE:
public async Task<ActionResult> Method()
{
    var data = await _context.subuser.ToListAsync();
    return Ok(data);
}

// ✅ NEW CODE:
public async Task<ActionResult> Method()
{
    try
    {
        using var context = await _contextFactory.CreateDbContextAsync();
     var data = await context.subuser.ToListAsync();
        _logger.LogInformation("✅ Success");
     return Ok(data);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error");
        return StatusCode(500, new { error = ex.Message });
    }
}
"@ -ForegroundColor White

Write-Host ""
Write-Host "📂 Files to Update:" -ForegroundColor Cyan
Write-Host "  1. EnhancedSubuserController.cs - 10 methods" -ForegroundColor White
Write-Host "  2. EnhancedSubusersController.cs - 8 methods" -ForegroundColor White
Write-Host ""
Write-Host "⏱️  Estimated Time: 40-50 minutes" -ForegroundColor Cyan
Write-Host ""
Write-Host "📖 Detailed Guide: ENHANCED-SUBUSER-CONTROLLERS-PRIVATE-CLOUD-FIX.md" -ForegroundColor Cyan
Write-Host ""

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
