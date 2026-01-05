# 🚀 Enhanced Subusers - Quick Reference

## 📋 **Subuser में Name और Roles - Quick Guide**

### **✅ हर Response में ये Fields होते हैं:**

```json
{
  "subuser_id": 1,
  "subuser_email": "user@example.com",
  "user_email": "parent@example.com",
  
  // ✅ USER INFORMATION
  "name": "John Doe",   // Subuser का नाम
  "phone": "+1234567890",    // Phone number
  "jobTitle": "Developer",   // Job title
  "department": "IT",        // Department

  // ✅ ROLES INFORMATION
  "roles": [
    {
      "roleId": 5,
      "roleName": "SubUser",
      "description": "Basic subuser access",
      "hierarchyLevel": 6,
      "assignedAt": "2024-01-22T10:00:00Z",
      "assignedBy": "parent@example.com"
  }
  ],
  
  // ✅ PERMISSIONS FROM ROLES
  "permissions": [
    "VIEW_OWN_MACHINES",
  "VIEW_OWN_REPORTS"
  ]
}
```

---

## 🎯 **Common Operations**

### **1. Get All Subusers with Names & Roles**
```http
GET /api/EnhancedSubusers
GET /api/EnhancedSubusers?name=John
GET /api/EnhancedSubusers?department=IT
GET /api/EnhancedSubusers?role=team_member
```

### **2. Get Single Subuser (Full Details)**
```http
GET /api/EnhancedSubusers/by-email/john@example.com
```

### **3. Create Subuser with Name**
```http
POST /api/EnhancedSubusers
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "Pass@123",
  "phone": "+1234567890",
  "jobTitle": "Developer",
  "department": "IT"
}
```

### **4. Update Subuser Name**
```http
PUT /api/EnhancedSubusers/john@example.com
{
  "name": "John Smith",
  "jobTitle": "Senior Developer"
}
```

### **5. Assign Role**
```http
POST /api/EnhancedSubusers/john@example.com/assign-role
{
  "roleName": "Manager"
}
```

### **6. Remove Role**
```http
DELETE /api/EnhancedSubusers/john@example.com/remove-role/Manager
```

### **7. Get Statistics (with Role Distribution)**
```http
GET /api/EnhancedSubusers/statistics
GET /api/EnhancedSubusers/statistics?parentEmail=parent@example.com
```

---

## 🔍 **Available Filters**

| Filter | Example | Description |
|--------|---------|-------------|
| `name` | `?name=John` | Filter by name |
| `subuserEmail` | `?subuserEmail=john@` | Filter by email |
| `department` | `?department=IT` | Filter by department |
| `role` | `?role=team_member` | Filter by role |
| `status` | `?status=active` | Filter by status |
| `page` | `?page=0` | Page number |
| `pageSize` | `?pageSize=50` | Items per page |

**Combined Example:**
```http
GET /api/EnhancedSubusers?status=active&department=IT&page=0&pageSize=50
```

---

## 📊 **Statistics Response Includes:**

```json
{
  "totalSubusers": 10,
  "activeSubusers": 8,
  "verifiedEmails": 7,
  
  "roleDistribution": [     // ✅ Role distribution
    { "roleName": "SubUser", "count": 10 },
    { "roleName": "Manager", "count": 2 }
  ],
  
  "departmentDistribution": [  // ✅ Department distribution
    { "department": "IT", "count": 5 },
    { "department": "Support", "count": 3 }
  ],
  
  "recentSubusers": [  // ✅ Recent with names
    {
      "subuser_email": "john@example.com",
      "name": "John Doe",
      "roles": ["SubUser", "Manager"],
      "createdAt": "2024-01-22T10:00:00Z"
    }
  ]
}
```

---

## 💻 **Frontend Examples**

### **Display Subuser Card**
```javascript
// Fetch subuser
const subuser = await fetch('/api/EnhancedSubusers/by-email/john@example.com', {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());

// Display
console.log(`Name: ${subuser.name}`);
console.log(`Email: ${subuser.subuser_email}`);
console.log(`Department: ${subuser.department}`);
console.log(`Roles: ${subuser.roles.map(r => r.roleName).join(', ')}`);
```

### **Create Subuser Form**
```javascript
const newSubuser = {
  name: document.getElementById('name').value,
  email: document.getElementById('email').value,
  password: document.getElementById('password').value,
  phone: document.getElementById('phone').value,
  jobTitle: document.getElementById('jobTitle').value,
  department: document.getElementById('department').value,
  role: document.getElementById('role').value
};

await fetch('/api/EnhancedSubusers', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(newSubuser)
});
```

### **Display Role Badges**
```javascript
subuser.roles.forEach(role => {
  const badge = document.createElement('span');
  badge.className = `badge badge-${role.hierarchyLevel <= 3 ? 'primary' : 'secondary'}`;
  badge.textContent = role.roleName;
  container.appendChild(badge);
});
```

---

## 🔒 **Access Control**

### **Who Can Do What?**

| Operation | User | Admin | SuperAdmin |
|-----------|------|-------|------------|
| View own subusers | ✅ | ✅ | ✅ |
| View all subusers | ❌ | ✅ | ✅ |
| Create subuser | ✅ | ✅ | ✅ |
| Update own subuser | ✅ | ✅ | ✅ |
| Update any subuser | ❌ | ✅ | ✅ |
| Assign roles | ✅* | ✅ | ✅ |
| Delete own subuser | ✅ | ✅ | ✅ |
| Delete any subuser | ❌ | ✅ | ✅ |
| View statistics | ✅ | ✅ | ✅ |

*✅* = Only to own subusers

---

## 📝 **Key Points**

1. ✅ **हर subuser में name automatically track होता है**
2. ✅ **Roles की complete information मिलती है**
3. ✅ **Permissions automatically calculate होती हैं**
4. ✅ **Department और job title tracking**
5. ✅ **Role assignment history maintain होती है**
6. ✅ **Filtering by name, department, role possible**
7. ✅ **Statistics में role distribution शामिल है**

---

## 🎯 **Default Behavior**

### **On Subuser Creation:**
- ✅ Automatically **SubUser** role assigned
- ✅ Status set to **active**
- ✅ Email verification set to **false**
- ✅ Default **maxMachines = 5**
- ✅ Notifications enabled by default

### **On Role Assignment:**
- ✅ Multiple roles can be assigned
- ✅ Assignment history tracked
- ✅ Permissions automatically calculated
- ✅ Duplicate roles prevented

---

## ❓ **Common Questions**

**Q: क्या subuser के multiple roles हो सकते हैं?**  
A: ✅ हां! Multiple roles assign कर सकते हैं।

**Q: क्या name field required है?**  
A: ✅ हां! Create करते समय name provide करना होगा।

**Q: Permissions कैसे decide होती हैं?**  
A: ✅ सभी assigned roles की permissions automatically combine होती हैं।

**Q: क्या department filtering available है?**  
A: ✅ हां! `?department=IT` से filter कर सकते हैं।

**Q: क्या role distribution देख सकते हैं?**  
A: ✅ हां! `/statistics` endpoint में complete distribution है।

---

## 🎉 **Summary**

**EnhancedSubusersController में अब:**
- ✅ Complete user profile (name, phone, job title, department)
- ✅ Full roles information with assignment history
- ✅ Automatic permission calculation
- ✅ Advanced filtering options
- ✅ Comprehensive statistics with role distribution

**सब कुछ ready है! 🚀**

---

**For detailed examples, see:** `ENHANCED_SUBUSERS_WITH_NAMES_AND_ROLES.md`
