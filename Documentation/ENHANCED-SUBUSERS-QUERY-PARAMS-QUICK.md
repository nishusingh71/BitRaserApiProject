# ⚡ Quick Reference: Query Parameters for Single-Field Updates

## 🎯 Endpoint
```
PATCH /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}
```

---

## 📝 Query Parameters

| Parameter | Field | Example |
|-----------|-------|---------|
| `?subuser_name=` | Name | `?subuser_name=John%20Smith` |
| `?subuser_phone=` | Phone | `?subuser_phone=1234567890` |
| `?subuser_department=` | Department | `?subuser_department=IT` |
| `?subuser_role=` | Role | `?subuser_role=Manager` |
| `?subuser_status=` | Status | `?subuser_status=active` |

---

## ⚡ Quick Examples

### Update Name Only:
```
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_name=New%20Name
```

### Update Phone Only:
```
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_phone=9876543210
```

### Update Status Only:
```
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_status=inactive
```

### Update Multiple Fields:
```
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_name=John&subuser_phone=123&subuser_status=active
```

---

## 🔧 cURL Template

```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}?subuser_name={value}" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## ✅ Response
```json
{
  "success": true,
  "updatedFields": ["Name"],
  "subuser": {
    "name": "Updated Value",
    ...
  }
}
```

---

## 📊 Two Methods

### Method 1: Query Parameters (NEW!)
```
?subuser_name=Value
```
✅ No JSON body needed  
✅ Perfect for single fields  

### Method 2: JSON Body (Original)
```json
{
  "Name": "Value"
}
```
✅ Perfect for multiple fields

---

**Both methods work! Choose what's easier for you!** 🎉
