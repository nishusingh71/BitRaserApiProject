# Logout Functionality Implementation - BitRaser API Project

## 🚪 **Comprehensive Logout System**

This document describes the complete logout functionality implementation that handles JWT token invalidation and session expiration for both users and subusers.

---

## 📋 **Overview**

The logout system consists of:
1. **Enhanced Authentication Controller** - Handles login with session creation
2. **Logout Controller** - Manages logout operations and session termination
3. **Session Management** - Tracks active sessions and manages expiration
4. **Audit Logging** - Records all authentication events

---

## 🏗️ **Implementation Details**

### **1. Enhanced Authentication Controller**

**File:** `BitRaserApiProject/Controllers/EnhancedAuthController.cs`

#### **Features:**
- ✅ Unified login for users and subusers
- ✅ Session creation during login
- ✅ JWT token generation with session ID
- ✅ Token refresh functionality
- ✅ Token validation endpoint
- ✅ Comprehensive audit logging

#### **Key Endpoints:**

##### **POST /api/EnhancedAuth/login**
```json
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
  "expiresAt": "2024-01-01T16:00:00Z",
  "sessionId": 123
}
```

##### **POST /api/EnhancedAuth/refresh**
- Refreshes an existing valid token
- Extends session expiration
- Requires valid authorization header

##### **POST /api/EnhancedAuth/validate**
- Validates current token
- Returns session information
- Useful for frontend session checks

---

### **2. Logout Controller**

**File:** `BitRaserApiProject/Controllers/LogoutController.cs`

#### **Features:**
- ✅ Single session logout
- ✅ All sessions logout
- ✅ Specific session logout by ID
- ✅ Session status checking
- ✅ Audit trail logging
- ✅ Support for both users and subusers

#### **Key Endpoints:**

##### **POST /api/Logout**
```json
{
  "sessionId": 123,
  "logoutAllSessions": false
}
```

**Response:**
```json
{
  "message": "Successfully logged out - 1 session(s) ended",
  "sessionsEnded": 1,
  "logoutTime": "2024-01-01T12:00:00Z",
  "email": "user@example.com",
  "userType": "user"
}
```

##### **POST /api/Logout/all**
- Convenience endpoint to logout from all sessions
- No request body required
- Uses current user's token to identify sessions

##### **GET /api/Logout/session-status**
- Returns current session information
- Lists all active sessions for the user
- Identifies current session from JWT token

**Response:**
```json
{
  "email": "user@example.com",
  "user_type": "user",
  "active_sessions": [
    {
      "session_id": 123,
      "login_time": "2024-01-01T10:00:00Z",
      "ip_address": "192.168.1.100",
      "device_info": "Chrome 120.0 on Windows 11",
      "is_current": true
    }
  ],
  "total_active_sessions": 1,
  "session_valid": true
}
```

---

## 🔄 **Session Management Flow**

### **Login Process:**
1. User provides credentials
2. System validates user/subuser credentials using BCrypt
3. Creates new session record in database
4. Generates JWT token with session ID embedded
5. Returns token and session information
6. Logs authentication event

### **Session Tracking:**
- Each session has unique ID
- Tracks login time, IP address, device info
- Session status: `active`, `closed`, `expired`, `force_closed`
- JWT tokens contain session ID for tracking

### **Logout Process:**
1. Extract user info from JWT token
2. Identify sessions to terminate:
   - Current session (from JWT)
   - Specific session (by ID)
   - All user sessions
3. Update session status to `closed`
4. Set logout timestamp
5. Log logout event
6. Return confirmation

---

## 📊 **Database Schema Impact**

### **Sessions Table:**
```sql
- session_id (int, primary key)
- user_email (varchar, indexed)
- login_time (datetime)
- logout_time (datetime, nullable)
- ip_address (varchar)
- device_info (varchar)
- session_status (varchar) -- 'active', 'closed', 'expired', 'force_closed'
```

### **Logs Table:**
```sql
- log_id (int, primary key)
- user_email (varchar)
- log_level (varchar) -- 'INFO', 'WARNING', 'ERROR'
- log_message (varchar)
- log_details_json (json)
- created_at (datetime)
```

---

## 🔐 **Security Features**

### **JWT Token Enhancement:**
- Includes `session_id` claim for session tracking
- Includes `user_type` claim (user/subuser)
- 8-hour expiration time
- Secure signing with configured secret key

### **Session Security:**
- IP address tracking
- Device fingerprinting
- Automatic session cleanup
- Force logout capability (admin)
- Audit trail for all authentication events

### **Authorization:**
- Users can only manage their own sessions
- Admins can force logout other users
- Subusers have same session management as users
- Permission-based access control

---

## 🚀 **Usage Examples**

### **Frontend JavaScript Integration:**

#### **Login:**
```javascript
async function login(email, password) {
    const response = await fetch('/api/EnhancedAuth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
    });
    
    const data = await response.json();
    if (response.ok) {
        localStorage.setItem('authToken', data.token);
        localStorage.setItem('sessionId', data.sessionId);
        return data;
    }
    throw new Error(data.message);
}
```

#### **Logout:**
```javascript
async function logout(logoutAllSessions = false) {
    const token = localStorage.getItem('authToken');
    const response = await fetch('/api/Logout', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ logoutAllSessions })
    });
    
    const data = await response.json();
    if (response.ok) {
        localStorage.removeItem('authToken');
        localStorage.removeItem('sessionId');
        return data;
    }
    throw new Error(data.message);
}
```

#### **Session Check:**
```javascript
async function checkSession() {
    const token = localStorage.getItem('authToken');
    const response = await fetch('/api/Logout/session-status', {
        headers: { 'Authorization': `Bearer ${token}` }
    });
    
    if (response.ok) {
        return await response.json();
    }
    
    // Token invalid, redirect to login
    localStorage.removeItem('authToken');
    window.location.href = '/login';
    return null;
}
```

### **cURL Examples:**

#### **Login:**
```bash
curl -X POST "http://localhost:4000/api/EnhancedAuth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'
```

#### **Logout:**
```bash
curl -X POST "http://localhost:4000/api/Logout" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"logoutAllSessions": false}'
```

#### **Session Status:**
```bash
curl -X GET "http://localhost:4000/api/Logout/session-status" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## 🛠️ **Configuration**

### **JWT Settings (appsettings.json):**
```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong123456789!",
    "Issuer": "BitRaserAPI",
    "Audience": "BitRaserAPIUsers"
  }
}
```

### **Environment Variables (.env):**
```
Jwt__Key=YourSuperSecretKeyThatIsAtLeast32CharactersLong123456789!
Jwt__Issuer=BitRaserAPI
Jwt__Audience=BitRaserAPIUsers
```

---

## 📈 **Monitoring & Analytics**

### **Session Analytics:**
- Track concurrent sessions per user
- Monitor login/logout patterns
- Identify suspicious activity (multiple IPs)
- Generate session reports

### **Audit Trail:**
- All authentication events logged
- User type tracking (user/subuser)
- IP address and device tracking
- Logout reason tracking (user vs forced)

### **Sample Log Entries:**
```json
{
  "user_email": "user@example.com",
  "log_level": "INFO",
  "log_message": "User login successful",
  "log_details_json": {
    "user_type": "user",
    "session_id": 123,
    "login_time": "2024-01-01T10:00:00Z",
    "ip_address": "192.168.1.100",
    "user_agent": "Chrome 120.0 on Windows 11"
  }
}
```

---

## 🔧 **Administration**

### **Force Logout (Admin Feature):**
```javascript
// Implemented in RoleBasedAuthController
async function forceLogout(targetEmail, isSubuser = false) {
    const response = await fetch('/api/RoleBasedAuth/force-logout', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${adminToken}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            targetEmail,
            isTargetSubuser: isSubuser
        })
    });
    return response.json();
}
```

### **Session Cleanup:**
- Automatic cleanup on session access
- Manual cleanup endpoints available
- Configurable session timeouts
- Expired session handling

---

## 🎯 **Testing**

### **Test Scenarios:**
1. ✅ **Normal Login/Logout Flow**
2. ✅ **Multiple Session Management**
3. ✅ **Token Expiration Handling**
4. ✅ **Invalid Token Scenarios**
5. ✅ **Session Hijacking Prevention**
6. ✅ **Force Logout by Admin**
7. ✅ **Subuser Authentication**
8. ✅ **Audit Log Verification**

### **Postman Collection Available:**
- Complete API endpoint testing
- Authentication flow testing
- Session management testing
- Error scenario testing

---

## 🚨 **Error Handling**

### **Common Error Responses:**

#### **401 Unauthorized:**
```json
{
  "message": "Invalid token or user not found"
}
```

#### **403 Forbidden:**
```json
{
  "error": "Insufficient permissions to logout this user"
}
```

#### **404 Not Found:**
```json
{
  "message": "Session not found or already expired"
}
```

#### **500 Internal Server Error:**
```json
{
  "message": "An error occurred during logout"
}
```

---

## 🎉 **Status: Implementation Complete**

### **✅ Implemented Features:**
- ✅ Enhanced authentication with session creation
- ✅ Comprehensive logout functionality
- ✅ Session status checking
- ✅ Audit logging
- ✅ JWT token management
- ✅ Multiple session support
- ✅ Force logout capability
- ✅ User and subuser support
- ✅ IP and device tracking
- ✅ Secure session management

### **🚀 Ready for Production:**
- ✅ Complete error handling
- ✅ Security best practices
- ✅ Comprehensive logging
- ✅ API documentation
- ✅ Frontend integration examples
- ✅ Admin management features

---

## 📞 **Support**

For questions or issues with the logout functionality:
1. Check the audit logs for authentication events
2. Verify JWT token configuration
3. Review session database records
4. Check network connectivity and CORS settings
5. Validate user permissions and roles

**Happy Coding! 🚀**