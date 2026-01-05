# Quick Reference: Simplified PATCH Endpoint

## 🎯 Endpoint
```
PATCH /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}
```

---

## ✅ Allowed Fields (5 ONLY)
| Field | Type | Example | Required |
|-------|------|---------|----------|
| `Name` | string | "John Smith" | ❌ Optional |
| `Phone` | string | "1234567890" | ❌ Optional |
| `Department` | string | "IT" | ❌ Optional |
| `Role` | string | "Manager" | ❌ Optional |
| `Status` | string | "active" | ❌ Optional |

---

## 📝 Minimal Request
```json
{
  "Name": "Updated Name"
}
```

## 📝 Full Request
```json
{
  "Name": "John Smith",
  "Phone": "1234567890",
  "Department": "IT",
"Role": "Manager",
  "Status": "active"
}
```

---

## ✅ Success Response
```json
{
  "success": true,
  "message": "Subuser updated successfully",
  "parent_email": "admin@example.com",
  "subuser_email": "john@example.com",
  "updatedFields": ["Name", "Phone"],
  "updatedBy": "admin@example.com",
  "updatedAt": "2025-01-26T10:30:00Z",
  "subuser": {
    "subuser_email": "john@example.com",
    "user_email": "admin@example.com",
    "name": "John Smith",
 "phone": "1234567890",
    "department": "IT",
    "role": "Manager",
    "status": "active"
}
}
```

---

## 🔑 Authorization
```
Header: Authorization: Bearer YOUR_JWT_TOKEN
Permission: UPDATE_SUBUSER (or be the parent user)
```

---

## 🚫 What You CANNOT Update
- Email
- Password
- Licenses
- Permissions (CanView*, CanManage*, etc.)
- Group assignments
- Max machines
- Notifications settings

**For these, use other endpoints!**

---

## 🧪 Quick Test (cURL)
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/test@example.com" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"Name":"New Name","Status":"active"}'
```

---

**Last Updated:** 2025-01-26  
**Status:** ✅ Working
