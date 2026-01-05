# Dashboard & Subuser Management API - Debug & Fix Guide

## 🔴 Common Errors Analysis

### Error Types Identified:
1. **401 Unauthorized** - Authentication failures
2. **403 Forbidden** - Permission/Role access issues
3. **500 Internal Server Error** - Backend exceptions

---

## 🐛 Problem 1: Dashboard APIs Return 403/401

### Root Causes:

#### 1. **Hard-coded Role Authorization**
```csharp
// ❌ PROBLEM in DashboardController.cs
[Authorize(Roles = "Admin")]  // Fails if user doesn't have exact "Admin" role
public class AdminDashboardController : ControllerBase
```

**Issue**: 
- Uses hardcoded `Roles = "Admin"` instead of dynamic role-based auth
- Fails when user has role but it's not named exactly "Admin"
- Doesn't check against database roles

#### 2. **Missing User Identity Name**
```csharp
// ❌ PROBLEM
var userEmail = User.Identity?.Name;  // Often returns NULL!
```

**Issue**:
- JWT token uses `ClaimTypes.NameIdentifier` or `JwtRegisteredClaimNames.Sub`
- `User.Identity.Name` is often null because claim type mismatch
- Should use `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`

#### 3. **Incorrect JWT Token Claims**
```csharp
// ❌ PROBLEM in DashboardAuthController
new Claim(JwtRegisteredClaimNames.Sub, email),
new Claim(JwtRegisteredClaimNames.Email, email),
// Missing NameIdentifier claim!
```

**Issue**:
- Missing `ClaimTypes.NameIdentifier` in JWT
- Controllers rely on this claim but it's not set

---

## 🐛 Problem 2: Subuser Management Returns 403/500

### Root Causes:

#### 1. **Permission Check Failures**
```csharp
// ❌ PROBLEM
if (!await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser))
{
    // Users without this specific permission cannot access
}
```

**Issue**:
- Too restrictive permission checks
- Regular users should be able to manage their own subusers without special permissions
- Permission names might not exist in database

#### 2. **Null Reference Exceptions**
```csharp
// ❌ PROBLEM
var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
// If null, causes 500 error later when used with !
```

**Issue**:
- No null check before using `currentUserEmail!`
- Can cause NullReferenceException

#### 3. **Missing Roles in Database**
```csharp
// ❌ PROBLEM
await AssignRoleToSubuserAsync(request.SubuserEmail, "SubUser", currentUserEmail!);
```

**Issue**:
- Assumes "SubUser" role exists in database
- If not seeded, causes 500 error

---

## ✅ SOLUTIONS

### Fix 1: Update Dashboard Controllers - Remove Hard-coded Roles

Create a new fixed version:

```csharp
/// <summary>
/// Admin Dashboard Controller - Fixed Version
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // ✅ Remove hard-coded Roles requirement
public class AdminDashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRoleBasedAuthService _authService;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(
        ApplicationDbContext context,
        IRoleBasedAuthService authService,
        ILogger<AdminDashboardController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    // GET: api/AdminDashboard/overview
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetDashboardOverview()
    {
        try
        {
            // ✅ FIX: Get email from correct claim
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var isSubuser = await _context.subuser.AnyAsync(s => s.subuser_email == userEmail);

            // ✅ FIX: Check permissions instead of hard-coded roles
            if (!await _authService.HasPermissionAsync(userEmail, "VIEW_ORGANIZATION_HIERARCHY", isSubuser))
            {
                return StatusCode(403, new { message = "Insufficient permissions to view dashboard" });
            }

            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.updated_at >= DateTime.UtcNow.AddDays(-30));
            var totalMachines = await _context.Machines.CountAsync();
            var activeMachines = await _context.Machines.CountAsync(m => m.license_activated);
            
            var recentActivities = await _context.logs
                .OrderByDescending(l => l.created_at)
                .Take(10)
                .Select(l => new ActivityDto
                {
                    Id = l.log_id.ToString(),
                    Type = l.log_level ?? "Info",
                    Description = l.log_message ?? "No description",
                    User = l.user_email ?? "System",
                    Timestamp = l.created_at,
                    Status = l.log_level ?? "Info"
                })
                .ToListAsync();

            return Ok(new DashboardOverviewDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalLicenses = totalMachines,
                UsedLicenses = activeMachines,
                TotalMachines = totalMachines,
                ActiveMachines = activeMachines,
                RecentActivities = recentActivities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard overview");
            return StatusCode(500, new { message = "Error retrieving dashboard data", error = ex.Message });
        }
    }
}
```

### Fix 2: Update JWT Token Generation

```csharp
private string GenerateJwtToken(string email, string userType, IEnumerable<string> roles, IEnumerable<string> permissions)
{
    var jwtSettings = _configuration.GetSection("Jwt");
    var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT key not configured");
    var issuer = jwtSettings["Issuer"];
    var audience = jwtSettings["Audience"];

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        // ✅ FIX: Add NameIdentifier for User.FindFirst to work
        new Claim(ClaimTypes.NameIdentifier, email),
        new Claim(ClaimTypes.Name, email), // ✅ For User.Identity.Name
        new Claim(JwtRegisteredClaimNames.Sub, email),
        new Claim(JwtRegisteredClaimNames.Email, email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("user_type", userType),
        new Claim("email", email) // ✅ Additional email claim
    };

    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    foreach (var permission in permissions)
    {
        claims.Add(new Claim("permission", permission));
    }

    var token = new JwtSecurityToken(
        issuer,
        audience,
        claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Fix 3: Relax Subuser Permission Checks

```csharp
/// <summary>
/// Get all subusers - FIXED VERSION
/// </summary>
[HttpGet]
public async Task<ActionResult<IEnumerable<object>>> GetSubusers([FromQuery] SubuserFilterRequest? filter)
{
    // ✅ FIX: Proper null checking
    var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(currentUserEmail))
    {
        return Unauthorized(new { message = "User not authenticated" });
    }

    var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);
    
    IQueryable<subuser> query = _context.subuser;

    // ✅ FIX: Simplified permission logic
    // Check if user has admin-level permissions
    bool hasAdminPermission = await _authService.HasPermissionAsync(
        currentUserEmail, "READ_ALL_SUBUSERS", isCurrentUserSubuser);

    if (!hasAdminPermission)
    {
        // ✅ Regular users can see their own subusers
        if (isCurrentUserSubuser)
        {
            // Subusers cannot see any subusers by default
            return Ok(new List<object>());
        }
        
        // Filter to only show current user's subusers
        query = query.Where(s => s.user_email == currentUserEmail);
    }

    // Apply additional filters
    if (filter != null)
    {
        if (!string.IsNullOrEmpty(filter.ParentUserEmail))
            query = query.Where(s => s.user_email.Contains(filter.ParentUserEmail));

        if (!string.IsNullOrEmpty(filter.SubuserEmail))
            query = query.Where(s => s.subuser_email.Contains(filter.SubuserEmail));
    }

    try
    {
        var subusers = await query
            .Include(s => s.SubuserRoles)
            .ThenInclude(sr => sr.Role)
            .OrderByDescending(s => s.subuser_id)
            .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
            .Take(filter?.PageSize ?? 100)
            .Select(s => new {
                s.subuser_email,
                s.user_email,
                s.subuser_id,
                roles = s.SubuserRoles.Select(sr => sr.Role.RoleName).ToList(),
                hasPassword = !string.IsNullOrEmpty(s.subuser_password)
            })
            .ToListAsync();

        return Ok(subusers);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving subusers for {Email}", currentUserEmail);
        return StatusCode(500, new { 
            message = "Error retrieving subusers", 
            error = ex.Message 
        });
    }
}
```

### Fix 4: Safe Role Assignment

```csharp
private async Task<bool> AssignRoleToSubuserAsync(string subuserEmail, string roleName, string assignedByEmail)
{
    try
    {
        var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == subuserEmail);
        if (subuser == null)
        {
            _logger.LogWarning("Subuser {Email} not found for role assignment", subuserEmail);
            return false;
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleName} not found, creating default User role instead", roleName);
            
            // ✅ FIX: Fallback to default role if specified role doesn't exist
            role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
            if (role == null)
            {
                _logger.LogError("No default role found in system");
                return false;
            }
        }

        // Check if role already assigned
        var existingRole = await _context.SubuserRoles
            .FirstOrDefaultAsync(sr => sr.SubuserId == subuser.subuser_id && sr.RoleId == role.RoleId);

        if (existingRole == null)
        {
            var subuserRole = new SubuserRole
            {
                SubuserId = subuser.subuser_id,
                RoleId = role.RoleId,
                AssignedAt = DateTime.UtcNow,
                AssignedByEmail = assignedByEmail
            };

            _context.SubuserRoles.Add(subuserRole);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Role {RoleName} assigned to subuser {Email}", role.RoleName, subuserEmail);
            return true;
        }

        return true; // Already assigned
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error assigning role {RoleName} to subuser {Email}", roleName, subuserEmail);
        return false;
    }
}
```

---

## 🔧 Quick Fix Checklist

### Step 1: Update DashboardController.cs
- [ ] Remove `[Authorize(Roles = "Admin")]`
- [ ] Replace with `[Authorize]` + permission checks
- [ ] Fix `User.Identity?.Name` to `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- [ ] Add null checks for email

### Step 2: Fix JWT Token Claims
- [ ] Add `ClaimTypes.NameIdentifier` claim
- [ ] Add `ClaimTypes.Name` claim
- [ ] Keep all existing claims

### Step 3: Update Subuser Controller
- [ ] Add null checks for `currentUserEmail`
- [ ] Relax permission requirements for own subusers
- [ ] Add try-catch blocks with proper error messages
- [ ] Make role assignment safe with fallbacks

### Step 4: Verify Database
```sql
-- Check if roles exist
SELECT * FROM Roles;

-- Check if permissions exist  
SELECT * FROM Permissions WHERE PermissionName LIKE '%SUBUSER%';

-- Check role-permission mappings
SELECT r.RoleName, p.PermissionName 
FROM RolePermissions rp
JOIN Roles r ON rp.RoleId = r.RoleId
JOIN Permissions p ON rp.PermissionId = p.PermissionId;
```

### Step 5: Test APIs
```bash
# 1. Login
POST /api/DashboardAuth/login
{
  "Email": "test@example.com",
  "Password": "Test@123"
}

# 2. Get Dashboard (with token)
GET /api/AdminDashboard/overview
Header: Authorization: Bearer <token>

# 3. Get Subusers
GET /api/EnhancedSubuser
Header: Authorization: Bearer <token>

# 4. Create Subuser
POST /api/EnhancedSubuser
{
  "SubuserEmail": "subuser@example.com",
  "SubuserPassword": "Pass@123"
}
```

---

## 📊 Error Debugging Guide

### 401 Unauthorized
**Symptoms**: "Unauthorized" response
**Causes**:
1. No Authorization header
2. Invalid/expired token
3. Missing ClaimTypes.NameIdentifier in token

**Debug Steps**:
```csharp
// Add logging in controller
_logger.LogInformation("Claims: {Claims}", 
    string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}")));
```

### 403 Forbidden
**Symptoms**: "Insufficient permissions" or "Forbidden"
**Causes**:
1. User doesn't have required role
2. Permission check fails
3. Hard-coded role mismatch

**Debug Steps**:
```csharp
var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var isSubuser = await _context.subuser.AnyAsync(s => s.subuser_email == userEmail);
var hasPermission = await _authService.HasPermissionAsync(userEmail!, "PERMISSION_NAME", isSubuser);
_logger.LogWarning("User {Email}, IsSubuser: {IsSubuser}, HasPermission: {HasPermission}", 
    userEmail, isSubuser, hasPermission);
```

### 500 Internal Server Error
**Symptoms**: Generic "An error occurred" message
**Causes**:
1. NullReferenceException
2. Database errors
3. Missing data/roles

**Debug Steps**:
```csharp
try
{
    // Your code
}
catch (Exception ex)
{
    _logger.LogError(ex, "Detailed error in {Method}", nameof(YourMethod));
    return StatusCode(500, new { 
        message = "Error occurred", 
        error = ex.Message,
        stackTrace = ex.StackTrace // Only in development!
    });
}
```

---

## 🎯 Testing Commands

```bash
# Test with curl
curl -X POST http://localhost:5000/api/DashboardAuth/login \
  -H "Content-Type: application/json" \
  -d '{"Email":"test@example.com","Password":"Test@123"}'

# Save token
TOKEN="eyJhbGc..."

# Test dashboard
curl -X GET http://localhost:5000/api/AdminDashboard/overview \
  -H "Authorization: Bearer $TOKEN"

# Test subusers
curl -X GET http://localhost:5000/api/EnhancedSubuser \
  -H "Authorization: Bearer $TOKEN"
```

---

## ✅ Expected Behavior After Fixes

1. ✅ Dashboard APIs work without hard-coded Admin role
2. ✅ Users can view their own subusers without special permissions
3. ✅ Admins can view all subusers with proper permissions
4. ✅ JWT tokens include all required claims
5. ✅ Proper error messages instead of generic 500 errors
6. ✅ Safe fallbacks when roles don't exist

---

**Last Updated**: 2025-01-26  
**Status**: Ready for Implementation
