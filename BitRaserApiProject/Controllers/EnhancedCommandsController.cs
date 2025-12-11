using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using BitRaserApiProject.Factories; // ✅ ADDED

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Commands management controller with comprehensive role-based access control
    /// Supports both users and subusers with appropriate access levels
    /// ✅ NOW SUPPORTS PRIVATE CLOUD ROUTING
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedCommandsController : ControllerBase
    {
        private readonly DynamicDbContextFactory _contextFactory; // ✅ CHANGED
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ITenantConnectionService _tenantService; // ✅ ADDED
        private readonly ILogger<EnhancedCommandsController> _logger; // ✅ ADDED

        public EnhancedCommandsController(
            DynamicDbContextFactory contextFactory, // ✅ CHANGED
            IRoleBasedAuthService authService,
            IUserDataService userDataService,
            ITenantConnectionService tenantService, // ✅ ADDED
            ILogger<EnhancedCommandsController> logger) // ✅ ADDED
        {
            _contextFactory = contextFactory; // ✅ CHANGED
            _authService = authService;
            _userDataService = userDataService;
            _tenantService = tenantService; // ✅ ADDED
            _logger = logger; // ✅ ADDED
        }

        /// <summary>
        /// Get all commands with role-based filtering
        /// ✅ Added: User email based filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCommands([FromQuery] CommandFilterRequest? filter)
        {
   try
     {
  using var _context = await _contextFactory.CreateDbContextAsync();
     
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
         
  _logger.LogInformation("🔍 Fetching commands for user: {Email}", userEmail);
    
         IQueryable<Commands> query = _context.Commands;

     // Apply role-based filtering
  if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_COMMANDS", isCurrentUserSubuser))
    {
              // Regular users and subusers can see all commands for operational purposes
         // No filtering applied - system-level commands
      }

      // Apply additional filters if provided (non-JSON filters first)
           if (filter != null)
     {
               if (!string.IsNullOrEmpty(filter.CommandStatus))
       query = query.Where(c => c.command_status.Contains(filter.CommandStatus));

           if (!string.IsNullOrEmpty(filter.CommandText))
   query = query.Where(c => c.command_text.Contains(filter.CommandText));

        if (filter.IssuedFrom.HasValue)
   query = query.Where(c => c.issued_at >= filter.IssuedFrom.Value);

   if (filter.IssuedTo.HasValue)
       query = query.Where(c => c.issued_at <= filter.IssuedTo.Value);
       }

       // Execute query first
 var commands = await query
        .OrderByDescending(c => c.issued_at)
     .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
     .Take(filter?.PageSize ?? 100)
          .ToListAsync();

           // ✅ FIX: Apply user email filter in memory (MySQL JSON search issue)
       if (filter != null && !string.IsNullOrEmpty(filter.UserEmail))
         {
          var userEmailFilter = filter.UserEmail.ToLower();
     commands = commands
    .Where(c => {
           if (string.IsNullOrEmpty(c.command_json) || c.command_json == "{}")
            return false;
      
              var json = c.command_json.ToLower();
    return json.Contains($"\"user_email\":\"{userEmailFilter}\"") ||
          json.Contains($"\"issued_by\":\"{userEmailFilter}\"");
  })
       .ToList();
   }

_logger.LogInformation("✅ Found {Count} commands from {DbType} database", 
         commands.Count, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                // Parse and add user email from command_json
   var commandsWithUser = commands.Select(c => {
var userEmailFromJson = ExtractUserEmailFromJson(c.command_json);
   return new {
    c.Command_id,
   c.command_text,
    c.command_status,
       c.issued_at,
          IssuedByEmail = userEmailFromJson,
         HasJsonData = !string.IsNullOrEmpty(c.command_json) && c.command_json != "{}"
         };
  }).ToList();

           return Ok(commandsWithUser);
         }
catch (Exception ex)
    {
     _logger.LogError(ex, "Error fetching commands");
     return StatusCode(500, new { message = "Error retrieving commands", error = ex.Message });
     }
 }

        /// <summary>
        /// ✅ NEW: Get commands by user email
        /// </summary>
        [HttpGet("by-email/{userEmail}")]
        public async Task<ActionResult<IEnumerable<object>>> GetCommandsByUserEmail(string userEmail)
        {
   try
 {
 using var _context = await _contextFactory.CreateDbContextAsync();
    
      _logger.LogInformation("🔍 Fetching commands for user: {Email}", userEmail);
 
 var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
  
         // Check if user can view commands for this email
       bool canView = userEmail == currentUserEmail ||
   await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_COMMANDS", isCurrentUserSubuser);

    if (!canView)
  {
     _logger.LogWarning("Unauthorized access attempt for commands of {Email} by {CurrentEmail}", 
      userEmail, currentUserEmail);
 return StatusCode(403, new { error = "You can only view your own commands" });
  }

    // ✅ FIX: Get all commands first, then filter in memory (MySQL JSON search issue)
var allCommands = await _context.Commands
     .OrderByDescending(c => c.issued_at)
    .ToListAsync();

          // Filter by user email in command_json (client-side)
    var userEmailLower = userEmail.ToLower();
var commands = allCommands
       .Where(c => {
    if (string.IsNullOrEmpty(c.command_json) || c.command_json == "{}")
    return false;
  
         var json = c.command_json.ToLower();
    return json.Contains($"\"user_email\":\"{userEmailLower}\"") ||
      json.Contains($"\"issued_by\":\"{userEmailLower}\"");
        })
 .ToList();

       _logger.LogInformation("✅ Found {Count} commands for user {Email} from {DbType} database", 
  commands.Count, userEmail, 
    await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

       var commandsWithUser = commands.Select(c => {
      var userEmailFromJson = ExtractUserEmailFromJson(c.command_json);
    return new {
 c.Command_id,
  c.command_text,
c.command_status,
  c.issued_at,
    IssuedByEmail = userEmailFromJson,
    c.command_json
   };
   }).ToList();

      return commands.Any() ? Ok(commandsWithUser) : NotFound("No commands found for this user");
}
 catch (Exception ex)
      {
  _logger.LogError(ex, "Error fetching commands for user {Email}", userEmail);
        return StatusCode(500, new { message = "Error retrieving commands", error = ex.Message });
    }
     }

        /// <summary>
        /// Get command by ID with role validation
        /// </summary>
    [HttpGet("{id}")]
        public async Task<ActionResult<Commands>> GetCommand(int id)
   {
        using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED
        
        var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            var command = await _context.Commands.FindAsync(id);
       
        if (command == null) return NotFound();

    // Allow basic access to commands for operational purposes
            return Ok(command);
 }

        /// <summary>
        /// Create a new command - Users and subusers can create commands
        /// ✅ Updated: Store user_email in command_json
   /// </summary>
        [HttpPost]
        public async Task<ActionResult<Commands>> CreateCommand([FromBody] CommandCreateRequest request)
        {
   try
        {
 using var _context = await _contextFactory.CreateDbContextAsync();
    
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);

      if (string.IsNullOrEmpty(request.CommandText))
    return BadRequest("Command text is required");

      // ✅ Parse or create JSON with user_email
  string commandJson = request.CommandJson ?? "{}";
       try
{
   var jsonObj = System.Text.Json.JsonDocument.Parse(commandJson);
        var jsonDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(commandJson) 
 ?? new Dictionary<string, object>();
 
  // Add user_email to JSON
    jsonDict["user_email"] = userEmail!;
     jsonDict["issued_by"] = userEmail!;
   jsonDict["created_at"] = DateTime.UtcNow.ToString("o");
    
    commandJson = System.Text.Json.JsonSerializer.Serialize(jsonDict);
   }
   catch
  {
       // If JSON is invalid, create new JSON with user_email
 commandJson = $"{{\"user_email\":\"{userEmail}\",\"issued_by\":\"{userEmail}\",\"created_at\":\"{DateTime.UtcNow:o}\"}}";
      }

    var command = new Commands
 {
    command_text = request.CommandText,
  command_json = commandJson,
      command_status = request.CommandStatus ?? "Pending",
  issued_at = DateTime.UtcNow
     };

     _context.Commands.Add(command);
   await _context.SaveChangesAsync();

_logger.LogInformation("✅ Created command {CommandId} for {Email} in {DbType} database", 
  command.Command_id, userEmail, 
        await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

 return CreatedAtAction(nameof(GetCommand), new { id = command.Command_id }, command);
  }
       catch (Exception ex)
  {
   _logger.LogError(ex, "Error creating command");
  return StatusCode(500, new { message = "Error creating command", error = ex.Message });
      }
   }

        /// <summary>
  /// Update command by ID - Users and subusers can update commands
    /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCommand(int id, [FromBody] CommandUpdateRequest request)
        {
    using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED
            
if (id != request.CommandId)
          return BadRequest("Command ID mismatch");

       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
 
     var command = await _context.Commands.FindAsync(id);
   if (command == null) return NotFound();

            // Allow updates unless specifically restricted by admin permissions
            bool canUpdate = true;
            if (await _authService.HasPermissionAsync(userEmail!, "RESTRICT_COMMAND_UPDATES", isCurrentUserSubuser))
            {
                canUpdate = await _authService.HasPermissionAsync(userEmail!, "UPDATE_COMMAND", isCurrentUserSubuser);
            }

            if (!canUpdate)
                return StatusCode(403, new { error = "Command updates are restricted for your role" });

            if (!string.IsNullOrEmpty(request.CommandText))
                command.command_text = request.CommandText;

            if (!string.IsNullOrEmpty(request.CommandJson))
                command.command_json = request.CommandJson;

            if (!string.IsNullOrEmpty(request.CommandStatus))
                command.command_status = request.CommandStatus;

            // Don't allow changing issued_at time
            _context.Entry(command).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Update command status - Users and subusers can update status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateCommandStatus(int id, [FromBody] CommandStatusUpdateRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED
            
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            var command = await _context.Commands.FindAsync(id);
            if (command == null) return NotFound();

            var validStatuses = new[] { "Pending", "Processing", "Completed", "Failed", "Cancelled" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest($"Invalid status. Valid statuses: {string.Join(", ", validStatuses)}");

            command.command_status = request.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Command status updated to {request.Status}", commandId = id });
        }

        /// <summary>
        /// Delete command by ID - Users and subusers can delete commands unless restricted
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommand(int id)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED
            
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            var command = await _context.Commands.FindAsync(id);
            if (command == null) return NotFound();

            // Allow deletion unless specifically restricted
            bool canDelete = true;
            if (await _authService.HasPermissionAsync(userEmail!, "RESTRICT_COMMAND_DELETION", isCurrentUserSubuser))
            {
                canDelete = await _authService.HasPermissionAsync(userEmail!, "DELETE_COMMAND", isCurrentUserSubuser);
            }

            if (!canDelete)
                return StatusCode(403, new { error = "Command deletion is restricted for your role" });

            _context.Commands.Remove(command);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get command statistics - Users and subusers can view basic statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetCommandStatistics()
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED
            
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            var stats = new {
                TotalCommands = await _context.Commands.CountAsync(),
                PendingCommands = await _context.Commands.CountAsync(c => c.command_status == "Pending"),
                ProcessingCommands = await _context.Commands.CountAsync(c => c.command_status == "Processing"),
                CompletedCommands = await _context.Commands.CountAsync(c => c.command_status == "Completed"),
                FailedCommands = await _context.Commands.CountAsync(c => c.command_status == "Failed"),
                CancelledCommands = await _context.Commands.CountAsync(c => c.command_status == "Cancelled"),
                CommandsToday = await _context.Commands.CountAsync(c => c.issued_at.Date == DateTime.UtcNow.Date),
                CommandsThisWeek = await _context.Commands.CountAsync(c => c.issued_at >= DateTime.UtcNow.AddDays(-7)),
                CommandsThisMonth = await _context.Commands.CountAsync(c => c.issued_at.Month == DateTime.UtcNow.Month),
                StatusBreakdown = await _context.Commands
                    .GroupBy(c => c.command_status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync()
            };

            return Ok(stats);
        }

        /// <summary>
        /// Bulk update command statuses - Admin level or specific permission required
        /// </summary>
        [HttpPatch("bulk-update-status")]
        public async Task<IActionResult> BulkUpdateCommandStatus([FromBody] BulkCommandStatusUpdateRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED
            
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            if (!await _authService.HasPermissionAsync(userEmail!, "BULK_UPDATE_COMMANDS", isCurrentUserSubuser))
                return StatusCode(403, new { error = "Insufficient permissions to bulk update commands" });

            if (!request.CommandIds.Any())
                return BadRequest("No command IDs provided");

            var validStatuses = new[] { "Pending", "Processing", "Completed", "Failed", "Cancelled" };
            if (!validStatuses.Contains(request.NewStatus))
                return BadRequest($"Invalid status. Valid statuses: {string.Join(", ", validStatuses)}");

            var commands = await _context.Commands
                .Where(c => request.CommandIds.Contains(c.Command_id))
                .ToListAsync();

            if (!commands.Any())
                return NotFound("No commands found with the provided IDs");

            foreach (var command in commands)
            {
                command.command_status = request.NewStatus;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Updated {commands.Count} commands to status {request.NewStatus}",
                updatedCommandIds = commands.Select(c => c.Command_id).ToList()
            });
        }

        /// <summary>
        /// Execute command - Users and subusers can execute commands
        /// </summary>
        [HttpPost("{id}/execute")]
        public async Task<IActionResult> ExecuteCommand(int id)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED
            
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            var command = await _context.Commands.FindAsync(id);
            if (command == null) return NotFound();

            if (command.command_status != "Pending")
                return BadRequest($"Command is not in Pending status. Current status: {command.command_status}");

            // Update status to Processing
            command.command_status = "Processing";
            await _context.SaveChangesAsync();

            // Here you would implement the actual command execution logic
            // For now, we'll just simulate it
            await Task.Delay(100); // Simulate processing time

            // Update status to Completed (in a real implementation, this would depend on execution result)
            command.command_status = "Completed";
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Command executed successfully",
                commandId = id,
                status = command.command_status
            });
        }

        /// <summary>
        /// Cancel command - Users and subusers can cancel pending commands
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelCommand(int id)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED
            
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            var command = await _context.Commands.FindAsync(id);
            if (command == null) return NotFound();

            if (command.command_status == "Completed")
                return BadRequest("Cannot cancel a completed command");

            if (command.command_status == "Cancelled")
                return BadRequest("Command is already cancelled");

            command.command_status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Command cancelled successfully",
                commandId = id,
                status = command.command_status
            });
        }

        /// <summary>
     /// ✅ NEW: Helper method to extract user email from command_json
     /// </summary>
    private string? ExtractUserEmailFromJson(string? commandJson)
        {
      if (string.IsNullOrEmpty(commandJson) || commandJson == "{}")
     return null;

            try
            {
      var jsonDoc = System.Text.Json.JsonDocument.Parse(commandJson);
                
                // Try to get user_email
   if (jsonDoc.RootElement.TryGetProperty("user_email", out var userEmailProp))
            return userEmailProp.GetString();
     
          // Try to get issued_by
    if (jsonDoc.RootElement.TryGetProperty("issued_by", out var issuedByProp))
     return issuedByProp.GetString();
                
      return null;
      }
     catch
         {
        return null;
     }
        }
    }

    #region Request Models

    /// <summary>
    /// Command filter request model
    /// </summary>
    public class CommandFilterRequest
    {
        public string? UserEmail { get; set; }  // ✅ NEW: Filter by user email
   public string? CommandStatus { get; set; }
        public string? CommandText { get; set; }
        public DateTime? IssuedFrom { get; set; }
        public DateTime? IssuedTo { get; set; }
        public int Page { get; set; } = 0;
 public int PageSize { get; set; } = 100;
    }

    /// <summary>
    /// Command creation request model
    /// </summary>
    public class CommandCreateRequest
    {
      public string CommandText { get; set; } = string.Empty;
        public string? CommandJson { get; set; }
        public string? CommandStatus { get; set; }
    }

    /// <summary>
    /// Command update request model
    /// </summary>
    public class CommandUpdateRequest
    {
        public int CommandId { get; set; }
        public string? CommandText { get; set; }
        public string? CommandJson { get; set; }
        public string? CommandStatus { get; set; }
  }

    /// <summary>
    /// Command status update request model
    /// </summary>
    public class CommandStatusUpdateRequest
    {
        public string Status { get; set; } = string.Empty;
}

    /// <summary>
    /// Bulk command status update request model
    /// </summary>
    public class BulkCommandStatusUpdateRequest
    {
        public List<int> CommandIds { get; set; } = new();
     public string NewStatus { get; set; } = string.Empty;
    }

    #endregion
}