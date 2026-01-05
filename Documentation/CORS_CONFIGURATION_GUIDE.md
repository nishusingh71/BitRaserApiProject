# CORS Configuration Guide for BitRaser API

## 🌐 CORS Successfully Configured!

आपके BitRaser API project में CORS (Cross-Origin Resource Sharing) successfully configure हो गया है। अब आप आसानी से अपने frontend applications को API के साथ connect कर सकते हैं।

## ✅ What Has Been Configured

### 1. **Multiple CORS Policies**
- **DevelopmentPolicy**: Development के लिए - सभी origins allow करता है
- **ProductionPolicy**: Production के लिए - specific origins के साथ security
- **StrictPolicy**: High-security environments के लिए

### 2. **Supported Frontend Ports**
आपका API निम्नलिखित ports को automatically support करता है:
- `http://localhost:3000` - React (default)
- `http://localhost:3001` - React (alternative)
- `http://localhost:4200` - Angular (default)
- `http://localhost:5173` - Vite (default)
- `http://localhost:8080` - Vue.js (default)
- `http://localhost:8081` - Vue.js (alternative)
- सभी HTTPS variants भी supported हैं

### 3. **Allowed HTTP Methods**
- GET, POST, PUT, DELETE, PATCH, OPTIONS

### 4. **Allowed Headers**
- Authorization
- Content-Type
- Accept
- Origin
- X-Requested-With

## 🧪 Testing Your CORS Configuration

### 1. **Basic CORS Test**
```javascript
// Simple fetch test
fetch('http://localhost:4000/api/corstest/test')
  .then(response => response.json())
  .then(data => console.log('CORS Test:', data))
  .catch(error => console.error('Error:', error));
```

### 2. **Test with Authentication**
```javascript
// Test with JWT token
fetch('http://localhost:4000/api/corstest/test-auth', {
  headers: {
    'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE',
    'Content-Type': 'application/json'
  }
})
.then(response => response.json())
.then(data => console.log('Auth Test:', data));
```

### 3. **Test POST Request**
```javascript
// Test POST request
fetch('http://localhost:4000/api/corstest/test-post', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ message: 'Hello from frontend!' })
})
.then(response => response.json())
.then(data => console.log('POST Test:', data));
```

## 🔧 Frontend Framework Examples

### React Example
```javascript
// React component example
import React, { useEffect, useState } from 'react';

function ApiTest() {
  const [data, setData] = useState(null);

  useEffect(() => {
    fetch('http://localhost:4000/api/corstest/test')
      .then(response => response.json())
      .then(data => setData(data))
      .catch(error => console.error('Error:', error));
  }, []);

  return (
    <div>
      <h2>API Connection Test</h2>
      {data ? (
        <pre>{JSON.stringify(data, null, 2)}</pre>
      ) : (
        <p>Loading...</p>
      )}
    </div>
  );
}

export default ApiTest;
```

### Angular Example
```typescript
// Angular service example
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'http://localhost:4000/api';

  constructor(private http: HttpClient) {}

  testCors(): Observable<any> {
    return this.http.get(`${this.baseUrl}/corstest/test`);
  }

  loginUser(credentials: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/login`, credentials);
  }
}
```

### Vue.js Example
```javascript
// Vue.js composition API example
import { ref, onMounted } from 'vue';

export default {
  setup() {
    const apiData = ref(null);
    const loading = ref(true);

    const testApi = async () => {
      try {
        const response = await fetch('http://localhost:4000/api/corstest/test');
        const data = await response.json();
        apiData.value = data;
      } catch (error) {
        console.error('API Error:', error);
      } finally {
        loading.value = false;
      }
    };

    onMounted(() => {
      testApi();
    });

    return {
      apiData,
      loading,
      testApi
    };
  }
};
```

## 🛠️ Configuration Customization

### Environment Variables
आप निम्नलिखित environment variables से CORS को customize कर सकते हैं:

```bash
# .env file
CORS__AllowedOrigins=http://localhost:3000,https://yourdomain.com
CORS__StrictOrigins=https://yourdomain.com
CORS__Policy=ProductionPolicy
```

### appsettings.json Configuration
```json
{
  "CORS": {
    "Policy": "ProductionPolicy",
    "AllowedOrigins": "http://localhost:3000,https://yourdomain.com",
    "AllowCredentials": true,
    "AllowedMethods": "GET,POST,PUT,DELETE,OPTIONS",
    "AllowedHeaders": "Authorization,Content-Type,Accept",
    "PreflightMaxAge": 600
  }
}
```

## 🚀 Production Deployment

### For Production, make sure to:

1. **Update AllowedOrigins** to your actual domain:
```json
{
  "CORS": {
    "AllowedOrigins": "https://yourdomain.com,https://www.yourdomain.com"
  }
}
```

2. **Use HTTPS** in production
3. **Set proper environment variables**

## 📝 Available Test Endpoints

- `GET /api/corstest/test` - Basic CORS test (no auth required)
- `GET /api/corstest/test-auth` - CORS test with authentication
- `POST /api/corstest/test-post` - Test POST requests
- `OPTIONS /api/corstest/*` - Preflight requests
- `GET /api/corstest/config` - CORS configuration info

## 🔍 Troubleshooting

### Common Issues:

1. **"Access-Control-Allow-Origin" error**
   - Check if your frontend port is in the allowed origins list
   - Verify your API is running on port 4000

2. **Preflight requests failing**
   - Make sure OPTIONS method is allowed
   - Check if required headers are in allowed headers list

3. **Authentication issues**
   - Ensure `AllowCredentials` is set to true
   - Include proper Authorization header in requests

### Debug Steps:
1. Test basic endpoint: `http://localhost:4000/api/corstest/test`
2. Check browser network tab for CORS errors
3. Verify API is running and accessible
4. Check frontend and API ports match configuration

## 🎉 Success!

आपका CORS configuration ready है! अब आप अपने frontend application को BitRaser API के साथ connect कर सकते हैं।

**Happy Coding! 🚀**