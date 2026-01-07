using  System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DSecureApi.Models
{
    /// <summary>
    /// Machine Log Entity - Tracks machine-level events and activities
    /// </summary>
    public class MachineLog
    {
        [Key]
        public int Id { get; set; }

        // Machine Reference
        [Required]
        [MaxLength(255)]
        public string FingerprintHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string MachineName { get; set; } = string.Empty;

        // Log Details
        [Required]
        [MaxLength(50)]
        public string LogType { get; set; } = "info"; // info, warning, error, critical, security

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "system"; // system, security, application, network, performance

        [Required]
        [MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EventCode { get; set; }

        [MaxLength(50)]
        public string Severity { get; set; } = "low"; // low, medium, high, critical

        // Source Information
        [MaxLength(100)]
        public string? Source { get; set; }

        [MaxLength(100)]
        public string? ProcessName { get; set; }

        public int? ProcessId { get; set; }

        // User Information
        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(255)]
        public string? UserEmail { get; set; }

        // Network Information
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(100)]
        public string? Hostname { get; set; }

        // Additional Data
        public string? AdditionalDataJson { get; set; }

        public string? StackTrace { get; set; }

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "unread"; // unread, read, resolved, ignored

        public bool IsAcknowledged { get; set; } = false;

        [MaxLength(255)]
        public string? AcknowledgedByEmail { get; set; }

        public DateTime? AcknowledgedAt { get; set; }

        // Timestamps
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// System Log Entity - Tracks system-level events and API activities
    /// </summary>
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string LogType { get; set; } = "info"; // info, warning, error, critical, audit

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "system"; // system, api, database, authentication, authorization

        [Required]
        [MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EventCode { get; set; }

        [MaxLength(50)]
        public string Severity { get; set; } = "low"; // low, medium, high, critical

        // User Information
        [MaxLength(255)]
        public string? UserEmail { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(50)]
        public string? UserRole { get; set; }

        // Request Information
        [MaxLength(10)]
        public string? HttpMethod { get; set; }

        [MaxLength(500)]
        public string? RequestPath { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public int? StatusCode { get; set; }

        // Additional Data
        public string? RequestBody { get; set; }

        public string? ResponseBody { get; set; }

        public string? AdditionalDataJson { get; set; }

        public string? StackTrace { get; set; }

        public string? ExceptionType { get; set; }

        // Performance
        public long? ExecutionTimeMs { get; set; }

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "unread"; // unread, read, resolved, investigating

        public bool IsAcknowledged { get; set; } = false;

        [MaxLength(255)]
        public string? AcknowledgedByEmail { get; set; }

        public DateTime? AcknowledgedAt { get; set; }

        // Timestamps
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Performance Report Entity - Tracks system performance metrics
    /// </summary>
    public class PerformanceReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = "performance"; // performance, system, user

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // CPU, Memory, Disk, Network, etc.

        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }

        // Metrics
        [Column(TypeName = "decimal(18,2)")]
        public decimal AverageValue { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinValue { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxValue { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentValue { get; set; } = 0;

        [MaxLength(20)]
        public string Unit { get; set; } = "%"; // %, GB, MB, ms, etc.

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "good"; // good, warning, critical

        public int TotalIncidents { get; set; } = 0;

        // Metadata
        [MaxLength(255)]
        public string? GeneratedByEmail { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? DataJson { get; set; }
    }

    /// <summary>
    /// Audit Report Entity - Tracks compliance audits
    /// </summary>
    public class AuditReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string AuditType { get; set; } = "license"; // license, user, system, security

        public DateTime AuditStartDate { get; set; }
        
        public DateTime AuditEndDate { get; set; }

        // Audit Summary
        public int TotalRecords { get; set; } = 0;
        
        public int PassedRecords { get; set; } = 0;
        
        public int FailedRecords { get; set; } = 0;
        
        public int WarningRecords { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ComplianceScore { get; set; } = 0; // 0-100

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "completed"; // pending, in_progress, completed, failed

        [MaxLength(50)]
        public string Severity { get; set; } = "low"; // low, medium, high, critical

        // Findings
        public int TotalFindings { get; set; } = 0;
        
        public int CriticalFindings { get; set; } = 0;
        
        public int ResolvedFindings { get; set; } = 0;

        // Metadata
        [MaxLength(255)]
        public string? GeneratedByEmail { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Summary { get; set; }

        [MaxLength(1000)]
        public string? Recommendations { get; set; }

        public string? DetailedDataJson { get; set; }

        [MaxLength(500)]
        public string? FilePath { get; set; }
    }

    /// <summary>
    /// Enhanced Subuser Entity - Team members with granular permissions
    /// </summary>
    public class EnhancedSubuser
    {
        [Key]
        public int Id { get; set; }

        // Parent User Reference
        [MaxLength(255)]
        public string ParentUserEmail { get; set; } = string.Empty;

        // Subuser Details
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        // Role & Permissions
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "subuser"; // subuser, team_member, limited_admin

        [MaxLength(50)]
        public string AccessLevel { get; set; } = "limited"; // full, limited, read_only

        public string? PermissionsJson { get; set; }

        // Machine & License Access
        public int? AssignedMachines { get; set; } = 0;
        
        public int? MaxMachines { get; set; } = 5;

        public string? MachineIdsJson { get; set; }

        public string? LicenseIdsJson { get; set; }

        // Group Access
        [MaxLength(100)]
        public string? GroupName { get; set; }

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, inactive, suspended

        public bool IsEmailVerified { get; set; } = false;

        public bool CanCreateSubusers { get; set; } = false;

        public bool CanViewReports { get; set; } = true;

        public bool CanManageMachines { get; set; } = false;

        public bool CanAssignLicenses { get; set; } = false;

        // Notifications
        public bool EmailNotifications { get; set; } = true;

        public bool SystemAlerts { get; set; } = true;

        // Session & Security
        [MaxLength(500)]
        public string? LastLoginIp { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockedUntil { get; set; }

        // Audit
        [MaxLength(255)]
        public string? CreatedByEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(255)]
        public string? UpdatedByEmail { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Machine License Assignment - Links machines to license keys
    /// </summary>
    public class MachineLicense
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FingerprintHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string LicenseKey { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ProductName { get; set; } = "BitRaser";

        [MaxLength(50)]
        public string LicenseType { get; set; } = "standard"; // trial, standard, premium, enterprise

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; }

        public int DaysValid { get; set; } = 365;

        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, expired, revoked

        [MaxLength(255)]
        public string? AssignedByEmail { get; set; }

        public string? LicenseDetailsJson { get; set; }
    }
}
