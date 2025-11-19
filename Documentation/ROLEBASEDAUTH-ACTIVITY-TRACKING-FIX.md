# ⚠️ RoleBasedAuthController - Activity Tracking Integration

## 🚨 **CRITICAL: File Corrupted**

The `RoleBasedAuthController.cs` file got corrupted during edit. Please **manually restore** from git or backup.

---

## ✅ **What Needs to Be Added:**

### **1. Login Endpoint - Add Server Time & Activity Tracking**

**Location:** `[HttpPost("login")]` method

**Add after authentication success (around line 120):**

```csharp
// ✅ Get server time from TimeController  
DateTime loginTime;
try
{
    var client = new HttpClient { BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}") };
    var timeResponse = await client.GetAsync("/api/Time/server-time");
    if (timeResponse.IsSuccessStatusCode)
    {
        var content = await timeResponse.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(content);
        var serverTimeStr = json.RootElement.GetProperty("server_time").GetString();
        loginTime = DateTime.Parse(serverTimeStr!);
    }
    else
    {
        loginTime = DateTime.UtcNow;
    }
}
catch
{
    loginTime = DateTime.UtcNow; // Fallback to UTC
}
```

**Replace existing session creation & last_login update:**

```csharp
// Create session entry for tracking
var session = new Sessions
{
    user_email = userEmail,
    login_time = loginTime,  // ✅ Use server time instead of DateTime.UtcNow
    ip_address = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
    device_info = Request.Headers["User-Agent"].ToString(),
 session_status = "active"
};

_context.Sessions.Add(session);

// ✅ Update last_login, last_logout, activity_status using server time
if (isSubuser && subuserData != null)
{
    subuserData.last_login = loginTime;
    subuserData.last_logout = null; // Clear logout on new login
    subuserData.LastLoginIp = session.ip_address;
    subuserData.activity_status = "online"; // ✅ Set to online
    _context.Entry(subuserData).State = EntityState.Modified;
}
else if (mainUser != null)
{
    mainUser.last_login = loginTime;
    mainUser.last_logout = null; // Clear logout on new login
    mainUser.activity_status = "online"; // ✅ Set to online
    _context.Entry(mainUser).State = EntityState.Modified;
}
```

---

### **2. Logout Endpoint - Add Server Time & Activity Tracking**

**Location:** `[HttpPost("logout")]` method

**Replace existing logout time logic:**

```csharp
var isSubuser = userType == "subuser";

// ✅ Get server time from TimeController
DateTime logoutTime;
try
{
    var client = new HttpClient { BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}") };
    var timeResponse = await client.GetAsync("/api/Time/server-time");
    if (timeResponse.IsSuccessStatusCode)
    {
    var content = await timeResponse.Content.ReadAsStringAsync();
 var json = System.Text.Json.JsonDocument.Parse(content);
        var serverTimeStr = json.RootElement.GetProperty("server_time").GetString();
        logoutTime = DateTime.Parse(serverTimeStr!);
    }
    else
    {
      logoutTime = DateTime.UtcNow;
    }
}
catch
{
    logoutTime = DateTime.UtcNow; // Fallback to UTC
}

// End all active sessions for this user
var activeSessions = await _context.Sessions
    .Where(s => s.user_email == userEmail && s.session_status == "active")
    .ToListAsync();

foreach (var session in activeSessions)
{
    session.logout_time = logoutTime;  // ✅ Use server time
    session.session_status = "closed";
}

// ✅ Update last_logout and activity_status in users or subusers table
if (isSubuser)
{
    var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == userEmail);
    if (subuser != null)
    {
        subuser.last_logout = logoutTime;
        subuser.activity_status = "offline"; // ✅ Set to offline
        _context.Entry(subuser).State = EntityState.Modified;
    }
}
else
{
var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
    if (user != null)
    {
 user.last_logout = logoutTime;
   user.activity_status = "offline"; // ✅ Set to offline
        _context.Entry(user).State = EntityState.Modified;
    }
}

await _context.SaveChangesAsync();

_logger.LogInformation("User logout: {Email} ({UserType}) at {LogoutTime}",  
    userEmail, isSubuser ? "subuser" : "user", logoutTime);
```

**Update logout response to include activity_status:**

```csharp
return Ok(new
{
    success = true,
    message = "Logout successful - JWT token cleared, user logged out automatically",
    email = userEmail,
    userType = isSubuser ? "subuser" : "user",
    logoutTime = logoutTime,
    activity_status = "offline", // ✅ Confirm offline status
    sessionsEnded = activeSessions.Count,
    clearToken = true,
    swaggerLogout = true
});
```

---

## 📋 **Summary of Changes:**

| Change | Before | After |
|--------|--------|-------|
| **Login Time** | `DateTime.UtcNow` | Server time from `/api/Time/server-time` |
| **Logout Time** | `DateTime.UtcNow` | Server time from `/api/Time/server-time` |
| **activity_status on Login** | Not updated | Set to `"online"` |
| **activity_status on Logout** | Not updated | Set to `"offline"` |
| **last_logout on Login** | Not cleared | Set to `null` |

---

## ✅ **After Making Changes:**

1. **Restore the file from git:**
   ```bash
   git checkout BitRaserApiProject/Controllers/RoleBasedAuthController.cs
   ```

2. **Apply changes manually** (copy-paste from above)

3. **Build the project:**
   ```bash
   dotnet build
   ```

4. **Test login:**
   ```bash
   POST /api/RoleBasedAuth/login
   {
  "email": "test@example.com",
     "password": "password"
   }
   ```

5. **Verify database:**
   ```sql
   SELECT user_email, last_login, last_logout, activity_status 
   FROM users 
   WHERE user_email = 'test@example.com';
   ```

---

## 🎯 **Expected Results:**

### **After Login:**
```json
{
  "success": true,
  "token": "...",
  "email": "test@example.com",
  "loginTime": "2025-01-26T15:30:00Z"
}
```

**Database:**
```
last_login: 2025-01-26 15:30:00
last_logout: NULL
activity_status: "online"
```

### **After Logout:**
```json
{
  "success": true,
  "email": "test@example.com",
  "logoutTime": "2025-01-26T16:00:00Z",
  "activity_status": "offline"
}
```

**Database:**
```
last_login: 2025-01-26 15:30:00
last_logout: 2025-01-26 16:00:00
activity_status: "offline"
```

---

**Status:** ⚠️ **Manual Fix Required**  
**File:** `RoleBasedAuthController.cs`  
**Action:** Restore from git and apply changes above  

---

**Perfect tracking with server time! 🎉✅🚀**
