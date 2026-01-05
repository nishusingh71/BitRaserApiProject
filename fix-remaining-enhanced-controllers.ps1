# 🔧 FIX REMAINING 3 ENHANCED CONTROLLERS
# This script shows EXACT changes needed for remaining controllers

Write-Host "🔧 ENHANCED CONTROLLERS - REMAINING FIXES" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ ALREADY FIXED:" -ForegroundColor Green
Write-Host "  - EnhancedSessionsController (14 methods updated)" -ForegroundColor Green
Write-Host ""

Write-Host "⏳ NEED TO FIX:" -ForegroundColor Yellow
Write-Host "  1. EnhancedLogsController" -ForegroundColor Yellow
Write-Host "  2. EnhancedCommandsController" -ForegroundColor Yellow
Write-Host "  3. EnhancedMachinesController" -ForegroundColor Yellow
Write-Host ""

Write-Host "📋 STEP-BY-STEP INSTRUCTIONS:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🔨 FOR EACH CONTROLLER, DO THESE 5 CHANGES:" -ForegroundColor Magenta
Write-Host ""

Write-Host "CHANGE 1: Add Import" -ForegroundColor Yellow
Write-Host @"
// Add at top with other using statements:
using BitRaserApiProject.Factories;
"@ -ForegroundColor White
Write-Host ""

Write-Host "CHANGE 2: Update Fields" -ForegroundColor Yellow
Write-Host @"
// Replace:
private readonly ApplicationDbContext _context;

// With:
private readonly DynamicDbContextFactory _contextFactory;
private readonly ILogger<EnhancedXXXController> _logger; // XXX = Controller name
"@ -ForegroundColor White
Write-Host ""

Write-Host "CHANGE 3: Update Constructor" -ForegroundColor Yellow
Write-Host @"
// Replace:
public EnhancedXXXController(
    ApplicationDbContext context,
    ...)
{
    _context = context;
    ...
}

// With:
public EnhancedXXXController(
    DynamicDbContextFactory contextFactory,
    ...,
    ILogger<EnhancedXXXController> logger)
{
    _contextFactory = contextFactory;
    ...
    _logger = logger;
}
"@ -ForegroundColor White
Write-Host ""

Write-Host "CHANGE 4: Update ALL Methods" -ForegroundColor Yellow
Write-Host @"
// In EVERY method, add as FIRST LINE:
using var _context = await _contextFactory.CreateDbContextAsync();

// Example:
[HttpGet]
public async Task<ActionResult<IEnumerable<object>>> GetXXX()
{
    using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADD THIS
    
    // ...rest of existing code unchanged...
}
"@ -ForegroundColor White
Write-Host ""

Write-Host "CHANGE 5: Add Logging to by-email Methods" -ForegroundColor Yellow
Write-Host @"
[HttpGet("by-email/{email}")]
public async Task<ActionResult> GetXXXByEmail(string email)
{
    using var _context = await _contextFactory.CreateDbContextAsync();
    
    _logger.LogInformation("🔍 Fetching XXX for user: {Email}", email); // ✅ ADD THIS
    
    // ...existing query code...
    
    _logger.LogInformation("✅ Found {Count} XXX for user: {Email}", items.Count, email); // ✅ ADD THIS
    
    return Ok(items);
}
"@ -ForegroundColor White
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "🎯 CONTROLLER 1: EnhancedLogsController" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "File:" -ForegroundColor Yellow
Write-Host "  BitRaserApiProject/Controllers/EnhancedLogsController.cs" -ForegroundColor White
Write-Host ""

Write-Host "Methods to Update (~10 methods):" -ForegroundColor Yellow
Write-Host @"
  ✅ Add 'using var _context = ...' to:
  1. GetLogs()
  2. GetLog(int id)
  3. GetLogsByEmail(string email) ← ADD LOGGING
  4. CreateLog()
  5. SearchLogs()
  6. GetLogStatistics()
  7. ExportLogsAsCsv()
  8. DeleteLog(int id)
  9. DeleteOldLogs()
  10. Any helper methods
"@ -ForegroundColor White
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "🎯 CONTROLLER 2: EnhancedCommandsController" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "File:" -ForegroundColor Yellow
Write-Host "BitRaserApiProject/Controllers/EnhancedCommandsController.cs" -ForegroundColor White
Write-Host ""

Write-Host "Methods to Update (~12 methods):" -ForegroundColor Yellow
Write-Host @"
  ✅ Add 'using var _context = ...' to:
  1. GetCommands()
  2. GetCommand(int id)
  3. GetCommandsByEmail(string email) ← ADD LOGGING
  4. CreateCommand()
  5. UpdateCommand(int id)
  6. UpdateCommandStatus(int id)
  7. DeleteCommand(int id)
  8. GetCommandStatistics()
  9. BulkUpdateCommandStatus()
  10. GetCommandHistory(int id)
  11. GetCommandsByStatus(string status)
  12. Any helper methods
"@ -ForegroundColor White
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "🎯 CONTROLLER 3: EnhancedMachinesController" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "File:" -ForegroundColor Yellow
Write-Host "  BitRaserApiProject/Controllers/EnhancedMachinesController.cs" -ForegroundColor White
Write-Host ""

Write-Host "Methods to Update (~15 methods):" -ForegroundColor Yellow
Write-Host @"
  ✅ Add 'using var _context = ...' to:
  1. GetMachines()
  2. GetMachine(string hash)
  3. GetMachineByMac(string mac)
  4. GetMachinesByEmail(string email) ← ADD LOGGING
  5. CreateMachine()
  6. UpdateMachine(string hash)
  7. DeleteMachine(string hash)
  8. GetMachineStatistics()
  9. GetMachinesByStatus(string status)
  10. BulkUpdateMachines()
  11. AssignMachineToUser()
  12. GetUnassignedMachines()
  13. RenewMachineLicense()
  14. GetMachineLicenseStatus()
  15. Any helper methods
"@ -ForegroundColor White
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "✅ AFTER ALL FIXES, RUN BUILD:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "dotnet build" -ForegroundColor Green
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "🧪 TESTING COMMANDS:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Test EnhancedLogs (Private Cloud):" -ForegroundColor Yellow
Write-Host @"
curl -X GET 'http://localhost:4000/api/EnhancedLogs/by-email/devste@gmail.com' \
  -H "Authorization: Bearer {token}"
"@ -ForegroundColor White
Write-Host ""

Write-Host "Test EnhancedCommands (Regular User):" -ForegroundColor Yellow
Write-Host @"
curl -X GET 'http://localhost:4000/api/EnhancedCommands/by-email/regular@example.com' \
  -H "Authorization: Bearer {token}"
"@ -ForegroundColor White
Write-Host ""

Write-Host "Test EnhancedMachines (Private Cloud):" -ForegroundColor Yellow
Write-Host @"
curl -X GET 'http://localhost:4000/api/EnhancedMachines/by-email/devste@gmail.com' \
  -H "Authorization: Bearer {token}"
"@ -ForegroundColor White
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "📊 PROGRESS TRACKER:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ EnhancedSessionsController    [################] 100% DONE" -ForegroundColor Green
Write-Host "⏳ EnhancedLogsController      [     ]   0% TODO" -ForegroundColor Yellow
Write-Host "⏳ EnhancedCommandsController    [ ]   0% TODO" -ForegroundColor Yellow
Write-Host "⏳ EnhancedMachinesController    [          ]   0% TODO" -ForegroundColor Yellow
Write-Host ""
Write-Host "Overall Progress: 25% (1/4 controllers)" -ForegroundColor Cyan
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "🎯 SUMMARY:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Total Controllers: 4" -ForegroundColor White
Write-Host "Fixed: 1 (EnhancedSessionsController)" -ForegroundColor Green
Write-Host "Remaining: 3" -ForegroundColor Yellow
Write-Host "Total Methods to Update: ~37" -ForegroundColor White
Write-Host "Estimated Time: ~40 minutes" -ForegroundColor White
Write-Host ""

Write-Host "🚀 Pattern is PROVEN WORKING - just apply to remaining 3!" -ForegroundColor Green
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "💡 QUICK TIPS:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Open each controller file" -ForegroundColor Yellow
Write-Host "2. Follow the 5-step pattern above" -ForegroundColor Yellow
Write-Host "3. Update constructor first" -ForegroundColor Yellow
Write-Host "4. Add 'using var _context = ...' to EVERY method" -ForegroundColor Yellow
Write-Host "5. Add logging to by-email methods" -ForegroundColor Yellow
Write-Host "6. Build and test" -ForegroundColor Yellow
Write-Host ""

Write-Host "✅ READY TO IMPLEMENT! 🚀" -ForegroundColor Green
Write-Host ""

# Prompt user
Write-Host "Press any key to continue..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
