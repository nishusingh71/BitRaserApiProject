# 🔍 SUBUSER API TROUBLESHOOTING GUIDE

## 📊 **ISSUE: Subusers nahi mil rahe frontend ko**

**Date:** 2025-01-29  
**Status:** DEBUGGING  
**Priority:** HIGH

---

## 🎯 **AVAILABLE ENDPOINTS:**

### **1. EnhancedSubuserController** ✅
```
GET  /api/EnhancedSubuser                           - Get all (filtered)
GET  /api/EnhancedSubuser/{email}                   - Get single
GET  /api/EnhancedSubuser/by-parent/{parentEmail}   - Get by parent ← MAIN
POST /api/EnhancedSubuser                           - Create
PUT  /api/EnhancedSubuser/{email}                   - Update
DELETE /api/EnhancedSubuser/{email}                 - Delete
```

### **2. EnhancedSubusersController** ✅
```
GET  /api/EnhancedSubusers                          - Get all (filtered)
GET  /api/EnhancedSubusers/by-email/{email}         - Get single
GET  /api/EnhancedSubusers/by-parent/{parentEmail}  - Get by parent ← MAIN
POST /api/EnhancedSubusers                          - Create
PUT  /api/EnhancedSubusers/{email}                  - Update
DELETE /api/EnhancedSubusers/{email}                - Delete
```

### **3. SubuserController (AllTableController)** ✅
```
GET  /api/Subuser                                    - Get all
GET  /api/Subuser/{id}                              - Get by ID
GET  /api/Subuser/by-superuser/{parentUserEmail}    - Get by parent ← MAIN
POST /api/Subuser                                   - Create
PUT  /api/Subuser/{id}                              - Update
DELETE /api/Subuser/{id}                            - Delete
```

---

## 🔧 **DEBUGGING STEPS:**

### **Step 1: Verify Email Encoding** 🎯

#### **Test in Swagger:**
```
1. Open: http://localhost:4000/swagger
2. Navigate to: /api/EmailDebug/encode/{email}
3. Try: GET /api/EmailDebug/encode/your-email@example.com
4. Copy the base64Email value
```

#### **Example:**
```http
GET /api/EmailDebug/encode/nishu@example.com

Response:
{
  "success": true,
  "plainEmail": "nishu@example.com",
  "base64Email": "bmlzaHVAZXhhbXBsZS5jb20",  ← Use this!
  "testEndpoints": {
    "getSubusers": "/api/EnhancedSubuser/by-parent/bmlzaHVAZXhhbXBsZS5jb20"
  }
}
```

---

### **Step 2: Test Subuser Endpoint** 🎯

#### **Option A: Using Swagger (Plain Email)**
```
1. Open Swagger: http://localhost:4000/swagger
2. Authorize with JWT token
3. Find: GET /api/EnhancedSubuser/by-parent/{parentEmail}
4. Click "Try it out"
5. Enter: your-email@example.com  ← Plain email works in Swagger!
6. Click "Execute"
```

#### **Option B: Using Postman/Curl (Base64)**
```bash
# Get Base64 email first:
curl http://localhost:4000/api/EmailDebug/encode/nishu@example.com

# Use the base64Email in request:
curl -X GET "http://localhost:4000/api/EnhancedSubuser/by-parent/bmlzaHVAZXhhbXBsZS5jb20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### **Option C: Using Frontend (JavaScript)**
```javascript
// Encode email function
const encodeEmail = (email) => btoa(email)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');

// Get subusers
const getSubusers = async (parentEmail) => {
    const encoded = encodeEmail(parentEmail);
    const response = await fetch(
        `http://localhost:4000/api/EnhancedSubuser/by-parent/${encoded}`,
        {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        }
    );
    
    if (!response.ok) {
        console.error('Error:', response.status);
        const error = await response.json();
        console.error('Details:', error);
        return [];
    }
    
    return await response.json();
};

// Usage:
const subusers = await getSubusers('nishu@example.com');
console.log('Subusers:', subusers);
```

---

### **Step 3: Check Current User Email** 🎯

```http
GET /api/EmailDebug/current-user
Authorization: Bearer YOUR_JWT_TOKEN

Response:
{
  "success": true,
  "currentUser": {
    "email": "nishu@example.com",  ← Your email from JWT
    "role": "User"
  },
  "encodedEmail": "bmlzaHVAZXhhbXBsZS5jb20",  ← Use this!
  "testEndpoints": {
    "getSubusers": "/api/EnhancedSubuser/by-parent/bmlzaHVAZXhhbXBsZS5jb20"
  }
}
```

Now use the `encodedEmail` in your request!

---

### **Step 4: Test All Scenarios** 🎯

```http
POST /api/EmailDebug/test-all
Content-Type: application/json

{
  "email": "nishu@example.com"
}

Response:
{
  "success": true,
  "testedEmail": "nishu@example.com",
  "totalTests": 3,
  "passed": 3,
  "results": [
    {
      "test": "Encoding Plain Email",
      "status": "✅ PASS",
      "output": "bmlzaHVAZXhhbXBsZS5jb20"
    },
    ...
  ]
}
```

---

## ❌ **COMMON ERRORS & SOLUTIONS:**

### **Error 1: 400 Bad Request - EMAIL_NOT_ENCODED**
```json
{
  "error": "Invalid URL format",
  "code": "EMAIL_NOT_ENCODED"
}
```

**Solution:**
```javascript
// ❌ Wrong: Using plain email in URL
fetch(`/api/EnhancedSubuser/by-parent/nishu@example.com`);

// ✅ Correct: Encode first
const encoded = encodeEmail('nishu@example.com');
fetch(`/api/EnhancedSubuser/by-parent/${encoded}`);
```

---

### **Error 2: 401 Unauthorized**
```json
{
  "error": "Unauthorized"
}
```

**Solution:**
```javascript
// ❌ Wrong: No token
fetch(`/api/EnhancedSubuser/by-parent/${encoded}`);

// ✅ Correct: Add Authorization header
fetch(`/api/EnhancedSubuser/by-parent/${encoded}`, {
    headers: {
        'Authorization': `Bearer ${token}`
    }
});
```

---

### **Error 3: 403 Forbidden**
```json
{
  "error": "You can only view your own subusers"
}
```

**Solution:**
- You're trying to access someone else's subusers
- Use your own email as parentEmail
- Check if you're logged in with correct account

```javascript
// Get current user email from JWT
const currentEmail = 'nishu@example.com'; // From JWT
const encoded = encodeEmail(currentEmail);
fetch(`/api/EnhancedSubuser/by-parent/${encoded}`);
```

---

### **Error 4: 404 Not Found (Empty Array)**
```json
[]
```

**Reasons:**
1. ✅ No subusers created yet
2. ✅ Wrong parentEmail
3. ✅ Subusers exist in different database (private cloud)

**Solution:**
```javascript
// Verify email is correct
console.log('Fetching for:', parentEmail);
console.log('Encoded:', encodeEmail(parentEmail));

// Check database
// 1. Login to MySQL/phpMyAdmin
// 2. SELECT * FROM subuser WHERE user_email = 'nishu@example.com';
```

---

### **Error 5: CORS Error**
```
Access to fetch at '...' from origin '...' has been blocked by CORS policy
```

**Solution:**
Already configured in Program.cs! But if still occurring:

```javascript
// Make sure frontend URL is in CORS whitelist
// Check Program.cs line ~90:
// "http://localhost:3000",
// "http://localhost:4200",
// "http://localhost:5173",
```

---

## 🎯 **QUICK DEBUG CHECKLIST:**

```
[ ] 1. Email is Base64-encoded (use /api/EmailDebug/encode)
[ ] 2. JWT token is valid and included in Authorization header
[ ] 3. Using correct endpoint (/api/EnhancedSubuser/by-parent/{encoded})
[ ] 4. ParentEmail matches JWT email (logged-in user)
[ ] 5. Subusers actually exist in database
[ ] 6. CORS is configured for your frontend origin
[ ] 7. API is running (http://localhost:4000)
[ ] 8. Database connection is working
```

---

## 📝 **COMPLETE WORKING EXAMPLE:**

### **React/Next.js:**
```javascript
// utils/api.js
const API_BASE = 'http://localhost:4000';

const encodeEmail = (email) => btoa(email)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');

export const getSubusers = async (parentEmail, token) => {
    try {
        const encoded = encodeEmail(parentEmail);
        
        console.log('📧 Fetching subusers for:', parentEmail);
        console.log('📧 Encoded email:', encoded);
        console.log('🔗 URL:', `${API_BASE}/api/EnhancedSubuser/by-parent/${encoded}`);
        
        const response = await fetch(
            `${API_BASE}/api/EnhancedSubuser/by-parent/${encoded}`,
            {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            }
        );
        
        console.log('📊 Response status:', response.status);
        
        if (!response.ok) {
            const error = await response.json();
            console.error('❌ Error response:', error);
            throw new Error(error.message || 'Failed to fetch subusers');
        }
        
        const data = await response.json();
        console.log('✅ Subusers loaded:', data.length);
        
        return data;
    } catch (error) {
        console.error('❌ Error fetching subusers:', error);
        throw error;
    }
};

// Usage in component:
const MyComponent = () => {
    const [subusers, setSubusers] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    
    useEffect(() => {
        const fetchSubusers = async () => {
            setLoading(true);
            setError(null);
            
            try {
                const token = localStorage.getItem('token');
                const userEmail = localStorage.getItem('email'); // or from JWT
                
                const data = await getSubusers(userEmail, token);
                setSubusers(data);
            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };
        
        fetchSubusers();
    }, []);
    
    if (loading) return <div>Loading...</div>;
    if (error) return <div>Error: {error}</div>;
    
    return (
        <div>
            <h1>Subusers ({subusers.length})</h1>
            {subusers.map(sub => (
                <div key={sub.subuser_id}>
                    {sub.subuser_name} - {sub.subuser_email}
                </div>
            ))}
        </div>
    );
};
```

---

## 🚀 **TESTING IN BROWSER CONSOLE:**

```javascript
// Copy-paste this in browser console:

const API_BASE = 'http://localhost:4000';
const token = 'YOUR_JWT_TOKEN_HERE';
const parentEmail = 'nishu@example.com';

const encodeEmail = (email) => btoa(email)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');

const testSubusers = async () => {
    const encoded = encodeEmail(parentEmail);
    console.log('Encoded:', encoded);
    
    const response = await fetch(
        `${API_BASE}/api/EnhancedSubuser/by-parent/${encoded}`,
        {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        }
    );
    
    const data = await response.json();
    console.log('Response:', data);
    return data;
};

// Run test:
testSubusers();
```

---

## 📞 **NEED MORE HELP?**

### **Debug Endpoints:**
1. `/api/EmailDebug/encode/{email}` - Encode email
2. `/api/EmailDebug/decode/{encoded}` - Decode email
3. `/api/EmailDebug/current-user` - Get JWT info
4. `/api/EmailDebug/test-all` - Test encoding/decoding
5. `/api/EmailDebug/test/{value}` - Test if value works

### **Check Logs:**
```bash
# Backend logs will show:
🔍 Fetching subusers for parent: {email}
✅ Retrieved {count} subusers for parent {email}
```

---

**Status:** Ready for debugging! ✅  
**Build:** SUCCESS  
**Endpoints:** All working

**Happy Debugging! 🔍✨**
