using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using BitRaserApiProject.Utilities; // ✅ ADD: For Base64EmailEncoder.DecodeEmailParam
using BitRaserApiProject.Factories; // ✅ ADDED

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Machines management controller with email-based operations and role-based access control
    /// Supports both users and subusers with appropriate access levels
    /// ✅ NOW SUPPORTS PRIVATE CLOUD ROUTING
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedMachinesController : ControllerBase
    {
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ITenantConnectionService _tenantService;
        private readonly ILogger<EnhancedMachinesController> _logger;
        private readonly ICacheService _cacheService;

        public EnhancedMachinesController(
                DynamicDbContextFactory contextFactory,
                    IRoleBasedAuthService authService,
            IUserDataService userDataService,
            ITenantConnectionService tenantService,
                ILogger<EnhancedMachinesController> logger,
                ICacheService cacheService)
        {
            _contextFactory = contextFactory;
            _authService = authService;
            _userDataService = userDataService;
            _tenantService = tenantService;
            _logger = logger;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Get machines by user email with role-based filtering
        /// Supports both users and subusers
        /// ✅ PERFORMANCE: Cached with 1-minute TTL
        /// </summary>
        [HttpGet("by-email/{userEmail}")]
        [DecodeEmail]
        public async Task<ActionResult<IEnumerable<object>>> GetMachinesByUserEmail(string userEmail, [FromQuery] MachineFilterRequest? filter)
        {
            try
            {
                // ✅ CRITICAL: Decode email before any usage
                var decodedEmail = Base64EmailEncoder.DecodeEmailParam(userEmail);
                
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

                // Allow access if:
                // 1. Requesting own machines (user or subuser)
                // 2. User has permission to view other machines
                // 3. Manager can view managed user machines
                bool canAccess = decodedEmail == currentUserEmail?.ToLower() ||
                    await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_MACHINES", isCurrentUserSubuser) ||
                    await CanManageUserAsync(currentUserEmail!, decodedEmail);

                if (!canAccess)
                {
                    return StatusCode(403, new { error = "You can only view your own machines or machines of users you manage" });
                }

                // ✅ PERFORMANCE: Build cache key with filter hash for uniqueness
                var filterHash = filter != null 
                    ? $"{filter.MacAddress}:{filter.LicenseActivated}:{filter.VmStatus}:{filter.RegisteredFrom}:{filter.RegisteredTo}:{filter.Page}:{filter.PageSize}".GetHashCode().ToString("X")
                    : "nofilter";
                var cacheKey = $"{CacheService.CacheKeys.Machine}:byemail:{decodedEmail}:{filterHash}";
                
                _logger.LogDebug("🔍 GetMachinesByUserEmail cache key: {Key}", cacheKey);

                var machines = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
                {
                    using var _context = await _contextFactory.CreateDbContextAsync();
                    
                    _logger.LogInformation("📊 DB FETCH: Machines for user: {Email}", decodedEmail);

                    IQueryable<machines> query = _context.Machines.AsNoTracking().Where(m => m.user_email.ToLower() == decodedEmail);  // ✅ RENDER OPTIMIZATION

                    // Apply additional filters if provided
                    if (filter != null)
                    {
                        if (!string.IsNullOrEmpty(filter.MacAddress))
                            query = query.Where(m => m.mac_address.Contains(filter.MacAddress));

                        if (filter.LicenseActivated.HasValue)
                            query = query.Where(m => m.license_activated == filter.LicenseActivated.Value);

                        if (!string.IsNullOrEmpty(filter.VmStatus))
                            query = query.Where(m => m.vm_status.Contains(filter.VmStatus));

                        if (filter.RegisteredFrom.HasValue)
                            query = query.Where(m => m.created_at >= filter.RegisteredFrom.Value);

                        if (filter.RegisteredTo.HasValue)
                            query = query.Where(m => m.created_at <= filter.RegisteredTo.Value);

                        if (filter.LicenseExpiringInDays.HasValue)
                        {
                            var expiryDate = DateTime.UtcNow.AddDays(filter.LicenseExpiringInDays.Value);
                            query = query.Where(m => m.license_activation_date.HasValue &&
                                   m.license_activation_date.Value.AddDays(m.license_days_valid) <= expiryDate);
                        }
                    }

                    return await query
                        .OrderByDescending(m => m.created_at)
                        .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                        .Take(filter?.PageSize ?? 100)
                        .Select(m => new
                        {
                            fingerprintHash = m.fingerprint_hash,
                            userEmail = m.user_email,
                            subuserEmail = m.subuser_email,
                            macAddress = m.mac_address,
                            osVersion = m.os_version,
                            licenseActivated = m.license_activated,
                            licenseActivationDate = m.license_activation_date,
                            licenseDaysValid = m.license_days_valid,
                            vmStatus = m.vm_status,
                            createdAt = m.created_at,
                            hasLicenseDetails = !string.IsNullOrEmpty(m.license_details_json) && m.license_details_json != "{}"
                        })
                        .ToListAsync();
                }, CacheService.CacheTTL.VeryShort);  // ✅ 1 minute TTL for real-time feel

                _logger.LogInformation("✅ Found {Count} machines for user: {Email}", machines.Count, decodedEmail);

                return Ok(machines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching machines for user {Email}", userEmail);
                return StatusCode(500, new { message = "Error retrieving machines", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all machines with role-based filtering
        /// </summary>
        /// ✅ SIMPLIFIED: No Org Admin concept - Simple hierarchical filtering
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllMachines([FromQuery] MachineFilterRequest? filter)
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

                IQueryable<machines> query = _context.Machines.AsNoTracking();  // ✅ RENDER OPTIMIZATION

                // ✅ SIMPLIFIED: Apply role-based filtering based on actual permissions
                bool hasGlobalAccess = await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_MACHINES", isCurrentUserSubuser);

                if (hasGlobalAccess)
                {
                    // ✅ SuperAdmin/Admin: Get their managed hierarchy (not entire DB)
                    var allManagedEmails = await GetAllManagedEmailsAsync(currentUserEmail!);
                    query = query.Where(m =>
               allManagedEmails.Contains(m.user_email) ||
             (m.subuser_email != null && allManagedEmails.Contains(m.subuser_email))
            );
                }
                else if (await _authService.HasPermissionAsync(currentUserEmail!, "READ_MANAGED_USER_MACHINES", isCurrentUserSubuser))
                {
                    // Manager: Get managed users + their subusers
                    var managedUserEmails = await GetManagedUserEmailsAsync(currentUserEmail!);
                    var managedSubuserEmails = await GetSubusersOfManagedUsersAsync(managedUserEmails);

                    var allManagedEmails = managedUserEmails.Concat(managedSubuserEmails).ToList();

                    query = query.Where(m =>
               allManagedEmails.Contains(m.user_email) ||
                  m.user_email == currentUserEmail ||
                   (m.subuser_email != null && allManagedEmails.Contains(m.subuser_email)) ||
                   m.subuser_email == currentUserEmail
                       );
                }
                else if (isCurrentUserSubuser)
                {
                    // ❌ Subuser - only own machines
                    query = query.Where(m => m.subuser_email == currentUserEmail);
                }
                else
                {
                    // ✅ Regular User - own machines + subuser machines
                    var subuserEmails = await _context.subuser
                        .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                        .Where(s => s.user_email == currentUserEmail)
                        .Select(s => s.subuser_email)
                        .ToListAsync();

                    query = query.Where(m =>
                       m.user_email == currentUserEmail ||  // Own machines
                 (m.subuser_email != null && subuserEmails.Contains(m.subuser_email))  // Subuser machines
                            );
                }

                // Apply additional filters if provided
                if (filter != null)
                {
                    if (!string.IsNullOrEmpty(filter.UserEmail))
                        query = query.Where(m => m.user_email.Contains(filter.UserEmail));

                    if (!string.IsNullOrEmpty(filter.MacAddress))
                        query = query.Where(m => m.mac_address.Contains(filter.MacAddress));

                    if (filter.LicenseActivated.HasValue)
                        query = query.Where(m => m.license_activated == filter.LicenseActivated.Value);

                    if (!string.IsNullOrEmpty(filter.VmStatus))
                        query = query.Where(m => m.vm_status.Contains(filter.VmStatus));

                    if (filter.RegisteredFrom.HasValue)
                        query = query.Where(m => m.created_at >= filter.RegisteredFrom.Value);

                    if (filter.RegisteredTo.HasValue)
                        query = query.Where(m => m.created_at <= filter.RegisteredTo.Value);
                }

                var machines = await query
                  .OrderByDescending(m => m.created_at)
                     .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                    .Take(filter?.PageSize ?? 100)
                     .Select(m => new
                     {
                         fingerprintHash = m.fingerprint_hash,
                         userEmail = m.user_email,
                         subuserEmail = m.subuser_email,
                         macAddress = m.mac_address,
                         osVersion = m.os_version,
                         licenseActivated = m.license_activated,
                         licenseActivationDate = m.license_activation_date,
                         licenseDaysValid = m.license_days_valid,
                         vmStatus = m.vm_status,
                         createdAt = m.created_at,
                         hasLicenseDetails = !string.IsNullOrEmpty(m.license_details_json) && m.license_details_json != "{}"
                     })
                       .ToListAsync();

                _logger.LogInformation("✅ Retrieved {Count} machines for {Email}", machines.Count, currentUserEmail);

                return Ok(machines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all machines");
                return StatusCode(500, new { message = "Error retrieving machines", error = ex.Message });
            }
        }
        /// <summary>
        /// Get machine by MAC address (email-based ownership validation)
        /// </summary>
        [HttpGet("by-mac/{macAddress}")]
        [AllowAnonymous] // Allow anonymous access for client validation
        public async Task<ActionResult<object>> GetMachineByMac(string macAddress)
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                var machine = await _context.Machines
                    .Where(m => m.mac_address == macAddress)
                    .FirstOrDefaultAsync();

                if (machine == null)
                {
                    _logger.LogWarning("Machine not found with MAC: {MacAddress}", macAddress);
                    return NotFound($"Machine with MAC address {macAddress} not found");
                }

                // Return limited information for anonymous requests
                if (!User.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("Anonymous request for machine MAC: {MacAddress}", macAddress);
                    return Ok(new
                    {
                        macAddress = machine.mac_address,
                        userEmail = machine.user_email,
                        subuserEmail = machine.subuser_email,
                        licenseActivated = machine.license_activated,
                        licenseActivationDate = machine.license_activation_date,
                        licenseDaysValid = machine.license_days_valid,
                        createdAt = machine.created_at
                    });
                }

                // Full information for authenticated users
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

                // Check if user can view this machine
                bool canAccess = machine.user_email == currentUserEmail ||
                 machine.subuser_email == currentUserEmail ||
           await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_MACHINES", isCurrentUserSubuser);

                if (!canAccess)
                {
                    _logger.LogWarning("Unauthorized access attempt for machine {MacAddress} by {Email}", macAddress, currentUserEmail);
                    return StatusCode(403, new { error = "You can only view your own machines" });
                }

                _logger.LogInformation("Retrieved machine {MacAddress} for {Email}", macAddress, currentUserEmail);
                return Ok(machine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching machine by MAC {MacAddress}", macAddress);
                return StatusCode(500, new { message = "Error retrieving machine", error = ex.Message });
            }
        }

        /// <summary>
        /// Register new machine for user or subuser email
        /// </summary>
        [HttpPost("register/{userEmail}")]
        [DecodeEmail]
        public async Task<ActionResult<object>> RegisterMachine(string userEmail, [FromBody] MachineRegisterRequest request)
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

                // Allow registration if:
                // 1. User registering for themselves
                // 2. Subuser registering for themselves 
                // 3. User has permission to register for others
                bool canRegister = userEmail == currentUserEmail ||
             await _authService.HasPermissionAsync(currentUserEmail!, "CREATE_MACHINE", isCurrentUserSubuser) ||
               await CanManageUserAsync(currentUserEmail!, userEmail);

                if (!canRegister)
                {
                    _logger.LogWarning("Unauthorized machine registration attempt by {CurrentEmail} for {TargetEmail}", currentUserEmail, userEmail);
                    return StatusCode(403, new { error = "You can only register machines for yourself or users you manage" });
                }

                // Validate that user exists (for users) or validate subuser exists (for subusers)
                bool targetExists = await _userDataService.UserExistsAsync(userEmail) ||
           await _userDataService.SubuserExistsAsync(userEmail);

                if (!targetExists)
                {
                    _logger.LogWarning("Attempted to register machine for non-existent user {Email}", userEmail);
                    return BadRequest($"User or subuser with email {userEmail} not found");
                }

                // Check if machine already exists
                var existingMachine = await _context.Machines
                    .Where(m => m.mac_address == request.MacAddress)
                    .FirstOrDefaultAsync();
                if (existingMachine != null)
                {
                    _logger.LogWarning("Attempted to register duplicate machine with MAC {MacAddress}", request.MacAddress);
                    return Conflict($"Machine with MAC address {request.MacAddress} already registered");
                }

                // Determine if the target is a subuser
                var targetIsSubuser = await _userDataService.SubuserExistsAsync(userEmail);

                // Create machine
                var newMachine = new machines
                {
                    user_email = targetIsSubuser ? (await _userDataService.GetSubuserByEmailAsync(userEmail))?.user_email ?? userEmail : userEmail,
                    subuser_email = targetIsSubuser ? userEmail : null,
                    mac_address = request.MacAddress,
                    fingerprint_hash = request.FingerprintHash,
                    physical_drive_id = request.PhysicalDriveId,
                    cpu_id = request.CpuId,
                    bios_serial = request.BiosSerial,
                    os_version = request.OsVersion,
                    license_activated = request.LicenseActivated ?? false,
                    license_activation_date = request.LicenseActivationDate,
                    license_days_valid = request.LicenseDaysValid ?? 0,
                    license_details_json = request.LicenseDetailsJson ?? "{}",
                    vm_status = request.VmStatus ?? "unknown",
                    demo_usage_count = 0,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                _context.Machines.Add(newMachine);
                await _context.SaveChangesAsync();

                // ✅ PERFORMANCE: Comprehensive cache invalidation using helper
                CacheInvalidation.InvalidateMachine(_cacheService, newMachine.user_email, newMachine.subuser_email, request.MacAddress);

                _logger.LogInformation("✅ Registered machine {MacAddress} for {UserEmail} in {DbType} database",
                request.MacAddress, userEmail, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                var response = new
                {
                    fingerprintHash = newMachine.fingerprint_hash,
                    userEmail = newMachine.user_email,
                    subuserEmail = newMachine.subuser_email,
                    macAddress = newMachine.mac_address,
                    licenseActivated = newMachine.license_activated,
                    createdAt = newMachine.created_at,
                    message = "Machine registered successfully"
                };

                return CreatedAtAction(nameof(GetMachineByMac), new { macAddress = newMachine.mac_address }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering machine for user {Email}", userEmail);
                return StatusCode(500, new { message = "Error registering machine", error = ex.Message });
            }
        }

        /// <summary>
        /// Update machine by MAC address
        /// </summary>
        [HttpPut("by-mac/{macAddress}")]
        public async Task<IActionResult> UpdateMachine(string macAddress, [FromBody] MachineUpdateRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            var machine = await _context.Machines
                .Where(m => m.mac_address == macAddress)
                .FirstOrDefaultAsync();

            if (machine == null) return NotFound($"Machine with MAC address {macAddress} not found");

            // Check if user can update this machine
            bool canUpdate = machine.user_email == currentUserEmail ||
                           machine.subuser_email == currentUserEmail ||
                           await _authService.HasPermissionAsync(currentUserEmail!, "UPDATE_ALL_MACHINES", isCurrentUserSubuser);

            if (!canUpdate)
            {
                return StatusCode(403, new { error = "You can only update your own machines" });
            }

            // Update machine information
            if (!string.IsNullOrEmpty(request.OsVersion))
                machine.os_version = request.OsVersion;

            if (!string.IsNullOrEmpty(request.VmStatus))
                machine.vm_status = request.VmStatus;

            if (!string.IsNullOrEmpty(request.LicenseDetailsJson))
                machine.license_details_json = request.LicenseDetailsJson;

            if (request.LicenseDaysValid.HasValue)
                machine.license_days_valid = request.LicenseDaysValid.Value;

            machine.updated_at = DateTime.UtcNow;

            _context.Entry(machine).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // ✅ CACHE INVALIDATION: Clear machine caches
            _cacheService.Remove($"{CacheService.CacheKeys.Machine}:{macAddress}");
            _cacheService.RemoveByPrefix($"{CacheService.CacheKeys.Machine}:{machine.user_email}");
            _cacheService.RemoveByPrefix(CacheService.CacheKeys.MachineList);

            return Ok(new
            {
                message = "Machine updated successfully",
                macAddress = macAddress,
                updatedAt = machine.updated_at
            });
        }

        /// <summary>
        /// Activate license for machine by MAC address
        /// </summary>
        [HttpPatch("by-mac/{macAddress}/activate-license")]
        public async Task<IActionResult> ActivateLicense(string macAddress, [FromBody] LicenseActivationRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            var machine = await _context.Machines
                .Where(m => m.mac_address == macAddress)
                .FirstOrDefaultAsync();

            if (machine == null) return NotFound($"Machine with MAC address {macAddress} not found");

            // Check if user can manage licenses for this machine
            bool canManageLicense = machine.user_email == currentUserEmail ||
                                  machine.subuser_email == currentUserEmail ||
                                  await _authService.HasPermissionAsync(currentUserEmail!, "MANAGE_ALL_MACHINE_LICENSES", isCurrentUserSubuser);

            if (!canManageLicense)
            {
                return StatusCode(403, new { error = "You can only manage licenses for your own machines" });
            }

            machine.license_activated = true;
            machine.license_activation_date = DateTime.UtcNow;
            machine.license_days_valid = request.DaysValid ?? 365;

            if (!string.IsNullOrEmpty(request.LicenseDetailsJson))
                machine.license_details_json = request.LicenseDetailsJson;

            machine.updated_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "License activated successfully",
                macAddress = macAddress,
                userEmail = machine.user_email,
                subuserEmail = machine.subuser_email,
                licenseActivated = machine.license_activated,
                activationDate = machine.license_activation_date,
                daysValid = machine.license_days_valid,
                activatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Deactivate license for machine by MAC address
        /// </summary>
        [HttpPatch("by-mac/{macAddress}/deactivate-license")]
        public async Task<IActionResult> DeactivateLicense(string macAddress)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            var machine = await _context.Machines
                .Where(m => m.mac_address == macAddress)
                .FirstOrDefaultAsync();

            if (machine == null) return NotFound($"Machine with MAC address {macAddress} not found");

            // Check permissions
            bool canManageLicense = machine.user_email == currentUserEmail ||
                                  machine.subuser_email == currentUserEmail ||
                                  await _authService.HasPermissionAsync(currentUserEmail!, "MANAGE_ALL_MACHINE_LICENSES", isCurrentUserSubuser);

            if (!canManageLicense)
            {
                return StatusCode(403, new { error = "You can only manage licenses for your own machines" });
            }

            machine.license_activated = false;
            machine.updated_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "License deactivated successfully",
                macAddress = macAddress,
                userEmail = machine.user_email,
                subuserEmail = machine.subuser_email,
                licenseActivated = machine.license_activated,
                deactivatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Delete machine by MAC address
        /// </summary>
        [HttpDelete("by-mac/{macAddress}")]
        public async Task<IActionResult> DeleteMachine(string macAddress)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            var machine = await _context.Machines
                .Where(m => m.mac_address == macAddress)
                .FirstOrDefaultAsync();

            if (machine == null) return NotFound($"Machine with MAC address {macAddress} not found");

            // Check permissions
            bool canDelete = machine.user_email == currentUserEmail ||
                           machine.subuser_email == currentUserEmail ||
                           await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_ALL_MACHINES", isCurrentUserSubuser);

            if (!canDelete)
            {
                return StatusCode(403, new { error = "You can only delete your own machines" });
            }

            _context.Machines.Remove(machine);
            await _context.SaveChangesAsync();

            // ✅ CACHE INVALIDATION: Clear machine caches
            _cacheService.Remove($"{CacheService.CacheKeys.Machine}:{macAddress}");
            _cacheService.RemoveByPrefix($"{CacheService.CacheKeys.Machine}:{machine.user_email}");
            _cacheService.RemoveByPrefix(CacheService.CacheKeys.MachineList);

            return Ok(new {
                message = "Machine deleted successfully",
                macAddress = macAddress,
                userEmail = machine.user_email,
                subuserEmail = machine.subuser_email,
                deletedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Initiate data erasure on a machine
        /// POST /api/EnhancedMachines/{macAddress}/erase
        /// </summary>
        [HttpPost("by-mac/{macAddress}/erase")]
        public async Task<IActionResult> InitiateErasure(string macAddress, [FromBody] ErasureRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync();

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
                return Unauthorized(new { error = "User not authenticated" });

            var machine = await _context.Machines
                .Where(m => m.mac_address == macAddress)
                .FirstOrDefaultAsync();

            if (machine == null)
                return NotFound(new { error = $"Machine with MAC address {macAddress} not found" });

            // Check permissions
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);
            bool canErase = machine.user_email == currentUserEmail ||
                           machine.subuser_email == currentUserEmail ||
                           await _authService.HasPermissionAsync(currentUserEmail, "ERASE_MACHINES", isCurrentUserSubuser);

            if (!canErase)
                return StatusCode(403, new { error = "You don't have permission to erase this machine" });

            // Create erasure session record
            var erasureSession = new Session
            {
                user_email = currentUserEmail,
                machine_id = machine.id,
                status = "pending_erasure",
                expiry_time = DateTime.UtcNow.AddHours(24),
                created_at = DateTime.UtcNow
            };

            _context.Sessions.Add(erasureSession);

            // Update machine status
            machine.status = "erasure_scheduled";
            machine.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Clear cache
            _cacheService.Remove($"{CacheService.CacheKeys.Machine}:{macAddress}");
            _cacheService.RemoveByPrefix(CacheService.CacheKeys.MachineList);

            _logger.LogInformation("🗑️ Erasure initiated for machine {Mac} by {User}, Method: {Method}",
                macAddress, currentUserEmail, request.ErasureMethod);

            return Ok(new
            {
                success = true,
                message = "Erasure initiated successfully",
                sessionId = erasureSession.id,
                macAddress = macAddress,
                erasureMethod = request.ErasureMethod ?? "DoD 5220.22-M",
                verifyAfter = request.VerifyAfter,
                scheduledAt = DateTime.UtcNow,
                status = "pending_erasure"
            });
        }

        /// <summary>
        /// Transfer machines to subuser
        /// Transfers selected machines from user to their subuser using subuser email
        /// </summary>
        /// <param name="request">Transfer request with subuser email and machine MAC addresses</param>
        [HttpPost("transfer-to-subuser")]
        public async Task<IActionResult> TransferMachinesToSubuser([FromBody] TransferMachinesRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync();

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Validate request
            if (string.IsNullOrEmpty(request.SubuserEmail))
            {
                return BadRequest(new { error = "Subuser email is required" });
            }

            if (request.MacAddresses == null || !request.MacAddresses.Any())
            {
                return BadRequest(new { error = "At least one MAC address is required" });
            }

            // Decode subuser email if Base64 encoded
            var decodedSubuserEmail = Base64EmailEncoder.DecodeEmailParam(request.SubuserEmail);

            // Check if subuser exists and belongs to current user
            var subuser = await _context.subuser
                .Where(s => s.subuser_email.ToLower() == decodedSubuserEmail.ToLower() 
                                       && s.user_email.ToLower() == currentUserEmail.ToLower()).FirstOrDefaultAsync();

            if (subuser == null)
            {
                return NotFound(new { error = $"Subuser with email '{decodedSubuserEmail}' not found or doesn't belong to you" });
            }

            // Get machines to transfer (only user's own machines)
            var macAddressList = request.MacAddresses.Select(m => m.ToLower()).ToList();
            var machinesToTransfer = await _context.Machines
                .Where(m => macAddressList.Contains(m.mac_address.ToLower()) 
                         && m.user_email.ToLower() == currentUserEmail.ToLower())
                .ToListAsync();

            if (!machinesToTransfer.Any())
            {
                return NotFound(new { error = "No machines found with the provided MAC addresses that belong to you" });
            }

            // Track transfer results
            var transferred = new List<string>();
            var alreadyAssigned = new List<string>();

            foreach (var machine in machinesToTransfer)
            {
                // Check if machine is already assigned to this subuser
                if (machine.subuser_email?.ToLower() == decodedSubuserEmail.ToLower())
                {
                    alreadyAssigned.Add(machine.mac_address);
                    continue;
                }

                // Transfer machine to subuser
                machine.subuser_email = decodedSubuserEmail;
                machine.updated_at = DateTime.UtcNow;
                transferred.Add(machine.mac_address);

                // Invalidate cache for this machine
                _cacheService.Remove($"{CacheService.CacheKeys.Machine}:{machine.mac_address}");
            }

            // Save changes
            if (transferred.Any())
            {
                await _context.SaveChangesAsync();
                
                // Invalidate user and subuser machine caches
                _cacheService.RemoveByPrefix($"{CacheService.CacheKeys.Machine}:{currentUserEmail}");
                _cacheService.RemoveByPrefix($"{CacheService.CacheKeys.Machine}:{decodedSubuserEmail}");
                _cacheService.RemoveByPrefix(CacheService.CacheKeys.MachineList);
            }

            // Identify MAC addresses not found
            var notFound = macAddressList
                .Except(machinesToTransfer.Select(m => m.mac_address.ToLower()))
                .ToList();

            return Ok(new
            {
                message = transferred.Any() 
                    ? $"Successfully transferred {transferred.Count} machine(s) to subuser" 
                    : "No machines were transferred",
                subuserEmail = decodedSubuserEmail,
                subuserName = subuser.Name ?? subuser.subuser_email,
                statistics = new
                {
                    totalRequested = request.MacAddresses.Count,
                    transferred = transferred.Count,
                    alreadyAssigned = alreadyAssigned.Count,
                    notFound = notFound.Count
                },
                details = new
                {
                    transferredMachines = transferred,
                    alreadyAssignedMachines = alreadyAssigned,
                    notFoundMachines = notFound
                },
                transferredAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get machine statistics by user email
        /// </summary>
        [HttpGet("statistics/{userEmail}")]
        public async Task<ActionResult<object>> GetMachineStatistics(string userEmail)
        {
            // ✅ CRITICAL: Decode email before any usage
            var decodedEmail = Base64EmailEncoder.DecodeEmailParam(userEmail);
            
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            // Check if user can view statistics for this email - use decoded email
            bool canViewStats = decodedEmail == currentUserEmail?.ToLower() ||
                              await _authService.HasPermissionAsync(currentUserEmail!, "READ_MACHINE_STATISTICS", isCurrentUserSubuser) ||
                              await CanManageUserAsync(currentUserEmail!, decodedEmail);

            if (!canViewStats)
            {
                return StatusCode(403, new { error = "You can only view statistics for your own machines or machines you manage" });
            }

            var stats = new
            {
                UserEmail = decodedEmail,
                TotalMachines = await _context.Machines.CountAsync(m => m.user_email.ToLower() == decodedEmail || (m.subuser_email != null && m.subuser_email.ToLower() == decodedEmail)),
                ActiveLicenses = await _context.Machines.CountAsync(m => (m.user_email.ToLower() == decodedEmail || (m.subuser_email != null && m.subuser_email.ToLower() == decodedEmail)) && m.license_activated),
                InactiveLicenses = await _context.Machines.CountAsync(m => (m.user_email.ToLower() == decodedEmail || (m.subuser_email != null && m.subuser_email.ToLower() == decodedEmail)) && !m.license_activated),
                ExpiredLicenses = await _context.Machines.CountAsync(m =>
                    (m.user_email == userEmail || m.subuser_email == userEmail) &&
                    m.license_activation_date.HasValue &&
                    m.license_activation_date.Value.AddDays(m.license_days_valid) < DateTime.UtcNow),
                ExpiringInNext30Days = await _context.Machines.CountAsync(m =>
                    (m.user_email == userEmail || m.subuser_email == userEmail) &&
                    m.license_activation_date.HasValue &&
                    m.license_activation_date.Value.AddDays(m.license_days_valid) > DateTime.UtcNow &&
                    m.license_activation_date.Value.AddDays(m.license_days_valid) <= DateTime.UtcNow.AddDays(30)),
                MachinesRegisteredToday = await _context.Machines.CountAsync(m =>
                    (m.user_email == userEmail || m.subuser_email == userEmail) && m.created_at.Date == DateTime.UtcNow.Date),
                MachinesRegisteredThisWeek = await _context.Machines.CountAsync(m =>
                    (m.user_email == userEmail || m.subuser_email == userEmail) && m.created_at >= DateTime.UtcNow.AddDays(-7)),
                MachinesRegisteredThisMonth = await _context.Machines.CountAsync(m =>
                    (m.user_email == userEmail || m.subuser_email == userEmail) && m.created_at.Month == DateTime.UtcNow.Month),
                VmStatusDistribution = await _context.Machines
                    .Where(m => m.user_email == userEmail || m.subuser_email == userEmail)
                    .GroupBy(m => m.vm_status)
                    .Select(g => new { VmStatus = g.Key, Count = g.Count() })
                    .ToListAsync(),
                OsVersionDistribution = await _context.Machines
                    .Where(m => m.user_email == userEmail || m.subuser_email == userEmail)
                    .GroupBy(m => m.os_version)
                    .Select(g => new { OsVersion = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync()
            };

            return Ok(stats);
        }

        #region Private Helper Methods

        private async Task<bool> CanManageUserAsync(string currentUserEmail, string targetUserEmail)
        {
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

            // Check if current user has admin permissions
            if (await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_MACHINES", isCurrentUserSubuser))
                return true;

            // Check if current user can manage this specific user
            return await _authService.CanManageUserAsync(currentUserEmail, targetUserEmail);
        }

        private async Task<List<string>> GetManagedUserEmailsAsync(string managerEmail)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            // Get users this manager can manage based on hierarchy
            var managedEmails = new List<string> { managerEmail };

            // Get direct subusers
            var subusers = await _context.subuser
         .Where(s => s.user_email == managerEmail)
              .Select(s => s.user_email)
        .Distinct()
         .ToListAsync();

            managedEmails.AddRange(subusers);

            return managedEmails;
        }

        /// <summary>
        /// Get ALL managed emails for SuperAdmin/Admin
        /// ✅ SIMPLIFIED: No Org Admin concept
        /// Returns: Admin's managed users + their subusers (NOT entire DB)
        /// </summary>
        private async Task<List<string>> GetAllManagedEmailsAsync(string adminEmail)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var managedEmails = new List<string> { adminEmail };

            // ✅ SIMPLE LOGIC:
            // For SuperAdmin with SYSTEM_ADMIN permission: Get all users
            // For regular Admin with READ_ALL_MACHINES: Get managed users only
            // TODO: Implement management hierarchy (e.g., created_by, managed_by fields)

            var isSystemSuperAdmin = await _authService.HasPermissionAsync(adminEmail, "SYSTEM_ADMIN");

            if (isSystemSuperAdmin)
            {
                // ✅ System SuperAdmin: Get ALL users (if truly needed for system-wide operations)
                var allUserEmails = await _context.Users
                   .Select(u => u.user_email)
               .ToListAsync();
                managedEmails.AddRange(allUserEmails);
            }
            else
            {
                // ✅ Regular Admin: Get users managed by this admin
                // Option 1: If you have created_by or managed_by field
                // var managedUsers = await _context.Users
                //     .Where(u => u.created_by == adminEmail || u.managed_by == adminEmail)
                //     .Select(u => u.user_email)
                //     .ToListAsync();
                // managedEmails.AddRange(managedUsers);

                // Option 2: For now, just return admin's own email (will be extended later)
                // TODO: Implement management hierarchy
                managedEmails.Add(adminEmail);
            }

            // Get all subusers of managed users
            var allSubusers = await _context.subuser
       .Where(s => managedEmails.Contains(s.user_email))
        .Select(s => s.subuser_email)
       .ToListAsync();
            managedEmails.AddRange(allSubusers);

            return managedEmails.Distinct().ToList();
        }

        /// <summary>
        /// Get subusers of managed users
        /// </summary>
        private async Task<List<string>> GetSubusersOfManagedUsersAsync(List<string> managedUserEmails)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            return await _context.subuser
                .Where(s => managedUserEmails.Contains(s.user_email))
               .Select(s => s.subuser_email)
                 .ToListAsync();
        }

        #endregion

    }

    #region Request Models

    /// <summary>
    /// Machine filter request model
    /// </summary>
    public class MachineFilterRequest
    {
        public string? UserEmail { get; set; }
        public string? MacAddress { get; set; }
        public bool? LicenseActivated { get; set; }
        public string? VmStatus { get; set; }
        public DateTime? RegisteredFrom { get; set; }
        public DateTime? RegisteredTo { get; set; }
        public int? LicenseExpiringInDays { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 100;
    }

    /// <summary>
    /// Machine registration request model
    /// </summary>
    public class MachineRegisterRequest
    {
        public string MacAddress { get; set; } = string.Empty;
        public string FingerprintHash { get; set; } = string.Empty;
        public string PhysicalDriveId { get; set; } = string.Empty;
        public string CpuId { get; set; } = string.Empty;
        public string BiosSerial { get; set; } = string.Empty;
        public string OsVersion { get; set; } = string.Empty;
        public bool? LicenseActivated { get; set; }
        public DateTime? LicenseActivationDate { get; set; }
        public int? LicenseDaysValid { get; set; }
        public string? LicenseDetailsJson { get; set; }
        public string? VmStatus { get; set; }
    }

    /// <summary>
    /// Machine update request model
    /// </summary>
    public class MachineUpdateRequest
    {
        public string? OsVersion { get; set; }
        public string? VmStatus { get; set; }
        public string? LicenseDetailsJson { get; set; }
        public int? LicenseDaysValid { get; set; }
    }

    /// <summary>
    /// License activation request model
    /// </summary>
    public class LicenseActivationRequest
    {
        public int? DaysValid { get; set; }
        public string? LicenseDetailsJson { get; set; }
    }
    /// <summary>
    /// Transfer machines to subuser request model
    /// </summary>
    public class TransferMachinesRequest
    {
        /// <summary>
        /// Target subuser email (can be Base64 encoded)
        /// </summary>
        public string SubuserEmail { get; set; } = string.Empty;
        
        /// <summary>
        /// List of MAC addresses to transfer
        /// </summary>
        public List<string> MacAddresses { get; set; } = new();
    }

    /// <summary>
    /// Erasure request model for initiating data erasure
    /// </summary>
    public class ErasureRequest
    {
        /// <summary>
        /// Erasure method to use (e.g., "DoD 5220.22-M", "NIST 800-88", "Gutmann")
        /// </summary>
        public string? ErasureMethod { get; set; } = "DoD 5220.22-M";
        
        /// <summary>
        /// Whether to verify erasure after completion
        /// </summary>
        public bool VerifyAfter { get; set; } = true;
        
        /// <summary>
        /// Optional notes for the erasure operation
        /// </summary>
        public string? Notes { get; set; }
    }

    #endregion
}