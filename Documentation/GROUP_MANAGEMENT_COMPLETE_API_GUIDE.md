# 🔐 Group Management API - Complete Guide

## Overview
Complete Group Management System based on D-Secure UI Design for managing user groups, permissions, and license allocations.

---

## 📋 Table of Contents
1. [Authentication](#authentication)
2. [API Endpoints](#api-endpoints)
3. [Request/Response Examples](#request-response-examples)
4. [Permission System](#permission-system)
5. [UI Integration Guide](#ui-integration-guide)
6. [Error Handling](#error-handling)

---

## 🔑 Authentication

All endpoints require JWT Bearer token authentication:

```http
Authorization: Bearer YOUR_JWT_TOKEN
```

**Required Permissions:**
- `VIEW_GROUPS` - View groups
- `MANAGE_GROUPS` - Full CRUD operations
- `CREATE_GROUP` - Create new groups
- `UPDATE_GROUP` - Update existing groups
- `DELETE_GROUP` - Delete groups

---

## 🎯 API Endpoints

### 1. **Get All Groups** (List View)
**Matches: Manage Groups Screenshot**

```http
GET /api/GroupManagement
```

**Query Parameters:**
- `search` (string, optional) - Search by name or description
- `page` (int, default: 1) - Page number
- `pageSize` (int, default: 10) - Items per page
- `sortBy` (string, default: "name") - Sort by: name, users, licenses, created
- `sortOrder` (string, default: "asc") - asc or desc

**Response:**
```json
{
  "title": "Manage Groups",
  "description": "Create and manage user groups with specific permissions",
  "groups": [
  {
      "groupId": 1,
      "groupName": "Default Group",
      "description": "Default users Selection",
      "userCount": 150,
      "licenseCount": 2322,
      "permissions": [
        "Basic Access",
        "Report Generation"
      ],
 "morePermissions": 0,
      "createdDate": "2024-11-15T00:00:00Z",
      "createdDateFormatted": "15/11/2024"
    },
    {
      "groupId": 2,
      "groupName": "Pool Group",
      "description": "Pool users",
      "userCount": 45,
      "licenseCount": 200,
      "permissions": [
        "Basic Access"
      ],
      "morePermissions": 0,
      "createdDate": "2024-02-20T00:00:00Z",
      "createdDateFormatted": "20/02/2024"
    },
    {
      "groupId": 3,
      "groupName": "IT Department",
      "description": "IT Department Users",
  "userCount": 25,
      "licenseCount": 150,
      "permissions": [
        "Basic Access",
"Advanced Erasure",
        "Report Generation"
      ],
      "morePermissions": 1,
      "createdDate": "2024-03-19T00:00:00Z",
      "createdDateFormatted": "19/03/2024"
    },
    {
      "groupId": 4,
      "groupName": "Security Team",
   "description": "Security Operations",
      "userCount": 8,
      "licenseCount": 75,
      "permissions": [
    "Basic Access",
        "Advanced Erasure",
        "Report Generation"
      ],
      "morePermissions": 2,
      "createdDate": "2024-11-15T00:00:00Z",
      "createdDateFormatted": "15/11/2024"
  }
  ],
  "totalCount": 4,
  "page": 1,
  "pageSize": 10,
  "showing": "Showing 1-4 of 4 groups"
}
```

---

### 2. **Get Single Group** (Edit View)
**Matches: Edit Group Screenshot**

```http
GET /api/GroupManagement/{groupId}
```

**Response:**
```json
{
  "groupId": 1,
  "groupName": "Default Group",
  "description": "Default users Selection",
  "licenseAllocation": 2322,
  "userCount": 150,
  "permissions": [
    "BASIC_ACCESS",
    "REPORT_GENERATION"
  ],
  "createdDate": "2024-11-15T00:00:00Z"
}
```

---

### 3. **Create New Group**
**Matches: Add New Group Screenshot**

```http
POST /api/GroupManagement
```

**Request Body:**
```json
{
  "groupName": "New Group Name",
  "description": "Describe the purpose of this group",
  "licenseAllocation": 100,
  "permissions": [
    "BASIC_ACCESS",
  "ADVANCED_ERASURE",
    "REPORT_GENERATION",
    "USER_MANAGEMENT",
    "SYSTEM_SETTINGS"
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Group created successfully",
  "groupId": 5,
  "groupName": "New Group Name",
  "createdAt": "2024-12-29T10:30:00Z"
}
```

**Validation Rules:**
- ✅ Group name: Required, 1-100 characters, must be unique
- ✅ Description: Required, max 500 characters
- ✅ License allocation: 1-10000
- ✅ Permissions: At least one permission required

---

### 4. **Update Group**
**Matches: Edit Group Screenshot**

```http
PUT /api/GroupManagement/{groupId}
```

**Request Body:**
```json
{
  "groupName": "Updated Group Name",
  "description": "Updated description",
  "licenseAllocation": 2322,
  "permissions": [
    "BASIC_ACCESS",
    "REPORT_GENERATION"
  ]
}
```

**Response:**
```json
{
  "message": "Group updated successfully",
  "groupId": 1,
  "groupName": "Updated Group Name"
}
```

---

### 5. **Delete Group**

```http
DELETE /api/GroupManagement/{groupId}
```

**Response (Success):**
```json
{
  "message": "Group deleted successfully",
  "groupId": 1
}
```

**Response (Error - Has Users):**
```json
{
  "message": "Cannot delete group with active users",
  "userCount": 150,
  "suggestion": "Remove all users from this group before deleting"
}
```

---

### 6. **Get Available Permissions**

```http
GET /api/GroupManagement/available-permissions
```

**Response:**
```json
{
"categories": [
    {
      "categoryName": "basic_access",
      "categoryLabel": "Basic Access",
      "permissions": [
  {
  "value": "BASIC_ACCESS",
  "label": "Basic Access",
          "description": "Access to basic erasure tools",
       "isChecked": true
      }
      ]
    },
    {
      "categoryName": "advanced_erasure",
      "categoryLabel": "Advanced Erasure",
      "permissions": [
  {
          "value": "ADVANCED_ERASURE",
        "label": "Advanced Erasure",
          "description": "Access to advanced erasure methods",
          "isChecked": false
    }
 ]
    },
    {
 "categoryName": "report_generation",
      "categoryLabel": "Report Generation",
      "permissions": [
   {
          "value": "REPORT_GENERATION",
          "label": "Report Generation",
      "description": "Generate and download reports",
 "isChecked": true
        }
      ]
    },
    {
      "categoryName": "user_management",
  "categoryLabel": "User Management",
      "permissions": [
        {
   "value": "USER_MANAGEMENT",
 "label": "User Management",
          "description": "Manage other users (Admins only)",
          "isChecked": false
        }
      ]
  },
    {
      "categoryName": "system_settings",
      "categoryLabel": "System Settings",
    "permissions": [
        {
     "value": "SYSTEM_SETTINGS",
       "label": "System Settings",
     "description": "Configure system settings",
        "isChecked": false
        }
      ]
    },
    {
      "categoryName": "license_management",
      "categoryLabel": "License Management",
  "permissions": [
        {
          "value": "LICENSE_MANAGEMENT",
          "label": "License Management",
          "description": "Manage license allocation",
        "isChecked": false
        }
      ]
    }
  ]
}
```

---

### 7. **Get Group Members**

```http
GET /api/GroupManagement/{groupId}/members
```

**Response:**
```json
{
  "groupId": 1,
  "groupName": "Default Group",
  "members": [
    {
      "userEmail": "john.doe@company.com",
      "userName": "John Doe",
      "userType": "user",
    "joinedDate": "2024-01-15T00:00:00Z",
      "joinedDateFormatted": "15/01/2024",
 "status": "active"
    },
    {
      "userEmail": "jane.smith@company.com",
      "userName": "Jane Smith",
   "userType": "subuser",
      "joinedDate": "2024-02-20T00:00:00Z",
      "joinedDateFormatted": "20/02/2024",
      "status": "active"
    }
  ],
  "totalMembers": 150
}
```

---

### 8. **Bulk Add Users to Group**

```http
POST /api/GroupManagement/{groupId}/add-users
```

**Request Body:**
```json
{
  "groupId": 1,
  "userEmails": [
    "user1@company.com",
    "user2@company.com",
    "user3@company.com"
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Added 2 users to group. 1 failed.",
  "successCount": 2,
  "failedCount": 1,
  "failedEmails": [
    "user3@company.com"
  ],
  "successEmails": [
    "user1@company.com",
    "user2@company.com"
  ]
}
```

---

### 9. **Get Group Statistics**

```http
GET /api/GroupManagement/statistics
```

**Response:**
```json
{
  "totalGroups": 4,
  "totalUsers": 228,
  "totalLicenses": 2747,
  "averageUsersPerGroup": 57,
  "topGroups": [
    {
      "groupName": "Default Group",
      "userCount": 150,
      "licenseCount": 2322,
      "percentage": 65.8
    },
    {
      "groupName": "Pool Group",
      "userCount": 45,
      "licenseCount": 200,
      "percentage": 19.7
    },
    {
      "groupName": "IT Department",
"userCount": 25,
      "licenseCount": 150,
      "percentage": 11.0
    },
    {
      "groupName": "Security Team",
      "userCount": 8,
      "licenseCount": 75,
  "percentage": 3.5
    }
  ]
}
```

---

## 🎨 UI Integration Guide

### React/Vue.js Example - Manage Groups Page

```javascript
// Fetch groups with search and pagination
const fetchGroups = async (search = '', page = 1) => {
  const response = await fetch(
    `/api/GroupManagement?search=${search}&page=${page}&pageSize=10&sortBy=name&sortOrder=asc`,
    {
      headers: {
   'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }
  );
  
  const data = await response.json();
  return data;
};

// Example response handling
{
  groups: [
    {
      groupId: 1,
      groupName: "Default Group",
      description: "Default users Selection",
      userCount: 150,
      licenseCount: 2322,
      permissions: ["Basic Access", "Report Generation"],
      morePermissions: 0,
      createdDateFormatted: "15/11/2024"
    }
  ],
  showing: "Showing 1-4 of 4 groups"
}
```

### Create Group Form

```javascript
const createGroup = async (formData) => {
  const response = await fetch('/api/GroupManagement', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
 'Content-Type': 'application/json'
    },
    body: JSON.stringify({
  groupName: formData.groupName,
      description: formData.description,
      licenseAllocation: formData.licenseAllocation,
      permissions: formData.selectedPermissions // Array of permission names
    })
  });
  
  if (response.ok) {
    const result = await response.json();
    console.log('Group created:', result.groupId);
  }
};
```

### Edit Group Form

```javascript
// 1. Fetch existing group data
const loadGroup = async (groupId) => {
  const response = await fetch(`/api/GroupManagement/${groupId}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return await response.json();
};

// 2. Update group
const updateGroup = async (groupId, formData) => {
  const response = await fetch(`/api/GroupManagement/${groupId}`, {
    method: 'PUT',
    headers: {
    'Authorization': `Bearer ${token}`,
'Content-Type': 'application/json'
    },
    body: JSON.stringify({
 groupName: formData.groupName,
    description: formData.description,
      licenseAllocation: formData.licenseAllocation,
      permissions: formData.selectedPermissions
    })
  });
  
  return await response.json();
};
```

---

## 🚨 Error Handling

### HTTP Status Codes

| Code | Meaning | Example Response |
|------|---------|------------------|
| 200 | Success | `{ "message": "Success" }` |
| 400 | Bad Request | `{ "message": "Group name is required" }` |
| 401 | Unauthorized | `{ "message": "User not authenticated" }` |
| 403 | Forbidden | `{ "message": "Insufficient permissions to manage groups" }` |
| 404 | Not Found | `{ "message": "Group not found" }` |
| 409 | Conflict | `{ "message": "Group with this name already exists" }` |
| 500 | Server Error | `{ "message": "Error creating group", "error": "..." }` |

### Common Error Scenarios

#### 1. Duplicate Group Name
```json
{
  "message": "Group with this name already exists"
}
```

#### 2. Cannot Delete Group with Users
```json
{
  "message": "Cannot delete group with active users",
  "userCount": 150,
  "suggestion": "Remove all users from this group before deleting"
}
```

#### 3. Invalid Permissions
```json
{
  "message": "At least one permission must be selected"
}
```

#### 4. Invalid License Allocation
```json
{
  "message": "License allocation must be between 1 and 10000"
}
```

---

## 🔐 Permission System

### Available Permissions

| Permission | Description | Category |
|------------|-------------|----------|
| `BASIC_ACCESS` | Access to basic erasure tools | Basic Access |
| `VIEW_GROUPS` | View groups | Basic Access |
| `ADVANCED_ERASURE` | Access to advanced erasure methods | Advanced Erasure |
| `REPORT_GENERATION` | Generate and download reports | Report Generation |
| `USER_MANAGEMENT` | Manage other users | User Management |
| `MANAGE_GROUPS` | Full group management | User Management |
| `CREATE_GROUP` | Create new groups | User Management |
| `UPDATE_GROUP` | Update existing groups | User Management |
| `DELETE_GROUP` | Delete groups | User Management |
| `SYSTEM_SETTINGS` | Configure system settings | System Settings |
| `LICENSE_MANAGEMENT` | Manage license allocation | License Management |

---

## 📱 UI Components Mapping

### Manage Groups Page (Screenshot 3)
- **Search Bar**: Use `search` query parameter
- **Add New Group Button**: Navigate to create form
- **Group Cards**: Display `GroupCardDto` data
  - Edit icon → GET `/api/GroupManagement/{id}`
  - Delete icon → DELETE `/api/GroupManagement/{id}`
- **Permissions badges**: Show first 3 from `permissions` array
- **"+X more" indicator**: Use `morePermissions` value
- **Pagination**: Use `page` and `pageSize` parameters

### Add New Group Page (Screenshot 2)
- **Group Name**: Text input → `groupName`
- **Description**: Textarea → `description`
- **License Allocation**: Number input → `licenseAllocation`
- **Permissions**: Checkboxes → `permissions` array
  - Categories from `/api/GroupManagement/available-permissions`
- **Selected Permissions**: Display selected count
- **Create Group Button**: POST `/api/GroupManagement`
- **Cancel Button**: Return to list

### Edit Group Page (Screenshot 1)
- Load data: GET `/api/GroupManagement/{id}`
- Update: PUT `/api/GroupManagement/{id}`
- Same form structure as create
- Pre-populate with existing values
- Show "Update Group" instead of "Create Group"

---

## 🧪 Testing Guide

### Postman Collection

```json
{
  "info": {
    "name": "Group Management API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
   "name": "Get All Groups",
      "request": {
        "method": "GET",
        "url": "{{baseUrl}}/api/GroupManagement?search=&page=1&pageSize=10"
      }
    },
    {
      "name": "Create Group",
      "request": {
        "method": "POST",
 "url": "{{baseUrl}}/api/GroupManagement",
        "body": {
       "mode": "raw",
          "raw": "{\n  \"groupName\": \"Test Group\",\n  \"description\": \"Test Description\",\n  \"licenseAllocation\": 100,\n  \"permissions\": [\"BASIC_ACCESS\"]\n}"
        }
      }
  }
  ]
}
```

---

## 📊 Database Structure

### Tables Used
- `Roles` - Group definitions
- `Permissions` - Available permissions
- `RolePermissions` - Group-permission mapping
- `UserRoles` - User-group membership
- `SubuserRoles` - Subuser-group membership
- `machines` - License tracking

---

## 🎯 Best Practices

1. **Always validate input** - Use DTOs with validation attributes
2. **Check permissions** - Verify user has required permissions
3. **Handle errors gracefully** - Return meaningful error messages
4. **Log operations** - Track who created/updated/deleted groups
5. **Prevent orphaned users** - Don't allow deleting groups with active users
6. **Use pagination** - Don't load all groups at once
7. **Cache permissions** - Reduce database queries for permission checks
8. **Audit trail** - Track all group changes

---

## ✅ Checklist

- [ ] JWT authentication configured
- [ ] User has required permissions
- [ ] Form validation on frontend
- [ ] Error handling implemented
- [ ] Loading states for async operations
- [ ] Success/error messages displayed
- [ ] Pagination working
- [ ] Search functionality working
- [ ] Permission checkboxes updating correctly
- [ ] Delete confirmation dialog
- [ ] Refresh list after create/update/delete

---

## 📞 Support

For issues or questions:
1. Check error message in response
2. Verify JWT token is valid
3. Check user permissions
4. Review server logs
5. Test with Postman/Swagger

---

**Last Updated:** 2024-12-29  
**API Version:** 1.0  
**Based on:** D-Secure UI Design
