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
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedCommandsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;

        public EnhancedCommandsController(ApplicationDbContext context, IRoleBasedAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        /// <summary>
        /// Get all commands with role-based filtering
        /// </summary>
        [HttpGet]
        [RequirePermission("READ_ALL_COMMANDS")]
        public async Task<ActionResult<IEnumerable<object>>> GetCommands([FromQuery] CommandFilterRequest? filter)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            IQueryable<Commands> query = _context.Commands;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_COMMANDS"))
            {
                // If user doesn't have permission to see all commands, apply restrictions
                // For now, let managers and above see all commands
                if (!await _authService.HasPermissionAsync(userEmail!, "MANAGE_COMMANDS"))
                {
                    return StatusCode(403,new { error = "Insufficient permissions to view commands" });
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
        [RequirePermission("READ_COMMAND")]
        public async Task<ActionResult<Commands>> GetCommand(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!await _authService.HasPermissionAsync(userEmail!, "READ_COMMAND"))
                return StatusCode(403,new { error = "Insufficient permissions to view commands" });

            var command = await _context.Commands.FindAsync(id);
            
            if (command == null) return NotFound();

            return Ok(command);
        }

        /// <summary>
        /// Create a new command - Manager level access required
        /// </summary>
        [HttpPost]
        [RequirePermission("CREATE_COMMAND")]
        public async Task<ActionResult<Commands>> CreateCommand([FromBody] CommandCreateRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!await _authService.HasPermissionAsync(userEmail!, "CREATE_COMMAND"))
                return StatusCode(403,new { error = "Insufficient permissions to create commands" });

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
        /// Update command by ID - Admin level access required
        /// </summary>
        [HttpPut("{id}")]
        [RequirePermission("UPDATE_COMMAND")]
        public async Task<IActionResult> UpdateCommand(int id, [FromBody] CommandUpdateRequest request)
        {
            if (id != request.CommandId)
                return BadRequest("Command ID mismatch");

            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!await _authService.HasPermissionAsync(userEmail!, "UPDATE_COMMAND"))
                return StatusCode(403,new { error = "Insufficient permissions to update commands" });

            var command = await _context.Commands.FindAsync(id);
            if (command == null) return NotFound();

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
        /// Update command status - Support level access
        /// </summary>
        [HttpPatch("{id}/status")]
        [RequirePermission("UPDATE_COMMAND_STATUS")]
        public async Task<IActionResult> UpdateCommandStatus(int id, [FromBody] CommandStatusUpdateRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!await _authService.HasPermissionAsync(userEmail!, "UPDATE_COMMAND_STATUS"))
                return StatusCode(403,new { error = "Insufficient permissions to update command status" });

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
        /// Delete command by ID - Admin only
        /// </summary>
        [HttpDelete("{id}")]
        [RequirePermission("DELETE_COMMAND")]
        public async Task<IActionResult> DeleteCommand(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!await _authService.HasPermissionAsync(userEmail!, "DELETE_COMMAND"))
                return StatusCode(403,new { error = "Insufficient permissions to delete commands" });

            var command = await _context.Commands.FindAsync(id);
            if (command == null) return NotFound();

            _context.Commands.Remove(command);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get command statistics - Manager level access
        /// </summary>
        [HttpGet("statistics")]
        [RequirePermission("READ_COMMAND_STATISTICS")]
        public async Task<ActionResult<object>> GetCommandStatistics()
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!await _authService.HasPermissionAsync(userEmail!, "READ_COMMAND_STATISTICS"))
                return StatusCode(403,new { error = "Insufficient permissions to view command statistics" });

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
        /// Bulk update command statuses - Admin only
        /// </summary>
        [HttpPatch("bulk-update-status")]
        [RequirePermission("BULK_UPDATE_COMMANDS")]
        public async Task<IActionResult> BulkUpdateCommandStatus([FromBody] BulkCommandStatusUpdateRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!await _authService.HasPermissionAsync(userEmail!, "BULK_UPDATE_COMMANDS"))
                return StatusCode(403,new { error = "Insufficient permissions to bulk update commands" });

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