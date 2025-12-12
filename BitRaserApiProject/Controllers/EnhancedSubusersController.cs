using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using BitRaserApiProject.Factories; // ✅ ADD THIS
using Microsoft.Extensions.Logging;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Subusers management controller with comprehensive user name and role information
    /// Supports email-based operations and role-based access control
    /// ✅ NOW SUPPORTS PRIVATE CLOUD ROUTING
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedSubusersController : ControllerBase
    {
  private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly DynamicDbContextFactory _contextFactory; // ✅ ADD THIS
        private readonly ILogger<EnhancedSubusersController> _logger; // ✅ ADD THIS

        public EnhancedSubusersController(
         IRoleBasedAuthService authService,
      IUserDataService userDataService,
            DynamicDbContextFactory contextFactory, // ✅ ADD THIS
            ILogger<EnhancedSubusersController> logger) // ✅ ADD THIS
     {
    _authService = authService;
       _userDataService = userDataService;
            _contextFactory = contextFactory; // ✅ ADD THIS
        _logger = logger; // ✅ ADD THIS
    }

    // ✅ ADD THIS HELPER METHOD
    private async Task<ApplicationDbContext> GetContextAsync()
    {
        return await _contextFactory.CreateDbContextAsync();
 }

        /// <summary>
   /// Get all subusers with user name and roles
        /// ✅ NOW SUPPORTS PRIVATE CLOUD ROUTING
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
  
       // ✅ GET DYNAMIC CONTEXT (ROUTES TO PRIVATE DB IF CONFIGURED)
   using var _context = await _contextFactory.CreateDbContextAsync();
    
     _logger.LogInformation("🔍 Fetching subusers for user: {Email}", currentUserEmail);
   
      IQueryable<subuser> query = _context.subuser
      .Include(s => s.SubuserRoles)
    .ThenInclude(sr => sr.Role);

          // ✅ HIERARCHICAL FILTERING: Users can only see subusers they can manage
 if (await _authService.IsSuperAdminAsync(currentUserEmail!, isCurrentUserSubuser))
        {
     // SuperAdmin sees all subusers - no filtering
  _logger.LogInformation("👑 SuperAdmin access - showing all subusers");
    }
   else if (await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser))
    {
// Has READ_ALL_SUBUSERS permission - can see all manageable subusers
   var managedEmails = await _authService.GetManagedUserEmailsAsync(currentUserEmail!);
    query = query.Where(s => managedEmails.Contains(s.subuser_email) || managedEmails.Contains(s.user_email));
         _logger.LogInformation("🔐 READ_ALL_SUBUSERS permission - showing managed subusers");
}
            else
     {
   // Default: Users can only see their own subusers
query = query.Where(s => s.user_email == currentUserEmail);
   _logger.LogInformation("👤 Default access - showing own subusers only");
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

     _logger.LogInformation("✅ Found {Count} subusers", subusers.Count);
     
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
  subuser_group = s.subuser_group ?? "No Group",
isEmailVerified = s.IsEmailVerified,
  assignedMachines = s.AssignedMachines ?? 0,
maxMachines = s.MaxMachines ?? 5,
        license_allocation = s.license_allocation ?? 0,
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
    
      // ✅ ADD DYNAMIC CONTEXT
      using var _context = await GetContextAsync();
      
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
    subuser_group = subuser.subuser_group ?? "No Group",
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
    license_allocation = subuser.license_allocation ?? 0,
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
   
            // ✅ USE DYNAMIC CONTEXT (ROUTES TO PRIVATE DB IF CONFIGURED)
    using var _context = await _contextFactory.CreateDbContextAsync();
       
            _logger.LogInformation("🔍 Fetching subusers for parent: {ParentEmail}", parentEmail);
        
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

 _logger.LogInformation("✅ Found {Count} subusers for parent: {ParentEmail}", 
    subusers.Count, parentEmail);

  var subuserDetails = subusers.Select(s => new {
           s.subuser_id,
       s.subuser_email,
       name = s.Name ?? "N/A",
    phone = s.Phone ?? "N/A",
  department = s.Department ?? "N/A",
   role = s.Role ?? "N/A",
      status = s.status ?? "active",
       last_login = s.last_login,
          subuser_group = s.subuser_group ?? "No Group",
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
      try
      {
 var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
      
  _logger.LogInformation("🔍 Creating subuser - Current user: {Email}, IsSubuser: {IsSubuser}", 
         currentUserEmail, isCurrentUserSubuser);

      // ✅ SIMPLIFIED CHECK: If RequirePermission passed, user has CREATE_SUBUSER permission
   // The [RequirePermission("CREATE_SUBUSER")] attribute already validated this
   
 // Optional: Log user's roles for debugging
  var userRoles = await _authService.GetUserRolesAsync(currentUserEmail!, isCurrentUserSubuser);
         _logger.LogInformation("User roles: {Roles}", string.Join(", ", userRoles));

       // ✅ GET DYNAMIC CONTEXT (ROUTES TO PRIVATE DB IF CONFIGURED)
     using var _context = await _contextFactory.CreateDbContextAsync();
            
    _logger.LogInformation("💾 Creating subuser in database for user: {Email}", currentUserEmail);

   // Check if subuser already exists
     var existingSubuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == request.Email);
  if (existingSubuser != null)
{
    _logger.LogWarning("⚠️ Subuser already exists: {Email}", request.Email);
     return Conflict($"Subuser with email {request.Email} already exists");
   }

   // ✅ SMART PARENT EMAIL RESOLUTION - FIX: Use SAME dynamic context
  string parentUserEmail;
int parentUserId;

     if (isCurrentUserSubuser)
    {
// ✅ If SUBUSER is creating: Find their parent user IN SAME DB
     var currentSubuser = await _context.subuser
   .FirstOrDefaultAsync(s => s.subuser_email == currentUserEmail);

  if (currentSubuser == null)
   {
       _logger.LogError("❌ Current subuser not found: {Email}", currentUserEmail);
return BadRequest("Current subuser not found");
          }

 // Use the subuser's parent as the parent for new subuser
   parentUserEmail = currentSubuser.user_email;
      parentUserId = currentSubuser.superuser_id ?? 0;
         
  _logger.LogInformation("📧 Subuser creating for parent: {ParentEmail}", parentUserEmail);
      }
    else
   {
   // ✅ If REGULAR USER is creating: Use their email as parent
   // ✅ FIX: Find user IN SAME DYNAMIC CONTEXT (private or main)
 var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
   
        if (parentUser == null)
 {
    _logger.LogError("❌ Parent user not found in current database: {Email}", currentUserEmail);
    
            // ✅ FIX: If not found in current DB, might be creating for first time in private DB
   // Use placeholder values - user will be created if needed
       parentUserEmail = currentUserEmail!;
 parentUserId = 0; // Will be resolved later if needed
          
       _logger.LogInformation("⚠️ Parent user not found - using current email as parent: {ParentEmail}", parentUserEmail);
  }
      else
        {
            parentUserEmail = parentUser.user_email;
 parentUserId = parentUser.user_id;
  
     _logger.LogInformation("👤 Regular user creating subuser for themselves: {ParentEmail}", parentUserEmail);
     }
  }

        // ✅ SECURITY CHECK: Verify user is creating for themselves or has permission
  // Only block if trying to create for someone else without permission
    if (!isCurrentUserSubuser && parentUserEmail != currentUserEmail)
 {
     // Regular user trying to create for someone else
   if (!await _authService.HasPermissionAsync(currentUserEmail!, "CREATE_SUBUSERS_FOR_OTHERS", isCurrentUserSubuser))
    {
  _logger.LogWarning("⚠️ Unauthorized attempt to create subuser for another user by {Email}", currentUserEmail);
    return StatusCode(403, new { 
 success = false,
 error = "You can only create subusers for yourself" 
     });
  }
      }

   // Create subuser with name
 var newSubuser = new subuser
      {
subuser_email = request.Email,
  subuser_password = BCrypt.Net.BCrypt.HashPassword(request.Password),
  user_email = parentUserEmail,
 superuser_id = parentUserId > 0 ? parentUserId : null,
  Name = request.Name,
  Phone = request.Phone ?? "",
    Department = request.Department ?? "",
  Role = request.Role ?? "subuser",
  status = "active",
  IsEmailVerified = false,
   MaxMachines = request.MaxMachines ?? 5,
   GroupId = request.GroupId,
    license_allocation = request.LicenseAllocation ?? 0,
   CanCreateSubusers = request.CanCreateSubusers ?? false,
 CanViewReports = request.CanViewReports ?? true,
  CanManageMachines = request.CanManageMachines ?? false,
    CanAssignLicenses = request.CanAssignLicenses ?? false,
 EmailNotifications = request.EmailNotifications ?? true,
     SystemAlerts = request.SystemAlerts ?? true,
   CreatedBy = parentUserId,
 CreatedAt = DateTime.UtcNow,
   Notes = request.Notes
    };

        _logger.LogInformation("💾 Saving subuser to database: {SubuserEmail}", newSubuser.subuser_email);
   _context.subuser.Add(newSubuser);
  await _context.SaveChangesAsync();
  _logger.LogInformation("✅ Subuser saved successfully with ID: {SubuserId}", newSubuser.subuser_id);

     // ✅ Assign default SubUser role (or custom role if validated above)
var roleToAssign = !string.IsNullOrEmpty(request.Role) && await _authService.CanAssignRoleAsync(currentUserEmail!, request.Role)
   ? request.Role
     : "SubUser";

_logger.LogInformation("🔐 Assigning role '{Role}' to subuser: {SubuserEmail}", roleToAssign, newSubuser.subuser_email);
       await AssignRoleToSubuserAsync(_context, newSubuser.subuser_email, roleToAssign, currentUserEmail!);

  // Reload with roles
      var createdSubuser = await _context.subuser
.Include(s => s.SubuserRoles)
    .ThenInclude(sr => sr.Role)
   .FirstOrDefaultAsync(s => s.subuser_id == newSubuser.subuser_id);

       var response = new {
 success = true,
 subuser_id = createdSubuser!.subuser_id,
    subuser_email = createdSubuser.subuser_email,
    name = createdSubuser.Name,
  phone = createdSubuser.Phone,
     department = createdSubuser.Department,
      role = createdSubuser.Role,
   parentUserEmail = createdSubuser.user_email,
roles = createdSubuser.SubuserRoles.Select(sr => new {
    roleName = sr.Role.RoleName,
            hierarchyLevel = sr.Role.HierarchyLevel
      }).ToList(),
  createdAt = createdSubuser.CreatedAt,
   createdBy = isCurrentUserSubuser ? $"Subuser: {currentUserEmail}" : $"User: {currentUserEmail}",
    databaseLocation = "Dynamic", // ✅ Database routing handled by factory
    message = "Subuser created successfully"
 };

  _logger.LogInformation("🎉 Subuser creation complete for: {SubuserEmail}", newSubuser.subuser_email);
       return CreatedAtAction(nameof(GetSubuserByEmail), new { email = newSubuser.subuser_email }, response);
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "❌ Error creating subuser for user {Email}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
     return StatusCode(500, new { 
      success = false,
 message = "Error creating subuser", 
  error = ex.Message,
  detail = ex.InnerException?.Message
        });
}
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
       // ✅ ADD DYNAMIC CONTEXT
   using var _context = await GetContextAsync();
       
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
     
       // ✅ ADD DYNAMIC CONTEXT
       using var _context = await GetContextAsync();
       
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

      if (request.GroupId.HasValue)
    {
    subuser.GroupId = request.GroupId.Value;
           updatedFields.Add("GroupId");
        }

 if (request.LicenseAllocation.HasValue) // ✅ Added
{
   subuser.license_allocation = request.LicenseAllocation.Value;
       updatedFields.Add("LicenseAllocation");
  }

   if (!string.IsNullOrEmpty(request.SubuserGroup)) // ✅ Added
    {
       subuser.subuser_group = request.SubuserGroup;
    updatedFields.Add("SubuserGroup");
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
        subuser_email = subuser.subuser_email,
   updatedFields = updatedFields,
   updatedBy = currentUserEmail,
      updatedAt = subuser.UpdatedAt,
          // Return updated data
      subuser = new {
      subuser_email = subuser.subuser_email,
  name = subuser.Name,
   phone = subuser.Phone,
          department = subuser.Department,
     role = subuser.Role,
            status = subuser.status,
     groupId = subuser.GroupId,
   maxMachines = subuser.MaxMachines,
      license_allocation = subuser.license_allocation,
  subuser_group = subuser.subuser_group, // ✅ Fixed: Direct string field
        canViewReports = subuser.CanViewReports,
   canManageMachines = subuser.CanManageMachines,
      canAssignLicenses = subuser.CanAssignLicenses,
  canCreateSubusers = subuser.CanCreateSubusers,
          emailNotifications = subuser.EmailNotifications,
     systemAlerts = subuser.SystemAlerts,
   notes = subuser.Notes
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
        /// Route: /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}
   /// Only allows updating: name, phone, department, role, status
        /// Parent users can update their own subusers without special permissions
        /// Supports single or multiple field updates via JSON body
  /// Supports both camelCase (Name, Phone) and snake_case (subuser_name, subuser_phone) for compatibility
     /// </summary>
     [HttpPatch("by-parent/{parentEmail}/subuser/{subuserEmail}")]
      public async Task<IActionResult> PatchSubuserByParent(
    string parentEmail, 
          string subuserEmail, 
     [FromBody] UpdateSubuserByParentDto request)
        {
 try
         {
   var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

          // ✅ ADD DYNAMIC CONTEXT
   using var _context = await GetContextAsync();

        // Find subuser by both parent email and subuser email
      var subuser = await _context.subuser
   .FirstOrDefaultAsync(s => s.user_email == parentEmail && s.subuser_email == subuserEmail);
                
          if (subuser == null)
        return NotFound(new { 
       success = false,
        message = $"Subuser '{subuserEmail}' not found under parent '{parentEmail}'" 
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

// ✅ Update fields - Support both naming conventions
         // Priority: snake_case (subuser_name) over camelCase (Name) for compatibility
        
     // Name field
        if (!string.IsNullOrEmpty(request.subuser_name))
    {
               subuser.Name = request.subuser_name;
     updatedFields.Add("Name");
         }
        else if (!string.IsNullOrEmpty(request.Name))
{
        subuser.Name = request.Name;
   updatedFields.Add("Name");
             }

    // Phone field
                if (!string.IsNullOrEmpty(request.subuser_phone))
       {
            subuser.Phone = request.subuser_phone;
  updatedFields.Add("Phone");
    }
     else if (!string.IsNullOrEmpty(request.Phone))
    {
         subuser.Phone = request.Phone;
         updatedFields.Add("Phone");
      }

       // Department field (only camelCase supported)
                if (!string.IsNullOrEmpty(request.Department))
                {
     subuser.Department = request.Department;
 updatedFields.Add("Department");
        }

   // Role field
     if (!string.IsNullOrEmpty(request.subuser_role))
     {
      subuser.Role = request.subuser_role;
           updatedFields.Add("Role");
           }
     else if (!string.IsNullOrEmpty(request.Role))
        {
   subuser.Role = request.Role;
 updatedFields.Add("Role");
}

     // Status field (only camelCase supported)
          if (!string.IsNullOrEmpty(request.Status))
                {
      subuser.status = request.Status;
        updatedFields.Add("Status");
     }

  // Check if at least one field was provided
       if (updatedFields.Count == 0)
     {
         return BadRequest(new {
         success = false,
             message = "No fields to update. Provide at least one field in the request body.",
          acceptedFields = new {
      camelCase = new[] { "Name", "Phone", "Department", "Role", "Status" },
          snake_case = new[] { "subuser_name", "subuser_phone", "subuser_role" }
   },
          example1_camelCase = new {
Name = "John Smith",
   Phone = "1234567890",
   Department = "IT",
          Role = "Manager",
          Status = "active"
 },
         example2_snake_case = new {
      subuser_name = "John Smith",
     subuser_phone = "1234567890",
          subuser_role = "Manager"
           }
          });
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

      return Ok(new { 
       success = true,
       message = "Subuser updated successfully",
          parent_email = parentEmail,
          subuser_email = subuserEmail,
      updatedFields = updatedFields,
        updatedBy = currentUserEmail,
       updatedAt = subuser.UpdatedAt,
     // Return updated data - ONLY the 5 allowed fields
        subuser = new {
   subuser_email = subuser.subuser_email,
              user_email = subuser.user_email,
        name = subuser.Name,
     phone = subuser.Phone,
      department = subuser.Department,
          role = subuser.Role,
   status = subuser.status
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
        /// Delete subuser
        /// </summary>
        [HttpDelete("{email}")]
   [RequirePermission("DELETE_SUBUSER")]
  public async Task<IActionResult> DeleteSubuser(string email)
        {
  var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
  
    // ✅ ADD DYNAMIC CONTEXT
    using var _context = await GetContextAsync();
    
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

        private async Task AssignRoleToSubuserAsync(ApplicationDbContext context, string subuserEmail, string roleName, string assignedByEmail)
  {
var subuser = await context.subuser.FirstOrDefaultAsync(s => s.subuser_email == subuserEmail);
       var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);

       if (subuser != null && role != null)
   {
     // Check if role already assigned
      var existingRole = await context.Set<SubuserRole>()
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

context.Set<SubuserRole>().Add(subuserRole);
           await context.SaveChangesAsync();
      
 _logger.LogInformation("✅ Role '{RoleName}' assigned to subuser: {SubuserEmail}", roleName, subuserEmail);
   }
   else
            {
    _logger.LogInformation("ℹ️ Role '{RoleName}' already assigned to subuser: {SubuserEmail}", roleName, subuserEmail);
    }
   }
     else
  {
       _logger.LogWarning("⚠️ Could not assign role - Subuser or Role not found. Subuser: {SubuserEmail}, Role: {RoleName}", subuserEmail, roleName);
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

 /// <summary>
    /// DTO for updating subuser via by-parent endpoint
    /// ONLY allows updating: name, phone, department, role, status
    /// Supports both camelCase (Name, Phone) and snake_case (subuser_name, subuser_phone) for compatibility
    /// </summary>
    public class UpdateSubuserByParentDto
    {
        // ✅ CamelCase properties (standard C# naming)
 [MaxLength(100)]
    public string? Name { get; set; }
        
        [MaxLength(20)]
        public string? Phone { get; set; }
  
        [MaxLength(100)]
        public string? Department { get; set; }
  
  [MaxLength(50)]
        public string? Role { get; set; }
    
[MaxLength(50)]
        public string? Status { get; set; }

        // ✅ snake_case properties (for API compatibility)
        [MaxLength(100)]
        public string? subuser_name { get; set; }
   
        [MaxLength(20)]
        public string? subuser_phone { get; set; }
     
        [MaxLength(50)]
        public string? subuser_role { get; set; }
    }

    #endregion
}
