# 📧 Email Configuration Guide for Forgot Password OTP

## 🚨 Current Issue
```
The SMTP server requires a secure connection or the client was not authenticated.
5.7.0 Authentication Required.
```

This means Gmail is **blocking** the email because:
1. ❌ App Password is incorrect/expired
2. ❌ 2-Step Verification not enabled
3. ❌ Less Secure Apps setting (deprecated)

---

## ✅ **Solution: Gmail App Password Setup**

### **Step 1: Enable 2-Step Verification**

1. Go to: **https://myaccount.google.com/security**
2. Scroll to **"Signing in to Google"**
3. Click **"2-Step Verification"**
4. Follow steps to enable it (if not already enabled)

### **Step 2: Generate App Password**

1. Go to: **https://myaccount.google.com/apppasswords**
   - Or: Google Account → Security → 2-Step Verification → App passwords

2. **Select app:** Choose **"Mail"** or **"Other (Custom name)"**
   - Name it: `DSecure API` or `BitRaser OTP`

3. Click **"Generate"**

4. **Copy the 16-character password** (looks like: `abcd efgh ijkl mnop`)
   - ⚠️ **Remove spaces!** Final password: `abcdefghijklmnop`

### **Step 3: Update appsettings.json**

```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "FromEmail": "nishus877@gmail.com",
    "FromPassword": "abcdefghijklmnop",  // ← 16-char App Password (NO SPACES!)
    "FromName": "DSecure Support",
    "EnableSsl": true
  }
}
```

⚠️ **IMPORTANT:**
- Use **App Password**, NOT your regular Gmail password
- Remove all spaces from the 16-character code
- Keep it in `appsettings.json`, NOT in code

---

## 🧪 **Test Email Configuration**

### **Method 1: Using Test Endpoint**

```http
POST https://localhost:XXXX/api/ForgotPassword/test-email
Content-Type: application/json

{
  "email": "your-email@gmail.com"
}
```

**Success Response:**
```json
{
  "success": true,
  "message": "✅ Test email sent successfully! Check your inbox.",
  "email": "your-email@gmail.com",
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
    "step1": "Verify Gmail App Password in appsettings.json",
    "step2": "Enable 2-Step Verification in Google Account",
    "step3": "Generate new App Password from https://myaccount.google.com/apppasswords",
    "step4": "Check if 'Less secure app access' is needed (deprecated)",
    "step5": "Try using different email provider (SendGrid, Mailtrap)"
  }
}
```

### **Method 2: Check Application Logs**

Look for these log messages:
```
📧 Email Configuration - Host: smtp.gmail.com, Port: 587, From: nishus877@gmail.com, SSL: True
❌ Email password is empty! Check appsettings.json EmailSettings:FromPassword
🔐 SMTP Authentication Failed! Please check:
   1. Gmail App Password is correct in appsettings.json
   2. 2-Step Verification is enabled in Google Account
   3. Generate new App Password: https://myaccount.google.com/apppasswords
```

---

## 🔧 **Alternative Email Providers**

### **Option 1: Mailtrap (Development/Testing)**

Free testing SMTP - emails don't go to real inboxes.

```json
{
  "EmailSettings": {
    "SmtpHost": "sandbox.smtp.mailtrap.io",
    "SmtpPort": "587",
    "FromEmail": "test@example.com",
 "FromPassword": "your-mailtrap-password",
    "FromName": "DSecure Support",
    "EnableSsl": true
  }
}
```

**Setup:**
1. Sign up: https://mailtrap.io
2. Go to: **Email Testing** → **Inboxes** → **My Inbox**
3. Copy **SMTP credentials**
4. Update `appsettings.json`

---

### **Option 2: SendGrid (Production)**

Free tier: 100 emails/day

```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.sendgrid.net",
    "SmtpPort": "587",
    "FromEmail": "noreply@yourdomain.com",
    "FromPassword": "YOUR_SENDGRID_API_KEY",
    "FromName": "DSecure Support",
    "EnableSsl": true
  }
}
```

**Setup:**
1. Sign up: https://sendgrid.com
2. Create **API Key**: Settings → API Keys → Create API Key
3. Copy API key
4. Use it as `FromPassword`

---

### **Option 3: Ethereal Email (Development)**

Fake SMTP - view emails in web interface.

1. Go to: https://ethereal.email/create
2. Copy SMTP credentials
3. Update `appsettings.json`

---

## 🐛 **Troubleshooting**

### **Error: "Authentication Required"**

✅ **Solution:**
1. Generate new App Password
2. Copy **exactly** (no spaces)
3. Update `appsettings.json`
4. Restart application

### **Error: "Password is empty"**

✅ **Solution:**
Check `appsettings.json`:
```json
"FromPassword": "abcdefghijklmnop"  // ✅ Must have value
"FromPassword": ""      // ❌ Empty - will fail
```

### **Error: "Connection timeout"**

✅ **Solutions:**
1. Check firewall blocking port 587
2. Try port 465 with SSL
3. Check antivirus blocking SMTP

### **Gmail Still Blocking?**

Try these:
1. **Wait 10 minutes** after generating App Password
2. **Logout/Login** to Google Account
3. **Revoke old App Passwords** and create new one
4. **Switch to different email** (Outlook, SendGrid)

---

## 📋 **Complete Working Example**

### **appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=BitRaserDb;user=root;password=yourpass;"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "FromEmail": "nishus877@gmail.com",
    "FromPassword": "stjhuyzc yhpoykiq",  // ← Your actual App Password (remove spaces!)
    "FromName": "DSecure Support",
    "EnableSsl": true
  }
}
```

### **Test Steps:**

1. **Generate Gmail App Password:**
   ```
   https://myaccount.google.com/apppasswords
   ```

2. **Update appsettings.json** with 16-char password (no spaces)

3. **Restart API:**
   ```sh
   dotnet run
   ```

4. **Test Email:**
   ```
   POST /api/ForgotPassword/test-email
   { "email": "your-email@gmail.com" }
   ```

5. **Check Inbox** for test OTP email

6. **If successful, test actual flow:**
   ```
   POST /api/ForgotPassword/request-otp
   { "email": "user@example.com" }
   ```

---

## ✅ **Success Indicators**

You'll see these logs if email works:
```
📧 Email Configuration - Host: smtp.gmail.com, Port: 587, From: nishus877@gmail.com, SSL: True
📧 Attempting to send OTP email to user@example.com from nishus877@gmail.com
📧 Connecting to SMTP server...
✅ OTP email sent successfully to user@example.com
```

---

## 🎊 **Quick Fix Checklist**

- [ ] 2-Step Verification enabled in Google Account
- [ ] App Password generated (16 characters)
- [ ] Spaces removed from App Password
- [ ] `appsettings.json` updated with App Password
- [ ] Application restarted
- [ ] Test endpoint returns success
- [ ] Email received in inbox
- [ ] Actual forgot password flow tested

---

## 📞 **Still Not Working?**

### **Option A: Use Mailtrap (Recommended for Testing)**
- Sign up: https://mailtrap.io
- Free forever
- Perfect for development

### **Option B: Use Different Gmail Account**
- Create fresh Gmail account
- Enable 2-Step Verification
- Generate App Password
- Test immediately

### **Option C: Use SendGrid (Production-Ready)**
- Sign up: https://sendgrid.com
- 100 emails/day free
- More reliable than Gmail

---

**Good luck! 🚀**
