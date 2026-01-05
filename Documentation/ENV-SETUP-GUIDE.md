# 🔐 Environment Variables Setup Guide (.env)

## ✅ **What We Did**

### **1. Created `.env` File**
Created `.env` file in project root with all sensitive credentials:
```env
EmailSettings__FromEmail=nishus877@gmail.com
EmailSettings__FromPassword=nbaoivfshlzgawtj
```

### **2. Updated `.gitignore`**
Added `.env` to gitignore so credentials won't be committed to Git:
```gitignore
.env
.env.local
.env.development
.env.production
```

### **3. Updated `appsettings.json`**
Removed sensitive data and added placeholder values:
```json
{
  "EmailSettings": {
    "FromEmail": "configure-in-env-file@example.com",
 "FromPassword": "configure-in-env-file"
  }
}
```

### **4. Updated `appsettings.json.example`**
Template for other developers:
```json
{
  "EmailSettings": {
    "FromEmail": "your-email@gmail.com",
    "FromPassword": "your-gmail-app-password-here"
  }
}
```

---

## 🚀 **How Environment Variables Work**

### **Priority Order (Highest to Lowest):**
1. ✅ **Environment Variables** (`.env` file) - **HIGHEST PRIORITY**
2. ⚙️ `appsettings.json`
3. 📝 `appsettings.Development.json`
4. 🏭 `appsettings.Production.json`

### **Example:**
```env
# .env file
EmailSettings__FromEmail=real-email@gmail.com
EmailSettings__FromPassword=real-app-password
```

```json
// appsettings.json (will be overridden)
{
  "EmailSettings": {
    "FromEmail": "placeholder@example.com",
    "FromPassword": "placeholder"
  }
}
```

**Result:** Application will use `real-email@gmail.com` from `.env`, NOT `placeholder@example.com`

---

## 📋 **Complete `.env` File Structure**

```env
# ==============================================
# 🔐 ENVIRONMENT VARIABLES - BITRASER API
# ==============================================

# Database Configuration
ConnectionStrings__ApplicationDbContextConnection=server=localhost;database=BitRaserDb;user id=root;password=password;
ConnectionStrings__DefaultConnection=server=localhost;database=BitRaserDb;user id=root;password=password;

# JWT Configuration
Jwt__Key=YourSuperSecretKeyThatIsAtLeast32CharactersLong123456789!
Jwt__Issuer=DSecureAPI
Jwt__Audience=DSecureAPIUsers
Jwt__ExpirationInMinutes=480

# ✉️ EMAIL CONFIGURATION (SENSITIVE!)
EmailSettings__SmtpHost=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__FromEmail=nishus877@gmail.com
EmailSettings__FromPassword=nbaoivfshlzgawtj
EmailSettings__FromName=Dsecure Support
EmailSettings__EnableSsl=true

# CORS Configuration
CORS__AllowedOrigins=https://dsecure-frontend.vercel.app,http://localhost:3000

# Server Configuration
PORT=4000
ASPNETCORE_ENVIRONMENT=Development
```

---

## 🔧 **How to Use**

### **Step 1: Copy `.env.example` to `.env`**
```bash
cp .env.example .env
```

### **Step 2: Update Values in `.env`**
```env
EmailSettings__FromEmail=YOUR_ACTUAL_EMAIL@gmail.com
EmailSettings__FromPassword=YOUR_ACTUAL_APP_PASSWORD
```

### **Step 3: Verify `.env` is Loaded**
Program.cs already has this code:
```csharp
try
{
    DotNetEnv.Env.Load();
    Console.WriteLine("✅ Environment variables loaded successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Could not load .env file: {ex.Message}");
}
```

### **Step 4: Run Application**
```bash
dotnet run
```

You should see:
```
✅ Environment variables loaded successfully
```

---

## 🧪 **Testing**

### **Test 1: Verify Environment Variables are Loaded**

Add this temporary code to test:
```csharp
// In Program.cs, after Env.Load()
Console.WriteLine($"📧 Email from .env: {Environment.GetEnvironmentVariable("EmailSettings__FromEmail")}");
Console.WriteLine($"🔐 Password loaded: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EmailSettings__FromPassword"))}");
```

### **Test 2: Use Test Email Endpoint**
```http
POST https://localhost:44316/api/ForgotPassword/test-email
Content-Type: application/json

{
  "email": "nishus877@gmail.com"
}
```

**Expected Success:**
```json
{
  "success": true,
  "message": "✅ Test email sent successfully!"
}
```

---

## 📝 **Naming Convention**

### **`.env` Format:**
Use **double underscores (`__`)** for nested properties:

```env
# CORRECT ✅
EmailSettings__FromEmail=test@gmail.com
EmailSettings__SmtpPort=587

# WRONG ❌
EmailSettings:FromEmail=test@gmail.com  # Colon won't work
EmailSettings.FromEmail=test@gmail.com  # Dot won't work
```

### **Mapping:**
```json
// appsettings.json structure
{
  "EmailSettings": {
    "FromEmail": "value",
    "SmtpPort": "587"
  }
}
```
↓ Maps to ↓
```env
# .env format
EmailSettings__FromEmail=value
EmailSettings__SmtpPort=587
```

---

## 🔒 **Security Best Practices**

### **✅ DO:**
1. ✅ Keep `.env` in `.gitignore`
2. ✅ Use App Passwords, not regular passwords
3. ✅ Rotate credentials regularly
4. ✅ Use different credentials for Dev/Prod
5. ✅ Document required variables in `.env.example`

### **❌ DON'T:**
1. ❌ Commit `.env` to Git
2. ❌ Share `.env` file publicly
3. ❌ Use production credentials in development
4. ❌ Hardcode credentials in code
5. ❌ Store credentials in `appsettings.json` (use placeholders only)

---

## 📦 **Production Deployment**

### **Azure App Service:**
Add environment variables in **Configuration** → **Application settings**:
```
EmailSettings__FromEmail = nishus877@gmail.com
EmailSettings__FromPassword = nbaoivfshlzgawtj
```

### **Docker:**
Use `docker-compose.yml`:
```yaml
environment:
  - EmailSettings__FromEmail=nishus877@gmail.com
  - EmailSettings__FromPassword=nbaoivfshlzgawtj
```

Or use `.env` file with Docker:
```yaml
env_file:
  - .env
```

### **Linux Server:**
Export variables in shell:
```bash
export EmailSettings__FromEmail=nishus877@gmail.com
export EmailSettings__FromPassword=nbaoivfshlzgawtj
```

Or use systemd service file:
```ini
[Service]
Environment="EmailSettings__FromEmail=nishus877@gmail.com"
Environment="EmailSettings__FromPassword=nbaoivfshlzgawtj"
```

---

## 🐛 **Troubleshooting**

### **Issue: `.env` file not loading**

**Solution 1:** Check file location
```
✅ CORRECT:
  BitRaserApiProject/
    ├── .env← Root level
    ├── Program.cs
    ├── appsettings.json
    └── BitRaserApiProject.csproj

❌ WRONG:
  BitRaserApiProject/
    └── BitRaserApiProject/
        ├── .env    ← Too deep
   └── Program.cs
```

**Solution 2:** Check file encoding
- Must be **UTF-8 without BOM**
- No special characters in variable names

**Solution 3:** Verify DotNetEnv package
```bash
dotnet add package DotNetEnv
```

### **Issue: Variables not overriding appsettings.json**

**Check priority:**
```csharp
// This will show which value is being used
var fromEmail = builder.Configuration["EmailSettings:FromEmail"];
Console.WriteLine($"Using email: {fromEmail}");
```

### **Issue: Empty password error**

**Check `.env` file:**
```env
# WRONG ❌
EmailSettings__FromPassword=

# CORRECT ✅
EmailSettings__FromPassword=nbaoivfshlzgawtj
```

---

## ✅ **Verification Checklist**

- [ ] `.env` file created in project root
- [ ] `.env` added to `.gitignore`
- [ ] Sensitive data removed from `appsettings.json`
- [ ] `.env.example` created for team reference
- [ ] Gmail App Password configured
- [ ] Application loads `.env` successfully
- [ ] Test email endpoint works
- [ ] Actual forgot password flow tested
- [ ] `.env` NOT committed to Git

---

## 🎊 **Success!**

Now your sensitive credentials are:
- ✅ **Secure** - Not in Git repository
- ✅ **Flexible** - Easy to change without code changes
- ✅ **Safe** - Different for each environment
- ✅ **Professional** - Following industry best practices

**Your email configuration is now secure and production-ready!** 🚀
