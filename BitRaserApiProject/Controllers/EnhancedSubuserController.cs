using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using BitRaserApiProject.Utilities; // ✅ ADD: For Base64EmailEncoder.DecodeEmailParam
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;
using BitRaserApiProject.Factories;
using Microsoft.Extensions.Logging; // ✅ ADDED

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Subuser management controller with email-based operations and role-based access control
    /// Supports user-friendly subuser management without strict permission requirements
    /// ✅ UPDATED: Routes to Private Cloud if enabled
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedSubuserController : ControllerBase
    {
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly IQuotaService _quotaService;
        private readonly ICacheService _cacheService;
        private readonly IActivityLogService _activityLogService;
        private readonly ILogger<EnhancedSubuserController> _logger;

        public EnhancedSubuserController(
           DynamicDbContextFactory contextFactory,
        IRoleBasedAuthService authService,
         IUserDataService userDataService,
         IQuotaService quotaService,
         ICacheService cacheService,
         IActivityLogService activityLogService,
      ILogger<EnhancedSubuserController> logger)
        {
            _contextFactory = contextFactory;
            _authService = authService;
            _userDataService = userDataService;
            _quotaService = quotaService;
            _cacheService = cacheService;
            _activityLogService = activityLogService;
            _logger = logger;
        }

        /// <summary>
        /// Get all subusers with role-based filtering (email-based operations)
        /// ✅ ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSubusers([FromQuery] SubuserFilterRequest? filter)
        {
            try
            {
                // ✅ Use dynamic context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // ✅ Log which database is being used
                var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
                var connectionString = await tenantService.GetConnectionStringForUserAsync(currentUserEmail);
                var mainConnectionString = HttpContext.RequestServices.GetRequiredService<IConfiguration>()
                    .GetConnectionString("ApplicationDbContextConnection");
                var dbSource = (connectionString != mainConnectionString && !string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("Server="))
                    ? "Private Cloud DB"
                    : "Main DB";

                _logger.LogInformation("🔍 GetSubusers called by {Email}, Database: {DbSource}", currentUserEmail, dbSource);

                // ✅ Check if user is subuser in the CORRECT database context (not Main DB only)
                var currentSubuser = await context.subuser.Where(s => s.subuser_email == currentUserEmail).FirstOrDefaultAsync();
                bool isCurrentUserSubuser = currentSubuser != null;
                string? parentEmail = currentSubuser?.user_email;

                IQueryable<subuser> query = context.subuser.AsNoTracking();

                // Check if user has admin-level permissions
                bool hasAdminPermission = await _authService.HasPermissionAsync(
               currentUserEmail, "READ_ALL_SUBUSERS", isCurrentUserSubuser);

                if (!hasAdminPermission)
                {
                    if (isCurrentUserSubuser && parentEmail != null)
                    {
                        // ✅ FIX: Subusers can see:
                        // 1. Their OWN subusers (where user_email = currentUserEmail)
                        // 2. Other subusers under their PARENT (where user_email = parentEmail)
                        query = query.Where(s => s.user_email == currentUserEmail || s.user_email == parentEmail);
                        _logger.LogInformation("👤 Subuser {Email} viewing own subusers + subusers under parent {Parent}", currentUserEmail, parentEmail);
                    }
                    else
                    {
                        // Regular users can see their own subusers
                        query = query.Where(s => s.user_email == currentUserEmail);
                        _logger.LogInformation("👤 User {Email} viewing their own subusers", currentUserEmail);
                    }
                }

                // Apply additional filters if provided
                if (filter != null)
                {
                    if (!string.IsNullOrEmpty(filter.ParentUserEmail))
                        query = query.Where(s => s.user_email.Contains(filter.ParentUserEmail));

                    if (!string.IsNullOrEmpty(filter.SubuserEmail))
                        query = query.Where(s => s.subuser_email.Contains(filter.SubuserEmail));
                }

                var subusers = await query
                    .Include(s => s.SubuserRoles)
                    .ThenInclude(sr => sr.Role)
                    .AsSplitQuery()  // ✅ RENDER OPTIMIZATION: Prevent cartesian explosion
                    .OrderByDescending(s => s.subuser_id)
                    .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                    .Take(filter?.PageSize ?? 100)
                    .Select(s => new
               {
                   s.subuser_id,
                   s.subuser_email,
                   s.user_email,
                   subuser_name = s.Name,
                   s.Department,
                   s.Role,
                   s.Phone,
                   s.subuser_group,
                   s.license_allocation,
                   s.status,
                   s.IsEmailVerified,
                   s.CreatedAt,
                   s.UpdatedAt,
                   roles = s.SubuserRoles.Select(sr => sr.Role.RoleName).ToList(),
                   hasPassword = !string.IsNullOrEmpty(s.subuser_password)
               })
               .ToListAsync();

                _logger.LogInformation("Retrieved {Count} enhanced subusers for {Email}", subusers.Count, currentUserEmail);
                return Ok(subusers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving enhanced subusers");
                return StatusCode(500, new
                {
                    message = "Error retrieving subusers",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get subuser by email with role validation
        /// </summary>
        [HttpGet("{email}")]
        [DecodeEmail]
        public async Task<ActionResult<object>> GetSubuser(string email)
        {
            try
            {
                // ✅ CRITICAL: Decode email before any usage
                var decodedEmail = Base64EmailEncoder.DecodeEmailParam(email);
                
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

                var subuser = await context.subuser
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION: Read-only query
                    .Include(s => s.SubuserRoles)
                    .ThenInclude(sr => sr.Role)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .AsSplitQuery()  // ✅ Prevent cartesian explosion
                    .Where(s => s.subuser_email.ToLower() == decodedEmail)
                    .FirstOrDefaultAsync(); // ✅ Use decoded email

                if (subuser == null) return NotFound($"Subuser with email {decodedEmail} not found");

                // Check if user can view this subuser
                bool canView = subuser.user_email == currentUserEmail ||
                    await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser);

                if (!canView)
                {
                    return StatusCode(403, new { error = "You can only view your own subusers" });
                }

                var subuserDetails = new
                {
                    subuser.subuser_id,
                    subuser.subuser_email,
                    subuser.user_email,
                    subuser_name = subuser.Name,
                    subuser.Department,
                    subuser.Role,
                    subuser.Phone,
                    subuser.subuser_group,
                    subuser.license_allocation,
                    subuser.status,
                    subuser.IsEmailVerified,
                    subuser.CreatedAt,
                    subuser.UpdatedAt,
                    roles = subuser.SubuserRoles.Select(sr => new
                    {
                        sr.Role.RoleName,
                        sr.Role.Description,
                        sr.AssignedAt,
                        sr.AssignedByEmail
                    }).ToList(),
                    permissions = subuser.SubuserRoles
                     .SelectMany(sr => sr.Role.RolePermissions)
                          .Select(rp => rp.Permission.PermissionName)
                      .Distinct()
                  .ToList(),
                    hasPassword = !string.IsNullOrEmpty(subuser.subuser_password)
                };

                _logger.LogInformation("✅ Retrieved subuser {Email}", email);
                return Ok(subuserDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subuser {Email}", email);
                return StatusCode(500, new { message = "Error retrieving subuser", error = ex.Message });
            }
        }

        /// <summary>
        /// Get subusers by parent user email with management hierarchy
        /// ✅ PERFORMANCE: Cached with 3-minute TTL
        /// </summary>
        [HttpGet("by-parent/{parentEmail}")]
        [DecodeParentEmail]
        public async Task<ActionResult<IEnumerable<object>>> GetSubusersByParent(string parentEmail)
        {
            try
            {
                // ✅ CRITICAL: Decode parentEmail before any usage
                var decodedParentEmail = Base64EmailEncoder.DecodeEmailParam(parentEmail);
                
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // ✅ Log which database is being used
                var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
                var connectionString = await tenantService.GetConnectionStringForUserAsync(currentUserEmail!);
                var mainConnectionString = HttpContext.RequestServices.GetRequiredService<IConfiguration>()
                    .GetConnectionString("ApplicationDbContextConnection");
                var dbSource = (connectionString != mainConnectionString && !string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("Server="))
                    ? "Private Cloud DB"
                    : "Main DB";

                // ✅ Check if user is subuser in the CORRECT database context
                var currentSubuser = await context.subuser.Where(s => s.subuser_email == currentUserEmail).FirstOrDefaultAsync();
                bool isCurrentUserSubuser = currentSubuser != null;

                _logger.LogInformation("🔍 GetSubusersByParent called by {Email} for parent {ParentEmail}, Database: {DbSource}", 
                    currentUserEmail, decodedParentEmail, dbSource);

                // Check if user can view subusers for this parent email - use decoded email
                // ✅ FIX: Allow subusers to view:
                // 1. Their own subusers (parentEmail = currentUserEmail)
                // 2. Subusers under their parent (parentEmail = currentSubuser.user_email)
                bool canView = decodedParentEmail == currentUserEmail?.ToLower() || // Their own subusers
         await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser) ||
                 await _authService.CanManageUserAsync(currentUserEmail!, decodedParentEmail) ||
                 (isCurrentUserSubuser && currentSubuser?.user_email?.ToLower() == decodedParentEmail); // Subusers can view under their parent

                if (!canView)
                {
                    return StatusCode(403, new { error = "You can only view your own subusers or subusers of users you manage" });
                }

                // ✅ PERFORMANCE: Cache subuser list with 3-minute TTL
                var cacheKey = $"{CacheService.CacheKeys.SubuserList}:parent:{decodedParentEmail}:{dbSource}";
                
                _logger.LogDebug("🔍 GetSubusersByParent cache key: {Key}", cacheKey);

                var subusers = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
                {
                    _logger.LogInformation("📊 DB FETCH: Subusers for parent: {Email}", decodedParentEmail);
                    
                    return await context.subuser
                        .Include(s => s.SubuserRoles)
                        .ThenInclude(sr => sr.Role)
                        .Where(s => s.user_email.ToLower() == decodedParentEmail)
                        .OrderByDescending(s => s.subuser_id)
                        .Select(s => new
                        {
                            s.subuser_id,
                            s.subuser_email,
                            s.user_email,
                            subuser_name = s.Name,
                            s.Department,
                            s.Role,
                            s.Phone,
                            s.subuser_group,
                            s.license_allocation,
                            s.status,
                            s.IsEmailVerified,
                            s.CreatedAt,
                            s.UpdatedAt,
                            roles = s.SubuserRoles.Select(sr => sr.Role.RoleName).ToList(),
                            hasPassword = !string.IsNullOrEmpty(s.subuser_password)
                        })
                        .ToListAsync();
                }, CacheService.CacheTTL.Short);  // ✅ 3 minutes TTL

                _logger.LogInformation("✅ Retrieved {Count} subusers for parent {ParentEmail}", subusers.Count, parentEmail);
                return Ok(subusers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subusers for parent {ParentEmail}", parentEmail);
                return StatusCode(500, new { message = "Error retrieving subusers", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new subuser - Users can create subusers for themselves
        /// ✅ Create new subuser - ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> CreateSubuser([FromBody] SubuserCreateRequest request)
        {
            try
            {
                // ✅ Use dynamic context - automatically routes to correct database
                // TenantConnectionService now searches Private Cloud DBs for subusers
                using var context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

                // ✅ Log which database is being used
                var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
                var connectionString = await tenantService.GetConnectionStringForUserAsync(currentUserEmail!);
                var mainConnectionString = HttpContext.RequestServices.GetRequiredService<IConfiguration>()
                    .GetConnectionString("ApplicationDbContextConnection");
                var dbSource = (connectionString != mainConnectionString && !string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("Server="))
                    ? "Private Cloud DB"
                    : "Main DB";

                _logger.LogInformation("🔍 Creating subuser - Current user: {Email}, IsSubuser: {IsSubuser}, Database: {DbSource}",
          currentUserEmail, isCurrentUserSubuser, dbSource);

                // ✅ REMOVED: Redundant CanCreateSubusersAsync check
                // If we reach here, it means basic permissions are validated
                // Users should be able to create their own subusers

                // Optional: Log user's roles for debugging
                var userRoles = await _authService.GetUserRolesAsync(currentUserEmail!, isCurrentUserSubuser);
                _logger.LogInformation("User roles: {Roles}", string.Join(", ", userRoles));

                // ✅ QUOTA CHECK - Enforced from MAIN DB (cannot be bypassed even in Private Cloud)
                var parentEmail = isCurrentUserSubuser 
                    ? (await context.subuser.Where(s => s.subuser_email == currentUserEmail).FirstOrDefaultAsync())?.user_email ?? currentUserEmail!
                    : currentUserEmail!;

                var quotaCheck = await _quotaService.CanCreateSubuserAsync(parentEmail);
                if (!quotaCheck.CanProceed)
                {
                    _logger.LogWarning("⚠️ Quota limit reached for {Email}: {Message}", parentEmail, quotaCheck.Message);
                    return StatusCode(403, new
                    {
                        success = false,
                        error = "QUOTA_LIMIT_REACHED",
                        message = quotaCheck.Message,
                        limit = quotaCheck.Limit,
                        used = quotaCheck.Used,
                        remaining = quotaCheck.Remaining
                    });
                }

                _logger.LogInformation("✅ Quota check passed for {Email}: {Limit} max, {Used} used, {Remaining} remaining",
                    parentEmail, quotaCheck.Limit, quotaCheck.Used, quotaCheck.Remaining);

                // Validate input - only email and password are required
                if (string.IsNullOrEmpty(request.subuser_email) || string.IsNullOrEmpty(request.subuser_password))
                {
                    _logger.LogWarning("⚠️ Invalid request - missing email or password");
                    return BadRequest("Subuser email and password are required");
                }

                // Check if subuser already exists
                var existingSubuser = await context.subuser
                 .Where(s => s.subuser_email == request.subuser_email)
           .Select(s => new { s.subuser_email })
            .FirstOrDefaultAsync();

                if (existingSubuser != null)
                {
                    _logger.LogWarning("⚠️ Subuser already exists: {Email}", request.subuser_email);
                    return Conflict($"Subuser with email {request.subuser_email} already exists");
                }


                // ✅ SMART PARENT EMAIL RESOLUTION
                string parentUserEmail;
                int parentUserId;

                if (isCurrentUserSubuser)
                {
                    // ✅ If SUBUSER is creating: Find their parent user
                    var currentSubuser = await context.subuser
                   .Where(s => s.subuser_email == currentUserEmail).FirstOrDefaultAsync();

                    if (currentSubuser == null)
                    {
                        _logger.LogError("❌ Current subuser not found: {Email}", currentUserEmail);
                        return BadRequest("Current subuser not found");
                    }

                    // Use the subuser's parent as the parent for new subuser
                    parentUserEmail = currentSubuser.user_email;
                    parentUserId = currentSubuser.superuser_id ?? 0;

                    _logger.LogInformation(
                     "📧 Subuser {SubuserEmail} creating subuser. Using parent {ParentEmail}",
                  currentUserEmail,
                   parentUserEmail
                    );
                }
                else
                {
                    // ✅ If REGULAR USER is creating: Use their email as parent
                    parentUserEmail = request.parentUserEmail ?? currentUserEmail!;

                    // ✅ FIX: Don't query Users table in private cloud
                    // Try to get parent info from subuser table first
                    var parentSubuser = await context.subuser
                     .Where(s => s.subuser_email == parentUserEmail)
                                 .Select(s => new { s.superuser_id })
                                .FirstOrDefaultAsync();

                    if (parentSubuser != null)
                    {
                        // Parent is also a subuser
                        parentUserId = parentSubuser.superuser_id ?? 0;
                    }
                    else
                    {
                        // Parent is a regular user - use a placeholder ID
                        // In private cloud, we don't have access to Users table
                        parentUserId = 0; // Default parent ID
                    }

                    _logger.LogInformation(
               "👤 User {UserEmail} creating subuser with parent {ParentEmail}",
                 currentUserEmail,
               parentUserEmail
                 );
                }

                // Create subuser with all fields
                var newSubuser = new subuser
                {
                    subuser_email = request.subuser_email,
                    subuser_password = BCrypt.Net.BCrypt.HashPassword(request.subuser_password),
                    user_email = parentUserEmail,
                    superuser_id = parentUserId > 0 ? parentUserId : null,
                    Name = request.subuser_name ?? request.subuser_email.Split('@')[0],
                    Department = request.department,
                    Role = request.role ?? "subuser",
                    Phone = request.phone,
                    subuser_group = request.subuser_group,
                    license_allocation = request.license_allocation ?? 0,
                    status = "active",
                    IsEmailVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = parentUserId // Use parentUserId directly (0 if no valid parent)
                };

                context.subuser.Add(newSubuser);
                await context.SaveChangesAsync();

                // ✅ Assign role to rolebasedAuth system (SubuserRoles table)
                var roleToAssign = request.role ?? "SubUser";
                var roleAssigned = await AssignRoleToSubuserAsync(request.subuser_email, roleToAssign, currentUserEmail!, context);

                if (!roleAssigned)
                {
                    _logger.LogWarning("Failed to assign role {RoleName} to subuser {Email} in RBAC system", roleToAssign, request.subuser_email);
                }

                _logger.LogInformation("✅ Enhanced subuser created: ID={SubuserId}, Email={Email} in {DbSource}", newSubuser.subuser_id, newSubuser.subuser_email, dbSource);

                // ✅ INCREMENT USED SUBUSER COUNT in MAIN DB
                await _quotaService.IncrementSubuserCountAsync(parentEmail);
                
                // ✅ UPDATE LICENSE USAGE if license was allocated
                if (request.license_allocation.HasValue && request.license_allocation.Value > 0)
                {
                    await _quotaService.UpdateUsedLicensesAsync(parentEmail, request.license_allocation.Value);
                }

                // ✅ INVALIDATE CACHE: Clear quota, license & subuser cache for this user
                _cacheService.RemoveByPrefix($"quota:status:{parentEmail}");
                _cacheService.RemoveByPrefix($"license:info:{parentEmail}");
                
                // ✅ PERFORMANCE: Invalidate subuser list cache so next fetch gets fresh data
                CacheInvalidation.InvalidateSubuser(_cacheService, newSubuser.subuser_email, parentEmail);

                // ✅ LOG ACTIVITY: Subuser created
                await _activityLogService.LogActivityAsync(
                    currentUserEmail!,
                    ActivityTypes.SUBUSER_CREATE,
                    newSubuser.subuser_id.ToString(),
                    "subuser",
                    new { subuserEmail = newSubuser.subuser_email, department = newSubuser.Department });

                var response = new
                {
                    success = true,
                    subuserEmail = newSubuser.subuser_email,
                    subuserName = newSubuser.Name,
                    department = newSubuser.Department,
                    role = newSubuser.Role,
                    phone = newSubuser.Phone,
                    subuser_group = newSubuser.subuser_group,
                    license_allocation = newSubuser.license_allocation,
                    parentUserEmail = newSubuser.user_email,
                    subuserID = newSubuser.subuser_id,
                    status = newSubuser.status,
                    createdAt = newSubuser.CreatedAt,
                    createdBy = isCurrentUserSubuser ? $"Subuser: {currentUserEmail}" : $"User: {currentUserEmail}",
                    roleAssignedToRBAC = roleAssigned,
                    databaseSource = dbSource,  // ✅ NEW: Show which database was used
                    message = roleAssigned
            ? "Subuser created successfully with role assigned to RBAC"
                 : "Subuser created successfully (role assignment to RBAC failed)"
                };

                return CreatedAtAction(nameof(GetSubuser), new { email = newSubuser.subuser_email }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating enhanced subuser");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating subuser",
                    error = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }
        /// <summary>
        /// Update subuser information by email
        /// ✅ ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpPut("{email}")]
        [DecodeEmail]
        public async Task<IActionResult> UpdateSubuser(string email, [FromBody] SubuserUpdateRequest request)
        {
            try
            {
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                if (email != request.SubuserEmail)
                    return BadRequest("Email mismatch in request");

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
                var subuser = await context.subuser.Where(s => s.subuser_email == email).FirstOrDefaultAsync();

                if (subuser == null) return NotFound($"Subuser with email {email} not found");

                // Check if user can update this subuser
                bool canUpdate = subuser.user_email == currentUserEmail ||
        await _authService.HasPermissionAsync(currentUserEmail!, "UPDATE_ALL_SUBUSERS", isCurrentUserSubuser);

                if (!canUpdate)
                {
                    return StatusCode(403, new { error = "You can only update your own subusers" });
                }

                // Update all fields if provided
                if (!string.IsNullOrEmpty(request.SubuserName))
                {
                    subuser.Name = request.SubuserName;
                }

                if (request.Department != null)
                {
                    subuser.Department = string.IsNullOrEmpty(request.Department) ? null : request.Department;
                }

                if (request.Role != null)
                {
                    subuser.Role = string.IsNullOrEmpty(request.Role) ? "subuser" : request.Role;
                }

                if (request.Phone != null)
                {
                    subuser.Phone = string.IsNullOrEmpty(request.Phone) ? null : request.Phone;
                }

                if (request.SubuserGroup != null)
                {
                    subuser.subuser_group = string.IsNullOrEmpty(request.SubuserGroup) ? null : request.SubuserGroup;
                }

                if (request.LicenseAllocation.HasValue)
                {
                    subuser.license_allocation = request.LicenseAllocation.Value;
                }

                if (!string.IsNullOrEmpty(request.Status))
                {
                    subuser.status = request.Status;
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    subuser.subuser_password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                }

                // Update parent user if provided and user has permission
                if (!string.IsNullOrEmpty(request.NewParentUserEmail) &&
               request.NewParentUserEmail != subuser.user_email)
                {
                    if (!await _authService.HasPermissionAsync(currentUserEmail!, "REASSIGN_SUBUSERS", isCurrentUserSubuser))
                    {
                        return StatusCode(403, new { error = "Insufficient permissions to reassign subuser to different parent" });
                    }

                    // ✅ FIX: Don't query Users table in private cloud
                    // Try to find new parent in subuser table first
                    var newParentSubuser = await context.subuser
                      .Where(s => s.subuser_email == request.NewParentUserEmail)
                         .Select(s => new { s.superuser_id })
                      .FirstOrDefaultAsync();

                    if (newParentSubuser != null)
                    {
                        // New parent is a subuser
                        subuser.user_email = request.NewParentUserEmail;
                        subuser.superuser_id = newParentSubuser.superuser_id ?? 0;
                    }
                    else
                    {
                        // New parent might be a regular user (not accessible in private cloud)
                        // Just update the email reference
                        subuser.user_email = request.NewParentUserEmail;
                        // Keep existing superuser_id or set to 0
                    }
                }

                // Update timestamp
                subuser.UpdatedAt = DateTime.UtcNow;
                // ✅ FIX: Get updater ID from subuser table
                var updaterSubuser = await context.subuser
                  .Where(s => s.subuser_email == currentUserEmail)
                .Select(s => new { s.superuser_id })
             .FirstOrDefaultAsync();
                subuser.UpdatedBy = updaterSubuser?.superuser_id ?? null;

                context.Entry(subuser).State = EntityState.Modified;
                await context.SaveChangesAsync();

                // ✅ PERFORMANCE: Invalidate subuser cache after update
                CacheInvalidation.InvalidateSubuser(_cacheService, email, subuser.user_email);

                _logger.LogInformation("✅ Subuser updated: {Email}", email);

                return Ok(new
                {
                    message = "Subuser updated successfully",
                    subuserEmail = email,
                    subuser_name = subuser.Name,
                    department = subuser.Department,
                    role = subuser.Role,
                    phone = subuser.Phone,
                    subuser_group = subuser.subuser_group,
                    license_allocation = subuser.license_allocation,
                    status = subuser.status,
                    updatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subuser {Email}", email);
                return StatusCode(500, new { message = "Error updating subuser", error = ex.Message });
            }
        }

        /// <summary>
        /// PATCH: Flexible update for subuser - Update single or multiple fields
        /// Can find subuser by parent user email OR by subuser email
        /// Supports partial updates - only fields provided will be updated
        /// ✅ ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpPatch("update")]
        public async Task<IActionResult> PatchSubuser([FromBody] SubuserPatchRequest request)
        {
            try
            {
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

                // Find subuser by subuser_email OR by parentUserEmail + subuser_email combination
                subuser? targetSubuser = null;

                if (!string.IsNullOrEmpty(request.subuser_email))
                {
                    // First try to find by subuser_email directly
                    targetSubuser = await context.subuser
                 .Where(s => s.subuser_email == request.subuser_email).FirstOrDefaultAsync();

                    // If parentUserEmail is also provided, validate it matches
                    if (targetSubuser != null && !string.IsNullOrEmpty(request.parentUserEmail))
                    {
                        if (targetSubuser.user_email != request.parentUserEmail)
                        {
                            return BadRequest(new
                            {
                                message = "Subuser email and parent user email mismatch",
                                subuserEmail = request.subuser_email,
                                providedParent = request.parentUserEmail,
                                actualParent = targetSubuser.user_email
                            });
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(request.parentUserEmail))
                {
                    // If only parent email provided, we need more context
                    return BadRequest(new
                    {
                        message = "subuser_email is required for update. Provide subuser_email to identify which subuser to update"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        message = "Either subuser_email or both subuser_email and parentUserEmail are required"
                    });
                }

                if (targetSubuser == null)
                {
                    return NotFound(new
                    {
                        message = $"Subuser not found",
                        subuser_email = request.subuser_email
                    });
                }

                // Check if current user can update this subuser
                bool canUpdate = targetSubuser.user_email == currentUserEmail ||
                await _authService.HasPermissionAsync(currentUserEmail, "UPDATE_ALL_SUBUSERS", isCurrentUserSubuser);

                if (!canUpdate)
                {
                    return StatusCode(403, new
                    {
                        message = "You can only update your own subusers",
                        currentUser = currentUserEmail,
                        subuserParent = targetSubuser.user_email
                    });
                }

                var updatedFields = new List<string>();

                // Update subuser_email if provided (email change)
                if (!string.IsNullOrEmpty(request.new_subuser_email) &&
                   request.new_subuser_email != targetSubuser.subuser_email)
                {
                    // Check if new email already exists
                    var emailExists = await context.subuser
                 .AnyAsync(s => s.subuser_email == request.new_subuser_email && s.subuser_id != targetSubuser.subuser_id);

                    if (emailExists)
                    {
                        return Conflict(new { message = $"Email {request.new_subuser_email} is already in use" });
                    }

                    targetSubuser.subuser_email = request.new_subuser_email;
                    updatedFields.Add("subuser_email");
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(request.subuser_password))
                {
                    targetSubuser.subuser_password = BCrypt.Net.BCrypt.HashPassword(request.subuser_password);
                    updatedFields.Add("subuser_password");
                }

                // Update subuser_name if provided
                if (request.subuser_name != null)
                {
                    targetSubuser.Name = string.IsNullOrEmpty(request.subuser_name) ? null : request.subuser_name;
                    updatedFields.Add("subuser_name");
                }

                // Update department if provided
                if (request.department != null)
                {
                    targetSubuser.Department = string.IsNullOrEmpty(request.department) ? null : request.department;
                    updatedFields.Add("department");
                }

                // Update role if provided
                if (request.role != null)
                {
                    targetSubuser.Role = string.IsNullOrEmpty(request.role) ? "subuser" : request.role;
                    updatedFields.Add("role");
                }

                // Update phone if provided
                if (request.phone != null)
                {
                    targetSubuser.Phone = string.IsNullOrEmpty(request.phone) ? null : request.phone;
                    updatedFields.Add("phone");
                }

                // Update subuser_group if provided
                if (request.subuser_group != null)
                {
                    targetSubuser.subuser_group = string.IsNullOrEmpty(request.subuser_group) ? null : request.subuser_group;
                    updatedFields.Add("subuser_group");
                }

                // Update parentUserEmail if provided (reassign subuser to different parent)
                if (!string.IsNullOrEmpty(request.new_parentUserEmail) &&
                    request.new_parentUserEmail != targetSubuser.user_email)
                {
                    // Check permission for reassignment
                    if (!await _authService.HasPermissionAsync(currentUserEmail, "REASSIGN_SUBUSERS", isCurrentUserSubuser))
                    {
                        return StatusCode(403, new
                        {
                            message = "Insufficient permissions to reassign subuser to different parent"
                        });
                    }

                    // ✅ FIX: Don't query Users table - get from subuser table
                    var newParentSubuser = await context.subuser
                  .Where(s => s.subuser_email == request.new_parentUserEmail)
                      .Select(s => new { s.superuser_id })
             .FirstOrDefaultAsync();

                    if (newParentSubuser != null)
                    {
                        targetSubuser.user_email = request.new_parentUserEmail;
                        targetSubuser.superuser_id = newParentSubuser.superuser_id ?? 0;
                        updatedFields.Add("parentUserEmail");
                    }
                    else
                    {
                        // Parent not found in subuser table - might be regular user
                        targetSubuser.user_email = request.new_parentUserEmail;
                        // Keep existing superuser_id
                        updatedFields.Add("parentUserEmail");
                    }
                }

                // Update license_allocation if provided
                if (request.license_allocation.HasValue)
                {
                    // ✅ VALIDATION: Calculate actual used licenses (consumed)
                    // We must prevent revoking licenses that are already active/used
                    var consumedLicenses = await context.LicenseActivations
                        .CountAsync(l => l.UserEmail == targetSubuser.subuser_email && l.Status == "ACTIVE");

                    int newAllocation = request.license_allocation.Value;

                    if (newAllocation < consumedLicenses)
                    {
                        return BadRequest(new 
                        { 
                            message = $"Cannot reduce allocation below {consumedLicenses}. Subuser has already used {consumedLicenses} licenses.",
                            consumed = consumedLicenses,
                            requested = newAllocation
                        });
                    }

                    int oldAllocation = targetSubuser.license_allocation ?? 0;
                    int difference = newAllocation - oldAllocation;

                    if (difference != 0)
                    {
                        // ✅ Check quota if increasing allocation
                        if (difference > 0)
                        {
                            var quotaCheck = await _quotaService.CanCreateSubuserAsync(targetSubuser.user_email);
                            // Note: Proceeding assuming admin has enough available.
                        }

                        // ✅ Update Parent's Used Licenses
                        await _quotaService.UpdateUsedLicensesAsync(targetSubuser.user_email, difference);
                        
                        // ✅ Invalidate cache 
                        _cacheService.RemoveByPrefix($"license:info:{targetSubuser.user_email}");
                    }

                    targetSubuser.license_allocation = newAllocation;
                    updatedFields.Add("license_allocation");
                }

                // Update status if provided
                if (!string.IsNullOrEmpty(request.status))
                {
                    targetSubuser.status = request.status;
                    updatedFields.Add("status");
                }

                if (updatedFields.Count == 0)
                {
                    return BadRequest(new { message = "No fields to update. Please provide at least one field to update" });
                }

                // Update timestamp
                targetSubuser.UpdatedAt = DateTime.UtcNow;
                // ✅ FIX: Get updater ID from subuser table
                var updaterSubuser = await context.subuser
                     .Where(s => s.subuser_email == currentUserEmail)
              .Select(s => new { s.superuser_id })
                  .FirstOrDefaultAsync();
                targetSubuser.UpdatedBy = updaterSubuser?.superuser_id ?? null;

                context.Entry(targetSubuser).State = EntityState.Modified;
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Subuser updated: {Email}, Fields: {Fields}",
                targetSubuser.subuser_email, string.Join(", ", updatedFields));

                // ✅ Calculate usage stats for response
                var currentUsage = await context.LicenseActivations
                    .CountAsync(l => l.UserEmail == targetSubuser.subuser_email && l.Status == "ACTIVE");
                
                var totalAllocated = targetSubuser.license_allocation ?? 0;
                var unusedCount = totalAllocated - currentUsage;

                return Ok(new
                {
                    message = "Subuser updated successfully",
                    subuser_email = targetSubuser.subuser_email,
                    subuser_name = targetSubuser.Name,
                    department = targetSubuser.Department,
                    role = targetSubuser.Role,
                    phone = targetSubuser.Phone,
                    subuser_group = targetSubuser.subuser_group,
                    parentUserEmail = targetSubuser.user_email,
                    license_allocation = totalAllocated,
                    used_licenses = currentUsage,      // ✅ Active usages
                    unused_licenses = unusedCount,     // ✅ Available to revoke
                    status = targetSubuser.status,
                    updatedFields = updatedFields,
                    updatedAt = targetSubuser.UpdatedAt,
                    updatedBy = currentUserEmail
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating subuser");
                return StatusCode(409, new { message = "Concurrency error. The subuser was modified by another user. Please retry." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subuser");
                return StatusCode(500, new
                {
                    message = "Error updating subuser",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Change subuser password by email - Requires current password verification
        /// ✅ ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpPatch("{email}/change-password")]
        [DecodeEmail]
        public async Task<IActionResult> ChangeSubuserPassword(string email, [FromBody] ChangePasswordRequest request)
        {
            try
            {
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);
                var subuser = await context.subuser.Where(s => s.subuser_email == email).FirstOrDefaultAsync();

                if (subuser == null)
                {
                    return NotFound(new { message = $"Subuser with email {email} not found" });
                }

                // Check if user can change password for this subuser
                bool canChange = subuser.user_email == currentUserEmail ||
            await _authService.HasPermissionAsync(currentUserEmail, "CHANGE_ALL_SUBUSER_PASSWORDS", isCurrentUserSubuser);

                if (!canChange)
                {
                    return StatusCode(403, new { message = "You can only change passwords for your own subusers" });
                }

                // Validate request
                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    return BadRequest(new { message = "Current password is required" });
                }

                if (string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { message = "New password is required" });
                }

                if (request.NewPassword.Length < 8)
                {
                    return BadRequest(new { message = "New password must be at least 8 characters" });
                }

                // Verify current password
                if (string.IsNullOrEmpty(subuser.subuser_password))
                {
                    return BadRequest(new { message = "Subuser password not set. Cannot verify current password." });
                }

                bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, subuser.subuser_password);
                if (!isCurrentPasswordValid)
                {
                    _logger.LogWarning("Failed password change attempt for subuser {Email} - incorrect current password", email);
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                // Check if new password is same as current password
                bool isSamePassword = BCrypt.Net.BCrypt.Verify(request.NewPassword, subuser.subuser_password);
                if (isSamePassword)
                {
                    return BadRequest(new { message = "New password must be different from current password" });
                }

                // Update password
                string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                subuser.subuser_password = newHashedPassword;
                subuser.UpdatedAt = DateTime.UtcNow;
                // ✅ FIX: Get updater ID from subuser table
                var updaterSubuser = await context.subuser
                       .Where(s => s.subuser_email == currentUserEmail)
                .Select(s => new { s.superuser_id })
                 .FirstOrDefaultAsync();
                subuser.UpdatedBy = updaterSubuser?.superuser_id ?? null;

                context.Entry(subuser).State = EntityState.Modified;
                context.Entry(subuser).Property(s => s.subuser_password).IsModified = true;

                int rowsAffected = await context.SaveChangesAsync();

                if (rowsAffected == 0)
                {
                    _logger.LogError("SaveChanges returned 0 rows affected for subuser {Email}", email);
                    return StatusCode(500, new
                    {
                        message = "Failed to save password changes to database",
                        error = "No rows were modified"
                    });
                }

                _logger.LogInformation("✅ Password changed successfully for subuser {Email}", email);
                return Ok(new
                {
                    success = true,
                    message = "Password changed successfully",
                    subuserEmail = email,
                    changedAt = DateTime.UtcNow,
                    rowsAffected = rowsAffected
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error changing password for subuser {Email}", email);
                return StatusCode(409, new
                {
                    success = false,
                    message = "Password change failed due to concurrency conflict",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for subuser {Email}", email);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error changing password",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Simple password change - Only requires subuser email
        /// Subuser can change their own password without needing parent user email
        /// Route: PATCH /api/EnhancedSubuser/simple-change-password
        /// ✅ ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpPatch("simple-change-password")]
        public async Task<IActionResult> SimpleChangePassword([FromBody] SimplePasswordChangeRequest request)
        {
            try
            {
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                // Validate request
                if (string.IsNullOrEmpty(request.SubuserEmail))
                {
                    return BadRequest(new { message = "Subuser email is required" });
                }

                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    return BadRequest(new { message = "Current password is required" });
                }

                if (string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { message = "New password is required" });
                }

                if (request.NewPassword.Length < 8)
                {
                    return BadRequest(new { message = "New password must be at least 8 characters" });
                }

                // Find subuser - NO NEED for parent user email
                var subuser = await context.subuser
                      .Where(s => s.subuser_email == request.SubuserEmail).FirstOrDefaultAsync();

                if (subuser == null)
                {
                    return NotFound(new
                    {
                        message = $"Subuser with email {request.SubuserEmail} not found"
                    });
                }

                // Verify current password
                if (string.IsNullOrEmpty(subuser.subuser_password))
                {
                    return BadRequest(new
                    {
                        message = "Subuser password not set. Cannot verify current password."
                    });
                }

                bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
                   request.CurrentPassword,
              subuser.subuser_password
                 );

                if (!isCurrentPasswordValid)
                {
                    _logger.LogWarning(
                               "Failed password change attempt for subuser {Email} - incorrect current password",
                      request.SubuserEmail
                         );
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                // Check if new password is same as current password
                bool isSamePassword = BCrypt.Net.BCrypt.Verify(
                    request.NewPassword,
              subuser.subuser_password
             );

                if (isSamePassword)
                {
                    return BadRequest(new
                    {
                        message = "New password must be different from current password"
                    });
                }

                // Update password
                string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                subuser.subuser_password = newHashedPassword;
                subuser.UpdatedAt = DateTime.UtcNow;

                // Mark entity as modified
                context.Entry(subuser).State = EntityState.Modified;
                context.Entry(subuser).Property(s => s.subuser_password).IsModified = true;

                // Save changes
                int rowsAffected = await context.SaveChangesAsync();

                if (rowsAffected == 0)
                {
                    _logger.LogError(
                     "SaveChanges returned 0 rows affected for subuser {Email}",
                  request.SubuserEmail
                       );
                    return StatusCode(500, new
                    {
                        message = "Failed to save password changes to database",
                        error = "No rows were modified"
                    });
                }

                _logger.LogInformation(
                     "✅ Password changed successfully for subuser {Email}. Rows affected: {RowsAffected}",
                           request.SubuserEmail,
                rowsAffected
                    );

                return Ok(new
                {
                    success = true,
                    message = "Password changed successfully",
                    subuserEmail = request.SubuserEmail,
                    changedAt = DateTime.UtcNow,
                    rowsAffected = rowsAffected
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error changing password for subuser {Email}", request.SubuserEmail);
                return StatusCode(409, new
                {
                    success = false,
                    message = "Password change failed due to concurrency conflict",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for subuser {Email}", request.SubuserEmail);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error changing password",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Assign role to subuser by email - Users can assign roles to their subusers
        /// ✅ ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpPost("{email}/assign-role")]
        [DecodeEmail]
        public async Task<IActionResult> AssignRoleToSubuser(string email, [FromBody] AssignRoleRequest request)
        {
            try
            {
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

                var subuser = await context.subuser.Where(s => s.subuser_email == email).FirstOrDefaultAsync();
                if (subuser == null) return NotFound($"Subuser with email {email} not found");

                // Check if user can assign roles to this subuser
                bool canAssign = subuser.user_email == currentUserEmail ||
            await _authService.HasPermissionAsync(currentUserEmail!, "ASSIGN_ALL_SUBUSER_ROLES", isCurrentUserSubuser);

                if (!canAssign)
                {
                    return StatusCode(403, new { error = "You can only assign roles to your own subusers" });
                }

                await AssignRoleToSubuserAsync(email, request.RoleName, currentUserEmail!, context);

                _logger.LogInformation("✅ Role {RoleName} assigned to subuser {Email}", request.RoleName, email);

                return Ok(new
                {
                    message = $"Role {request.RoleName} assigned to subuser {email}",
                    subuserEmail = email,
                    roleName = request.RoleName,
                    assignedBy = currentUserEmail,
                    assignedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to subuser {Email}", email);
                return StatusCode(500, new { message = "Error assigning role", error = ex.Message });
            }
        }

        /// <summary>
        /// Remove role from subuser by email
        /// ✅ ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpDelete("{email}/remove-role/{roleName}")]
        [DecodeEmail]
        public async Task<IActionResult> RemoveRoleFromSubuser(string email, string roleName)
        {
            try
            {
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

                var subuser = await context.subuser.Where(s => s.subuser_email == email).FirstOrDefaultAsync();
                if (subuser == null) return NotFound($"Subuser with email {email} not found");

                // Check permissions
                bool canRemove = subuser.user_email == currentUserEmail ||
                       await _authService.HasPermissionAsync(currentUserEmail!, "REMOVE_ALL_SUBUSER_ROLES", isCurrentUserSubuser);

                if (!canRemove)
                {
                    return StatusCode(403, new { error = "You can only remove roles from your own subusers" });
                }

                var role = await context.Roles.Where(r => r.RoleName == roleName).FirstOrDefaultAsync();
                if (role == null) return NotFound($"Role {roleName} not found");

                var subuserRole = await context.SubuserRoles
              .Where(sr => sr.SubuserId == subuser.subuser_id && sr.RoleId == role.RoleId).FirstOrDefaultAsync();

                if (subuserRole == null)
                    return NotFound($"Role {roleName} not assigned to subuser {email}");

                context.SubuserRoles.Remove(subuserRole);
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Role {RoleName} removed from subuser {Email}", roleName, email);

                return Ok(new
                {
                    message = $"Role {roleName} removed from subuser {email}",
                    subuserEmail = email,
                    roleName = roleName,
                    removedBy = currentUserEmail,
                    removedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from subuser {Email}", email);
                return StatusCode(500, new { message = "Error removing role", error = ex.Message });
            }
        }
        /// <summary>
        /// Delete subuser by email
        /// ✅ ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpDelete("{email}")]
        [DecodeEmail]
        public async Task<IActionResult> DeleteSubuser(string email)
        {
            try
            {
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
                var subuser = await context.subuser.Where(s => s.subuser_email == email).FirstOrDefaultAsync();

                if (subuser == null) return NotFound($"Subuser with email {email} not found");

                // Check if user can delete this subuser
                bool canDelete = subuser.user_email == currentUserEmail ||
                    await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_ALL_SUBUSERS", isCurrentUserSubuser);

                if (!canDelete)
                {
                    return StatusCode(403, new { error = "You can only delete your own subusers" });
                }

                context.subuser.Remove(subuser);
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Subuser deleted: {Email}", email);

                // ✅ DECREMENT USED SUBUSER COUNT in MAIN DB
                var parentEmail = subuser.user_email;
                await _quotaService.DecrementSubuserCountAsync(parentEmail);
                
                // ✅ RETURN LICENSE USAGE if license was allocated
                if (subuser.license_allocation.HasValue && subuser.license_allocation.Value > 0)
                {
                    await _quotaService.UpdateUsedLicensesAsync(parentEmail, -subuser.license_allocation.Value);
                }

                // ✅ INVALIDATE CACHE: Clear quota & license cache for this user
                _cacheService.RemoveByPrefix($"quota:status:{parentEmail}");
                _cacheService.RemoveByPrefix($"license:info:{parentEmail}");

                // ✅ LOG ACTIVITY: Subuser deleted
                await _activityLogService.LogActivityAsync(
                    currentUserEmail!,
                    ActivityTypes.SUBUSER_DELETE,
                    subuser.subuser_id.ToString(),
                    "subuser",
                    new { subuserEmail = email, parentEmail });

                return Ok(new
                {
                    message = "Subuser deleted successfully",
                    subuserEmail = email,
                    deletedAt = DateTime.UtcNow,
                    quotaFreed = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subuser {Email}", email);
                return StatusCode(500, new { message = "Error deleting subuser", error = ex.Message });
            }
        }

        /// <summary>
        /// Get subuser statistics by parent user email
        /// ✅ ROUTES TO PRIVATE CLOUD IF ENABLED
        /// </summary>
        [HttpGet("statistics/{parentEmail}")]
        [DecodeEmail]
        public async Task<ActionResult<object>> GetSubuserStatistics(string parentEmail)
        {
            try
            {
                // ✅ Create context - routes to private cloud if enabled
                using var context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

                // Check if user can view statistics for this parent email
                bool canView = parentEmail == currentUserEmail ||
          await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSER_STATISTICS", isCurrentUserSubuser);

                if (!canView)
                {
                    return StatusCode(403, new { error = "You can only view statistics for your own subusers" });
                }

                var stats = new
                {
                    ParentUserEmail = parentEmail,
                    TotalSubusers = await context.subuser.CountAsync(s => s.user_email == parentEmail),
                    SubusersWithRoles = await context.subuser
                     .Where(s => s.user_email == parentEmail)
                  .CountAsync(s => s.SubuserRoles.Any()),
                    SubusersWithoutRoles = await context.subuser
                   .Where(s => s.user_email == parentEmail)
                  .CountAsync(s => !s.SubuserRoles.Any()),
                    RoleDistribution = await context.SubuserRoles
                   .Join(context.subuser, sr => sr.SubuserId, s => s.subuser_id, (sr, s) => new { sr, s })
                  .Where(joined => joined.s.user_email == parentEmail)
                   .Join(context.Roles, joined => joined.sr.RoleId, r => r.RoleId, (joined, r) => r.RoleName)
                 .GroupBy(roleName => roleName)
                   .Select(g => new { RoleName = g.Key, Count = g.Count() })
                  .ToListAsync()
                };

                _logger.LogInformation("✅ Retrieved statistics for parent {ParentEmail}", parentEmail);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics for {ParentEmail}", parentEmail);
                return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get quota status for current user (from MAIN DB - cannot be bypassed)
        /// Shows max limits, current usage, and remaining quotas
        /// ✅ ENCRYPTED RESPONSE - Sensitive quota data
        /// </summary>
        [HttpGet("quota-status")]
        [EncryptResponse]
        public async Task<IActionResult> GetQuotaStatus()
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                // Get parent email if current user is a subuser (optimized with WHERE)
                using var context = await _contextFactory.CreateDbContextAsync();
                var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);
                var parentEmail = isSubuser
                    ? (await context.subuser.Where(s => s.subuser_email == currentUserEmail).Select(s => s.user_email).FirstOrDefaultAsync()) ?? currentUserEmail
                    : currentUserEmail;

                // ✅ CACHE: Get quota from cache or fetch from DB (5 min TTL)
                var cacheKey = $"quota:status:{parentEmail}";
                var quota = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
                {
                    return await _quotaService.GetQuotaStatusAsync(parentEmail);
                }, TimeSpan.FromMinutes(5));

                return Ok(new
                {
                    success = true,
                    userEmail = parentEmail,
                    cached = true,
                    quotas = new
                    {
                        subusers = new { max = quota.MaxSubusers, used = quota.UsedSubusers, remaining = quota.RemainingSubusers },
                        groups = new { max = quota.MaxGroups, used = quota.UsedGroups, remaining = quota.RemainingGroups },
                        departments = new { max = quota.MaxDepartments, used = quota.UsedDepartments },
                        licenses = new { max = quota.MaxLicenses, used = quota.UsedLicenses, remaining = quota.RemainingLicenses }
                    },
                    license = new
                    {
                        hasExpiry = quota.HasLicenseExpiry,
                        expiryDate = quota.LicenseExpiryDate,
                        isExpired = quota.IsLicenseExpired,
                        daysRemaining = quota.DaysUntilExpiry
                    },
                    lastSyncedAt = quota.QuotaSyncedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quota status");
                return StatusCode(500, new { message = "Error getting quota status", error = ex.Message });
            }
        }
        /// <summary>
        /// Get available licenses for current user
        /// Shows total allocation, used licenses, and available licenses
        /// ✅ ENCRYPTED RESPONSE - Sensitive license data
        /// </summary>
        [HttpGet("license-info")]
        [EncryptResponse]
        public async Task<IActionResult> GetLicenseInfo()
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                // ✅ CACHE: Check cache first (3 min TTL)
                var cacheKey = $"license:info:{currentUserEmail}";
                var cachedResult = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
                {
                    using var context = await _contextFactory.CreateDbContextAsync();

                    // ✅ Optimized query with WHERE and projection
                    var subusers = await context.subuser
                        .Where(s => s.user_email == currentUserEmail)
                        .Select(s => new
                        {
                            subuserEmail = s.subuser_email,
                            name = s.Name,
                            licenseAllocation = s.license_allocation ?? 0,
                            status = s.status
                        })
                        .ToListAsync();

                    var totalAllocated = subusers.Sum(s => s.licenseAllocation);
                    var activeSubusers = subusers.Count(s => s.status == "active");

                    // Get user's total license pool (from main user if available)
                    var userLicensePool = await context.subuser
                        .Where(s => s.subuser_email == currentUserEmail)
                        .Select(s => s.license_allocation ?? 0)
                        .FirstOrDefaultAsync();

                    if (userLicensePool == 0)
                    {
                        userLicensePool = totalAllocated + 100; // Placeholder
                    }

                    return new
                    {
                        success = true,
                        userEmail = currentUserEmail,
                        totalLicensePool = userLicensePool,
                        totalAllocated = totalAllocated,
                        availableLicenses = userLicensePool - totalAllocated,
                        subuserCount = subusers.Count,
                        activeSubusers = activeSubusers,
                        subusers = subusers,
                        cached = true
                    };
                }, TimeSpan.FromMinutes(3));

                return Ok(cachedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting license info");
                return StatusCode(500, new { message = "Error getting license info", error = ex.Message });
            }
        }

        /// <summary>
        /// Transfer/Revoke license allocation from one subuser to another
        /// Admin can revoke licenses from inactive subuser and assign to another
        /// </summary>
        [HttpPost("transfer-license")]
        public async Task<IActionResult> TransferLicense([FromBody] TransferLicenseRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

                // Validate request
                if (string.IsNullOrEmpty(request.ToSubuserEmail))
                    return BadRequest(new { message = "Target subuser email is required" });

                if (request.LicenseCount <= 0)
                    return BadRequest(new { message = "License count must be greater than 0" });

                // Find source subuser (if revoking from someone)
                subuser? fromSubuser = null;
                if (!string.IsNullOrEmpty(request.FromSubuserEmail))
                {
                    fromSubuser = await context.subuser
                        .Where(s => s.subuser_email == request.FromSubuserEmail 
                                                && s.user_email == currentUserEmail).FirstOrDefaultAsync();
                    
                    if (fromSubuser == null)
                        return NotFound(new { message = $"Source subuser '{request.FromSubuserEmail}' not found or not owned by you" });

                    if ((fromSubuser.license_allocation ?? 0) < request.LicenseCount)
                        return BadRequest(new { 
                            message = "Insufficient licenses to transfer",
                            available = fromSubuser.license_allocation ?? 0,
                            requested = request.LicenseCount
                        });
                }

                // Find target subuser
                var toSubuser = await context.subuser
                    .Where(s => s.subuser_email == request.ToSubuserEmail 
                                            && s.user_email == currentUserEmail).FirstOrDefaultAsync();

                if (toSubuser == null)
                    return NotFound(new { message = $"Target subuser '{request.ToSubuserEmail}' not found or not owned by you" });

                // Perform transfer
                if (fromSubuser != null)
                {
                    // Revoke from source
                    fromSubuser.license_allocation = (fromSubuser.license_allocation ?? 0) - request.LicenseCount;
                    fromSubuser.UpdatedAt = DateTime.UtcNow;
                }

                // Add to target
                toSubuser.license_allocation = (toSubuser.license_allocation ?? 0) + request.LicenseCount;
                toSubuser.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                _logger.LogInformation("License transfer: {Count} licenses from {From} to {To}",
                    request.LicenseCount, 
                    request.FromSubuserEmail ?? "pool",
                    request.ToSubuserEmail);

                return Ok(new
                {
                    success = true,
                    message = $"Successfully transferred {request.LicenseCount} license(s)",
                    fromSubuser = fromSubuser != null ? new
                    {
                        email = fromSubuser.subuser_email,
                        newAllocation = fromSubuser.license_allocation
                    } : null,
                    toSubuser = new
                    {
                        email = toSubuser.subuser_email,
                        newAllocation = toSubuser.license_allocation
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring license");
                return StatusCode(500, new { message = "Error transferring license", error = ex.Message });
            }
        }

        #region Private Helper Methods

        private async Task<bool> AssignRoleToSubuserAsync(string subuserEmail, string roleName, string assignedByEmail, ApplicationDbContext context)
        {
            try
            {
                var subuser = await context.subuser.Where(s => s.subuser_email == subuserEmail).FirstOrDefaultAsync();
                if (subuser == null)
                {
                    _logger.LogWarning("Subuser {Email} not found for role assignment", subuserEmail);
                    return false;
                }

                var role = await context.Roles.Where(r => r.RoleName == roleName).FirstOrDefaultAsync();
                if (role == null)
                {
                    _logger.LogWarning("Role {RoleName} not found, creating default User role instead", roleName);

                    // Fallback to default role if specified role doesn't exist
                    role = await context.Roles.Where(r => r.RoleName == "User").FirstOrDefaultAsync();
                    if (role == null)
                    {
                        _logger.LogError("No default role found in system");
                        return false;
                    }
                }

                // Check if role already assigned
                var existingRole = await context.SubuserRoles
               .Where(sr => sr.SubuserId == subuser.subuser_id && sr.RoleId == role.RoleId).FirstOrDefaultAsync();

                if (existingRole == null)
                {
                    var subuserRole = new SubuserRole
                    {
                        SubuserId = subuser.subuser_id,
                        RoleId = role.RoleId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedByEmail = assignedByEmail
                    };

                    context.SubuserRoles.Add(subuserRole);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Role {RoleName} assigned to subuser {Email}", role.RoleName, subuserEmail);
                    return true;
                }

                return true; // Already assigned
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleName} to subuser {Email}", roleName, subuserEmail);
                return false;
            }
        }

        #endregion
    }

    #region Request Models

    /// <summary>
    /// Subuser filter request model
    /// </summary>
    public class SubuserFilterRequest
    {
        public string? ParentUserEmail { get; set; }
        public string? SubuserEmail { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 100;
    }

    /// <summary>
    /// Subuser creation request model
    /// </summary>
    public class SubuserCreateRequest
    {
        [Required(ErrorMessage = "Subuser email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(255)]
        public string subuser_email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string subuser_password { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? subuser_name { get; set; }

        [MaxLength(100)]
        public string? department { get; set; }

        [MaxLength(50)]
        public string? role { get; set; }

        [MaxLength(20)]
        public string? phone { get; set; }

        [MaxLength(100)]
        public string? subuser_group { get; set; }

        public int? license_allocation { get; set; } = 0;

        [EmailAddress(ErrorMessage = "Invalid parent email format")]
        public string? parentUserEmail { get; set; }
    }

    /// <summary>
    /// Subuser update request model
    /// </summary>
    public class SubuserUpdateRequest
    {
        public string SubuserEmail { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SubuserName { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(50)]
        public string? Role { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? SubuserGroup { get; set; }

        public int? LicenseAllocation { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        public string? NewPassword { get; set; }
        public string? NewParentUserEmail { get; set; }
    }

    /// <summary>
    /// Flexible PATCH request model for partial subuser updates
    /// </summary>
    public class SubuserPatchRequest
    {
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? subuser_email { get; set; }

        [EmailAddress(ErrorMessage = "Invalid parent email format")]
        public string? parentUserEmail { get; set; }

        [EmailAddress(ErrorMessage = "Invalid new email format")]
        public string? new_subuser_email { get; set; }

        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string? subuser_password { get; set; }

        [MaxLength(100)]
        public string? subuser_name { get; set; }

        [MaxLength(100)]
        public string? department { get; set; }

        [MaxLength(50)]
        public string? role { get; set; }

        [MaxLength(20)]
        public string? phone { get; set; }

        [MaxLength(100)]
        public string? subuser_group { get; set; }

        [EmailAddress(ErrorMessage = "Invalid new parent email format")]
        public string? new_parentUserEmail { get; set; }

        /// <summary>
        /// License allocation for this subuser (can be updated by admin)
        /// </summary>
        public int? license_allocation { get; set; }

        /// <summary>
        /// Status of the subuser (active, inactive, suspended)
        /// </summary>
        [MaxLength(20)]
        public string? status { get; set; }
    }

    /// <summary>
    /// Change password request model
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Simple password change request
    /// </summary>
    public class SimplePasswordChangeRequest
    {
        [Required(ErrorMessage = "Subuser email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(255)]
        public string SubuserEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Assign role request model
    /// </summary>
    public class AssignRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Transfer license request model
    /// </summary>
    public class TransferLicenseRequest
    {
        /// <summary>
        /// Source subuser email (optional - if null, licenses from pool)
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid source email format")]
        public string? FromSubuserEmail { get; set; }

        /// <summary>
        /// Target subuser email (required)
        /// </summary>
        [Required(ErrorMessage = "Target subuser email is required")]
        [EmailAddress(ErrorMessage = "Invalid target email format")]
        public string ToSubuserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Number of licenses to transfer
        /// </summary>
        [Required(ErrorMessage = "License count is required")]
        [Range(1, 10000, ErrorMessage = "License count must be between 1 and 10000")]
        public int LicenseCount { get; set; }
    }

    #endregion
}