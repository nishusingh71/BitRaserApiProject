using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    // Dashboard-specific DTOs (renamed to avoid conflicts with Models/DTOs/UserDtos.cs)
    
    // Models/Auth/DashboardLoginRequestDto.cs
    public class DashboardLoginRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // Models/Auth/DashboardLoginResponseDto.cs
    public class DashboardLoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DashboardUserDto User { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    // Models/Auth/DashboardUserDto.cs
    public class DashboardUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string TimeZone { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
    }

    // Models/Dashboard/DashboardOverviewDto.cs
    public class DashboardOverviewDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalLicenses { get; set; }
        public int UsedLicenses { get; set; }
        public int TotalMachines { get; set; }
        public int ActiveMachines { get; set; }
        public List<ActivityDto> RecentActivities { get; set; } = new();
    }

    // Models/Dashboard/ActivityDto.cs
    public class ActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // Models/Users/AdminUserDto.cs
    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Groups { get; set; } = new();
        public int LicenseCount { get; set; }
    }

    // Models/Users/CreateUserDto.cs
    public class CreateUserDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public List<string> GroupIds { get; set; } = new();
    }

    // Models/Licenses/AdminLicenseDto.cs
    public class AdminLicenseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Features { get; set; } = string.Empty;
    }

    // Models/Licenses/BulkAssignLicensesDto.cs
    public class BulkAssignLicensesDto
    {
        public List<string> UserIds { get; set; } = new();
        public string LicenseType { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public List<string> Features { get; set; } = new();
    }

    // Models/Common/PagedResultDto.cs
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // Models/Common/ApiResponseDto.cs
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
