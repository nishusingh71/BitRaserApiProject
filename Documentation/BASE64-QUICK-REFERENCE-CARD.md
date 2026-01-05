# 📇 BASE64 EMAIL ENCODING - QUICK REFERENCE CARD

## 🎯 **THE RULE:**
```
ALL emails in URL paths MUST be Base64-encoded!
❌ /api/Users/user@example.com  (REJECTED)
✅ /api/Users/dXNlckBleGFtcGxlLmNvbQ  (ACCEPTED)
```

---

## 🔧 **ENCODING FUNCTIONS:**

### **JavaScript/TypeScript:**
```javascript
const encodeEmail = (email) => btoa(email)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');

// Usage:
const email = "user@example.com";
const encoded = encodeEmail(email);
// Result: "dXNlckBleGFtcGxlLmNvbQ"

// In API call:
fetch(`/api/Users/${encoded}`);
```

### **C#:**
```csharp
using BitRaserApiProject.Utilities;

var email = "user@example.com";
var encoded = Base64EmailEncoder.Encode(email);
// Result: "dXNlckBleGFtcGxlLmNvbQ"

// In API call:
var response = await httpClient.GetAsync($"/api/Users/{encoded}");
```

### **Python:**
```python
import base64

def encode_email(email):
    encoded = base64.b64encode(email.encode()).decode()
    return encoded.replace('+', '-').replace('/', '_').rstrip('=')

# Usage:
email = "user@example.com"
encoded = encode_email(email)
# Result: "dXNlckBleGFtcGxlLmNvbQ"

# In API call:
url = f"/api/Users/{encoded}"
```

---

## 📋 **COMMON ENCODINGS:**

| Plain Email | Base64 Encoded |
|-------------|----------------|
| `user@example.com` | `dXNlckBleGFtcGxlLmNvbQ` |
| `admin@test.org` | `YWRtaW5AdGVzdC5vcmc` |
| `test@gmail.com` | `dGVzdEBnbWFpbC5jb20` |
| `subuser@company.co.uk` | `c3VidXNlckBjb21wYW55LmNvLnVr` |

---

## ❌ **ERROR RESPONSES:**

### **Raw Email (400 Bad Request):**
```http
GET /api/Users/user@example.com

Response: 400 Bad Request
{
  "error": "Invalid URL format",
  "message": "Email addresses must be Base64-encoded in URLs",
  "code": "EMAIL_NOT_ENCODED",
  "hint": "Use Base64EmailEncoder.Encode(email)"
}
```

### **Invalid Base64 (400 Bad Request):**
```http
GET /api/Users/not-valid-base64!!!

Response: 400 Bad Request
{
  "error": "Invalid Base64 encoding",
  "parameter": "email",
  "message": "Parameter 'email' must be a valid Base64-encoded email"
}
```

---

## ✅ **SUCCESSFUL REQUEST:**

```http
GET /api/Users/dXNlckBleGFtcGxlLmNvbQ
Authorization: Bearer {token}

Response: 200 OK
{
  "user_email": "user@example.com",
  "user_name": "John Doe",
  ...
}
```

---

## 🎯 **AFFECTED ENDPOINTS:**

### **Pattern:**
```
Any endpoint with {email}, {parentEmail}, {subuserEmail}, {userEmail}
```

### **Examples:**
```
GET    /api/Users/{email}
GET    /api/Sessions/by-email/{email}
GET    /api/Subuser/by-superuser/{parentEmail}
GET    /api/Logs/by-email/{email}
GET    /api/Machines/by-email/{email}
GET    /api/EnhancedSubuser/{email}
GET    /api/EnhancedSubusers/by-parent/{parentEmail}
PATCH  /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}
```

---

## 🔍 **WHY BASE64?**

1. **Privacy** - Emails hidden from URL logs
2. **GDPR** - No PII in access logs
3. **Security** - Prevents email harvesting
4. **URL Safety** - Avoids special characters

---

## 🧪 **TESTING IN SWAGGER:**

1. Open: `http://localhost:4000/swagger`
2. Find endpoint with `{email}` parameter
3. See **Base64 encoding instructions**
4. Example value: `dXNlckBleGFtcGxlLmNvbQ`
5. Click "Try it out" and test!

---

## 💡 **TIPS:**

### **Postman:**
```javascript
// Pre-request Script
const email = pm.variables.get("userEmail");
const encoded = CryptoJS.enc.Base64.stringify(CryptoJS.enc.Utf8.parse(email))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
pm.variables.set("encodedEmail", encoded);

// Use: /api/Users/{{encodedEmail}}
```

### **React/Vue/Angular:**
```javascript
// Create a utility file: utils/base64.js
export const encodeEmail = (email) => btoa(email)
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

// Use everywhere:
import { encodeEmail } from '@/utils/base64';

const fetchUser = async (email) => {
    const encoded = encodeEmail(email);
    const response = await fetch(`/api/Users/${encoded}`);
    return response.json();
};
```

---

## 📚 **MORE INFO:**

- **Full Guide:** `BASE64-EMAIL-ENCODING-GUIDE.md`
- **Quick Start:** `QUICK-START-BASE64.md`
- **Swagger Integration:** `SWAGGER-BASE64-INTEGRATION-COMPLETE.md`

---

**🎯 REMEMBER: Always encode emails before sending to API!**

**✅ Build:** SUCCESS  
**🔒 Security:** MAXIMUM  
**📖 Docs:** COMPLETE

---

**Print this card and keep it handy! 📇✨**
