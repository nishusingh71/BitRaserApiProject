# 🎯 Hierarchical Access Control - Quick Reference Card

## Role Hierarchy (Smaller Number = Higher Power)
```
1. SuperAdmin ← सबसे शक्तिशाली
2. Admin      ← SuperAdmin नहीं बना सकते
3. Manager  ← Admin नहीं बना सकते
4. Support    ← Manager नहीं बना सकते
5. User       ← Subuser नहीं बना सकते ⚠️
6. SubUser    ← सबसे कम power
```

## ✅ Access Rules (Quick Check)

### Can Create Users?
| Your Role | SuperAdmin | Admin | Manager | Support | User | SubUser |
|-----------|------------|-------|---------|---------|------|---------|
| SuperAdmin | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Admin | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| Manager | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Support | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| User | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

### Can Create Subusers?
- ✅ SuperAdmin, Admin, Manager, Support
- ❌ User (Important!)

### Can Assign Roles?
| Your Role | Can Assign To |
|-----------|--------------|
| SuperAdmin | Any role |
| Admin | Manager, Support, User, SubUser |
| Manager | Support, User, SubUser |
| Support | User, SubUser |
| User | SubUser (if can create) |

## 🔍 Quick Validation Formula

```javascript
// Can Manager manage Target?
if (managerLevel < targetLevel) {
  return "✅ YES";
} else {
  return "❌ NO";
}

// Examples:
Admin (2) < Manager (3) → ✅ YES
Admin (2) < Admin (2) → ❌ NO (Same Level!)
Manager (3) < Admin (2) → ❌ NO (Higher Level!)
```

## 🚨 Common Errors & Fixes

### Error 1: "You cannot create user with role 'SuperAdmin'"
**Cause:** Admin trying to create SuperAdmin user  
**Fix:** Only SuperAdmin can create other SuperAdmins

### Error 2: "You cannot create subusers"
**Cause:** User role trying to create subuser  
**Fix:** Assign higher role (Support, Manager, Admin) to user

### Error 3: "You cannot assign role 'Admin'"
**Cause:** Manager trying to assign Admin role  
**Fix:** Only Admin and SuperAdmin can assign Admin role

### Error 4: "You cannot manage this user"
**Cause:** Trying to manage same-level or higher-level user  
**Fix:** Can only manage users with lower privilege

## 📊 Who Can See What?

| Your Role | You Can See |
|-----------|-------------|
| SuperAdmin | Everyone |
| Admin | Manager, Support, User, SubUser (NOT SuperAdmin) |
| Manager | Support, User, SubUser (NOT Admin, SuperAdmin) |
| Support | User, SubUser (NOT Manager and above) |
| User | Own profile + Own subusers |
| SubUser | Own profile only |

## 🎯 API Endpoints Quick Reference

### Create User
```http
POST /api/EnhancedUsers
```
**Validates:** Role assignment hierarchy

### Create Subuser
```http
POST /api/RoleBasedAuth/create-subuser
POST /api/EnhancedSubusers
```
**Validates:** User role restriction + Role hierarchy

### Assign Role
```http
POST /api/RoleBasedAuth/assign-role
```
**Validates:** Assigner can assign role + Can manage target

### View Users
```http
GET /api/EnhancedUsers
```
**Filters:** Hierarchically based on requester role

### View Subusers
```http
GET /api/EnhancedSubusers
```
**Filters:** Hierarchically based on requester role

## ⚡ Quick Test Commands

### Test 1: Admin Creating SuperAdmin (Should Fail)
```bash
curl -X POST http://localhost:4000/api/EnhancedUsers \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{"UserEmail":"test@test.com","UserName":"Test","Password":"Test@123","DefaultRole":"SuperAdmin"}'
# Expected: 403 Forbidden
```

### Test 2: User Creating Subuser (Should Fail)
```bash
curl -X POST http://localhost:4000/api/RoleBasedAuth/create-subuser \
  -H "Authorization: Bearer {user_token}" \
  -H "Content-Type: application/json" \
  -d '{"SubuserEmail":"sub@test.com","SubuserPassword":"Test@123"}'
# Expected: 403 Forbidden
```

### Test 3: Manager Creating Subuser (Should Pass)
```bash
curl -X POST http://localhost:4000/api/EnhancedSubusers \
  -H "Authorization: Bearer {manager_token}" \
  -H "Content-Type: application/json" \
  -d '{"Email":"sub@test.com","Password":"Test@123","Name":"Subuser","Role":"Support"}'
# Expected: 200 OK
```

## 💡 Pro Tips

1. **Always check hierarchy level before operations**
   - Lower number = Higher privilege
   
2. **User role is special**
   - Cannot create subusers
   - Can only view own data

3. **Same-level restriction is strict**
   - Admin cannot manage another Admin
   - Manager cannot manage another Manager

4. **SuperAdmin bypasses all**
   - Full system access
   - No restrictions

5. **Error messages are descriptive**
   - Read them for quick debugging
   - They tell you exactly what's wrong

## 🔧 Database Quick Check

```sql
-- Check your role
SELECT u.user_email, r.RoleName, r.HierarchyLevel
FROM Users u
JOIN UserRoles ur ON u.user_id = ur.UserId
JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.user_email = 'your.email@example.com';

-- Check all roles
SELECT RoleId, RoleName, HierarchyLevel 
FROM Roles 
ORDER BY HierarchyLevel;
```

## 📞 Need Help?

1. Check error message (very descriptive)
2. Verify your role: `GET /api/RoleBasedAuth/my-permissions`
3. Check hierarchy level in database
4. Review detailed docs:
   - HIERARCHICAL-ACCESS-CONTROL-COMPLETE.md
   - HIERARCHICAL-ACCESS-CONTROL-HINDI.md
   - HIERARCHICAL-ACCESS-CONTROL-TESTING.md

---

**Print this card and keep it handy! 🎯**
