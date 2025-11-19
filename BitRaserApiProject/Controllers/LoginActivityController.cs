using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Login Activity Controller - Automatic login/logout tracking for Users and Subusers
    /// ✅ UPDATES: activity_status field directly in database (online/offline)
    /// ✅ UPDATES: last_login, last_logout, LastLoginIp
    /// ❌ NEVER TOUCHES: status field (account status)
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LoginActivityController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LoginActivityController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginActivityController(
            ApplicationDbContext context,
            ILogger<LoginActivityController> logger,
            IHttpClientFactory httpClientFactory)
        {
     _context = context;
            _logger = logger;
   _httpClientFactory = httpClientFactory;
  }

  #region Helper Methods

        /// <summary>
        /// Get server time from TimeController
        /// </summary>
        private async Task<DateTime> GetServerTimeAsync()
     {
         try
        {
       var client = _httpClientFactory.CreateClient();
  client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");
    
      var response = await client.GetAsync("/api/Time/server-time");
      if (response.IsSuccessStatusCode)
   {
         var content = await response.Content.ReadAsStringAsync();
     var json = System.Text.Json.JsonDocument.Parse(content);
      var serverTimeStr = json.RootElement.GetProperty("server_time").GetString();
        return DateTime.Parse(serverTimeStr!);
  }
            }
            catch (Exception ex)
 {
           _logger.LogWarning(ex, "Failed to get server time, using UTC now");
     }
            
      return DateTime.UtcNow;
        }

        /// <summary>
        /// Get client IP address
  /// </summary>
        private string GetClientIpAddress()
    {
   return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
      }

    #endregion

        #region User Login/Logout

        /// <summary>
    /// Record user login - Updates activity_status to "online" in database
      /// POST /api/LoginActivity/user/login
        /// </summary>
        [HttpPost("user/login")]
        public async Task<IActionResult> RecordUserLogin([FromBody] LoginRequest request)
        {
            try
{
            var user = await _context.Users
     .FirstOrDefaultAsync(u => u.user_email == request.Email);

        if (user == null)
     {
     return NotFound(new { success = false, message = "User not found" });
       }

     var serverTime = await GetServerTimeAsync();
                var ipAddress = GetClientIpAddress();

   // ✅ Update activity fields in database
  user.last_login = serverTime;
   user.last_logout = null; // Clear last logout on new login
      user.activity_status = "online"; // ✅ Store in database
        
       _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();

      _logger.LogInformation("User login recorded: {Email} at {Time} from {IP}", 
            request.Email, serverTime, ipAddress);

   return Ok(new
     {
 success = true,
           message = "User login recorded successfully",
     data = new
      {
         email = user.user_email,
        user_name = user.user_name,
          last_login = user.last_login,
     last_logout = user.last_logout,
    activity_status = user.activity_status, // From database
          server_time = serverTime
        }
             });
         }
            catch (Exception ex)
       {
    _logger.LogError(ex, "Error recording user login");
     return StatusCode(500, new { success = false, message = "Error recording login", error = ex.Message });
 }
        }

        /// <summary>
     /// Record user logout - Updates activity_status to "offline" in database
        /// POST /api/LoginActivity/user/logout
        /// </summary>
        [HttpPost("user/logout")]
        public async Task<IActionResult> RecordUserLogout([FromBody] LoginRequest request)
        {
            try
            {
  var user = await _context.Users
            .FirstOrDefaultAsync(u => u.user_email == request.Email);

                if (user == null)
   {
              return NotFound(new { success = false, message = "User not found" });
        }

        var serverTime = await GetServerTimeAsync();

// ✅ Update logout fields in database
                user.last_logout = serverTime;
                user.activity_status = "offline"; // ✅ Store in database
      
                _context.Entry(user).State = EntityState.Modified;
             await _context.SaveChangesAsync();

     _logger.LogInformation("User logout recorded: {Email} at {Time}", 
        request.Email, serverTime);

    return Ok(new
  {
        success = true,
        message = "User logout recorded successfully",
data = new
           {
         email = user.user_email,
      user_name = user.user_name,
   last_login = user.last_login,
     last_logout = user.last_logout,
 activity_status = user.activity_status, // From database
          server_time = serverTime
                 }
       });
      }
    catch (Exception ex)
       {
                _logger.LogError(ex, "Error recording user logout");
           return StatusCode(500, new { success = false, message = "Error recording logout", error = ex.Message });
    }
        }

        /// <summary>
        /// Get user login activity details
   /// GET /api/LoginActivity/user/{email}
        /// </summary>
        [HttpGet("user/{email}")]
        public async Task<IActionResult> GetUserActivity(string email)
        {
        try
          {
         var user = await _context.Users
       .FirstOrDefaultAsync(u => u.user_email == email);

 if (user == null)
     {
   return NotFound(new { success = false, message = "User not found" });
   }

        var serverTime = await GetServerTimeAsync();
                
// Calculate real-time activity status
  var activityStatus = CalculateActivityStatus(user.last_login, user.last_logout, serverTime);

          return Ok(new
           {
        success = true,
        data = new
           {
     email = user.user_email,
    user_name = user.user_name,
  last_login = user.last_login,
        last_logout = user.last_logout,
        activity_status = activityStatus, // Real-time calculated
         server_time = serverTime
             }
          });
 }
   catch (Exception ex)
      {
    _logger.LogError(ex, "Error getting user activity");
        return StatusCode(500, new { success = false, message = "Error getting activity", error = ex.Message });
        }
        }

        #endregion

        #region Subuser Login/Logout

      /// <summary>
        /// Record subuser login - Updates activity_status to "online" in database
     /// POST /api/LoginActivity/subuser/login
        /// </summary>
        [HttpPost("subuser/login")]
        public async Task<IActionResult> RecordSubuserLogin([FromBody] LoginRequest request)
    {
            try
   {
          var subuser = await _context.subuser
             .FirstOrDefaultAsync(s => s.subuser_email == request.Email);

         if (subuser == null)
        {
    return NotFound(new { success = false, message = "Subuser not found" });
         }

      var serverTime = await GetServerTimeAsync();
      var ipAddress = GetClientIpAddress();

       // ✅ Update activity fields in database
        subuser.last_login = serverTime;
  subuser.last_logout = null; // Clear last logout on new login
         subuser.LastLoginIp = ipAddress;
        subuser.activity_status = "online"; // ✅ Store in database
                
             _context.Entry(subuser).State = EntityState.Modified;
           await _context.SaveChangesAsync();

       _logger.LogInformation("Subuser login recorded: {Email} at {Time} from {IP}", 
        request.Email, serverTime, ipAddress);

       return Ok(new
        {
      success = true,
       message = "Subuser login recorded successfully",
       data = new
  {
      email = subuser.subuser_email,
         name = subuser.Name,
     parent_email = subuser.user_email,
         last_login = subuser.last_login,
     last_logout = subuser.last_logout,
            last_login_ip = subuser.LastLoginIp,
         activity_status = subuser.activity_status, // From database
 server_time = serverTime
          }
          });
          }
  catch (Exception ex)
            {
      _logger.LogError(ex, "Error recording subuser login");
    return StatusCode(500, new { success = false, message = "Error recording login", error = ex.Message });
    }
     }

        /// <summary>
        /// Record subuser logout - Updates activity_status to "offline" in database
        /// POST /api/LoginActivity/subuser/logout
      /// </summary>
        [HttpPost("subuser/logout")]
   public async Task<IActionResult> RecordSubuserLogout([FromBody] LoginRequest request)
        {
    try
      {
         var subuser = await _context.subuser
         .FirstOrDefaultAsync(s => s.subuser_email == request.Email);

  if (subuser == null)
   {
             return NotFound(new { success = false, message = "Subuser not found" });
     }

         var serverTime = await GetServerTimeAsync();

    // ✅ Update logout fields in database
     subuser.last_logout = serverTime;
                subuser.activity_status = "offline"; // ✅ Store in database
       
 _context.Entry(subuser).State = EntityState.Modified;
   await _context.SaveChangesAsync();

 _logger.LogInformation("Subuser logout recorded: {Email} at {Time}", 
      request.Email, serverTime);

           return Ok(new
     {
     success = true,
     message = "Subuser logout recorded successfully",
   data = new
        {
  email = subuser.subuser_email,
    name = subuser.Name,
         parent_email = subuser.user_email,
          last_login = subuser.last_login,
  last_logout = subuser.last_logout,
        last_login_ip = subuser.LastLoginIp,
  activity_status = subuser.activity_status, // From database
       server_time = serverTime
      }
    });
   }
          catch (Exception ex)
       {
         _logger.LogError(ex, "Error recording subuser logout");
   return StatusCode(500, new { success = false, message = "Error recording logout", error = ex.Message });
          }
        }

 /// <summary>
        /// Get subuser login activity details
     /// GET /api/LoginActivity/subuser/{email}
        /// </summary>
        [HttpGet("subuser/{email}")]
        public async Task<IActionResult> GetSubuserActivity(string email)
    {
    try
            {
          var subuser = await _context.subuser
      .FirstOrDefaultAsync(s => s.subuser_email == email);

 if (subuser == null)
    {
          return NotFound(new { success = false, message = "Subuser not found" });
       }

           var serverTime = await GetServerTimeAsync();
  
     // Calculate real-time activity status
     var activityStatus = CalculateActivityStatus(subuser.last_login, subuser.last_logout, serverTime);

         return Ok(new
        {
             success = true,
 data = new
   {
            email = subuser.subuser_email,
     name = subuser.Name,
        parent_email = subuser.user_email,
            last_login = subuser.last_login,
       last_logout = subuser.last_logout,
           last_login_ip = subuser.LastLoginIp,
             activity_status = activityStatus, // Real-time calculated
          server_time = serverTime
    }
      });
         }
    catch (Exception ex)
            {
         _logger.LogError(ex, "Error getting subuser activity");
     return StatusCode(500, new { success = false, message = "Error getting activity", error = ex.Message });
            }
        }

    #endregion

     #region Get All Activities

        /// <summary>
        /// Get all users activity (login/logout details)
        /// GET /api/LoginActivity/users
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsersActivity()
        {
            try
        {
         var serverTime = await GetServerTimeAsync();
    var users = await _context.Users.ToListAsync();

      var activities = users.Select(u => new
    {
         email = u.user_email,
  user_name = u.user_name,
              last_login = u.last_login,
        last_logout = u.last_logout,
         activity_status = CalculateActivityStatus(u.last_login, u.last_logout, serverTime) // Real-time
    }).ToList();

       return Ok(new
     {
       success = true,
    server_time = serverTime,
           total = activities.Count,
       online_count = activities.Count(a => a.activity_status == "online"),
  offline_count = activities.Count(a => a.activity_status == "offline"),
     data = activities
       });
            }
catch (Exception ex)
 {
         _logger.LogError(ex, "Error getting all users activity");
            return StatusCode(500, new { success = false, message = "Error getting activities", error = ex.Message });
        }
        }

        /// <summary>
    /// Get all subusers activity (login/logout details)
/// GET /api/LoginActivity/subusers
        /// </summary>
     [HttpGet("subusers")]
        public async Task<IActionResult> GetAllSubusersActivity()
 {
        try
            {
   var serverTime = await GetServerTimeAsync();
             var subusers = await _context.subuser.ToListAsync();

    var activities = subusers.Select(s => new
      {
          email = s.subuser_email,
      name = s.Name,
          parent_email = s.user_email,
       last_login = s.last_login,
 last_logout = s.last_logout,
    last_login_ip = s.LastLoginIp,
     activity_status = CalculateActivityStatus(s.last_login, s.last_logout, serverTime) // Real-time
            }).ToList();

     return Ok(new
     {
           success = true,
            server_time = serverTime,
  total = activities.Count,
             online_count = activities.Count(a => a.activity_status == "online"),
     offline_count = activities.Count(a => a.activity_status == "offline"),
     data = activities
           });
  }
            catch (Exception ex)
            {
      _logger.LogError(ex, "Error getting all subusers activity");
                return StatusCode(500, new { success = false, message = "Error getting activities", error = ex.Message });
     }
}

        /// <summary>
        /// Get parent's subusers activity
      /// GET /api/LoginActivity/parent/{parentEmail}/subusers
   /// </summary>
    [HttpGet("parent/{parentEmail}/subusers")]
      public async Task<IActionResult> GetParentSubusersActivity(string parentEmail)
        {
            try
            {
      var serverTime = await GetServerTimeAsync();
   var subusers = await _context.subuser
          .Where(s => s.user_email == parentEmail)
        .ToListAsync();

          var activities = subusers.Select(s => new
      {
               email = s.subuser_email,
       name = s.Name,
         role = s.Role,
      department = s.Department,
    last_login = s.last_login,
         last_logout = s.last_logout,
        last_login_ip = s.LastLoginIp,
            activity_status = CalculateActivityStatus(s.last_login, s.last_logout, serverTime) // Real-time
}).ToList();

                return Ok(new
   {
   success = true,
 parent_email = parentEmail,
        server_time = serverTime,
         total = activities.Count,
      online_count = activities.Count(a => a.activity_status == "online"),
offline_count = activities.Count(a => a.activity_status == "offline"),
   data = activities
       });
       }
    catch (Exception ex)
            {
       _logger.LogError(ex, "Error getting parent subusers activity");
  return StatusCode(500, new { success = false, message = "Error getting activities", error = ex.Message });
            }
     }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Calculate activity status based on last login/logout
        /// Online if: last_login exists AND (no logout OR logout before login) AND within 5 mins
      /// </summary>
private string CalculateActivityStatus(DateTime? lastLogin, DateTime? lastLogout, DateTime serverTime)
   {
            if (lastLogin == null) return "offline";

            if (lastLogout.HasValue && lastLogout > lastLogin) return "offline";
            
    var minutesSinceLogin = (serverTime - lastLogin.Value).TotalMinutes;
            return minutesSinceLogin <= 5 ? "online" : "offline";
        }

 #endregion
    }

    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginRequest
    {
   public string Email { get; set; } = string.Empty;
  }
}
