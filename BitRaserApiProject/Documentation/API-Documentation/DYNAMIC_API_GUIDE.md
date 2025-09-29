# Dynamic Email-Based API System

## Overview

This document describes the new dynamic, email-based API system that eliminates the need for hardcoded IDs in your BitRaser application. All operations are now performed using email addresses as the primary identifier, making the system more user-friendly and eliminating the need to remember or look up numeric IDs.

## Key Benefits

1. **No Hardcoded IDs**: All operations use email addresses instead of database IDs
2. **Dynamic Role Assignment**: Roles and permissions are assigned by name, not by ID
3. **User-Scoped Data**: Users automatically see only their own data and their subusers' data
4. **Simplified API**: Cleaner, more intuitive endpoints
5. **Better Security**: Built-in access control based on user relationships

## New Services

### IUserDataService

The `IUserDataService` is the core service that handles all email-based operations:

```csharp
// Get user information
var user = await _userDataService.GetUserByEmailAsync("user@example.com");

// Get user's machines
var machines = await _userDataService.GetMachinesByUserEmailAsync("user@example.com");

// Check permissions
var hasPermission = await _userDataService.HasPermissionAsync("user@example.com", "UserManagement");

// Assign roles by name
await _userDataService.AssignRoleByEmailAsync("user@example.com", "Manager", "admin@example.com");
```

## New Dynamic Controller: `/api/DynamicUser`

### User Profile Management

#### Get My Profile
```http
GET /api/DynamicUser/profile
Authorization: Bearer {jwt-token}
```

**Response:**
```json
{
  "user_email": "user@example.com",
  "user_name": "John Doe",
  "phone_number": "+1234567890",
  "roles": ["Manager", "Support"],
  "permissions": ["UserManagement", "ReportAccess", "ViewOnly"],
  "subusers": ["subuser1@example.com", "subuser2@example.com"]
}
```

#### Update My Profile
```http
PUT /api/DynamicUser/profile
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "userName": "John Smith",
  "phoneNumber": "+0987654321"
}
```

### Subuser Management

#### Get My Subusers
```http
GET /api/DynamicUser/subusers
Authorization: Bearer {jwt-token}
```

**Response:**
```json
[
  {
    "subuser_email": "subuser1@example.com",
    "roles": ["Support"],
    "permissions": ["ViewOnly", "ReportAccess"]
  }
]
```

#### Create Subuser
```http
POST /api/DynamicUser/subusers
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "subuserEmail": "newsubuser@example.com",
  "password": "SecurePassword123",
  "defaultRole": "Support"
}
```

#### Manage Subuser Roles
```http
PUT /api/DynamicUser/subusers/{subuserEmail}/roles
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "rolesToAdd": ["Manager"],
  "rolesToRemove": ["Support"]
}
```

#### Delete Subuser
```http
DELETE /api/DynamicUser/subusers/{subuserEmail}
Authorization: Bearer {jwt-token}
```

### Data Access (User-Scoped)

All data endpoints automatically filter results to show only data belonging to the authenticated user:

#### Get My Machines
```http
GET /api/DynamicUser/machines
Authorization: Bearer {jwt-token}
```

#### Get My Reports
```http
GET /api/DynamicUser/reports
Authorization: Bearer {jwt-token}
```

#### Get My Sessions
```http
GET /api/DynamicUser/sessions
Authorization: Bearer {jwt-token}
```

#### Get My Logs
```http
GET /api/DynamicUser/logs
Authorization: Bearer {jwt-token}
```

### Access Control Information

#### Get My Permissions
```http
GET /api/DynamicUser/permissions
Authorization: Bearer {jwt-token}
```

**Response:**
```json
{
  "email": "user@example.com",
  "userType": "user",
  "roles": ["Manager", "Support"],
  "permissions": ["UserManagement", "ReportAccess", "ViewOnly"]
}
```

#### Get Available Roles (for assignment)
```http
GET /api/DynamicUser/available-roles
Authorization: Bearer {jwt-token}
```

**Response:**
```json
[
  {
    "roleId": 4,
    "roleName": "Support",
    "description": "Support access",
    "hierarchyLevel": 4
  },
  {
    "roleId": 5,
    "roleName": "User",
    "description": "Basic user access", 
    "hierarchyLevel": 5
  }
]
```

#### Check Access for Operation
```http
POST /api/DynamicUser/check-access
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "operation": "UserManagement",
  "resourceOwner": "target@example.com"
}
```

**Response:**
```json
{
  "userEmail": "user@example.com",
  "operation": "UserManagement",
  "resourceOwner": "target@example.com",
  "hasAccess": true,
  "userType": "user"
}
```

## Enhanced Authentication System

### Login with Role Information

The enhanced login endpoint now returns comprehensive role and permission information:

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
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userType": "user",
  "email": "user@example.com",
  "roles": ["Manager", "Support"],
  "permissions": ["UserManagement", "ReportAccess", "ViewOnly"],
  "expiresAt": "2024-01-01T12:00:00Z"
}
```

## Migration from Old System

### Before (ID-based)
```http
# Old way - required knowing user ID
GET /api/Users/123
PUT /api/Users/123

# Old way - required knowing role ID
POST /api/RoleBasedAuth/assign-role
{
  "userId": 123,
  "roleId": 2
}
```

### After (Email-based)
```http
# New way - use email directly
GET /api/DynamicUser/profile
PUT /api/DynamicUser/profile

# New way - use role names
PUT /api/DynamicUser/subusers/subuser@example.com/roles
{
  "rolesToAdd": ["Manager"],
  "rolesToRemove": ["Support"]
}
```

## Available Roles and Permissions

### Default Roles (by hierarchy level)
1. **SuperAdmin** (Level 1) - Complete system access
2. **Admin** (Level 2) - Administrative access
3. **Manager** (Level 3) - Management access  
4. **Support** (Level 4) - Support access
5. **User** (Level 5) - Basic user access

### Default Permissions
- **FullAccess** - Complete system access
- **UserManagement** - Manage users and subusers
- **ReportAccess** - Access and manage reports
- **MachineManagement** - Manage machines
- **ViewOnly** - Read-only access
- **LicenseManagement** - Manage licenses
- **SystemLogs** - Access system logs

## Best Practices

### 1. Always Use Email-Based Endpoints
```csharp
// ✅ Good - email-based
var user = await _userDataService.GetUserByEmailAsync("user@example.com");

// ❌ Avoid - ID-based (legacy)
var user = await _context.Users.FindAsync(123);
```

### 2. Use Role Names Instead of IDs
```csharp
// ✅ Good - role name
await _userDataService.AssignRoleByEmailAsync("user@example.com", "Manager", "admin@example.com");

// ❌ Avoid - role ID (legacy)
await _roleService.AssignRoleToUserAsync(123, 2, "admin@example.com");
```

### 3. Let the System Handle Access Control
```csharp
// ✅ Good - automatic access control
var machines = await _userDataService.GetMachinesByUserEmailAsync(currentUserEmail);

// ❌ Avoid - manual filtering
var allMachines = await _context.Machines.ToListAsync();
var userMachines = allMachines.Where(m => m.user_email == currentUserEmail);
```

### 4. Use Permission Names for Checks
```csharp
// ✅ Good - permission name
if (await _userDataService.HasPermissionAsync(userEmail, "UserManagement"))
{
    // Allow operation
}

// ❌ Avoid - hardcoded checks
if (userRoleId == 1 || userRoleId == 2)
{
    // Allow operation
}
```

## Error Handling

The new system provides better error messages:

```json
{
  "message": "You don't have permission to manage roles",
  "details": "Current user: user@example.com, Required permission: UserManagement"
}
```

## Security Features

1. **Automatic Data Scoping**: Users can only access their own data and their subusers' data
2. **Hierarchical Role Management**: Users can only assign roles lower than their own
3. **Permission-Based Access Control**: Each operation checks specific permissions
4. **Audit Trail**: All role assignments track who made the change and when

## Example Usage Scenarios

### Scenario 1: Manager Creates Subuser
```http
POST /api/DynamicUser/subusers
Authorization: Bearer {manager-token}

{
  "subuserEmail": "employee@company.com",
  "password": "TempPassword123",
  "defaultRole": "Support"
}
```

### Scenario 2: Admin Updates User Roles
```http
PUT /api/DynamicUser/subusers/employee@company.com/roles
Authorization: Bearer {admin-token}

{
  "rolesToAdd": ["Manager"],
  "rolesToRemove": ["Support"]
}
```

### Scenario 3: User Checks Their Permissions
```http
GET /api/DynamicUser/permissions
Authorization: Bearer {user-token}
```

This system eliminates the complexity of managing IDs and provides a more intuitive, secure way to manage users and permissions in your BitRaser application.