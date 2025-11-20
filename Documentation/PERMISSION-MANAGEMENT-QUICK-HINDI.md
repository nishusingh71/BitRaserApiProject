# 🔐 Permission Management - Quick Reference (Hindi)

## 🎯 मुख्य बातें

### **कौन क्या कर सकता है?**

| Role | Permission Modify कर सकते हैं? | किन Roles के लिए? |
|------|-------------------------------|-------------------|
| **SuperAdmin** | ✅ हाँ | सभी roles (SuperAdmin, Admin, Manager, Support, User) |
| **Admin** | ✅ हाँ | Manager, Support, User, SubUser (SuperAdmin नहीं) |
| **Manager** | ❌ नहीं | कोई भी नहीं |
| **Support** | ❌ नहीं | कोई भी नहीं |
| **User** | ❌ नहीं | कोई भी नहीं |

---

## 📋 API Endpoints (सरल हिंदी में)

### **1. Role की Permissions देखें**

```
GET /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {token}
```

**Response:**
```json
{
  "roleName": "Manager",
  "permissions": [
    "UserManagement",
    "ReportAccess",
    "MachineManagement"
  ]
}
```

✅ **कौन कर सकता है:** सभी users

---

### **2. सभी Available Permissions देखें**

```
GET /api/RoleBasedAuth/permissions/all
Authorization: Bearer {token}
```

✅ **कौन कर सकता है:** सभी users

---

### **3. Role में Permission जोड़ें**

```
POST /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "PermissionName": "DELETE_USER"
}
```

**Response:**
```json
{
  "success": true,
"message": "Permission 'DELETE_USER' added to role 'Manager'"
}
```

✅ **कौन कर सकता है:** SuperAdmin, Admin (नीचे के roles के लिए)

---

### **4. Role से Permission हटाएं**

```
DELETE /api/RoleBasedAuth/roles/Manager/permissions/DELETE_USER
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Permission 'DELETE_USER' removed from role 'Manager'"
}
```

✅ **कौन कर सकता है:** SuperAdmin, Admin

---

### **5. Role की सभी Permissions बदलें**

```
PUT /api/RoleBasedAuth/roles/Manager/permissions
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "PermissionNames": [
    "UserManagement",
    "ReportAccess",
    "DELETE_USER"
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Permissions updated for role 'Manager'",
  "permissions": [
    "UserManagement",
    "ReportAccess",
    "DELETE_USER"
  ]
}
```

✅ **कौन कर सकता है:** SuperAdmin, Admin

---

## 🔍 उदाहरण (Examples)

### **उदाहरण 1: Admin Manager को DELETE_USER permission दे रहा है**

```bash
# Step 1: Admin के रूप में login करें
POST /api/RoleBasedAuth/login
{
  "email": "admin@company.com",
  "password": "admin123"
}

# Step 2: Manager की current permissions देखें
GET /api/RoleBasedAuth/roles/Manager/permissions
# Result: ["UserManagement", "ReportAccess"]

# Step 3: DELETE_USER permission जोड़ें
POST /api/RoleBasedAuth/roles/Manager/permissions
{
  "PermissionName": "DELETE_USER"
}
# Result: Success!

# Step 4: Updated permissions देखें
GET /api/RoleBasedAuth/roles/Manager/permissions
# Result: ["UserManagement", "ReportAccess", "DELETE_USER"]
```

**Result:** ✅ अब सभी Manager users DELETE_USER कर सकते हैं!

---

### **उदाहरण 2: Admin SuperAdmin की permissions बदलने की कोशिश (Fail होगी)**

```bash
# Admin SuperAdmin role में permission add करने की कोशिश
POST /api/RoleBasedAuth/roles/SuperAdmin/permissions
{
  "PermissionName": "SomePermission"
}

# Result: 403 Forbidden
{
  "message": "You cannot modify permissions for role 'SuperAdmin'"
}
```

**Result:** ❌ Admin SuperAdmin की permissions नहीं बदल सकते!

---

### **उदाहरण 3: Support Role की सभी Permissions बदलें**

```bash
# SuperAdmin के रूप में Support role की सभी permissions replace करें
PUT /api/RoleBasedAuth/roles/Support/permissions
{
  "PermissionNames": [
    "ViewOnly",
    "READ_USER",
    "READ_REPORT",
    "READ_LOG"
  ]
}

# Result: Success!
# सभी पुरानी permissions हट गईं, नई permissions assign हो गईं
```

---

## 🎯 Permission Change होने के बाद क्या करें?

### **Option 1: फिर से Login करें**

```bash
POST /api/RoleBasedAuth/login
{
  "email": "manager@company.com",
  "password": "password123"
}
```

Response में updated permissions आएंगी!

---

### **Option 2: Current Permissions Check करें**

```bash
GET /api/RoleBasedAuth/my-permissions
Authorization: Bearer {current_token}
```

Updated permissions तुरंत दिखेंगी (बिना re-login के)!

---

## 📊 Common Permissions (Hindi में)

### **User Management**
- `UserManagement` - पूरा user management
- `CREATE_USER` - नए user बनाएं
- `UPDATE_USER` - User details update करें
- `DELETE_USER` - Users delete करें
- `CREATE_SUBUSER` - Subusers बनाएं

### **Reports**
- `ReportAccess` - Reports देखें और manage करें
- `CREATE_REPORT` - नई reports बनाएं
- `UPDATE_REPORT` - Reports update करें
- `EXPORT_REPORTS` - Reports export करें

### **Machines & Licenses**
- `MachineManagement` - Machines manage करें
- `LicenseManagement` - Licenses manage करें
- `ADD_MACHINE` - नई machines add करें
- `ValidateLicense` - License validate करें

### **System**
- `FullAccess` - पूरा system access (SuperAdmin only)
- `SystemLogs` - System logs देखें
- `ViewOnly` - केवल देख सकते हैं

---

## 🚨 Common Errors और Solutions

### **Error 1: "You cannot modify permissions for role 'SuperAdmin'"**

**Meaning:** आप SuperAdmin role की permissions नहीं बदल सकते

**Solution:** केवल SuperAdmin ही SuperAdmin role की permissions बदल सकते हैं

---

### **Error 2: Permission Added but User Can't Access**

**Solution:** User को फिर से login करना होगा या current permissions check करने होंगे

```bash
# Re-login
POST /api/RoleBasedAuth/login

# या

# Current permissions check करें
GET /api/RoleBasedAuth/my-permissions
```

---

### **Error 3: 403 Forbidden**

**Meaning:** आपके पास permission modify करने की authority नहीं है

**Check:**
1. क्या आप SuperAdmin या Admin हैं?
2. क्या target role आपसे नीचे का है?
3. क्या आपके पास सही token है?

---

## ✅ Quick Checklist

### **Permission Add करने से पहले:**

- [ ] क्या आप SuperAdmin या Admin हैं?
- [ ] क्या target role आपसे नीचे का है?
- [ ] क्या permission name सही है?
- [ ] क्या permission already exist नहीं करता?

### **Permission Add करने के बाद:**

- [ ] Verify: Role की permissions देखें
- [ ] Test: User को re-login करके test करें
- [ ] Document: Change को document करें
- [ ] Notify: Affected users को बताएं

---

## 📝 Summary (सारांश)

### **मुख्य बातें:**

1. ✅ **SuperAdmin** - सभी roles की permissions बदल सकते हैं
2. ✅ **Admin** - Manager, Support, User की permissions बदल सकते हैं
3. ❌ **Others** - Permission modify नहीं कर सकते
4. ✅ **Immediate Effect** - Re-login के बाद तुरंत लागू होता है
5. ✅ **Safe** - गलत changes से protect करता है

### **Quick Commands:**

```bash
# Permissions देखें
GET /api/RoleBasedAuth/roles/Manager/permissions

# Permission add करें
POST /api/RoleBasedAuth/roles/Manager/permissions
{"PermissionName": "DELETE_USER"}

# Permission हटाएं
DELETE /api/RoleBasedAuth/roles/Manager/permissions/DELETE_USER

# सभी permissions replace करें
PUT /api/RoleBasedAuth/roles/Manager/permissions
{"PermissionNames": ["Permission1", "Permission2"]}
```

---

## 🎉 अब आप तैयार हैं!

अब आप आसानी से roles की permissions manage कर सकते हैं:

1. ✅ देखें कि किस role की क्या permissions हैं
2. ✅ नई permissions add करें
3. ✅ पुरानी permissions हटाएं
4. ✅ पूरी permission list replace करें

**सब कुछ API के through - कोई database changes की जरूरत नहीं!** 🚀
