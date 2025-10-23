using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Machines Management Models - Based on D-Secure Machines UI
    /// Screenshot 3: Machines page with erase option, license, status filters
    /// </summary>

    #region Machines List

    /// <summary>
    /// Machines list response
  /// </summary>
    public class MachinesManagementListDto
    {
      public List<MachineManagementItemDto> Machines { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// Individual machine item for management table
    /// </summary>
    public class MachineManagementItemDto
    {
        public string Hostname { get; set; } = string.Empty;
    public string EraseOption { get; set; } = string.Empty; // Secure Erase, Quick Erase
        public string License { get; set; } = string.Empty; // Enterprise, Basic
        public string Status { get; set; } = string.Empty; // online, offline
    public bool CanView { get; set; } = true;
        public bool CanEdit { get; set; } = true;
    }

    #endregion

    #region Machines Filters

    /// <summary>
  /// Machines management filters (from Screenshot 3)
    /// </summary>
    public class MachinesManagementFiltersDto
    {
        [MaxLength(200)]
   public string? Search { get; set; }

      public string? EraseOption { get; set; } // All Options, Secure Erase, Quick Erase

   public string? License { get; set; } // All Licenses, Enterprise, Basic

        public string? Status { get; set; } // All Statuses, online, offline

        public bool ShowUniqueRecordsOnly { get; set; } = false;

        [MaxLength(50)]
        public string? SortBy { get; set; } = "Hostname"; // Hostname, EraseOption, License, Status

public int SortDirection { get; set; } = 1; // 1 = Ascending, -1 = Descending

        public int Page { get; set; } = 1;

 public int PageSize { get; set; } = 5;
    }

    #endregion

    #region Export Machines

    /// <summary>
    /// Export machines request
 /// </summary>
    public class ExportMachinesRequest
    {
public List<string>? Hostnames { get; set; }

        public string ExportFormat { get; set; } = "CSV"; // CSV, Excel

        public bool ExportAll { get; set; } = false;

        public MachinesManagementFiltersDto? Filters { get; set; }
    }

    #endregion

    #region Machine Details

    /// <summary>
    /// Detailed machine information
  /// </summary>
    public class MachineDetailDto
    {
        public string FingerprintHash { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
   public string MacAddress { get; set; } = string.Empty;
   public string PhysicalDriveId { get; set; } = string.Empty;
        public string CpuId { get; set; } = string.Empty;
   public string BiosSerial { get; set; } = string.Empty;
   public string OsVersion { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? SubuserEmail { get; set; }
public string EraseOption { get; set; } = string.Empty;
  public string License { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
   public bool LicenseActivated { get; set; }
        public DateTime? LicenseActivationDate { get; set; }
   public int LicenseDaysValid { get; set; }
        public int DemoUsageCount { get; set; }
  public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string VmStatus { get; set; } = string.Empty;
    }

    #endregion

    #region Machine Actions

    /// <summary>
    /// Update machine license request
  /// </summary>
  public class UpdateMachineLicenseRequest
    {
 [Required]
  public string FingerprintHash { get; set; } = string.Empty;

        [Required]
        public string License { get; set; } = string.Empty; // Enterprise, Basic

        public bool Activate { get; set; } = true;

public int DaysValid { get; set; } = 365;
    }

    /// <summary>
/// Update machine status request
    /// </summary>
 public class UpdateMachineStatusRequest
    {
  [Required]
 public string FingerprintHash { get; set; } = string.Empty;

[Required]
        public string Status { get; set; } = string.Empty; // online, offline
    }

    #endregion

    #region Filter Options

    /// <summary>
    /// Available filter options
    /// </summary>
  public class MachinesFilterOptionsDto
    {
 public List<string> EraseOptions { get; set; } = new() 
  { 
     "All Options", 
       "Secure Erase", 
            "Quick Erase" 
  };

  public List<string> Licenses { get; set; } = new() 
        { 
   "All Licenses", 
   "Enterprise", 
            "Basic" 
        };

     public List<string> Statuses { get; set; } = new() 
        { 
     "All Statuses", 
     "online", 
     "offline" 
        };

        public List<string> SortOptions { get; set; } = new() 
 { 
   "Hostname", 
     "EraseOption", 
      "License", 
     "Status" 
        };
    }

    #endregion

    #region Machine Statistics

    /// <summary>
    /// Machines statistics
    /// </summary>
    public class MachinesStatisticsDto
    {
  public int TotalMachines { get; set; }
        public int OnlineMachines { get; set; }
    public int OfflineMachines { get; set; }
        public int LicensedMachines { get; set; }
   public int UnlicensedMachines { get; set; }
  public Dictionary<string, int> MachinesByLicense { get; set; } = new();
        public Dictionary<string, int> MachinesByEraseOption { get; set; } = new();
        public List<MachineManagementItemDto> RecentlyAdded { get; set; } = new();
    }

    #endregion
}
