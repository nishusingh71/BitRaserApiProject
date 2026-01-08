using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using BitRaserApiProject.Factories;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Department Controller - Complete CRUD operations for Departments
    /// Supports: Create, Read, Update (PUT & PATCH), Delete
    /// Linked to Groups for organizational structure
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<DepartmentController> _logger;

        public DepartmentController(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<DepartmentController> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/Department
        /// Get all departments with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDepartments(
            [FromQuery] string? search = null,
            [FromQuery] int? groupId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.Set<Department>().AsNoTracking();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(d => d.Name.Contains(search) || 
                                           (d.Description != null && d.Description.Contains(search)));
                }

                if (groupId.HasValue)
                {
                    query = query.Where(d => d.GroupId == groupId.Value);
                }

                var total = await query.CountAsync();
                var departments = await query
                    .OrderBy(d => d.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new DepartmentResponseDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description,
                        GroupId = d.GroupId,
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt,
                        MemberCount = context.Set<subuser>()
                            .Count(s => s.Department == d.Name)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = departments,
                    pagination = new
                    {
                        page,
                        pageSize,
                        total,
                        totalPages = (int)Math.Ceiling((double)total / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching departments");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/Department/{id}
        /// Get single department by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartment(int id)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var department = await context.Set<Department>()
                    .AsNoTracking()
                    .Where(d => d.Id == id)
                    .Select(d => new DepartmentResponseDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description,
                        GroupId = d.GroupId,
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt,
                        MemberCount = context.Set<subuser>()
                            .Count(s => s.Department == d.Name)
                    })
                    .FirstOrDefaultAsync();

                if (department == null)
                    return NotFound(new { success = false, message = $"Department with ID {id} not found" });

                return Ok(new { success = true, data = department });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching department {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/Department/by-group/{groupId}
        /// Get all departments for a specific group
        /// </summary>
        [HttpGet("by-group/{groupId}")]
        public async Task<IActionResult> GetDepartmentsByGroup(int groupId)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var departments = await context.Set<Department>()
                    .AsNoTracking()
                    .Where(d => d.GroupId == groupId)
                    .OrderBy(d => d.Name)
                    .Select(d => new DepartmentResponseDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description,
                        GroupId = d.GroupId,
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt,
                        MemberCount = context.Set<subuser>()
                            .Count(s => s.Department == d.Name)
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = departments, count = departments.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching departments for group {GroupId}", groupId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/Department
        /// Create a new department
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequest(new { success = false, message = "Department name is required" });

                using var context = await _contextFactory.CreateDbContextAsync();

                // Check for duplicate name
                var exists = await context.Set<Department>()
                    .AnyAsync(d => d.Name == dto.Name && d.GroupId == dto.GroupId);

                if (exists)
                    return Conflict(new { success = false, message = $"Department '{dto.Name}' already exists in this group" });

                var department = new Department
                {
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    GroupId = dto.GroupId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Set<Department>().Add(department);
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Department created: {Name} (ID: {Id})", department.Name, department.Id);

                return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, new
                {
                    success = true,
                    message = "Department created successfully",
                    data = new DepartmentResponseDto
                    {
                        Id = department.Id,
                        Name = department.Name,
                        Description = department.Description,
                        GroupId = department.GroupId,
                        CreatedAt = department.CreatedAt,
                        UpdatedAt = department.UpdatedAt,
                        MemberCount = 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating department");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PUT: api/Department/{id}
        /// Update entire department (full update)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var department = await context.Set<Department>().FindAsync(id);
                if (department == null)
                    return NotFound(new { success = false, message = $"Department with ID {id} not found" });

                // Check for duplicate name (excluding current)
                if (!string.IsNullOrEmpty(dto.Name))
                {
                    var exists = await context.Set<Department>()
                        .AnyAsync(d => d.Name == dto.Name && d.GroupId == (dto.GroupId ?? department.GroupId) && d.Id != id);

                    if (exists)
                        return Conflict(new { success = false, message = $"Department '{dto.Name}' already exists" });
                }

                department.Name = dto.Name?.Trim() ?? department.Name;
                department.Description = dto.Description?.Trim();
                department.GroupId = dto.GroupId ?? department.GroupId;
                department.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Department updated: {Name} (ID: {Id})", department.Name, department.Id);

                return Ok(new
                {
                    success = true,
                    message = "Department updated successfully",
                    data = new DepartmentResponseDto
                    {
                        Id = department.Id,
                        Name = department.Name,
                        Description = department.Description,
                        GroupId = department.GroupId,
                        CreatedAt = department.CreatedAt,
                        UpdatedAt = department.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating department {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PATCH: api/Department/{id}
        /// Partially update department
        /// </summary>
        [HttpPatch("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> PatchDepartment(int id, [FromBody] PatchDepartmentDto dto)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var department = await context.Set<Department>().FindAsync(id);
                if (department == null)
                    return NotFound(new { success = false, message = $"Department with ID {id} not found" });

                if (!string.IsNullOrEmpty(dto.Name))
                {
                    // Check for duplicate
                    var exists = await context.Set<Department>()
                        .AnyAsync(d => d.Name == dto.Name && d.Id != id);
                    if (exists)
                        return Conflict(new { success = false, message = $"Department '{dto.Name}' already exists" });

                    department.Name = dto.Name.Trim();
                }

                if (dto.Description != null)
                    department.Description = dto.Description.Trim();

                if (dto.GroupId.HasValue)
                    department.GroupId = dto.GroupId.Value;

                department.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Department patched: {Name} (ID: {Id})", department.Name, department.Id);

                return Ok(new { success = true, message = "Department updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error patching department {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// DELETE: api/Department/{id}
        /// Delete a department
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var department = await context.Set<Department>().FindAsync(id);
                if (department == null)
                    return NotFound(new { success = false, message = $"Department with ID {id} not found" });

                // Check if department has members
                var memberCount = await context.Set<subuser>()
                    .CountAsync(s => s.Department == department.Name);

                if (memberCount > 0)
                    return BadRequest(new { 
                        success = false, 
                        message = $"Cannot delete department with {memberCount} members. Reassign members first." 
                    });

                context.Set<Department>().Remove(department);
                await context.SaveChangesAsync();

                _logger.LogWarning("🗑️ Department deleted: {Name} (ID: {Id})", department.Name, id);

                return Ok(new { success = true, message = "Department deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting department {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/Department/{id}/members
        /// Get all members (subusers) in a department
        /// </summary>
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetDepartmentMembers(int id)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var department = await context.Set<Department>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (department == null)
                    return NotFound(new { success = false, message = $"Department with ID {id} not found" });

                var members = await context.Set<subuser>()
                    .AsNoTracking()
                    .Where(s => s.Department == department.Name)
                    .Select(s => new
                    {
                        email = s.subuser_email,
                        name = s.Name,
                        status = s.status,
                        role = s.Role,
                        createdAt = s.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    departmentName = department.Name,
                    data = members,
                    count = members.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching department members for {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/Department/statistics
        /// Get department statistics
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetDepartmentStatistics()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var departments = await context.Set<Department>()
                    .AsNoTracking()
                    .ToListAsync();

                var stats = new
                {
                    totalDepartments = departments.Count,
                    departmentsWithMembers = 0,
                    topDepartments = new List<object>()
                };

                // Get member counts per department
                var departmentStats = new List<object>();
                foreach (var dept in departments.Take(10))
                {
                    var memberCount = await context.Set<subuser>()
                        .CountAsync(s => s.Department == dept.Name);
                    
                    departmentStats.Add(new
                    {
                        id = dept.Id,
                        name = dept.Name,
                        memberCount
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalDepartments = departments.Count,
                        topDepartments = departmentStats.OrderByDescending(d => ((dynamic)d).memberCount).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching department statistics");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    #region Department Entity & DTOs

    /// <summary>
    /// Department Entity
    /// </summary>
    [Table("Departments")]
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? GroupId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DepartmentResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? GroupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MemberCount { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? GroupId { get; set; }
    }

    public class UpdateDepartmentDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? GroupId { get; set; }
    }

    public class PatchDepartmentDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? GroupId { get; set; }
    }

    #endregion
}
