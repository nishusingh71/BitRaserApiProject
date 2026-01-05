# ⚡ Quick Reference: UserActivity Login/Logout APIs

## 🎯 **7 NEW ENDPOINTS**

### 1. **Record Login** 
```
POST /api/UserActivity/record-login?email={email}&userType={user|subuser}
```
✅ Logs user/subuser login with server time

---

### 2. **Record Logout**
```
POST /api/UserActivity/record-logout?email={email}&userType={user|subuser}
```
✅ Logs user/subuser logout with server time

---

### 3. **Get Status**
```
GET /api/UserActivity/status/{email}?userType={user|subuser}
```
✅ Returns login/logout times and online/offline status

---

### 4. **All Users Status**
```
GET /api/UserActivity/all-users-status
```
✅ Lists all users with online/offline status

---

### 5. **All Subusers Status**
```
GET /api/UserActivity/all-subusers-status
```
✅ Lists all subusers with online/offline status

---

### 6. **Parent's Subusers Status**
```
GET /api/UserActivity/parent/{parentEmail}/subusers-status
```
✅ Shows specific parent's subusers status

---

### 7. **Update All Status (Batch)**
```
POST /api/UserActivity/update-all-status
```
✅ Updates all users/subusers status based on activity

---

## 📊 **Status Logic**

### **Online:**
- Last login within 5 minutes
- No logout OR logout before last login

### **Offline:**
- Last login > 5 minutes ago
- Logout after last login
- Never logged in

---

## ⚡ **Quick Examples**

### Login:
```bash
curl -X POST "http://localhost:4000/api/UserActivity/record-login?email=admin@example.com&userType=user" \
  -H "Authorization: Bearer TOKEN"
```

### Logout:
```bash
curl -X POST "http://localhost:4000/api/UserActivity/record-logout?email=admin@example.com&userType=user" \
  -H "Authorization: Bearer TOKEN"
```

### Get Status:
```bash
curl -X GET "http://localhost:4000/api/UserActivity/status/admin@example.com" \
  -H "Authorization: Bearer TOKEN"
```

### All Users:
```bash
curl -X GET "http://localhost:4000/api/UserActivity/all-users-status" \
  -H "Authorization: Bearer TOKEN"
```

---

## ✅ **Response Format**

```json
{
  "success": true,
  "email": "admin@example.com",
  "userType": "user",
  "last_login": "2025-01-26T12:30:45Z",
  "last_logout": "2025-01-26T14:30:45Z",
  "status": "offline",
  "server_time": "2025-01-26T15:00:00Z"
}
```

---

**✅ Server time se accurate tracking!** 🎉
