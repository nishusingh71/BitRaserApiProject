# Quick Reference - EnhancedUsers PATCH Endpoints

## 🚀 All PATCH Endpoints

| Endpoint | Purpose | Permission Required | Updates Field |
|----------|---------|---------------------|---------------|
| `PATCH /{email}/change-password` | Change password | Own: None, Others: `CHANGE_USER_PASSWORDS` | `user_password` |
| `PATCH /{email}/update-license` | Update license | Own: None, Others: `UPDATE_USER_LICENSE` | `license_details_json` |
| `PATCH /{email}/update-payment` | Update payment | Own: None, Others: `UPDATE_PAYMENT_DETAILS` | `payment_details_json` |

---

## 📝 Request Examples

### 1. Change Password
```json
{
  "CurrentPassword": "OldPass@123",
  "NewPassword": "NewPass@456"
}
```

### 2. Update License
```json
{
  "LicenseDetailsJson": "{\"licenseKey\":\"ABC-123\",\"plan\":\"premium\",\"expiryDate\":\"2025-12-31\"}"
}
```

### 3. Update Payment
```json
{
  "PaymentDetailsJson": "{\"cardType\":\"Visa\",\"last4\":\"1234\",\"expiryMonth\":12,\"expiryYear\":2026}"
}
```

---

## ✅ Success Responses

All endpoints return similar structure:
```json
{
  "message": "Operation completed successfully",
  "userEmail": "user@example.com",
  "updatedAt": "2025-01-26T10:30:00Z"
}
```

---

## ❌ Error Responses

| Code | Message | Meaning |
|------|---------|---------|
| 400 | "Required field is missing" | Invalid request body |
| 400 | "Invalid JSON format" | Malformed JSON string |
| 400 | "Current password is incorrect" | Wrong current password |
| 401 | "User not authenticated" | No or invalid token |
| 403 | "Insufficient permissions" | Lacks required permission |
| 404 | "User not found" | Email doesn't exist |
| 500 | "Error updating..." | Server error |

---

## 🔑 Authorization Rules

### Own Data:
✅ All users can update their own:
- Password (with current password)
- License details
- Payment details

### Others' Data:
✅ Only admins with specific permissions can update:
- `CHANGE_USER_PASSWORDS` → Change any password
- `UPDATE_USER_LICENSE` → Update any license
- `UPDATE_PAYMENT_DETAILS` → Update any payment

---

## 🧪 Quick Test Commands

```bash
# Set variables
EMAIL="test@example.com"
TOKEN="your-jwt-token"
BASE_URL="http://localhost:5000/api/EnhancedUsers"

# 1. Change Password
curl -X PATCH "$BASE_URL/$EMAIL/change-password" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"CurrentPassword":"Old@123","NewPassword":"New@456"}'

# 2. Update License
curl -X PATCH "$BASE_URL/$EMAIL/update-license" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"LicenseDetailsJson":"{\"plan\":\"premium\"}"}'

# 3. Update Payment
curl -X PATCH "$BASE_URL/$EMAIL/update-payment" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"PaymentDetailsJson":"{\"method\":\"card\"}"}'
```

---

## 🔍 Verify Database Changes

```sql
-- Check if updates applied
SELECT 
    user_email,
    SUBSTRING(user_password, 1, 20) as password_hash,
    license_details_json,
    payment_details_json,
    updated_at
FROM users
WHERE user_email = 'test@example.com';
```

**Look for**:
- ✅ `updated_at` timestamp changed
- ✅ `license_details_json` contains new data
- ✅ `payment_details_json` contains new data
- ✅ `user_password` hash changed (for password updates)

---

## ⚡ Common Issues & Solutions

### Issue: Database not updating
**Solution**: Check that both lines are present:
```csharp
_context.Entry(user).State = EntityState.Modified;
await _context.SaveChangesAsync();
```

### Issue: 403 Forbidden
**Solution**: 
- Verify JWT token is valid
- Check user has required permission
- Ensure trying to update own data OR has admin permission

### Issue: Invalid JSON error
**Solution**: Escape quotes properly:
```json
// ✅ Correct
{"LicenseDetailsJson": "{\"key\":\"value\"}"}

// ❌ Wrong
{"LicenseDetailsJson": "{"key":"value"}"}
```

### Issue: Current password incorrect
**Solution**: 
- Verify current password is correct
- Use BCrypt.Verify to test locally
- Admins can bypass this check

---

## 📊 Database Impact

Each PATCH call updates:

| Field | Always Updated | Conditionally Updated |
|-------|----------------|----------------------|
| `updated_at` | ✅ Always | - |
| `user_password` | - | ✅ change-password only |
| `license_details_json` | - | ✅ update-license only |
| `payment_details_json` | - | ✅ update-payment only |

---

## 🎯 Best Practices

1. **Always validate JSON** before sending:
   ```javascript
   try {
     JSON.parse(licenseDetailsJson);
   } catch (e) {
     console.error("Invalid JSON");
   }
   ```

2. **Store sensitive data encrypted** in JSON:
   ```json
   {
     "cardNumber": "ENCRYPTED_VALUE",
     "cvv": "ENCRYPTED_VALUE"
   }
   ```

3. **Log all updates** for audit trail:
   ```csharp
   _logger.LogInformation("User {Email} updated license at {Time}", 
       email, DateTime.UtcNow);
   ```

4. **Verify changes** after update:
   ```csharp
   var updatedUser = await _context.Users.FindAsync(userId);
   Assert.NotNull(updatedUser.license_details_json);
   ```

---

**Status**: ✅ Working  
**Last Tested**: 2025-01-26  
**Build**: Successful
