# ✅ RoleBasedAuth - Server Time & Activity Status Integration

## 🎉 **COMPLETE: Automatic Activity Tracking**

RoleBasedAuthController ab **automatic** login/logout tracking karta hai using **server time** aur `activity_status` field.

---

## 📋 **What Was Implemented:**

### **1. IHttpClientFactory Dependency Added**

```csharp
private readonly IHttpClientFactory _httpClientFactory;

public RoleBasedAuthController(
    ApplicationDbContext context, 
    IConfiguration configuration,
    IRoleBasedAuthService roleService,
    ILogger<RoleBasedAuthController> logger,
    IHttpClientFactory httpClientFactory)  // ✅ Added
{
    _context = context;
    _configuration = configuration;
    _roleService = roleService;
    _logger = logger;
    _httpClientFactory = httpClientFactory;  // ✅ Injected
}
```

---

### **2. Helper Method - GetServerTimeAsync()**

```csharp
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
    
    return DateTime.UtcNow;  // Fallback
}
```

**Features:**
- ✅ Calls `/api/Time/server-time` endpoint
- ✅ Handles failures gracefully with UTC fallback
- ✅ Logs warnings on error
- ✅ Returns `DateTime` for use in login/logout

---

### **3. Login Endpoint Updates**

#### **Before:**
```csharp
var loginTime = DateTime.UtcNow;
var session = new Sessions
{
    user_email = userEmail,
    login_time = loginTime,
    // ...
};

// Update last_login only
if (isSubuser && subuserData != null)
{
    subuserData.last_login = loginTime;
 subuserData.LastLoginIp = session.ip_address;
}
else if (mainUser != null)
{
    mainUser.last_login = loginTime;
}
```

#### **After:**
```csharp
// ✅ Get server time for login
var loginTime = await GetServerTimeAsync();

var session = new Sessions
{
    user_email = userEmail,
    login_time = loginTime,  // ✅ Server time
 // ...
};

// ✅ Update last_login, last_logout, activity_status using server time
if (isSubuser && subuserData != null)
{
    subuserData.last_login = loginTime;  // ✅ Server time
  subuserData.last_logout = null;  // ✅ Clear logout
    subuserData.LastLoginIp = session.ip_address;
    subuserData.activity_status = "online";  // ✅ Set online
}
else if (mainUser != null)
{
    mainUser.last_login = loginTime;  // ✅ Server time
    mainUser.last_logout = null;  // ✅ Clear logout
    mainUser.activity_status = "online";  // ✅ Set online
}
```

---

### **4. Logout Endpoint Updates**

#### **Before:**
```csharp
var logoutTime = DateTime.UtcNow;

foreach (var session in activeSessions)
{
    session.logout_time = logoutTime;
    session.session_status = "closed";
}

// Update last_logout only
if (isSubuser)
{
    var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == userEmail);
    if (subuser != null)
    {
        subuser.last_logout = logoutTime;
    }
}
else
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
    if (user != null)
    {
        user.last_logout = logoutTime;
    }
}
```

#### **After:**
```csharp
// ✅ Get server time for logout
var logoutTime = await GetServerTimeAsync();

foreach (var session in activeSessions)
{
 session.logout_time = logoutTime;  // ✅ Server time
    session.session_status = "closed";
}

// ✅ Update last_logout and activity_status using server time
if (isSubuser)
{
    var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == userEmail);
    if (subuser != null)
    {
   subuser.last_logout = logoutTime;  // ✅ Server time
    subuser.activity_status = "offline";  // ✅ Set offline
    }
}
else
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
    if (user != null)
    {
        user.last_logout = logoutTime;  // ✅ Server time
        user.activity_status = "offline";  // ✅ Set offline
    }
}
```

**Logout Response Enhanced:**
```csharp
return Ok(new
{
 success = true,
    message = "Logout successful - JWT token cleared, user logged out automatically",
    email = userEmail,
    userType = isSubuser ? "subuser" : "user",
    logoutTime = logoutTime,
    activity_status = "offline",  // ✅ Confirm offline status
    sessionsEnded = activeSessions.Count,
    clearToken = true,
 swaggerLogout = true
});
```

---

## 📊 **How It Works:**

### **Login Flow:**

```
1. User/Subuser authenticates with email & password
   ↓
2. GetServerTimeAsync() fetches time from /api/Time/server-time
   ↓
3. Create session with server time
   ↓
4. Update database:
   - last_login = server time
   - last_logout = NULL
   - activity_status = "online"
   ↓
5. Return JWT token + login details
```

### **Logout Flow:**

```
1. User/Subuser calls /logout with valid JWT
   ↓
2. GetServerTimeAsync() fetches time from /api/Time/server-time
   ↓
3. End all active sessions with server time
 ↓
4. Update database:
   - last_logout = server time
 - activity_status = "offline"
   ↓
5. Return success response with logout details
```

---

## 🧪 **Testing:**

### **1. Test Login (User)**

```bash
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "Admin@123"
}
```

**Expected Response:**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userType": "user",
  "email": "admin@example.com",
  "loginTime": "2025-01-26T15:30:00Z",  // ✅ Server time
  "roles": ["SuperAdmin"],
  "permissions": [...]
}
```

**Database Verification:**
```sql
SELECT user_email, last_login, last_logout, activity_status, status 
FROM users 
WHERE user_email = 'admin@example.com';
```

**Expected:**
```
user_email: admin@example.com
last_login: 2025-01-26 15:30:00  ✅ Server time
last_logout: NULL  ✅ Cleared on login
activity_status: online  ✅ Set to online
status: active  ✅ Unchanged (account status)
```

---

### **2. Test Login (Subuser)**

```bash
POST http://localhost:4000/api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "subuser@example.com",
  "password": "Subuser@123"
}
```

**Expected Response:**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userType": "subuser",
  "email": "subuser@example.com",
  "loginTime": "2025-01-26T15:30:00Z",  // ✅ Server time
  "parentUserEmail": "admin@example.com",
  "roles": ["Manager"],
  "permissions": [...]
}
```

**Database Verification:**
```sql
SELECT subuser_email, last_login, last_logout, LastLoginIp, activity_status, status 
FROM subuser 
WHERE subuser_email = 'subuser@example.com';
```

**Expected:**
```
subuser_email: subuser@example.com
last_login: 2025-01-26 15:30:00  ✅ Server time
last_logout: NULL  ✅ Cleared on login
LastLoginIp: 192.168.1.100  ✅ IP recorded
activity_status: online  ✅ Set to online
status: active  ✅ Unchanged (account status)
```

---

### **3. Test Logout (User)**

```bash
POST http://localhost:4000/api/RoleBasedAuth/logout
Authorization: Bearer YOUR_JWT_TOKEN
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Logout successful - JWT token cleared, user logged out automatically",
  "email": "admin@example.com",
  "userType": "user",
  "logoutTime": "2025-01-26T16:00:00Z",  // ✅ Server time
"activity_status": "offline",  // ✅ Confirmed offline
  "sessionsEnded": 1,
  "clearToken": true,
"swaggerLogout": true
}
```

**Database Verification:**
```sql
SELECT user_email, last_login, last_logout, activity_status 
FROM users 
WHERE user_email = 'admin@example.com';
```

**Expected:**
```
user_email: admin@example.com
last_login: 2025-01-26 15:30:00  ✅ Preserved
last_logout: 2025-01-26 16:00:00  ✅ Server time
activity_status: offline  ✅ Set to offline
```

---

### **4. Test Logout (Subuser)**

```bash
POST http://localhost:4000/api/RoleBasedAuth/logout
Authorization: Bearer YOUR_SUBUSER_JWT_TOKEN
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Logout successful - JWT token cleared, user logged out automatically",
  "email": "subuser@example.com",
  "userType": "subuser",
  "logoutTime": "2025-01-26T16:00:00Z",  // ✅ Server time
  "activity_status": "offline",  // ✅ Confirmed offline
  "sessionsEnded": 1,
  "clearToken": true,
  "swaggerLogout": true
}
```

**Database Verification:**
```sql
SELECT subuser_email, last_login, last_logout, activity_status 
FROM subuser 
WHERE subuser_email = 'subuser@example.com';
```

**Expected:**
```
subuser_email: subuser@example.com
last_login: 2025-01-26 15:30:00  ✅ Preserved
last_logout: 2025-01-26 16:00:00  ✅ Server time
activity_status: offline✅ Set to offline
```

---

## 📋 **Summary of Changes:**

| Aspect | Before | After |
|--------|--------|-------|
| **Login Time Source** | `DateTime.UtcNow` | Server time from `/api/Time/server-time` |
| **Logout Time Source** | `DateTime.UtcNow` | Server time from `/api/Time/server-time` |
| **Login Updates** | `last_login` only | `last_login`, `last_logout=NULL`, `activity_status="online"` |
| **Logout Updates** | `last_logout` only | `last_logout`, `activity_status="offline"` |
| **Fallback Handling** | N/A | Falls back to UTC on API failure |
| **Logging** | Basic | Enhanced with warnings on server time failure |
| **Response Fields** | Basic | Includes `activity_status` confirmation |

---

## ✅ **Benefits:**

1. ✅ **Centralized Time:** All timestamps come from server time API
2. ✅ **Timezone Consistency:** No local timezone issues
3. ✅ **Activity Tracking:** Real-time online/offline status
4. ✅ **Automatic Updates:** No manual intervention needed
5. ✅ **Graceful Degradation:** Falls back to UTC on API failure
6. ✅ **Comprehensive Logging:** Easy debugging and monitoring
7. ✅ **Clean Separation:** `status` (account) vs `activity_status` (online/offline)

---

## 🎯 **Database Schema:**

### **Users Table:**
```sql
last_login       DATETIME NULL      -- Server time of last login
last_logout      DATETIME NULL      -- Server time of last logout
activity_status  VARCHAR(50) NULL   -- "online" or "offline"
status  VARCHAR(50) NULL   -- Account status (active, inactive, etc.)
```

### **Subuser Table:**
```sql
last_login       DATETIME NULL      -- Server time of last login
last_logout      DATETIME NULL      -- Server time of last logout
LastLoginIp VARCHAR(500) NULL  -- IP address of last login
activity_status  VARCHAR(50) NULL   -- "online" or "offline"
status           VARCHAR(50) NULL   -- Account status (active, inactive, etc.)
```

### **Sessions Table:**
```sql
login_time       DATETIME NOT NULL  -- Server time of session start
logout_time      DATETIME NULL      -- Server time of session end
session_status   VARCHAR(50) NULL   -- "active" or "closed"
```

---

## 🚀 **Complete Integration:**

Your system now has **three complementary components**:

### **1. RoleBasedAuthController** (✅ Complete)
- Automatic login/logout tracking
- Server time integration
- Activity status management
- Both users & subusers supported

### **2. LoginActivityController** (✅ Complete)
- Manual activity tracking endpoints
- Real-time status calculation
- Detailed activity reports
- Parent-subuser relationship tracking

### **3. TimeController** (✅ Complete)
- Centralized server time source
- Timezone-aware timestamps
- Consistent across all endpoints

---

## 📊 **Final Status:**

| Component | Status | Details |
|-----------|--------|---------|
| **IHttpClientFactory** | ✅ Injected | Dependency added |
| **GetServerTimeAsync()** | ✅ Complete | Helper method working |
| **Login Tracking** | ✅ Complete | Server time + activity_status |
| **Logout Tracking** | ✅ Complete | Server time + activity_status |
| **User Support** | ✅ Complete | Updates `users` table |
| **Subuser Support** | ✅ Complete | Updates `subuser` table |
| **Error Handling** | ✅ Complete | Graceful fallback to UTC |
| **Logging** | ✅ Complete | Comprehensive logging added |
| **Build** | ✅ Successful | No compilation errors |

---

## 🎉 **Achievement Unlocked!**

**RoleBasedAuthController** ab fully automatic activity tracking karta hai:

✅ **Server time se login/logout record hota hai**  
✅ **activity_status automatically update hota hai**  
✅ **Users aur Subusers dono track hote hain**  
✅ **Graceful error handling with UTC fallback**  
✅ **Build successful - production ready!**

---

**Perfect integration - Ready for production! 🎉✅🚀**
