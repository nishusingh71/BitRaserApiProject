# 📧 Email-Based Operations - Quick Reference

## 🎯 **At a Glance**

All your Enhanced API controllers support **BOTH** ID-based and Email-based operations!

---

## 📊 **Comparison Table**

| Operation | ID-Based (Old) | Email-Based (New) | Status |
|-----------|----------------|-------------------|---------|
| **Get User** | `GET /api/Users/{id}` | `GET /api/EnhancedUsers/{email}` | ✅ |
| **Update User** | `PUT /api/Users/{id}` | `PUT /api/EnhancedUsers/{email}` | ✅ |
| **Delete User** | `DELETE /api/Users/{id}` | `DELETE /api/EnhancedUsers/{email}` | ✅ |
| **Change Password** | `PATCH /api/Users/{id}/password` | `PATCH /api/EnhancedUsers/{email}/change-password` | ✅ |
| **Get User Machines** | `GET /api/Machines?userId={id}` | `GET /api/EnhancedMachines/by-email/{email}` | ✅ |
| **Get User Sessions** | `GET /api/Sessions?userId={id}` | `GET /api/EnhancedSessions/by-email/{email}` | ✅ |
| **Get User Reports** | `GET /api/Reports?userId={id}` | `GET /api/EnhancedAuditReports/by-email/{email}` | ✅ |
| **Get User Logs** | `GET /api/Logs?userId={id}` | `GET /api/EnhancedLogs/by-email/{email}` | ✅ |
| **Get User Statistics** | `GET /api/Users/{id}/stats` | `GET /api/EnhancedUsers/{email}/statistics` | ✅ |

---

## 🚀 **Common Email-Based Patterns**

### **Pattern 1: Get By Email (Primary Endpoint)**
```http
GET /api/EnhancedUsers/user@example.com
GET /api/EnhancedSessions/by-email/user@example.com
GET /api/EnhancedMachines/by-email/user@example.com
GET /api/EnhancedAuditReports/by-email/client@example.com
GET /api/EnhancedLogs/by-email/user@example.com
```

### **Pattern 2: Filter By Email (Query Parameter)**
```http
GET /api/EnhancedUsers?UserEmail=user@example.com
GET /api/EnhancedSessions?UserEmail=user@example.com
GET /api/EnhancedMachines?UserEmail=user@example.com
GET /api/EnhancedAuditReports?ClientEmail=client@example.com
GET /api/EnhancedLogs?UserEmail=user@example.com
```

### **Pattern 3: Statistics By Email**
```http
GET /api/EnhancedUsers/{email}/statistics
GET /api/EnhancedMachines/statistics/{email}
GET /api/EnhancedSessions/statistics?userEmail={email}
GET /api/EnhancedAuditReports/statistics?clientEmail={email}
GET /api/EnhancedLogs/statistics?userEmail={email}
```

### **Pattern 4: Operations By Email**
```http
PUT /api/EnhancedUsers/{email}
PATCH /api/EnhancedUsers/{email}/change-password
PATCH /api/EnhancedUsers/{email}/update-license
DELETE /api/EnhancedUsers/{email}
POST /api/EnhancedUsers/{email}/assign-role
```

---

## 📝 **Controller-Specific Quick Reference**

### **EnhancedUsersController**
```http
GET    /api/EnhancedUsers/{email}  # Get user by email
PUT    /api/EnhancedUsers/{email}  # Update user by email
DELETE /api/EnhancedUsers/{email}             # Delete user by email
PATCH  /api/EnhancedUsers/{email}/change-password      # Change password
PATCH  /api/EnhancedUsers/{email}/update-license     # Update license
PATCH  /api/EnhancedUsers/{email}/update-payment       # Update payment
POST   /api/EnhancedUsers/{email}/assign-role          # Assign role
DELETE /api/EnhancedUsers/{email}/remove-role/{role}   # Remove role
GET    /api/EnhancedUsers/{email}/statistics    # User statistics
```

### **EnhancedMachinesController**
```http
GET    /api/EnhancedMachines/by-email/{email}         # Get machines by email
GET    /api/EnhancedMachines/by-mac/{macAddress}            # Get machine by MAC
POST   /api/EnhancedMachines/register/{email}     # Register machine
PUT /api/EnhancedMachines/by-mac/{macAddress}  # Update machine
PATCH  /api/EnhancedMachines/by-mac/{mac}/activate-license # Activate license
DELETE /api/EnhancedMachines/by-mac/{macAddress}            # Delete machine
GET    /api/EnhancedMachines/statistics/{email}             # Machine statistics
```

### **EnhancedSessionsController**
```http
GET   /api/EnhancedSessions/by-email/{email} # Get sessions by email
POST  /api/EnhancedSessions            # Create session (login)
PATCH /api/EnhancedSessions/{id}/end        # End session (logout)
PATCH /api/EnhancedSessions/end-all/{email}   # End all user sessions
PATCH /api/EnhancedSessions/{id}/extend   # Extend session
GET   /api/EnhancedSessions/statistics            # Session statistics
```

### **EnhancedAuditReportsController**
```http
GET   /api/EnhancedAuditReports/by-email/{email}  # Get reports by email
POST  /api/EnhancedAuditReports        # Create report
PUT   /api/EnhancedAuditReports/{id}          # Update report
DELETE /api/EnhancedAuditReports/{id}  # Delete report
GET   /api/EnhancedAuditReports/statistics         # Report statistics
GET   /api/EnhancedAuditReports/export-csv    # Export to CSV
GET   /api/EnhancedAuditReports/export-pdf          # Export to PDF
```

### **EnhancedLogsController**
```http
GET  /api/EnhancedLogs/by-email/{email}   # Get logs by email
POST /api/EnhancedLogs            # Create log
POST /api/EnhancedLogs/search           # Search logs
GET  /api/EnhancedLogs/statistics       # Log statistics
GET  /api/EnhancedLogs/export-csv      # Export logs to CSV
```

### **EnhancedCommandsController**
```http
GET   /api/EnhancedCommands/by-email/{email}    # Get commands by email
POST  /api/EnhancedCommands       # Create command
PATCH /api/EnhancedCommands/{id}/status          # Update command status
GET   /api/EnhancedCommands/statistics           # Command statistics
```

### **EnhancedProfileController** (JWT-based)
```http
GET   /api/EnhancedProfile/profile      # Get own profile (from JWT)
PUT   /api/EnhancedProfile/profile       # Update own profile
PATCH /api/EnhancedProfile/change-password       # Change own password
GET   /api/EnhancedProfile/statistics# Own statistics
```

---

## 💻 **JavaScript Usage Examples**

### **Example 1: User Management**
```javascript
const email = 'user@example.com';
const token = 'your-jwt-token';

// Get user
const user = await fetch(`/api/EnhancedUsers/${email}`, {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());

// Update user
await fetch(`/api/EnhancedUsers/${email}`, {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    UserEmail: email,
    UserName: 'New Name'
  })
});

// Change password
await fetch(`/api/EnhancedUsers/${email}/change-password`, {
  method: 'PATCH',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    CurrentPassword: 'old',
    NewPassword: 'new'
  })
});

// Get statistics
const stats = await fetch(`/api/EnhancedUsers/${email}/statistics`, {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());
```

### **Example 2: Machine Management**
```javascript
const email = 'user@example.com';
const macAddress = 'AA:BB:CC:DD:EE:FF';
const token = 'your-jwt-token';

// Get all machines for user
const machines = await fetch(`/api/EnhancedMachines/by-email/${email}`, {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());

// Get specific machine by MAC
const machine = await fetch(`/api/EnhancedMachines/by-mac/${macAddress}`, {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());

// Register new machine
await fetch(`/api/EnhancedMachines/register/${email}`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    MacAddress: 'BB:CC:DD:EE:FF:AA',
    FingerprintHash: 'hash123',
    OsVersion: 'Windows 11'
  })
});

// Activate license
await fetch(`/api/EnhancedMachines/by-mac/${macAddress}/activate-license`, {
  method: 'PATCH',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
body: JSON.stringify({
    DaysValid: 365
  })
});
```

### **Example 3: Session Management**
```javascript
const email = 'user@example.com';
const token = 'your-jwt-token';

// Get all sessions
const sessions = await fetch(`/api/EnhancedSessions/by-email/${email}`, {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());

// End all sessions
await fetch(`/api/EnhancedSessions/end-all/${email}`, {
  method: 'PATCH',
  headers: { 'Authorization': `Bearer ${token}` }
});

// Get session statistics
const stats = await fetch(`/api/EnhancedSessions/statistics?userEmail=${email}`, {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());
```

### **Example 4: Reports Management**
```javascript
const email = 'client@example.com';
const token = 'your-jwt-token';

// Get all reports
const reports = await fetch(`/api/EnhancedAuditReports/by-email/${email}`, {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());

// Create report
await fetch(`/api/EnhancedAuditReports`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    ClientEmail: email,
    ReportName: 'Report 2024',
    ErasureMethod: 'DoD 5220.22-M'
  })
});

// Export reports to CSV
const csvBlob = await fetch(
  `/api/EnhancedAuditReports/export-csv?ClientEmail=${email}`,
  {
    headers: { 'Authorization': `Bearer ${token}` }
  }
).then(r => r.blob());

// Download CSV
const url = URL.createObjectURL(csvBlob);
const a = document.createElement('a');
a.href = url;
a.download = 'reports.csv';
a.click();
```

---

## 🎨 **React Hook Example**

```javascript
import { useState, useEffect } from 'react';

// Custom hook for email-based API calls
function useUserData(email, token) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
  if (!email || !token) return;

    fetch(`/api/EnhancedUsers/${email}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    })
      .then(r => r.json())
      .then(data => {
      setUser(data);
      setLoading(false);
  })
      .catch(err => {
        setError(err.message);
        setLoading(false);
      });
  }, [email, token]);

  const updateUser = async (updates) => {
    const response = await fetch(`/api/EnhancedUsers/${email}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ UserEmail: email, ...updates })
    });
    
    if (response.ok) {
      const updated = await response.json();
      setUser(updated);
    }
  };

  const changePassword = async (currentPassword, newPassword) => {
    const response = await fetch(`/api/EnhancedUsers/${email}/change-password`, {
      method: 'PATCH',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
  body: JSON.stringify({
        CurrentPassword: currentPassword,
        NewPassword: newPassword
      })
    });
    
    return response.ok;
  };

  return { user, loading, error, updateUser, changePassword };
}

// Usage in component
function UserProfile() {
  const email = 'user@example.com'; // From login/JWT
  const token = localStorage.getItem('authToken');
  const { user, loading, updateUser, changePassword } = useUserData(email, token);

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      <h1>{user.userName}</h1>
      <p>{user.userEmail}</p>
      <button onClick={() => updateUser({ UserName: 'New Name' })}>
        Update Name
      </button>
      <button onClick={() => changePassword('old', 'new')}>
        Change Password
      </button>
    </div>
  );
}
```

---

## ⚡ **Performance Tips**

### **1. Use Query Parameters for Filtering**
```javascript
// Instead of fetching all and filtering client-side
const allUsers = await fetch('/api/EnhancedUsers');
const filtered = allUsers.filter(u => u.email.includes('@example.com'));

// Do this - filter server-side
const filtered = await fetch('/api/EnhancedUsers?UserEmail=@example.com');
```

### **2. Cache Email-Based Responses**
```javascript
const cache = new Map();

async function getUserByEmail(email, token) {
  if (cache.has(email)) {
    return cache.get(email);
  }

  const user = await fetch(`/api/EnhancedUsers/${email}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  }).then(r => r.json());

  cache.set(email, user);
  return user;
}
```

### **3. Batch Email-Based Requests**
```javascript
// Instead of sequential requests
for (const email of emails) {
  await fetch(`/api/EnhancedUsers/${email}`);
}

// Do this - parallel requests
await Promise.all(
  emails.map(email =>
    fetch(`/api/EnhancedUsers/${email}`)
  )
);
```

---

## ✅ **Summary**

**All your APIs support email-based operations!** 🎉

No additional implementation needed. Your Enhanced controllers are already:
- ✅ Email-based friendly
- ✅ Secure by design
- ✅ Frontend optimized
- ✅ Performance efficient
- ✅ Self-documenting

---

**Happy Coding! 🚀**
