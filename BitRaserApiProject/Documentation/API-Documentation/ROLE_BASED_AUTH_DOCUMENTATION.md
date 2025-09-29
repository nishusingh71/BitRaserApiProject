# Role-Based Authentication System Documentation

## Overview

This system implements a comprehensive role-based authentication and authorization system with hierarchical access control. It supports both main users and subusers with different permission levels.

## Key Features

1. **Hierarchical Role System**: SuperAdmin > Admin > Manager > Support > User
2. **Permission-Based Access Control**: Each role has specific permissions
3. **Route-Level Authorization**: Each API endpoint requires specific permissions
4. **User and Subuser Support**: Main users can create subusers with assigned roles
5. **JWT Authentication**: Secure token-based authentication
6. **Unified Login**: Both users and subusers use the same login endpoint

## Default Roles and Permissions

### Roles (Hierarchy Level)
1. **SuperAdmin (Level 1)**: Complete system access
2. **Admin (Level 2)**: Administrative access (cannot manage SuperAdmins)
3. **Manager (Level 3)**: Management access (reports, machines)
4. **Support (Level 4)**: Support-specific access (reports, logs)
5. **User (Level 5)**: Basic read-only access

### Permissions
- **FullAccess**: Complete system access (SuperAdmin only)
- **UserManagement**: Manage users and subusers
- **ReportAccess**: Access and manage reports
- **MachineManagement**: Manage machines
- **LicenseManagement**: Manage licenses
- **SystemLogs**: Access system logs
- **ViewOnly**: Read-only access

## API Endpoints

### Authentication

#### 1. Login (Users and Subusers)
```http
POST /api/RoleBasedAuth/login
Content-Type: application/json

{
    "email": "user@example.com",
    "password": "password123"
}
```

**Response:**
```json
{
    "token": "jwt_token_here",
    "userType": "user|subuser",
    "email": "user@example.com",
    "roles": ["SuperAdmin"],
    "permissions": ["FullAccess", "UserManagement", ...],
    "expiresAt": "2024-01-01T08:00:00Z"
}
```

#### 2. Create Subuser
```http
POST /api/RoleBasedAuth/create-subuser
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
    "subuserEmail": "subuser@example.com",
    "subuserPassword": "password123",
    "roleIds": [3, 4]  // Manager and Support roles
}
```

#### 3. Assign Role
```http
POST /api/RoleBasedAuth/assign-role
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
    "userId": 1,          // For main users
    "subuserId": 2,       // For subusers (optional)
    "roleId": 3
}
```

#### 4. Get My Permissions
```http
GET /api/RoleBasedAuth/my-permissions
Authorization: Bearer {jwt_token}
```

#### 5. Get Available Roles
```http
GET /api/RoleBasedAuth/roles
Authorization: Bearer {jwt_token}
```

#### 6. Get My Subusers
```http
GET /api/RoleBasedAuth/my-subusers
Authorization: Bearer {jwt_token}
```

## How It Works

### 1. User Registration
- First user created automatically gets SuperAdmin role
- Subsequent users need roles assigned by existing users with UserManagement permission

### 2. Subuser Creation
- Main users with UserManagement permission can create subusers
- Subusers can only be assigned roles with lower privilege than the creator

### 3. Role Hierarchy
- Users can only manage other users with lower hierarchy levels
- SuperAdmin (1) can manage everyone
- Admin (2) can manage Manager (3), Support (4), User (5)
- And so on...

### 4. Permission Checks
- Each controller/action can use attributes for permission checking:
  - `[RequirePermission("PermissionName")]`
  - `[RequireRole("RoleName")]`
  - `[RequireHierarchyLevel(2)]`

### 5. Route-Based Authorization
- Middleware automatically checks if user has permission for accessed route
- Routes are pre-configured with required permissions
- Fallback: Unknown routes require SuperAdmin access

## Usage Examples

### 1. Using Permission Attributes
```csharp
[RequirePermission("UserManagement")]
[HttpGet]
public async Task<IActionResult> GetUsers()
{
    // Only users with UserManagement permission can access
}

[RequireRole("Admin", "SuperAdmin")]
[HttpPost]
public async Task<IActionResult> CreateUser()
{
    // Only Admins and SuperAdmins can access
}

[RequireHierarchyLevel(2)]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(int id)
{
    // Only users with hierarchy level 2 or lower (Admin and above)
}
```

### 2. Manual Permission Checking
```csharp
public class MyController : ControllerBase
{
    private readonly IRoleBasedAuthService _roleService;
    
    public async Task<IActionResult> SomeAction()
    {
        var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var hasPermission = await _roleService.HasPermissionAsync(email, "ReportAccess");
        
        if (!hasPermission)
            return Forbid();
            
        // Continue with action
    }
}
```

## Database Schema

### New Tables Added:
- **Roles**: Stores role definitions with hierarchy levels
- **Permissions**: Stores permission definitions
- **Routes**: Stores API route definitions
- **RolePermissions**: Many-to-many relationship between roles and permissions
- **UserRoles**: Many-to-many relationship between users and roles
- **SubuserRoles**: Many-to-many relationship between subusers and roles
- **PermissionRoutes**: Many-to-many relationship between permissions and routes

## Security Features

1. **Hierarchical Access Control**: Users can only manage lower-privilege users
2. **Permission-Based Authorization**: Granular control over system access
3. **Route-Level Security**: Every endpoint is protected
4. **JWT Security**: Secure token-based authentication
5. **Password Hashing**: All passwords are securely hashed using BCrypt
6. **Audit Trail**: Role assignments track who assigned them and when

## Migration

To use this system in your existing application:

1. **Database Update**: Run the migration to create new tables
2. **Update Controllers**: Add permission attributes to existing controllers
3. **Update Frontend**: Use new login endpoint and handle role-based UI
4. **Test Access**: Verify that routes are properly protected

## Backward Compatibility

- Old `/api/Auth/login` endpoint still works for basic authentication
- Existing user accounts remain unchanged
- New role system is additive - doesn't break existing functionality

## Best Practices

1. **Principle of Least Privilege**: Assign minimum required permissions
2. **Regular Audits**: Review user roles and permissions periodically
3. **Role Documentation**: Document what each role should be able to do
4. **Permission Naming**: Use clear, descriptive permission names
5. **Testing**: Test authorization thoroughly, especially edge cases

## Troubleshooting

### Common Issues:

1. **403 Forbidden**: User lacks required permission
2. **401 Unauthorized**: Invalid or expired JWT token
3. **Role Assignment Failed**: Trying to assign higher privilege role
4. **Route Access Denied**: Route requires permission user doesn't have

### Debug Steps:

1. Check user's current roles: `GET /api/RoleBasedAuth/my-permissions`
2. Verify JWT token is valid and not expired
3. Check role hierarchy levels
4. Review route-permission mappings
5. Check application logs for detailed error messages