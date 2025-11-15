# ✅ Forgot Password - Production Deployment Checklist

## 🎯 **Quick Fix for Production**

### **Problem:** Forgot Password locally kaam kar raha hai but production mein nahi

### **Solution:** 3 steps mein fix karo

---

## 📋 **Step 1: Gmail App Password Setup** (5 minutes)

```sh
# 1. Enable 2-Step Verification
https://myaccount.google.com/security

# 2. Generate App Password
https://myaccount.google.com/apppasswords

# 3. Copy 16-character password (NO SPACES!)
# Example: abcdefghijklmnop
```

---

## 📋 **Step 2: Update Production appsettings.json**

**File:** `appsettings.Production.json`

```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
  "FromEmail": "nishus877@gmail.com",
    "FromPassword": "YOUR-16-CHAR-APP-PASSWORD-HERE",
    "FromName": "Dsecure Support",
    "EnableSsl": true,
    "Timeout": 60000
  }
}
```

---

## 📋 **Step 3: Deploy & Test**

### **Deploy:**
```sh
# 1. Build project
dotnet publish -c Release

# 2. Upload to server

# 3. Restart application
sudo systemctl restart your-app
# OR
az webapp restart --name YourAppName --resource-group YourResourceGroup
```

### **Test:**
```sh
# Test email configuration
curl -X POST "https://your-domain.com/api/ForgotPassword/test-email" \
  -H "Content-Type: application/json" \
  -d '{"email":"your-test-email@gmail.com"}'

# Expected Response:
# {
# "success": true,
#   "message": "Test email sent successfully!",
#   "testOtp": "123456"
# }
```

---

## 🔧 **Alternative: Environment Variables (For Azure/Docker)**

### **Azure App Service:**
```sh
# Portal > App Service > Configuration > Application settings

EmailSettings__SmtpHost = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__FromEmail = nishus877@gmail.com
EmailSettings__FromPassword = YOUR_APP_PASSWORD
EmailSettings__FromName = Dsecure Support
EmailSettings__EnableSsl = true
EmailSettings__Timeout = 60000
```

### **Docker:**
```yaml
# docker-compose.yml
environment:
  - EmailSettings__SmtpHost=smtp.gmail.com
  - EmailSettings__SmtpPort=587
  - EmailSettings__FromEmail=nishus877@gmail.com
  - EmailSettings__FromPassword=YOUR_APP_PASSWORD
  - EmailSettings__FromName=Dsecure Support
  - EmailSettings__EnableSsl=true
  - EmailSettings__Timeout=60000
```

---

## 🧪 **Full API Test**

```sh
# 1. Request OTP
curl -X POST "https://your-domain.com/api/ForgotPassword/request-otp" \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com"}'

# 2. Check email for OTP (6 digits)

# 3. Verify OTP
curl -X POST "https://your-domain.com/api/ForgotPassword/verify-otp" \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","otp":"123456"}'

# 4. Reset Password
curl -X POST "https://your-domain.com/api/ForgotPassword/reset-password" \
  -H "Content-Type: application/json" \
  -d '{
    "email":"user@example.com",
    "otp":"123456",
    "newPassword":"NewPassword@123"
  }'
```

---

## ⚠️ **Common Production Issues**

### **Issue 1: SMTP Authentication Failed**
```
❌ Status: 535
```

**Fix:**
- ✅ Verify Gmail App Password is 16 characters
- ✅ Remove all spaces from password
- ✅ Check 2-Step Verification is enabled
- ✅ Generate new App Password

---

### **Issue 2: Connection Timeout**
```
❌ The operation has timed out
```

**Fix:**
```sh
# Check if port 587 is open
telnet smtp.gmail.com 587

# If fails, check firewall
sudo ufw allow out 587/tcp

# Or increase timeout in appsettings.json
"Timeout": 60000  // 60 seconds
```

---

### **Issue 3: Email Not Received**
```
✅ API returns success but email not received
```

**Fix:**
1. Check spam/junk folder
2. Verify email address is correct
3. Check logs: `/var/log/your-app/`
4. Test with different email provider

---

## 📊 **Success Indicators**

### **Logs (Production):**
```
📧 Email Configuration [Environment: Production]
   Host: smtp.gmail.com:587
   From: nishus877@gmail.com
   SSL: True, Timeout: 60000ms
📧 Sending OTP email to user@example.com...
📧 Attempt 1/3 - Sending email...
✅ OTP email sent successfully to user@example.com
```

### **User Experience:**
- ✅ OTP received within 30 seconds
- ✅ Email formatted correctly
- ✅ OTP is 6 digits
- ✅ Valid for 10 minutes

---

## 🎯 **Quick Troubleshooting**

```sh
# 1. Check email config
curl https://your-domain.com/api/ForgotPassword/email-config-check

# 2. Send test email
curl -X POST https://your-domain.com/api/ForgotPassword/test-email \
  -H "Content-Type: application/json" \
  -d '{"email":"your-email@gmail.com"}'

# 3. View logs
tail -f /var/log/your-app/application.log

# 4. Check SMTP connectivity
telnet smtp.gmail.com 587
```

---

## ✅ **Final Checklist**

Before going live:

- [ ] ✅ Gmail App Password generated (16 chars)
- [ ] ✅ 2-Step Verification enabled
- [ ] ✅ `appsettings.Production.json` updated
- [ ] ✅ App Password has NO spaces
- [ ] ✅ Timeout set to 60000 (60 seconds)
- [ ] ✅ Test email endpoint successful
- [ ] ✅ Production logs show success
- [ ] ✅ Test OTP received in inbox
- [ ] ✅ Full forgot password flow tested
- [ ] ✅ Application restarted

---

## 🚀 **Deploy Command Summary**

```sh
# Local → Production

# 1. Build
dotnet publish -c Release -o ./publish

# 2. Upload (example: SCP)
scp -r ./publish/* user@server:/var/www/your-app/

# 3. Restart
ssh user@server 'sudo systemctl restart your-app'

# 4. Test
curl -X POST "https://your-domain.com/api/ForgotPassword/test-email" \
  -H "Content-Type: application/json" \
  -d '{"email":"your-email@gmail.com"}'

# 5. Check logs
ssh user@server 'tail -f /var/log/your-app/application.log'
```

---

## 📞 **Still Not Working?**

### **Option 1: Use SendGrid (Recommended for Production)**
```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.sendgrid.net",
    "SmtpPort": "587",
"FromEmail": "noreply@yourdomain.com",
    "FromPassword": "YOUR_SENDGRID_API_KEY",
    "EnableSsl": true
  }
}
```

Signup: https://sendgrid.com

### **Option 2: Use AWS SES**
```json
{
  "EmailSettings": {
    "SmtpHost": "email-smtp.us-east-1.amazonaws.com",
    "SmtpPort": "587",
    "FromEmail": "noreply@yourdomain.com",
    "FromPassword": "YOUR_AWS_SES_PASSWORD"
  }
}
```

---

**Last Updated:** 2025-01-14  
**Status:** ✅ **PRODUCTION READY**  
**Build:** ✅ **SUCCESSFUL**

**Ab production mein kaam karega! 🎉**
