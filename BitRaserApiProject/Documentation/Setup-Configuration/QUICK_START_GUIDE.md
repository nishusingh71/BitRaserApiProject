# Role-Based Authentication System - Quick Start Guide

## Step 1: Create First User (SuperAdmin)

```bash
curl -X POST "http://localhost:4000/api/Users" \
-H "Content-Type: application/json" \
-d '{
    "user_name": "System Administrator",
    "user_email": "admin@company.com",
    "user_password": "SecurePassword123!",
    "phone_number": "+1234567890"
}'
```

## Step 2: Login as SuperAdmin

```bash
curl -X POST "http://localhost:4000/api/RoleBasedAuth/login" \
-H "Content-Type: application/json" \
-d '{
    "email": "admin@company.com",
    "password": "SecurePassword123!"
}'
```

Response will include JWT token:
```json
{
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userType": "user",
    "email": "admin@company.com",
    "roles": ["SuperAdmin"],
    "permissions": ["FullAccess", "UserManagement", "ReportAccess", ...],
    "expiresAt": "2024-01-01T08:00:00Z"
}
```

## Step 3: Create Another User

```bash
curl -X POST "http://localhost:4000/api/Users" \
-H "Content-Type: application/json" \
-d '{
    "user_name": "Manager User",
    "user_email": "manager@company.com",
    "user_password": "ManagerPass123!",
    "phone_number": "+1234567891"
}'
```

## Step 4: Assign Manager Role to New User

```bash
curl -X POST "http://localhost:4000/api/RoleBasedAuth/assign-role" \
-H "Authorization: Bearer YOUR_JWT_TOKEN" \
-H "Content-Type: application/json" \
-d '{
    "userId": 2,
    "roleId": 3
}'
```

## Step 5: Create Subuser

```bash
curl -X POST "http://localhost:4000/api/RoleBasedAuth/create-subuser" \
-H "Authorization: Bearer YOUR_JWT_TOKEN" \
-H "Content-Type: application/json" \
-d '{
    "subuserEmail": "support@company.com",
    "subuserPassword": "SupportPass123!",
    "roleIds": [4]
}'
```

## Step 6: Test Login as Subuser

```bash
curl -X POST "http://localhost:4000/api/RoleBasedAuth/login" \
-H "Content-Type: application/json" \
-d '{
    "email": "support@company.com",
    "password": "SupportPass123!"
}'
```

## Step 7: Test Permission-Protected Endpoint

```bash
# This should work (Support has ViewOnly permission)
curl -X GET "http://localhost:4000/api/AuditReports" \
-H "Authorization: Bearer SUBUSER_JWT_TOKEN"

# This should fail (Support doesn't have UserManagement permission)
curl -X GET "http://localhost:4000/api/Users" \
-H "Authorization: Bearer SUBUSER_JWT_TOKEN"
```

## Step 8: Check Your Permissions

```bash
curl -X GET "http://localhost:4000/api/RoleBasedAuth/my-permissions" \
-H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Role and Permission Matrix

| Role | Level | Permissions |
|------|-------|-------------|
| SuperAdmin | 1 | All permissions |
| Admin | 2 | UserManagement, ReportAccess, MachineManagement, LicenseManagement, SystemLogs, ViewOnly |
| Manager | 3 | ReportAccess, MachineManagement, ViewOnly |
| Support | 4 | ReportAccess, SystemLogs, ViewOnly |
| User | 5 | ViewOnly |

## Endpoint Protection Examples

### Controllers with Role-Based Protection

```csharp
// Requires UserManagement permission
[RequirePermission("UserManagement")]
public class UsersController : ControllerBase
{
    // All actions require UserManagement permission
}

// Requires SystemLogs permission
[RequirePermission("SystemLogs")]
public class LogsController : ControllerBase
{
    // All actions require SystemLogs permission
}

// Mixed permissions
public class MachinesController : ControllerBase
{
    [RequirePermission("ViewOnly")]
    public async Task<IActionResult> GetMachines() { }
    
    [RequirePermission("MachineManagement")]
    public async Task<IActionResult> CreateMachine() { }
}
```

## Testing Different Scenarios

### 1. Test Hierarchy (Admin trying to manage SuperAdmin)
```bash
# Login as Admin
curl -X POST "http://localhost:4000/api/RoleBasedAuth/login" \
-H "Content-Type: application/json" \
-d '{"email": "admin@company.com", "password": "AdminPass123!"}'

# Try to assign SuperAdmin role (should fail)
curl -X POST "http://localhost:4000/api/RoleBasedAuth/assign-role" \
-H "Authorization: Bearer ADMIN_JWT_TOKEN" \
-H "Content-Type: application/json" \
-d '{"userId": 3, "roleId": 1}'
```

### 2. Test Subuser Management
```bash
# Login as Manager
curl -X POST "http://localhost:4000/api/RoleBasedAuth/login" \
-H "Content-Type: application/json" \
-d '{"email": "manager@company.com", "password": "ManagerPass123!"}'

# Check subusers (should show only their own subusers)
curl -X GET "http://localhost:4000/api/RoleBasedAuth/my-subusers" \
-H "Authorization: Bearer MANAGER_JWT_TOKEN"
```

## Common Use Cases

### 1. Company Admin Setup
1. Create company admin as SuperAdmin
2. Company admin creates department managers as Admin
3. Managers create team leads as Manager
4. Team leads create support staff as Support
5. Everyone creates basic users as User

### 2. Multi-tenant Setup
1. Each tenant has their own SuperAdmin
2. Tenant SuperAdmin manages their organization
3. Subusers are isolated to their parent user's scope
4. Cross-tenant access is prevented by hierarchy

### 3. Department-based Access
1. HR department: UserManagement permission
2. IT department: SystemLogs, MachineManagement permissions
3. Operations: ReportAccess, ViewOnly permissions
4. Support: ReportAccess, SystemLogs permissions

## Security Best Practices

1. **Always use HTTPS in production**
2. **Rotate JWT keys regularly**
3. **Set appropriate token expiration times**
4. **Monitor failed login attempts**
5. **Regular permission audits**
6. **Use strong password policies**
7. **Log all role changes**