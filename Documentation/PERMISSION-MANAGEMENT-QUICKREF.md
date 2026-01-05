# 🎯 Permission Management - Quick Reference Card

## Super Quick Commands

### View Permissions
```bash
# किसी role की permissions देखें
GET /api/RoleBasedAuth/roles/Manager/permissions

# सभी available permissions देखें
GET /api/RoleBasedAuth/permissions/all

# अपनी current permissions देखें
GET /api/RoleBasedAuth/my-permissions
```

### Modify Permissions (Admin/SuperAdmin only)
```bash
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

## Who Can Do What? (एक नज़र में)

| Role | Can Modify? | Which Roles? |
|------|------------|--------------|
| **SuperAdmin** | ✅ YES | All roles |
| **Admin** | ✅ YES | Manager, Support, User, SubUser |
| **Others** | ❌ NO | None |

---

## Common Use Cases

### 1. Manager को DELETE_USER permission दें
```bash
POST /api/RoleBasedAuth/roles/Manager/permissions
{"PermissionName": "DELETE_USER"}
```

### 2. Support role की सभी permissions update करें
```bash
PUT /api/RoleBasedAuth/roles/Support/permissions
{
  "PermissionNames": [
    "ViewOnly",
    "READ_USER",
    "READ_REPORT"
  ]
}
```

### 3. किसी permission को हटाएं
```bash
DELETE /api/RoleBasedAuth/roles/Manager/permissions/DELETE_USER
```

---

## Error Messages (Hindi में)

| Error | Meaning | Solution |
|-------|---------|----------|
| 403 Forbidden | आपके पास authority नहीं | Admin/SuperAdmin token use करें |
| "Cannot modify SuperAdmin" | Admin SuperAdmin modify नहीं कर सकते | केवल SuperAdmin ही कर सकते हैं |
| "Permission already exists" | Permission पहले से है | Check current permissions |
| 400 Bad Request | Invalid request | Permission name check करें |

---

## Testing Quick Steps

### Swagger में Test करें:
1. 🔒 Click "Authorize" button
2. Enter Admin/SuperAdmin token
3. Try endpoints
4. Verify responses

### Expected Results:
- ✅ View permissions → 200 OK
- ✅ Admin adds to Manager → 200 OK
- ❌ Admin adds to SuperAdmin → 403 Forbidden
- ❌ Manager adds anything → 403 Forbidden

---

## Permission Categories (Most Used)

### User Management
- `UserManagement` ← पूरा control
- `CREATE_USER`, `UPDATE_USER`, `DELETE_USER`
- `CREATE_SUBUSER`, `UPDATE_SUBUSER`, `DELETE_SUBUSER`

### Reports
- `ReportAccess` ← Reports manage करें
- `CREATE_REPORT`, `READ_REPORT`, `UPDATE_REPORT`
- `EXPORT_REPORTS`

### System
- `FullAccess` ← SuperAdmin only
- `SystemLogs`, `ViewOnly`

---

## Checklist Before Modifying

- [ ] क्या आप Admin/SuperAdmin हैं?
- [ ] क्या target role आपसे नीचे है?
- [ ] क्या permission name सही है?
- [ ] Test environment में try किया?

---

## After Modifying

- [ ] Verify: Role की permissions check करें
- [ ] Test: User को test करने को बोलें
- [ ] Document: Change को note करें
- [ ] Monitor: Logs check करें

---

**Print this card and keep it handy!** 📋
