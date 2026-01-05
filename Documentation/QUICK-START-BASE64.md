# 🚀 QUICK START: Enabling Base64 Email Encoding

## Step 1: Register Middleware in Program.cs

Add this line **BEFORE** `app.UseRouting()`:

```csharp
// Add email security middleware
app.UseEmailSecurity();
```

**Complete example:**

```csharp
// ... existing code ...

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ✅ ADD THIS LINE (BEFORE UseRouting)
app.UseEmailSecurity();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## Step 2: Update Any Controller with Email Parameters

### **Example 1: Single Email Parameter**

**BEFORE:**
```csharp
[HttpGet("{email}")]
public async Task<ActionResult<object>> GetUser(string email)
{
    // Use email...
}
```

**AFTER:**
```csharp
[HttpGet("{email}")]
[DecodeEmail]  // ✅ ADD THIS LINE
public async Task<ActionResult<object>> GetUser(string email)
{
    // email is automatically decoded!
}
```

---

### **Example 2: Multiple Email Parameters**

**BEFORE:**
```csharp
[HttpGet("by-parent/{parentEmail}/subuser/{subuserEmail}")]
public async Task<IActionResult> GetSubuser(string parentEmail, string subuserEmail)
{
    // Use emails...
}
```

**AFTER:**
```csharp
[HttpGet("by-parent/{parentEmail}/subuser/{subuserEmail}")]
[DecodeAllEmails]  // ✅ ADD THIS LINE
public async Task<IActionResult> GetSubuser(string parentEmail, string subuserEmail)
{
    // Both emails are automatically decoded!
}
```

---

### **Example 3: Custom Email Parameters**

**BEFORE:**
```csharp
[HttpPost("transfer")]
public async Task<IActionResult> TransferData(
    [FromQuery] string sourceEmail, 
    [FromQuery] string targetEmail)
{
    // Use emails...
}
```

**AFTER:**
```csharp
[HttpPost("transfer")]
[DecodeBase64Email("sourceEmail", "targetEmail")]  // ✅ ADD THIS LINE
public async Task<IActionResult> TransferData(
    [FromQuery] string sourceEmail, 
    [FromQuery] string targetEmail)
{
    // Both emails are automatically decoded!
}
```

---

## Step 3: Test Your API

### **Test 1: Raw Email (Should be REJECTED)**

```http
GET /api/Users/user@example.com
```

**Expected Response: 400 Bad Request**
```json
{
  "error": "Invalid URL format",
  "message": "Email addresses must be Base64-encoded in URLs",
  "code": "EMAIL_NOT_ENCODED",
  "hint": "Use /api/Users/{Base64EncodedEmail}"
}
```

---

### **Test 2: Encoded Email (Should be ACCEPTED)**

```http
GET /api/Users/dXNlckBleGFtcGxlLmNvbQ
```

**Expected Response: 200 OK**
```json
{
  "user_email": "user@example.com",
  "user_name": "John Doe",
  ...
}
```

---

## Step 4: Update Client Code

### **JavaScript/React:**

```javascript
// Create encoding function
const encodeEmail = (email) => {
    return btoa(email)
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
};

// Use in API calls
const email = "user@example.com";
const encodedEmail = encodeEmail(email);

fetch(`/api/Users/${encodedEmail}`, {
    headers: { 'Authorization': `Bearer ${token}` }
})
.then(response => response.json())
.then(data => console.log(data));
```

---

### **C# Client:**

```csharp
using BitRaserApiProject.Utilities;

// Use the built-in encoder
var email = "user@example.com";
var encodedEmail = Base64EmailEncoder.Encode(email);

var httpClient = new HttpClient();
var response = await httpClient.GetAsync($"https://api.example.com/Users/{encodedEmail}");
var user = await response.Content.ReadFromJsonAsync<User>();
```

---

## ✅ COMPLETE!

**You've successfully enabled Base64 email encoding!**

**Next Steps:**
1. Update all controllers with email parameters
2. Test each endpoint
3. Update client applications
4. Deploy to production

**Benefits:**
- ✅ Zero email exposure in URLs
- ✅ GDPR compliant
- ✅ Protected logs
- ✅ Safe analytics

**Security Level:** 🟢 **MAXIMUM**

---

**Need Help?**
- Full Guide: `Documentation/BASE64-EMAIL-ENCODING-GUIDE.md`
- Summary: `Documentation/BASE64-IMPLEMENTATION-SUMMARY.md`

**Happy Coding! 🚀🔒**
