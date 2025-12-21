using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Group Controller - Complete CRUD operations for Groups
    /// Supports: Create, Read, Update (PUT & PATCH), Delete
    /// Fields: groupname, groupdescription, groplicenseallocation, grouppermission
  /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GroupController : ControllerBase
  {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GroupController> _logger;

      public GroupController(ApplicationDbContext context, ILogger<GroupController> logger)
        {
          _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/Group
      /// Get all groups with filtering and pagination
   /// </summary>
        [HttpGet]
      public async Task<ActionResult<IEnumerable<GroupResponseDto>>> GetGroups(
            [FromQuery] string? search = null,
   [FromQuery] string? status = null,
     [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
     try
        {
       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
         if (string.IsNullOrEmpty(userEmail))
          return Unauthorized(new { message = "User not authenticated" });

    var query = _context.Groups.AsQueryable();

      // Apply filters
     if (!string.IsNullOrEmpty(search))
          query = query.Where(g =>
      g.name.Contains(search) ||
(g.description != null && g.description.Contains(search)));

       if (!string.IsNullOrEmpty(status))
         query = query.Where(g => g.status == status);

      var total = await query.CountAsync();

   var groups = await query
      .OrderByDescending(g => g.created_at)
     .Skip((page - 1) * pageSize)
    .Take(pageSize)
        .Select(g => new GroupResponseDto
  {
         GroupId = g.group_id,
    GroupName = g.name,
            GroupDescription = g.description,
         GroupLicenseAllocation = g.license_allocation,
           GroupPermission = g.permissions_json,
     Status = g.status,
         CreatedAt = g.created_at,
         UpdatedAt = g.updated_at
    })
.ToListAsync();

      Response.Headers.Append("X-Total-Count", total.ToString());
     Response.Headers.Append("X-Page", page.ToString());
  Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(groups);
  }
            catch (Exception ex)
{
_logger.LogError(ex, "Error fetching groups");
       return StatusCode(500, new { message = "Error retrieving groups", error = ex.Message });
       }
        }

        /// <summary>
  /// GET: api/Group/{id}
        /// Get single group details
 /// </summary>
        [HttpGet("{id}")]
   public async Task<ActionResult<GroupResponseDto>> GetGroup(int id)
        {
            try
   {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     if (string.IsNullOrEmpty(userEmail))
        return Unauthorized(new { message = "User not authenticated" });

      var group = await _context.Groups.FindAsync(id);
        if (group == null)
         return NotFound(new { message = "Group not found" });

      return Ok(new GroupResponseDto
                {
GroupId = group.group_id,
      GroupName = group.name,
    GroupDescription = group.description,
    GroupLicenseAllocation = group.license_allocation,
     GroupPermission = group.permissions_json,
  Status = group.status,
 CreatedAt = group.created_at,
  UpdatedAt = group.updated_at
    });
         }
            catch (Exception ex)
 {
                _logger.LogError(ex, "Error fetching group {GroupId}", id);
  return StatusCode(500, new { message = "Error retrieving group", error = ex.Message });
  }
      }

        /// <summary>
        /// POST: api/Group
        /// Create new group
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "superadmin,admin")]
   public async Task<ActionResult<GroupResponseDto>> CreateGroup([FromBody] CreateGroupRequestDto dto)
      {
     try
     {
   var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     if (string.IsNullOrEmpty(userEmail))
          return Unauthorized(new { message = "User not authenticated" });

        // Validate required fields
   if (string.IsNullOrWhiteSpace(dto.GroupName))
     return BadRequest(new { message = "Group name is required" });

  // Check if group name already exists
            if (await _context.Groups.AnyAsync(g => g.name == dto.GroupName))
  return Conflict(new { message = "Group with this name already exists" });

    var group = new Group
{
    name = dto.GroupName,
              description = dto.GroupDescription,
  license_allocation = dto.GroupLicenseAllocation ?? 0,
    permissions_json = dto.GroupPermission,
               status = "active",
             created_at = DateTime.UtcNow
      };

     _context.Groups.Add(group);
     await _context.SaveChangesAsync();

      _logger.LogInformation("Group {GroupName} created by {Email}", dto.GroupName, userEmail);

 return CreatedAtAction(nameof(GetGroup), new { id = group.group_id },
           new GroupResponseDto
  {
      GroupId = group.group_id,
       GroupName = group.name,
           GroupDescription = group.description,
                  GroupLicenseAllocation = group.license_allocation,
            GroupPermission = group.permissions_json,
            Status = group.status,
   CreatedAt = group.created_at,
      UpdatedAt = group.updated_at
    });
       }
         catch (Exception ex)
{
       _logger.LogError(ex, "Error creating group");
         return StatusCode(500, new { message = "Error creating group", error = ex.Message });
  }
        }

        /// <summary>
 /// PUT: api/Group/{id}
        /// Update entire group (full update)
   /// </summary>
 [HttpPut("{id}")]
        [Authorize(Roles = "superadmin,admin")]
   public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpdateGroupRequestDto dto)
        {
            try
    {
         var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          if (string.IsNullOrEmpty(userEmail))
         return Unauthorized(new { message = "User not authenticated" });

        var group = await _context.Groups.FindAsync(id);
                if (group == null)
      return NotFound(new { message = "Group not found" });

           // Check if new name conflicts with existing group
       if (dto.GroupName != null && dto.GroupName != group.name)
       {
        if (await _context.Groups.AnyAsync(g => g.name == dto.GroupName && g.group_id != id))
 return Conflict(new { message = "Group with this name already exists" });
       }

   // Update all fields
          if (dto.GroupName != null) group.name = dto.GroupName;
            if (dto.GroupDescription != null) group.description = dto.GroupDescription;
       if (dto.GroupLicenseAllocation.HasValue) group.license_allocation = dto.GroupLicenseAllocation.Value;
   if (dto.GroupPermission != null) group.permissions_json = dto.GroupPermission;
 if (dto.Status != null) group.status = dto.Status;

          group.updated_at = DateTime.UtcNow;

      await _context.SaveChangesAsync();

      _logger.LogInformation("Group {GroupId} updated by {Email}", id, userEmail);

                return Ok(new
          {
             message = "Group updated successfully",
         groupId = group.group_id,
   groupName = group.name,
        updatedAt = group.updated_at
             });
        }
 catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating group {GroupId}", id);
    return StatusCode(500, new { message = "Error updating group", error = ex.Message });
            }
        }

   /// <summary>
        /// PATCH: api/Group/{id}
        /// Partially update group (supports partial updates)
  /// </summary>
        [HttpPatch("{id}")]
        [Authorize(Roles = "superadmin,admin")]
   public async Task<IActionResult> PatchGroup(int id, [FromBody] UpdateGroupRequestDto dto)
  {
        try
            {
         var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
   return Unauthorized(new { message = "User not authenticated" });

         var group = await _context.Groups.FindAsync(id);
             if (group == null)
            return NotFound(new { message = "Group not found" });

 // Check if new name conflicts with existing group
         if (dto.GroupName != null && dto.GroupName != group.name)
           {
  if (await _context.Groups.AnyAsync(g => g.name == dto.GroupName && g.group_id != id))
               return Conflict(new { message = "Group with this name already exists" });
     }

                // Update only provided fields (partial update)
   if (dto.GroupName != null) group.name = dto.GroupName;
       if (dto.GroupDescription != null) group.description = dto.GroupDescription;
      if (dto.GroupLicenseAllocation.HasValue) group.license_allocation = dto.GroupLicenseAllocation.Value;
            if (dto.GroupPermission != null) group.permissions_json = dto.GroupPermission;
          if (dto.Status != null) group.status = dto.Status;

  group.updated_at = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Group {GroupId} partially updated by {Email}", id, userEmail);

  return Ok(new
             {
       message = "Group partially updated successfully",
groupId = group.group_id,
       groupName = group.name,
        updatedAt = group.updated_at
     });
         }
  catch (Exception ex)
         {
         _logger.LogError(ex, "Error patching group {GroupId}", id);
 return StatusCode(500, new { message = "Error updating group", error = ex.Message });
            }
 }

        /// <summary>
   /// DELETE: api/Group/{id}
        /// Delete group
        /// </summary>
     [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin")]
  public async Task<IActionResult> DeleteGroup(int id)
      {
      try
   {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
   return Unauthorized(new { message = "User not authenticated" });

    var group = await _context.Groups.FindAsync(id);
if (group == null)
      return NotFound(new { message = "Group not found" });

           // Check if group has any subusers assigned
                var subuserCount = await _context.subuser.CountAsync(s => s.GroupId == id);
     if (subuserCount > 0)
         {
           return BadRequest(new
       {
       message = "Cannot delete group with assigned subusers",
          subuserCount = subuserCount,
     suggestion = "Remove all subusers from this group before deleting or use soft delete"
             });
                }

     _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Group {GroupId} deleted by {Email}", id, userEmail);

       return Ok(new
      {
 message = "Group deleted successfully",
       groupId = id,
     deletedAt = DateTime.UtcNow
                });
    }
  catch (Exception ex)
            {
    _logger.LogError(ex, "Error deleting group {GroupId}", id);
     return StatusCode(500, new { message = "Error deleting group", error = ex.Message });
      }
        }

        /// <summary>
     /// GET: api/Group/{id}/members
        /// Get all subusers in a group
        /// </summary>
        [HttpGet("{id}/members")]
        public async Task<ActionResult<GroupMembersResponseDto>> GetGroupMembers(int id)
        {
      try
    {
   var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 if (string.IsNullOrEmpty(userEmail))
   return Unauthorized(new { message = "User not authenticated" });

      var group = await _context.Groups.FindAsync(id);
    if (group == null)
        return NotFound(new { message = "Group not found" });

   var members = await _context.subuser
         .Where(s => s.GroupId == id)
.Select(s => new GroupMemberDto
           {
         SubuserId = s.subuser_id,
      SubuserEmail = s.subuser_email,
Name = s.Name ?? s.subuser_email,
            Role = s.Role,
       Status = s.status,
            JoinedAt = s.CreatedAt
      })
             .ToListAsync();

     return Ok(new GroupMembersResponseDto
       {
       GroupId = group.group_id,
         GroupName = group.name,
         TotalMembers = members.Count,
      Members = members
                });
   }
            catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching group members for group {GroupId}", id);
 return StatusCode(500, new { message = "Error retrieving group members", error = ex.Message });
     }
        }

        /// <summary>
        /// GET: api/Group/{id}/members/by-email/{email}
        /// Get specific member in a group by email
  /// </summary>
        [HttpGet("{id}/members/by-email/{email}")]
        public async Task<ActionResult<GroupMemberDto>> GetGroupMemberByEmail(int id, string email)
      {
  try
            {
   var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
   return Unauthorized(new { message = "User not authenticated" });

 var group = await _context.Groups.FindAsync(id);
             if (group == null)
         return NotFound(new { message = "Group not found" });

         var member = await _context.subuser
         .Where(s => s.GroupId == id && s.subuser_email == email)
      .Select(s => new GroupMemberDto
      {
  SubuserId = s.subuser_id,
      SubuserEmail = s.subuser_email,
        Name = s.Name ?? s.subuser_email,
        Role = s.Role,
  Status = s.status,
       JoinedAt = s.CreatedAt
 })
     .FirstOrDefaultAsync();

         if (member == null)
   return NotFound(new { message = "Member not found in this group" });

              return Ok(member);
   }
          catch (Exception ex)
  {
    _logger.LogError(ex, "Error fetching group member by email for group {GroupId}", id);
 return StatusCode(500, new { message = "Error retrieving group member", error = ex.Message });
          }
  }

  /// <summary>
        /// POST: api/Group/{id}/members/by-email
        /// Add member to group by email
        /// </summary>
        [HttpPost("{id}/members/by-email")]
        [Authorize(Roles = "superadmin,admin")]
  public async Task<IActionResult> AddMemberByEmail(int id, [FromBody] AddMemberByEmailDto dto)
  {
        try
      {
   var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userEmail))
         return Unauthorized(new { message = "User not authenticated" });

          var group = await _context.Groups.FindAsync(id);
   if (group == null)
  return NotFound(new { message = "Group not found" });

      var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == dto.Email);
           if (subuser == null)
 return NotFound(new { message = "Subuser not found with this email" });

  if (subuser.GroupId == id)
          return BadRequest(new { message = "Subuser is already in this group" });

          subuser.GroupId = id;
         subuser.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync();

                _logger.LogInformation("Subuser {Email} added to group {GroupId} by {AdminEmail}", dto.Email, id, userEmail);

     return Ok(new
     {
      message = "Member added to group successfully",
              groupId = id,
             groupName = group.name,
 memberEmail = dto.Email,
         addedBy = userEmail,
     addedAt = DateTime.UtcNow
    });
            }
            catch (Exception ex)
   {
 _logger.LogError(ex, "Error adding member to group by email");
    return StatusCode(500, new { message = "Error adding member to group", error = ex.Message });
            }
        }

   /// <summary>
     /// DELETE: api/Group/{id}/members/by-email/{email}
        /// Remove member from group by email
        /// </summary>
     [HttpDelete("{id}/members/by-email/{email}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> RemoveMemberByEmail(int id, string email)
  {
            try
            {
 var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  if (string.IsNullOrEmpty(userEmail))
       return Unauthorized(new { message = "User not authenticated" });

  var group = await _context.Groups.FindAsync(id);
     if (group == null)
return NotFound(new { message = "Group not found" });

         var subuser = await _context.subuser
       .FirstOrDefaultAsync(s => s.subuser_email == email && s.GroupId == id);

                if (subuser == null)
   return NotFound(new { message = "Member not found in this group" });

                subuser.GroupId = null;
            subuser.UpdatedAt = DateTime.UtcNow;

       await _context.SaveChangesAsync();

 _logger.LogInformation("Subuser {Email} removed from group {GroupId} by {AdminEmail}", email, id, userEmail);

    return Ok(new
        {
  message = "Member removed from group successfully",
         groupId = id,
               groupName = group.name,
        memberEmail = email,
       removedBy = userEmail,
              removedAt = DateTime.UtcNow
          });
     }
            catch (Exception ex)
    {
     _logger.LogError(ex, "Error removing member from group by email");
        return StatusCode(500, new { message = "Error removing member from group", error = ex.Message });
            }
        }

        /// <summary>
      /// GET: api/Group/by-member-email/{email}
        /// Get group by member email
        /// </summary>
        [HttpGet("by-member-email/{email}")]
        public async Task<ActionResult<GroupResponseDto>> GetGroupByMemberEmail(string email)
        {
      try
{
 var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
        return Unauthorized(new { message = "User not authenticated" });

        var subuser = await _context.subuser
        .Include(s => s.GroupId)
          .FirstOrDefaultAsync(s => s.subuser_email == email);

        if (subuser == null)
   return NotFound(new { message = "Subuser not found" });

     if (!subuser.GroupId.HasValue)
       return NotFound(new { message = "Subuser is not assigned to any group" });

   var group = await _context.Groups.FindAsync(subuser.GroupId.Value);
if (group == null)
          return NotFound(new { message = "Group not found" });

                return Ok(new GroupResponseDto
              {
    GroupId = group.group_id,
                    GroupName = group.name,
    GroupDescription = group.description,
GroupLicenseAllocation = group.license_allocation,
         GroupPermission = group.permissions_json,
       Status = group.status,
      CreatedAt = group.created_at,
    UpdatedAt = group.updated_at
       });
  }
     catch (Exception ex)
            {
       _logger.LogError(ex, "Error fetching group by member email");
        return StatusCode(500, new { message = "Error retrieving group", error = ex.Message });
   }
   }

     /// <summary>
        /// GET: api/Group/statistics
        /// Get group statistics
  /// </summary>
     [HttpGet("statistics")]
 [Authorize(Roles = "superadmin,admin")]
    public async Task<ActionResult<GroupStatisticsResponseDto>> GetStatistics()
        {
     try
            {
          var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userEmail))
    return Unauthorized(new { message = "User not authenticated" });

      var totalGroups = await _context.Groups.CountAsync();
         var activeGroups = await _context.Groups.CountAsync(g => g.status == "active");
           var inactiveGroups = await _context.Groups.CountAsync(g => g.status == "inactive");
      var totalLicenseAllocation = await _context.Groups.SumAsync(g => (int?)g.license_allocation) ?? 0;
   var totalMembers = await _context.subuser.CountAsync(s => s.GroupId != null);

                var groupsWithMemberCount = await _context.Groups
          .Select(g => new
  {
       Group = g,
     MemberCount = _context.subuser.Count(s => s.GroupId == g.group_id)
        })
          .OrderByDescending(x => x.MemberCount)
       .Take(5)
             .ToListAsync();

         return Ok(new GroupStatisticsResponseDto
                {
     TotalGroups = totalGroups,
        ActiveGroups = activeGroups,
      InactiveGroups = inactiveGroups,
             TotalLicenseAllocation = totalLicenseAllocation,
     TotalMembers = totalMembers,
               TopGroups = groupsWithMemberCount.Select(x => new TopGroupDto
   {
     GroupId = x.Group.group_id,
          GroupName = x.Group.name,
   MemberCount = x.MemberCount,
   LicenseAllocation = x.Group.license_allocation
           }).ToList()
     });
        }
            catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching group statistics");
    return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
 }
        }
    }

    #region DTOs

    public class GroupResponseDto
    {
    public int GroupId { get; set; }
      public string GroupName { get; set; } = string.Empty;
        public string? GroupDescription { get; set; }
        public int GroupLicenseAllocation { get; set; }
        public string? GroupPermission { get; set; }
        public string Status { get; set; } = "active";
 public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateGroupRequestDto
    {
[Required(ErrorMessage = "Group name is required")]
        [MaxLength(100)]
        public string GroupName { get; set; } = string.Empty;

     [MaxLength(500)]
        public string? GroupDescription { get; set; }

        [Range(0, int.MaxValue)]
        public int? GroupLicenseAllocation { get; set; } = 0;

 public string? GroupPermission { get; set; }
    }

public class UpdateGroupRequestDto
    {
        [MaxLength(100)]
    public string? GroupName { get; set; }

        [MaxLength(500)]
   public string? GroupDescription { get; set; }

        [Range(0, int.MaxValue)]
        public int? GroupLicenseAllocation { get; set; }

        public string? GroupPermission { get; set; }

        [MaxLength(50)]
   public string? Status { get; set; }
    }

    public class GroupMembersResponseDto
    {
      public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
     public int TotalMembers { get; set; }
        public List<GroupMemberDto> Members { get; set; } = new();
}

    public class GroupMemberDto
    {
        public int SubuserId { get; set; }
public string SubuserEmail { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
   public DateTime JoinedAt { get; set; }
    }

    public class GroupStatisticsResponseDto
    {
        public int TotalGroups { get; set; }
        public int ActiveGroups { get; set; }
        public int InactiveGroups { get; set; }
        public int TotalLicenseAllocation { get; set; }
        public int TotalMembers { get; set; }
        public List<TopGroupDto> TopGroups { get; set; } = new();
    }

    public class TopGroupDto
    {
        public int GroupId { get; set; }
public string GroupName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int LicenseAllocation { get; set; }
    }

    public class AddMemberByEmailDto
{
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    #endregion
}
