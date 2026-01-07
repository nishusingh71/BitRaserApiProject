using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BitRaserApiProject;
using BitRaserApiProject.Services;
using BitRaserApiProject.Repositories;
using BitRaserApiProject.BackgroundServices;
using BitRaserApiProject.Middleware;
using BitRaserApiProject.Swagger;
using BitRaserApiProject.Converters;  // ✅ ADD: Custom DateTime Converters
using BitRaserApiProject.Factories;   // ✅ ADD: Factories for multi-tenant support
using BitRaserApiProject.Diagnostics; // ✅ ADD: TiDB Diagnostics & Observability
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;  // ✅ ADD: For DataProtection in Docker
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using QuestPDF.Infrastructure;
using DotNetEnv;

// Enhanced startup with structured logging
var builder = WebApplication.CreateBuilder(args);

// Configure enhanced logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Load environment variables with error handling
try
{
    DotNetEnv.Env.Load();
    Console.WriteLine("✅ Environment variables loaded successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Could not load .env file: {ex.Message}");
}

// Configure QuestPDF license
try
{
    QuestPDF.Settings.License = LicenseType.Community;
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ QuestPDF license configuration failed: {ex.Message}");
}

// Get configuration values with enhanced fallbacks - Environment variables take priority
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ApplicationDbContextConnection")
    ?? builder.Configuration.GetConnectionString("ApplicationDbContextConnection")
    ?? throw new InvalidOperationException("Database connection string is required. Set ConnectionStrings__ApplicationDbContextConnection environment variable.");

var cloudEraseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CloudEraseConnection")
    ?? builder.Configuration.GetConnectionString("CloudEraseConnection");

// Log if CloudEraseConnection is configured
if (!string.IsNullOrEmpty(cloudEraseConnectionString))
{
    Console.WriteLine("✅ CloudEraseConnection configured (available but not used by multi-tenant system)");
}

// ✅ NOTE: Private cloud connection strings are NOT stored in config
// They are dynamically fetched from private_cloud_databases table per user
// This allows unlimited users to have their own private databases
// CloudEraseConnection (if set) can be used for system-level operations

var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key is required. Set Jwt__Key environment variable with a strong 256-bit key.");

var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer is required. Set Jwt__Issuer environment variable.");

var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience")
    ?? builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT Audience is required. Set Jwt__Audience environment variable.");

// Validate JWT key strength
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException($"JWT Key must be at least 32 characters (256 bits). Current length: {jwtKey.Length}");
}

Console.WriteLine($"✅ JWT configured - Issuer: {jwtIssuer}, Audience: {jwtAudience}");

// ✅ Configure DataProtection for Docker/Production environments
// This prevents FileSystemXmlRepository and XmlKeyManager warnings
var keysPath = Environment.GetEnvironmentVariable("DATAPROTECTION_KEYS_PATH") ?? "/app/keys";
var keysDirectory = new DirectoryInfo(keysPath);
if (!keysDirectory.Exists)
{
    try { keysDirectory.Create(); } catch { /* Ignore in read-only containers */ }
}

builder.Services.AddDataProtection()
    .SetApplicationName("BitRaserApiProject")
    .PersistKeysToFileSystem(keysDirectory)
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Configure CORS with comprehensive settings - FIXED FOR VERCEL
builder.Services.AddCors(options =>
{
    // Universal policy for all environments
    options.AddPolicy("AllowVercelFrontend", policy =>
    {
        policy.WithOrigins(
            "https://dsecure-frontend.vercel.app",
            "https://dsecuretech.com",
            "https://www.dsecuretech.com",
            "http://localhost:5173"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });

    // Development policy - allows specific origins only (NO AllowAnyOrigin)
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });

    // Production policy - restricted origins for security
    options.AddPolicy("ProductionPolicy", policy =>
    {
        var allowedOrigins = new List<string>();

        // Get allowed origins from environment variables
        var envOrigins = Environment.GetEnvironmentVariable("CORS__AllowedOrigins")
 ?? builder.Configuration["CORS:AllowedOrigins"];

        if (!string.IsNullOrEmpty(envOrigins))
        {
            allowedOrigins.AddRange(envOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(origin => origin.Trim()));
        }

        // Default allowed origins for common frontend frameworks + Vercel
        if (allowedOrigins.Count == 0)
        {
            allowedOrigins.AddRange(new[]
                {
         "https://dsecure-frontend.vercel.app",
          "http://localhost:3000",
    "http://localhost:3001",
                "http://localhost:4200",
      "http://localhost:5173",
                "http://localhost:8080",
            "http://localhost:8081",
                "http://localhost:5000",
"http://localhost:5001",
    "https://localhost:3000",
          "https://localhost:4200",
   "https://localhost:5174",
       "https://localhost:8080",
            "https://dsecuretech.com",
         "https://www.dsecuretech.com"
 });
        }

        policy.WithOrigins(allowedOrigins.ToArray())
            .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "Accept", "Origin", "X-Requested-With", "X-No-Encryption")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });

    // Strict policy for high-security environments
    options.AddPolicy("StrictPolicy", policy =>
    {
        var strictOrigins = Environment.GetEnvironmentVariable("CORS__StrictOrigins")
      ?? builder.Configuration["CORS:StrictOrigins"];

        if (!string.IsNullOrEmpty(strictOrigins))
        {
            var origins = strictOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
          .Select(origin => origin.Trim())
             .ToArray();

            policy.WithOrigins(origins)
                   .WithMethods("GET", "POST", "PUT", "DELETE")
                  .WithHeaders("Authorization", "Content-Type")
            .AllowCredentials();
        }
        else
        {
            // Default to localhost + Vercel if no strict origins specified
            policy.WithOrigins(
           "http://localhost:3000",
         "https://localhost:3000",
                 "https://dsecure-frontend.vercel.app",
         "https://dsecuretech.com"
                        )
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
               .WithHeaders("Authorization", "Content-Type")
                .AllowCredentials();
        }
    });
});

// ✅ Configure MAIN database context - SIMPLE STATIC CONNECTION
// Controllers that need Private Cloud routing should use DynamicDbContextFactory
// This avoids synchronous database lookups during DI resolution which causes login failures
// ✅ Configure MAIN database context with Connection Pooling and Diagnostics Interceptor
// WHY AddDbContextPool? - Reuses DbContext instances, reduces allocation overhead
// Pool size 20 is optimal for TiDB Cloud free tier (max ~100 connections)
builder.Services.AddDbContextPool<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)),
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            mysqlOptions.CommandTimeout(30);
            // ✅ Connection pool size limit for TiDB
            mysqlOptions.MaxBatchSize(100);
        });

    // ✅ ADD: Diagnostics Interceptor for SQL query monitoring
    var interceptor = serviceProvider.GetService<DbDiagnosticsInterceptor>();
    if (interceptor != null)
    {
        options.AddInterceptors(interceptor);
    }

    // Enhanced logging and performance options - ONLY in Development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    else
    {
        // Production: No sensitive data logging
        options.EnableDetailedErrors(false);
    }

    // Performance optimizations
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}, poolSize: 20);  // ✅ Pool size limited to 20 for TiDB

// Configure JWT Authentication with enhanced security
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };

        options.Events = new JwtBearerEvents
        {
            // ✅ CRITICAL: Skip JWT authentication for webhook endpoints
            // Webhooks use signature verification, NOT JWT tokens
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path.Value?.ToLowerInvariant();
                if (path != null && (path.Contains("/webhook") || path.Contains("/api/webhooks")))
                {
                    // Skip JWT processing for webhooks
                    context.NoResult();
                    return Task.CompletedTask;
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
       {
           if (builder.Environment.IsDevelopment())
           {
               Console.WriteLine($"🔒 JWT Authentication failed: {context.Exception.Message}");
           }
           return Task.CompletedTask;
       },
            OnTokenValidated = context =>
          {
              if (builder.Environment.IsDevelopment())
              {
                  Console.WriteLine($"🔒 JWT Token validated for: {context.Principal?.Identity?.Name}");
              }
              return Task.CompletedTask;
          },
            OnChallenge = context =>
   {
       if (builder.Environment.IsDevelopment())
       {
           Console.WriteLine($"🔒 JWT Challenge: {context.Error} - {context.ErrorDescription}");
       }
       return Task.CompletedTask;
   }
        };
    });

// Enhanced Authorization with policies
builder.Services.AddAuthorization(options =>
{
    // Define custom policies for different permission levels
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("SuperAdmin"));

    options.AddPolicy("AdminOrAbove", policy =>
   policy.RequireRole("SuperAdmin", "Admin"));

    options.AddPolicy("ManagerOrAbove", policy =>
        policy.RequireRole("SuperAdmin", "Admin", "Manager"));

    options.AddPolicy("SupportOrAbove", policy =>
  policy.RequireRole("SuperAdmin", "Admin", "Manager", "Support"));

    // Add permission-based policies
    options.AddPolicy("CanManageUsers", policy =>
   policy.RequireClaim("permission", "UserManagement", "FullAccess"));

    options.AddPolicy("CanViewReports", policy =>
        policy.RequireClaim("permission", "ReportAccess", "FullAccess"));
});

// Register core services
builder.Services.AddScoped<IDynamicPermissionService, DynamicPermissionService>();
builder.Services.AddScoped<IRoleBasedAuthService, RoleBasedAuthService>();
builder.Services.AddScoped<IUserDataService, UserDataService>();
builder.Services.AddScoped<DynamicRouteService>();
builder.Services.AddScoped<MigrationUtilityService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<IDapperService, DapperService>();  // ✅ NEW: Dapper Service for high-performance queries
builder.Services.AddScoped<IQuotaService, QuotaService>();    // ✅ NEW: Quota & Limits Service
builder.Services.AddScoped<IActivityLogService, ActivityLogService>(); // ✅ NEW: Activity Logging Service
builder.Services.AddScoped<ILicenseExportService, LicenseExportService>(); // ✅ License Export (Excel/PDF)
builder.Services.AddScoped<ICloudLicenseService, CloudLicenseService>(); // ✅ Cloud License Activation
builder.Services.AddScoped<IOfflineLicenseService, OfflineLicenseService>(); // ✅ Offline License Activation
builder.Services.AddSingleton<IRsaTokenService, RsaTokenService>(); // ✅ RSA Token Signing (singleton for key persistence)
builder.Services.AddSingleton<ILicenseKeyGenerator, LicenseKeyGenerator>(); // ✅ License Key Generator
builder.Services.AddScoped<IPurchaseDomainService, PurchaseDomainService>(); // ✅ Purchase Domain Service

// ✅ PERFORMANCE: Centralized caching services
builder.Services.AddScoped<BitRaserApiProject.Services.Abstractions.IUserContextService, BitRaserApiProject.Services.Implementations.UserContextService>();
builder.Services.AddScoped<BitRaserApiProject.Services.Abstractions.IPrivateCloudConfigCache, BitRaserApiProject.Services.Implementations.PrivateCloudConfigCache>();

// ✅ FORGOT PASSWORD SERVICES - OTP AND EMAIL (OLD - RETAINED FOR BACKWARD COMPATIBILITY)
builder.Services.AddSingleton<IOtpService, OtpService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ✅ FORGOT/RESET PASSWORD WITHOUT EMAIL - NEW IMPLEMENTATION
builder.Services.AddScoped<IForgotPasswordRepository, ForgotPasswordRepository>();
builder.Services.AddScoped<IForgotPasswordService, ForgotPasswordService>();

// ✅ AUTO-CLEANUP BACKGROUND SERVICE FOR EXPIRED PASSWORD RESET REQUESTS (Runs once per day)
builder.Services.AddHostedService<ForgotPasswordCleanupBackgroundService>();

// ✅ KEEP-ALIVE SERVICE TO PREVENT RENDER.COM FREE TIER SPIN-DOWN
builder.Services.AddHttpClient();  // Register HttpClient for keep-alive pings
builder.Services.AddHostedService<KeepAliveBackgroundService>();

// ✅ RATE LIMIT CLEANUP BACKGROUND SERVICE
builder.Services.AddHostedService<RateLimitCleanupBackgroundService>();

// ✅ PRIVATE CLOUD DATABASE SERVICE
builder.Services.AddScoped<IPrivateCloudService, PrivateCloudService>();

// ✅ DATABASE CONTEXT FACTORY - Multi-tenant database routing
builder.Services.AddScoped<IDatabaseContextFactory, DatabaseContextFactory>();

// ✅ POLAR PAYMENT SERVICE - Payment integration with Polar.sh
builder.Services.AddHttpClient("PolarApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5
});
builder.Services.AddScoped<IPolarPaymentService, PolarPaymentService>();

// ✅ DODO PAYMENT SERVICE - Payment integration with Dodo Payments
builder.Services.AddHttpClient("DodoApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5
});
builder.Services.AddScoped<IDodoPaymentService, DodoPaymentService>();

// ✅ HYBRID EMAIL SYSTEM - SendGrid + Microsoft Graph with Quota Management
builder.Services.AddScoped<BitRaserApiProject.Services.Email.IEmailQuotaService, BitRaserApiProject.Services.Email.EmailQuotaService>();
builder.Services.AddScoped<BitRaserApiProject.Services.Email.IEmailProvider, BitRaserApiProject.Services.Email.Providers.SendGridEmailProvider>();
builder.Services.AddScoped<BitRaserApiProject.Services.Email.IEmailProvider, BitRaserApiProject.Services.Email.Providers.MicrosoftGraphEmailProvider>();
builder.Services.AddScoped<BitRaserApiProject.Services.Email.IEmailOrchestrator, BitRaserApiProject.Services.Email.EmailProviderOrchestrator>();
builder.Services.AddScoped<BitRaserApiProject.Services.Email.ExcelExportService>();

// ✅ LICENSE VALIDATION HOOK - Placeholder for future product-based license integration
builder.Services.AddScoped<BitRaserApiProject.Services.License.ILicenseValidationHook, BitRaserApiProject.Services.License.DefaultLicenseValidationHook>();

// ✅ HYBRID MULTI-TENANT SUPPORT - Automatic tenant routing
builder.Services.AddHttpContextAccessor();  // Required for reading JWT claims
builder.Services.AddScoped<ITenantConnectionService, TenantConnectionService>();
builder.Services.AddScoped<DynamicDbContextFactory>();

// ✅ ENTERPRISE CACHING SYSTEM - IMemoryCache with prefix invalidation
// Singleton for cache persistence across requests
builder.Services.AddEnterpriseCaching(options =>
{
    options.CompactionPercentage = 0.10; // gentle cleanup
});

// ✅ TIDB DIAGNOSTICS & OBSERVABILITY SYSTEM
// Thread-safe in-memory metrics store (Singleton for global metrics)
builder.Services.AddSingleton<DiagnosticsMetricsStore>();
// DB Command Interceptor for SQL query monitoring
builder.Services.AddSingleton<DbDiagnosticsInterceptor>();
// TiDB Health Service for cluster inspection
// TiDB Health Service for cluster inspection
builder.Services.AddScoped<ITiDbHealthService, TiDbHealthService>();

// ✅ CONFIGURE FORWARDED HEADERS (Crucial for Ngrok/Proxies/Vercel)
// This ensures we see the REAL Client IP, not the Proxy IP (localhost)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        
    // Clear known networks/proxies to trust headers from anywhere (Safe for this setup where we check the IP later)
    // In strict production behind a specific load balancer, you would list its IP here.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Controllers with enhanced JSON options
builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    // Enhanced JSON serialization options
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.AllowTrailingCommas = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;

    // ✅ ADD: Custom DateTime converters for ISO 8601 format with UTC
    options.JsonSerializerOptions.Converters.Add(new Iso8601DateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new Iso8601NullableDateTimeConverter());

    // Add enum converter
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();

// Configure enhanced Swagger with comprehensive documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DSecure API Project",
        Version = "v2.0",
        Description = "Enhanced API for managing devices, users, licenses, and PDF reports with advanced Role-Based Access Control",
        Contact = new OpenApiContact
        {
            Name = "Dhruv Rai",
            Email = "Dhruv.rai@stellarinfo.com",
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Enhanced XML documentation
    try
    {
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Could not load XML documentation: {ex.Message}");
    }

    // Enhanced schema configuration
    c.CustomSchemaIds(type =>
    {
        if (type.FullName != null)
        {
            return type.FullName.Replace("+", ".");
        }
        return type.Name;
    });

    c.SupportNonNullableReferenceTypes();
    c.OperationFilter<SwaggerDefaultValues>();

    // ✅ NEW: Add X-No-Encryption header parameter to all endpoints
    c.OperationFilter<NoEncryptionHeaderOperationFilter>();

    // ❌ BASE64 EMAIL ENCODING FILTERS - DISABLED FOR SWAGGER UI
    // Backend still handles Base64 encoding/decoding automatically
    // Swagger UI shows normal emails for better user experience
    // c.ParameterFilter<BitRaserApiProject.Filters.Base64EmailParameterFilter>();
    // c.OperationFilter<BitRaserApiProject.Filters.Base64EmailOperationFilter>();
    // c.DocumentFilter<BitRaserApiProject.Filters.Base64EmailDocumentFilter>();

    // Enhanced security definitions
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
     {
       new OpenApiSecurityScheme
         {
    Reference = new OpenApiReference
     {
            Type = ReferenceType.SecurityScheme,
           Id = "Bearer"
    }
            },
            Array.Empty<string>()
 }
    });

    // Add examples for better API documentation
    c.EnableAnnotations();
});

// Configure server URLs
var port = Environment.GetEnvironmentVariable("PORT") ?? "4000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
Console.WriteLine($"🚀 Server configured to start on port: {port}");

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════════════════
// ✅ CRITICAL FIX: Initialize Email Quotas at Startup
// Without this, hybrid email providers report "not available" and ALL emails fail
// ═══════════════════════════════════════════════════════════════════════════════
try
{
    using var scope = app.Services.CreateScope();
    var quotaService = scope.ServiceProvider.GetService<BitRaserApiProject.Services.Email.IEmailQuotaService>();
    if (quotaService != null)
    {
        quotaService.InitializeQuotasAsync().GetAwaiter().GetResult();
        Console.WriteLine("✅ Email quota service initialized successfully");
    }
    else
    {
        Console.WriteLine("⚠️ Email quota service not registered - emails may fail");
    }
}
catch (Exception emailInitEx)
{
    // Don't crash app, but log critical warning
    Console.WriteLine($"⚠️ Failed to initialize email quotas: {emailInitEx.Message}");
    Console.WriteLine("⚠️ Email system may use fallback providers");
}

// CRITICAL FIX: Apply CORS BEFORE other middleware
// This must be one of the first middleware in the pipeline
app.UseCors("AllowVercelFrontend");

// ✅ ENABLE FORWARDED HEADERS - MUST BE AT THE TOP
// This reads X-Forwarded-For so Request.RemoteIpAddress becomes the real user IP
app.UseForwardedHeaders();
Console.WriteLine("🌐 CORS configured for Vercel Frontend: https://dsecure-frontend.vercel.app");

// ✅ CRITICAL: Enable request body buffering for webhook endpoints EARLY in pipeline
// This allows webhook controllers to read the request body even after middlewares process it
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
    
    // Enable buffering for all webhook endpoints
    if (path.Contains("/webhook") || 
        path.Contains("/api/webhooks") || 
        path.Contains("/api/payments/dodo") ||
        path.Contains("/api/payments/polar"))
    {
        // Enable buffering BEFORE any read
        context.Request.EnableBuffering();
    }
    
    await next();
});

// Configure Swagger ONLY in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DSecure API Project v2.0");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "DSecure API Project - Enhanced Documentation";
        options.DefaultModelsExpandDepth(-1);
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        options.EnableTryItOutByDefault();

        // Inject JavaScript for automatic token clearing after logout
        options.HeadContent = @"
            <script>
       document.addEventListener('DOMContentLoaded', function() {
         console.log('🔐 Swagger Token Auto-Clear loaded');
     
 // Override fetch to monitor logout API calls
    const originalFetch = window.fetch;
      window.fetch = function(...args) {
           const url = args[0];
        
      return originalFetch.apply(this, args).then(response => {
  // Check if this is a logout API call
              if (url && url.toString().includes('/logout') && response.ok) {
        response.clone().json().then(data => {
         if (data && data.swaggerLogout) {
              console.log('🚪 Logout detected - clearing Swagger tokens');
   
             setTimeout(() => {
           // Clear all possible token storage locations
             localStorage.removeItem('swagger-ui-bearer-token');
            localStorage.removeItem('authToken');
         localStorage.removeItem('auth-token');
          localStorage.removeItem('jwt');
     localStorage.removeItem('bearer-token');
   
       sessionStorage.removeItem('swagger-ui-bearer-token');
      sessionStorage.removeItem('authToken');
    sessionStorage.removeItem('auth-token');
          
          // Logout from Swagger UI
     if (window.ui && window.ui.authActions) {
             window.ui.authActions.logout(['Bearer']);
              console.log('🔓 Swagger UI logout executed');
  }
                
                 // Refresh to show open lock icon
   setTimeout(() => {
  window.location.reload();
    }, 500);
         
    }, 1000);
     }
   }).catch(e => {
             console.log('Response parsing failed:', e);
       });
           }
          
         return response;
    }).catch(error => {
      console.error('Fetch error:', error);
         return Promise.reject(error);
         });
    };
        
         // Add global function for manual token clearing
             window.clearSwaggerToken = function() {
            localStorage.removeItem('swagger-ui-bearer-token');
    sessionStorage.removeItem('swagger-ui-bearer-token');
      if (window.ui && window.ui.authActions) {
 window.ui.authActions.logout(['Bearer']);
      }
           location.reload();
    console.log('✅ Swagger token manually cleared');
         };
    });
  </script>
            <style>
       .swagger-ui .auth-wrapper .authorize { 
         border: 2px solid #49cc90; 
  }
       .swagger-ui .auth-wrapper .authorize.unlocked { 
         border: 2px solid #f93e3e; 
          }
 </style>
        ";
    });
}

// Security headers middleware
app.Use(async (context, next) =>
{
    // Basic security headers
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' https://sandbox-api.polar.sh https://api.polar.sh https://test.dodopayments.com https://live.dodopayments.com; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";
    
    // Permissions Policy
    context.Response.Headers["Permissions-Policy"] = 
        "geolocation=(), " +
        "payment=()";
        // "microphone=(), " +
        // "camera=(), " +
        // "payment=();
        // "usb=(), " +
        // "magnetometer=(), " +
        // "gyroscope=(), " +
        // "accelerometer=()";

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
    }

    await next();
});

app.UseEmailSecurity();

// ✅ HTTPS Redirection - Skip in Docker/environments without HTTPS
// Set DISABLE_HTTPS_REDIRECT=true in Docker to skip
var disableHttpsRedirect = Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT");
if (string.IsNullOrEmpty(disableHttpsRedirect) || disableHttpsRedirect.ToLower() != "true")
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

// ✅ IP BINDING - Prevent Token Theft
// Verify that the request IP matches the token's bound IP
app.UseMiddleware<BitRaserApiProject.Middleware.IpBindingMiddleware>();

// ✅ RATE LIMITING MIDDLEWARE - Protect against abuse
// Place AFTER Authentication so we can identify users
// - Private Cloud Users: 500 requests/min
// - Normal Users: 100 requests/min  
// - Forgot Password: 5 requests/min
// - Unauthenticated: 50 requests/min
app.UseRateLimiting();

// ✅ RESPONSE ENCRYPTION MIDDLEWARE - AES-256-CBC
// Place AFTER Authorization and BEFORE other custom middleware
// This encrypts all API responses automatically
app.UseResponseEncryption();

// ✅ TIDB DIAGNOSTICS: Request-level DB tracking middleware
// Place BEFORE DatabaseContext middleware to track all DB operations
app.UseDbRequestTracking();

// ✅ PRIVATE CLOUD: Automatic database context routing middleware
// This middleware automatically detects user from JWT and injects appropriate DB context
app.UseDatabaseContextMiddleware();

// Enhanced request logging in development
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogDebug("📡 {Method} {Path} - {Time}",
     context.Request.Method,
            context.Request.Path,
            DateTime.Now.ToString("HH:mm:ss"));

        await next();

        logger.LogDebug("📡 Response: {StatusCode} - {Time}",
  context.Response.StatusCode,
     DateTime.Now.ToString("HH:mm:ss"));
    });
}

app.MapControllers();

// Startup completion message with enhanced information
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
appLogger.LogInformation("🎉 DSecure API Project (Enhanced) v2.0 started successfully!");
appLogger.LogInformation("📖 Swagger UI available at: http://localhost:{Port}/swagger", port);
appLogger.LogInformation("🔗 Base URL: http://localhost:{Port}", port);
appLogger.LogInformation("🌍 Environment: {Environment}", app.Environment.EnvironmentName);
appLogger.LogInformation("🌐 CORS enabled for: https://dsecure-frontend.vercel.app");
app.Logger.LogInformation("CORS enabled for: https://dsecuretech.com");

try
{
    app.Run();
}
catch (Exception ex)
{
    appLogger.LogCritical(ex, "❌ Application terminated unexpectedly: {Message}", ex.Message);
    throw;
}

// ✅ STATIC HELPER: Decrypt Private Cloud connection string for DI factory
// This is a copy of TenantConnectionService.DecryptConnectionString for use in Program.cs
// static string? DecryptConnectionStringStatic(string encryptedConnectionString, string encryptionKey)
// {
//     try
//     {
//         if (string.IsNullOrEmpty(encryptedConnectionString))
//             return null;

//         // Handle already decrypted connection strings
//         if (encryptedConnectionString.Contains("Server=") || encryptedConnectionString.Contains("server="))
//             return encryptedConnectionString;

//         var fullCipher = Convert.FromBase64String(encryptedConnectionString);

//         using var aes = System.Security.Cryptography.Aes.Create();
//         aes.Key = System.Text.Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
//         aes.Mode = System.Security.Cryptography.CipherMode.CBC;
//         aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

//         var iv = new byte[16];
//         var cipher = new byte[fullCipher.Length - 16];

//         Array.Copy(fullCipher, 0, iv, 0, 16);
//         Array.Copy(fullCipher, 16, cipher, 0, cipher.Length);

//         aes.IV = iv;

//         using var decryptor = aes.CreateDecryptor();
//         var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
//         return System.Text.Encoding.UTF8.GetString(decryptedBytes);
//     }
//     catch
//     {
//         return null;
//     }
// }
