# 🚀 Render.com - Environment Variables Setup Guide

## ⚡ **Quick Setup (5 Minutes)**

### **Step 1: Go to Render Dashboard**
```
https://dashboard.render.com
  └─ Select Your Service (BitRaserApi)
      └─ Click "Environment" tab
```

---

### **Step 2: Add These Environment Variables**

Click **"Add Environment Variable"** and add each one:

| Key | Value | Description |
|-----|-------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Sets .NET environment |
| `EmailSettings__SmtpHost` | `smtp.gmail.com` | SMTP server |
| `EmailSettings__SmtpPort` | `587` | SMTP port |
| `EmailSettings__FromEmail` | `nishus877@gmail.com` | Sender email |
| `EmailSettings__FromPassword` | `nbaoivfshlzgawtj` | Gmail App Password (16 chars) |
| `EmailSettings__FromName` | `Dsecure Support` | Sender name |
| `EmailSettings__EnableSsl` | `true` | Enable SSL |
| `EmailSettings__Timeout` | `60000` | Timeout (60 seconds) |

---

### **Step 3: Screenshot Guide**

```
┌─────────────────────────────────────────────────┐
│  Render Dashboard │
├─────────────────────────────────────────────────┤
│         │
│  Your Service: bitraser-api  │
│    │
│  Tabs: [ Overview | Environment | Settings ]    │
│         ──────────────────────          │
│    │
│  Environment Variables        │
│  ┌───────────────────────────────────────────┐ │
│  │ + Add Environment Variable    │ │
│  └───────────────────────────────────────────┘ │
│           │
│  Existing Variables:          │
│  ┌───────────────────────────────────────────┐ │
│  │ Key: ASPNETCORE_ENVIRONMENT           │ │
│  │ Value: Production │ │
│  │ [Edit] [Delete]    │ │
│  ├───────────────────────────────────────────┤ │
│  │ Key: EmailSettings__SmtpHost           │ │
│  │ Value: smtp.gmail.com            │ │
│  │ [Edit] [Delete]           │ │
│  ├───────────────────────────────────────────┤ │
│  │ Key: EmailSettings__SmtpPort              │ │
│  │ Value: 587      │ │
│  │ [Edit] [Delete]│ │
│  └───────────────────────────────────────────┘ │
│         │
│  [Save Changes]       │
│    │
└─────────────────────────────────────────────────┘
```

---

## 📋 **Copy-Paste Ready Values**

### **All Environment Variables (Copy entire block):**

```env
ASPNETCORE_ENVIRONMENT=Production
EmailSettings__SmtpHost=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__FromEmail=nishus877@gmail.com
EmailSettings__FromPassword=nbaoivfshlzgawtj
EmailSettings__FromName=Dsecure Support
EmailSettings__EnableSsl=true
EmailSettings__Timeout=60000
EmailSettings__UseDefaultCredentials=false
```

---

## 🔐 **Gmail App Password Setup**

### **If you don't have App Password:**

1. **Go to Google Account Security:**
   ```
   https://myaccount.google.com/security
   ```

2. **Enable 2-Step Verification:**
   - Click "2-Step Verification"
   - Follow steps to enable

3. **Generate App Password:**
   ```
   https://myaccount.google.com/apppasswords
 ```

4. **Select:**
   - App: **Mail**
   - Device: **Other (Custom name)** → Enter "Render DSecure API"

5. **Copy 16-character password:**
   ```
   Example: abcd efgh ijkl mnop
   
   ⚠️ Remove spaces before using:
   Correct: abcdefghijklmnop
   Wrong: abcd efgh ijkl mnop
   ```

6. **Update Render Environment Variable:**
   ```
   EmailSettings__FromPassword=abcdefghijklmnop
   ```

---

## 🔄 **After Adding Variables**

### **Save & Deploy:**

1. Click **"Save Changes"** button
2. Render will automatically redeploy
3. Wait 3-5 minutes for deployment

### **Monitor Deployment:**

```
Render Dashboard > Your Service > Logs (Live)

Expected logs:
==> Building from cache...
==> Running build command 'dotnet publish -c Release'
==> Build succeeded
==> Starting service with 'dotnet BitRaserApiProject.dll'
info: Application started
📧 Email Configuration [Environment: Production]
   Host: smtp.gmail.com:587
✅ Ready to accept connections
```

---

## 🧪 **Test After Deploy**

### **Method 1: Browser**
```
https://YOUR-APP-NAME.onrender.com/api/ForgotPassword/email-config-check
```

Expected response:
```json
{
  "fromConfiguration": {
    "smtpHost": "smtp.gmail.com",
    "smtpPort": "587",
    "fromEmail": "nishus877@gmail.com",
    "fromPassword": "SET (16 chars)",
    "enableSsl": "true"
  },
  "fromEnvironmentVariables": {
    "fromEmail": "nishus877@gmail.com",
    "password": "SET (16 chars)"
  }
}
```

### **Method 2: Send Test Email**
```
POST https://YOUR-APP-NAME.onrender.com/api/ForgotPassword/test-email
Body: {"email":"your-email@gmail.com"}
```

---

## ⚠️ **Common Mistakes to Avoid**

| ❌ Wrong | ✅ Correct |
|---------|-----------|
| `EmailSettings:SmtpHost` | `EmailSettings__SmtpHost` |
| `EmailSettings.SmtpPort` | `EmailSettings__SmtpPort` |
| `smtp.gmail.com:587` | `smtp.gmail.com` (port separate) |
| `abcd efgh ijkl mnop` | `abcdefghijklmnop` (no spaces) |
| `"587"` (quotes in value) | `587` (no quotes) |
| `true` (lowercase in JSON) | `true` (string in env var) |

---

## 🔧 **Alternative: Using Render Secrets**

For sensitive data, use Render's Secret Files:

1. **Create `appsettings.Production.json` as Secret File:**

```
Render Dashboard
  └─ Your Service
└─ Environment
└─ Secret Files
           └─ Add Secret File
```

2. **File Path:**
```
/etc/secrets/appsettings.Production.json
```

3. **Content:**
```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
  "FromEmail": "nishus877@gmail.com",
    "FromPassword": "nbaoivfshlzgawtj",
    "FromName": "Dsecure Support",
  "EnableSsl": true,
    "Timeout": 60000
  }
}
```

4. **Update Program.cs to load secret:**
```csharp
builder.Configuration.AddJsonFile(
    "/etc/secrets/appsettings.Production.json",
    optional: true,
 reloadOnChange: true
);
```

---

## 🎯 **Verification Checklist**

After setting environment variables:

- [ ] ✅ All 8 `EmailSettings__*` variables added
- [ ] ✅ `ASPNETCORE_ENVIRONMENT=Production` set
- [ ] ✅ No typos in variable names (double underscore `__`)
- [ ] ✅ Gmail App Password has NO spaces
- [ ] ✅ Saved changes in Render dashboard
- [ ] ✅ Deployment completed successfully
- [ ] ✅ Logs show "Email Configuration" message
- [ ] ✅ Test email endpoint returns success

---

## 📊 **Environment Variables Priority**

Render loads settings in this order:

```
1. Render Environment Variables (HIGHEST)
   ↓
2. appsettings.Production.json (from Git)
   ↓
3. appsettings.json (DEFAULT)
```

So if `EmailSettings__FromPassword` is in both Render env vars and `appsettings.Production.json`, **Render env var wins**.

---

## 🚀 **Quick Deploy Command**

After setting env vars, force redeploy:

```sh
# Option 1: Manual Deploy (Render Dashboard)
Render Dashboard > Your Service > Manual Deploy > Deploy latest commit

# Option 2: Git Push (Auto-deploy)
git add .
git commit -m "chore: Update production settings"
git push origin main
# Render auto-deploys in ~3 minutes
```

---

## 📞 **Support**

**If environment variables not working:**

1. Check Render Logs for errors
2. Verify variable names (double underscore `__`)
3. Try Manual Deploy
4. Contact Render support

**Common Issues:**
- Variable name typo → Double-check spelling
- Password with spaces → Remove all spaces
- Port as string → Use `587` not `"587"`
- Old deployment cached → Force manual deploy

---

**Last Updated:** 2025-01-14  
**Platform:** Render.com  
**Status:** ✅ **READY**

**Environment variables set karo aur forget password kaam karega! 🎉**
