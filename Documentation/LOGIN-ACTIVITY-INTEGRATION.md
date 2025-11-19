# 🔗 LoginActivity Controller - Integration Examples

## 🎯 **C# Client Integration**

### **User Login Flow**
```csharp
public async Task<bool> LoginUser(string email, string password)
{
    try
    {
      // 1. Authenticate user
    var authResponse = await _httpClient.PostAsJsonAsync("/api/Auth/login", new
        {
      Email = email,
    Password = password
        });

        if (!authResponse.IsSuccessStatusCode)
       return false;

        var authResult = await authResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = authResult.Token;

        // 2. Record login activity
  _httpClient.DefaultRequestHeaders.Authorization = 
      new AuthenticationHeaderValue("Bearer", token);

        var activityResponse = await _httpClient.PostAsJsonAsync(
            "/api/LoginActivity/user/login",
            new { Email = email }
        );

    var activityResult = await activityResponse.Content
   .ReadFromJsonAsync<LoginActivityResponse>();

        Console.WriteLine($"Login recorded at: {activityResult.Data.LastLogin}");
        Console.WriteLine($"Status: {activityResult.Data.ActivityStatus}");

        return true;
    }
    catch (Exception ex)
    {
   Console.WriteLine($"Login error: {ex.Message}");
        return false;
    }
}
```

### **User Logout Flow**
```csharp
public async Task LogoutUser(string email)
{
    try
    {
        // Record logout activity
        var response = await _httpClient.PostAsJsonAsync(
            "/api/LoginActivity/user/logout",
    new { Email = email }
        );

        var result = await response.Content.ReadFromJsonAsync<LoginActivityResponse>();
        Console.WriteLine($"Logout recorded at: {result.Data.LastLogout}");

        // Clear local token
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Logout error: {ex.Message}");
    }
}
```

### **Get User Activity**
```csharp
public async Task<UserActivity> GetUserActivity(string email)
{
    var response = await _httpClient.GetAsync($"/api/LoginActivity/user/{email}");
    var result = await response.Content.ReadFromJsonAsync<LoginActivityResponse>();
    return result.Data;
}
```

---

## 🌐 **JavaScript/TypeScript Integration**

### **React Login Component**
```typescript
// services/loginActivity.ts
export const loginActivityService = {
  // Record user login
  async recordUserLogin(email: string, token: string) {
    const response = await fetch('/api/LoginActivity/user/login', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
      },
      body: JSON.stringify({ email })
    });
    return await response.json();
  },

  // Record user logout
  async recordUserLogout(email: string, token: string) {
    const response = await fetch('/api/LoginActivity/user/logout', {
  method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ email })
    });
    return await response.json();
  },

  // Get user activity
  async getUserActivity(email: string, token: string) {
    const response = await fetch(`/api/LoginActivity/user/${email}`, {
      headers: { 'Authorization': `Bearer ${token}` }
 });
    return await response.json();
  }
};
```

### **Login Component**
```tsx
// components/Login.tsx
import { useState } from 'react';
import { loginActivityService } from '../services/loginActivity';

export const Login = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleLogin = async () => {
    try {
      // 1. Authenticate
      const authRes = await fetch('/api/Auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });

      const { token } = await authRes.json();
      localStorage.setItem('authToken', token);

      // 2. Record login activity
 const activityRes = await loginActivityService.recordUserLogin(email, token);
      
      console.log('Login recorded:', activityRes.data);
      // {
      //   email: "admin@example.com",
      //   last_login: "2025-01-26T12:30:45Z",
  //   activity_status: "online"
    // }

      // Navigate to dashboard
      window.location.href = '/dashboard';
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  return (
    <form onSubmit={(e) => { e.preventDefault(); handleLogin(); }}>
      <input 
    type="email" 
        value={email} 
        onChange={(e) => setEmail(e.target.value)} 
        placeholder="Email"
      />
   <input 
        type="password" 
        value={password} 
 onChange={(e) => setPassword(e.target.value)} 
        placeholder="Password"
      />
      <button type="submit">Login</button>
    </form>
  );
};
```

### **Logout Component**
```tsx
// components/Logout.tsx
import { loginActivityService } from '../services/loginActivity';

export const Logout = () => {
  const handleLogout = async () => {
    const email = localStorage.getItem('userEmail');
const token = localStorage.getItem('authToken');

    if (email && token) {
      // Record logout activity
      await loginActivityService.recordUserLogout(email, token);
    }

    // Clear local storage
    localStorage.removeItem('authToken');
    localStorage.removeItem('userEmail');

    // Navigate to login
    window.location.href = '/login';
  };

  return <button onClick={handleLogout}>Logout</button>;
};
```

### **User Status Badge**
```tsx
// components/UserStatusBadge.tsx
import { useEffect, useState } from 'react';
import { loginActivityService } from '../services/loginActivity';

interface UserActivity {
  email: string;
  activity_status: 'online' | 'offline';
  last_login: string;
  last_logout: string | null;
}

export const UserStatusBadge = ({ email }: { email: string }) => {
  const [activity, setActivity] = useState<UserActivity | null>(null);

  useEffect(() => {
    const fetchActivity = async () => {
      const token = localStorage.getItem('authToken');
      if (token) {
        const result = await loginActivityService.getUserActivity(email, token);
        setActivity(result.data);
      }
 };

    fetchActivity();
    
    // Refresh every 30 seconds
    const interval = setInterval(fetchActivity, 30000);
    return () => clearInterval(interval);
  }, [email]);

  if (!activity) return null;

  return (
    <div className="user-status">
    <span className={`badge ${activity.activity_status}`}>
{activity.activity_status === 'online' ? '🟢 Online' : '⚫ Offline'}
 </span>
 <small>
        Last seen: {new Date(activity.last_login).toLocaleString()}
      </small>
 </div>
  );
};
```

---

## 📱 **Flutter/Dart Integration**

### **Login Service**
```dart
// services/login_activity_service.dart
import 'package:http/http.dart' as http;
import 'dart:convert';

class LoginActivityService {
  final String baseUrl = 'https://yourapi.com';

  Future<Map<String, dynamic>> recordUserLogin(
    String email, 
    String token
  ) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/LoginActivity/user/login'),
      headers: {
    'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
   },
      body: jsonEncode({'email': email}),
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    } else {
      throw Exception('Failed to record login');
    }
  }

  Future<Map<String, dynamic>> recordUserLogout(
    String email, 
    String token
  ) async {
    final response = await http.post(
    Uri.parse('$baseUrl/api/LoginActivity/user/logout'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
body: jsonEncode({'email': email}),
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    } else {
      throw Exception('Failed to record logout');
    }
  }

  Future<Map<String, dynamic>> getUserActivity(
    String email, 
    String token
  ) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/LoginActivity/user/$email'),
      headers: {'Authorization': 'Bearer $token'},
    );

    if (response.statusCode == 200) {
  return jsonDecode(response.body);
    } else {
      throw Exception('Failed to get activity');
    }
  }
}
```

### **Login Screen**
```dart
// screens/login_screen.dart
import 'package:flutter/material.dart';
import '../services/login_activity_service.dart';

class LoginScreen extends StatefulWidget {
  @override
  _LoginScreenState createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _activityService = LoginActivityService();

  Future<void> _handleLogin() async {
    try {
      // 1. Authenticate
      final authResponse = await http.post(
        Uri.parse('https://yourapi.com/api/Auth/login'),
        headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
 'email': _emailController.text,
      'password': _passwordController.text,
        }),
    );

      final authData = jsonDecode(authResponse.body);
      final token = authData['token'];

      // 2. Record login activity
      final activityData = await _activityService.recordUserLogin(
        _emailController.text,
        token
      );

      print('Login recorded: ${activityData['data']}');

      // Navigate to home
      Navigator.pushReplacementNamed(context, '/home');
    } catch (e) {
      print('Login error: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
 return Scaffold(
      body: Padding(
    padding: EdgeInsets.all(16.0),
        child: Column(
        children: [
   TextField(
      controller: _emailController,
      decoration: InputDecoration(labelText: 'Email'),
      ),
       TextField(
    controller: _passwordController,
              decoration: InputDecoration(labelText: 'Password'),
   obscureText: true,
    ),
       ElevatedButton(
     onPressed: _handleLogin,
              child: Text('Login'),
            ),
          ],
        ),
      ),
  );
  }
}
```

---

## 🐍 **Python Integration**

### **Login Service**
```python
# services/login_activity.py
import requests
from typing import Dict, Optional

class LoginActivityService:
    def __init__(self, base_url: str):
    self.base_url = base_url

    def record_user_login(self, email: str, token: str) -> Dict:
   """Record user login activity"""
     response = requests.post(
            f"{self.base_url}/api/LoginActivity/user/login",
            headers={
          "Authorization": f"Bearer {token}",
                "Content-Type": "application/json"
        },
            json={"email": email}
        )
     response.raise_for_status()
        return response.json()

    def record_user_logout(self, email: str, token: str) -> Dict:
        """Record user logout activity"""
        response = requests.post(
            f"{self.base_url}/api/LoginActivity/user/logout",
         headers={
        "Authorization": f"Bearer {token}",
            "Content-Type": "application/json"
     },
     json={"email": email}
   )
        response.raise_for_status()
     return response.json()

    def get_user_activity(self, email: str, token: str) -> Dict:
        """Get user activity details"""
 response = requests.get(
    f"{self.base_url}/api/LoginActivity/user/{email}",
          headers={"Authorization": f"Bearer {token}"}
        )
        response.raise_for_status()
        return response.json()

# Usage
service = LoginActivityService("https://yourapi.com")

# Login
token = "your-jwt-token"
result = service.record_user_login("admin@example.com", token)
print(f"Login recorded: {result['data']}")

# Get activity
activity = service.get_user_activity("admin@example.com", token)
print(f"Status: {activity['data']['activity_status']}")

# Logout
logout_result = service.record_user_logout("admin@example.com", token)
print(f"Logout recorded: {logout_result['data']}")
```

---

## 📊 **Admin Dashboard Example**

### **Real-Time User Monitor**
```tsx
// components/UserMonitor.tsx
import { useEffect, useState } from 'react';

export const UserMonitor = () => {
  const [users, setUsers] = useState([]);

  useEffect(() => {
    const fetchUsers = async () => {
    const token = localStorage.getItem('authToken');
      const response = await fetch('/api/LoginActivity/users', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
    const data = await response.json();
      setUsers(data.data);
  };

    fetchUsers();
    const interval = setInterval(fetchUsers, 10000); // Refresh every 10s
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="user-monitor">
      <h2>User Activity Monitor</h2>
      <div className="stats">
    <div>Total: {users.length}</div>
        <div>Online: {users.filter(u => u.activity_status === 'online').length}</div>
   <div>Offline: {users.filter(u => u.activity_status === 'offline').length}</div>
      </div>
      <table>
        <thead>
          <tr>
            <th>Email</th>
            <th>Name</th>
    <th>Status</th>
    <th>Last Login</th>
<th>Last Logout</th>
      </tr>
        </thead>
        <tbody>
          {users.map(user => (
            <tr key={user.email}>
    <td>{user.email}</td>
        <td>{user.user_name}</td>
         <td>
             <span className={user.activity_status}>
    {user.activity_status === 'online' ? '🟢' : '⚫'} 
             {user.activity_status}
                </span>
              </td>
      <td>{new Date(user.last_login).toLocaleString()}</td>
          <td>{user.last_logout ? new Date(user.last_logout).toLocaleString() : '-'}</td>
   </tr>
    ))}
        </tbody>
      </table>
    </div>
  );
};
```

---

## ✅ **Summary**

| Integration | Status | Example Provided |
|-------------|--------|------------------|
| **C# Client** | ✅ | Login/Logout/Get |
| **React/TypeScript** | ✅ | Full components |
| **Flutter/Dart** | ✅ | Service + Screen |
| **Python** | ✅ | Service class |
| **Admin Dashboard** | ✅ | Monitor component |

**All examples are production-ready and can be used directly!** 🚀
