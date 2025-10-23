using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BitRaserApiProject.Models.DTOs
{
    /// <summary>
    /// Comprehensive DTOs for Subusers (Team Members / Child Users) Management
    /// Based on DOTNET-SUBUSERS-API.md documentation
    /// </summary>
    
    public class SubuserDetailedDto
    {
        public int Id { get; set; }
        public int ParentUserId { get; set; }
        public string ParentUserName { get; set; } = string.Empty;
        public string ParentUserEmail { get; set; } = string.Empty;
        public string? SubuserUsername { get; set; } // Added
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string Role { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public int? AssignedMachines { get; set; }
        public int? MaxMachines { get; set; }
        public int? GroupId { get; set; } // Added
        public string? GroupName { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public bool CanCreateSubusers { get; set; }
        public bool CanViewReports { get; set; }
        public bool CanManageMachines { get; set; }
        public bool CanAssignLicenses { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateSubuserDto
    {
        [MaxLength(100)]
        public string? SubuserUsername { get; set; } // Optional
   
        [MaxLength(100)]
        public string? Name { get; set; } // Optional
    
        [Required, EmailAddress, MaxLength(100)]
     public string Email { get; set; } = string.Empty; // Required
    
    [Required, MinLength(8)]
      public string Password { get; set; } = string.Empty; // Required
        
   [MaxLength(20)]
        public string? Phone { get; set; } // Optional
        
        [MaxLength(100)]
      public string? JobTitle { get; set; } // Optional
        
        [MaxLength(100)]
        public string? Department { get; set; } // Optional
     
   [MaxLength(50)]
        public string? Role { get; set; } // Optional - will default to "subuser"
  
        [MaxLength(50)]
        public string? AccessLevel { get; set; } // Optional - will default to "limited"
  
        public int? MaxMachines { get; set; } // Optional - will default to 5
 public int? GroupId { get; set; } // Optional
        public bool? CanCreateSubusers { get; set; } // Optional - will default to false
        public bool? CanViewReports { get; set; } // Optional - will default to true
        public bool? CanManageMachines { get; set; } // Optional - will default to false
        public bool? CanAssignLicenses { get; set; } // Optional - will default to false
        public bool? EmailNotifications { get; set; } // Optional - will default to true
        public bool? SystemAlerts { get; set; } // Optional - will default to true
        
        [MaxLength(500)]
        public string? Notes { get; set; } // Optional
    }

    public class UpdateSubuserDto
    {
        [MaxLength(100)]
      public string? SubuserUsername { get; set; } // Added
        
[MaxLength(100)]
        public string? Name { get; set; }
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
   [MaxLength(100)]
        public string? JobTitle { get; set; }
        
        [MaxLength(100)]
        public string? Department { get; set; }
        
        [MaxLength(50)]
        public string? Role { get; set; }
      
        [MaxLength(50)]
   public string? AccessLevel { get; set; }
        
        public int? MaxMachines { get; set; }
 public int? GroupId { get; set; }
        
        [MaxLength(50)]
    public string? Status { get; set; }
        
        public bool? CanCreateSubusers { get; set; }
        public bool? CanViewReports { get; set; }
        public bool? CanManageMachines { get; set; }
        public bool? CanAssignLicenses { get; set; }
        public bool? EmailNotifications { get; set; }
        public bool? SystemAlerts { get; set; }
  
        [MaxLength(500)]
  public string? Notes { get; set; }
    }

    public class SubuserChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required, Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AssignMachinesToSubuserDto
    {
        [Required]
        public int SubuserId { get; set; }
        
        [Required]
        public List<int> MachineIds { get; set; } = new();
    }

    public class AssignLicensesToSubuserDto
    {
        [Required]
        public int SubuserId { get; set; }
        
        [Required]
        public List<int> LicenseIds { get; set; } = new();
    }

    public class SubuserStatsDto
    {
        public int TotalSubusers { get; set; }
        public int ActiveSubusers { get; set; }
        public int InactiveSubusers { get; set; }
        public int SuspendedSubusers { get; set; }
        public int VerifiedSubusers { get; set; }
        public int UnverifiedSubusers { get; set; }
        public Dictionary<string, int> SubusersByRole { get; set; } = new();
        public Dictionary<string, int> SubusersByAccessLevel { get; set; } = new();
        public Dictionary<string, int> SubusersByDepartment { get; set; } = new();
        public List<TopParentUserDto> TopParentUsersWithSubusers { get; set; } = new();
    }

    public class TopParentUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int SubuserCount { get; set; }
    }
}
