# ✅ RoleBasedAuth Edit Profile Endpoint

## 🎯 **OVERVIEW:**

New **PATCH /api/RoleBasedAuth/edit-profile** endpoint added to allow both **Users** and **Subusers** to edit their own profile information.

### **Key Features:**
- ✅ **Self-service:** Logged-in user can update their own profile
- ✅ **PATCH method:** Partial updates (only send fields you want to change)
- ✅ **Multi-tenant:** Works with both Main DB and Private Cloud databases
- ✅ **Smart detection:** Automatically detects User vs Subuser
- ✅ **Editable fields:** Name, Phone, Timezone

---

## 📋 **ENDPOINT DETAILS:**

### **URL:**
```
PATCH /api/RoleBasedAuth/edit-profile
```

### **Authentication:**
```
Authorization: Bearer <JWT_TOKEN>
```

### **Request Body (JSON):**
```json
{
  "Name": "John Doe",              // Optional
  "Phone": "1234567890",           // Optional (null to clear)
  "Timezone": "Asia/Kolkata"       // Optional
}
```

**Note:** All fields are optional. Send only the fields you want to update.

---

## 🔧 **HOW IT WORKS:**

### **Flow Diagram:**
```
1. Get logged-in user email from JWT token
   ↓
2. Try to find as SUBUSER first
   ↓
   ├─ FOUND → Update in current database (Main or Private Cloud)
   │          Update: Name, Phone, Timezone
   │          Save changes
   │          Return success
   ↓
3. NOT FOUND as Subuser → Try to find as USER
   ↓
   ├─ FOUND → Update in Main database
   │          Update: user_name, phone_number, timezone
   │          Save changes
   │          Return success
   ↓
4. NOT FOUND → Return 404 (shouldn't happen if authenticated)
```

### **Smart Field Mapping:**

| User Type | Database Field | Request Field | Database Table |
|-----------|---------------|---------------|----------------|
| **Subuser** | `Name` | `Name` | `subuser` |
| **Subuser** | `Phone` | `Phone` | `subuser` |
| **Subuser** | `timezone` | `Timezone` | `subuser` |
| **User** | `user_name` | `Name` | `users` |
| **User** | `phone_number` | `Phone` | `users` |
| **User** | `timezone` | `Timezone` | `users` |

---

## 📊 **EXAMPLES:**

### **Example 1: User Updates Name and Phone**

**Request:**
```sh
PATCH /api/RoleBasedAuth/edit-profile
Authorization: Bearer eyJhbG...
Content-Type: application/json

{
  "Name": "John Smith",
  "Phone": "9876543210"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Profile updated successfully",
  "userType": "user",
  "email": "john@example.com",
  "name": "John Smith",
  "phone": "9876543210",
  "timezone": "Asia/Kolkata",
  "updatedFields": ["Name", "Phone"],
  "updatedAt": "2024-12-25T10:30:00Z"
}
```

---

### **Example 2: Subuser Updates Only Timezone**

**Request:**
```sh
PATCH /api/RoleBasedAuth/edit-profile
Authorization: Bearer eyJhbG...
Content-Type: application/json

{
  "Timezone": "America/New_York"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Profile updated successfully",
  "userType": "subuser",
  "email": "subuser@example.com",
  "name": "Jane Doe",
  "phone": "1234567890",
  "timezone": "America/New_York",
  "updatedFields": ["Timezone"],
  "updatedAt": "2024-12-25T10:30:00Z"
}
```

---

### **Example 3: Clear Phone Number**

**Request:**
```sh
PATCH /api/RoleBasedAuth/edit-profile
Authorization: Bearer eyJhbG...
Content-Type: application/json

{
  "Phone": null
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Profile updated successfully",
  "userType": "user",
  "email": "john@example.com",
  "name": "John Smith",
  "phone": null,
  "timezone": "Asia/Kolkata",
  "updatedFields": ["Phone"],
  "updatedAt": "2024-12-25T10:30:00Z"
}
```

---

### **Example 4: Update All Fields**

**Request:**
```sh
PATCH /api/RoleBasedAuth/edit-profile
Authorization: Bearer eyJhbG...
Content-Type: application/json

{
  "Name": "Robert Johnson",
  "Phone": "5555555555",
  "Timezone": "Europe/London"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Profile updated successfully",
  "userType": "subuser",
  "email": "robert@example.com",
  "name": "Robert Johnson",
  "phone": "5555555555",
  "timezone": "Europe/London",
  "updatedFields": ["Name", "Phone", "Timezone"],
  "updatedAt": "2024-12-25T10:30:00Z"
}
```

---

### **Example 5: No Fields Provided (Error)**

**Request:**
```sh
PATCH /api/RoleBasedAuth/edit-profile
Authorization: Bearer eyJhbG...
Content-Type: application/json

{}
```

**Response (400 Bad Request):**
```json
{
  "message": "No fields to update. Provide at least one field: Name, Phone, or Timezone"
}
```

---

### **Example 6: Empty/Whitespace Values (Ignored)**

**Request:**
```sh
PATCH /api/RoleBasedAuth/edit-profile
Authorization: Bearer eyJhbG...
Content-Type: application/json

{
  "Name": "   ",
  "Phone": "",
  "Timezone": "Asia/Kolkata"
}
```

**Behavior:**
- `Name`: Whitespace only → **Ignored** (not updated)
- `Phone`: Empty string → **Sets to NULL** (clears phone)
- `Timezone`: Valid value → **Updated**

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Profile updated successfully",
  "userType": "user",
  "email": "john@example.com",
  "name": "John Smith",
  "phone": null,
  "timezone": "Asia/Kolkata",
  "updatedFields": ["Phone", "Timezone"],
  "updatedAt": "2024-12-25T10:30:00Z"
}
```

---

## 🔍 **VALIDATION RULES:**

### **Name Field:**
```csharp
// ✅ Valid
"Name": "John Doe"           → Updates to "John Doe"
"Name": "  John Doe  "       → Updates to "John Doe" (trimmed)

// ❌ Ignored
"Name": null                 → Not updated
"Name": ""                   → Not updated (empty)
"Name": "   "                → Not updated (whitespace only)
(field not sent)             → Not updated
```

### **Phone Field:**
```csharp
// ✅ Valid
"Phone": "1234567890"        → Updates to "1234567890"
"Phone": "  123-456  "       → Updates to "123-456" (trimmed)
"Phone": null                → Clears phone (sets to NULL)
"Phone": ""                  → Clears phone (sets to NULL)

// ❌ Invalid
"Phone": "12345678901234567890123"  → Too long (max 20 chars)
```

### **Timezone Field:**
```csharp
// ✅ Valid
"Timezone": "Asia/Kolkata"   → Updates to "Asia/Kolkata"
"Timezone": "America/New_York" → Updates to "America/New_York"

// ❌ Ignored
"Timezone": null             → Not updated
"Timezone": ""               → Not updated (empty)
"Timezone": "   "            → Not updated (whitespace only)
```

---

## 🔐 **SECURITY:**

### **Self-Service Only:**
```csharp
// User can ONLY update their OWN profile
var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

// ✅ Logged-in user: john@example.com
// ✅ Updates profile for: john@example.com
// ❌ Cannot update: jane@example.com (different user)
```

### **No Admin Override:**
```
This endpoint is SELF-SERVICE only.
Admins cannot use this to edit other users' profiles.

For admin features, use:
- /api/EnhancedSubuser/{email} (PATCH)
- /api/EnhancedProfile/profile/{email} (PUT)
```

---

## 🗄️ **DATABASE OPERATIONS:**

### **For Users (Main Database):**
```sql
-- Find user
SELECT * FROM users WHERE user_email = 'john@example.com';

-- Update fields
UPDATE users 
SET 
  user_name = 'John Smith',      -- if Name provided
  phone_number = '9876543210',   -- if Phone provided
  timezone = 'Asia/Kolkata',     -- if Timezone provided
  updated_at = '2024-12-25T10:30:00Z'
WHERE user_email = 'john@example.com';
```

### **For Subusers (Main or Private Cloud DB):**
```sql
-- Find subuser
SELECT * FROM subuser WHERE subuser_email = 'subuser@example.com';

-- Update fields
UPDATE subuser 
SET 
  Name = 'Jane Smith',           -- if Name provided
  Phone = '9876543210',          -- if Phone provided
  timezone = 'Asia/Kolkata',     -- if Timezone provided
  UpdatedAt = '2024-12-25T10:30:00Z'
WHERE subuser_email = 'subuser@example.com';
```

---

## 📊 **FIELD TRACKING:**

### **Updated Fields List:**

The response includes `updatedFields` array showing which fields were actually changed:

```json
{
  "updatedFields": ["Name", "Phone"]
}
```

**Possible values:**
- `"Name"` - Name was updated
- `"Phone"` - Phone was updated
- `"Timezone"` - Timezone was updated

**Empty array:** No fields were updated (returns 400 Bad Request)

---

## 🎯 **USE CASES:**

### **Use Case 1: User Registration Completion**
```
User signs up with email only.
Later, user completes profile by adding:
- Name
- Phone
- Timezone
```

**Request:**
```json
{
  "Name": "John Doe",
  "Phone": "1234567890",
  "Timezone": "Asia/Kolkata"
}
```

---

### **Use Case 2: Phone Number Change**
```
User changes phone number only.
```

**Request:**
```json
{
  "Phone": "9999999999"
}
```

---

### **Use Case 3: Timezone Update (Travel)**
```
User travels to different country.
Updates timezone for correct timestamps.
```

**Request:**
```json
{
  "Timezone": "Europe/London"
}
```

---

### **Use Case 4: Remove Phone Number (Privacy)**
```
User wants to remove phone from profile.
```

**Request:**
```json
{
  "Phone": null
}
```

---

## 🔍 **TROUBLESHOOTING:**

### **Issue 1: "User or subuser profile not found"**

**Cause:** Authenticated user not found in database.

**Debug:**
```sh
# Check JWT token
GET /api/RoleBasedAuth/my-permissions
Authorization: Bearer <token>

# Response will show:
{
  "userType": "user" or "subuser",
  "email": "your@email.com"
}
```

**Solution:** Verify user exists in database.

---

### **Issue 2: "No fields to update"**

**Cause:** Request body is empty or all fields are null/whitespace.

**Example:**
```json
{}                          ❌ Empty
{ "Name": null }           ❌ Null
{ "Name": "" }             ❌ Empty string
{ "Name": "   " }          ❌ Whitespace only
{ "Name": "John" }         ✅ Valid!
```

---

### **Issue 3: Field Not Updating**

**Possible causes:**

1. **Whitespace-only value:**
   ```json
   { "Name": "   " }  ❌ Ignored
   ```

2. **Null value (for Name/Timezone):**
   ```json
   { "Name": null }   ❌ Ignored
   ```

3. **Field not sent:**
   ```json
   { "Phone": "123" }  // Name not sent → not updated ✅
   ```

---

### **Issue 4: Private Cloud Subuser Not Updating**

**Cause:** Subuser in private cloud database, but endpoint querying main DB.

**Fix:** This endpoint automatically handles both databases:
1. First checks current database (private cloud if applicable)
2. Then falls back to main database

**No manual intervention needed!** ✅

---

## 📈 **RESPONSE CODES:**

| Status | Meaning | Example |
|--------|---------|---------|
| **200 OK** | Profile updated successfully | All good ✅ |
| **400 Bad Request** | No fields to update | Empty request body |
| **401 Unauthorized** | Missing or invalid JWT token | Not logged in |
| **404 Not Found** | User/Subuser not found | Invalid email in token |
| **500 Internal Server Error** | Database error | Connection issue |

---

## 🔗 **RELATED ENDPOINTS:**

| Endpoint | Purpose | Method |
|----------|---------|--------|
| `/api/RoleBasedAuth/edit-profile` | Edit own profile (self-service) | PATCH |
| `/api/RoleBasedAuth/change-password` | Change own password | PATCH |
| `/api/RoleBasedAuth/update-timezone` | Update timezone (older endpoint) | PATCH |
| `/api/EnhancedSubuser/{email}` | Admin edit subuser | PUT |
| `/api/EnhancedProfile/profile/{email}` | Admin edit any profile | PUT |

---

## 💡 **TIPS:**

### **1. Partial Updates:**
```
You can update any combination of fields:
- Just Name
- Just Phone
- Just Timezone
- Name + Phone
- All three fields
```

### **2. Null vs Empty:**
```json
// For Phone (both clear the value):
"Phone": null      ✅ Clears phone
"Phone": ""        ✅ Clears phone

// For Name/Timezone (ignored):
"Name": null       ❌ Ignored (not updated)
"Name": ""         ❌ Ignored (not updated)
```

### **3. Whitespace Handling:**
```json
// Automatic trimming:
"Name": "  John Doe  "  → Saved as "John Doe"
"Phone": "  123-456  "  → Saved as "123-456"
```

### **4. Response Inspection:**
```json
// Check updatedFields to see what changed:
{
  "updatedFields": ["Name", "Timezone"]
  // Phone was NOT updated (not sent in request)
}
```

---

## 📝 **FRONTEND INTEGRATION:**

### **React/TypeScript Example:**
```typescript
// API call
const updateProfile = async (updates: {
  Name?: string;
  Phone?: string | null;
  Timezone?: string;
}) => {
  const response = await fetch('/api/RoleBasedAuth/edit-profile', {
    method: 'PATCH',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(updates)
  });
  
  const data = await response.json();
  return data;
};

// Usage examples:
await updateProfile({ Name: 'John Doe' });
await updateProfile({ Phone: '1234567890' });
await updateProfile({ 
  Name: 'John Doe',
  Phone: '1234567890',
  Timezone: 'Asia/Kolkata'
});
```

### **JavaScript (Vanilla) Example:**
```javascript
// Update only name
fetch('/api/RoleBasedAuth/edit-profile', {
  method: 'PATCH',
  headers: {
    'Authorization': 'Bearer ' + token,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    Name: 'John Doe'
  })
})
.then(res => res.json())
.then(data => {
  console.log('Updated:', data.updatedFields);
  console.log('New name:', data.name);
});
```

### **Angular Example:**
```typescript
import { HttpClient } from '@angular/common/http';

updateProfile(updates: Partial<{
  Name: string;
  Phone: string | null;
  Timezone: string;
}>) {
  return this.http.patch('/api/RoleBasedAuth/edit-profile', updates);
}

// Usage:
this.updateProfile({ Name: 'John Doe' }).subscribe(response => {
  console.log('Profile updated:', response);
});
```

---

## ✅ **SUMMARY:**

| Feature | Value |
|---------|-------|
| **Endpoint** | `PATCH /api/RoleBasedAuth/edit-profile` |
| **Method** | PATCH (partial updates) |
| **Authentication** | Required (JWT Bearer token) |
| **User Types** | Both Users and Subusers |
| **Editable Fields** | Name, Phone, Timezone |
| **Database Support** | Main DB + Private Cloud |
| **Self-Service** | ✅ Yes (user edits own profile) |
| **Admin Override** | ❌ No (use other endpoints) |
| **Validation** | Trimming, null handling, max length |
| **Response** | Updated profile + changed fields list |

---

**Fix Applied:** ✅ COMPLETE  
**Date:** 2024-12-XX  
**Feature:** Edit Profile endpoint for Users and Subusers  
**Method:** PATCH (partial updates)  
**Database Support:** Main DB + Private Cloud automatic routing  

---

**Ab Users aur Subusers dono apni profile easily edit kar sakte hain! 🎉**
