# 🎨 Group Management Frontend Integration Guide

## Quick Implementation Guide for React/Vue/Angular

Based on the D-Secure UI screenshots, here's how to integrate the Group Management API with your frontend.

---

## 📦 Component Structure

```
src/
├── pages/
│   ├── ManageGroups.jsx      # Screenshot 3 - List view
│   ├── AddNewGroup.jsx          # Screenshot 2 - Create form
│   └── EditGroup.jsx        # Screenshot 1 - Edit form
├── components/
│   ├── GroupCard.jsx              # Individual group card
│   ├── PermissionSelector.jsx    # Permission checkboxes with categories
│   └── GroupMembersList.jsx    # Group members display
└── services/
    └── groupService.js # API calls
```

---

## 🔌 API Service Setup

### groupService.js

```javascript
const API_BASE = 'http://localhost:4000/api';

// Get JWT token from your auth system
const getAuthHeaders = () => ({
  'Authorization': `Bearer ${localStorage.getItem('token')}`,
  'Content-Type': 'application/json'
});

export const groupService = {
  // Get all groups with search and pagination
getAllGroups: async (search = '', page = 1, pageSize = 10) => {
    const response = await fetch(
      `${API_BASE}/GroupManagement?search=${search}&page=${page}&pageSize=${pageSize}`,
      { headers: getAuthHeaders() }
    );
    return response.json();
  },

  // Get single group details
  getGroup: async (groupId) => {
    const response = await fetch(
      `${API_BASE}/GroupManagement/${groupId}`,
    { headers: getAuthHeaders() }
    );
    return response.json();
  },

  // Create new group
  createGroup: async (groupData) => {
    const response = await fetch(
      `${API_BASE}/GroupManagement`,
      {
        method: 'POST',
      headers: getAuthHeaders(),
        body: JSON.stringify(groupData)
      }
    );
    return response.json();
  },

  // Update group
  updateGroup: async (groupId, groupData) => {
    const response = await fetch(
      `${API_BASE}/GroupManagement/${groupId}`,
      {
        method: 'PUT',
        headers: getAuthHeaders(),
        body: JSON.stringify(groupData)
      }
    );
    return response.json();
  },

  // Delete group
  deleteGroup: async (groupId) => {
    const response = await fetch(
      `${API_BASE}/GroupManagement/${groupId}`,
      {
        method: 'DELETE',
        headers: getAuthHeaders()
      }
    );
    return response.json();
  },

  // Get available permissions
  getAvailablePermissions: async () => {
    const response = await fetch(
      `${API_BASE}/GroupManagement/available-permissions`,
      { headers: getAuthHeaders() }
    );
    return response.json();
  },

  // Get group members
  getGroupMembers: async (groupId) => {
    const response = await fetch(
      `${API_BASE}/GroupManagement/${groupId}/members`,
      { headers: getAuthHeaders() }
    );
    return response.json();
},

  // Get statistics
  getStatistics: async () => {
    const response = await fetch(
  `${API_BASE}/GroupManagement/statistics`,
    { headers: getAuthHeaders() }
    );
    return response.json();
  }
};
```

---

## 📄 Page 1: Manage Groups (Screenshot 3)

### ManageGroups.jsx (React Example)

```jsx
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { groupService } from '../services/groupService';
import GroupCard from '../components/GroupCard';

const ManageGroups = () => {
  const navigate = useNavigate();
  const [groups, setGroups] = useState([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 10,
    totalCount: 0,
    showing: ''
  });

  useEffect(() => {
    fetchGroups();
  }, [search, pagination.page]);

  const fetchGroups = async () => {
    setLoading(true);
    try {
      const data = await groupService.getAllGroups(search, pagination.page, pagination.pageSize);
      setGroups(data.groups);
      setPagination({
        page: data.page,
        pageSize: data.pageSize,
        totalCount: data.totalCount,
        showing: data.showing
      });
    } catch (error) {
      console.error('Error fetching groups:', error);
    }
    setLoading(false);
  };

  const handleDelete = async (groupId) => {
    if (window.confirm('Are you sure you want to delete this group?')) {
      try {
  await groupService.deleteGroup(groupId);
   fetchGroups(); // Refresh list
      } catch (error) {
        alert('Error deleting group: ' + error.message);
      }
    }
  };

  return (
    <div className="manage-groups-container">
      {/* Header */}
      <div className="page-header">
        <div>
       <h1>Manage Groups</h1>
          <p>Create and manage user groups with specific permissions</p>
        </div>
        <button 
          className="btn-primary"
     onClick={() => navigate('/groups/new')}
        >
     + Add New Group
    </button>
      </div>

      {/* Search Bar */}
  <div className="search-bar">
        <input
        type="text"
          placeholder="Search by group name or description..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {/* Groups Grid */}
      <div className="groups-grid">
      {loading ? (
  <div>Loading...</div>
        ) : (
          groups.map(group => (
         <GroupCard
      key={group.groupId}
   group={group}
    onEdit={() => navigate(`/groups/${group.groupId}/edit`)}
              onDelete={() => handleDelete(group.groupId)}
         />
          ))
        )}
      </div>

      {/* Pagination Info */}
      <div className="pagination-info">
        {pagination.showing}
      </div>
    </div>
  );
};

export default ManageGroups;
```

### GroupCard.jsx Component

```jsx
import React from 'react';

const GroupCard = ({ group, onEdit, onDelete }) => {
  return (
    <div className="group-card">
 {/* Header */}
      <div className="card-header">
        <div>
        <h3>{group.groupName}</h3>
          <p className="description">{group.description}</p>
        </div>
        <div className="card-actions">
     <button onClick={onEdit} className="icon-btn" title="Edit">
            <i className="icon-edit"></i>
          </button>
          <button onClick={onDelete} className="icon-btn" title="Delete">
            <i className="icon-delete"></i>
      </button>
 </div>
      </div>

      {/* Stats */}
      <div className="card-stats">
        <div className="stat">
          <label>Users</label>
       <span>{group.userCount}</span>
     </div>
      <div className="stat">
          <label>Licenses</label>
    <span>{group.licenseCount}</span>
        </div>
      </div>

      {/* Permissions */}
      <div className="card-permissions">
        <label>Permissions</label>
        <div className="permission-badges">
          {group.permissions.map((perm, index) => (
       <span key={index} className="badge">{perm}</span>
    ))}
          {group.morePermissions > 0 && (
            <span className="badge more">+{group.morePermissions} more</span>
          )}
        </div>
      </div>

      {/* Footer */}
      <div className="card-footer">
        <small>Created: {group.createdDateFormatted}</small>
      </div>
    </div>
  );
};

export default GroupCard;
```

---

## 📄 Page 2: Add New Group (Screenshot 2)

### AddNewGroup.jsx

```jsx
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { groupService } from '../services/groupService';
import PermissionSelector from '../components/PermissionSelector';

const AddNewGroup = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    groupName: '',
    description: '',
    licenseAllocation: 100,
    permissions: []
  });
  const [permissionCategories, setPermissionCategories] = useState([]);
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});

  useEffect(() => {
    fetchPermissions();
  }, []);

  const fetchPermissions = async () => {
    try {
      const data = await groupService.getAvailablePermissions();
      setPermissionCategories(data.categories);
} catch (error) {
      console.error('Error fetching permissions:', error);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    // Validation
    const newErrors = {};
    if (!formData.groupName) newErrors.groupName = 'Group name is required';
    if (!formData.description) newErrors.description = 'Description is required';
    if (formData.licenseAllocation < 1) newErrors.licenseAllocation = 'License allocation must be at least 1';
    if (formData.permissions.length === 0) newErrors.permissions = 'At least one permission must be selected';

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setLoading(true);
    try {
      const result = await groupService.createGroup(formData);
      alert('Group created successfully!');
      navigate('/groups');
    } catch (error) {
      alert('Error creating group: ' + error.message);
    }
    setLoading(false);
  };

  const handlePermissionChange = (permissionValue, isChecked) => {
    setFormData(prev => ({
      ...prev,
      permissions: isChecked
        ? [...prev.permissions, permissionValue]
  : prev.permissions.filter(p => p !== permissionValue)
    }));
  };

  return (
    <div className="add-group-container">
      <div className="page-header">
 <button onClick={() => navigate('/groups')} className="back-btn">
          ← Back
        </button>
     <h1>Add New Group</h1>
        <p>Create a new user group with specific permissions and license allocation</p>
    </div>

      <form onSubmit={handleSubmit} className="group-form">
        <div className="form-section">
        <h3>Group Information</h3>
  <p className="section-subtitle">Configure the new group settings</p>

          {/* Group Name */}
          <div className="form-group">
            <label>Group Name *</label>
          <input
              type="text"
              placeholder="Enter group name"
   value={formData.groupName}
     onChange={(e) => setFormData({...formData, groupName: e.target.value})}
            className={errors.groupName ? 'error' : ''}
            />
            {errors.groupName && <span className="error-text">{errors.groupName}</span>}
       </div>

 {/* Description */}
 <div className="form-group">
            <label>Description *</label>
            <textarea
          placeholder="Describe the purpose of this group"
 value={formData.description}
     onChange={(e) => setFormData({...formData, description: e.target.value})}
              className={errors.description ? 'error' : ''}
    rows={4}
            />
  {errors.description && <span className="error-text">{errors.description}</span>}
   </div>

          {/* License Allocation */}
          <div className="form-group">
  <label>License Allocation</label>
    <input
  type="number"
            min="1"
      max="10000"
              value={formData.licenseAllocation}
     onChange={(e) => setFormData({...formData, licenseAllocation: parseInt(e.target.value)})}
      />
    <small>Number of licenses allocated to this group</small>
          </div>
        </div>

     {/* Group Permissions */}
        <div className="form-section">
          <h3>Group Permissions *</h3>
  <p className="section-subtitle">Select permissions for this group</p>
  
   <PermissionSelector
            categories={permissionCategories}
  selectedPermissions={formData.permissions}
 onChange={handlePermissionChange}
          />
          {errors.permissions && <span className="error-text">{errors.permissions}</span>}
   </div>

        {/* Selected Permissions Summary */}
        <div className="selected-permissions">
    <strong>Selected Permissions:</strong>
          <div className="permission-badges">
       {formData.permissions.map(perm => (
  <span key={perm} className="badge-selected">
   {perm}
              </span>
    ))}
   </div>
     </div>

        {/* Action Buttons */}
 <div className="form-actions">
        <button type="button" onClick={() => navigate('/groups')} className="btn-cancel">
    Cancel
     </button>
  <button type="submit" disabled={loading} className="btn-primary">
 {loading ? 'Creating...' : 'Create Group'}
    </button>
        </div>
      </form>
    </div>
  );
};

export default AddNewGroup;
```

### PermissionSelector.jsx Component

```jsx
import React from 'react';

const PermissionSelector = ({ categories, selectedPermissions, onChange }) => {
  return (
    <div className="permission-selector">
      {categories.map(category => (
        <div key={category.categoryName} className="permission-category">
          <h4>{category.categoryLabel}</h4>
     <div className="permission-checkboxes">
        {category.permissions.map(permission => (
          <label key={permission.value} className="checkbox-label">
<input
     type="checkbox"
      checked={selectedPermissions.includes(permission.value)}
      onChange={(e) => onChange(permission.value, e.target.checked)}
   />
         <span>{permission.label}</span>
                {permission.description && (
       <small>{permission.description}</small>
      )}
   </label>
         ))}
          </div>
        </div>
      ))}
    </div>
  );
};

export default PermissionSelector;
```

---

## 📄 Page 3: Edit Group (Screenshot 1)

### EditGroup.jsx

```jsx
import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { groupService } from '../services/groupService';
import PermissionSelector from '../components/PermissionSelector';

const EditGroup = () => {
  const { groupId } = useParams();
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    groupName: '',
    description: '',
    licenseAllocation: 100,
    permissions: []
  });
  const [permissionCategories, setPermissionCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadData();
  }, [groupId]);

  const loadData = async () => {
    setLoading(true);
    try {
 // Load group data and permissions in parallel
      const [groupData, permissionsData] = await Promise.all([
    groupService.getGroup(groupId),
        groupService.getAvailablePermissions()
  ]);

      setFormData({
        groupName: groupData.groupName,
description: groupData.description,
        licenseAllocation: groupData.licenseAllocation,
        permissions: groupData.permissions
      });
      setPermissionCategories(permissionsData.categories);
    } catch (error) {
      console.error('Error loading group:', error);
      alert('Error loading group data');
      navigate('/groups');
    }
    setLoading(false);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      await groupService.updateGroup(groupId, formData);
    alert('Group updated successfully!');
      navigate('/groups');
    } catch (error) {
      alert('Error updating group: ' + error.message);
}
    setSaving(false);
  };

  const handlePermissionChange = (permissionValue, isChecked) => {
    setFormData(prev => ({
  ...prev,
 permissions: isChecked
        ? [...prev.permissions, permissionValue]
      : prev.permissions.filter(p => p !== permissionValue)
    }));
  };

if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="edit-group-container">
   <div className="page-header">
        <button onClick={() => navigate('/groups')} className="back-btn">
  ← Back
</button>
  <h1>Edit Group</h1>
     <p>Update group settings and permissions</p>
      </div>

      <form onSubmit={handleSubmit} className="group-form">
 {/* Same form structure as AddNewGroup */}
        {/* ... (copy from AddNewGroup component) ... */}

        <div className="form-actions">
      <button type="button" onClick={() => navigate('/groups')} className="btn-cancel">
          Cancel
          </button>
          <button type="submit" disabled={saving} className="btn-primary">
            {saving ? 'Updating...' : 'Update Group'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default EditGroup;
```

---

## 🎨 Sample CSS Styles

```css
/* Manage Groups Container */
.manage-groups-container {
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
}

.page-header h1 {
  font-size: 28px;
  font-weight: 600;
  margin: 0 0 8px 0;
}

.page-header p {
  color: #666;
  margin: 0;
}

/* Search Bar */
.search-bar {
  margin-bottom: 24px;
}

.search-bar input {
  width: 100%;
  max-width: 500px;
  padding: 12px 16px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
}

/* Groups Grid */
.groups-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 24px;
  margin-bottom: 24px;
}

/* Group Card */
.group-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 20px;
  transition: box-shadow 0.2s;
}

.group-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.card-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}

.card-header h3 {
font-size: 18px;
  font-weight: 600;
  margin: 0 0 4px 0;
}

.description {
  color: #666;
font-size: 14px;
}

.card-actions {
  display: flex;
  gap: 8px;
}

.icon-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: 8px;
  color: #666;
  transition: color 0.2s;
}

.icon-btn:hover {
  color: #007bff;
}

/* Card Stats */
.card-stats {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  margin-bottom: 16px;
  padding: 16px;
  background: #f9fafb;
  border-radius: 8px;
}

.stat label {
  display: block;
  font-size: 12px;
  color: #666;
  margin-bottom: 4px;
}

.stat span {
  display: block;
  font-size: 24px;
  font-weight: 600;
}

/* Permissions */
.card-permissions label {
  display: block;
  font-size: 12px;
  font-weight: 600;
  color: #374151;
  margin-bottom: 8px;
}

.permission-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.badge {
  background: #e0f2fe;
  color: #0369a1;
  padding: 4px 12px;
  border-radius: 16px;
  font-size: 12px;
  font-weight: 500;
}

.badge.more {
  background: #f3f4f6;
  color: #6b7280;
}

.card-footer {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #e5e7eb;
}

.card-footer small {
  color: #9ca3af;
  font-size: 12px;
}

/* Form Styles */
.group-form {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 24px;
}

.form-section {
  margin-bottom: 32px;
}

.form-section h3 {
  font-size: 18px;
  font-weight: 600;
  margin: 0 0 8px 0;
}

.section-subtitle {
  color: #666;
  font-size: 14px;
  margin: 0 0 20px 0;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 8px;
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
}

.form-group input.error,
.form-group textarea.error {
  border-color: #ef4444;
}

.error-text {
  color: #ef4444;
  font-size: 12px;
  display: block;
  margin-top: 4px;
}

.form-group small {
  display: block;
  color: #666;
  font-size: 12px;
  margin-top: 4px;
}

/* Permission Selector */
.permission-selector {
  display: grid;
  gap: 20px;
}

.permission-category h4 {
  font-size: 16px;
  font-weight: 600;
  margin: 0 0 12px 0;
}

.permission-checkboxes {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 12px;
}

.checkbox-label {
  display: flex;
  flex-direction: column;
  padding: 12px;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.checkbox-label:hover {
  background: #f9fafb;
  border-color: #007bff;
}

.checkbox-label input[type="checkbox"] {
  margin-right: 8px;
  width: auto;
}

.checkbox-label span {
  font-weight: 500;
  margin-bottom: 4px;
}

.checkbox-label small {
  color: #666;
  font-size: 12px;
}

/* Form Actions */
.form-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  padding-top: 24px;
  border-top: 1px solid #e5e7eb;
}

.btn-primary {
  background: #007bff;
  color: white;
  padding: 12px 24px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-primary:hover:not(:disabled) {
  background: #0056b3;
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-cancel {
  background: white;
  color: #374151;
  padding: 12px 24px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-cancel:hover {
  background: #f9fafb;
}

.back-btn {
  background: none;
  border: none;
  color: #007bff;
  font-size: 14px;
  cursor: pointer;
  padding: 8px 0;
  margin-bottom: 16px;
}

.back-btn:hover {
  text-decoration: underline;
}

/* Selected Permissions Summary */
.selected-permissions {
  background: #f0f9ff;
  border: 1px solid #bae6fd;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 24px;
}

.selected-permissions strong {
  display: block;
  margin-bottom: 12px;
  color: #0369a1;
}

.badge-selected {
  background: #0284c7;
  color: white;
  padding: 6px 12px;
  border-radius: 16px;
  font-size: 12px;
  font-weight: 500;
  display: inline-block;
  margin: 0 4px 4px 0;
}

/* Pagination */
.pagination-info {
  text-align: center;
  color: #666;
  font-size: 14px;
  padding: 16px 0;
}
```

---

## 🚀 Quick Start Checklist

### Backend Setup
- [x] Controller created: `GroupManagementController.cs`
- [x] Models created: `GroupManagementModels.cs`
- [x] Build successful
- [x] API documentation created

### Frontend Tasks
- [ ] Install dependencies (React Router, etc.)
- [ ] Create API service (`groupService.js`)
- [ ] Create Manage Groups page (list view)
- [ ] Create Add New Group page
- [ ] Create Edit Group page
- [ ] Create reusable components (GroupCard, PermissionSelector)
- [ ] Add CSS styles
- [ ] Test all CRUD operations
- [ ] Add error handling
- [ ] Add loading states

---

## 🧪 Testing Your Integration

### 1. Test API Endpoints
```javascript
// Use browser console or Postman
const token = 'YOUR_JWT_TOKEN';

// Test Get All Groups
fetch('http://localhost:4000/api/GroupManagement', {
  headers: { 'Authorization': `Bearer ${token}` }
})
.then(r => r.json())
.then(console.log);
```

### 2. Test Create Group
```javascript
fetch('http://localhost:4000/api/GroupManagement', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    groupName: 'Test Group',
 description: 'Test Description',
    licenseAllocation: 100,
    permissions: ['BASIC_ACCESS', 'REPORT_GENERATION']
  })
})
.then(r => r.json())
.then(console.log);
```

---

## 📚 Additional Resources

- Full API Documentation: `Documentation/GROUP_MANAGEMENT_COMPLETE_API_GUIDE.md`
- Backend Controller: `BitRaserApiProject/Controllers/GroupManagementController.cs`
- Data Models: `BitRaserApiProject/Models/GroupManagementModels.cs`

---

**Need Help?**
- Check API responses in browser Network tab
- Verify JWT token is valid
- Check user has required permissions
- Review server logs for errors

**Last Updated:** 2024-12-29  
**Compatible with:** .NET 8, React 18+, Vue 3+, Angular 15+
