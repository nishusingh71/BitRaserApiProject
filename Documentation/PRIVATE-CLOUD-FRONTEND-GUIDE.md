# 🚀 Private Cloud Setup Guide - Complete Frontend Integration

## 📋 Overview

Yeh guide complete step-by-step process batata hai ki kaise user apni **TiDB** ya kisi bhi MySQL-compatible database ko configure kar sakta hai aur apna saara data (Audit Reports, Subusers, Machines, Sessions, Logs) us database mein store kar sakta hai.

---

## 🗄️ **Database Tables Structure**

User ki private database mein yeh saare tables automatically create honge **with proper relationships**:

### **Table Creation Order (Dependency-based):**

```
1. users (Parent table - no dependencies)
2. groups         (Independent table)
3. subuser    (depends on users)
4. machines       (depends on users, subuser)
5. audit_reports  (depends on users)
6. sessions       (depends on users)
7. logs     (depends on users)
8. commands     (depends on users)
```

### **Foreign Key Relationships:**

```
users (Parent)
  ↓
  ├─→ subuser (via user_email)
  ├─→ machines (via user_email)
  ├─→ audit_reports (via client_email)
  ├─→ sessions (via user_email)
  ├─→ logs (via user_email)
└─→ commands (via user_email)

subuser
  └─→ machines (via subuser_email)
```

---

## 🎨 **Frontend Setup Wizard (React/Next.js)**

### **Complete Setup Flow:**

```tsx
// pages/PrivateCloudSetup.tsx
import { useState, useEffect } from 'react';
import axios from 'axios';

interface SetupStep {
  step: number;
  title: string;
  description: string;
  completed: boolean;
}

export default function PrivateCloudSetup() {
  const [currentStep, setCurrentStep] = useState(1);
  const [accessStatus, setAccessStatus] = useState(null);
  const [formData, setFormData] = useState({
    databaseType: 'mysql',
serverHost: '',
    serverPort: 3306,
    databaseName: '',
    databaseUsername: '',
    databasePassword: '',
    storageLimitMb: null,
    notes: ''
  });
  const [testResult, setTestResult] = useState(null);
  const [validationResult, setValidationResult] = useState(null);
  const [loading, setLoading] = useState(false);

  // Step 1: Check Access
  useEffect(() => {
    checkAccess();
  }, []);

  const checkAccess = async () => {
  try {
  const { data } = await axios.get('/api/PrivateCloud/check-access');
      setAccessStatus(data);

    if (data.isSchemaInitialized) {
   setCurrentStep(6); // Already setup complete
      } else if (data.isConfigured) {
        setCurrentStep(4); // Move to schema initialization
  }
    } catch (error) {
      console.error('Error checking access:', error);
    }
  };

  // Step 2: Setup Database Configuration
  const handleSetupDatabase = async () => {
    setLoading(true);
    try {
      const { data } = await axios.post('/api/PrivateCloud/setup', formData);
   console.log(data.message);
    setCurrentStep(3); // Move to test connection
    } catch (error) {
      console.error('Setup failed:', error);
      alert('Setup failed: ' + error.response?.data?.message);
    } finally {
      setLoading(false);
    }
  };

  // Step 3: Test Connection
  const handleTestConnection = async () => {
    setLoading(true);
    try {
      const { data } = await axios.post('/api/PrivateCloud/test');
      setTestResult(data);
  
    if (data.success) {
      console.log('✅ Connection successful!');
      setCurrentStep(4); // Move to initialize schema
  } else {
        alert('Connection test failed: ' + data.message);
   }
    } catch (error) {
      console.error('Connection test failed:', error);
      alert('Connection test failed');
  } finally {
      setLoading(false);
    }
  };

  // Step 4: Initialize Schema
  const handleInitializeSchema = async () => {
    setLoading(true);
  try {
   const { data } = await axios.post('/api/PrivateCloud/initialize-schema');
    console.log(data.message);
      setCurrentStep(5); // Move to validate schema
    } catch (error) {
   console.error('Schema initialization failed:', error);
      alert('Schema initialization failed');
    } finally {
      setLoading(false);
    }
  };

  // Step 5: Validate Schema
  const handleValidateSchema = async () => {
    setLoading(true);
    try {
      const { data } = await axios.post('/api/PrivateCloud/validate-schema');
   setValidationResult(data);
      
      if (data.isValid) {
        console.log('✅ All tables created successfully!');
        setCurrentStep(6); // Setup complete
      } else {
      alert(`Missing tables: ${data.missingTables.join(', ')}`);
      }
    } catch (error) {
      console.error('Schema validation failed:', error);
    } finally {
      setLoading(false);
    }
  };

  // Step 6: View Required Tables
  const [requiredTables, setRequiredTables] = useState([]);

  const loadRequiredTables = async () => {
    try {
    const { data } = await axios.get('/api/PrivateCloud/required-tables');
      setRequiredTables(data.tables);
    } catch (error) {
      console.error('Error loading tables:', error);
    }
  };

  if (!accessStatus) {
    return <div>Loading...</div>;
  }

  if (!accessStatus.hasPrivateCloudAccess) {
return (
 <div className="alert alert-warning">
   <h3>⚠️ Private Cloud Feature Not Available</h3>
        <p>Contact administrator to enable private cloud access for your account.</p>
        <p>Admin needs to set <code>is_private_cloud = TRUE</code> for your user.</p>
      </div>
    );
  }

  return (
    <div className="private-cloud-setup">
      <h1>🔐 Private Cloud Database Setup</h1>
      
      {/* Progress Indicator */}
      <div className="progress-steps">
        {[1, 2, 3, 4, 5, 6].map(step => (
   <div 
  key={step} 
     className={`step ${currentStep >= step ? 'active' : ''} ${currentStep > step ? 'completed' : ''}`}
          >
      {step}
          </div>
      ))}
  </div>

      {/* Step 1: Database Type */}
      {currentStep === 1 && (
    <div className="setup-step">
<h2>Step 1: Select Database Type</h2>
        <p>Choose your database type (Currently supports MySQL/TiDB)</p>
          
          <select
      value={formData.databaseType}
            onChange={(e) => setFormData({...formData, databaseType: e.target.value})}
      className="form-select"
          >
       <option value="mysql">MySQL / TiDB</option>
 <option value="postgresql">PostgreSQL (Coming Soon)</option>
            <option value="sqlserver">SQL Server (Coming Soon)</option>
          </select>

          <div className="info-box">
   <h4>📝 What is TiDB?</h4>
      <p>TiDB is a MySQL-compatible distributed database. You can use your TiDB connection string here.</p>
 <p>Example: <code>gateway01.ap-southeast-1.prod.aws.tidbcloud.com:4000</code></p>
          </div>

          <button onClick={() => setCurrentStep(2)} className="btn btn-primary">
            Next →
          </button>
        </div>
      )}

    {/* Step 2: Database Connection Details */}
    {currentStep === 2 && (
        <div className="setup-step">
     <h2>Step 2: Enter Database Connection Details</h2>
          
     <div className="form-group">
    <label>Server Host / IP Address</label>
     <input
  type="text"
              placeholder="e.g., localhost or gateway01.ap-southeast-1.prod.aws.tidbcloud.com"
      value={formData.serverHost}
              onChange={(e) => setFormData({...formData, serverHost: e.target.value})}
     className="form-control"
          />
         <small>For TiDB Cloud, use the gateway URL from your cluster connection info</small>
          </div>

  <div className="form-group">
     <label>Port</label>
    <input
 type="number"
  placeholder="3306 (MySQL/TiDB) or 5432 (PostgreSQL)"
  value={formData.serverPort}
              onChange={(e) => setFormData({...formData, serverPort: parseInt(e.target.value)})}
   className="form-control"
            />
        </div>

       <div className="form-group">
    <label>Database Name</label>
            <input
            type="text"
    placeholder="e.g., my_private_db"
     value={formData.databaseName}
     onChange={(e) => setFormData({...formData, databaseName: e.target.value})}
  className="form-control"
   />
      </div>

          <div className="form-group">
         <label>Username</label>
 <input
        type="text"
    placeholder="Database username"
              value={formData.databaseUsername}
      onChange={(e) => setFormData({...formData, databaseUsername: e.target.value})}
           className="form-control"
            />
    </div>

     <div className="form-group">
   <label>Password</label>
            <input
  type="password"
    placeholder="Database password"
      value={formData.databasePassword}
              onChange={(e) => setFormData({...formData, databasePassword: e.target.value})}
    className="form-control"
     />
          </div>

        <div className="form-group">
        <label>Storage Limit (MB) - Optional</label>
            <input
              type="number"
   placeholder="e.g., 1024 for 1GB limit"
      value={formData.storageLimitMb || ''}
        onChange={(e) => setFormData({...formData, storageLimitMb: parseInt(e.target.value) || null})}
              className="form-control"
            />
   </div>

<div className="form-group">
         <label>Notes - Optional</label>
    <textarea
        placeholder="Any notes about this database configuration"
        value={formData.notes}
              onChange={(e) => setFormData({...formData, notes: e.target.value})}
      className="form-control"
       rows={3}
    />
     </div>

   <div className="button-group">
  <button onClick={() => setCurrentStep(1)} className="btn btn-secondary">
           ← Back
   </button>
       <button 
   onClick={handleSetupDatabase} 
        className="btn btn-primary"
        disabled={loading}
       >
            {loading ? 'Saving...' : 'Save & Test Connection →'}
            </button>
   </div>
        </div>
      )}

  {/* Step 3: Test Connection */}
      {currentStep === 3 && (
        <div className="setup-step">
   <h2>Step 3: Test Database Connection</h2>
          <p>We'll verify that your database is accessible.</p>

      {!testResult ? (
       <button 
    onClick={handleTestConnection} 
  className="btn btn-primary btn-lg"
          disabled={loading}
            >
       {loading ? 'Testing Connection...' : '🔌 Test Connection'}
            </button>
          ) : (
       <div>
    {testResult.success ? (
                <div className="alert alert-success">
     <h3>✅ Connection Successful!</h3>
       <ul>
        <li>Server Version: {testResult.serverVersion}</li>
        <li>Response Time: {testResult.responseTimeMs}ms</li>
   <li>Schema Exists: {testResult.schemaExists ? 'Yes' : 'No'}</li>
      {testResult.missingTables?.length > 0 && (
        <li>Missing Tables: {testResult.missingTables.join(', ')}</li>
    )}
       </ul>
        </div>
          ) : (
                <div className="alert alert-danger">
      <h3>❌ Connection Failed</h3>
        <p>{testResult.message}</p>
<p>Error: {testResult.error}</p>
        </div>
 )}

    <div className="button-group">
          <button onClick={() => setCurrentStep(2)} className="btn btn-secondary">
    ← Edit Configuration
    </button>
    {testResult.success && (
    <button onClick={() => setCurrentStep(4)} className="btn btn-primary">
    Next: Initialize Schema →
      </button>
                )}
     </div>
        </div>
    )}
        </div>
  )}

      {/* Step 4: Initialize Schema */}
    {currentStep === 4 && (
        <div className="setup-step">
     <h2>Step 4: Initialize Database Schema</h2>
          <p>We will create the following tables in your private database:</p>

      <div className="table-list">
   <h4>📋 Required Tables:</h4>
        <ul>
  <li>✅ <strong>users</strong> - Main user accounts</li>
            <li>✅ <strong>subuser</strong> - Sub-users under main users</li>
            <li>✅ <strong>machines</strong> - Registered machines/devices</li>
   <li>✅ <strong>audit_reports</strong> - Data erasure audit reports</li>
            <li>✅ <strong>sessions</strong> - User login sessions</li>
              <li>✅ <strong>logs</strong> - System activity logs</li>
 <li>✅ <strong>commands</strong> - Remote commands</li>
 <li>✅ <strong>groups</strong> - User groups</li>
    </ul>
       </div>

     <div className="info-box">
            <h4>ℹ️ Important Information:</h4>
      <ul>
    <li>All tables will have proper foreign key relationships</li>
              <li>Indexes will be created for optimal performance</li>
  <li>Your existing data will NOT be affected</li>
              <li>This process may take 30-60 seconds</li>
            </ul>
          </div>

     <button 
          onClick={handleInitializeSchema} 
 className="btn btn-primary btn-lg"
     disabled={loading}
    >
            {loading ? 'Creating Tables...' : '🏗️ Initialize Schema'}
        </button>
 </div>
      )}

      {/* Step 5: Validate Schema */}
      {currentStep === 5 && (
        <div className="setup-step">
     <h2>Step 5: Validate Database Schema</h2>
     <p>Verifying that all tables were created successfully...</p>

          {!validationResult ? (
    <button 
           onClick={handleValidateSchema} 
      className="btn btn-primary btn-lg"
      disabled={loading}
            >
    {loading ? 'Validating...' : '✅ Validate Schema'}
            </button>
          ) : (
      <div>
              {validationResult.isValid ? (
          <div className="alert alert-success">
         <h3>✅ Schema Validation Successful!</h3>
           <p>All {validationResult.existingTables.length} required tables have been created.</p>
    <ul>
    {validationResult.existingTables.map(table => (
           <li key={table}>✅ {table}</li>
     ))}
       </ul>
                </div>
     ) : (
                <div className="alert alert-warning">
  <h3>⚠️ Some Tables are Missing</h3>
   <p>Missing tables: {validationResult.missingTables.join(', ')}</p>
       <button onClick={handleInitializeSchema} className="btn btn-warning">
          Retry Schema Creation
       </button>
  </div>
     )}

    {validationResult.isValid && (
          <button onClick={() => setCurrentStep(6)} className="btn btn-success btn-lg">
    Complete Setup →
         </button>
      )}
            </div>
  )}
        </div>
      )}

      {/* Step 6: Setup Complete */}
      {currentStep === 6 && (
        <div className="setup-step">
<div className="success-card">
          <h2>🎉 Setup Complete!</h2>
            <p>Your private cloud database is now configured and ready to use.</p>

   <div className="setup-summary">
 <h3>📊 Configuration Summary:</h3>
      <table className="table">
     <tbody>
    <tr>
         <td><strong>Database Type:</strong></td>
  <td>{accessStatus?.databaseType || 'MySQL'}</td>
   </tr>
          <tr>
    <td><strong>Status:</strong></td>
         <td className="text-success">✅ Active</td>
    </tr>
         <tr>
                <td><strong>Schema Initialized:</strong></td>
     <td className="text-success">✅ Yes</td>
         </tr>
    <tr>
       <td><strong>Last Tested:</strong></td>
  <td>{new Date(accessStatus?.lastTested).toLocaleString()}</td>
         </tr>
  </tbody>
     </table>
      </div>

 <div className="next-steps">
    <h3>🚀 Next Steps:</h3>
     <ul>
            <li>Your audit reports will now be stored in your private database</li>
          <li>All subusers you create will be stored in your private database</li>
         <li>Machine registrations will be stored in your private database</li>
          <li>Sessions and logs will be isolated to your private database</li>
      </ul>
         </div>

            <div className="button-group">
   <button onClick={() => window.location.href = '/dashboard'} className="btn btn-primary btn-lg">
                Go to Dashboard
     </button>
     <button onClick={() => window.location.href = '/reports'} className="btn btn-secondary">
          View Reports
  </button>
          </div>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## 🎨 **CSS Styling**

```css
/* styles/PrivateCloudSetup.module.css */

.private-cloud-setup {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
}

.progress-steps {
  display: flex;
  justify-content: space-between;
  margin: 30px 0;
  padding: 0 20px;
}

.step {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #e0e0e0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  color: #666;
  transition: all 0.3s;
}

.step.active {
  background: #2196F3;
  color: white;
}

.step.completed {
  background: #4CAF50;
  color: white;
}

.setup-step {
  background: white;
  padding: 30px;
  border-radius: 10px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  margin: 20px 0;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  font-weight: 600;
  margin-bottom: 8px;
  color: #333;
}

.form-control {
  width: 100%;
  padding: 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 14px;
}

.form-control:focus {
  outline: none;
  border-color: #2196F3;
  box-shadow: 0 0 0 3px rgba(33, 150, 243, 0.1);
}

.info-box {
  background: #E3F2FD;
  border-left: 4px solid #2196F3;
  padding: 15px;
  margin: 20px 0;
  border-radius: 4px;
}

.info-box h4 {
  margin-top: 0;
  color: #1976D2;
}

.alert {
  padding: 20px;
  border-radius: 8px;
  margin: 20px 0;
}

.alert-success {
  background: #E8F5E9;
  border: 1px solid #4CAF50;
  color: #2E7D32;
}

.alert-danger {
  background: #FFEBEE;
  border: 1px solid #F44336;
  color: #C62828;
}

.alert-warning {
  background: #FFF3E0;
  border: 1px solid #FF9800;
  color: #E65100;
}

.button-group {
  display: flex;
  gap: 10px;
  margin-top: 20px;
}

.btn {
  padding: 12px 24px;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: #2196F3;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #1976D2;
}

.btn-secondary {
  background: #757575;
  color: white;
}

.btn-success {
  background: #4CAF50;
  color: white;
}

.btn-lg {
  padding: 16px 32px;
  font-size: 16px;
}

.table-list ul {
  list-style: none;
  padding: 0;
}

.table-list li {
  padding: 10px;
  border-bottom: 1px solid #eee;
}

.success-card {
  text-align: center;
  padding: 40px;
}

.success-card h2 {
  color: #4CAF50;
  font-size: 32px;
  margin-bottom: 20px;
}

.setup-summary {
  margin: 30px 0;
  text-align: left;
}

.table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 15px;
}

.table td {
  padding: 12px;
  border-bottom: 1px solid #eee;
}

.text-success {
  color: #4CAF50;
  font-weight: 600;
}

.next-steps {
  text-align: left;
  margin: 30px 0;
}

.next-steps ul {
  line-height: 1.8;
}
```

---

## 📝 **API Endpoint Summary for Frontend**

### **Complete API Flow:**

```javascript
// 1. Check Access
GET /api/PrivateCloud/check-access
Response: {
  hasPrivateCloudAccess: true,
  isConfigured: false,
  isSchemaInitialized: false
}

// 2. Setup Database
POST /api/PrivateCloud/setup
Body: {
  databaseType: "mysql",
  serverHost: "gateway01.ap-southeast-1.prod.aws.tidbcloud.com",
  serverPort: 4000,
  databaseName: "Cloud_Erase",
  databaseUsername: "2tdeFNZMcsWKkDR.root",
  databasePassword: "76wtaj1GZkg7Qhek",
  storageLimitMb: 1024
}

// 3. Test Connection
POST /api/PrivateCloud/test
Response: {
  success: true,
  message: "Connection successful",
  serverVersion: "8.0.11-TiDB-v7.5.0",
  responseTimeMs: 245,
  schemaExists: false,
  missingTables: ["users", "subuser", "machines", ...]
}

// 4. Get Required Tables (Optional - for display)
GET /api/PrivateCloud/required-tables
Response: {
  tables: ["users", "subuser", "machines", "audit_reports", "sessions", "logs", "commands", "groups"],
  totalCount: 8
}

// 5. Initialize Schema
POST /api/PrivateCloud/initialize-schema
Response: {
  message: "Database schema initialized successfully",
  note: "All required tables have been created in your private database"
}

// 6. Validate Schema
POST /api/PrivateCloud/validate-schema
Response: {
  isValid: true,
  message: "All tables exist",
  existingTables: ["users", "subuser", "machines", ...],
  missingTables: [],
  requiredTables: ["users", "subuser", "machines", ...]
}

// 7. Get Current Configuration
GET /api/PrivateCloud/config
Response: {
  configId: 1,
  userId: 123,
  userEmail: "user@example.com",
  connectionString: "***ENCRYPTED***",
  databaseType: "mysql",
  serverHost: "gateway01.ap-southeast-1...",
  isActive: true,
  schemaInitialized: true
}
```

---

## ✅ **Success Checklist for Frontend**

- [ ] User has `is_private_cloud = TRUE` in database
- [ ] Access check shows `hasPrivateCloudAccess: true`
- [ ] User can enter database credentials
- [ ] Connection test passes
- [ ] Schema initialization completes
- [ ] Schema validation passes
- [ ] User can view configuration
- [ ] Dashboard shows private database status

---

**Perfect! Ab user apni TiDB/MySQL database configure kar sakta hai aur uska saara data isolated storage mein jayega! 🎉🔐**

**Next: Build karo aur test karo! 🚀**
