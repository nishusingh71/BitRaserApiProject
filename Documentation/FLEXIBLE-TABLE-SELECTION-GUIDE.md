# 🎯 Flexible Table Selection for Private Cloud - Complete Guide

## 📋 **Overview**

Ab users **apni marzi ke tables** select kar sakte hain private cloud database ke liye!

**Benefits:**
- ✅ Storage optimization (sirf zaroorti tables)
- ✅ Flexible configuration
- ✅ API automatic routing (main DB ↔ private DB)
- ✅ Per-table control

---

## 🗄️ **Available Tables**

### **Complete List:**

| Table Name | Description | Dependencies | Default |
|------------|-------------|--------------|---------|
| `audit_reports` | Erasure reports | None | ✅ Yes |
| `subuser` | Subuser accounts | None | ✅ Yes |
| `Roles` | Role definitions | None | ✅ Yes |
| `SubuserRoles` | Role assignments | subuser, Roles | ✅ Yes |
| `machines` | Registered devices | None | ❌ No |
| `sessions` | Login/logout sessions | None | ❌ No |
| `logs` | Activity logs | None | ❌ No |
| `commands` | Remote commands | None | ❌ No |
| `groups` | User groups | None | ❌ No |

---

## 📊 **Database Schema Update**

### **1. Add Column to `private_cloud_databases`:**

```sql
-- MySQL/TiDB/MariaDB
ALTER TABLE private_cloud_databases 
ADD COLUMN selected_tables JSON 
COMMENT 'JSON object storing which tables user wants in private cloud';

-- Example data
UPDATE private_cloud_databases 
SET selected_tables = '{
  "audit_reports": true,
  "subuser": true,
  "Roles": true,
  "SubuserRoles": true,
  "machines": false,
  "sessions": false,
  "logs": false,
  "commands": false,
  "groups": false
}'
WHERE user_email = 'user@example.com';
```

### **2. PostgreSQL:**
```sql
ALTER TABLE private_cloud_databases 
ADD COLUMN selected_tables JSONB;
```

### **3. SQL Server:**
```sql
ALTER TABLE private_cloud_databases 
ADD selected_tables NVARCHAR(MAX) 
CHECK (ISJSON(selected_tables) = 1);
```

---

## 🎨 **Frontend Implementation**

### **React/Next.js - Table Selection UI:**

```tsx
// components/TableSelector.tsx
import { useState } from 'react';

interface TableConfig {
  name: string;
  displayName: string;
  description: string;
  dependencies: string[];
  default: boolean;
  icon: string;
}

const AVAILABLE_TABLES: TableConfig[] = [
  {
    name: 'audit_reports',
    displayName: 'Audit Reports',
    description: 'Erasure reports and compliance data',
    dependencies: [],
    default: true,
    icon: '📊'
  },
{
 name: 'subuser',
    displayName: 'Subusers',
    description: 'Subuser accounts and management',
    dependencies: [],
    default: true,
    icon: '👥'
  },
  {
    name: 'Roles',
    displayName: 'Roles',
    description: 'Role definitions for access control',
    dependencies: [],
    default: true,
    icon: '🔐'
  },
  {
    name: 'SubuserRoles',
    displayName: 'Subuser Role Assignments',
    description: 'Links subusers to their roles',
    dependencies: ['subuser', 'Roles'],
    default: true,
    icon: '🔗'
  },
  {
    name: 'machines',
    displayName: 'Machines',
    description: 'Registered devices and hardware',
    dependencies: [],
    default: false,
    icon: '💻'
  },
  {
    name: 'sessions',
    displayName: 'Sessions',
    description: 'Login/logout session tracking',
    dependencies: [],
    default: false,
    icon: '🔑'
  },
  {
    name: 'logs',
    displayName: 'Activity Logs',
    description: 'System activity and audit logs',
    dependencies: [],
    default: false,
    icon: '📝'
  },
  {
    name: 'commands',
    displayName: 'Commands',
    description: 'Remote command execution logs',
    dependencies: [],
    default: false,
    icon: '⚡'
  },
  {
    name: 'groups',
    displayName: 'Groups',
    description: 'User group management',
    dependencies: [],
    default: false,
    icon: '👨‍👩‍👧‍👦'
  }
];

export default function TableSelector({ 
  selectedTables, 
  onSelectionChange 
}: {
  selectedTables: Record<string, boolean>;
  onSelectionChange: (tables: Record<string, boolean>) => void;
}) {
  const handleToggle = (tableName: string) => {
    const table = AVAILABLE_TABLES.find(t => t.name === tableName);
    const newSelection = { ...selectedTables };

    if (newSelection[tableName]) {
      // Deselecting - check if other tables depend on this
      const dependentTables = AVAILABLE_TABLES.filter(t => 
        t.dependencies.includes(tableName) && newSelection[t.name]
      );

      if (dependentTables.length > 0) {
        alert(`Cannot deselect ${tableName}. These tables depend on it: ${
          dependentTables.map(t => t.displayName).join(', ')
        }`);
        return;
   }

   newSelection[tableName] = false;
    } else {
      // Selecting - auto-select dependencies
      newSelection[tableName] = true;
    table?.dependencies.forEach(dep => {
        newSelection[dep] = true;
      });
    }

    onSelectionChange(newSelection);
  };

  const selectedCount = Object.values(selectedTables).filter(Boolean).length;
  const estimatedSize = selectedCount * 10; // Rough estimate: 10MB per table

  return (
    <div className="space-y-4">
      <div className="bg-blue-50 p-4 rounded-lg">
  <h3 className="font-semibold text-blue-900 mb-2">
      📋 Select Tables for Private Cloud
        </h3>
        <p className="text-sm text-blue-700">
          Choose which tables to store in your private database. 
          Required dependencies will be automatically selected.
        </p>
        <div className="mt-2 text-sm text-blue-600">
  Selected: <strong>{selectedCount}</strong> tables 
      | Estimated storage: <strong>~{estimatedSize}MB</strong>
   </div>
  </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
   {AVAILABLE_TABLES.map(table => (
          <div
     key={table.name}
        className={`p-4 border-2 rounded-lg cursor-pointer transition ${
    selectedTables[table.name]
  ? 'border-green-500 bg-green-50'
     : 'border-gray-300 bg-white hover:border-gray-400'
      }`}
 onClick={() => handleToggle(table.name)}
 >
        <div className="flex items-start justify-between">
              <div className="flex items-center space-x-2">
         <span className="text-2xl">{table.icon}</span>
       <input
  type="checkbox"
     checked={selectedTables[table.name]}
      onChange={() => {}}
          className="w-5 h-5"
             />
  </div>
        {table.default && (
                <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded">
     Recommended
        </span>
              )}
      </div>

    <h4 className="font-semibold mt-2">{table.displayName}</h4>
       <p className="text-sm text-gray-600 mt-1">{table.description}</p>

            {table.dependencies.length > 0 && (
              <div className="mt-2 text-xs text-gray-500">
    Requires: {table.dependencies.join(', ')}
              </div>
     )}
 </div>
        ))}
      </div>

      <div className="bg-yellow-50 p-4 rounded-lg">
        <h4 className="font-semibold text-yellow-900 mb-2">⚠️ Important Notes:</h4>
        <ul className="text-sm text-yellow-800 space-y-1">
      <li>• <strong>audit_reports</strong> and <strong>subuser</strong> are recommended for most users</li>
          <li>• <strong>SubuserRoles</strong> requires both <strong>subuser</strong> and <strong>Roles</strong></li>
   <li>• Tables not selected will use the main database</li>
      <li>• You can change this selection later</li>
   </ul>
 </div>
    </div>
  );
}
```

### **Usage in Setup Form:**

```tsx
// pages/PrivateCloudSetup.tsx
import { useState } from 'react';
import TableSelector from '../components/TableSelector';

export default function PrivateCloudSetup() {
  const [selectedTables, setSelectedTables] = useState({
    audit_reports: true,
    subuser: true,
    Roles: true,
    SubuserRoles: true,
    machines: false,
    sessions: false,
  logs: false,
    commands: false,
    groups: false
  });

  const [formData, setFormData] = useState({
    databaseType: 'mysql',
    serverHost: '',
    serverPort: 3306,
    databaseName: '',
  databaseUsername: '',
    databasePassword: '',
    selectedTables: selectedTables, // ✅ Include selected tables
    notes: ''
  });

  const handleSubmit = async () => {
    try {
      const response = await fetch('/api/PrivateCloud/setup', {
   method: 'POST',
        headers: {
          'Content-Type': 'application/json',
 'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          ...formData,
          selectedTables: JSON.stringify(selectedTables) // ✅ Send as JSON string
        })
    });

      const result = await response.json();
      console.log('Setup result:', result);
    } catch (error) {
      console.error('Setup failed:', error);
    }
  };

  return (
    <div className="max-w-4xl mx-auto p-6">
      <h1 className="text-3xl font-bold mb-8">Private Cloud Setup</h1>

      {/* Step 1: Database Connection */}
  <div className="mb-8">
        <h2 className="text-2xl font-semibold mb-4">1. Database Connection</h2>
        {/* Connection form fields... */}
      </div>

 {/* Step 2: Table Selection */}
      <div className="mb-8">
        <h2 className="text-2xl font-semibold mb-4">2. Select Tables</h2>
        <TableSelector
          selectedTables={selectedTables}
          onSelectionChange={setSelectedTables}
        />
      </div>

      {/* Step 3: Review & Submit */}
      <div className="mb-8">
        <h2 className="text-2xl font-semibold mb-4">3. Review Configuration</h2>
        <div className="bg-gray-50 p-4 rounded-lg">
  <h3 className="font-semibold mb-2">Selected Tables:</h3>
       <ul className="space-y-1">
            {Object.entries(selectedTables)
        .filter(([_, selected]) => selected)
              .map(([table]) => (
   <li key={table} className="text-green-600">
          ✓ {table}
        </li>
              ))}
          </ul>
  </div>
      </div>

      <button
        onClick={handleSubmit}
        className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700"
 >
    Complete Setup
      </button>
    </div>
  );
}
```

---

## 🔧 **Backend API Updates**

### **1. Update DTO:**

```csharp
// Models/PrivateCloudDatabaseDto.cs
public class PrivateCloudDatabaseDto
{
    [Required]
public string UserEmail { get; set; } = string.Empty;

 [Required]
    public string DatabaseType { get; set; } = "mysql";

    [Required]
    public string ServerHost { get; set; } = string.Empty;

    [Required]
    public int ServerPort { get; set; } = 3306;

 [Required]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    public string DatabaseUsername { get; set; } = string.Empty;

    [Required]
    public string DatabasePassword { get; set; } = string.Empty;

    // ✅ NEW: Selected tables as JSON string
 public string? SelectedTables { get; set; }

    public string? Notes { get; set; }
}
```

### **2. Update SetupPrivateDatabaseAsync:**

```csharp
// Services/PrivateCloudService.cs
public async Task<bool> SetupPrivateDatabaseAsync(PrivateCloudDatabaseDto dto)
{
    try
    {
      // ... existing validation code ...

     if (existingConfig != null)
        {
   // Update existing
   existingConfig.ConnectionString = EncryptConnectionString(connectionString);
            existingConfig.DatabaseType = dto.DatabaseType;
            existingConfig.ServerHost = dto.ServerHost;
            existingConfig.ServerPort = dto.ServerPort;
            existingConfig.DatabaseName = dto.DatabaseName;
   existingConfig.DatabaseUsername = dto.DatabaseUsername;
      existingConfig.SelectedTables = dto.SelectedTables; // ✅ NEW
          existingConfig.Notes = dto.Notes;
      existingConfig.TestStatus = "success";
            existingConfig.LastTestedAt = DateTime.UtcNow;
    existingConfig.UpdatedAt = DateTime.UtcNow;
    existingConfig.IsActive = true;

    _mainContext.Entry(existingConfig).State = EntityState.Modified;
      }
        else
     {
            // Create new
   var newConfig = new PrivateCloudDatabase
 {
         UserId = user.user_id,
                UserEmail = dto.UserEmail,
        ConnectionString = EncryptConnectionString(connectionString),
      DatabaseType = dto.DatabaseType,
   ServerHost = dto.ServerHost,
      ServerPort = dto.ServerPort,
   DatabaseName = dto.DatabaseName,
       DatabaseUsername = dto.DatabaseUsername,
            SelectedTables = dto.SelectedTables, // ✅ NEW
      Notes = dto.Notes,
           TestStatus = "success",
     LastTestedAt = DateTime.UtcNow,
 IsActive = true,
     CreatedBy = dto.UserEmail
     };

         await _mainContext.Set<PrivateCloudDatabase>().AddAsync(newConfig);
        }

 await _mainContext.SaveChangesAsync();
        return true;
    }
    catch (Exception ex)
    {
  _logger.LogError(ex, "Error setting up private database");
        return false;
    }
}
```

### **3. Update Schema Initialization:**

```csharp
// Services/PrivateCloudService.cs
public async Task<bool> InitializeDatabaseSchemaAsync(string userEmail)
{
    try
    {
        var config = await GetUserPrivateDatabaseAsync(userEmail);
      if (config == null)
        {
            _logger.LogError("Private database configuration not found");
            return false;
        }

        if (config.SchemaInitialized)
        {
      _logger.LogInformation("Schema already initialized");
            return true;
  }

        // ✅ NEW: Parse selected tables
   var selectedTables = ParseSelectedTables(config.SelectedTables);
        
  var connectionString = DecryptConnectionString(config.ConnectionString);

        // ✅ NEW: Create only selected tables
        var success = await CreateSelectedTablesAsync(
            connectionString, 
          config.DatabaseType, 
            selectedTables
    );

        if (success)
        {
            config.SchemaInitialized = true;
            config.SchemaInitializedAt = DateTime.UtcNow;
 config.UpdatedAt = DateTime.UtcNow;
 await _mainContext.SaveChangesAsync();

            _logger.LogInformation("✅ Schema initialized successfully for {Email}", userEmail);
  }

        return success;
}
  catch (Exception ex)
    {
    _logger.LogError(ex, "Error initializing database schema");
      return false;
    }
}

// ✅ NEW: Helper method to parse selected tables
private Dictionary<string, bool> ParseSelectedTables(string? selectedTablesJson)
{
    if (string.IsNullOrEmpty(selectedTablesJson))
    {
        // Default selection
   return new Dictionary<string, bool>
        {
  ["audit_reports"] = true,
            ["subuser"] = true,
 ["Roles"] = true,
            ["SubuserRoles"] = true,
  ["machines"] = false,
         ["sessions"] = false,
            ["logs"] = false,
       ["commands"] = false,
 ["groups"] = false
    };
    }

    try
    {
return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(selectedTablesJson)
            ?? new Dictionary<string, bool>();
    }
    catch
    {
    _logger.LogWarning("Failed to parse selected tables JSON, using defaults");
        return new Dictionary<string, bool>();
    }
}

// ✅ NEW: Create only selected tables
private async Task<bool> CreateSelectedTablesAsync(
    string connectionString, 
    string databaseType, 
    Dictionary<string, bool> selectedTables)
{
    try
    {
        _logger.LogInformation("🏗️ Creating selected tables...");
  
   var normalizedDbType = databaseType.ToLower();
        
   if (!_databaseSchemas.ContainsKey(normalizedDbType))
        {
            _logger.LogError("❌ Unsupported database type: {Type}", databaseType);
        return false;
        }

        var schemas = _databaseSchemas[normalizedDbType];
        
        // ✅ Filter tables based on user selection
        var tablesToCreate = new List<string>();
        
    // Respect dependencies
        var allTables = new[] { "audit_reports", "subuser", "Roles", "SubuserRoles", 
                "machines", "sessions", "logs", "commands", "groups" };
        
   foreach (var table in allTables)
        {
            if (selectedTables.ContainsKey(table) && selectedTables[table])
            {
                tablesToCreate.Add(table);
          
         // Auto-add dependencies
              if (table == "SubuserRoles")
            {
      if (!tablesToCreate.Contains("subuser"))
                 tablesToCreate.Add("subuser");
   if (!tablesToCreate.Contains("Roles"))
  tablesToCreate.Add("Roles");
     }
        }
 }

        _logger.LogInformation("📋 Tables to create: {Tables}", string.Join(", ", tablesToCreate));

  using DbConnection connection = normalizedDbType switch
        {
 "mysql" => new MySqlConnection(connectionString),
"postgresql" => new NpgsqlConnection(connectionString),
   "sqlserver" => new SqlConnection(connectionString),
 _ => throw new NotSupportedException($"Database type {databaseType} not supported")
     };

        await connection.OpenAsync();

        foreach (var tableName in tablesToCreate)
        {
     if (schemas.TryGetValue(tableName, out var schema))
 {
   _logger.LogInformation("🔨 Creating table: {TableName}...", tableName);

     using var command = connection.CreateCommand();
       command.CommandText = schema;
        command.CommandTimeout = 120;

                await command.ExecuteNonQueryAsync();

         _logger.LogInformation("✅ Table created: {TableName}", tableName);
  }
        }

  await connection.CloseAsync();
  _logger.LogInformation("🎉 All selected tables created successfully!");
        return true;
    }
    catch (Exception ex)
    {
     _logger.LogError(ex, "❌ Error creating selected tables");
     return false;
    }
}
```

---

## 🔄 **API Routing Logic**

### **DynamicDbContextFactory - Table-aware Routing:**

```csharp
// Factories/DynamicDbContextFactory.cs
public class DynamicDbContextFactory
{
    private readonly ITenantConnectionService _tenantConnectionService;
    private readonly IPrivateCloudService _privateCloudService;
    private readonly ILogger<DynamicDbContextFactory> _logger;

    public async Task<ApplicationDbContext> CreateDbContextAsync()
    {
        var userEmail = _tenantConnectionService.GetCurrentUserEmail();
     
        if (string.IsNullOrEmpty(userEmail))
        {
   return CreateMainDbContext();
        }

 // Check if user has private cloud enabled
        var isPrivateCloudUser = await _privateCloudService.IsPrivateCloudUserAsync(userEmail);
        
      if (!isPrivateCloudUser)
        {
            return CreateMainDbContext();
        }

        // Get private cloud configuration
    var config = await _privateCloudService.GetUserPrivateDatabaseAsync(userEmail);
      
        if (config == null || !config.IsActive || !config.SchemaInitialized)
        {
   return CreateMainDbContext();
        }

     // ✅ NEW: Store selected tables in context for routing decisions
        var connectionString = await _privateCloudService.GetConnectionStringAsync(userEmail);
        var context = CreatePrivateDbContext(connectionString);
        
        // Attach metadata for routing
        context.ChangeTracker.StateChanged += (sender, e) =>
     {
            // This allows controllers to check which tables are in private DB
        };

      return context;
}

    // ✅ NEW: Check if specific table is in private cloud
    public async Task<bool> IsTableInPrivateCloudAsync(string userEmail, string tableName)
    {
        var config = await _privateCloudService.GetUserPrivateDatabaseAsync(userEmail);
    
     if (config == null || string.IsNullOrEmpty(config.SelectedTables))
        {
         return false;
        }

        var selectedTables = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(
            config.SelectedTables
        );

      return selectedTables != null && 
               selectedTables.ContainsKey(tableName) && 
    selectedTables[tableName];
    }
}
```

---

## ✅ **Testing**

### **1. Setup with Table Selection:**

```bash
POST http://localhost:5000/api/PrivateCloud/setup
Authorization: Bearer {token}
Content-Type: application/json

{
  "userEmail": "test@example.com",
  "databaseType": "mysql",
  "serverHost": "gateway01.ap-southeast-1.prod.aws.tidbcloud.com",
  "serverPort": 4000,
  "databaseName": "Cloud_Erase_Private",
  "databaseUsername": "root",
  "databasePassword": "password",
  "selectedTables": "{\"audit_reports\": true, \"subuser\": true, \"Roles\": true, \"SubuserRoles\": true, \"machines\": false}"
}
```

### **2. Initialize Schema (Only Selected Tables):**

```bash
POST http://localhost:5000/api/PrivateCloud/initialize-schema
Authorization: Bearer {token}

# Expected: Creates only selected tables
# - audit_reports ✅
# - subuser ✅
# - Roles ✅
# - SubuserRoles ✅
# - machines ❌ (not selected)
```

### **3. Validate Schema:**

```bash
POST http://localhost:5000/api/PrivateCloud/validate-schema
Authorization: Bearer {token}

# Expected Response:
{
  "isValid": true,
  "message": "All selected tables exist",
  "existingTables": ["audit_reports", "subuser", "Roles", "SubuserRoles"],
  "missingTables": []
}
```

---

## 🎉 **Summary**

### **What This Feature Provides:**

1. ✅ **Flexible Table Selection** - Users choose which tables to store privately
2. ✅ **Automatic Dependency Resolution** - Related tables auto-selected
3. ✅ **Smart API Routing** - APIs automatically route to correct database
4. ✅ **Storage Optimization** - Only selected tables consume space
5. ✅ **Easy Configuration** - Simple checkbox UI
6. ✅ **Validation** - Ensures all dependencies are met

### **User Benefits:**

- 💰 **Cost Savings** - Pay only for tables you need
- 🚀 **Performance** - Smaller database = faster queries
- 🔒 **Control** - Full control over data storage
- 📊 **Flexibility** - Change selection anytime
- 🎯 **Simplicity** - User-friendly interface

---

**Perfect Implementation! Ab users apni marzi ke tables select kar sakte hain! 🎉✨**
