# 🔐 Hierarchical Access Control - हिंदी गाइड

## 📋 मुख्य बातें (Key Points)

### 1. **रोल पदानुक्रम** (Role Hierarchy)

```
स्तर 1: SuperAdmin← सबसे ऊपर (सब कुछ कर सकते हैं)
  ↓
स्तर 2: Admin     ← प्रशासनिक अधिकार
  ↓
स्तर 3: Manager       ← विभाग प्रबंधन
  ↓
स्तर 4: Support       ← सहायता कार्य
  ↓
स्तर 5: User          ← सामान्य उपयोगकर्ता
स्तर 6: SubUser       ← अधीनस्थ उपयोगकर्ता
```

### 2. **मुख्य नियम** (Main Rules)

#### ✅ **क्या हो सकता है** (What is Allowed)

1. **SuperAdmin**
   - पूरे system पर पूर्ण नियंत्रण
   - सभी users और subusers को manage कर सकते हैं
   - कोई भी role assign कर सकते हैं

2. **Admin**
   - Manager, Support, User, SubUser को manage कर सकते हैं
   - SuperAdmin users create/manage **नहीं** कर सकते
   - Manager, Support, User roles assign कर सकते हैं

3. **Manager**
   - Support, User, SubUser को manage कर सकते हैं
   - Admin या SuperAdmin को manage **नहीं** कर सकते
   - Support और User roles assign कर सकते हैं

4. **Support**
   - User और SubUser को manage कर सकते हैं
   - Manager या उससे ऊपर के roles को manage **नहीं** कर सकते

5. **User**
   - केवल अपने SubUsers को manage कर सकते हैं
   - नए subusers create **नहीं** कर सकते (महत्वपूर्ण!)

#### ❌ **क्या नहीं हो सकता** (What is NOT Allowed)

1. **कोई भी user अपने समान या उच्च स्तर के user को manage नहीं कर सकता**
   - Admin दूसरे Admin को manage नहीं कर सकते
   - Manager दूसरे Manager को manage नहीं कर सकते

2. **User role नए subusers create नहीं कर सकते**
   - यह एक विशेष प्रतिबंध है

3. **कोई भी user अपने से ऊपर के role assign नहीं कर सकता**
   - Admin SuperAdmin role assign नहीं कर सकते
   - Manager Admin role assign नहीं कर सकते

## 🎯 उदाहरण (Examples)

### **उदाहरण 1: Admin द्वारा User बनाना**

```http
✅ सही (CORRECT):
POST /api/EnhancedUsers
{
  "UserEmail": "manager@company.com",
  "DefaultRole": "Manager"
}
→ सफल! Admin Manager बना सकते हैं

❌ गलत (WRONG):
POST /api/EnhancedUsers
{
  "UserEmail": "superadmin@company.com",
  "DefaultRole": "SuperAdmin"
}
→ त्रुटि! Admin SuperAdmin नहीं बना सकते
```

### **उदाहरण 2: Manager द्वारा Subuser बनाना**

```http
✅ सही (CORRECT):
POST /api/EnhancedSubusers
{
  "Email": "support@company.com",
  "Role": "Support"
}
→ सफल! Manager Support role assign कर सकते हैं

❌ गलत (WRONG):
POST /api/EnhancedSubusers
{
  "Email": "admin@company.com",
  "Role": "Admin"
}
→ त्रुटि! Manager Admin role assign नहीं कर सकते
```

### **उदाहरण 3: User द्वारा Subuser बनाना**

```http
❌ नहीं हो सकता (NOT POSSIBLE):
POST /api/RoleBasedAuth/create-subuser
{
  "SubuserEmail": "subuser@company.com",
  "SubuserPassword": "password123"
}
→ त्रुटि! User role subusers नहीं बना सकते
```

## 📊 Access Matrix - किसे क्या दिखेगा

| आपका Role | आपको क्या दिखेगा (What you can see) |
|-----------|-------------------------------------|
| **SuperAdmin** | सभी users और subusers |
| **Admin** | Manager, Support, User, SubUser (SuperAdmin नहीं) |
| **Manager** | Support, User, SubUser (Admin और SuperAdmin नहीं) |
| **Support** | User और SubUser (Manager और उससे ऊपर नहीं) |
| **User** | केवल अपनी profile और अपने subusers |

## 🔍 परीक्षण परिदृश्य (Testing Scenarios)

### **Test 1: Admin का SuperAdmin बनाने की कोशिश**

```bash
# 1. Admin के रूप में login करें
POST /api/RoleBasedAuth/login
{
  "email": "admin@company.com",
  "password": "admin123"
}

# 2. SuperAdmin user बनाने की कोशिश
POST /api/EnhancedUsers
{
  "UserEmail": "superadmin2@company.com",
  "DefaultRole": "SuperAdmin"
}

# Result: 403 Forbidden
# Message: "आप 'SuperAdmin' role के साथ user नहीं बना सकते"
```

### **Test 2: User role का subuser बनाने की कोशिश**

```bash
# 1. User के रूप में login करें
POST /api/RoleBasedAuth/login
{
  "email": "user@company.com",
  "password": "user123"
}

# 2. Subuser बनाने की कोशिश
POST /api/RoleBasedAuth/create-subuser
{
  "SubuserEmail": "subuser@company.com",
  "SubuserPassword": "password123"
}

# Result: 403 Forbidden
# Message: "'User' role वाले users subusers नहीं बना सकते"
```

### **Test 3: Manager का users देखना**

```bash
# 1. Manager के रूप में login करें
POST /api/RoleBasedAuth/login
{
  "email": "manager@company.com",
  "password": "manager123"
}

# 2. सभी users देखने की कोशिश
GET /api/EnhancedUsers

# Result: केवल Support, User, SubUser दिखेंगे
# SuperAdmin और Admin नहीं दिखेंगे
```

## 🚨 त्रुटि संदेश (Error Messages)

### **1. Role Assignment त्रुटि**
```json
{
  "success": false,
  "message": "आप 'SuperAdmin' role assign नहीं कर सकते",
  "detail": "आप केवल अपने से नीचे के roles ही assign कर सकते हैं"
}
```

### **2. Subuser Creation त्रुटि**
```json
{
  "success": false,
  "message": "आप subusers नहीं बना सकते",
  "detail": "'User' role वाले users को subusers बनाने की अनुमति नहीं है"
}
```

### **3. Management त्रुटि**
```json
{
  "error": "आप इस user को manage नहीं कर सकते"
}
```

## 💡 महत्वपूर्ण बातें (Important Points)

### **1. Hierarchy Level का मतलब**
- **छोटी संख्या = ज्यादा शक्ति**
  - Level 1 (SuperAdmin) सबसे शक्तिशाली
  - Level 6 (SubUser) सबसे कम शक्तिशाली

### **2. Same Level Restriction**
- कोई भी user अपने समान level के user को manage नहीं कर सकता
- उदाहरण: एक Admin दूसरे Admin को manage नहीं कर सकता

### **3. User Role की विशेष सीमा**
- User role वाले users subusers नहीं बना सकते
- यह केवल User role के लिए है

### **4. SuperAdmin की असीमित शक्ति**
- SuperAdmin सभी restrictions को bypass कर सकते हैं
- पूरे system पर पूर्ण नियंत्रण

## 🔧 कॉन्फ़िगरेशन (Configuration)

### **Database में Roles**
```sql
-- Hierarchy Levels
SuperAdmin: 1 (सबसे ऊपर)
Admin: 2
Manager: 3
Support: 4
User: 5
SubUser: 6 (सबसे नीचे)
```

### **Permissions**
- **SuperAdmin**: सभी permissions
- **Admin**: FullAccess छोड़कर सब
- **Manager**: Management permissions
- **Support**: सीमित management
- **User**: केवल basic permissions

## 📖 सारांश (Summary)

### **याद रखने योग्य बातें:**

1. ✅ आप केवल अपने से **नीचे** के users को manage कर सकते हैं
2. ✅ आप केवल अपने से **नीचे** के roles assign कर सकते हैं
3. ✅ User role subusers नहीं बना सकते
4. ✅ Admin SuperAdmin users नहीं बना सकते
5. ✅ SuperAdmin सब कुछ कर सकते हैं

### **फॉर्मूला:**
```
Manager Level (3) < Target Level (4) = ✅ Access Granted
Manager Level (3) < Target Level (2) = ❌ Access Denied
Manager Level (3) < Target Level (3) = ❌ Access Denied (Same Level)
```

---

## 🎉 यह System आपको क्या देता है?

1. **सुरक्षा** - केवल authorized access
2. **स्पष्टता** - साफ hierarchy और जिम्मेदारी
3. **Automatic Filtering** - API responses automatically filter होते हैं
4. **Audit Trail** - सब कुछ track होता है

**अब आपका system पूरी तरह secure और hierarchical है!** 🚀
