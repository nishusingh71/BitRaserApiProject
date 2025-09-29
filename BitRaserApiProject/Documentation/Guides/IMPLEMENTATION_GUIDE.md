# Implementation Guide: Dynamic Email-Based API System

## Overview

Your BitRaser API has been successfully updated with a dynamic, email-based system that eliminates hardcoded IDs and provides a more user-friendly experience. This guide explains how to implement and use the new system.

## What's New

### ✅ New Services Added
1. **IUserDataService & UserDataService** - Core email-based operations
2. **MigrationUtilityService** - Helps transition from old ID-based system
3. **DynamicUserController** - New email-based API endpoints
4. **SystemMigrationController** - System management and migration tools

### ✅ Key Improvements
- **No More Hardcoded IDs**: All operations use emails as primary identifiers
- **Automatic Data Scoping**: Users see only their own data and subusers' data
- **Dynamic Role Management**: Assign roles by name, not by ID
- **Better Security**: Built-in permission checks and access control
- **Simplified API**: More intuitive endpoints that don't require ID lookups

## Quick Start Guide

### 1. System Validation (SuperAdmin Only)

First, validate that your system is properly configured:

```http
GET /api/SystemMigration/validate-system
Authorization: Bearer {admin-token}
```

If issues are found, run the migration:

```http
POST /api/SystemMigration/migrate-user-roles
Authorization: Bearer {admin-token}
```

### 2. Login with Enhanced Information

The login now returns comprehensive user information:

```http
POST /api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response includes:**
```json
{
  "token": "jwt-token...",
  "userType": "user",
  "email": "user@example.com", 
  "roles": ["Manager", "Support"],
  "permissions": ["UserManagement", "ReportAccess"],
  "expiresAt": "2024-01-01T12:00:00Z"
}
```

### 3. Use New Email-Based Endpoints

#### Get Your Profile and Permissions
```http
GET /api/DynamicUser/profile
Authorization: Bearer {jwt-token}
```

#### Manage Your Subusers
```http
# Get all your subusers
GET /api/DynamicUser/subusers

# Create a new subuser
POST /api/DynamicUser/subusers
{
  "subuserEmail": "employee@company.com",
  "password": "TempPassword123",
  "defaultRole": "Support"
}

# Update subuser roles  
PUT /api/DynamicUser/subusers/employee@company.com/roles
{
  "rolesToAdd": ["Manager"],
  "rolesToRemove": ["Support"]
}
```

#### Access Your Data
```http
# Your machines (includes subuser machines)
GET /api/DynamicUser/machines

# Your reports
GET /api/DynamicUser/reports

# Your sessions  
GET /api/DynamicUser/sessions

# Your logs
GET /api/DynamicUser/logs
```

## Migration Strategy

### Phase 1: Immediate Benefits (No Code Changes Required)
1. ✅ New services are automatically available
2. ✅ Existing endpoints continue to work unchanged
3. ✅ Enhanced security through better permission checking
4. ✅ Automatic role assignment for new users

### Phase 2: Gradual Adoption (Recommended)
1. **Start using new endpoints** for new features
2. **Test email-based operations** with existing data
3. **Update client applications** gradually
4. **Train users** on new email-based workflows

### Phase 3: Full Migration (Optional)
1. **Migrate all client code** to use email-based endpoints
2. **Deprecate old ID-based endpoints**
3. **Cleanup legacy data** using migration tools

## Common Usage Patterns

### Pattern 1: User Self-Service
```csharp
// Users manage their own data
var currentUser = await _userDataService.GetUserByEmailAsync(userEmail);
var userMachines = await _userDataService.GetMachinesByUserEmailAsync(userEmail);
var userPermissions = await _userDataService.GetUserPermissionsAsync(userEmail);
```

### Pattern 2: Manager Managing Subusers
```csharp
// Manager creates and manages subusers
var subusers = await _userDataService.GetSubusersByParentEmailAsync(managerEmail);
await _userDataService.AssignRoleByEmailAsync(subuserEmail, "Support", managerEmail, true);
```

### Pattern 3: Admin Role Management
```csharp
// Admin assigns roles to users
await _userDataService.AssignRoleByEmailAsync(userEmail, "Manager", adminEmail, false);
var availableRoles = await _userDataService.GetAvailableRolesForUserAsync(adminEmail);
```

### Pattern 4: Permission Checking
```csharp
// Check permissions before operations
if (await _userDataService.HasPermissionAsync(userEmail, "UserManagement"))
{
    // Allow user management operations
}

if (await _userDataService.CanUserAccessDataAsync(requesterEmail, targetEmail))
{
    // Allow access to target user's data
}
```

## API Endpoint Comparison

### Old vs New Approach

#### User Management
```http
# OLD: Required knowing user ID
GET /api/Users/123
PUT /api/Users/123  
DELETE /api/Users/123

# NEW: Use email directly
GET /api/DynamicUser/profile
PUT /api/DynamicUser/profile
# Deletion handled through proper user management workflow
```

#### Role Assignment
```http
# OLD: Required knowing user ID and role ID
POST /api/RoleBasedAuth/assign-role
{
  "userId": 123,
  "roleId": 2
}

# NEW: Use email and role name
PUT /api/DynamicUser/subusers/user@example.com/roles
{
  "rolesToAdd": ["Manager"],
  "rolesToRemove": ["Support"]
}
```

#### Data Access
```http
# OLD: Had to filter manually or know relationships
GET /api/Machines  # Returns all machines
GET /api/Sessions  # Returns all sessions

# NEW: Automatically scoped to user
GET /api/DynamicUser/machines  # Only user's machines
GET /api/DynamicUser/sessions  # Only user's sessions
```

## Security Improvements

### 1. Automatic Data Scoping
- Users can only see their own data and their subusers' data
- No risk of accidentally accessing other users' information
- Built-in protection against data leakage

### 2. Hierarchical Role Management
- Users can only assign roles lower than their own privilege level
- SuperAdmins can manage everything
- Managers can only manage Support and User roles

### 3. Permission-Based Access Control
- Every operation checks specific permissions
- Fine-grained control over what users can do
- Easy to audit and modify permissions

### 4. Relationship Validation
- System validates user-subuser relationships
- Prevents unauthorized cross-user access
- Maintains data integrity

## Troubleshooting

### Common Issues and Solutions

#### Issue: User doesn't have any roles
**Solution:**
```http
POST /api/SystemMigration/migrate-user-roles
```

#### Issue: Permission denied errors
**Check:**
1. User has the required permission
2. User has access to the target resource
3. Role hierarchy is correctly configured

#### Issue: Can't find user by email
**Verify:**
1. Email format is correct
2. User exists in the database
3. No typos in email address

#### Issue: Subuser operations fail
**Check:**
1. Subuser belongs to the current user
2. Current user has UserManagement permission
3. Subuser email is correctly formatted

### System Health Check
```http
GET /api/SystemMigration/system-stats
```

This endpoint provides comprehensive information about:
- Current user's permissions and roles
- System configuration status
- Available roles and their hierarchy
- Recommendations for improvements

## Best Practices

### 1. Always Use Email-Based Services
```csharp
// ✅ Good
var user = await _userDataService.GetUserByEmailAsync(email);

// ❌ Avoid
var user = await _context.Users.FindAsync(userId);
```

### 2. Check Permissions Before Operations
```csharp
// ✅ Good
if (await _userDataService.HasPermissionAsync(userEmail, "UserManagement"))
{
    // Perform operation
}

// ❌ Avoid
// Performing operations without permission checks
```

### 3. Use Role Names Instead of IDs
```csharp
// ✅ Good
await _userDataService.AssignRoleByEmailAsync(email, "Manager", assignerEmail);

// ❌ Avoid
await _roleService.AssignRoleToUserAsync(userId, roleId, assignerEmail);
```

### 4. Let the System Handle Access Control
```csharp
// ✅ Good - automatic filtering
var userMachines = await _userDataService.GetMachinesByUserEmailAsync(userEmail);

// ❌ Avoid - manual filtering
var allMachines = await _context.Machines.ToListAsync();
var filtered = allMachines.Where(m => m.user_email == userEmail);
```

## Performance Considerations

### Caching Recommendations
- Cache user permissions and roles (they don't change frequently)
- Cache available roles for assignment
- Use appropriate cache invalidation when roles change

### Database Optimization
- Ensure proper indexes on email fields
- Consider connection pooling for high-traffic scenarios
- Monitor query performance for complex permission checks

## Support and Maintenance

### Monitoring
- Use the system health endpoints to monitor system status
- Set up alerts for permission-related errors
- Monitor user role assignment patterns

### Regular Maintenance
- Run orphaned role cleanup periodically
- Validate machine-user associations
- Review and update role hierarchies as needed

## Next Steps

1. **Test the new system** with a few users
2. **Update client applications** to use new endpoints gradually
3. **Train your team** on the new email-based workflows
4. **Monitor system health** using the provided tools
5. **Provide feedback** on any issues or improvement suggestions

The new system provides a solid foundation for scalable, secure, and user-friendly API operations. All operations are now email-based, eliminating the complexity of managing IDs while providing better security and user experience.