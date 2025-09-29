# 🚀 **Enhanced Commands, Sessions, and Logs Controllers Guide**

## 📋 **Overview**

आपके BitRaser API Project में अब **Enhanced Commands**, **Enhanced Sessions**, और **Enhanced Logs** controllers add हो गए हैं। ये सभी comprehensive **role-based access control**, **session expiration management**, और **advanced logging features** के साथ आते हैं।

---

## 🎯 **New Enhanced Controllers**

### **1. EnhancedCommandsController** 
**Path**: `http://localhost:4000/api/EnhancedCommands`

#### **🔧 Key Features:**
- ✅ **Role-based Command Management** - Commands को roles के based पर manage करना
- ✅ **Command Status Tracking** - Real-time command execution status
- ✅ **Bulk Operations** - Multiple commands को एक साथ update करना
- ✅ **Command Analytics** - Command statistics और analytics
- ✅ **Comprehensive Validation** - Security और data integrity

#### **📊 Command Status Flow:**
```
Pending → Processing → Completed/Failed/Cancelled
```

#### **🛡️ Permission Requirements:**
- `READ_ALL_COMMANDS` - View all commands (Admin+)
- `CREATE_COMMAND` - Create new commands (Manager+)
- `UPDATE_COMMAND` - Update command details (Admin+)
- `UPDATE_COMMAND_STATUS` - Update command status (Support+)
- `DELETE_COMMAND` - Delete commands (Admin only)
- `BULK_UPDATE_COMMANDS` - Bulk operations (Admin only)
- `READ_COMMAND_STATISTICS` - View analytics (Manager+)

#### **Sample Endpoints:**
```http
GET /api/EnhancedCommands - Get all commands (filtered by role)
GET /api/EnhancedCommands/{id} - Get specific command
POST /api/EnhancedCommands - Create new command
PUT /api/EnhancedCommands/{id} - Update command
PATCH /api/EnhancedCommands/{id}/status - Update command status
DELETE /api/EnhancedCommands/{id} - Delete command
GET /api/EnhancedCommands/statistics - Command analytics
PATCH /api/EnhancedCommands/bulk-update-status - Bulk status update
```

---

### **2. EnhancedSessionsController** 
**Path**: `http://localhost:4000/api/EnhancedSessions`

#### **🔧 Key Features:**
- ✅ **Automatic Session Expiration** - Configurable session timeouts
- ✅ **Session Extension** - Users can extend their sessions
- ✅ **Multi-device Management** - Track sessions across devices
- ✅ **Session Analytics** - Comprehensive session statistics
- ✅ **Security Features** - IP tracking, device fingerprinting

#### **⏰ Session Expiration System:**
```javascript
Default Session: 24 hours
Extended Session: 7 days (Remember Me)
Automatic Cleanup: Expired sessions marked as "expired"
Real-time Tracking: Time remaining calculation
```

#### **🛡️ Permission Requirements:**
- `READ_ALL_SESSIONS` - View all sessions (Admin+)
- `READ_SESSION` - View individual sessions
- `READ_USER_SESSIONS` - View sessions for managed users (Manager+)
- `END_SESSION` - End own sessions
- `END_ALL_SESSIONS` - End any session (Admin+)
- `END_USER_SESSIONS` - End user sessions (Manager+)
- `EXTEND_SESSION` - Extend session duration
- `READ_SESSION_STATISTICS` - View session analytics
- `CLEANUP_SESSIONS` - Clean expired sessions (Admin+)

#### **Sample Endpoints:**
```http
GET /api/EnhancedSessions - Get all sessions (role-filtered)
GET /api/EnhancedSessions/{id} - Get specific session
GET /api/EnhancedSessions/by-email/{email} - Get sessions by user
POST /api/EnhancedSessions - Create new session (login)
PATCH /api/EnhancedSessions/{id}/end - End session (logout)
PATCH /api/EnhancedSessions/end-all/{email} - End all user sessions
PATCH /api/EnhancedSessions/{id}/extend - Extend session
GET /api/EnhancedSessions/statistics - Session analytics
POST /api/EnhancedSessions/cleanup-expired - Cleanup expired sessions
```

---

### **3. EnhancedLogsController** 
**Path**: `http://localhost:4000/api/EnhancedLogs`

#### **🔧 Key Features:**
- ✅ **Advanced Log Filtering** - Multiple filter criteria
- ✅ **Log Search** - Full-text search capabilities
- ✅ **Log Analytics** - Error rates, trends, statistics
- ✅ **Log Export** - CSV export functionality
- ✅ **Retention Management** - Automatic log cleanup
- ✅ **System & User Logs** - Support for both types

#### **📊 Log Levels Supported:**
```
Trace → Debug → Info/Information → Warning → Error → Critical → Fatal
```

#### **🛡️ Permission Requirements:**
- `READ_ALL_LOGS` - View all logs (Admin+)
- `READ_LOG` - View individual log entries
- `READ_USER_LOGS` - View logs for managed users (Manager+)
- `CREATE_LOG` - Create log entries (System/Admin)
- `DELETE_LOG` - Delete log entries (Admin only)
- `SEARCH_LOGS` - Advanced log search (Support+)
- `EXPORT_LOGS` - Export logs (Manager+)
- `READ_LOG_STATISTICS` - View log analytics
- `CLEANUP_LOGS` - Clean old logs (Admin only)

#### **Sample Endpoints:**
```http
GET /api/EnhancedLogs - Get all logs (role-filtered)
GET /api/EnhancedLogs/{id} - Get specific log entry
GET /api/EnhancedLogs/by-email/{email} - Get logs by user
POST /api/EnhancedLogs - Create new log entry
POST /api/EnhancedLogs/system - Create system log entry
DELETE /api/EnhancedLogs/{id} - Delete log entry
GET /api/EnhancedLogs/statistics - Log analytics
POST /api/EnhancedLogs/search - Advanced log search
GET /api/EnhancedLogs/export-csv - Export logs to CSV
POST /api/EnhancedLogs/cleanup - Cleanup old logs
```

---

## 🧪 **Testing the Enhanced Controllers**

### **1. Test Enhanced Commands**

#### **Create Command:**
```bash
curl -X POST "http://localhost:4000/api/EnhancedCommands" \
  -H "Authorization: Bearer <manager_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "commandText": "systemctl restart service",
    "commandJson": "{\"service\": \"nginx\"}",
    "commandStatus": "Pending"
  }'
```

#### **Update Command Status:**
```bash
curl -X PATCH "http://localhost:4000/api/EnhancedCommands/1/status" \
  -H "Authorization: Bearer <support_token>" \
  -H "Content-Type: application/json" \
  -d '{"status": "Processing"}'
```

#### **Get Command Statistics:**
```bash
curl -X GET "http://localhost:4000/api/EnhancedCommands/statistics" \
  -H "Authorization: Bearer <manager_token>"
```

### **2. Test Enhanced Sessions**

#### **Create Session (Login):**
```bash
curl -X POST "http://localhost:4000/api/EnhancedSessions" \
  -H "Content-Type: application/json" \
  -d '{
    "userEmail": "user@example.com",
    "ipAddress": "192.168.1.100",
    "deviceInfo": "Chrome 120.0 on Windows 11"
  }'
```

#### **Extend Session:**
```bash
curl -X PATCH "http://localhost:4000/api/EnhancedSessions/1/extend" \
  -H "Authorization: Bearer <user_token>" \
  -H "Content-Type: application/json" \
  -d '{"extendedSession": true}'
```

#### **End All User Sessions:**
```bash
curl -X PATCH "http://localhost:4000/api/EnhancedSessions/end-all/user@example.com" \
  -H "Authorization: Bearer <admin_token>"
```

### **3. Test Enhanced Logs**

#### **Create Log Entry:**
```bash
curl -X POST "http://localhost:4000/api/EnhancedLogs" \
  -H "Authorization: Bearer <admin_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "userEmail": "user@example.com",
    "logLevel": "Info",
    "logMessage": "User successfully logged in",
    "logDetailsJson": "{\"ip\": \"192.168.1.100\", \"browser\": \"Chrome\"}"
  }'
```

#### **Search Logs:**
```bash
curl -X POST "http://localhost:4000/api/EnhancedLogs/search" \
  -H "Authorization: Bearer <support_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "searchTerm": "error",
    "logLevels": ["Error", "Critical"],
    "dateFrom": "2024-01-01",
    "maxResults": 100
  }'
```

#### **Export Logs:**
```bash
curl -X GET "http://localhost:4000/api/EnhancedLogs/export-csv?dateFrom=2024-01-01" \
  -H "Authorization: Bearer <manager_token>" \
  --output "logs_export.csv"
```

---

## 🔧 **JavaScript Integration Examples**

### **Commands Management:**
```javascript
class CommandsManager {
    constructor(authToken) {
        this.authToken = authToken;
        this.baseUrl = '/api/EnhancedCommands';
    }

    async createCommand(commandData) {
        const response = await fetch(this.baseUrl, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(commandData)
        });
        return response.json();
    }

    async updateCommandStatus(commandId, status) {
        const response = await fetch(`${this.baseUrl}/${commandId}/status`, {
            method: 'PATCH',
            headers: {
                'Authorization': `Bearer ${this.authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ status })
        });
        return response.json();
    }

    async getCommandStatistics() {
        const response = await fetch(`${this.baseUrl}/statistics`, {
            headers: {
                'Authorization': `Bearer ${this.authToken}`
            }
        });
        return response.json();
    }
}
```

### **Session Management:**
```javascript
class SessionManager {
    constructor(authToken) {
        this.authToken = authToken;
        this.baseUrl = '/api/EnhancedSessions';
    }

    async extendSession(sessionId, extended = false) {
        const response = await fetch(`${this.baseUrl}/${sessionId}/extend`, {
            method: 'PATCH',
            headers: {
                'Authorization': `Bearer ${this.authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ extendedSession: extended })
        });
        return response.json();
    }

    async endSession(sessionId) {
        const response = await fetch(`${this.baseUrl}/${sessionId}/end`, {
            method: 'PATCH',
            headers: {
                'Authorization': `Bearer ${this.authToken}`
            }
        });
        return response.json();
    }

    async getSessionStatistics() {
        const response = await fetch(`${this.baseUrl}/statistics`, {
            headers: {
                'Authorization': `Bearer ${this.authToken}`
            }
        });
        return response.json();
    }
}
```

### **Logs Management:**
```javascript
class LogsManager {
    constructor(authToken) {
        this.authToken = authToken;
        this.baseUrl = '/api/EnhancedLogs';
    }

    async searchLogs(searchCriteria) {
        const response = await fetch(`${this.baseUrl}/search`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(searchCriteria)
        });
        return response.json();
    }

    async exportLogs(filters) {
        const params = new URLSearchParams(filters);
        const response = await fetch(`${this.baseUrl}/export-csv?${params}`, {
            headers: {
                'Authorization': `Bearer ${this.authToken}`
            }
        });
        const blob = await response.blob();
        
        // Download file
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `logs_export_${new Date().toISOString().slice(0,10)}.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
    }

    async createLog(logData) {
        const response = await fetch(this.baseUrl, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(logData)
        });
        return response.json();
    }
}
```

---

## 📊 **Session Expiration Configuration**

### **Default Settings:**
```csharp
// In EnhancedSessionsController
private readonly TimeSpan DefaultSessionTimeout = TimeSpan.FromHours(24); // 24 hours
private readonly TimeSpan ExtendedSessionTimeout = TimeSpan.FromDays(7);  // 7 days for remember me
```

### **Session Status Flow:**
```
active → expired (auto-cleanup)
active → closed (manual logout)
```

### **Expiration Logic:**
1. **Automatic Detection**: Sessions check expiration on access
2. **Background Cleanup**: Manual cleanup endpoint for admins
3. **Extension Support**: Users can extend active sessions
4. **Time Tracking**: Real-time remaining time calculation

---

## 🛡️ **Security Features**

### **Commands Security:**
- ✅ **Role-based Access**: Different permissions for different roles
- ✅ **Command Validation**: Input sanitization and validation
- ✅ **Status Tracking**: Audit trail for all command operations
- ✅ **Bulk Operation Control**: Admin-only bulk operations

### **Sessions Security:**
- ✅ **IP Tracking**: Track sessions by IP address
- ✅ **Device Fingerprinting**: Device information tracking
- ✅ **Automatic Expiration**: Configurable session timeouts
- ✅ **Force Logout**: Admins can end any session
- ✅ **Session Analytics**: Monitor suspicious activity

### **Logs Security:**
- ✅ **Log Isolation**: Users see only relevant logs
- ✅ **Retention Policies**: Automatic cleanup of old logs
- ✅ **Export Control**: Controlled log export permissions
- ✅ **Search Limitations**: Role-based search restrictions
- ✅ **Audit Trail**: All log operations are tracked

---

## 📈 **Performance Optimizations**

### **Database Queries:**
- ✅ **Indexed Searches**: Optimized database queries
- ✅ **Pagination Support**: Large dataset handling
- ✅ **Efficient Joins**: Optimized table relationships
- ✅ **Query Filtering**: Role-based data filtering at DB level

### **Memory Management:**
- ✅ **Stream Processing**: Large export operations
- ✅ **Connection Pooling**: Efficient database connections
- ✅ **Async Operations**: Non-blocking operations
- ✅ **Resource Cleanup**: Proper disposal of resources

---

## 🎯 **Role-Permission Matrix**

| **Role** | **Commands** | **Sessions** | **Logs** |
|----------|-------------|-------------|----------|
| **SuperAdmin** | Full access + bulk ops | All sessions + cleanup | All logs + cleanup |
| **Admin** | Create, update, delete | All sessions management | All logs + export |
| **Manager** | View, create, status update | User sessions management | User logs + search |
| **Support** | View, status update | Basic session viewing | User logs + search |
| **User** | None | Own sessions only | Own logs only |

---

## 📋 **Summary**

Your Enhanced Controllers are now fully implemented with:

### **✅ Enhanced Commands Controller:**
- Complete command lifecycle management
- Role-based access control  
- Bulk operations support
- Real-time status tracking
- Command analytics

### **✅ Enhanced Sessions Controller:**
- Automatic session expiration (24h default, 7d extended)
- Session extension functionality
- Multi-device session management
- IP and device tracking
- Session analytics and cleanup

### **✅ Enhanced Logs Controller:**
- Advanced log filtering and search
- Export functionality (CSV)
- Log retention management
- Error rate analytics
- System and user log support

### **🚀 Ready for Production:**
- **Security**: Comprehensive role-based access control
- **Performance**: Optimized queries and pagination
- **Monitoring**: Real-time analytics and statistics
- **Maintenance**: Automated cleanup and retention policies

Your BitRaser API now has **enterprise-grade logging, session management, and command execution capabilities**! 🎊🚀