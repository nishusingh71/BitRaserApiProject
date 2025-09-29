using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BitRaserApiProject.Models.DTOs
{
    /// <summary>
    /// Data Transfer Objects for API requests and responses
    /// These DTOs prevent JSON serialization issues with navigation properties
    /// </summary>
    
    public class UserDto
    {
        public int user_id { get; set; }
        
        [Required, MaxLength(255)]
        public string user_name { get; set; } = string.Empty;
        
        [Required, MaxLength(255)]
        public string user_email { get; set; } = string.Empty;
        
        public bool is_private_cloud { get; set; } = false;
        public bool private_api { get; set; } = false;
        
        [MaxLength(20)]
        public string phone_number { get; set; } = string.Empty;
        
        public string payment_details_json { get; set; } = string.Empty;
        public string license_details_json { get; set; } = string.Empty;
        
        // Role information for responses
        public List<string> roles { get; set; } = new List<string>();
        public List<string> permissions { get; set; } = new List<string>();
    }

    public class UserCreateDto
    {
        [Required, MaxLength(255)]
        public string user_name { get; set; } = string.Empty;
        
        [Required, MaxLength(255), EmailAddress]
        public string user_email { get; set; } = string.Empty;
        
        [Required, MaxLength(255)]
        public string user_password { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string phone_number { get; set; } = string.Empty;
        
        public bool is_private_cloud { get; set; } = false;
        public bool private_api { get; set; } = false;
        
        public string payment_details_json { get; set; } = "{}";
        public string license_details_json { get; set; } = "{}";
        
        // Optional roles to assign on creation
        public List<string> initialRoles { get; set; } = new List<string>();
    }

    public class UserUpdateDto
    {
        [MaxLength(255)]
        public string? user_name { get; set; }
        
        [MaxLength(20)]
        public string? phone_number { get; set; }
        
        public bool? is_private_cloud { get; set; }
        public bool? private_api { get; set; }
        
        public string? payment_details_json { get; set; }
        public string? license_details_json { get; set; }
    }

    public class SubuserDto
    {
        public int subuser_id { get; set; }
        public string subuser_email { get; set; } = string.Empty;
        public string user_email { get; set; } = string.Empty;
        
        // Role information for responses
        public List<string> roles { get; set; } = new List<string>();
        public List<string> permissions { get; set; } = new List<string>();
    }

    public class SubuserCreateDto
    {
        [Required, MaxLength(255), EmailAddress]
        public string subuser_email { get; set; } = string.Empty;
        
        [Required, MaxLength(255)]
        public string subuser_password { get; set; } = string.Empty;
        
        [Required, MaxLength(255), EmailAddress]
        public string user_email { get; set; } = string.Empty;
        
        // Optional roles to assign on creation
        public List<string> initialRoles { get; set; } = new List<string>();
    }

    public class RoleAssignmentDto
    {
        public List<string> rolesToAdd { get; set; } = new List<string>();
        public List<string> rolesToRemove { get; set; } = new List<string>();
    }

    public class UserRoleInfoDto
    {
        public string userEmail { get; set; } = string.Empty;
        public string userType { get; set; } = string.Empty; // "user" or "subuser"
        public List<RoleInfo> roles { get; set; } = new List<RoleInfo>();
        public List<PermissionInfo> permissions { get; set; } = new List<PermissionInfo>();
    }

    public class RoleInfo
    {
        public int roleId { get; set; }
        public string roleName { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public int hierarchyLevel { get; set; }
        public DateTime assignedAt { get; set; }
        public string assignedBy { get; set; } = string.Empty;
    }

    public class PermissionInfo
    {
        public int permissionId { get; set; }
        public string permissionName { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
    }

    public class LoginRequestDto
    {
        [Required, EmailAddress]
        public string email { get; set; } = string.Empty;
        
        [Required]
        public string password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string token { get; set; } = string.Empty;
        public string userType { get; set; } = string.Empty; // "user" or "subuser"
        public string email { get; set; } = string.Empty;
        public List<string> roles { get; set; } = new List<string>();
        public List<string> permissions { get; set; } = new List<string>();
        public DateTime expiresAt { get; set; }
    }

    public class AccessCheckDto
    {
        [Required]
        public string operation { get; set; } = string.Empty;
        
        public string? resourceOwner { get; set; }
    }

    public class AccessCheckResponseDto
    {
        public string userEmail { get; set; } = string.Empty;
        public string operation { get; set; } = string.Empty;
        public string? resourceOwner { get; set; }
        public bool hasAccess { get; set; }
        public string userType { get; set; } = string.Empty;
        public List<string> requiredPermissions { get; set; } = new List<string>();
        public List<string> userPermissions { get; set; } = new List<string>();
        public string reason { get; set; } = string.Empty;
    }

    public class ValidationErrorDto
    {
        public string field { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string code { get; set; } = string.Empty;
    }

    public class ApiResponseDto<T>
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public T? data { get; set; }
        public List<ValidationErrorDto> errors { get; set; } = new List<ValidationErrorDto>();
        public DateTime timestamp { get; set; } = DateTime.UtcNow;
    }

    public class PagedResponseDto<T>
    {
        public List<T> items { get; set; } = new List<T>();
        public int totalCount { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public int totalPages { get; set; }
        public bool hasNextPage { get; set; }
        public bool hasPreviousPage { get; set; }
    }

    // Extension methods to convert between entities and DTOs
    public static class DtoExtensions
    {
        public static UserDto ToDto(this users user, List<string>? roles = null, List<string>? permissions = null)
        {
            return new UserDto
            {
                user_id = user.user_id,
                user_name = user.user_name,
                user_email = user.user_email,
                is_private_cloud = user.is_private_cloud ?? false,
                private_api = user.private_api ?? false,
                phone_number = user.phone_number ?? string.Empty,
                payment_details_json = user.payment_details_json ?? "{}",
                license_details_json = user.license_details_json ?? "{}",
                roles = roles ?? new List<string>(),
                permissions = permissions ?? new List<string>()
            };
        }

        public static SubuserDto ToDto(this subuser subuser, List<string>? roles = null, List<string>? permissions = null)
        {
            return new SubuserDto
            {
                subuser_id = subuser.subuser_id,
                subuser_email = subuser.subuser_email,
                user_email = subuser.user_email,
                roles = roles ?? new List<string>(),
                permissions = permissions ?? new List<string>()
            };
        }

        public static users ToEntity(this UserCreateDto dto)
        {
            return new users
            {
                user_name = dto.user_name,
                user_email = dto.user_email,
                user_password = dto.user_password,
                phone_number = dto.phone_number,
                is_private_cloud = dto.is_private_cloud,
                private_api = dto.private_api,
                payment_details_json = dto.payment_details_json,
                license_details_json = dto.license_details_json
            };
        }

        public static subuser ToEntity(this SubuserCreateDto dto)
        {
            return new subuser
            {
                subuser_email = dto.subuser_email,
                subuser_password = dto.subuser_password,
                user_email = dto.user_email
            };
        }
    }
}