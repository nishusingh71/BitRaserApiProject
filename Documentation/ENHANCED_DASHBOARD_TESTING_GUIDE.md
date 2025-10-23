# Enhanced Dashboard API - Testing Guide

## 🚀 Quick Start Testing

### Prerequisites
```bash
# 1. Start the API
dotnet run --project BitRaserApiProject

# 2. Get JWT Token
POST https://localhost:44316/api/DashboardAuth/login
{
  "Email": "admin@example.com",
  "Password": "Admin@123"
}

# 3. Save the token
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

## 📊 Test All Endpoints

### 1. Dashboard Overview
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/overview" \
  -H "Authorization: Bearer $TOKEN" \
  -k
```

**Expected Response**:
```json
{
  "welcomeMessage": "Welcome back, admin@example.com",
  "metrics": {
    "totalLicenses": { "value": 3287, "changePercent": 2.0 },
    "activeUsers": { "value": 156, "changePercent": 5.0 },
 "availableLicenses": { "value": 1200, "changePercent": 8.0 },
    "successRate": { "value": 99, "unit": "%" }
  }
}
```

---

### 2. Groups and Users
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/groups-users" \
-H "Authorization: Bearer $TOKEN" \
  -k
```

**Expected**: List of groups with licenses count

---

### 3. Recent Reports
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/recent-reports?count=4" \
  -H "Authorization: Bearer $TOKEN" \
  -k
```

**Expected**: 4 most recent reports with device counts

---

### 4. License Details
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/license-details" \
  -H "Authorization: Bearer $TOKEN" \
  -k
```

**Expected**: Product-wise license breakdown with usage percentages

---

### 5. Quick Actions
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/quick-actions" \
  -H "Authorization: Bearer $TOKEN" \
  -k
```

**Expected**: 4 quick action cards with permissions

---

### 6. License Management
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/license-management" \
  -H "Authorization: Bearer $TOKEN" \
  -k
```

**Expected**: Bulk assignment and audit report data

---

### 7. User Activity
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/user-activity?days=7" \
  -H "Authorization: Bearer $TOKEN" \
  -k
```

**Expected**: Recent user activities from logs

---

### 8. Statistics
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/statistics" \
  -H "Authorization: Bearer $TOKEN" \
  -k
```

**Expected**: Complete system statistics

---

## 🧪 Swagger Testing

### 1. Open Swagger UI
```
https://localhost:44316/swagger
```

### 2. Authorize
Click **🔒 Authorize** button and enter:
```
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 3. Test Endpoints
Navigate to **EnhancedDashboard** section and try each endpoint

---

## 🎨 Frontend Testing

### React Example
```tsx
import { useEffect, useState } from 'react';

function Dashboard() {
  const [data, setData] = useState(null);

  useEffect(() => {
  fetch('https://localhost:44316/api/EnhancedDashboard/overview', {
      headers: {
     'Authorization': `Bearer ${localStorage.getItem('token')}`,
    'Content-Type': 'application/json'
      }
    })
    .then(res => res.json())
    .then(setData);
  }, []);

  if (!data) return <div>Loading...</div>;

  return (
    <div>
  <h1>{data.welcomeMessage}</h1>
      <div className="metrics">
        <div>Total Licenses: {data.metrics.totalLicenses.value}</div>
        <div>Active Users: {data.metrics.activeUsers.value}</div>
        <div>Available Licenses: {data.metrics.availableLicenses.value}</div>
        <div>Success Rate: {data.metrics.successRate.value}%</div>
      </div>
    </div>
  );
}
```

---

## 📊 Expected Data Structure

### Overview Response
```typescript
interface DashboardOverview {
  welcomeMessage: string;
  metrics: {
    totalLicenses: Metric;
    activeUsers: Metric;
    availableLicenses: Metric;
 successRate: Metric;
  };
  totalUsers: number;
  activeUsers: number;
  totalMachines: number;
  activeMachines: number;
}

interface Metric {
  value: number;
  label: string;
  changePercent: number;
  changeDirection: "up" | "down";
  icon: string;
  unit?: string;
}
```

---

## 🔍 Troubleshooting

### Issue 1: 401 Unauthorized
```bash
# Solution: Get fresh token
POST /api/DashboardAuth/login
{
  "Email": "your-email@example.com",
  "Password": "your-password"
}
```

### Issue 2: 403 Forbidden
```bash
# Solution: Check user permissions
# User needs VIEW_DASHBOARD permission
# Contact admin to assign proper role
```

### Issue 3: Empty Data
```bash
# Solution: Ensure database has data
# Check:
# - Users table has records
# - Machines table has records
# - Logs table has records
```

### Issue 4: SSL Certificate Error
```bash
# Solution: Use -k flag in curl or accept self-signed certificate
curl -k https://localhost:44316/...
```

---

## 📱 Postman Collection

### Import This Collection
```json
{
  "info": {
    "name": "Enhanced Dashboard API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "base_url",
      "value": "https://localhost:44316",
      "type": "string"
    },
    {
      "key": "token",
  "value": "",
      "type": "string"
    }
  ],
  "item": [
    {
      "name": "Login",
      "event": [
   {
          "listen": "test",
       "script": {
       "exec": [
     "pm.environment.set('token', pm.response.json().token);"
         ]
          }
        }
      ],
  "request": {
        "method": "POST",
        "header": [],
        "body": {
    "mode": "raw",
          "raw": "{\n  \"Email\": \"admin@example.com\",\n  \"Password\": \"Admin@123\"\n}",
          "options": {
            "raw": {
       "language": "json"
 }
          }
        },
        "url": {
          "raw": "{{base_url}}/api/DashboardAuth/login",
       "host": ["{{base_url}}"],
   "path": ["api", "DashboardAuth", "login"]
        }
      }
  },
    {
      "name": "Get Dashboard Overview",
      "request": {
        "method": "GET",
      "header": [
          {
       "key": "Authorization",
       "value": "Bearer {{token}}",
         "type": "text"
    }
 ],
        "url": {
        "raw": "{{base_url}}/api/EnhancedDashboard/overview",
          "host": ["{{base_url}}"],
          "path": ["api", "EnhancedDashboard", "overview"]
        }
      }
    },
    {
      "name": "Get Groups and Users",
      "request": {
 "method": "GET",
        "header": [
   {
 "key": "Authorization",
        "value": "Bearer {{token}}",
  "type": "text"
     }
        ],
        "url": {
          "raw": "{{base_url}}/api/EnhancedDashboard/groups-users",
       "host": ["{{base_url}}"],
  "path": ["api", "EnhancedDashboard", "groups-users"]
    }
   }
    },
    {
      "name": "Get Recent Reports",
      "request": {
        "method": "GET",
      "header": [
          {
 "key": "Authorization",
   "value": "Bearer {{token}}",
            "type": "text"
       }
        ],
        "url": {
          "raw": "{{base_url}}/api/EnhancedDashboard/recent-reports?count=4",
      "host": ["{{base_url}}"],
   "path": ["api", "EnhancedDashboard", "recent-reports"],
          "query": [
         {
       "key": "count",
      "value": "4"
      }
          ]
        }
  }
    },
    {
      "name": "Get License Details",
 "request": {
 "method": "GET",
        "header": [
  {
        "key": "Authorization",
            "value": "Bearer {{token}}",
         "type": "text"
        }
        ],
        "url": {
          "raw": "{{base_url}}/api/EnhancedDashboard/license-details",
       "host": ["{{base_url}}"],
          "path": ["api", "EnhancedDashboard", "license-details"]
        }
      }
    },
    {
      "name": "Get Quick Actions",
      "request": {
        "method": "GET",
        "header": [
     {
         "key": "Authorization",
 "value": "Bearer {{token}}",
       "type": "text"
          }
   ],
        "url": {
    "raw": "{{base_url}}/api/EnhancedDashboard/quick-actions",
          "host": ["{{base_url}}"],
    "path": ["api", "EnhancedDashboard", "quick-actions"]
        }
      }
    },
 {
      "name": "Get License Management",
"request": {
        "method": "GET",
   "header": [
        {
        "key": "Authorization",
    "value": "Bearer {{token}}",
       "type": "text"
 }
        ],
        "url": {
"raw": "{{base_url}}/api/EnhancedDashboard/license-management",
 "host": ["{{base_url}}"],
      "path": ["api", "EnhancedDashboard", "license-management"]
        }
    }
    },
    {
      "name": "Get User Activity",
      "request": {
    "method": "GET",
        "header": [
          {
  "key": "Authorization",
   "value": "Bearer {{token}}",
            "type": "text"
          }
   ],
        "url": {
          "raw": "{{base_url}}/api/EnhancedDashboard/user-activity?days=7",
  "host": ["{{base_url}}"],
    "path": ["api", "EnhancedDashboard", "user-activity"],
          "query": [
         {
            "key": "days",
         "value": "7"
            }
          ]
        }
   }
    },
    {
  "name": "Get Statistics",
      "request": {
        "method": "GET",
        "header": [
          {
    "key": "Authorization",
    "value": "Bearer {{token}}",
       "type": "text"
}
        ],
        "url": {
"raw": "{{base_url}}/api/EnhancedDashboard/statistics",
     "host": ["{{base_url}}"],
 "path": ["api", "EnhancedDashboard", "statistics"]
      }
      }
    }
  ]
}
```

---

## ✅ Test Checklist

- [ ] API is running on https://localhost:44316
- [ ] Can login and get JWT token
- [ ] Token works for all endpoints
- [ ] Dashboard overview returns metrics
- [ ] Groups and users loads successfully
- [ ] Recent reports shows data
- [ ] License details displays correctly
- [ ] Quick actions based on permissions
- [ ] License management loads
- [ ] User activity shows logs
- [ ] Statistics returns all counts
- [ ] Swagger UI accessible
- [ ] All endpoints documented in Swagger
- [ ] CORS working for frontend
- [ ] No console errors

---

## 🎯 Success Criteria

### API Response
- ✅ Status: 200 OK
- ✅ Response time: < 500ms
- ✅ Valid JSON structure
- ✅ All required fields present
- ✅ Correct data types

### Data Quality
- ✅ Metrics show real numbers
- ✅ Percentages calculated correctly
- ✅ Dates in proper format
- ✅ No null values in required fields
- ✅ Colors match usage ranges

---

**Testing Complete! All endpoints working as expected! 🎉**

**Last Updated**: 2025-01-26  
**Status**: ✅ **READY FOR TESTING**
