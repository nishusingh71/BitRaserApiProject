# Quick Fix Reference - Model Organization

## 🎯 Model Location Guide

### ✅ Correct Structure

```
BitRaserApiProject/
├── Models/
│   └── AllModels.cs              ← All model definitions here
├── ApplicationDbContext.cs        ← Only DbContext configuration
└── Services/
    └── SecurityHelpers.cs         ← Security utilities (if separated)
```

## 📝 Model Definition Template

### In `Models/AllModels.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace BitRaserApiProject.Models
{
    public class YourModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [JsonIgnore]
        public ICollection<RelatedModel>? RelatedModels { get; set; }
    }
}
```

## 🔧 ApplicationDbContext Template

### In `ApplicationDbContext.cs`:

```csharp
using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options) { }

        // DbSets - NO model definitions here!
        public DbSet<YourModel> YourModels { get; set; }
        
        // For namespace conflicts, use fully qualified names
        public DbSet<Models.Route> Routes { get; set; }  // ✅ Good
        // public DbSet<Route> Routes { get; set; }      // ❌ Bad (conflicts with ASP.NET)

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Entity configurations here
            modelBuilder.Entity<YourModel>()
                .HasKey(m => m.Id);
                
            // For namespace conflicts
            modelBuilder.Entity<Models.Route>()
                .HasKey(r => r.RouteId);
        }
    }
}
```

## 🚫 Common Mistakes to Avoid

### ❌ DON'T: Define models in ApplicationDbContext.cs
```csharp
// ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    // ❌ WRONG - Don't define models here!
    public class MyModel 
    {
        public int Id { get; set; }
    }
}
```

### ❌ DON'T: Use ambiguous type names
```csharp
// ApplicationDbContext.cs
public DbSet<Route> Routes { get; set; }  // ❌ Ambiguous with ASP.NET Route
```

### ❌ DON'T: Forget navigation properties
```csharp
// Models/AllModels.cs
public class Parent
{
    public int Id { get; set; }
    // ❌ Missing: public ICollection<Child> Children { get; set; }
}
```

## ✅ Best Practices

### 1. Single Source of Truth
```csharp
// ✅ All models in Models/AllModels.cs
namespace BitRaserApiProject.Models
{
    public class User { }
    public class Session { }
    public class Log { }
}
```

### 2. Proper Using Statements
```csharp
// Controllers
using BitRaserApiProject.Models;  // ✅ Always include

// Services
using BitRaserApiProject.Models;  // ✅ Always include
```

### 3. Navigation Properties for Relationships
```csharp
public class User
{
    [JsonIgnore]  // Prevent circular references
    public ICollection<Session>? Sessions { get; set; }
}

public class Session
{
    [JsonIgnore]
    public User? User { get; set; }
}
```

### 4. Fully Qualified Names for Conflicts
```csharp
// When there's a namespace conflict
public DbSet<Models.Route> Routes { get; set; }
modelBuilder.Entity<Models.Route>().HasKey(r => r.RouteId);
```

## 🔍 Quick Troubleshooting

### Error: "Type 'X' exists in both assemblies"
**Solution**: Remove duplicate definition, keep only in `Models/AllModels.cs`

### Error: "'Route' is ambiguous"
**Solution**: Use `Models.Route` or fully qualified name

### Error: "Does not contain definition for 'NavigationProperty'"
**Solution**: Add navigation property to model class

### Error: "Type or namespace 'X' could not be found"
**Solution**: Add `using BitRaserApiProject.Models;` to file

## 📋 Verification Checklist

- [ ] All models defined in `Models/AllModels.cs`
- [ ] No model definitions in `ApplicationDbContext.cs`
- [ ] All controllers have `using BitRaserApiProject.Models;`
- [ ] All services have `using BitRaserApiProject.Models;`
- [ ] Navigation properties defined for all relationships
- [ ] Namespace conflicts resolved with fully qualified names
- [ ] `[JsonIgnore]` on navigation properties to prevent circular refs
- [ ] Project builds without errors: `dotnet build`

## 🎯 Quick Commands

```bash
# Verify no duplicate models
grep -r "public class User" --include="*.cs"

# Check all using statements
grep -r "using BitRaserApiProject.Models" --include="*.cs"

# Clean and rebuild
dotnet clean && dotnet build --no-incremental

# Check for build errors
dotnet build 2>&1 | grep -i "error"
```

---

**Pro Tip**: When adding a new model, always add it to `Models/AllModels.cs` first, then add the corresponding `DbSet` in `ApplicationDbContext.cs`.
