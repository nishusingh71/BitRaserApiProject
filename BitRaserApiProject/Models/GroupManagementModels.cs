using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Group Management DTOs - Based on BitRaser Manage Groups Design
    /// Complete models matching the UI screenshots exactly
    /// </summary>

    #region Manage Groups Main Response

    /// <summary>
    /// GET /api/GroupManagement - Manage Groups Page Response
    /// </summary>
    public class ManageGroupsResponseDto
    {
        public string Title { get; set; } = "Manage Groups";
        public string Description { get; set; } = "Create and manage user groups with specific permissions";
        public List<GroupCardDto> Groups { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string Showing { get; set; } = string.Empty;
    }

    #endregion

    #region Group Card (List View)

    /// <summary>
    /// Group Card for List Display (Matches screenshot design)
    /// </summary>
    public class GroupCardDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
 
        // Statistics
        public int UserCount { get; set; }
        public int LicenseCount { get; set; }
        
        // Permissions Display (First 3 visible)
        public List<string> Permissions { get; set; } = new();
        public int MorePermissions { get; set; } // "+2 more" indicator
  
        public DateTime CreatedDate { get; set; }
   
        // UI Helper
        public string CreatedDateFormatted => CreatedDate.ToString("dd/MM/yyyy");
    }

    #endregion

    #region Group Detail (Edit/View)

    /// <summary>
    /// GET /api/GroupManagement/{id} - Group Detail for Editing
    /// </summary>
    public class GroupDetailDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int LicenseAllocation { get; set; }
        public int UserCount { get; set; }
        public List<string> Permissions { get; set; } = new();
        public DateTime CreatedDate { get; set; }
    }

    #endregion

    #region Create Group (Add New Group)

    /// <summary>
    /// POST /api/GroupManagement - Create Group Request
    /// Matches "Add New Group" form from screenshot
    /// </summary>
    public class CreateGroupDto
    {
        [Required(ErrorMessage = "Group name is required")]
        [MaxLength(100, ErrorMessage = "Group name cannot exceed 100 characters")]
        public string GroupName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Range(1, 10000, ErrorMessage = "License allocation must be between 1 and 10000")]
        public int LicenseAllocation { get; set; } = 100;

        [Required(ErrorMessage = "At least one permission must be selected")]
        public List<string> Permissions { get; set; } = new();
    }

    /// <summary>
    /// POST /api/GroupManagement - Create Group Response
    /// </summary>
    public class CreateGroupResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Update Group (Edit Group)

    /// <summary>
    /// PUT /api/GroupManagement/{id} - Update Group Request
    /// Matches "Edit Group" form from screenshot
    /// </summary>
    public class UpdateGroupDto
    {
        [MaxLength(100)]
        public string? GroupName { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(1, 10000)]
        public int? LicenseAllocation { get; set; }

        public List<string>? Permissions { get; set; }
    }

    #endregion

    #region Available Permissions

    /// <summary>
    /// GET /api/GroupManagement/available-permissions
    /// Returns all available permissions with categories
    /// </summary>
    public class PermissionCategoriesDto
    {
        public List<PermissionCategoryDto> Categories { get; set; } = new();
    }

    public class PermissionCategoryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryLabel { get; set; } = string.Empty;
        public List<PermissionOptionDto> Permissions { get; set; } = new();
    }

    public class PermissionOptionDto
    {
        public string Value { get; set; } = string.Empty; // e.g., "BASIC_ACCESS"
        public string Label { get; set; } = string.Empty; // e.g., "Basic Access"
        public string Description { get; set; } = string.Empty;
        public bool IsChecked { get; set; } = false; // For UI binding
    }

    #endregion

    #region Group Members

    /// <summary>
    /// GET /api/GroupManagement/{id}/members - Get Group Members
    /// </summary>
    public class GroupMembersDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<GroupMemberItemDto> Members { get; set; } = new();
        public int TotalMembers { get; set; }
    }

    public class GroupMemberItemDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserType { get; set; } = "user"; // "user" or "subuser"
        public DateTime JoinedDate { get; set; }
        public string Status { get; set; } = "active"; // active, inactive, suspended
        public string JoinedDateFormatted => JoinedDate.ToString("dd/MM/yyyy");

        // ✅ New fields for UI match
        public string? Department { get; set; }
        public string? LicenseType { get; set; } = "Standard"; // Enterprise, Professional, Standard
        public string? Profile { get; set; } = "Member"; // Job Title e.g. Senior Developer
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// POST /api/GroupManagement/{id}/add-users - Bulk Add Users to Group
    /// </summary>
    public class BulkAddUsersToGroupDto
    {
        [Required]
        public int GroupId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one user email is required")]
        public List<string> UserEmails { get; set; } = new();
    }

    /// <summary>
    /// POST /api/GroupManagement/{id}/remove-users - Bulk Remove Users from Group
    /// </summary>
    public class BulkRemoveUsersFromGroupDto
    {
        [Required]
        public int GroupId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one user email is required")]
        public List<string> UserEmails { get; set; } = new();
    }

    /// <summary>
    /// Response for Bulk Operations
    /// </summary>
    public class BulkGroupOperationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> FailedEmails { get; set; } = new();
        public List<string> SuccessEmails { get; set; } = new();
    }

    #endregion

    #region Group Statistics

    /// <summary>
    /// GET /api/GroupManagement/statistics - Group Statistics Dashboard
    /// </summary>
    public class GroupStatisticsDto
    {
        public int TotalGroups { get; set; }
        public int TotalUsers { get; set; }
        public int TotalLicenses { get; set; }
        public int AverageUsersPerGroup { get; set; }
        public List<GroupStatsItemDto> TopGroups { get; set; } = new();
    }

    public class GroupStatsItemDto
    {
        public string GroupName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int LicenseCount { get; set; }
        public double Percentage { get; set; }
    }

    #endregion

    #region Search & Filter

    /// <summary>
    /// Query parameters for searching groups
    /// </summary>
    public class GroupSearchDto
    {
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "name"; // name, users, licenses, created
        public string? SortOrder { get; set; } = "asc"; // asc, desc
        public string? Status { get; set; } // active, inactive
        public int? MinUsers { get; set; }
        public int? MaxUsers { get; set; }
    }

    #endregion

    #region Permission Assignment

    /// <summary>
    /// POST /api/GroupManagement/{id}/assign-permission - Assign single permission
    /// </summary>
    public class AssignPermissionDto
    {
        [Required]
        public int GroupId { get; set; }

        [Required]
        public string PermissionName { get; set; } = string.Empty;
    }

    /// <summary>
    /// POST /api/GroupManagement/{id}/revoke-permission - Revoke single permission
    /// </summary>
    public class RevokePermissionDto
    {
        [Required]
        public int GroupId { get; set; }

        [Required]
        public string PermissionName { get; set; } = string.Empty;
    }

    #endregion

    #region Group Templates

    /// <summary>
    /// GET /api/GroupManagement/templates - Predefined group templates
    /// </summary>
    public class GroupTemplatesDto
    {
        public List<GroupTemplateDto> Templates { get; set; } = new();
    }

    public class GroupTemplateDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> DefaultPermissions { get; set; } = new();
        public int RecommendedLicenses { get; set; }
        public string Icon { get; set; } = string.Empty;
    }

    #endregion

    #region Validation Results

    /// <summary>
    /// Response for validation errors
    /// </summary>
    public class GroupValidationErrorDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public Dictionary<string, List<string>> FieldErrors { get; set; } = new();
    }

    #endregion
}
