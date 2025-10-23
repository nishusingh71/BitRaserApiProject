using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Subusers Management Models - Based on D-Secure Manage Subusers UI
    /// Screenshot 2: Manage Subusers page with role, status, department filters
    /// </summary>

    #region Subusers List

    /// <summary>
    /// Subusers list response
    /// </summary>
  public class SubusersManagementListDto
    {
        public List<SubuserManagementItemDto> Subusers { get; set; } = new();
    public int TotalCount { get; set; }
     public int Page { get; set; }
        public int PageSize { get; set; }
  public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// Individual subuser item for management table
    /// </summary>
    public class SubuserManagementItemDto
    {
  public int SubuserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // user, operator, admin
        public string Status { get; set; } = string.Empty; // active, inactive, pending
  public string Department { get; set; } = string.Empty;
 public DateTime? LastLogin { get; set; }
        public bool CanView { get; set; } = true;
        public bool CanEdit { get; set; } = true;
        public bool CanManagePermissions { get; set; } = true;
  public bool CanReset { get; set; } = true;
        public bool CanDeactivate { get; set; } = true;
        public bool CanDelete { get; set; } = true;
    }

    #endregion

    #region Subusers Filters

 /// <summary>
    /// Subusers management filters (from Screenshot 2)
    /// </summary>
    public class SubusersManagementFiltersDto
    {
        [MaxLength(200)]
     public string? Search { get; set; }

    public string? Role { get; set; } // All Roles, user, operator, admin

        public string? Status { get; set; } // All Statuses, active, inactive, pending

        public string? Department { get; set; } // All Departments, Finance, Operations, IT, HR

      public bool ShowUniqueRecordsOnly { get; set; } = false;

      [MaxLength(50)]
        public string? SortBy { get; set; } = "Email"; // Email, Role, Status, Department, Last Login

        public int SortDirection { get; set; } = 1; // 1 = Ascending, -1 = Descending

        public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 5;
    }

    #endregion

    #region Subuser Actions

    /// <summary>
    /// Deactivate subuser request
  /// </summary>
    public class DeactivateSubuserRequest
    {
 [Required]
  public int SubuserId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Reset subuser password request
    /// </summary>
    public class ResetSubuserPasswordRequest
    {
        [Required]
        public int SubuserId { get; set; }

        public bool SendEmail { get; set; } = true;
    }

 /// <summary>
    /// Update subuser permissions request
    /// </summary>
    public class UpdateSubuserPermissionsRequest
    {
 [Required]
        public int SubuserId { get; set; }

        [Required]
        public List<string> Permissions { get; set; } = new();
    }

#endregion

    #region Export Subusers

    /// <summary>
    /// Export subusers request
    /// </summary>
    public class ExportSubusersRequest
    {
      public List<int>? SubuserIds { get; set; }

        public string ExportFormat { get; set; } = "CSV"; // CSV, Excel

        public bool ExportAll { get; set; } = false;

        public SubusersManagementFiltersDto? Filters { get; set; }
    }

    #endregion

    #region Filter Options

    /// <summary>
    /// Available filter options
  /// </summary>
    public class SubusersFilterOptionsDto
    {
   public List<string> Roles { get; set; } = new() 
        { 
         "All Roles", 
            "user", 
    "operator", 
          "admin" 
   };

        public List<string> Statuses { get; set; } = new() 
        { 
            "All Statuses", 
      "active", 
   "inactive", 
 "pending" 
        };

    public List<string> Departments { get; set; } = new() 
 { 
    "All Departments",
    "Finance",
   "Operations",
   "IT",
            "HR",
     "Never"
        };

        public List<string> SortOptions { get; set; } = new() 
        { 
       "Email", 
      "Role", 
     "Status", 
  "Department",
            "Last Login"
 };
  }

    #endregion

    #region Subuser Statistics

    /// <summary>
    /// Subusers statistics
    /// </summary>
    public class SubusersStatisticsDto
    {
    public int TotalSubusers { get; set; }
        public int ActiveSubusers { get; set; }
   public int InactiveSubusers { get; set; }
 public int PendingSubusers { get; set; }
        public Dictionary<string, int> SubusersByRole { get; set; } = new();
        public Dictionary<string, int> SubusersByDepartment { get; set; } = new();
        public List<SubuserManagementItemDto> RecentlyActive { get; set; } = new();
    }

    #endregion
}
