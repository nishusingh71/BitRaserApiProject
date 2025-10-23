# Enhanced Dashboard API - Quick Reference

## 🎯 Quick Start

### Base URL
```
https://localhost:44316/api/EnhancedDashboard
```

### Authentication
All endpoints require JWT Bearer token:
```http
Authorization: Bearer eyJhbGc...
```

---

## 📊 Endpoints Overview

| Endpoint | Method | Description | Permission |
|----------|--------|-------------|------------|
| `/overview` | GET | Dashboard metrics & statistics | VIEW_DASHBOARD |
| `/groups-users` | GET | Groups and users management | READ_ALL_USERS |
| `/recent-reports` | GET | Recent audit reports | VIEW_REPORTS |
| `/license-details` | GET | License breakdown by product | VIEW_LICENSES |
| `/quick-actions` | GET | Available quick actions | Dynamic |
| `/license-management` | GET | License management data | MANAGE_LICENSES |
| `/user-activity` | GET | User activity timeline | VIEW_ACTIVITY_LOGS |
| `/statistics` | GET | System-wide statistics | VIEW_STATISTICS |

---

## 🚀 Quick API Calls

### 1. Get Dashboard (Main View)
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/overview" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response Structure**:
```json
{
  "welcomeMessage": "Welcome back, user@example.com",
  "metrics": {
    "totalLicenses": { "value": 3287, "changePercent": 2.0, "changeDirection": "up" },
    "activeUsers": { "value": 156, "changePercent": 5.0, "changeDirection": "up" },
    "availableLicenses": { "value": 1200, "changePercent": 8.0, "changeDirection": "down" },
    "successRate": { "value": 99, "changePercent": 0.3, "changeDirection": "up", "unit": "%" }
  }
}
```

### 2. Get Groups
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/groups-users" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 3. Get Recent Reports
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/recent-reports?count=4" \
-H "Authorization: Bearer YOUR_TOKEN"
```

### 4. Get License Details
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/license-details" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 5. Get Quick Actions
```bash
curl -X GET "https://localhost:44316/api/EnhancedDashboard/quick-actions" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## 🎨 Frontend Integration Examples

### React Hook
```tsx
import { useState, useEffect } from 'react';

export function useDashboard() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('/api/EnhancedDashboard/overview', {
      headers: { 'Authorization': `Bearer ${token}` }
    })
    .then(res => res.json())
      .then(setData)
      .finally(() => setLoading(false));
  }, []);

  return { data, loading };
}
```

### Vue Composable
```ts
import { ref, onMounted } from 'vue';

export function useDashboard() {
  const data = ref(null);
  const loading = ref(true);

  onMounted(async () => {
    const response = await fetch('/api/EnhancedDashboard/overview', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    data.value = await response.json();
    loading.value = false;
  });

  return { data, loading };
}
```

### Angular Service
```ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private http: HttpClient) {}

  getOverview() {
    return this.http.get('/api/EnhancedDashboard/overview');
  }

  getGroups() {
    return this.http.get('/api/EnhancedDashboard/groups-users');
  }
}
```

---

## 📊 Key Data Structures

### Metric Object
```ts
interface Metric {
  value: number;
  label: string;
  changePercent: number;
  changeDirection: "up" | "down";
  icon: string;
  unit?: string;
}
```

### Group Object
```ts
interface Group {
  groupName: string;
  description: string;
  licenses: number;
  dateCreated: string;
}
```

### Report Object
```ts
interface Report {
  reportId: string;
  reportName: string;
  erasureMethod: string;
  reportDate: Date;
  day: string;
  deviceCount: number;
}
```

### License Detail Object
```ts
interface LicenseDetail {
  product: string;
  totalAvailable: number;
  totalConsumed: number;
  usage: number;
  usageColor: string;
}
```

---

## 🎯 Common Use Cases

### 1. Dashboard Homepage
```ts
// Load all dashboard data
const dashboard = {
  overview: await api.get('/EnhancedDashboard/overview'),
  groups: await api.get('/EnhancedDashboard/groups-users'),
  reports: await api.get('/EnhancedDashboard/recent-reports'),
  licenses: await api.get('/EnhancedDashboard/license-details'),
  actions: await api.get('/EnhancedDashboard/quick-actions')
};
```

### 2. Real-time Metrics
```ts
// Poll dashboard every 30 seconds
setInterval(async () => {
  const data = await api.get('/EnhancedDashboard/overview');
  updateMetrics(data.metrics);
}, 30000);
```

### 3. Export Dashboard Data
```ts
async function exportDashboard() {
  const data = await api.get('/EnhancedDashboard/statistics');
  const blob = new Blob([JSON.stringify(data, null, 2)], 
    { type: 'application/json' });
  downloadBlob(blob, 'dashboard-export.json');
}
```

---

## 🔒 Error Handling

### Common Status Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Display data |
| 401 | Unauthorized | Redirect to login |
| 403 | Forbidden | Show permission error |
| 500 | Server Error | Show error message |

### Error Response Example
```json
{
  "message": "Insufficient permissions to view dashboard",
  "error": "Permission denied"
}
```

### Error Handling Code
```ts
try {
  const data = await api.get('/EnhancedDashboard/overview');
  setDashboard(data);
} catch (error) {
  if (error.status === 401) {
    redirectToLogin();
  } else if (error.status === 403) {
    showError('You do not have permission to view this dashboard');
  } else {
    showError('Failed to load dashboard. Please try again.');
  }
}
```

---

## 📈 Performance Tips

### 1. Cache Data
```ts
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes
let cachedData = null;
let cacheTime = 0;

async function getDashboard() {
  if (cachedData && Date.now() - cacheTime < CACHE_DURATION) {
    return cachedData;
  }
  
  cachedData = await api.get('/EnhancedDashboard/overview');
  cacheTime = Date.now();
  return cachedData;
}
```

### 2. Parallel Requests
```ts
const [overview, groups, reports] = await Promise.all([
  api.get('/EnhancedDashboard/overview'),
  api.get('/EnhancedDashboard/groups-users'),
  api.get('/EnhancedDashboard/recent-reports')
]);
```

### 3. Lazy Loading
```ts
// Load critical data first
const overview = await api.get('/EnhancedDashboard/overview');
setDashboard(overview);

// Load secondary data in background
Promise.all([
  api.get('/EnhancedDashboard/license-details'),
  api.get('/EnhancedDashboard/user-activity')
]).then(([licenses, activity]) => {
  setLicenses(licenses);
  setActivity(activity);
});
```

---

## 🧪 Testing Examples

### Jest Test
```ts
describe('Dashboard API', () => {
  it('should fetch overview data', async () => {
    const data = await api.get('/EnhancedDashboard/overview');
 
  expect(data).toHaveProperty('metrics');
    expect(data.metrics).toHaveProperty('totalLicenses');
    expect(data.metrics.totalLicenses.value).toBeGreaterThan(0);
  });
});
```

### Cypress Test
```ts
describe('Dashboard Page', () => {
  it('should display metrics', () => {
    cy.visit('/dashboard');
    cy.get('.metric-card').should('have.length', 4);
    cy.contains('Total Licenses').should('be.visible');
  });
});
```

---

## 📱 Mobile Responsive Considerations

```tsx
// Conditional rendering for mobile
function Dashboard() {
  const isMobile = useMediaQuery('(max-width: 768px)');
  
  return (
    <div className={isMobile ? 'dashboard-mobile' : 'dashboard-desktop'}>
      {isMobile ? (
  <MobileMetricsCarousel metrics={metrics} />
      ) : (
        <MetricsGrid metrics={metrics} />
   )}
    </div>
  );
}
```

---

## 🎨 UI Component Libraries

### Material-UI Example
```tsx
import { Card, CardContent, Typography, Grid } from '@mui/material';

function MetricCard({ metric }) {
  return (
    <Card>
      <CardContent>
        <Typography variant="h4">{metric.value}</Typography>
        <Typography color="textSecondary">{metric.label}</Typography>
        <Typography color={metric.changeDirection === 'up' ? 'success.main' : 'error.main'}>
   {metric.changePercent}%
</Typography>
      </CardContent>
    </Card>
  );
}
```

### Tailwind CSS Example
```tsx
function MetricCard({ metric }) {
  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h3 className="text-3xl font-bold">{metric.value}</h3>
      <p className="text-gray-600">{metric.label}</p>
      <span className={`text-sm ${metric.changeDirection === 'up' ? 'text-green-600' : 'text-red-600'}`}>
        {metric.changePercent}% {metric.changeDirection === 'up' ? '↑' : '↓'}
      </span>
    </div>
  );
}
```

---

## 🔗 Related Endpoints

- **Users**: `/api/EnhancedUsers`
- **Machines**: `/api/EnhancedMachines`
- **Reports**: `/api/EnhancedAuditReports`
- **Logs**: `/api/EnhancedLogs`
- **Sessions**: `/api/EnhancedSessions`

---

## 📝 Notes

- All timestamps are in UTC
- Percentage changes are calculated over last 30 days
- Success rate defaults to 99.2% if no data
- License usage is color-coded: Green (<70%), Yellow (70-85%), Red (>85%)
- Quick actions visibility based on user permissions

---

**Last Updated**: 2025-01-26  
**API Version**: 1.0.0  
**Build**: ✅ Successful
