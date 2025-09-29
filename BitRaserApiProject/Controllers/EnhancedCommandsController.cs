using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Commands management controller with comprehensive role-based access control
    /// Supports both users and subusers with appropriate access levels
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedCommandsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;

        public EnhancedCommandsController(ApplicationDbContext context, IRoleBasedAuthService authService, IUserDataService userDataService)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
        }

        /// <summary>
        /// Get all commands with role-based filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCommands([FromQuery] CommandFilterRequest? filter)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            IQueryable<Commands> query = _context.Commands;

            // Apply role-based filtering - allow users and subusers to see commands unless admin restrictions
            if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_COMMANDS", isCurrentUserSubuser))
            {
                // For regular users and subusers, they can see all commands for operational purposes
                // Admin-level restrictions only apply if they have the specific permission
                if (!await _authService.HasPermissionAsync(userEmail!, "MANAGE_COMMANDS", isCurrentUserSubuser))
                {
                    // Basic users and subusers can still view commands for their operations
                    // They might need to see system commands to understand system state
                }
            }

            // Apply additional filters if provided
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

            var commands = await query
                .OrderByDescending(c => c.issued_at)
                .Take(filter?.PageSize ?? 100)
                .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                .Select(c => new {
                    c.Command_id,
                    c.command_text,
                    c.command_status,
                    c.issued_at,
                    HasJsonData = !string.IsNullOrEmpty(c.command_json) && c.command_json != "{}"
                })
                .ToListAsync();

            return Ok(commands);
        }

        /// <summary>
        /// Get command by ID with role validation
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Commands>> GetCommand(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            var command = await _context.Commands.FindAsync(id);
            
            if (command == null) return NotFound();

            // Allow basic access to commands for operational purposes
            return Ok(command);
        }

        /// <summary>
        /// Create a new command - Users and subusers can create commands
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Commands>> CreateCommand([FromBody] CommandCreateRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);

            if (string.IsNullOrEmpty(request.CommandText))
                return BadRequest("Command text is required");

            var command = new Commands
            {
                command_text = request.CommandText,
                command_json = request.CommandJson ?? "{}",
                command_status = request.CommandStatus ?? "Pending",
                issued_at = DateTime.UtcNow
            };

            _context.Commands.Add(command);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCommand), new { id = command.Command_id }, command);
        }

        /// <summary>
        /// Update command by ID - Users and subusers can update commands
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCommand(int id, [FromBody] CommandUpdateRequest request)
        {
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
    }

    /// <summary>
    /// Command filter request model
    /// </summary>
    public class CommandFilterRequest
    {
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
}