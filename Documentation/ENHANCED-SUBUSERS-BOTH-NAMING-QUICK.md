# ⚡ Quick Reference: Both Naming Conventions

## 🎯 Endpoint
```
PATCH /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}
```

---

## 📝 Two Ways to Update

### Method 1: snake_case ⭐
```json
{
  "subuser_name": "John Smith",
  "subuser_phone": "1234567890",
  "subuser_role": "Manager"
}
```

### Method 2: CamelCase
```json
{
  "Name": "John Smith",
  "Phone": "1234567890",
  "Role": "Manager"
}
```

### Method 3: Mixed
```json
{
  "subuser_name": "John Smith",
  "Phone": "1234567890",
  "Department": "IT"
}
```

---

## 📊 Field Names

| snake_case | CamelCase | Updates |
|------------|-----------|---------|
| `subuser_name` | `Name` | Name |
| `subuser_phone` | `Phone` | Phone |
| `subuser_role` | `Role` | Role |
| - | `Department` | Department |
| - | `Status` | Status |

---

## ⚡ Examples

### Update Name (snake_case):
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"subuser_name":"John Updated"}'
```

### Update Phone (snake_case):
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"subuser_phone":"9876543210"}'
```

### Update Role (snake_case):
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"subuser_role":"Senior Developer"}'
```

### Update Multiple (Mixed):
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "subuser_name":"John",
    "subuser_phone":"123",
    "Department":"IT",
    "Status":"active"
  }'
```

---

## 🎯 Priority

```json
{
  "subuser_name": "This wins",
  "Name": "This is ignored"
}
```
**Result:** ✅ snake_case takes priority

---

## ✅ All Valid

```json
// 1. Only snake_case
{"subuser_name":"John","subuser_phone":"123"}

// 2. Only CamelCase
{"Name":"John","Phone":"123"}

// 3. Mixed
{"subuser_name":"John","Phone":"123","Status":"active"}
```

---

**✅ Dono naming conventions kaam karenge!** 🎉
- snake_case: `subuser_name`, `subuser_phone`, `subuser_role`
- CamelCase: `Name`, `Phone`, `Department`, `Role`, `Status`
