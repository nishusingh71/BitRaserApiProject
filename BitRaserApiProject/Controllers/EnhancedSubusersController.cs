using System.Security.Claims;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Subusers management controller with comprehensive user name and role information
    /// Supports email-based operations and role-based access control
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedSubusersController : ControllerBase
    {
  private readonly ApplicationDbContext _context;
   private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;

        public EnhancedSubusersController(
    ApplicationDbContext context,
         IRoleBasedAuthService authService,
      IUserDataService userDataService)
     {
_context = context;
    _authService = authService;
       _userDataService = userDataService;
        }

        /// <summary>
        /// Get all subusers with user name and roles
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllSubusers(
      [FromQuery] string? userEmail = null,
            [FromQuery] string? subuserEmail = null,
       [FromQuery] string? name = null,
[FromQuery] string? status = null,
            [FromQuery] string? role = null,
          [FromQuery] int page = 0,
            [FromQuery] int pageSize = 100)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
       
      IQueryable<subuser> query = _context.subuser
      .Include(s => s.SubuserRoles)
           .ThenInclude(sr => sr.Role);

            // Apply role-based filtering
 if (!await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser))
            {
     // Users can only see their own subusers
         query = query.Where(s => s.user_email == currentUserEmail);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(userEmail))
        query = query.Where(s => s.user_email.Contains(userEmail));

 if (!string.IsNullOrEmpty(subuserEmail))
        query = query.Where(s => s.subuser_email.Contains(subuserEmail));

   if (!string.IsNullOrEmpty(name))
       query = query.Where(s => s.Name != null && s.Name.Contains(name));

       if (!string.IsNullOrEmpty(status))
       query = query.Where(s => s.status != null && s.status.Contains(status));

  if (!string.IsNullOrEmpty(role))
     query = query.Where(s => s.Role.Contains(role));

    var subusers = await query
 .OrderByDescending(s => s.CreatedAt)
      .Skip(page * pageSize)
        .Take(pageSize)
           .ToListAsync();

     var subuserDetails = subusers.Select(s => new {
   s.subuser_id,
 s.subuser_email,
  s.user_email, // Parent user email
    name = s.Name ?? "N/A",
phone = s.Phone ?? "N/A",
      department = s.Department ?? "N/A",
      role = s.Role ?? "N/A",
    status = s.status ?? "active",
    last_login = s.last_login,
  subuser_group = s.GroupId.HasValue ? 
  _context.Set<Group>().Where(g => g.group_id == s.GroupId.Value).Select(g => g.name).FirstOrDefault() ?? "No Group" 
    : "No Group",
isEmailVerified = s.IsEmailVerified,
  assignedMachines = s.AssignedMachines ?? 0,
maxMachines = s.MaxMachines ?? 5,
        license_allocation = s.license_allocation ?? 0, // ✅ Added
     // Roles from SubuserRoles relationship
     roles = s.SubuserRoles.Select(sr => new {
   roleId = sr.RoleId,
   roleName = sr.Role.RoleName,
description = sr.Role.Description,
  hierarchyLevel = sr.Role.HierarchyLevel,
 assignedAt = sr.AssignedAt,
assignedBy = sr.AssignedByEmail
}).ToList(),
createdAt = s.CreatedAt,
        updatedAt = s.UpdatedAt,
 lastLoginIp = s.LastLoginIp ?? "N/A"
     }).ToList();

     return Ok(subuserDetails);
        }

        /// <summary>
    /// Get subuser by email with full details including name and roles
        /// </summary>
      [HttpGet("by-email/{email}")]
        public async Task<ActionResult<object>> GetSubuserByEmail(string email)
  {
     var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
    
      var subuser = await _context.subuser
       .Include(s => s.SubuserRoles)
    .ThenInclude(sr => sr.Role)
    .ThenInclude(r => r.RolePermissions)
    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(s => s.subuser_email == email);
     
       if (subuser == null) 
                return NotFound($"Subuser with email {email} not found");

            // Check if user can view this subuser
bool canView = subuser.user_email == currentUserEmail ||
     await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser);

       if (!canView)
    {
                return StatusCode(403, new { error = "You can only view your own subusers" });
            }

     var subuserDetails = new {
subuser.subuser_id,
  subuser.subuser_email,
     subuser.user_email,
   name = subuser.Name ?? "N/A",
   phone = subuser.Phone ?? "N/A",
   department = subuser.Department ?? "N/A",
    role = subuser.Role ?? "N/A",
    status = subuser.status ?? "active",
    last_login = subuser.last_login,
    subuser_group = subuser.GroupId.HasValue ? 
 _context.Set<Group>().Where(g => g.group_id == subuser.GroupId.Value).Select(g => g.name).FirstOrDefault() ?? "No Group"
: "No Group",
       isEmailVerified = subuser.IsEmailVerified,
 // Detailed roles information
  roles = subuser.SubuserRoles.Select(sr => new {
    roleId = sr.RoleId,
    roleName = sr.Role.RoleName,
description = sr.Role.Description,
 hierarchyLevel = sr.Role.HierarchyLevel,
 assignedAt = sr.AssignedAt,
    assignedBy = sr.AssignedByEmail
  }).ToList(),
  // Permissions from roles
   permissions = subuser.SubuserRoles
    .SelectMany(sr => sr.Role.RolePermissions)
   .Select(rp => rp.Permission.PermissionName)
  .Distinct()
   .ToList(),
  // Machine and license info
   assignedMachines = subuser.AssignedMachines ?? 0,
          maxMachines = subuser.MaxMachines ?? 5,
    license_allocation = subuser.license_allocation ?? 0, // ✅ Added
  groupId = subuser.GroupId,
// Permissions flags
    canCreateSubusers = subuser.CanCreateSubusers,
     canViewReports = subuser.CanViewReports,
canManageMachines = subuser.CanManageMachines,
   canAssignLicenses = subuser.CanAssignLicenses,
// Notifications
emailNotifications = subuser.EmailNotifications,
  systemAlerts = subuser.SystemAlerts,
     // Security info
   lastLoginIp = subuser.LastLoginIp ?? "N/A",
  failedLoginAttempts = subuser.FailedLoginAttempts,
    lockedUntil = subuser.LockedUntil,
  // Audit info
   createdAt = subuser.CreatedAt,
   createdBy = subuser.CreatedBy,
   updatedAt = subuser.UpdatedAt,
   updatedBy = subuser.UpdatedBy,
 notes = subuser.Notes ?? ""
  };

    return Ok(subuserDetails);
      }

    /// <summary>
        /// Get subusers by parent user email
   /// </summary>
        [HttpGet("by-parent/{parentEmail}")]
        public async Task<ActionResult<IEnumerable<object>>> GetSubusersByParent(string parentEmail)
    {
  var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
     
       // Check if user can view subusers for this parent
    bool canView = parentEmail == currentUserEmail ||
          await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser);

   if (!canView)
         {
   return StatusCode(403, new { error = "You can only view your own subusers" });
    }

            var subusers = await _context.subuser
      .Include(s => s.SubuserRoles)
       .ThenInclude(sr => sr.Role)
         .Where(s => s.user_email == parentEmail)
   .OrderByDescending(s => s.CreatedAt)
       .ToListAsync();

            var subuserDetails = subusers.Select(s => new {
    s.subuser_id,
  s.subuser_email,
    name = s.Name ?? "N/A",
  phone = s.Phone ?? "N/A",
  department = s.Department ?? "N/A",
  role = s.Role ?? "N/A",
       status = s.status ?? "active",
 last_login = s.last_login,
   subuser_group = s.GroupId.HasValue ? 
 _context.Set<Group>().Where(g => g.group_id == s.GroupId.Value).Select(g => g.name).FirstOrDefault() ?? "No Group"
        : "No Group",
 // Roles information
       roles = s.SubuserRoles.Select(sr => new {
  roleId = sr.RoleId,
    roleName = sr.Role.RoleName,
 hierarchyLevel = sr.Role.HierarchyLevel
 }).ToList(),
   assignedMachines = s.AssignedMachines ?? 0,
  maxMachines = s.MaxMachines ?? 5,
  isEmailVerified = s.IsEmailVerified,
   createdAt = s.CreatedAt
 }).ToList();

   return Ok(subuserDetails);
        }

    /// <summary>
        /// Create new subuser with name and role assignment
        /// </summary>
        [HttpPost]
      [RequirePermission("CREATE_SUBUSER")]
        public async Task<ActionResult<object>> CreateSubuser([FromBody] CreateSubuserDto request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
         if (!await _authService.HasPermissionAsync(currentUserEmail!, "CREATE_SUBUSER", isCurrentUserSubuser))
       return StatusCode(403, new { error = "Insufficient permissions to create subusers" });

       // Check if subuser already exists
     var existingSubuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == request.Email);
        if (existingSubuser != null)
     return Conflict($"Subuser with email {request.Email} already exists");

            // Get parent user
            var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
      if (parentUser == null)
           return BadRequest("Parent user not found");

            // Create subuser with name
 var newSubuser = new subuser
      {
subuser_email = request.Email,
  subuser_password = BCrypt.Net.BCrypt.HashPassword(request.Password),
      user_email = currentUserEmail!,
   superuser_id = parentUser.user_id,
  Name = request.Name,
  Phone = request.Phone ?? "",
    Department = request.Department ?? "",
  Role = request.Role ?? "subuser",
  status = "active",
    IsEmailVerified = false,
   MaxMachines = request.MaxMachines ?? 5,
   GroupId = request.GroupId,
        license_allocation = request.LicenseAllocation ?? 0, // ✅ Added
      CanCreateSubusers = request.CanCreateSubusers ?? false,
 CanViewReports = request.CanViewReports ?? true,
        CanManageMachines = request.CanManageMachines ?? false,
    CanAssignLicenses = request.CanAssignLicenses ?? false,
    EmailNotifications = request.EmailNotifications ?? true,
     SystemAlerts = request.SystemAlerts ?? true,
       CreatedBy = parentUser.user_id,
       CreatedAt = DateTime.UtcNow,
   Notes = request.Notes
    };

   _context.subuser.Add(newSubuser);
            await _context.SaveChangesAsync();

       // Assign default SubUser role
   await AssignRoleToSubuserAsync(newSubuser.subuser_email, "SubUser", currentUserEmail!);

            // Reload with roles
            var createdSubuser = await _context.subuser
   .Include(s => s.SubuserRoles)
                .ThenInclude(sr => sr.Role)
       .FirstOrDefaultAsync(s => s.subuser_id == newSubuser.subuser_id);

          var response = new {
      subuser_id = createdSubuser!.subuser_id,
    subuser_email = createdSubuser.subuser_email,
         name = createdSubuser.Name,
       phone = createdSubuser.Phone,
              department = createdSubuser.Department,
      role = createdSubuser.Role,
          roles = createdSubuser.SubuserRoles.Select(sr => new {
    roleName = sr.Role.RoleName,
                    hierarchyLevel = sr.Role.HierarchyLevel
                }).ToList(),
        createdAt = createdSubuser.CreatedAt,
       message = "Subuser created successfully"
 };

          return CreatedAtAction(nameof(GetSubuserByEmail), new { email = newSubuser.subuser_email }, response);
        }

   /// <summary>
        /// Update subuser details including name
      /// </summary>
        [HttpPut("{email}")]
        [RequirePermission("UPDATE_SUBUSER")]
        public async Task<IActionResult> UpdateSubuser(string email, [FromBody] UpdateSubuserDto request)
    {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
     
 if (subuser == null) return NotFound($"Subuser with email {email} not found");

    // Check if user can update this subuser
 bool canUpdate = subuser.user_email == currentUserEmail ||
      await _authService.HasPermissionAsync(currentUserEmail!, "UPDATE_ALL_SUBUSERS", isCurrentUserSubuser);

       if (!canUpdate)
{
             return StatusCode(403, new { error = "You can only update your own subusers" });
     }

     // Update subuser information
            if (!string.IsNullOrEmpty(request.Name))
    subuser.Name = request.Name;

     if (!string.IsNullOrEmpty(request.Phone))
        subuser.Phone = request.Phone;

      if (!string.IsNullOrEmpty(request.Department))
     subuser.Department = request.Department;

 if (!string.IsNullOrEmpty(request.Status))
    subuser.status = request.Status;

        if (request.MaxMachines.HasValue)
           subuser.MaxMachines = request.MaxMachines.Value;

      if (request.CanViewReports.HasValue)
     subuser.CanViewReports = request.CanViewReports.Value;

          if (request.CanManageMachines.HasValue)
       subuser.CanManageMachines = request.CanManageMachines.Value;

       if (request.CanAssignLicenses.HasValue)
       subuser.CanAssignLicenses = request.CanAssignLicenses.Value;

   if (!string.IsNullOrEmpty(request.Notes))
      subuser.Notes = request.Notes;

        var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
  if (parentUser != null)
            {
        subuser.UpdatedBy = parentUser.user_id;
      }
            subuser.UpdatedAt = DateTime.UtcNow;

     _context.Entry(subuser).State = EntityState.Modified;
         await _context.SaveChangesAsync();

        return Ok(new { 
         message = "Subuser updated successfully", 
     subuser_email = email,
      name = subuser.Name,
  updatedAt = subuser.UpdatedAt
 });
 }

        /// <summary>
   /// PATCH: Partial update subuser details by email
        /// Updates only the fields provided in the request
        /// </summary>
        [HttpPatch("{email}")]
        [RequirePermission("UPDATE_SUBUSER")]
        public async Task<IActionResult> PatchSubuser(string email, [FromBody] UpdateSubuserDto request)
   {
          try
 {
           var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
     
       var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
        
  if (subuser == null)
 return NotFound(new { 
success = false,
     message = $"Subuser with email '{email}' not found" 
});

  // Check if user can update this subuser
      bool canUpdate = subuser.user_email == currentUserEmail ||
         await _authService.HasPermissionAsync(currentUserEmail!, "UPDATE_ALL_SUBUSERS", isCurrentUserSubuser);

  if (!canUpdate)
{
   return StatusCode(403, new { 
        success = false,
    error = "You can only update your own subusers" 
   });
         }

  // Track which fields were updated
             var updatedFields = new List<string>();

   // ✅ Partial update - only update fields that are provided
          if (!string.IsNullOrEmpty(request.SubuserUsername))
     {
          subuser.subuser_username = request.SubuserUsername;
            updatedFields.Add("SubuserUsername");
        }

        if (!string.IsNullOrEmpty(request.Name))
           {
              subuser.Name = request.Name;
    updatedFields.Add("Name");
       }

          if (!string.IsNullOrEmpty(request.Phone))
  {
         subuser.Phone = request.Phone;
       updatedFields.Add("Phone");
    }

       if (!string.IsNullOrEmpty(request.Department))
    {
      subuser.Department = request.Department;
   updatedFields.Add("Department");
      }

 if (!string.IsNullOrEmpty(request.Role))
    {
      subuser.Role = request.Role;
   updatedFields.Add("Role");
      }

        if (!string.IsNullOrEmpty(request.PermissionsJson))
        {
     subuser.PermissionsJson = request.PermissionsJson;
      updatedFields.Add("PermissionsJson");
        }

        if (request.AssignedMachines.HasValue)
   {
            subuser.AssignedMachines = request.AssignedMachines.Value;
            updatedFields.Add("AssignedMachines");
        }

       if (!string.IsNullOrEmpty(request.Status))
        {
   subuser.status = request.Status;
     updatedFields.Add("Status");
      }

        if (request.MaxMachines.HasValue)
    {
  subuser.MaxMachines = request.MaxMachines.Value;
  updatedFields.Add("MaxMachines");
     }

        if (!string.IsNullOrEmpty(request.MachineIdsJson))
        {
            subuser.MachineIdsJson = request.MachineIdsJson;
       updatedFields.Add("MachineIdsJson");
        }

        if (!string.IsNullOrEmpty(request.LicenseIdsJson))
        {
     subuser.LicenseIdsJson = request.LicenseIdsJson;
     updatedFields.Add("LicenseIdsJson");
        }

         if (request.GroupId.HasValue)
{
    subuser.GroupId = request.GroupId.Value;
 updatedFields.Add("GroupId");
    }

    if (!string.IsNullOrEmpty(request.SubuserGroup))
        {
       subuser.subuser_group = request.SubuserGroup;
   updatedFields.Add("SubuserGroup");
        }

 if (request.LicenseAllocation.HasValue)
     {
   subuser.license_allocation = request.LicenseAllocation.Value;
            updatedFields.Add("LicenseAllocation");
  }

        if (!string.IsNullOrEmpty(request.Timezone))
      {
subuser.timezone = request.Timezone;
            updatedFields.Add("Timezone");
}

        if (request.IsEmailVerified.HasValue)
        {
      subuser.IsEmailVerified = request.IsEmailVerified.Value;
            updatedFields.Add("IsEmailVerified");
    }

     if (request.CanViewReports.HasValue)
    {
    subuser.CanViewReports = request.CanViewReports.Value;
  updatedFields.Add("CanViewReports");
        }

      if (request.CanManageMachines.HasValue)
    {
           subuser.CanManageMachines = request.CanManageMachines.Value;
  updatedFields.Add("CanManageMachines");
        }

  if (request.CanAssignLicenses.HasValue)
    {
      subuser.CanAssignLicenses = request.CanAssignLicenses.Value;
     updatedFields.Add("CanAssignLicenses");
    }

     if (request.CanCreateSubusers.HasValue)
    {
       subuser.CanCreateSubusers = request.CanCreateSubusers.Value;
  updatedFields.Add("CanCreateSubusers");
        }

if (request.EmailNotifications.HasValue)
  {
     subuser.EmailNotifications = request.EmailNotifications.Value;
       updatedFields.Add("EmailNotifications");
    }

    if (request.SystemAlerts.HasValue)
         {
 subuser.SystemAlerts = request.SystemAlerts.Value;
         updatedFields.Add("SystemAlerts");
     }

      if (!string.IsNullOrEmpty(request.Notes))
 {
     subuser.Notes = request.Notes;
 updatedFields.Add("Notes");
     }

    // Update audit fields
        var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
     if (parentUser != null)
       {
         subuser.UpdatedBy = parentUser.user_id;
 }
         subuser.UpdatedAt = DateTime.UtcNow;

    // Save changes
        _context.Entry(subuser).State = EntityState.Modified;
    await _context.SaveChangesAsync();

  // Get group name if updated
    string? groupName = null;
 if (subuser.GroupId.HasValue)
    {
         var group = await _context.Set<Group>().FindAsync(subuser.GroupId.Value);
          groupName = group?.name;
}

    return Ok(new { 
      success = true,
  message = "Subuser updated successfully",
        subuser_email = email,
            updatedFields = updatedFields,
   updatedBy = currentUserEmail,
      updatedAt = subuser.UpdatedAt,
          // Return updated data
            subuser = new {
      subuser_email = subuser.subuser_email,
        subuser_username = subuser.subuser_username,
  name = subuser.Name,
   phone = subuser.Phone,
     department = subuser.Department,
     role = subuser.Role,
      permissionsJson = subuser.PermissionsJson,
    assignedMachines = subuser.AssignedMachines,
      maxMachines = subuser.MaxMachines,
                machineIdsJson = subuser.MachineIdsJson,
        licenseIdsJson = subuser.LicenseIdsJson,
 status = subuser.status,
     groupId = subuser.GroupId,
  groupName = groupName,
         subuserGroup = subuser.subuser_group,
    license_allocation = subuser.license_allocation,
          timezone = subuser.timezone,
          isEmailVerified = subuser.IsEmailVerified,
    canViewReports = subuser.CanViewReports,
        canManageMachines = subuser.CanManageMachines,
    canAssignLicenses = subuser.CanAssignLicenses,
  canCreateSubusers = subuser.CanCreateSubusers,
 emailNotifications = subuser.EmailNotifications,
    systemAlerts = subuser.SystemAlerts,
   notes = subuser.Notes,
    updatedAt = subuser.UpdatedAt
     }
      });
}
            catch (Exception ex)
    {
       return StatusCode(500, new { 
   success = false,
         message = "Error updating subuser", 
       error = ex.Message 
      });
      }
        }

        /// <summary>
        /// PATCH: Update subuser by parent email and subuser email
        /// Allows updating subusers using parent user context
    /// </summary>
   [HttpPatch("by-parent/{parentEmail}/subuser/{subuserEmail}")]
     [RequirePermission("UPDATE_SUBUSER")]
        public async Task<IActionResult> PatchSubuserByParent(
  string parentEmail, 
          string subuserEmail, 
            [FromBody] UpdateSubuserDto request)
        {
            try
            {
     var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
  
           // Check if current user can manage this parent's subusers
     bool canManageParent = parentEmail == currentUserEmail ||
   await _authService.HasPermissionAsync(currentUserEmail!, "UPDATE_ALL_SUBUSERS", isCurrentUserSubuser);

       if (!canManageParent)
      {
  return StatusCode(403, new { 
     success = false,
   error = $"You cannot manage subusers for {parentEmail}" 
     });
        }

                // Find subuser by both parent and subuser email
    var subuser = await _context.subuser
             .FirstOrDefaultAsync(s => s.user_email == parentEmail && s.subuser_email == subuserEmail);
     
            if (subuser == null)
   {
 return NotFound(new { 
success = false,
        message = $"Subuser '{subuserEmail}' not found under parent '{parentEmail}'" 
         });
           }

          // Track updated fields
        var updatedFields = new List<string>();

     // ✅ Partial update logic (same as above)
     if (!string.IsNullOrEmpty(request.SubuserUsername))
      {
         subuser.subuser_username = request.SubuserUsername;
   updatedFields.Add("SubuserUsername");
 }

          if (!string.IsNullOrEmpty(request.Name))
    {
   subuser.Name = request.Name;
  updatedFields.Add("Name");
      }

     if (!string.IsNullOrEmpty(request.Phone))
            {
        subuser.Phone = request.Phone;
          updatedFields.Add("Phone");
             }

         if (!string.IsNullOrEmpty(request.Department))
   {
       subuser.Department = request.Department;
          updatedFields.Add("Department");
  }

    if (!string.IsNullOrEmpty(request.Role))
         {
     subuser.Role = request.Role;
updatedFields.Add("Role");
  }

        if (!string.IsNullOrEmpty(request.PermissionsJson))
      {
   subuser.PermissionsJson = request.PermissionsJson;
    updatedFields.Add("PermissionsJson");
   }

 if (request.AssignedMachines.HasValue)
        {
      subuser.AssignedMachines = request.AssignedMachines.Value;
    updatedFields.Add("AssignedMachines");
        }

    if (!string.IsNullOrEmpty(request.Status))
         {
     subuser.status = request.Status;
   updatedFields.Add("Status");
    }

           if (request.MaxMachines.HasValue)
  {
   subuser.MaxMachines = request.MaxMachines.Value;
        updatedFields.Add("MaxMachines");
    }

  if (!string.IsNullOrEmpty(request.MachineIdsJson))
        {
         subuser.MachineIdsJson = request.MachineIdsJson;
      updatedFields.Add("MachineIdsJson");
  }

   if (!string.IsNullOrEmpty(request.LicenseIdsJson))
        {
   subuser.LicenseIdsJson = request.LicenseIdsJson;
  updatedFields.Add("LicenseIdsJson");
  }

       if (request.GroupId.HasValue)
     {
        subuser.GroupId = request.GroupId.Value;
         updatedFields.Add("GroupId");
 }

  if (!string.IsNullOrEmpty(request.SubuserGroup))
 {
     subuser.subuser_group = request.SubuserGroup;
   updatedFields.Add("SubuserGroup");
  }

      if (request.LicenseAllocation.HasValue)
        {
          subuser.license_allocation = request.LicenseAllocation.Value;
 updatedFields.Add("LicenseAllocation");
    }

   if (!string.IsNullOrEmpty(request.Timezone))
        {
   subuser.timezone = request.Timezone;
       updatedFields.Add("Timezone");
   }

   if (request.IsEmailVerified.HasValue)
        {
   subuser.IsEmailVerified = request.IsEmailVerified.Value;
    updatedFields.Add("IsEmailVerified");
 }

   if (request.CanViewReports.HasValue)
       {
        subuser.CanViewReports = request.CanViewReports.Value;
         updatedFields.Add("CanViewReports");
      }

       if (request.CanManageMachines.HasValue)
  {
           subuser.CanManageMachines = request.CanManageMachines.Value;
     updatedFields.Add("CanManageMachines");
      }

    if (request.CanAssignLicenses.HasValue)
   {
   subuser.CanAssignLicenses = request.CanAssignLicenses.Value;
            updatedFields.Add("CanAssignLicenses");
  }

    if (request.CanCreateSubusers.HasValue)
   {
      subuser.CanCreateSubusers = request.CanCreateSubusers.Value;
  updatedFields.Add("CanCreateSubusers");
         }

       if (request.EmailNotifications.HasValue)
     {
    subuser.EmailNotifications = request.EmailNotifications.Value;
 updatedFields.Add("EmailNotifications");
   }

  if (request.SystemAlerts.HasValue)
         {
subuser.SystemAlerts = request.SystemAlerts.Value;
      updatedFields.Add("SystemAlerts");
  }

       if (!string.IsNullOrEmpty(request.Notes))
      {
    subuser.Notes = request.Notes;
updatedFields.Add("Notes");
           }

                // Update audit fields
     var updaterUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
          if (updaterUser != null)
              {
      subuser.UpdatedBy = updaterUser.user_id;
      }
  subuser.UpdatedAt = DateTime.UtcNow;

           // Save changes
       _context.Entry(subuser).State = EntityState.Modified;
     await _context.SaveChangesAsync();

  // Get group name
  string? groupName = null;
     if (subuser.GroupId.HasValue)
             {
        var group = await _context.Set<Group>().FindAsync(subuser.GroupId.Value);
            groupName = group?.name;
       }

                return Ok(new { 
                success = true,
        message = "Subuser updated successfully via parent email",
     parentEmail = parentEmail,
        subuserEmail = subuserEmail,
      updatedFields = updatedFields,
        updatedBy = currentUserEmail,
 updatedAt = subuser.UpdatedAt,
    subuser = new {
  subuser_email = subuser.subuser_email,
   user_email = subuser.user_email,
       subuser_username = subuser.subuser_username,
       name = subuser.Name,
  phone = subuser.Phone,
      department = subuser.Department,
role = subuser.Role,
            permissionsJson = subuser.PermissionsJson,
          assignedMachines = subuser.AssignedMachines,
  maxMachines = subuser.MaxMachines,
       machineIdsJson = subuser.MachineIdsJson,
        licenseIdsJson = subuser.LicenseIdsJson,
        status = subuser.status,
 groupId = subuser.GroupId,
       groupName = groupName,
 subuserGroup = subuser.subuser_group,
  license_allocation = subuser.license_allocation,
     timezone = subuser.timezone,
    isEmailVerified = subuser.IsEmailVerified,
   canViewReports = subuser.CanViewReports,
      canManageMachines = subuser.CanManageMachines,
         canAssignLicenses = subuser.CanAssignLicenses,
    canCreateSubusers = subuser.CanCreateSubusers,
      emailNotifications = subuser.EmailNotifications,
     systemAlerts = subuser.SystemAlerts,
  notes = subuser.Notes,
 updatedAt = subuser.UpdatedAt
    }
        });
            }
            catch (Exception ex)
         {
     return StatusCode(500, new { 
       success = false,
    message = "Error updating subuser by parent", 
      error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Delete subuser
        /// </summary>
        [HttpDelete("{email}")]
      [RequirePermission("DELETE_SUBUSER")]
        public async Task<IActionResult> DeleteSubuser(string email)
        {
  var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
   var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
    
            if (subuser == null) return NotFound($"Subuser with email {email} not found");

            // Check permissions
  bool canDelete = subuser.user_email == currentUserEmail ||
           await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_ALL_SUBUSERS", isCurrentUserSubuser);

  if (!canDelete)
          {
  return StatusCode(403, new { error = "You can only delete your own subusers" });
        }

   var subuserName = subuser.Name;
 _context.subuser.Remove(subuser);
     await _context.SaveChangesAsync();

            return Ok(new { 
     message = "Subuser deleted successfully", 
    subuser_email = email,
             name = subuserName,
         deletedAt = DateTime.UtcNow
    });
        }

        #region Private Helper Methods

        private async Task AssignRoleToSubuserAsync(string subuserEmail, string roleName, string assignedByEmail)
        {
var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == subuserEmail);
         var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);

       if (subuser != null && role != null)
   {
     // Check if role already assigned
      var existingRole = await _context.Set<SubuserRole>()
         .FirstOrDefaultAsync(sr => sr.SubuserId == subuser.subuser_id && sr.RoleId == role.RoleId);

    if (existingRole == null)
         {
            var subuserRole = new SubuserRole
        {
     SubuserId = subuser.subuser_id,
       RoleId = role.RoleId,
  AssignedAt = DateTime.UtcNow,
    AssignedByEmail = assignedByEmail
         };

      _context.Set<SubuserRole>().Add(subuserRole);
           await _context.SaveChangesAsync();
       }
   }
        }

        #endregion
    }

    #region Local DTOs

    /// <summary>
    /// DTO for assigning role to subuser
    /// </summary>
    public class SubuserRoleAssignDto
    {
        [System.ComponentModel.DataAnnotations.Required]
  public string RoleName { get; set; } = string.Empty;
    }

    #endregion
}
