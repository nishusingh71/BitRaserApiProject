# 🛠️ **ApplicationDbContext Fix Required**

## 🔍 **Current Build Errors:**

Lines 498-509 में `Role_permission` type की errors हैं। यह type exist नहीं करती।

### **Fix Required:**

Replace all `Role_permission` with `RolePermission` in the seed data section.

### **Current Error Lines:**
```csharp
// Line 498: new Role_permission { RoleId = 2, PermissionId = 6 },  ❌
// Line 499: new Role_permission { RoleId = 2, PermissionId = 7 },  ❌
// Line 502: new Role_permission { RoleId = 3, PermissionId = 3 },  ❌
// Line 503: new Role_permission { RoleId = 3, PermissionId = 4 },  ❌
// Line 504: new Role_permission { RoleId = 3, PermissionId = 5 },  ❌
// Line 507: new Role_permission { RoleId = 4, PermissionId = 3 },  ❌
// Line 508: new Role_permission { RoleId = 4, PermissionId = 5 },  ❌
// Line 509: new Role_permission { RoleId = 4, PermissionId = 7 },  ❌
```

### **Should Be:**
```csharp
// Line 498: new RolePermission { RoleId = 2, PermissionId = 6 },  ✅
// Line 499: new RolePermission { RoleId = 2, PermissionId = 7 },  ✅
// Line 502: new RolePermission { RoleId = 3, PermissionId = 3 },  ✅
// Line 503: new RolePermission { RoleId = 3, PermissionId = 4 },  ✅
// Line 504: new RolePermission { RoleId = 3, PermissionId = 5 },  ✅
// Line 507: new RolePermission { RoleId = 4, PermissionId = 3 },  ✅
// Line 508: new RolePermission { RoleId = 4, PermissionId = 5 },  ✅
// Line 509: new RolePermission { RoleId = 4, PermissionId = 7 },  ✅
```

## ⚡ **Quick Fix Steps:**

1. **Open** `ApplicationDbContext.cs`
2. **Find** lines 498-509 (around role-permission seed data)
3. **Replace** all `Role_permission` with `RolePermission`
4. **Save** and build again

## 🎯 **After Fix:**

Once fixed, you can:

1. **Create Enhanced Migration:**
   ```bash
   Add-Migration "Enhanced108PermissionsUpdate"
   ```

2. **Update TiDB Cloud Database:**
   ```bash
   Update-Database
   ```

3. **Verify Enhancement:**
   - 108 permissions instead of 7
   - Enhanced role mappings
   - Complete RBAC system ready

## 🚀 **Expected Result:**

```
✅ Build Success
✅ 108 Enhanced Permissions
✅ 5-Tier Role Hierarchy  
✅ Enhanced Controllers with RBAC
✅ Production-Ready System
```

**Fix these 8 lines and your enhanced system will be ready for migration! 🎊**