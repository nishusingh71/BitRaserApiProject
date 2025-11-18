# ⚡ Quick Reference: Request Body Only

## 🎯 Endpoint
```
PATCH /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}
```

---

## 📝 Request Body (JSON Only)

### Single Field Update:
```json
{
  "Name": "New Name"
}
```

### Multiple Fields Update:
```json
{
  "Name": "John Smith",
  "Phone": "1234567890",
  "Status": "active"
}
```

### All Fields Update:
```json
{
  "Name": "John Smith",
  "Phone": "1234567890",
  "Department": "IT Department",
  "Role": "Manager",
  "Status": "active"
}
```

---

## ⚡ Examples

### Update Name Only:
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"Name":"John Updated"}'
```

### Update Phone Only:
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"Phone":"9876543210"}'
```

### Update Multiple:
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"Name":"John","Phone":"123","Status":"active"}'
```

---

## ✅ Allowed Fields (5 Only)

| Field | Example |
|-------|---------|
| `Name` | "John Smith" |
| `Phone` | "1234567890" |
| `Department` | "IT Department" |
| `Role` | "Manager" |
| `Status` | "active" or "inactive" |

---

## 📊 Response
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

**✅ Ek field update karo ya multiple - dono kaam karenge!** 🎉
