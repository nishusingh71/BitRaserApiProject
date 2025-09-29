# 🚀 BitRaser API Project

A comprehensive .NET 8 Web API for device management, user authentication, license management, and audit report generation with advanced role-based access control.

## 📖 **Complete Documentation**

📁 **All documentation has been organized in the [`Documentation`](Documentation/) folder:**

### **Quick Links**
- **🚀 [Get Started](Documentation/Setup-Configuration/QUICK_START_GUIDE.md)** - Quick setup guide
- **📚 [API Documentation](Documentation/API-Documentation/)** - Complete API guides
- **🔧 [Troubleshooting](Documentation/Troubleshooting/)** - Error fixes and solutions
- **📖 [Implementation Guides](Documentation/Guides/)** - Feature implementation

### **📁 Documentation Structure**
```
Documentation/
├── 📚 API-Documentation/          # Enhanced controllers, authentication, dynamic systems
├── 🛠️  Setup-Configuration/       # Quick start, database setup
├── 📖 Guides/                     # Implementation, PDF export guides
└── 🔧 Troubleshooting/            # Console errors, Swagger fixes, JSON issues
```

## 🎯 **Key Features**

### **✅ Enhanced Email-Based Controllers**
- **Users Management** - Complete CRUD with email-based operations
- **Subuser Management** - Hierarchical user system with role assignments
- **Machine Registration** - Device licensing and activation
- **Session Management** - Auto-expiration and extended sessions
- **Advanced Logging** - Search, export, and analytics
- **Command Execution** - Status tracking and bulk operations
- **Audit Reports** - PDF generation and comprehensive reporting

### **✅ Advanced Security**
- **JWT Authentication** - Secure token-based authentication
- **Role-Based Access Control** - 5-tier permission system (SuperAdmin → User)
- **Dynamic Permissions** - 67+ granular permissions
- **Email-Based Authorization** - Operations based on email identifiers
- **Multi-level Hierarchy** - Parent-child user relationships

### **✅ System Architecture**
- **Dynamic Route Discovery** - Automatic API endpoint mapping
- **Permission Management** - Runtime permission assignment
- **Database Migrations** - Automated schema management
- **Real-time Analytics** - Comprehensive statistics and reporting
- **Export Functionality** - CSV, PDF export capabilities

## 🚀 **Quick Start**

### **1. Prerequisites**
- .NET 8 SDK
- MySQL 8.0+
- Visual Studio 2022 or VS Code

### **2. Setup**
```bash
# Clone the repository
git clone https://github.com/nishusingh71/BitRaserApiProject

# Navigate to project
cd BitRaserApiProject

# Restore packages
dotnet restore

# Update database
dotnet ef database update

# Run the application
dotnet run
```

### **3. Access**
- **Swagger UI:** http://localhost:4000/swagger
- **Base API:** http://localhost:4000/api
- **Health Check:** http://localhost:4000/api/health

## 📊 **API Statistics**

| Category | Controllers | Endpoints | Status |
|----------|-------------|-----------|---------|
| **Enhanced APIs** | 7 controllers | 45+ endpoints | ✅ Active |
| **Authentication** | 2 controllers | 8 endpoints | ✅ Active |
| **System Management** | 4 controllers | 15+ endpoints | ✅ Active |
| **Legacy APIs** | 5 controllers | 20+ endpoints | ✅ Active |
| **Total** | **18+ controllers** | **90+ endpoints** | **✅ Production Ready** |

## 🛠️ **Technology Stack**

- **Framework:** .NET 8 Web API
- **Database:** MySQL 8.0 with Entity Framework Core
- **Authentication:** JWT Bearer Tokens
- **Documentation:** Swagger/OpenAPI 3.0
- **PDF Generation:** QuestPDF
- **ORM:** Entity Framework Core 8.0
- **Security:** BCrypt password hashing
- **Logging:** Built-in .NET logging with Serilog

## 📞 **Support & Documentation**

### **📖 Full Documentation**
**→ [Complete Documentation Hub](Documentation/README.md)**

### **🔧 Need Help?**
- **Setup Issues:** [Setup Configuration Guides](Documentation/Setup-Configuration/)
- **API Questions:** [API Documentation](Documentation/API-Documentation/) 
- **Errors/Bugs:** [Troubleshooting Guides](Documentation/Troubleshooting/)
- **Implementation:** [Implementation Guides](Documentation/Guides/)

### **👨‍💻 Developer**
- **Name:** Dhruv Rai  
- **Email:** Dhruv.rai@stellarinfo.com
- **Project Version:** 1.0
- **Last Updated:** Current

## 🎊 **Project Status**

**✅ Production Ready** - All core features implemented and tested
- ✅ Email-based operations across all controllers
- ✅ Role-based security with 67+ permissions  
- ✅ Dynamic system with auto-discovery
- ✅ Comprehensive documentation
- ✅ Error handling and logging
- ✅ Database migrations and seed data
- ✅ Export and reporting functionality

---

**🚀 Ready to explore? Start with the [Documentation Hub](Documentation/README.md)! 🚀**