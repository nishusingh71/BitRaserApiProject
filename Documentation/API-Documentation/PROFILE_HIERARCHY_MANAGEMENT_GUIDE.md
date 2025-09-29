# 👤 Profile & Hierarchy Management System - Complete Guide

## 🎯 **Overview**

The Enhanced Profile Controller provides comprehensive profile management with hierarchical user relationships, allowing organizations to maintain structured user hierarchies with role-based access control.

## 📊 **System Architecture**

### **🏗️ Hierarchical Structure**
```
SuperAdmin (Level 1) - Complete system control
    ├── Admin (Level 2) - Administrative operations
    │   ├── Manager (Level 3) - Management functions
    │   │   ├── Support (Level 4) - Support operations
    │   │   │   └── User (Level 5) - Basic operations
```

### **🔐 Permission-Based Access**
- **Profile Permissions:** 15 specific permissions for profile operations
- **Hierarchy Management:** Dynamic user relationship management
- **Role-Based Visibility:** Users can only see/manage subordinates
- **Sensitive Data Protection:** Restricted access to sensitive information

## 🚀 **API Endpoints Overview**

### **📝 Profile Management**

#### **Get Current User Profile**
```http
GET /api/EnhancedProfile/my-profile
Authorization: Bearer {token}
Permission: VIEW_PROFILE
```

**Response Example:**
```json
{
  "personalInfo": {
    "user_email": "manager@company.com",
    "user_name": "John Manager",
    "phone_number": "+1234567890",
    "created_at": "2024-01-01T00:00:00Z",
    "accountAge": "25.12:30:45",
    "isPrivateCloud": true,
    "hasPrivateApi": false
  },
  "securityInfo": {
    "roles": [
      {
        "roleName": "Manager",
        "description": "Management functions",
        "hierarchyLevel": 3,
        "assignedAt": "2024-01-01T00:00:00Z",
        "assignedBy": "admin@company.com"
      }
    ],
    "permissions": [
      "VIEW_PROFILE", "UPDATE_PROFILE", "VIEW_USER_PROFILE", 
      "VIEW_HIERARCHY", "MANAGE_TEAM_MEMBERS"
    ],
    "highestRole": "Manager"
  },
  "statistics": {
    "totalMachines": 5,
    "activeLicenses": 3,
    "totalReports": 12,
    "totalSessions": 45,
    "totalSubusers": 2,
    "lastLoginDate": "2024-01-15T08:30:00Z"
  },
  "hierarchyInfo": {
    "currentLevel": 3,
    "currentRole": "Manager",
    "canManageUsers": true,
    "managedUserCount": 8,
    "reportsTo": "admin@company.com"
  },
  "recentActivity": {
    "recentLogs": [
      {
        "log_level": "Info",
        "log_message": "User profile updated",
        "created_at": "2024-01-15T10:30:00Z"
      }
    ],
    "lastSession": {
      "login_time": "2024-01-15T08:30:00Z",
      "logout_time": null,
      "ip_address": "192.168.1.100",
      "session_status": "active"
    }
  }
}
```

#### **Get User Profile by Email**
```http
GET /api/EnhancedProfile/profile/{userEmail}
Authorization: Bearer {token}
Permission: VIEW_USER_PROFILE
```

**Hierarchy Rules:**
- ✅ **Can view:** Own profile + subordinate profiles
- ❌ **Cannot view:** Superior or peer profiles (unless admin+)
- 🔒 **Sensitive Info:** Only with `VIEW_SENSITIVE_PROFILE_INFO` permission

#### **Update Own Profile**
```http
PUT /api/EnhancedProfile/my-profile
Authorization: Bearer {token}
Permission: UPDATE_PROFILE

{
  "userName": "Updated Name",
  "phoneNumber": "+9876543210"
}
```

### **👥 Hierarchy Management**

#### **Get My Hierarchy**
```http
GET /api/EnhancedProfile/my-hierarchy
Authorization: Bearer {token}
Permission: VIEW_HIERARCHY
```

**Response Example:**
```json
{
  "currentUser": {
    "user_email": "manager@company.com",
    "user_name": "John Manager",
    "role": "Manager",
    "hierarchyLevel": 3
  },
  "directReports": [
    {
      "user_email": "support1@company.com",
      "user_name": "Alice Support",
      "created_at": "2024-01-01T00:00:00Z",
      "role": "Support"
    }
  ],
  "allSubordinates": [
    {
      "user_email": "support1@company.com",
      "user_name": "Alice Support",
      "highestRole": "Support",
      "hierarchyLevel": 4,
      "canManage": true
    },
    {
      "user_email": "user1@company.com",
      "user_name": "Bob User",
      "highestRole": "User",
      "hierarchyLevel": 5,
      "canManage": true
    }
  ],
  "peers": [
    {
      "user_email": "manager2@company.com",
      "user_name": "Jane Manager",
      "role": "Manager"
    }
  ],
  "hierarchyStatistics": {
    "directReportCount": 1,
    "totalSubordinateCount": 2,
    "peerCount": 1,
    "canManageUsers": true
  }
}
```

#### **Get Organization Hierarchy**
```http
GET /api/EnhancedProfile/organization-hierarchy
Authorization: Bearer {token}
Permission: VIEW_ORGANIZATION_HIERARCHY
```

**Complete org chart with all roles and users**

### **👥 Team Management**

#### **Assign Direct Report**
```http
POST /api/EnhancedProfile/assign-direct-report
Authorization: Bearer {token}
Permission: MANAGE_HIERARCHY

{
  "userEmail": "newuser@company.com"
}
```

#### **Search Users**
```http
GET /api/EnhancedProfile/search-users?searchTerm=john&role=Manager&page=0&pageSize=50
Authorization: Bearer {token}
Permission: SEARCH_USERS
```

**Query Parameters:**
- `searchTerm`: Search in name/email
- `role`: Filter by role name
- `hierarchyLevel`: Filter by hierarchy level
- `createdFrom`: Filter by creation date range
- `createdTo`: Filter by creation date range
- `page`: Page number (0-based)
- `pageSize`: Results per page (default: 50)

### **📊 Analytics & Reporting**

#### **Profile Analytics**
```http
GET /api/EnhancedProfile/profile-analytics
Authorization: Bearer {token}
Permission: VIEW_PROFILE_ANALYTICS
```

**Comprehensive system analytics:**
- User distribution by role
- Hierarchy level distribution
- Recent registration trends
- Active user statistics
- System health metrics

## 🔐 **Permission System**

### **📋 Profile Permissions**

#### **Basic Profile Access**
- `VIEW_PROFILE` - View own profile information
- `UPDATE_PROFILE` - Update own profile information

#### **User Profile Management**
- `VIEW_USER_PROFILE` - View other user profiles
- `VIEW_SENSITIVE_PROFILE_INFO` - View sensitive information (phone, stats)

#### **Hierarchy Management**
- `VIEW_HIERARCHY` - View user hierarchy and relationships
- `VIEW_ORGANIZATION_HIERARCHY` - View complete organizational chart
- `MANAGE_HIERARCHY` - Manage user hierarchy relationships
- `ASSIGN_DIRECT_REPORTS` - Assign direct reports to managers

#### **Advanced Operations**
- `SEARCH_USERS` - Search users across the system
- `VIEW_PROFILE_ANALYTICS` - View profile analytics and statistics
- `MANAGE_USER_RELATIONSHIPS` - Manage relationships between users
- `VIEW_USER_ACTIVITY` - View user activity and recent actions
- `EXPORT_USER_DATA` - Export user profile and activity data
- `VIEW_SUBORDINATE_PROFILES` - View profiles of subordinate users
- `MANAGE_TEAM_MEMBERS` - Manage team member profiles and assignments

### **🎭 Role-Based Access Matrix**

| Permission | SuperAdmin | Admin | Manager | Support | User |
|------------|------------|-------|---------|---------|------|
| VIEW_PROFILE | ✅ | ✅ | ✅ | ✅ | ✅ |
| UPDATE_PROFILE | ✅ | ✅ | ✅ | ✅ | ✅ |
| VIEW_USER_PROFILE | ✅ | ✅ | ✅ | ✅ | ❌ |
| VIEW_SENSITIVE_PROFILE_INFO | ✅ | ✅ | ❌ | ❌ | ❌ |
| VIEW_HIERARCHY | ✅ | ✅ | ✅ | ✅ | ❌ |
| VIEW_ORGANIZATION_HIERARCHY | ✅ | ✅ | ❌ | ❌ | ❌ |
| MANAGE_HIERARCHY | ✅ | ✅ | ✅ | ❌ | ❌ |
| ASSIGN_DIRECT_REPORTS | ✅ | ✅ | ✅ | ❌ | ❌ |
| SEARCH_USERS | ✅ | ✅ | ✅ | ✅ | ❌ |
| VIEW_PROFILE_ANALYTICS | ✅ | ✅ | ❌ | ❌ | ❌ |
| MANAGE_USER_RELATIONSHIPS | ✅ | ✅ | ❌ | ❌ | ❌ |
| VIEW_USER_ACTIVITY | ✅ | ✅ | ✅ | ✅ | ❌ |
| EXPORT_USER_DATA | ✅ | ✅ | ❌ | ❌ | ❌ |
| VIEW_SUBORDINATE_PROFILES | ✅ | ✅ | ✅ | ❌ | ❌ |
| MANAGE_TEAM_MEMBERS | ✅ | ✅ | ✅ | ❌ | ❌ |

## 🎯 **Hierarchy Rules**

### **📐 Access Control Logic**

#### **Visibility Rules:**
```csharp
// Users can view profiles based on hierarchy
bool CanViewProfile(int managerLevel, int targetLevel)
{
    return managerLevel <= targetLevel; // Lower numbers = higher authority
}

// Examples:
// SuperAdmin (1) can view Admin (2) ✅
// Manager (3) can view Support (4) ✅
// Support (4) cannot view Manager (3) ❌
```

#### **Management Rules:**
```csharp
// Users can manage subordinates only
bool CanManageUser(int managerLevel, int targetLevel)
{
    return managerLevel < targetLevel; // Strict hierarchy
}

// Examples:
// Admin (2) can manage Manager (3) ✅
// Manager (3) can manage User (5) ✅
// Support (4) cannot manage Manager (3) ❌
```

### **🔄 Dynamic Relationships**

#### **Direct Reports:**
- **Manager → Support:** Direct reporting relationship
- **Admin → Manager:** Administrative oversight
- **Dynamic Assignment:** Managers can assign direct reports

#### **Peer Relationships:**
- **Same Level:** Users at same hierarchy level are peers
- **Limited Access:** Peers can view basic profile info only
- **No Management:** Peers cannot manage each other

## 💡 **Usage Examples**

### **🚀 Common Scenarios**

#### **1. Manager Views Team Members**
```bash
# Get manager's hierarchy
curl -H "Authorization: Bearer {manager-token}" \
     GET /api/EnhancedProfile/my-hierarchy

# View specific team member profile
curl -H "Authorization: Bearer {manager-token}" \
     GET /api/EnhancedProfile/profile/support@company.com
```

#### **2. Admin Searches for Users**
```bash
# Search for managers
curl -H "Authorization: Bearer {admin-token}" \
     GET "/api/EnhancedProfile/search-users?role=Manager&page=0&pageSize=10"

# View organization hierarchy
curl -H "Authorization: Bearer {admin-token}" \
     GET /api/EnhancedProfile/organization-hierarchy
```

#### **3. User Updates Own Profile**
```bash
# Update profile information
curl -H "Authorization: Bearer {user-token}" \
     -H "Content-Type: application/json" \
     -d '{"userName":"New Name","phoneNumber":"+1234567890"}' \
     PUT /api/EnhancedProfile/my-profile
```

### **📊 Integration Examples**

#### **Frontend Integration:**
```javascript
// Get current user profile with hierarchy info
const getMyProfile = async () => {
  const response = await fetch('/api/EnhancedProfile/my-profile', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  return response.json();
};

// Search team members
const searchTeamMembers = async (searchTerm) => {
  const response = await fetch(
    `/api/EnhancedProfile/search-users?searchTerm=${searchTerm}`,
    { headers: { 'Authorization': `Bearer ${token}` } }
  );
  return response.json();
};
```

## 🛠️ **Error Handling**

### **📋 Common Error Responses**

#### **403 Forbidden - Hierarchy Violation**
```json
{
  "error": "You can only view profiles you have access to in your hierarchy",
  "statusCode": 403,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### **404 Not Found - User Not Found**
```json
{
  "error": "User profile with email user@company.com not found",
  "statusCode": 404,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### **400 Bad Request - Invalid Data**
```json
{
  "error": "Invalid profile update data",
  "details": {
    "userName": "Name cannot be empty",
    "phoneNumber": "Invalid phone number format"
  },
  "statusCode": 400,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## 🚀 **Best Practices**

### **🎯 Implementation Guidelines**

#### **1. Security First**
- ✅ Always validate hierarchy permissions
- ✅ Protect sensitive information based on roles
- ✅ Use JWT tokens for authentication
- ✅ Log all profile access attempts

#### **2. Performance Optimization**
- ✅ Use pagination for large user lists
- ✅ Implement caching for frequently accessed profiles
- ✅ Optimize database queries with proper indexes
- ✅ Use async operations for all database calls

#### **3. User Experience**
- ✅ Provide clear hierarchy visualization
- ✅ Show meaningful error messages
- ✅ Implement real-time updates for profile changes
- ✅ Offer bulk operations for administrators

### **📈 Scalability Considerations**

#### **Database Optimization:**
```sql
-- Recommended indexes for performance
CREATE INDEX IX_UserRoles_HierarchyLevel ON UserRoles(HierarchyLevel);
CREATE INDEX IX_Users_Email ON Users(user_email);
CREATE INDEX IX_UserRoles_RoleId_UserId ON UserRoles(RoleId, UserId);
```

#### **Caching Strategy:**
```csharp
// Cache user permissions for 15 minutes
services.AddMemoryCache();
services.Configure<MemoryCacheEntryOptions>(options => {
    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
});
```

## 🎊 **Success Metrics**

### **✅ Feature Completeness**
- **Profile Management:** Complete CRUD operations with hierarchy validation
- **Security:** Role-based access control with 15+ granular permissions
- **Analytics:** Comprehensive reporting and statistics
- **Search:** Advanced filtering and pagination
- **Relationships:** Dynamic team management and direct reports
- **Audit:** Complete activity tracking and logging

### **📊 System Benefits**
- **🔐 Enhanced Security:** Granular permission control
- **👥 Better Organization:** Clear hierarchy management
- **📈 Improved Analytics:** Detailed profile and usage statistics
- **🚀 Scalable Architecture:** Handles large organizations efficiently
- **💼 Professional Features:** Enterprise-grade profile management

## 🎉 **Quick Start Checklist**

### **🚀 Getting Started**
1. ✅ **Authentication:** Ensure JWT token is properly configured
2. ✅ **Permissions:** Verify user has required profile permissions
3. ✅ **Database:** Confirm all profile permissions are seeded
4. ✅ **Testing:** Test hierarchy rules with different user roles
5. ✅ **Integration:** Implement in frontend with proper error handling

### **🎯 Next Steps**
- **Explore:** Test all endpoints with different user roles
- **Customize:** Adapt permission model to your organization
- **Extend:** Add custom profile fields as needed
- **Monitor:** Set up logging and analytics tracking
- **Scale:** Implement caching for production environments

---

**🎊 Your Profile & Hierarchy Management System is ready for enterprise use! 🎊**

*Complete with role-based security, hierarchical management, and comprehensive analytics.*