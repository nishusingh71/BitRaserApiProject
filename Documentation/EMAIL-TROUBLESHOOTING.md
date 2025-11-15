# 🔧 Email OTP Troubleshooting Guide

## 🚨 Problem: Email Not Sending Despite Correct App Password

### ✅ **Step-by-Step Diagnosis**

---

## 🧪 **Step 1: Check Configuration Loading**

### **Endpoint:** `GET /api/ForgotPassword/email-config-check`

```http
GET https://localhost:XXXX/api/ForgotPassword/email-config-check
```

**Expected Response:**
```json
{
  "fromConfiguration": {
    "smtpHost": "smtp.gmail.com",
    "smtpPort": "587",
    "fromEmail": "nishus877@gmail.com",
    "fromPassword": "SET (16 chars)",
    "fromName": "Dsecure Support",
    "enableSsl": "true"
  },
  "fromEnvironmentVariables": {
  "fromEmail": "nishus877@gmail.com",
    "password": "SET (16 chars)"
  },
  "recommendations": {
    "step1": "Check .env file exists in project root",
  "step2": "Verify EmailSettings__FromPassword has no spaces",
"step3": "Restart application after changing .env",
    "step4": "Gmail App Password should be 16 characters",
    "step5": "Try test-email endpoint to send actual email"
  }
}
```

### ❌ **If Password shows "NOT SET":**

1. **Check `.env` file location:**
```
BitRaserApiProject/
├── .env    ← Must be here (project root)
├── Program.cs
└── appsettings.json
```

2. **Verify `.env` file content:**
```env
EmailSettings__FromPassword=nbaoivfshlzgawtj
```

**Common Mistakes:**
- ❌ `EmailSettings:FromPassword` (colon instead of double underscore)
- ❌ `EmailSettings_FromPassword` (single underscore)
- ❌ Spaces in password: `nbao ivfs hlzg awtj`
- ✅ CORRECT: `EmailSettings__FromPassword=nbaoivfshlzgawtj`

3. **Restart application after changing `.env`**

---

## 🧪 **Step 2: Test Email Sending**

### **Endpoint:** `POST /api/ForgotPassword/test-email`

```http
POST https://localhost:XXXX/api/ForgotPassword/test-email
Content-Type: application/json

{
  "email": "nishus877@gmail.com"
}
```

**Success Response:**
```json
{
  "success": true,
  "message": "✅ Test email sent successfully! Check your inbox.",
  "email": "nishus877@gmail.com",
  "testOtp": "123456",
  "note": "If you received the email, your SMTP configuration is correct!"
}
```

**Failure Response:**
```json
{
  "success": false,
  "message": "❌ Failed to send test email. Check logs for details.",
  "troubleshooting": {
    "step1": "Verify Gmail App Password in .env file",
    "step2": "Enable 2-Step Verification in Google Account",
  "step3": "Generate new App Password",
   "step4": "Check if 'Less secure app access' is needed (deprecated)",
    "step5": "Try using different email provider (SendGrid, Mailtrap)"
  }
}
```

---

## 📋 **Step 3: Check Application Logs**

Look for these console messages when app starts:

### ✅ **Success Indicators:**
```
✅ Environment variables loaded successfully
📧 Email Configuration - Host: smtp.gmail.com, Port: 587, From: nishus877@gmail.com, SSL: True
📧 Password loaded: 16 characters
```

### ❌ **Error Indicators:**
```
❌ Email password is empty! Check .env file: EmailSettings__FromPassword
❌ Also check appsettings.json: EmailSettings:FromPassword
```

---

## 🔍 **Common Issues & Solutions**

### **Issue 1: "Password is empty"**

**Logs show:**
```
❌ Email password is empty! Check .env file: EmailSettings__FromPassword
```

**Solution:**
```bash
# Open .env file
nano .env

# Verify this line exists with NO SPACES in password:
EmailSettings__FromPassword=nbaoivfshlzgawtj

# Save and restart:
dotnet run
```

---

### **Issue 2: "SMTP Authentication Required"**

**Logs show:**
```
❌ SMTP Error: 5.7.0 Authentication Required
```

**Possible Causes:**
1. App Password is incorrect
2. App Password has spaces
3. 2-Step Verification not enabled
4. App Password expired/revoked

**Solution:**

**A. Verify App Password Format:**
```env
# ❌ WRONG (has spaces)
EmailSettings__FromPassword=nbao ivfs hlzg awtj

# ✅ CORRECT (no spaces)
EmailSettings__FromPassword=nbaoivfshlzgawtj
```

**B. Generate New App Password:**
1. Go to: https://myaccount.google.com/apppasswords
2. Select: **Mail** or **Other (Custom name)**
3. Click **Generate**
4. Copy the 16-character code
5. **Remove ALL spaces**
6. Update `.env`:
```env
EmailSettings__FromPassword=YOUR_NEW_PASSWORD_NO_SPACES
```
7. Restart app

---

### **Issue 3: ".env not loading"**

**Logs show:**
```
⚠️ Could not load .env file: File not found
```

**Solution:**

**A. Check file location:**
```bash
# List files
ls -la

# You should see:
# .env
# Program.cs
# appsettings.json
```

**B. Verify file name** (no typo):
```bash
# ❌ WRONG
.environment
env
.env.txt

# ✅ CORRECT
.env
```

**C. Check file encoding:**
- Must be UTF-8
- No BOM (Byte Order Mark)

---

### **Issue 4: "Connection timeout"**

**Logs show:**
```
❌ SMTP Error: Connection timeout
```

**Possible Causes:**
1. Firewall blocking port 587
2. Antivirus blocking SMTP
3. Network issues

**Solution:**

**A. Test port connectivity:**
```bash
# Windows PowerShell
Test-NetConnection smtp.gmail.com -Port 587

# Expected output:
# TcpTestSucceeded : True
```

**B. Try alternative port (465 with SSL):**
```env
EmailSettings__SmtpPort=465
EmailSettings__EnableSsl=true
```

**C. Temporarily disable antivirus/firewall for testing

---

### **Issue 5: "Gmail still blocking"**

**Even with correct App Password, Gmail blocks emails**

**Solutions:**

**A. Wait 10 minutes after generating App Password**
- Gmail needs time to propagate new credentials

**B. Try from different network**
- Switch from WiFi to mobile hotspot
- VPN might be blocking SMTP

**C. Check Gmail Account Settings:**
1. Go to: https://myaccount.google.com/security
2. Verify:
   - ✅ 2-Step Verification: **ON**
   - ✅ Less secure app access: **OFF** (deprecated)
   - ✅ App passwords: At least one generated

**D. Revoke old App Passwords:**
1. https://myaccount.google.com/apppasswords
2. Remove old/unused passwords
3. Generate fresh password
4. Use immediately

---

## 🔧 **Quick Fixes Checklist**

### **Before Testing:**
- [ ] `.env` file in project root
- [ ] Gmail App Password is 16 characters
- [ ] No spaces in password
- [ ] Double underscore: `EmailSettings__FromPassword`
- [ ] Application restarted after `.env` changes
- [ ] 2-Step Verification enabled in Google Account

### **Test Sequence:**
```bash
# 1. Check configuration
GET /api/ForgotPassword/email-config-check

# 2. Verify both sources show password as "SET (16 chars)"

# 3. Test email sending
POST /api/ForgotPassword/test-email
{
  "email": "nishus877@gmail.com"
}

# 4. Check inbox for test OTP email

# 5. If successful, test actual flow
POST /api/ForgotPassword/request-otp
{
  "email": "user@example.com"
}
```

---

## 🚀 **Alternative: Use Mailtrap (100% Reliable for Testing)**

If Gmail continues to cause issues, use Mailtrap for development:

### **Setup (5 minutes):**

1. **Sign up:** https://mailtrap.io (FREE)

2. **Get Credentials:**
   - Go to: **Inboxes** → **My Inbox**
   - Copy SMTP credentials

3. **Update `.env`:**
```env
EmailSettings__SmtpHost=sandbox.smtp.mailtrap.io
EmailSettings__SmtpPort=587
EmailSettings__FromEmail=test@example.com
EmailSettings__FromPassword=your_mailtrap_password
EmailSettings__FromName=Dsecure Support
EmailSettings__EnableSsl=true
```

4. **Restart & Test:**
```bash
dotnet run
```

5. **Check Mailtrap inbox** (not real email) for test messages

### **Benefits:**
- ✅ No authentication issues
- ✅ Instant delivery
- ✅ View all emails in web UI
- ✅ Perfect for testing
- ✅ Free forever (100 emails/day)

---

## 📊 **Diagnosis Decision Tree**

```
Email not sending?
│
├─ Step 1: Check /email-config-check endpoint
│  │
│  ├─ Password shows "NOT SET"?
│  │  └─> Fix .env file, restart app
│  │
│  └─ Password shows "SET (16 chars)"?
│     └─> Continue to Step 2
│
├─ Step 2: Test /test-email endpoint
│  │
│  ├─ Returns 500 error?
│  │  └─> Check logs for SMTP error
│  │     ├─ "Authentication Required"?
│  │     │  └─> Generate new App Password
│  │   │
│  │     ├─ "Connection timeout"?
│  │     │  └─> Check firewall/network
│  │     │
│  │     └─> "Password empty"?
│  │        └─> Fix .env format
│  │
│  └─ Returns 200 success?
│     ├─ Email received?
│     │  └─> ✅ System working!
│     │
│     └─ No email received?
│        └─> Check Gmail spam folder
│        or try Mailtrap
```

---

## 🎯 **Final Verification**

### **Your `.env` file should look exactly like this:**

```env
# ✉️ EMAIL CONFIGURATION
EmailSettings__SmtpHost=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__FromEmail=nishus877@gmail.com
EmailSettings__FromPassword=nbaoivfshlzgawtj
EmailSettings__FromName=Dsecure Support
EmailSettings__EnableSsl=true
```

### **Verification Commands:**

```bash
# 1. Check .env exists
ls -la .env

# 2. Start application
dotnet run

# 3. Look for in console:
# ✅ Environment variables loaded successfully
# 📧 Email Configuration - Host: smtp.gmail.com, Port: 587, From: nishus877@gmail.com
# 📧 Password loaded: 16 characters

# 4. Test endpoints
curl -X GET http://localhost:4000/api/ForgotPassword/email-config-check
curl -X POST http://localhost:4000/api/ForgotPassword/test-email -H "Content-Type: application/json" -d '{"email":"nishus877@gmail.com"}'
```

---

## 📞 **Still Not Working?**

1. **Try Mailtrap** (recommended for development)
2. **Use different Gmail account**
3. **Try SendGrid** (more reliable for production)
4. **Check Gmail account hasn't been flagged/suspended**

**Share these logs if asking for help:**
1. Output from `/email-config-check`
2. Console logs when sending email
3. Error message from `/test-email`

---

Good luck! 🚀
