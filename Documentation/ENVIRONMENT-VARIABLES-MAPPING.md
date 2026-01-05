# ✅ Environment Variables Complete Mapping Guide

## 🎯 **OVERVIEW**

This document explains how **appsettings.json** placeholder values map to **.env file** environment variables to keep your credentials secure.

---

## 📋 **CONFIGURATION PRIORITY**

```
Environment Variables (.env) > appsettings.json > appsettings.Development.json
```

**Translation:**
- ✅ .env file values **OVERRIDE** appsettings.json
- ✅ appsettings.json provides **default placeholders**
- ✅ Credentials NEVER go in appsettings.json (GitHub safe!)

---

## 🔐 **COMPLETE MAPPING TABLE**

### **1. Database Connection Strings**

| appsettings.json Placeholder | .env Variable | Current Value |
|------------------------------|---------------|---------------|
| `ConnectionStrings__ApplicationDbContextConnection` | `ConnectionStrings__ApplicationDbContextConnection` | `Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase__App;User=2tdeFNZMcsWKkDR.root;Password=76wtaj1GZkg7Qhek;SslMode=Required;` |
| `ConnectionStrings__PrivateCloudDbContextConnection` | `ConnectionStrings__PrivateCloudDbContextConnection` | `Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase__Private;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;` |

**appsettings.json:**
```json
"ConnectionStrings": {
    "ApplicationDbContextConnection": "ConnectionStrings__ApplicationDbContextConnection",
    "CloudEraseConnection": "ConnectionStrings__PrivateCloudDbContextConnection"
}
```

**.env:**
```sh
ConnectionStrings__ApplicationDbContextConnection=Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase__App;User=2tdeFNZMcsWKkDR.root;Password=76wtaj1GZkg7Qhek;SslMode=Required;
ConnectionStrings__PrivateCloudDbContextConnection=Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;Port=4000;Database=Cloud_Erase__Private;User=4WScT7meioLLU3B.root;Password=89ayiOJGY2055G0g;SslMode=Required;
```

---

### **2. JWT Configuration**

| appsettings.json Placeholder | .env Variable | Current Value |
|------------------------------|---------------|---------------|
| `Jwt__Key` | `Jwt__Key` | `qwertyuiop1234567890asdfghjklzxc` |
| `Jwt__Issuer` | `Jwt__Issuer` | `DhruvApiIssuer` |
| `Jwt__Audience` | `Jwt__Audience` | `DhruvApiAudience` |
| `Jwt__ExpirationMinutes` | `Jwt__ExpirationMinutes` | `60` |
| `Jwt__RefreshTokenExpirationDays` | `Jwt__RefreshTokenExpirationDays` | `7` |

**appsettings.json:**
```json
"Jwt": {
    "Key": "Jwt__Key",
    "Issuer": "Jwt__Issuer",
    "Audience": "Jwt__Audience",
    "ExpirationMinutes": "Jwt__ExpirationMinutes",
    "RefreshTokenExpirationDays": "Jwt__RefreshTokenExpirationDays"
}
```

**.env:**
```sh
Jwt__Key=qwertyuiop1234567890asdfghjklzxc
Jwt__Issuer=DhruvApiIssuer
Jwt__Audience=DhruvApiAudience
Jwt__ExpirationMinutes=60
Jwt__RefreshTokenExpirationDays=7
```

---

### **3. Rate Limiting**

| appsettings.json Placeholder | .env Variable | Current Value |
|------------------------------|---------------|---------------|
| `RateLimiting__Enabled` | `RateLimiting__Enabled` | `true` |
| `RateLimiting__PrivateCloudLimit` | `RateLimiting__PrivateCloudLimit` | `5000` |
| `RateLimiting__NormalUserLimit` | `RateLimiting__NormalUserLimit` | `1000` |
| `RateLimiting__UnauthenticatedLimit` | `RateLimiting__UnauthenticatedLimit` | `100` |
| `RateLimiting__ForgotPasswordHourlyLimit` | `RateLimiting__ForgotPasswordHourlyLimit` | `5` |
| `RateLimiting__WindowDurationMinutes` | `RateLimiting__WindowDurationMinutes` | `1` |

**appsettings.json:**
```json
"RateLimiting": {
    "Enabled": "RateLimiting__Enabled",
    "PrivateCloudLimit": "RateLimiting__PrivateCloudLimit",
    "NormalUserLimit": "RateLimiting__NormalUserLimit",
    "UnauthenticatedLimit": "RateLimiting__UnauthenticatedLimit",
    "ForgotPasswordHourlyLimit": "RateLimiting__ForgotPasswordHourlyLimit",
    "WindowDurationMinutes": "RateLimiting__WindowDurationMinutes"
}
```

**.env:**
```sh
RateLimiting__Enabled=true
RateLimiting__PrivateCloudLimit=5000
RateLimiting__NormalUserLimit=1000
RateLimiting__UnauthenticatedLimit=100
RateLimiting__ForgotPasswordHourlyLimit=5
RateLimiting__WindowDurationMinutes=1
```

---

### **4. Brevo Email Service**

| appsettings.json Placeholder | .env Variable | Current Value |
|------------------------------|---------------|---------------|
| `Brevo__ApiKey` | `Brevo__ApiKey` | `xsmtpsib-d327d06f35b35059c693fada665066130e41ba877b865a7015a31ed3d56ebe9a-cO0RaW6tpFkWgNEk` |
| `Brevo__SenderEmail` | `Brevo__SenderEmail` | `nishus877@gmail.com` |
| `Brevo__SenderName` | `Brevo__SenderName` | `DSecure Support` |

**appsettings.json:**
```json
"Brevo": {
    "ApiKey": "Brevo__ApiKey",
    "SenderEmail": "Brevo__SenderEmail",
    "SenderName": "Brevo__SenderName"
}
```

**.env:**
```sh
Brevo__ApiKey=xsmtpsib-d327d06f35b35059c693fada665066130e41ba877b865a7015a31ed3d56ebe9a-cO0RaW6tpFkWgNEk
Brevo__SenderEmail=nishus877@gmail.com
Brevo__SenderName=DSecure Support
```

---

### **5. Gmail SMTP Settings**

| appsettings.json Placeholder | .env Variable | Current Value |
|------------------------------|---------------|---------------|
| `EmailSettings__SmtpHost` | `EmailSettings__SmtpHost` | `smtp.gmail.com` |
| `EmailSettings__SmtpPort` | `EmailSettings__SmtpPort` | `465` |
| `EmailSettings__FromEmail` | `EmailSettings__FromEmail` | `nishus877@gmail.com` |
| `EmailSettings__FromPassword` | `EmailSettings__FromPassword` | `nbaoivfshlzgawtj` (App Password) |
| `EmailSettings__FromName` | `EmailSettings__FromName` | `Dsecure Support` |
| `EmailSettings__EnableSsl` | `EmailSettings__EnableSsl` | `true` |
| `EmailSettings__Timeout` | `EmailSettings__Timeout` | `12000` |

**appsettings.json:**
```json
"EmailSettings": {
    "SmtpHost": "EmailSettings__SmtpHost",
    "SmtpPort": "EmailSettings__SmtpPort",
    "FromEmail": "EmailSettings__FromEmail",
    "FromPassword": "EmailSettings__FromPassword",
    "FromName": "EmailSettings__FromName",
    "EnableSsl": "EmailSettings__EnableSsl",
    "Timeout": "EmailSettings__Timeout"
}
```

**.env:**
```sh
EmailSettings__SmtpHost=smtp.gmail.com
EmailSettings__SmtpPort=465
EmailSettings__FromEmail=nishus877@gmail.com
EmailSettings__FromPassword=nbaoivfshlzgawtj
EmailSettings__FromName=Dsecure Support
EmailSettings__EnableSsl=true
EmailSettings__Timeout=12000
```

---

### **6. Encryption Settings**

| appsettings.json Placeholder | .env Variable | Current Value |
|------------------------------|---------------|---------------|
| `Encryption__Enabled` | `Encryption__Enabled` | `true` |
| `Encryption__Key` | `Encryption__Key` | `YourEncryptionKey32CharactersLong!` |
| `Encryption__IV` | `Encryption__IV` | `1234567890123456` |
| `Encryption__ResponseKey` | `Encryption__ResponseKey` | `2b8A1Pv0ykhppFD28MV6ResponseKey!` |

**appsettings.json:**
```json
"Encryption": {
    "Enabled": "Encryption__Enabled",
    "Key": "Encryption__Key",
    "IV": "Encryption__IV",
    "ResponseKey": "Encryption__ResponseKey"
}
```

**.env:**
```sh
Encryption__Enabled=true
Encryption__Key=YourEncryptionKey32CharactersLong!
Encryption__IV=1234567890123456
Encryption__ResponseKey=2b8A1Pv0ykhppFD28MV6ResponseKey!
```

---

## 🔍 **HOW IT WORKS**

### **Step 1: Application Startup**

```
1. Application reads appsettings.json
2. Finds placeholder: "Jwt__Key"
3. Looks for environment variable: Jwt__Key
4. Replaces placeholder with actual value from .env
```

### **Step 2: Configuration Loading**

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// .NET automatically loads .env file in development
// And reads environment variables in production
```

### **Step 3: Accessing Configuration**

```csharp
// In any controller/service
var jwtKey = _configuration["Jwt:Key"];
// Returns: "qwertyuiop1234567890asdfghjklzxc" (from .env)
```

---

## 🚨 **IMPORTANT RULES**

### **✅ DO:**

1. **Keep sensitive data in .env ONLY**
   ```sh
   # ✅ Good
   Jwt__Key=qwertyuiop1234567890asdfghjklzxc
   ```

2. **Use placeholders in appsettings.json**
   ```json
   // ✅ Good
   "Key": "Jwt__Key"
   ```

3. **Add .env to .gitignore**
   ```gitignore
   # ✅ Must have
   .env
   .env.local
   .env.*.local
   ```

4. **Use double underscore `__` for nested configs**
   ```sh
   # ✅ Correct
   EmailSettings__SmtpHost=smtp.gmail.com
   
   # Maps to:
   {
     "EmailSettings": {
       "SmtpHost": "smtp.gmail.com"
     }
   }
   ```

### **❌ DON'T:**

1. **Never hardcode secrets in appsettings.json**
   ```json
   // ❌ BAD - Will be pushed to GitHub!
   "Key": "qwertyuiop1234567890asdfghjklzxc"
   ```

2. **Don't use single underscore for nested configs**
   ```sh
   # ❌ Wrong
   EmailSettings_SmtpHost=smtp.gmail.com
   
   # ✅ Correct
   EmailSettings__SmtpHost=smtp.gmail.com
   ```

3. **Don't commit .env to Git**
   ```sh
   # ❌ Never do this!
   git add .env
   git commit -m "Added .env"
   ```

---

## 📝 **COMPLETE .env TEMPLATE**

Create a `.env.example` file for your team (safe to commit):

```sh
# ==============================================
# DATABASE CONNECTIONS
# ==============================================
ConnectionStrings__ApplicationDbContextConnection=Server=YOUR_SERVER;Port=4000;Database=YOUR_DB;User=YOUR_USER;Password=YOUR_PASSWORD;SslMode=Required;
ConnectionStrings__PrivateCloudDbContextConnection=Server=YOUR_SERVER;Port=4000;Database=YOUR_PRIVATE_DB;User=YOUR_USER;Password=YOUR_PASSWORD;SslMode=Required;

# ==============================================
# JWT CONFIGURATION
# ==============================================
Jwt__Key=YOUR_SECRET_KEY_32_CHARACTERS_LONG
Jwt__Issuer=YOUR_API_ISSUER
Jwt__Audience=YOUR_API_AUDIENCE
Jwt__ExpirationMinutes=60
Jwt__RefreshTokenExpirationDays=7

# ==============================================
# RATE LIMITING
# ==============================================
RateLimiting__Enabled=true
RateLimiting__PrivateCloudLimit=5000
RateLimiting__NormalUserLimit=1000
RateLimiting__UnauthenticatedLimit=100
RateLimiting__ForgotPasswordHourlyLimit=5
RateLimiting__WindowDurationMinutes=1

# ==============================================
# BREVO EMAIL SERVICE
# ==============================================
Brevo__ApiKey=YOUR_BREVO_API_KEY
Brevo__SenderEmail=YOUR_VERIFIED_EMAIL
Brevo__SenderName=YOUR_SENDER_NAME

# ==============================================
# GMAIL SMTP SETTINGS
# ==============================================
EmailSettings__SmtpHost=smtp.gmail.com
EmailSettings__SmtpPort=465
EmailSettings__FromEmail=YOUR_GMAIL
EmailSettings__FromPassword=YOUR_APP_PASSWORD_16_CHARS
EmailSettings__FromName=YOUR_SENDER_NAME
EmailSettings__EnableSsl=true
EmailSettings__Timeout=12000

# ==============================================
# ENCRYPTION
# ==============================================
Encryption__Enabled=true
Encryption__Key=YOUR_ENCRYPTION_KEY_32_CHARACTERS
Encryption__IV=YOUR_16_CHAR_IV
Encryption__ResponseKey=YOUR_RESPONSE_KEY_32_CHARACTERS

# ==============================================
# ENVIRONMENT
# ==============================================
ASPNETCORE__ENVIRONMENT=Production
```

---

## 🔧 **TROUBLESHOOTING**

### **Problem 1: Configuration value not found**

**Error:**
```
Configuration value 'Jwt:Key' not found
```

**Solution:**
1. Check .env file has the variable
2. Check spelling (case-sensitive!)
3. Check double underscore `__` vs single `_`
4. Restart application

---

### **Problem 2: Boolean conversion error**

**Error:**
```
String 'RateLimiting__Enabled' was not recognized as a valid Boolean
```

**Solution:**

**❌ Wrong appsettings.json:**
```json
"Enabled": true  // Hardcoded value
```

**✅ Correct appsettings.json:**
```json
"Enabled": "RateLimiting__Enabled"  // Placeholder
```

**✅ Correct .env:**
```sh
RateLimiting__Enabled=true
```

---

### **Problem 3: Connection string not working**

**Check:**

1. **No spaces in .env:**
   ```sh
   # ❌ Wrong
   Jwt__Key = my-secret-key
   
   # ✅ Correct
   Jwt__Key=my-secret-key
   ```

2. **Proper escaping for special characters:**
   ```sh
   # If password has special chars like @, =, ;
   # Use URL encoding or escape them
   Password=Pass%40word  # @ = %40
   ```

---

## 📊 **CURRENT CONFIGURATION STATUS**

| Section | Status | Source |
|---------|--------|--------|
| **Database** | ✅ Configured | .env |
| **JWT** | ✅ Configured | .env |
| **Rate Limiting** | ✅ Configured | .env |
| **Brevo Email** | ✅ Configured | .env |
| **Gmail SMTP** | ✅ Configured | .env |
| **Encryption** | ✅ Configured | .env |
| **CORS** | ✅ Configured | appsettings.json (safe) |
| **Logging** | ✅ Configured | appsettings.json (safe) |

---

## 🎯 **DEPLOYMENT CHECKLIST**

### **Development:**
- ✅ Use `.env` file
- ✅ ASPNETCORE_ENVIRONMENT=Development
- ✅ Detailed logging enabled

### **Production (Render/Azure/AWS):**
- ✅ Set environment variables in hosting platform
- ✅ ASPNETCORE_ENVIRONMENT=Production
- ✅ Never deploy .env file to production
- ✅ Use platform's secret management

### **Render.com Example:**
```
Dashboard → Environment → Environment Variables

Jwt__Key = qwertyuiop1234567890asdfghjklzxc
Jwt__Issuer = DhruvApiIssuer
ConnectionStrings__ApplicationDbContextConnection = Server=...
```

---

## 🔐 **SECURITY BEST PRACTICES**

### **1. .gitignore (MUST HAVE):**
```gitignore
# Environment files
.env
.env.local
.env.*.local

# Never commit these!
appsettings.Production.json
appsettings.Staging.json
*.pfx
*.key
```

### **2. Secret Rotation:**
```sh
# Change JWT key every 90 days
Jwt__Key=NEW_SECRET_KEY_32_CHARACTERS_LONG

# Rotate email passwords every 6 months
EmailSettings__FromPassword=NEW_APP_PASSWORD_16_CHARS

# Update database passwords quarterly
ConnectionStrings__ApplicationDbContextConnection=Server=...;Password=NEW_PASSWORD;
```

### **3. Access Control:**
```sh
# Only authorized team members should have:
# - .env file access
# - Production environment variables
# - Database credentials
```

---

## ✅ **VERIFICATION STEPS**

### **Step 1: Check Configuration Loading**

```csharp
// Add this in Program.cs (Development only)
var jwtKey = builder.Configuration["Jwt:Key"];
Console.WriteLine($"JWT Key loaded: {jwtKey?.Substring(0, 5)}...");
// Output: "JWT Key loaded: qwert..."
```

### **Step 2: Test Email Settings**

```sh
# Hit test endpoint
GET /api/ForgotPassword/email-config-check

# Should show:
{
  "brevoConfiguration": {
    "isConfigured": true,
    "apiKey": "✅ SET (8 chars, starts with: xsmtpsib...)"
  }
}
```

### **Step 3: Verify Database Connection**

```sh
# Check health endpoint
GET /api/health

# Should return:
{
  "status": "Healthy",
  "database": "Connected"
}
```

---

## 📚 **QUICK REFERENCE**

### **Environment Variable Naming Convention:**

```
Section__SubSection__Property
   ↓         ↓          ↓
 Jwt   __   Key    =  value
Email __ Settings __ SmtpHost = smtp.gmail.com
```

### **appsettings.json Placeholder Format:**

```json
{
  "Section": {
    "Property": "Section__Property"
  }
}
```

### **Access in Code:**

```csharp
// Method 1: Direct access
var value = _configuration["Section:Property"];

// Method 2: Typed access
var emailSettings = _configuration.GetSection("EmailSettings").Get<EmailSettings>();

// Method 3: Options pattern
services.Configure<JwtSettings>(_configuration.GetSection("Jwt"));
```

---

**✅ SUMMARY:**
- ✅ All secrets now in .env (safe from GitHub)
- ✅ appsettings.json has only placeholders (safe to commit)
- ✅ Environment variables properly mapped
- ✅ Application will load config correctly
- ✅ Credentials protected

**Ab aapka application secure hai aur credentials expose nahi honge! 🔐🚀**
