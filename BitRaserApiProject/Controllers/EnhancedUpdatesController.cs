using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using System.Text;
using System.Security.Cryptography;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Updates Controller with comprehensive update management
    /// Supports both users and subusers with role-based access control
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedUpdatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<EnhancedUpdatesController> _logger;
        private readonly ICacheService _cacheService;

        public EnhancedUpdatesController(
            ApplicationDbContext context, 
            IRoleBasedAuthService authService,
            IUserDataService userDataService,
            ILogger<EnhancedUpdatesController> logger,
            ICacheService cacheService)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
            _logger = logger;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Get all updates with role-based filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllUpdates([FromQuery] UpdateFilterRequest? filter)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            IQueryable<Update> query = _context.Updates;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_UPDATES", isCurrentUserSubuser))
            {
                // Non-admin users can only see active updates
                query = query.Where(u => u.is_mandatory_update || u.release_date >= DateTime.UtcNow.AddMonths(-6));
            }

            // Apply additional filters if provided
            if (filter != null)
            {
                if (filter.ReleaseDateFrom.HasValue)
                    query = query.Where(u => u.release_date >= filter.ReleaseDateFrom.Value);

                if (filter.ReleaseDateTo.HasValue)
                    query = query.Where(u => u.release_date <= filter.ReleaseDateTo.Value);

                if (filter.IsMandatory.HasValue)
                    query = query.Where(u => u.is_mandatory_update == filter.IsMandatory.Value);
            }

            var updates = await query
                .OrderByDescending(u => u.release_date)
                .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 50))
                .Take(filter?.PageSize ?? 50)
                .ToListAsync();

            var userCanManageAllUpdates = await _authService.HasPermissionAsync(currentUserEmail!, "MANAGE_ALL_UPDATES", isCurrentUserSubuser);
            var userCanDeleteUpdates = await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_UPDATES", isCurrentUserSubuser);

            var updatesList = updates.Select(u => new {
                u.version_id,
                u.version_number,
                u.changelog,
                u.download_link,
                u.release_date,
                u.is_mandatory_update,
                can_edit = userCanManageAllUpdates,
                can_delete = userCanDeleteUpdates
            }).ToList();

            return Ok(new {
                updates = updatesList,
                pagination = new {
                    page = filter?.Page ?? 0,
                    pageSize = filter?.PageSize ?? 50,
                    totalCount = await query.CountAsync()
                },
                userContext = new {
                    email = currentUserEmail,
                    userType = isCurrentUserSubuser ? "Subuser" : "User",
                    canCreateUpdates = await _authService.HasPermissionAsync(currentUserEmail!, "CREATE_UPDATES", isCurrentUserSubuser),
                    canManageAllUpdates = await _authService.HasPermissionAsync(currentUserEmail!, "MANAGE_ALL_UPDATES", isCurrentUserSubuser)
                }
            });
        }

        /// <summary>
        /// Get latest available update (supports anonymous access for client checks)
        /// </summary>
        [HttpGet("latest")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetLatestUpdate([FromQuery] string? currentVersion = null)
        {
            var latestUpdate = await _context.Updates
                .OrderByDescending(u => u.release_date)
                .FirstOrDefaultAsync();

            if (latestUpdate == null)
                return NotFound(new { message = "No updates available" });

            // Check if this is actually newer than current version
            bool isNewerVersion = true;
            if (!string.IsNullOrEmpty(currentVersion))
            {
                isNewerVersion = CompareVersions(latestUpdate.version_number, currentVersion) > 0;
            }

            var response = new {
                version_id = latestUpdate.version_id,
                version_number = latestUpdate.version_number,
                changelog = latestUpdate.changelog,
                download_link = latestUpdate.download_link,
                release_date = latestUpdate.release_date,
                is_mandatory_update = latestUpdate.is_mandatory_update,
                is_newer_version = isNewerVersion,
                current_version_provided = currentVersion
            };

            return Ok(response);
        }

        /// <summary>
        /// Get specific update by version ID
        /// </summary>
        [HttpGet("{versionId}")]
        public async Task<ActionResult<object>> GetUpdate(int versionId)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            var update = await _context.Updates.Where(u => u.version_id == versionId).FirstOrDefaultAsync();
            
            if (update == null)
                return NotFound(new { message = $"Update with version ID {versionId} not found" });

            // Check access permissions
            var canViewDetails = await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_UPDATES", isCurrentUserSubuser) ||
                                await _authService.HasPermissionAsync(currentUserEmail!, "READ_UPDATE", isCurrentUserSubuser);

            if (!canViewDetails)
            {
                return StatusCode(403, new { error = "You don't have permission to view this update" });
            }

            var response = new {
                update.version_id,
                update.version_number,
                update.changelog,
                update.download_link,
                update.release_date,
                update.is_mandatory_update,
                can_edit = await _authService.HasPermissionAsync(currentUserEmail!, "MANAGE_ALL_UPDATES", isCurrentUserSubuser),
                can_delete = await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_UPDATES", isCurrentUserSubuser)
            };

            return Ok(response);
        }

        /// <summary>
        /// Check for updates newer than specified version (supports anonymous access)
        /// </summary>
        [HttpGet("check/{currentVersionId}")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> CheckForUpdates(int currentVersionId)
        {
            var updates = await _context.Updates
                .Where(u => u.version_id > currentVersionId)
                .OrderBy(u => u.version_id)
                .Select(u => new {
                    u.version_id,
                    u.version_number,
                    u.changelog,
                    u.download_link,
                    u.release_date,
                    u.is_mandatory_update
                })
                .ToListAsync();

            var response = new {
                current_version_id = currentVersionId,
                updates_available = updates.Count > 0,
                total_updates = updates.Count,
                mandatory_updates = updates.Count(u => u.is_mandatory_update),
                updates = updates,
                checked_at = DateTime.UtcNow
            };

            return Ok(response);
        }

        /// <summary>
        /// Create a new update (Admin/Manager only)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> CreateUpdate([FromBody] CreateUpdateRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "CREATE_UPDATES", isCurrentUserSubuser))
            {
                return StatusCode(403, new { error = "You don't have permission to create updates" });
            }

            // Validate version number format
            if (!IsValidVersionNumber(request.VersionNumber))
            {
                return BadRequest(new { error = "Invalid version number format. Use semantic versioning (e.g., 1.0.0)" });
            }

            // Check if version already exists
            var existingVersion = await _context.Updates
                .Where(u => u.version_number == request.VersionNumber).FirstOrDefaultAsync();
            
            if (existingVersion != null)
                return Conflict(new { error = $"Version {request.VersionNumber} already exists" });

            var newUpdate = new Update
            {
                version_number = request.VersionNumber,
                changelog = request.Changelog,
                download_link = request.DownloadLink,
                release_date = request.ReleaseDate ?? DateTime.UtcNow,
                is_mandatory_update = request.IsMandatoryUpdate
            };

            _context.Updates.Add(newUpdate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Update {Version} created by {User}", newUpdate.version_number, currentUserEmail);

            var response = new {
                version_id = newUpdate.version_id,
                version_number = newUpdate.version_number,
                created_by = currentUserEmail,
                message = "Update created successfully"
            };

            return CreatedAtAction(nameof(GetUpdate), new { versionId = newUpdate.version_id }, response);
        }

        /// <summary>
        /// Update an existing update
        /// </summary>
        [HttpPut("{versionId}")]
        public async Task<IActionResult> UpdateUpdate(int versionId, [FromBody] UpdateUpdateRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            var update = await _context.Updates.Where(u => u.version_id == versionId).FirstOrDefaultAsync();
            
            if (update == null)
                return NotFound(new { message = $"Update with version ID {versionId} not found" });

            // Check permissions
            var canEdit = await _authService.HasPermissionAsync(currentUserEmail!, "MANAGE_ALL_UPDATES", isCurrentUserSubuser) ||
                         await _authService.HasPermissionAsync(currentUserEmail!, "UPDATE_UPDATES", isCurrentUserSubuser);

            if (!canEdit)
            {
                return StatusCode(403, new { error = "You don't have permission to edit updates" });
            }

            // Update fields
            if (!string.IsNullOrEmpty(request.Changelog))
                update.changelog = request.Changelog;

            if (!string.IsNullOrEmpty(request.DownloadLink))
                update.download_link = request.DownloadLink;

            if (request.IsMandatoryUpdate.HasValue)
                update.is_mandatory_update = request.IsMandatoryUpdate.Value;

            _context.Entry(update).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Update {Version} modified by {User}", update.version_number, currentUserEmail);

            return Ok(new { 
                message = "Update modified successfully", 
                version_number = update.version_number,
                updated_by = currentUserEmail
            });
        }

        /// <summary>
        /// Delete an update (Admin only)
        /// </summary>
        [HttpDelete("{versionId}")]
        public async Task<IActionResult> DeleteUpdate(int versionId)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            var update = await _context.Updates.Where(u => u.version_id == versionId).FirstOrDefaultAsync();
            
            if (update == null)
                return NotFound(new { message = $"Update with version ID {versionId} not found" });

            // Check permissions
            var canDelete = await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_UPDATES", isCurrentUserSubuser) ||
                           await _authService.HasPermissionAsync(currentUserEmail!, "MANAGE_ALL_UPDATES", isCurrentUserSubuser);

            if (!canDelete)
            {
                return StatusCode(403, new { error = "You don't have permission to delete updates" });
            }

            _context.Updates.Remove(update);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Update {Version} deleted by {User}", update.version_number, currentUserEmail);

            return Ok(new { 
                message = "Update deleted successfully", 
                version_number = update.version_number,
                deleted_by = currentUserEmail,
                deleted_at = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get update statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetUpdateStatistics()
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "VIEW_UPDATE_STATISTICS", isCurrentUserSubuser))
            {
                return StatusCode(403, new { error = "You don't have permission to view update statistics" });
            }

            var totalUpdates = await _context.Updates.CountAsync();
            var mandatoryUpdates = await _context.Updates.CountAsync(u => u.is_mandatory_update);

            // ✅ CACHE: Update statistics with short TTL
            var cacheKey = $"updates:stats:{currentUserEmail}";
            var statistics = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                var recentUpdates = await _context.Updates
                    .Where(u => u.release_date >= DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(u => u.release_date)
                    .Take(5)
                    .Select(u => new {
                        u.version_number,
                        u.release_date,
                        u.is_mandatory_update
                    })
                    .ToListAsync();

                return new {
                    overview = new {
                        totalUpdates,
                        mandatoryUpdates,
                        optionalUpdates = totalUpdates - mandatoryUpdates
                    },
                    recentActivity = new {
                        updatesLast30Days = await _context.Updates.CountAsync(u => u.release_date >= DateTime.UtcNow.AddDays(-30)),
                        updatesLast7Days = await _context.Updates.CountAsync(u => u.release_date >= DateTime.UtcNow.AddDays(-7)),
                        recentUpdates = recentUpdates
                    },
                    generatedAt = DateTime.UtcNow,
                    generatedBy = currentUserEmail
                };
            }, CacheService.CacheTTL.Short);

            return Ok(statistics);
        }

        /// <summary>
        /// Download update information (tracks download statistics)
        /// </summary>
        [HttpGet("{versionId}/download")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDownloadInfo(int versionId, [FromQuery] string? userAgent = null)
        {
            var update = await _context.Updates
                .Where(u => u.version_id == versionId).FirstOrDefaultAsync();

            if (update == null)
                return NotFound(new { message = "Update not found or not available for download" });

            // Log download attempt
            _logger.LogInformation("Update {Version} download requested by {UserAgent}", 
                update.version_number, userAgent ?? "Unknown");

            // Return download information
            var downloadInfo = new {
                version_number = update.version_number,
                download_link = update.download_link,
                changelog = update.changelog,
                is_mandatory_update = update.is_mandatory_update,
                release_date = update.release_date,
                download_initiated_at = DateTime.UtcNow
            };

            return Ok(downloadInfo);
        }

        #region Private Helper Methods

        private static bool IsValidVersionNumber(string version)
        {
            if (string.IsNullOrEmpty(version)) return false;
            
            var parts = version.Split('.');
            if (parts.Length < 2 || parts.Length > 4) return false;
            
            return parts.All(part => int.TryParse(part, out _));
        }

        private static int CompareVersions(string version1, string version2)
        {
            var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
            var v2Parts = version2.Split('.').Select(int.Parse).ToArray();
            
            int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);
            
            for (int i = 0; i < maxLength; i++)
            {
                int v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
                int v2Part = i < v2Parts.Length ? v2Parts[i] : 0;
                
                if (v1Part != v2Part)
                    return v1Part.CompareTo(v2Part);
            }
            
            return 0;
        }

        #endregion
    }

    #region Request Models

    /// <summary>
    /// Filter request for updates
    /// </summary>
    public class UpdateFilterRequest
    {
        public bool? IsMandatory { get; set; }
        public DateTime? ReleaseDateFrom { get; set; }
        public DateTime? ReleaseDateTo { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// Request to create a new update
    /// </summary>
    public class CreateUpdateRequest
    {
        public string VersionNumber { get; set; } = string.Empty;
        public string Changelog { get; set; } = string.Empty;
        public string DownloadLink { get; set; } = string.Empty;
        public DateTime? ReleaseDate { get; set; }
        public bool IsMandatoryUpdate { get; set; } = false;
    }

    /// <summary>
    /// Request to update an existing update
    /// </summary>
    public class UpdateUpdateRequest
    {
        public string? Changelog { get; set; }
        public string? DownloadLink { get; set; }
        public bool? IsMandatoryUpdate { get; set; }
    }

    #endregion
}