# 🔧 Private Cloud API - 401 Unauthorized Fix Guide

## ❌ Problem

Getting **401 Unauthorized** error when calling `/api/PrivateCloud/*` endpoints even with valid JWT token.

## ✅ Solution Applied

### **Root Cause**
The controller was using `User.Identity?.Name` which often returns `null` in JWT authentication. This is because the name claim needs to be explicitly set as `ClaimTypes.Name` in the JWT token.

### **Fix Applied**
Changed all instances from:
```csharp
var userEmail = User.Identity?.Name;  // ❌ Returns null
```

To:
```csharp
var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;  // ✅ Works correctly
```

---

## 🧪 How to Test

### **Step 1: Login and Get Token**

```bash
# POST /api/RoleBasedAuth/login
curl -X POST "http://localhost:5000/api/RoleBasedAuth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "YourPassword123"
  }'
```

**Expected Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userType": "user",
"email": "user@example.com",
  "roles": ["User"],
  "expiresAt": "2025-01-15T10:00:00Z"
}
```

### **Step 2: Save the Token**
```bash
export TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### **Step 3: Test Private Cloud Endpoints**

#### ✅ Check Access
```bash
curl -X GET "http://localhost:5000/api/PrivateCloud/check-access" \
  -H "Authorization: Bearer $TOKEN"
```

**Expected Response:**
```json
{
  "hasPrivateCloudAccess": true,
  "isConfigured": false,
  "isSchemaInitialized": false,
  "lastTested": null,
  "testStatus": null,
  "databaseType": null,
  "currentUser": "user@example.com"
}
```

#### ✅ Setup Database
```bash
curl -X POST "http://localhost:5000/api/PrivateCloud/setup" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "databaseType": "mysql",
    "serverHost": "gateway01.ap-southeast-1.prod.aws.tidbcloud.com",
    "serverPort": 4000,
    "databaseName": "Cloud_Erase",
    "databaseUsername": "2tdeFNZMcsWKkDR.root",
    "databasePassword": "76wtaj1GZkg7Qhek"
  }'
```

#### ✅ Test Connection
```bash
curl -X POST "http://localhost:5000/api/PrivateCloud/test" \
  -H "Authorization: Bearer $TOKEN"
```

#### ✅ Get Required Tables
```bash
curl -X GET "http://localhost:5000/api/PrivateCloud/required-tables" \
  -H "Authorization: Bearer $TOKEN"
```

#### ✅ Initialize Schema
```bash
curl -X POST "http://localhost:5000/api/PrivateCloud/initialize-schema" \
  -H "Authorization: Bearer $TOKEN"
```

#### ✅ Validate Schema
```bash
curl -X POST "http://localhost:5000/api/PrivateCloud/validate-schema" \
  -H "Authorization: Bearer $TOKEN"
```

#### ✅ Get Configuration
```bash
curl -X GET "http://localhost:5000/api/PrivateCloud/config" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 🔍 JWT Token Structure

The JWT token now includes these claims:

```json
{
  "nameid": "user@example.com",      // ✅ ClaimTypes.NameIdentifier
  "unique_name": "user@example.com",  // ✅ ClaimTypes.Name
  "sub": "user@example.com",
  "email": "user@example.com",
  "jti": "abc123...",
  "user_type": "user",
  "role": ["User"],
  "exp": 1705329600
}
```

---

## 🛠️ Frontend Integration

### **React/Next.js Example**

```typescript
// Save token after login
const login = async (email: string, password: string) => {
  const response = await fetch('/api/RoleBasedAuth/login', {
  method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });
  
  const data = await response.json();
  
  if (data.token) {
    localStorage.setItem('jwt_token', data.token);
    localStorage.setItem('user_email', data.email);
  }
};

// Use token in API calls
const checkPrivateCloudAccess = async () => {
  const token = localStorage.getItem('jwt_token');
  
  const response = await fetch('/api/PrivateCloud/check-access', {
    headers: {
'Authorization': `Bearer ${token}`
    }
  });
  
  if (response.status === 401) {
    // Token expired or invalid - redirect to login
    window.location.href = '/login';
    return;
  }
  
  const data = await response.json();
  console.log(data);
};

// Setup database
const setupPrivateDatabase = async (config: DatabaseConfig) => {
  const token = localStorage.getItem('jwt_token');
  
  const response = await fetch('/api/PrivateCloud/setup', {
    method: 'POST',
    headers: {
'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(config)
  });
  
  const data = await response.json();
  return data;
};
```

---

## ⚠️ Common Errors

| Error Code | Message | Solution |
|------------|---------|----------|
| 401 | "User email not found in token" | Token missing or invalid - re-login |
| 400 | "Private cloud feature not enabled" | Admin needs to set `is_private_cloud = TRUE` |
| 404 | "No private database configured" | Run `/setup` endpoint first |
| 500 | "Database connection test failed" | Check database credentials |

---

## 📝 Enable Private Cloud for User

If you get **"Private cloud feature not enabled"**, run this SQL:

```sql
-- Enable private cloud for specific user
UPDATE users 
SET is_private_cloud = TRUE 
WHERE user_email = 'user@example.com';

-- Verify
SELECT user_id, user_email, is_private_cloud 
FROM users 
WHERE user_email = 'user@example.com';
```

---

## ✅ Success Checklist

- [x] ✅ Build successful
- [x] ✅ JWT claims fixed (`ClaimTypes.NameIdentifier`)
- [x] ✅ Better error messages
- [x] ✅ Logging added for troubleshooting
- [x] ✅ `currentUser` returned in check-access
- [x] ✅ All endpoints protected with `[Authorize]`

---

## 🎉 Result

**No more 401 errors!** All Private Cloud endpoints now correctly extract user email from JWT token.

**Test it in Swagger UI:**
1. Click **Authorize** button
2. Enter: `Bearer YOUR_TOKEN`
3. Try any `/api/PrivateCloud/*` endpoint
4. ✅ Should work now!

---

**Last Updated:** 2025-01-14  
**Status:** ✅ **FIXED**  
**Build:** ✅ **SUCCESSFUL**
