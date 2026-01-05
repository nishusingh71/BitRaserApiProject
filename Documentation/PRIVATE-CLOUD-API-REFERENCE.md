# 🎯 Private Cloud Database - Complete API Reference

## 📋 **Overview**

Complete REST API for Private Cloud Database management with multi-tenant architecture.

---

## 🔐 **Authentication**

All endpoints require JWT Bearer token except where noted.

```
Authorization: Bearer <your_jwt_token>
```

---

## 📡 **API Endpoints**

### **1. Check Private Cloud Access**

Check if user has private cloud feature enabled.

```http
GET /api/PrivateCloud/check-access
```

**Authorization:** Required

**Response 200:**
```json
{
  "hasPrivateCloudAccess": true,
  "isConfigured": true,
  "isSchemaInitialized": true,
  "lastTested": "2025-01-14T10:30:00Z",
  "testStatus": "success"
}
```

**Response Fields:**
- `hasPrivateCloudAccess`: User has `is_private_cloud = true`
- `isConfigured`: Database connection configured
- `isSchemaInitialized`: Database schema created
- `lastTested`: Last connection test timestamp
- `testStatus`: "success", "failed", or "pending"

---

### **2. Get Setup Wizard Steps**

Get step-by-step setup wizard configuration for frontend.

```http
GET /api/PrivateCloud/setup-wizard
```

**Authorization:** Required

**Response 200:**
```json
{
  "steps": [
    {
      "step": 1,
      "title": "Database Type",
      "description": "Select your database type (MySQL, PostgreSQL, SQL Server)",
      "fields": [
      {
          "name": "databaseType",
          "type": "select",
 "options": ["mysql", "postgresql", "sqlserver"],
    "required": true
        }
      ]
    },
    {
      "step": 2,
      "title": "Database Connection",
      "description": "Enter your database connection details",
      "fields": [
        {
          "name": "serverHost",
      "type": "text",
    "placeholder": "localhost or IP address",
 "required": true
    },
        {
     "name": "serverPort",
     "type": "number",
          "placeholder": "3306 (MySQL) or 5432 (PostgreSQL)",
          "required": true
},
        {
          "name": "databaseName",
   "type": "text",
          "placeholder": "your_database_name",
          "required": true
        },
      {
       "name": "databaseUsername",
  "type": "text",
          "placeholder": "database_user",
          "required": true
     },
    {
     "name": "databasePassword",
     "type": "password",
  "placeholder": "database_password",
          "required": true
        }
      ]
    },
    {
   "step": 3,
      "title": "Test Connection",
      "description": "Verify that the database connection is working",
  "action": "POST /api/PrivateCloud/test"
    },
  {
      "step": 4,
      "title": "Initialize Schema",
      "description": "Create required tables in your private database",
"action": "POST /api/PrivateCloud/initialize-schema"
    },
    {
      "step": 5,
      "title": "Complete Setup",
      "description": "Your private database is ready to use!",
      "note": "All your reports, subusers, and machines will now be stored in your private database"
    }
  ],
  "currentStep": 1,
  "totalSteps": 5
}
```

---

### **3. Setup Private Database**

Configure or update private database connection.

```http
POST /api/PrivateCloud/setup
Content-Type: application/json
```

**Authorization:** Required

**Request Body:**
```json
{
  "databaseType": "mysql",
  "serverHost": "gateway01.ap-southeast-1.prod.aws.tidbcloud.com",
  "serverPort": 4000,
  "databaseName": "Cloud_Erase",
  "databaseUsername": "2tdeFNZMcsWKkDR.root",
  "databasePassword": "76wtaj1GZkg7Qhek",
  "storageLimitMb": 1024,
  "notes": "Production TiDB database"
}
```

**Request Fields:**
- `databaseType`: Database type ("mysql", "postgresql", "sqlserver")
- `serverHost`: Database server hostname or IP
- `serverPort`: Database server port
- `databaseName`: Database name
- `databaseUsername`: Database username
- `databasePassword`: Database password
- `storageLimitMb` (optional): Storage limit in MB
- `notes` (optional): Additional notes

**Response 200:**
```json
{
  "message": "Private database configured successfully",
  "nextStep": "Test the connection using /test endpoint"
}
```

**Response 400:**
```json
{
  "message": "Private cloud feature not enabled for this user",
  "hint": "Please contact administrator to enable private cloud access"
}
```

---

### **4. Test Database Connection**

Test connection to configured private database.

```http
POST /api/PrivateCloud/test
```

**Authorization:** Required

**Response 200 (Success):**
```json
{
  "success": true,
  "message": "Connection successful",
  "serverVersion": "8.0.11-TiDB-v7.5.0",
  "responseTimeMs": 245,
  "schemaExists": false,
  "missingTables": [
    "users",
    "subuser",
    "machines",
    "audit_reports",
    "sessions",
    "logs",
    "commands",
    "groups"
  ],
  "testedAt": "2025-01-14T10:30:00Z"
}
```

**Response 200 (Failure):**
```json
{
  "success": false,
  "message": "Connection failed",
  "error": "Access denied for user '2tdeFNZMcsWKkDR.root'@'...' (using password: YES)",
  "testedAt": "2025-01-14T10:30:00Z"
}
```

**Response Fields:**
- `success`: Connection test result
- `message`: Human-readable message
- `serverVersion`: Database server version
- `responseTimeMs`: Connection response time
- `schemaExists`: All required tables exist
- `missingTables`: List of missing tables
- `error`: Error message if failed

---

### **5. Initialize Database Schema**

Create all required tables in private database with proper relationships.

```http
POST /api/PrivateCloud/initialize-schema
```

**Authorization:** Required

**Response 200:**
```json
{
  "message": "Database schema initialized successfully",
  "note": "All required tables have been created in your private database"
}
```

**Response 400:**
```json
{
  "message": "Failed to initialize database schema"
}
```

**Tables Created (in order):**
1. `users` - Main user accounts
2. `groups` - User groups
3. `subuser` - Sub-users (FK to users)
4. `machines` - Registered machines (FK to users, subuser)
5. `audit_reports` - Erasure reports (FK to users)
6. `sessions` - Login sessions (FK to users)
7. `logs` - Activity logs (FK to users)
8. `commands` - Remote commands (FK to users)

---

### **6. Validate Database Schema**

Check if all required tables exist with proper structure.

```http
POST /api/PrivateCloud/validate-schema
```

**Authorization:** Required

**Response 200 (Valid):**
```json
{
  "isValid": true,
  "message": "All tables exist",
  "existingTables": [
    "users",
    "subuser",
    "machines",
 "audit_reports",
    "sessions",
    "logs",
    "commands",
  "groups"
  ],
  "missingTables": [],
  "requiredTables": [
    "users",
    "subuser",
    "machines",
"audit_reports",
    "sessions",
    "logs",
    "commands",
    "groups"
  ]
}
```

**Response 200 (Invalid):**
```json
{
  "isValid": false,
  "message": "Missing tables: machines, audit_reports",
  "existingTables": [
    "users",
 "subuser",
    "sessions",
    "logs",
  "commands",
    "groups"
  ],
  "missingTables": [
    "machines",
    "audit_reports"
  ],
  "requiredTables": [
    "users",
    "subuser",
    "machines",
    "audit_reports",
    "sessions",
    "logs",
    "commands",
    "groups"
  ]
}
```

---

### **7. Get Required Tables**

Get list of tables that will be created in private database.

```http
GET /api/PrivateCloud/required-tables
```

**Authorization:** Required

**Response 200:**
```json
{
  "tables": [
    "users",
    "groups",
    "subuser",
    "machines",
    "audit_reports",
    "sessions",
    "logs",
    "commands"
  ],
  "totalCount": 8,
  "description": "These tables will be created in your private database"
}
```

---

### **8. Get Current Configuration**

Retrieve current private database configuration.

```http
GET /api/PrivateCloud/config
```

**Authorization:** Required

**Response 200:**
```json
{
  "configId": 1,
  "userId": 123,
  "userEmail": "user@example.com",
  "connectionString": "***ENCRYPTED***",
  "databaseType": "mysql",
  "serverHost": "gateway01.ap-southeast-1.prod.aws.tidbcloud.com",
  "serverPort": 4000,
  "databaseName": "Cloud_Erase",
  "databaseUsername": "2tdeFNZMcsWKkDR.root",
  "isActive": true,
  "lastTestedAt": "2025-01-14T10:30:00Z",
  "testStatus": "success",
  "testError": null,
  "schemaInitialized": true,
  "schemaInitializedAt": "2025-01-14T10:35:00Z",
  "schemaVersion": "1.0.0",
  "storageUsedMb": 125.50,
  "storageLimitMb": 1024.00,
  "createdAt": "2025-01-14T10:00:00Z",
  "updatedAt": "2025-01-14T10:35:00Z",
  "createdBy": "user@example.com",
  "notes": "Production TiDB database"
}
```

**Response 404:**
```json
{
  "message": "No private database configured"
}
```

---

### **9. Delete Configuration**

Remove private database configuration.

```http
DELETE /api/PrivateCloud/config
```

**Authorization:** Required

**Response 200:**
```json
{
  "message": "Private database configuration removed successfully"
}
```

**Response 404:**
```json
{
  "message": "No configuration found"
}
```

**⚠️ Warning:** This only removes the configuration. Data in private database is NOT deleted.

---

## 🔄 **Complete Setup Flow**

### **Sequence Diagram:**

```
User → Frontend → API → Private Database

1. GET /check-access
   ← hasPrivateCloudAccess: true

2. GET /setup-wizard
   ← 5 steps with fields

3. POST /setup
   → database credentials
   ← configuration saved

4. POST /test
   ← connection successful

5. GET /required-tables
   ← 8 tables list

6. POST /initialize-schema
   ← tables created

7. POST /validate-schema
   ← schema valid

8. GET /config
   ← complete configuration
```

---

## 🗄️ **Database Schema**

### **Table Relationships:**

```sql
users (PK: user_id, UK: user_email)
  ↓
  ├─→ subuser (FK: user_email → users.user_email)
  │     └─→ machines (FK: subuser_email → subuser.subuser_email)
  ├─→ machines (FK: user_email → users.user_email)
  ├─→ audit_reports (FK: client_email → users.user_email)
  ├─→ sessions (FK: user_email → users.user_email)
  ├─→ logs (FK: user_email → users.user_email)
└─→ commands (FK: user_email → users.user_email)

groups (PK: group_id) - Independent table
```

### **Foreign Key Actions:**

- **ON DELETE CASCADE**: subuser, audit_reports, sessions
- **ON DELETE SET NULL**: machines, logs, commands

---

## 🔒 **Security Features**

### **1. Encryption:**
- Connection strings encrypted using AES
- Passwords never stored in plain text
- Encryption key from configuration

### **2. Authentication:**
- JWT Bearer token required
- User must be authenticated
- Role-based access control

### **3. Validation:**
- Input validation on all endpoints
- SQL injection prevention
- Connection test before saving

### **4. Isolation:**
- Each user's data in separate database
- No cross-user data access
- Complete data isolation

---

## 📊 **Error Codes**

| Status | Description |
|--------|-------------|
| 200 | Success |
| 400 | Bad Request (Invalid input) |
| 401 | Unauthorized (No token or invalid token) |
| 403 | Forbidden (Private cloud not enabled) |
| 404 | Not Found (Configuration not found) |
| 500 | Internal Server Error |

---

## 🧪 **Testing Examples**

### **Using cURL:**

```bash
# 1. Check Access
curl -X GET "https://api.example.com/api/PrivateCloud/check-access" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 2. Setup Database
curl -X POST "https://api.example.com/api/PrivateCloud/setup" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "databaseType": "mysql",
    "serverHost": "localhost",
    "serverPort": 3306,
    "databaseName": "private_db",
    "databaseUsername": "root",
    "databasePassword": "password"
  }'

# 3. Test Connection
curl -X POST "https://api.example.com/api/PrivateCloud/test" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 4. Initialize Schema
curl -X POST "https://api.example.com/api/PrivateCloud/initialize-schema" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 5. Validate Schema
curl -X POST "https://api.example.com/api/PrivateCloud/validate-schema" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 6. Get Configuration
curl -X GET "https://api.example.com/api/PrivateCloud/config" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### **Using Postman:**

1. **Setup Collection:**
   - Base URL: `https://api.example.com`
   - Authorization: Bearer Token
   - Token: `{{jwt_token}}`

2. **Create Requests:**
   - Import cURL examples
   - Or manually create each endpoint

3. **Environment Variables:**
   - `jwt_token`: Your JWT token
   - `api_url`: Base API URL

---

## 📚 **Additional Resources**

- **Frontend Guide:** `Documentation/PRIVATE-CLOUD-FRONTEND-GUIDE.md`
- **Hindi Guide:** `Documentation/PRIVATE-CLOUD-HINDI-GUIDE.md`
- **Setup Guide:** `Documentation/PRIVATE-CLOUD-SETUP.md`

---

**Complete API implementation ready! 🎉**
