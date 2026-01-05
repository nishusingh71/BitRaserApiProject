// ✅ UPDATED LOGIN METHOD WITH DYNAMIC DATABASE SWITCHING
// File: Controllers/RoleBasedAuthController.cs
// Add this method to replace the existing Login method

[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] RoleBasedLoginRequest request)
{
    try
    {
        _logger.LogInformation("🔐 Login attempt for: {Email}", request.Email);

        // ========== STEP 1: CHECK USER IN MAIN DB ==========
        var mainUser = await _context.Users
       .AsNoTracking()
    .FirstOrDefaultAsync(u => u.user_email == request.Email);

        if (mainUser == null)
        {
            _logger.LogWarning("❌ User not found in Main DB: {Email}", request.Email);
            return Unauthorized(new
            {
                success = false,
                message = "Invalid email or password",
                userType = "Unknown"
            });
        }

        // ========== STEP 2: CHECK ACCOUNT STATUS ==========
        if (mainUser.status != "active")
        {
            _logger.LogWarning("❌ Inactive account: {Email}, Status: {Status}",
                  request.Email, mainUser.status);
            return Unauthorized(new
            {
                success = false,
                message = $"Account is {mainUser.status}. Please contact support.",
                status = mainUser.status
            });
        }

        // ========== STEP 3: DETERMINE DATABASE LOCATION ==========
        ApplicationDbContext authContext;
        Users userToAuthenticate;
        string dbLocation;
        bool isPrivateCloud = mainUser.is_private_cloud ?? false;

        if (isPrivateCloud)
        {
            _logger.LogInformation("🔍 Private Cloud user detected: {Email}", request.Email);

            try
            {
                // Get private DB connection string
                var privateConnStr = await _tenantConnectionService
                        .GetConnectionStringForUserAsync(request.Email);

                // Create private DB context
                authContext = _dynamicDbContextFactory.CreateDbContext(privateConnStr);
                dbLocation = "Private Cloud DB";

                // Fetch user from private DB
                userToAuthenticate = await authContext.Users
    .FirstOrDefaultAsync(u => u.user_email == request.Email);

                if (userToAuthenticate == null)
                {
                    _logger.LogError("❌ User exists in Main DB but not in Private DB: {Email}",
                    request.Email);
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "User data synchronization error. Please contact support.",
                        error = "USER_NOT_IN_PRIVATE_DB"
                    });
                }

                _logger.LogInformation("✅ User found in Private DB: {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error accessing Private DB for {Email}", request.Email);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error accessing your private database. Please contact support.",
                    error = "PRIVATE_DB_CONNECTION_ERROR"
                });
            }
        }
        else
        {
            _logger.LogInformation("🔍 Normal user detected: {Email}", request.Email);
            authContext = _context;
            userToAuthenticate = mainUser;
            dbLocation = "Main DB";
        }

        // ========== STEP 4: VERIFY PASSWORD ==========
        bool passwordValid = BCrypt.Net.BCrypt.Verify(
           request.Password,
            userToAuthenticate.user_password ?? userToAuthenticate.hash_password
             );

        if (!passwordValid)
        {
            _logger.LogWarning("❌ Invalid password for: {Email}", request.Email);
            return Unauthorized(new
            {
                success = false,
                message = "Invalid email or password"
            });
        }

        _logger.LogInformation("✅ Password verified for: {Email}", request.Email);

        // ========== STEP 5: GET SERVER TIME ==========
        var loginTime = await GetServerTimeAsync();
        var clientIp = GetClientIpAddress();

        // ========== STEP 6: UPDATE USER LOGIN INFO ==========
        userToAuthenticate.last_login = loginTime;
        userToAuthenticate.last_logout = null;
        userToAuthenticate.activity_status = "online";
        userToAuthenticate.updated_at = loginTime;

        authContext.Entry(userToAuthenticate).State = EntityState.Modified;
        await authContext.SaveChangesAsync();

        _logger.LogInformation("✅ Updated user login info for: {Email}", request.Email);

        // ========== STEP 7: CREATE SESSION ==========
        var session = new Sessions
        {
            user_email = request.Email,
            login_time = loginTime,
            ip_address = clientIp,
            user_agent = Request.Headers["User-Agent"].ToString(),
            session_status = "active",
            logout_time = null
        };

        authContext.Sessions.Add(session);
        await authContext.SaveChangesAsync();

        _logger.LogInformation("✅ Session created for: {Email}", request.Email);

        // ========== STEP 8: GENERATE JWT TOKEN ==========
        var token = await GenerateJwtTokenAsync(request.Email, false);

        // ========== STEP 9: GET ROLES AND PERMISSIONS ==========
        var roles = await _roleService.GetUserRolesAsync(request.Email, false);
        var permissions = await _roleService.GetUserPermissionsAsync(request.Email, false);

        // ========== STEP 10: RETURN SUCCESS RESPONSE ==========
        _logger.LogInformation("✅ Login successful: {Email} from {DB}", request.Email, dbLocation);

        return Ok(new RoleBasedLoginResponse
        {
            Token = token,
            UserType = isPrivateCloud ? "PrivateCloudUser" : "User",
            Email = request.Email,
            Roles = roles,
            Permissions = permissions,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UserName = userToAuthenticate.user_name,
            UserRole = userToAuthenticate.user_role,
            UserGroup = userToAuthenticate.user_group,
            Department = userToAuthenticate.department,
            Timezone = userToAuthenticate.timezone,
            LoginTime = loginTime,
            LastLogoutTime = userToAuthenticate.last_logout,
            Phone = userToAuthenticate.phone,
            ParentUserEmail = null,
            UserId = userToAuthenticate.user_id
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Login error for {Email}", request.Email);
        return StatusCode(500, new
        {
            success = false,
            message = "An error occurred during login. Please try again.",
            error = ex.Message,
            stackTrace = _configuration.GetValue<bool>("Performance:EnableDetailedErrorLogging")
            ? ex.StackTrace
            : null
        });
    }
}

// ========== HELPER METHOD: GET CLIENT IP ==========
private string GetClientIpAddress()
{
    var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwardedFor))
    {
        return forwardedFor.Split(',')[0].Trim();
    }

    var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
    if (!string.IsNullOrEmpty(realIp))
    {
        return realIp;
    }

    return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
