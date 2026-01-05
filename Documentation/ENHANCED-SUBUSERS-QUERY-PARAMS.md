# 🚀 ENHANCED: Query Parameters for Single-Field Updates

## 🎯 New Feature Added

**Endpoint:** `PATCH /api/EnhancedSubusers/by-parent/{parentEmail}/subuser/{subuserEmail}`

**What's New:** You can now update a **single field** easily using **query parameters** without needing to send a JSON body!

---

## ✅ Two Ways to Update

### Method 1: Query Parameters (NEW! ⚡)
**Perfect for:** Single-field updates, simple API calls, URL-based updates

### Method 2: JSON Body (Original)
**Perfect for:** Multiple-field updates, complex payloads

---

## 📝 Query Parameters Available

| Parameter | Maps to Field | Example Value | Description |
|-----------|---------------|---------------|-------------|
| `subuser_name` | `Name` | "John Smith" | Update subuser's name |
| `subuser_phone` | `Phone` | "1234567890" | Update phone number |
| `subuser_department` | `Department` | "IT Department" | Update department |
| `subuser_role` | `Role` | "Manager" | Update role |
| `subuser_status` | `Status` | "active" | Update status (active/inactive) |

---

## 🎯 Examples

### Example 1: Update Only Name (Query Parameter)
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_name=John%20Smith%20Updated
Authorization: Bearer YOUR_TOKEN
```

**No JSON body needed!** ✅

**Response:**
```json
{
  "success": true,
  "message": "Subuser updated successfully",
  "parent_email": "admin@example.com",
  "subuser_email": "john@example.com",
  "updatedFields": ["Name"],
  "updatedBy": "admin@example.com",
  "updatedAt": "2025-01-26T12:00:00Z",
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

### Example 2: Update Only Phone (Query Parameter)
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_phone=9876543210
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "success": true,
  "updatedFields": ["Phone"],
  "subuser": {
    "phone": "9876543210",
    ...
  }
}
```

---

### Example 3: Update Only Status (Query Parameter)
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_status=inactive
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
"success": true,
  "updatedFields": ["Status"],
  "subuser": {
    "status": "inactive",
    ...
  }
}
```

---

### Example 4: Update Multiple Fields (Query Parameters)
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_name=John%20Updated&subuser_phone=1111111111&subuser_status=active
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "success": true,
  "updatedFields": ["Name", "Phone", "Status"],
  "subuser": {
    "name": "John Updated",
    "phone": "1111111111",
    "status": "active",
    ...
  }
}
```

---

### Example 5: Update via JSON Body (Original Method)
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "Name": "John Smith",
  "Phone": "1234567890",
  "Status": "active"
}
```

**Response:** Same as above

---

## 🔧 cURL Examples

### Update Name Only:
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_name=John%20Updated" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Update Phone Only:
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_phone=9876543210" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Update Status Only:
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_status=inactive" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Update Role Only:
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_role=Senior%20Developer" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Update Department Only:
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_department=Engineering" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Update Multiple Fields:
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_name=John%20Smith&subuser_phone=1234567890&subuser_status=active" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## 🎯 Priority Logic

### When Both Query Parameters AND JSON Body Are Provided:

```http
PATCH /api/EnhancedSubusers/.../...?subuser_name=From%20Query
Content-Type: application/json

{
  "Name": "From Body"
}
```

**Result:** ✅ **Query parameters take priority!**
- Query parameters are processed first
- JSON body is only used if NO query parameters are provided

---

## ❌ Error Handling

### Error 1: No Fields Provided
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com
Authorization: Bearer YOUR_TOKEN
```

**Response: 400 Bad Request**
```json
{
  "success": false,
  "message": "No fields to update. Provide at least one field via query parameters or request body.",
  "acceptedQueryParams": [
    "subuser_name",
    "subuser_phone",
    "subuser_department",
    "subuser_role",
    "subuser_status"
  ],
  "acceptedBodyFields": [
    "Name",
    "Phone",
"Department",
    "Role",
    "Status"
  ]
}
```

---

### Error 2: Subuser Not Found
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/notfound@example.com?subuser_name=Test
Authorization: Bearer YOUR_TOKEN
```

**Response: 404 Not Found**
```json
{
  "success": false,
  "message": "Subuser 'notfound@example.com' not found under parent 'admin@example.com'"
}
```

---

### Error 3: Unauthorized
```http
PATCH /api/EnhancedSubusers/by-parent/admin@example.com/subuser/john@example.com?subuser_name=Test
Authorization: Bearer OTHER_USER_TOKEN
```

**Response: 403 Forbidden**
```json
{
  "success": false,
  "error": "You can only update your own subusers"
}
```

---

## 📊 Comparison: Query Parameters vs JSON Body

| Feature | Query Parameters | JSON Body |
|---------|------------------|-----------|
| **Easy Single Field** | ✅ Very Easy | ❌ Requires JSON |
| **Multiple Fields** | ✅ Possible | ✅ Clean |
| **URL Length** | ❌ Limited | ✅ Unlimited |
| **Content-Type Header** | ✅ Not needed | ❌ Required |
| **Browser Friendly** | ✅ Yes (can paste in browser) | ❌ No |
| **Postman/cURL** | ✅ Simple | ✅ Simple |
| **Best For** | Single field updates | Multiple field updates |

---

## 🎯 Use Cases

### ✅ Use Query Parameters When:
1. **Updating a single field** (e.g., change status to inactive)
2. **Quick updates** via browser/URL
3. **Simple API testing** without JSON tools
4. **Webhook/automation** with simple URL calls

### ✅ Use JSON Body When:
1. **Updating multiple fields** at once
2. **Complex payloads** with many values
3. **Standard REST API** practices
4. **Frontend forms** with multiple inputs

---

## 🔍 How It Works Internally

```csharp
// 1. Check query parameters first
if (!string.IsNullOrEmpty(subuser_name))
{
    subuser.Name = subuser_name;
    updatedFields.Add("Name");
}

// 2. If no query params, use JSON body
if (request != null && updatedFields.Count == 0)
{
    if (!string.IsNullOrEmpty(request.Name))
    {
        subuser.Name = request.Name;
        updatedFields.Add("Name");
    }
}

// 3. Require at least one field
if (updatedFields.Count == 0)
{
    return BadRequest("No fields to update");
}
```

---

## ✅ Benefits

### 1. **Simplicity**
```
Before: Need JSON body for every update
After: Just add ?subuser_name=NewName to URL
```

### 2. **Flexibility**
```
✅ Query parameters for simple updates
✅ JSON body for complex updates
✅ Both methods work!
```

### 3. **Developer Friendly**
```bash
# Quick test in browser:
http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/test@example.com?subuser_status=inactive

# Quick cURL:
curl -X PATCH "http://...?subuser_name=NewName" -H "Auth: Bearer TOKEN"
```

### 4. **No Breaking Changes**
```
✅ Old JSON body method still works
✅ New query parameter method added
✅ Backward compatible
```

---

## 🧪 Testing

### Test 1: Single Field via Query
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/test@example.com?subuser_name=Test%20User%20Updated" \
  -H "Authorization: Bearer YOUR_TOKEN"
```
**Expected:** ✅ Name updated

---

### Test 2: Multiple Fields via Query
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/test@example.com?subuser_name=Test&subuser_phone=111&subuser_status=active" \
  -H "Authorization: Bearer YOUR_TOKEN"
```
**Expected:** ✅ 3 fields updated

---

### Test 3: JSON Body (Original Method)
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/test@example.com" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"Name":"Test User","Phone":"1234567890"}'
```
**Expected:** ✅ 2 fields updated

---

### Test 4: No Fields
```bash
curl -X PATCH \
  "http://localhost:4000/api/EnhancedSubusers/by-parent/admin@example.com/subuser/test@example.com" \
  -H "Authorization: Bearer YOUR_TOKEN"
```
**Expected:** ❌ 400 Bad Request

---

## 📝 Summary

| Feature | Status |
|---------|--------|
| **Query Parameters** | ✅ Added (5 params) |
| **JSON Body** | ✅ Still works |
| **Single Field Update** | ✅ Super easy now |
| **Multiple Field Update** | ✅ Both methods work |
| **Backward Compatible** | ✅ Yes |
| **Build** | ✅ Successful |

---

**Status:** ✅ **COMPLETE**  
**Build:** ✅ **SUCCESSFUL**  
**New Feature:** ✅ **QUERY PARAMETERS FOR EASY UPDATES**

**Ab aap easily single field update kar sakte ho bina JSON body ke!** 🎉✅
