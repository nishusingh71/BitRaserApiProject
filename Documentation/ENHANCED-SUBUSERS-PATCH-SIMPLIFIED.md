# EnhancedSubusers PATCH Endpoint - Simplified Update

## 🎯 Endpoint Modified

**Route:** `PATCH /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}`

---

## ✅ What Changed

### Request Body DTO:
Now uses **`UpdateSubuserByParentDto`** which ONLY accepts 5 fields:

```csharp
public class UpdateSubuserByParentDto
{
    [MaxLength(100)]
    public string? Name { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(100)]
    public string? Department { get; set; }
    
    [MaxLength(50)]
    public string? Role { get; set; }
    
    [MaxLength(50)]
    public string? Status { get; set; }
}
```

### Before:
The endpoint used **`UpdateSubuserDto`** which had **16+ fields**

### After:
The endpoint uses **`UpdateSubuserByParentDto`** which has **ONLY 5 fields**

---

## 📝 Request Format

### Endpoint
```
PATCH /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}
```

### Parameters
- **parentEmail**: Email of the parent user (owner of subuser)
- **subuserEmail**: Email of the subuser to update

### Request Body (JSON) - ONLY These 5 Fields Accepted
```json
{
  "Name": "Updated Name",
  "Phone": "1234567890",
  "Department": "IT Department",
  "Role": "Manager",
  "Status": "active"
}
```

**Note:** All fields are **optional**. You can update just one field or all five fields.

**⚠️ Important:** If you try to send other fields (like `MaxMachines`, `GroupId`, `LicenseAllocation`, etc.), they will be **IGNORED** by the API.

---

## 📊 Examples

### Example 1: Update Only Name
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "Name": "John Smith Updated"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Subuser updated successfully",
  "parent_email": "admin@example.com",
  "subuser_email": "john@example.com",
  "updatedFields": ["Name"],
  "updatedBy": "admin@example.com",
  "updatedAt": "2025-01-26T10:30:00Z",
  "subuser": {
    "subuser_email": "john@example.com",
    "user_email": "admin@example.com",
    "name": "John Smith Updated",
    "phone": "1234567890",
    "department": "IT",
    "role": "Developer",
    "status": "active"
  }
}
```

---

### Example 2: Update Multiple Fields
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/sarah@example.com
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "Name": "Sarah Johnson",
  "Phone": "9876543210",
  "Department": "HR",
  "Role": "HR Manager",
  "Status": "active"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Subuser updated successfully",
  "parent_email": "admin@example.com",
  "subuser_email": "sarah@example.com",
  "updatedFields": ["Name", "Phone", "Department", "Role", "Status"],
  "updatedBy": "admin@example.com",
  "updatedAt": "2025-01-26T10:35:00Z",
  "subuser": {
    "subuser_email": "sarah@example.com",
    "user_email": "admin@example.com",
    "name": "Sarah Johnson",
    "phone": "9876543210",
 "department": "HR",
    "role": "HR Manager",
    "status": "active"
  }
}
```

---

### Example 3: Try to Send Extra Fields (They Will Be Ignored)
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/test@example.com
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "Name": "Test User",
  "Phone": "1111111111",
  "Department": "Sales",
  "Role": "Sales Rep",
  "Status": "active",
  "MaxMachines": 10,          ❌ IGNORED
  "LicenseAllocation": 5,  ❌ IGNORED
  "CanViewReports": true      ❌ IGNORED
}
```

**Result:** Only `Name`, `Phone`, `Department`, `Role`, and `Status` will be updated. The other fields will be **completely ignored**.

---

## ✅ Request Body Validation

The API will **automatically reject** invalid requests:

### ❌ Invalid: Field Too Long
```json
{
  "Name": "This is a very very very very very very very very very very very very very very very very long name that exceeds 100 characters limit"
}
```
**Error:** `400 Bad Request - Name exceeds maximum length of 100 characters`

### ❌ Invalid: Wrong Data Type
```json
{
  "Phone": 1234567890  // Should be string, not number
}
```
**Error:** `400 Bad Request - Invalid data type for Phone field`

---

## 🔒 Permissions

### Who Can Use This Endpoint?

1. **Parent User**: Can update their own subusers
   - No special permission required
   - Just need to be authenticated

2. **Admins**: Can update any subuser
   - Requires `UPDATE_ALL_SUBUSERS` permission

---

## 🚫 What Cannot Be Updated via This Endpoint

The following fields are **NOT** in the DTO and **CANNOT** be updated through this endpoint:

- `MaxMachines` - Use admin panel
- `GroupId` - Use group management endpoint
- `LicenseAllocation` - Use license management endpoint
- `SubuserGroup` - Use group management endpoint
- `CanViewReports`, `CanManageMachines`, `CanAssignLicenses`, `CanCreateSubusers` - Use permissions endpoint
- `EmailNotifications`, `SystemAlerts` - Use notification settings endpoint
- `Notes` - Use full update endpoint
- `Email` - Cannot change email
- `Password` - Use password change endpoint

---

## 📊 Comparison: Old vs New DTO

| Feature | Old DTO (UpdateSubuserDto) | New DTO (UpdateSubuserByParentDto) |
|---------|---------------------------|-------------------------------------|
| **Total Fields** | 16+ fields | 5 fields only |
| **Can Update Permissions** | ✅ Yes | ❌ No |
| **Can Update Licenses** | ✅ Yes | ❌ No |
| **Can Update Groups** | ✅ Yes | ❌ No |
| **Can Update Basic Info** | ✅ Yes | ✅ Yes |
| **Complexity** | High | Low |
| **Safety** | Can accidentally change critical settings | Safe for basic updates |

---

## ✅ Benefits of New DTO

1. **Type-Safe**: Only accepts the 5 allowed fields
2. **Validation**: Built-in length validation
3. **Clearer API**: Request body matches exactly what can be updated
4. **Prevents Errors**: Cannot accidentally send wrong fields
5. **Better Documentation**: Swagger/OpenAPI will only show 5 fields

---

## 🧪 Testing

### cURL Example
```bash
curl -X PATCH "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/test@example.com" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Name": "Test User Updated",
    "Phone": "1234567890",
 "Department": "Engineering",
    "Role": "Senior Developer",
    "Status": "active"
  }'
```

### Postman Example
```
Method: PATCH
URL: http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/test@example.com

Headers:
- Authorization: Bearer YOUR_JWT_TOKEN
- Content-Type: application/json

Body (raw JSON):
{
  "Name": "Test User Updated",
  "Phone": "1234567890",
  "Department": "Engineering",
  "Role": "Senior Developer",
  "Status": "active"
}
```

---

## 📝 Summary

| Property | Value |
|----------|-------|
| **Endpoint** | `PATCH /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}` |
| **Method** | PATCH |
| **Auth Required** | Yes (JWT Bearer Token) |
| **Permission** | `UPDATE_SUBUSER` or own subuser |
| **Updatable Fields** | name, phone, department, role, status (5 fields only) |
| **Response Format** | JSON |
| **Status Codes** | 200 (Success), 403 (Forbidden), 404 (Not Found), 500 (Error) |

---

**Status:** ✅ **COMPLETE**  
**Build:** ✅ **SUCCESSFUL**  
**DTO:** ✅ **UpdateSubuserByParentDto (5 fields only)**  
**Last Updated:** 2025-01-26
