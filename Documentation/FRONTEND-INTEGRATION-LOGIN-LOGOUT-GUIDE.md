# 🎯 Frontend Integration Guide - Login/Logout Timestamps

## ✅ **Login Response mein Available Fields**

RoleBasedAuth Login API ab ye fields return karta hai:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userType": "user",
  "email": "admin@example.com",
  "roles": ["SuperAdmin"],
  "permissions": ["UserManagement", "ReportAccess", ...],
  "expiresAt": "2025-11-24T13:07:11.3895396Z",
  
  "userName": "Admin User",
  "userRole": "SuperAdmin",
  "userGroup": "Management",
  "department": "IT",
  "phone": "+1234567890",
  "timezone": "Asia/Kolkata",
  
  "loginTime": "2025-11-24T05:07:11.3895396Z",      // ✅ Current login
  "lastLogoutTime": "2025-11-23T18:30:00.0000000Z", // ✅ Previous logout
  
  "parentUserEmail": null,
  "userId": 1
}
```

---

## 🚀 **React/JavaScript Integration**

### **1. Login Function with Timestamp Handling**

```javascript
// services/authService.js

export const login = async (email, password) => {
  try {
    const response = await fetch('http://localhost:4000/api/RoleBasedAuth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password })
    });

    if (!response.ok) {
      throw new Error('Login failed');
    }

    const data = await response.json();

    // ✅ Store in localStorage/sessionStorage
    localStorage.setItem('authToken', data.token);
    localStorage.setItem('userEmail', data.email);
    localStorage.setItem('userType', data.userType);
    localStorage.setItem('userName', data.userName);
    
    // ✅ Store login/logout timestamps
    localStorage.setItem('loginTime', data.loginTime);
    localStorage.setItem('lastLogoutTime', data.lastLogoutTime || 'Never');
    localStorage.setItem('expiresAt', data.expiresAt);
    
    // ✅ Store user details
    localStorage.setItem('roles', JSON.stringify(data.roles));
    localStorage.setItem('permissions', JSON.stringify(data.permissions));
    localStorage.setItem('department', data.department || '');
    localStorage.setItem('timezone', data.timezone || 'UTC');

    return {
      success: true,
      user: data
    };
  } catch (error) {
  console.error('Login error:', error);
    return {
  success: false,
      error: error.message
    };
  }
};
```

---

### **2. Display Login/Logout Information in Dashboard**

```javascript
// components/UserDashboard.jsx

import React, { useState, useEffect } from 'react';

const UserDashboard = () => {
  const [userInfo, setUserInfo] = useState({
    userName: '',
    email: '',
    userType: '',
    loginTime: '',
    lastLogoutTime: '',
    department: '',
    roles: []
  });

  useEffect(() => {
    // ✅ Load user info from localStorage
    const userName = localStorage.getItem('userName');
    const email = localStorage.getItem('userEmail');
    const userType = localStorage.getItem('userType');
    const loginTime = localStorage.getItem('loginTime');
    const lastLogoutTime = localStorage.getItem('lastLogoutTime');
    const department = localStorage.getItem('department');
    const roles = JSON.parse(localStorage.getItem('roles') || '[]');

    setUserInfo({
      userName,
      email,
      userType,
      loginTime,
   lastLogoutTime,
      department,
      roles
    });
  }, []);

  // ✅ Format datetime for display
  const formatDateTime = (isoString) => {
    if (!isoString || isoString === 'Never') return 'Never';
    
    const date = new Date(isoString);
    return new Intl.DateTimeFormat('en-IN', {
    dateStyle: 'medium',
      timeStyle: 'short',
      timeZone: localStorage.getItem('timezone') || 'UTC'
    }).format(date);
  };

  // ✅ Calculate session duration
  const getSessionDuration = () => {
    const loginTime = localStorage.getItem('loginTime');
    if (!loginTime) return 'N/A';

    const now = new Date();
    const login = new Date(loginTime);
    const diffMs = now - login;
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 60) {
      return `${diffMins} minutes`;
    } else {
      const hours = Math.floor(diffMins / 60);
    const mins = diffMins % 60;
      return `${hours}h ${mins}m`;
    }
  };

  return (
    <div className="dashboard">
      <div className="user-profile-card">
        <h2>Welcome, {userInfo.userName}!</h2>
        
        <div className="user-details">
          <div className="detail-row">
     <span className="label">Email:</span>
 <span className="value">{userInfo.email}</span>
          </div>
          
 <div className="detail-row">
    <span className="label">User Type:</span>
            <span className="value badge">{userInfo.userType}</span>
 </div>
          
 <div className="detail-row">
         <span className="label">Department:</span>
   <span className="value">{userInfo.department || 'N/A'}</span>
          </div>
      
     <div className="detail-row">
      <span className="label">Roles:</span>
            <span className="value">
       {userInfo.roles.map(role => (
       <span key={role} className="role-badge">{role}</span>
   ))}
     </span>
          </div>
        </div>

        {/* ✅ Login/Logout Information */}
      <div className="session-info">
          <h3>Session Information</h3>
          
 <div className="detail-row">
            <span className="label">Current Login:</span>
            <span className="value timestamp">
   {formatDateTime(userInfo.loginTime)}
 </span>
    </div>
  
        <div className="detail-row">
          <span className="label">Previous Logout:</span>
       <span className="value timestamp">
              {formatDateTime(userInfo.lastLogoutTime)}
            </span>
          </div>
          
       <div className="detail-row">
            <span className="label">Session Duration:</span>
    <span className="value">{getSessionDuration()}</span>
   </div>
     </div>
      </div>
    </div>
  );
};

export default UserDashboard;
```

---

### **3. CSS Styling for Dashboard**

```css
/* styles/dashboard.css */

.dashboard {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.user-profile-card {
  background: white;
  border-radius: 8px;
  padding: 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  margin-bottom: 20px;
}

.user-profile-card h2 {
  color: #333;
  margin-bottom: 20px;
  border-bottom: 2px solid #4CAF50;
  padding-bottom: 10px;
}

.user-details, .session-info {
  margin-top: 20px;
}

.session-info h3 {
  color: #555;
  font-size: 18px;
  margin-bottom: 15px;
  display: flex;
  align-items: center;
}

.session-info h3::before {
  content: "🕐";
  margin-right: 8px;
  font-size: 20px;
}

.detail-row {
  display: flex;
  justify-content: space-between;
  padding: 12px 0;
  border-bottom: 1px solid #eee;
}

.detail-row:last-child {
  border-bottom: none;
}

.label {
  font-weight: 600;
  color: #666;
  min-width: 180px;
}

.value {
  color: #333;
  text-align: right;
}

.timestamp {
  font-family: 'Courier New', monospace;
  background: #f5f5f5;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 14px;
  color: #2196F3;
}

.badge {
  background: #4CAF50;
  color: white;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  text-transform: uppercase;
  font-weight: bold;
}

.role-badge {
  background: #2196F3;
  color: white;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 12px;
  margin-left: 6px;
  display: inline-block;
}
```

---

### **4. Login History Component**

```javascript
// components/LoginHistory.jsx

import React, { useState, useEffect } from 'react';

const LoginHistory = () => {
  const [loginTime, setLoginTime] = useState('');
  const [lastLogoutTime, setLastLogoutTime] = useState('');
  const [sessionDuration, setSessionDuration] = useState('');

  useEffect(() => {
    loadLoginHistory();

    // Update session duration every minute
    const interval = setInterval(() => {
      updateSessionDuration();
    }, 60000); // 60 seconds

    return () => clearInterval(interval);
  }, []);

  const loadLoginHistory = () => {
    const login = localStorage.getItem('loginTime');
    const logout = localStorage.getItem('lastLogoutTime');
    
    setLoginTime(login);
    setLastLogoutTime(logout);
 updateSessionDuration();
  };

  const updateSessionDuration = () => {
    const loginTime = localStorage.getItem('loginTime');
    if (!loginTime) {
      setSessionDuration('N/A');
      return;
    }

  const now = new Date();
    const login = new Date(loginTime);
    const diffMs = now - login;
    const diffMins = Math.floor(diffMs / 60000);
 
    if (diffMins < 60) {
    setSessionDuration(`${diffMins} minutes ago`);
    } else {
      const hours = Math.floor(diffMins / 60);
const mins = diffMins % 60;
      setSessionDuration(`${hours}h ${mins}m ago`);
}
  };

  const formatDate = (isoString) => {
    if (!isoString || isoString === 'Never') {
      return 'Never logged out';
    }
    
    const date = new Date(isoString);
    const timezone = localStorage.getItem('timezone') || 'UTC';
    
    return new Intl.DateTimeFormat('en-IN', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    second: '2-digit',
      timeZone: timezone,
      timeZoneName: 'short'
    }).format(date);
};

  const getTimeDifference = () => {
    if (!loginTime || !lastLogoutTime || lastLogoutTime === 'Never') {
      return 'First login session';
    }

    const login = new Date(loginTime);
    const logout = new Date(lastLogoutTime);
    const diffMs = login - logout;
    const diffHours = Math.floor(diffMs / 3600000);
    
    if (diffHours < 24) {
      return `${diffHours} hours after last logout`;
 } else {
      const days = Math.floor(diffHours / 24);
      return `${days} day${days > 1 ? 's' : ''} after last logout`;
    }
  };

  return (
    <div className="login-history-card">
      <h3>📊 Login History</h3>
      
      <div className="history-timeline">
        {/* Current Login */}
        <div className="timeline-item current">
          <div className="timeline-marker">🟢</div>
          <div className="timeline-content">
      <div className="timeline-title">Current Session</div>
          <div className="timeline-time">{formatDate(loginTime)}</div>
            <div className="timeline-duration">{sessionDuration}</div>
          </div>
        </div>

        {/* Previous Logout */}
        <div className="timeline-item">
        <div className="timeline-marker">🔴</div>
  <div className="timeline-content">
    <div className="timeline-title">Previous Logout</div>
 <div className="timeline-time">{formatDate(lastLogoutTime)}</div>
 <div className="timeline-duration">{getTimeDifference()}</div>
          </div>
      </div>
      </div>

      <div className="history-stats">
        <div className="stat-item">
          <span className="stat-label">Status:</span>
   <span className="stat-value online">● Online</span>
        </div>
      </div>
    </div>
  );
};

export default LoginHistory;
```

---

### **5. Login History CSS**

```css
/* styles/loginHistory.css */

.login-history-card {
  background: white;
  border-radius: 8px;
  padding: 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  margin-top: 20px;
}

.login-history-card h3 {
  color: #333;
  margin-bottom: 20px;
  font-size: 20px;
}

.history-timeline {
  position: relative;
  padding-left: 40px;
}

.history-timeline::before {
  content: '';
  position: absolute;
  left: 15px;
  top: 20px;
  bottom: 20px;
  width: 2px;
  background: #ddd;
}

.timeline-item {
  position: relative;
  margin-bottom: 30px;
}

.timeline-marker {
  position: absolute;
  left: -28px;
  width: 30px;
height: 30px;
  border-radius: 50%;
  background: white;
  border: 2px solid #ddd;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
}

.timeline-item.current .timeline-marker {
  border-color: #4CAF50;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0%, 100% {
    box-shadow: 0 0 0 0 rgba(76, 175, 80, 0.7);
  }
  50% {
    box-shadow: 0 0 0 10px rgba(76, 175, 80, 0);
  }
}

.timeline-content {
  padding: 12px;
  background: #f9f9f9;
  border-radius: 6px;
}

.timeline-title {
  font-weight: 600;
  color: #333;
  margin-bottom: 4px;
}

.timeline-time {
  color: #666;
  font-size: 14px;
  margin-bottom: 4px;
}

.timeline-duration {
  color: #999;
  font-size: 12px;
  font-style: italic;
}

.history-stats {
  margin-top: 20px;
  padding-top: 20px;
  border-top: 1px solid #eee;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
}

.stat-label {
  font-weight: 600;
  color: #666;
}

.stat-value.online {
  color: #4CAF50;
  font-weight: 600;
}
```

---

## 📱 **Vue.js Integration**

### **1. Login Service (Vue)**

```javascript
// services/authService.js

export default {
  async login(email, password) {
    try {
const response = await fetch('http://localhost:4000/api/RoleBasedAuth/login', {
      method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password })
    });

      if (!response.ok) {
     throw new Error('Login failed');
      }

      const data = await response.json();

      // ✅ Store in localStorage
  localStorage.setItem('authToken', data.token);
  localStorage.setItem('loginTime', data.loginTime);
    localStorage.setItem('lastLogoutTime', data.lastLogoutTime || 'Never');
      localStorage.setItem('userInfo', JSON.stringify(data));

      return { success: true, user: data };
    } catch (error) {
      return { success: false, error: error.message };
    }
  }
};
```

### **2. Dashboard Component (Vue)**

```vue
<!-- components/UserDashboard.vue -->

<template>
  <div class="dashboard">
    <div class="user-profile-card">
      <h2>Welcome, {{ userInfo.userName }}!</h2>
      
    <div class="session-info">
        <h3>Session Information</h3>
  
        <div class="detail-row">
          <span class="label">Current Login:</span>
          <span class="value timestamp">{{ formatDateTime(userInfo.loginTime) }}</span>
        </div>
        
  <div class="detail-row">
          <span class="label">Previous Logout:</span>
          <span class="value timestamp">{{ formatDateTime(userInfo.lastLogoutTime) }}</span>
        </div>
        
     <div class="detail-row">
          <span class="label">Session Duration:</span>
          <span class="value">{{ sessionDuration }}</span>
    </div>
   </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'UserDashboard',
  data() {
    return {
      userInfo: {
        userName: '',
     loginTime: '',
        lastLogoutTime: ''
      },
      sessionDuration: ''
    };
  },
  mounted() {
    this.loadUserInfo();
    this.updateSessionDuration();
    
    // Update every minute
  setInterval(() => {
      this.updateSessionDuration();
    }, 60000);
  },
  methods: {
 loadUserInfo() {
 const storedInfo = localStorage.getItem('userInfo');
      if (storedInfo) {
        this.userInfo = JSON.parse(storedInfo);
      }
    },
    
    formatDateTime(isoString) {
      if (!isoString || isoString === 'Never') return 'Never';
      
      const date = new Date(isoString);
    return new Intl.DateTimeFormat('en-IN', {
 dateStyle: 'medium',
     timeStyle: 'short'
      }).format(date);
    },
    
    updateSessionDuration() {
      if (!this.userInfo.loginTime) {
        this.sessionDuration = 'N/A';
    return;
      }

      const now = new Date();
      const login = new Date(this.userInfo.loginTime);
      const diffMs = now - login;
      const diffMins = Math.floor(diffMs / 60000);
    
      if (diffMins < 60) {
        this.sessionDuration = `${diffMins} minutes`;
      } else {
   const hours = Math.floor(diffMins / 60);
        const mins = diffMins % 60;
    this.sessionDuration = `${hours}h ${mins}m`;
      }
    }
  }
};
</script>
```

---

## 🎯 **Angular Integration**

### **1. Auth Service (Angular)**

```typescript
// services/auth.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

interface LoginResponse {
  token: string;
  userType: string;
  email: string;
  userName: string;
  loginTime: string;
  lastLogoutTime: string;
  roles: string[];
  permissions: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:4000/api/RoleBasedAuth';

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { email, password })
      .pipe(
   tap(response => {
        // ✅ Store in localStorage
    localStorage.setItem('authToken', response.token);
          localStorage.setItem('loginTime', response.loginTime);
          localStorage.setItem('lastLogoutTime', response.lastLogoutTime || 'Never');
          localStorage.setItem('userInfo', JSON.stringify(response));
        })
      );
  }

  getLoginTime(): string {
    return localStorage.getItem('loginTime') || '';
  }

  getLastLogoutTime(): string {
    return localStorage.getItem('lastLogoutTime') || 'Never';
  }
}
```

### **2. Dashboard Component (Angular)**

```typescript
// components/dashboard/dashboard.component.ts

import { Component, OnInit, OnDestroy } from '@angular/core';
import { interval, Subscription } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  userInfo: any = {};
  sessionDuration: string = '';
private timerSubscription?: Subscription;

  ngOnInit(): void {
    this.loadUserInfo();
    this.updateSessionDuration();
    
    // Update every minute
    this.timerSubscription = interval(60000).subscribe(() => {
      this.updateSessionDuration();
    });
  }

  ngOnDestroy(): void {
    if (this.timerSubscription) {
      this.timerSubscription.unsubscribe();
    }
  }

  loadUserInfo(): void {
    const storedInfo = localStorage.getItem('userInfo');
    if (storedInfo) {
      this.userInfo = JSON.parse(storedInfo);
    }
  }

  formatDateTime(isoString: string): string {
    if (!isoString || isoString === 'Never') return 'Never';
    
    const date = new Date(isoString);
    return new Intl.DateTimeFormat('en-IN', {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(date);
  }

  updateSessionDuration(): void {
    if (!this.userInfo.loginTime) {
      this.sessionDuration = 'N/A';
      return;
    }

    const now = new Date();
    const login = new Date(this.userInfo.loginTime);
    const diffMs = now.getTime() - login.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 60) {
      this.sessionDuration = `${diffMins} minutes`;
    } else {
  const hours = Math.floor(diffMins / 60);
      const mins = diffMins % 60;
      this.sessionDuration = `${hours}h ${mins}m`;
    }
  }
}
```

---

## 📊 **Summary**

### **Available Fields:**
- ✅ `loginTime` - Current login timestamp (ISO 8601)
- ✅ `lastLogoutTime` - Previous logout timestamp (ISO 8601)
- ✅ `expiresAt` - Token expiry time
- ✅ `userName`, `email`, `userType`
- ✅ `roles`, `permissions`
- ✅ `department`, `timezone`

### **Common Operations:**
1. **Display formatted time** - Use `Intl.DateTimeFormat`
2. **Calculate session duration** - Subtract current time from loginTime
3. **Show time difference** - Calculate difference between login and last logout
4. **Auto-update duration** - Use `setInterval` to update every minute

### **Storage Strategy:**
- Store in `localStorage` for persistent sessions
- Store in `sessionStorage` for single-session only
- Use state management (Redux/Vuex/NgRx) for complex apps

---

**Frontend integration ab ready hai! Login/Logout timestamps easily display kar sakte ho! 🚀✨**
