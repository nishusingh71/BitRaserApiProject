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
    /// Enhanced Machines management controller with email-based operations and role-based access control
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedMachinesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;

        public EnhancedMachinesController(ApplicationDbContext context, IRoleBasedAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        /// <summary>
        /// Get machines by user email with role-based filtering
        /// </summary>
        [HttpGet("by-email/{userEmail}")]
        [RequirePermission("READ_MACHINE")]
        public async Task<ActionResult<IEnumerable<object>>> GetMachinesByUserEmail(string userEmail, [FromQuery] MachineFilterRequest? filter)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Check if user can view machines for this email
            if (userEmail != currentUserEmail && !await CanManageUserAsync(currentUserEmail!, userEmail))
            {
                return StatusCode(403,new { error = "You can only view your own machines or machines of users you manage" });
            }

            IQueryable<machines> query = _context.Machines.Where(m => m.user_email == userEmail);

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

            var machines = await query
                .OrderByDescending(m => m.created_at)
                .Take(filter?.PageSize ?? 100)
                .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                .Select(m => new {
                    fingerprintHash = m.fingerprint_hash,
                    userEmail = m.user_email,
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

            return Ok(machines);
        }

        /// <summary>
        /// Get all machines with role-based filtering
        /// </summary>
        [HttpGet]
        [RequirePermission("READ_ALL_MACHINES")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllMachines([FromQuery] MachineFilterRequest? filter)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            IQueryable<machines> query = _context.Machines;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_MACHINES"))
            {
                // Users can only see their own machines unless they have elevated permissions
                if (await _authService.HasPermissionAsync(currentUserEmail!, "READ_MANAGED_USER_MACHINES"))
                {
                    var managedUserEmails = await GetManagedUserEmailsAsync(currentUserEmail!);
                    query = query.Where(m => managedUserEmails.Contains(m.user_email));
                }
                else
                {
                    query = query.Where(m => m.user_email == currentUserEmail);
                }
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
                .Take(filter?.PageSize ?? 100)
                .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                .Select(m => new {
                    fingerprintHash = m.fingerprint_hash,
                    userEmail = m.user_email,
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

            return Ok(machines);
        }

        /// <summary>
        /// Get machine by MAC address (email-based ownership validation)
        /// </summary>
        [HttpGet("by-mac/{macAddress}")]
        [AllowAnonymous] // Allow anonymous access for client validation
        public async Task<ActionResult<object>> GetMachineByMac(string macAddress)
        {
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == macAddress);
            
            if (machine == null) 
                return NotFound($"Machine with MAC address {macAddress} not found");

            // Return limited information for anonymous requests
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Ok(new {
                    macAddress = machine.mac_address,
                    userEmail = machine.user_email,
                    licenseActivated = machine.license_activated,
                    licenseActivationDate = machine.license_activation_date,
                    licenseDaysValid = machine.license_days_valid,
                    createdAt = machine.created_at
                });
            }

            // Full information for authenticated users
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Check if user can view this machine
            if (machine.user_email != currentUserEmail && 
                !await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_MACHINES"))
            {
                return StatusCode(403,new { error = "You can only view your own machines" });
            }

            return Ok(machine);
        }

        /// <summary>
        /// Register new machine for user email
        /// </summary>
        [HttpPost("register/{userEmail}")]
        [RequirePermission("CREATE_MACHINE")]
        public async Task<ActionResult<object>> RegisterMachine(string userEmail, [FromBody] MachineRegisterRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Check if user can register machine for this email
            if (userEmail != currentUserEmail && !await CanManageUserAsync(currentUserEmail!, userEmail))
            {
                return StatusCode(403,new { error = "You can only register machines for yourself or users you manage" });
            }

            // Validate that user exists
            var userExists = await _context.Users.AnyAsync(u => u.user_email == userEmail);
            if (!userExists)
                return BadRequest($"User with email {userEmail} not found");

            // Check if machine already exists
            var existingMachine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == request.MacAddress);
            if (existingMachine != null)
                return Conflict($"Machine with MAC address {request.MacAddress} already registered");

            // Create machine
            var newMachine = new machines
            {
                user_email = userEmail,
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

            var response = new {
                fingerprintHash = newMachine.fingerprint_hash,
                userEmail = newMachine.user_email,
                macAddress = newMachine.mac_address,
                licenseActivated = newMachine.license_activated,
                createdAt = newMachine.created_at,
                message = "Machine registered successfully"
            };

            return CreatedAtAction(nameof(GetMachineByMac), new { macAddress = newMachine.mac_address }, response);
        }

        /// <summary>
        /// Update machine by MAC address
        /// </summary>
        [HttpPut("by-mac/{macAddress}")]
        [RequirePermission("UPDATE_MACHINE")]
        public async Task<IActionResult> UpdateMachine(string macAddress, [FromBody] MachineUpdateRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == macAddress);
            
            if (machine == null) return NotFound($"Machine with MAC address {macAddress} not found");

            // Check if user can update this machine
            if (machine.user_email != currentUserEmail &&
                !await _authService.HasPermissionAsync(currentUserEmail!, "UPDATE_ALL_MACHINES"))
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

            return Ok(new { 
                message = "Machine updated successfully", 
                macAddress = macAddress,
                updatedAt = machine.updated_at
            });
        }

        /// <summary>
        /// Activate license for machine by MAC address
        /// </summary>
        [HttpPatch("by-mac/{macAddress}/activate-license")]
        [RequirePermission("MANAGE_MACHINE_LICENSES")]
        public async Task<IActionResult> ActivateLicense(string macAddress, [FromBody] LicenseActivationRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == macAddress);
            
            if (machine == null) return NotFound($"Machine with MAC address {macAddress} not found");

            // Check if user can manage licenses for this machine
            if (machine.user_email != currentUserEmail && 
                !await _authService.HasPermissionAsync(currentUserEmail!, "MANAGE_ALL_MACHINE_LICENSES"))
            {
                return StatusCode(403,new { error = "You can only manage licenses for your own machines" });
            }

            machine.license_activated = true;
            machine.license_activation_date = DateTime.UtcNow;
            machine.license_days_valid = request.DaysValid ?? 365;
            
            if (!string.IsNullOrEmpty(request.LicenseDetailsJson))
                machine.license_details_json = request.LicenseDetailsJson;

            machine.updated_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "License activated successfully", 
                macAddress = macAddress,
                userEmail = machine.user_email,
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
        [RequirePermission("MANAGE_MACHINE_LICENSES")]
        public async Task<IActionResult> DeactivateLicense(string macAddress)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == macAddress);
            
            if (machine == null) return NotFound($"Machine with MAC address {macAddress} not found");

            // Check permissions
            if (machine.user_email != currentUserEmail && 
                !await _authService.HasPermissionAsync(currentUserEmail!, "MANAGE_ALL_MACHINE_LICENSES"))
            {
                return StatusCode(403,new { error = "You can only manage licenses for your own machines" });
            }

            machine.license_activated = false;
            machine.updated_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "License deactivated successfully", 
                macAddress = macAddress,
                userEmail = machine.user_email,
                licenseActivated = machine.license_activated,
                deactivatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Delete machine by MAC address
        /// </summary>
        [HttpDelete("by-mac/{macAddress}")]
        [RequirePermission("DELETE_MACHINE")]
        public async Task<IActionResult> DeleteMachine(string macAddress)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == macAddress);
            
            if (machine == null) return NotFound($"Machine with MAC address {macAddress} not found");

            // Check permissions
            if (machine.user_email != currentUserEmail && 
                !await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_ALL_MACHINES"))
            {
                return StatusCode(403,new { error = "You can only delete your own machines" });
            }

            _context.Machines.Remove(machine);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Machine deleted successfully", 
                macAddress = macAddress,
                userEmail = machine.user_email,
                deletedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get machine statistics by user email
        /// </summary>
        [HttpGet("statistics/{userEmail}")]
        [RequirePermission("READ_MACHINE_STATISTICS")]
        public async Task<ActionResult<object>> GetMachineStatistics(string userEmail)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Check if user can view statistics for this email
            if (userEmail != currentUserEmail && !await CanManageUserAsync(currentUserEmail!, userEmail))
            {
                return StatusCode(403,new { error = "You can only view statistics for your own machines or machines you manage" });
            }

            var stats = new {
                UserEmail = userEmail,
                TotalMachines = await _context.Machines.CountAsync(m => m.user_email == userEmail),
                ActiveLicenses = await _context.Machines.CountAsync(m => m.user_email == userEmail && m.license_activated),
                InactiveLicenses = await _context.Machines.CountAsync(m => m.user_email == userEmail && !m.license_activated),
                ExpiredLicenses = await _context.Machines.CountAsync(m => 
                    m.user_email == userEmail && 
                    m.license_activation_date.HasValue && 
                    m.license_activation_date.Value.AddDays(m.license_days_valid) < DateTime.UtcNow),
                ExpiringInNext30Days = await _context.Machines.CountAsync(m => 
                    m.user_email == userEmail && 
                    m.license_activation_date.HasValue && 
                    m.license_activation_date.Value.AddDays(m.license_days_valid) > DateTime.UtcNow && 
                    m.license_activation_date.Value.AddDays(m.license_days_valid) <= DateTime.UtcNow.AddDays(30)),
                MachinesRegisteredToday = await _context.Machines.CountAsync(m => 
                    m.user_email == userEmail && m.created_at.Date == DateTime.UtcNow.Date),
                MachinesRegisteredThisWeek = await _context.Machines.CountAsync(m => 
                    m.user_email == userEmail && m.created_at >= DateTime.UtcNow.AddDays(-7)),
                MachinesRegisteredThisMonth = await _context.Machines.CountAsync(m => 
                    m.user_email == userEmail && m.created_at.Month == DateTime.UtcNow.Month),
                VmStatusDistribution = await _context.Machines
                    .Where(m => m.user_email == userEmail)
                    .GroupBy(m => m.vm_status)
                    .Select(g => new { VmStatus = g.Key, Count = g.Count() })
                    .ToListAsync(),
                OsVersionDistribution = await _context.Machines
                    .Where(m => m.user_email == userEmail)
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
            // Check if current user has admin permissions
            if (await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_MACHINES"))
                return true;

            // Check if current user can manage this specific user
            return await _authService.CanManageUserAsync(currentUserEmail, targetUserEmail);
        }

        private async Task<List<string>> GetManagedUserEmailsAsync(string managerEmail)
        {
            // Get users this manager can manage based on hierarchy
            var managedEmails = new List<string> { managerEmail };

            // Add logic to get users managed by this manager
            // This is a placeholder - implement based on your user hierarchy
            
            return managedEmails;
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

    #endregion
}