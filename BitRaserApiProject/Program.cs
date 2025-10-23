using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BitRaserApiProject;
using BitRaserApiProject.Services;
using BitRaserApiProject.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

// Get configuration values with enhanced fallbacks
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ApplicationDbContextConnection")
    ?? builder.Configuration.GetConnectionString("ApplicationDbContextConnection")
    ?? throw new InvalidOperationException("Database connection string is required");

var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key")
    ?? builder.Configuration["Jwt:Key"]
    ?? (builder.Environment.IsDevelopment() ? "YourSuperSecretKeyThatIsAtLeast32CharactersLong123456789!" : null)
    ?? throw new InvalidOperationException("JWT Key is required");

var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "BitRaserAPI";

var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "BitRaserAPIUsers";

// Configure CORS with comprehensive settings
builder.Services.AddCors(options =>
{
    // Development policy - allows all origins (for testing)
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

        // Default allowed origins for common frontend frameworks
        if (allowedOrigins.Count == 0)
        {
            allowedOrigins.AddRange(new[]
            {
                "http://localhost:3000",    // React default
                "http://localhost:3001",    // Alternative React port
                "http://localhost:4200",    // Angular default
                "http://localhost:5173",    // Vite default
                "http://localhost:8080",    // Vue default
                "http://localhost:8081",    // Alternative Vue port
                "http://localhost:5000",    // .NET default
                "http://localhost:5001",    // .NET HTTPS default
                "https://localhost:3000",   // HTTPS variants
                "https://localhost:4200",
                "https://localhost:5174",
                "https://localhost:8080",
                "https://dsecure-frontend.vercel.app",
                "https://dsecuretech.com"
            });
        }

        policy.WithOrigins(allowedOrigins.ToArray())
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
              .WithHeaders("Authorization", "Content-Type", "Accept", "Origin", "X-Requested-With")
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
            // Default to localhost only if no strict origins specified
            policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "https://dsecuretech.com")
                  .WithMethods("GET", "POST", "PUT", "DELETE","PATCH")
                  .WithHeaders("Authorization", "Content-Type")
                  .AllowCredentials();
        }
    });
});

// Configure database with enhanced options
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)), 
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            
            mysqlOptions.CommandTimeout(30);
        });
    
    // Enhanced logging and performance options
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
    
    // Performance optimizations
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

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

// Add memory cache
builder.Services.AddMemoryCache();

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
    
    // Add custom converters for better serialization
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();

// Configure enhanced Swagger with comprehensive documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DSecure Api",
        Version = "2.0",
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

// Configure CORS policy based on environment
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentPolicy");
    Console.WriteLine("🌐 CORS configured for Development (Allow All Origins)");
}
else if (app.Environment.IsProduction())
{
    var corsPolicy = Environment.GetEnvironmentVariable("CORS__Policy") ?? "ProductionPolicy";
    app.UseCors(corsPolicy);
    Console.WriteLine($"🌐 CORS configured for Production (Policy: {corsPolicy})");
}
else
{
    app.UseCors("ProductionPolicy");
    Console.WriteLine("🌐 CORS configured with Production Policy");
}

// Configure Swagger for all environments
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BitRaser API Project v2.0");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "D-Secure API - Enhanced Documentation";
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
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    
    await next();
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

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
appLogger.LogInformation("🎉 BitRaser API Project (Enhanced) started successfully!");
appLogger.LogInformation("📖 Swagger UI available at: http://localhost:{Port}/swagger", port);
appLogger.LogInformation("🔗 Base URL: http://localhost:{Port}", port);
appLogger.LogInformation("🌍 Environment: {Environment}", app.Environment.EnvironmentName);

try
{
    app.Run();
}
catch (Exception ex)
{
    appLogger.LogCritical(ex, "❌ Application terminated unexpectedly: {Message}", ex.Message);
    throw;
}
