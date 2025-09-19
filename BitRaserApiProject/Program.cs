//using Microsoft.EntityFrameworkCore;
//using BitRaserApiProject;
//using Microsoft.OpenApi.Models;  // Import your DbContext namespace

//var builder = WebApplication.CreateBuilder(args);

//// Set up the MySQL connection string from appsettings.json
//string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//// Set the MySQL version (update this if you are using a different version)
//var serverVersion = new MySqlServerVersion(new Version(8, 0, 34)); // Use the correct MySQL version (8.0.34 in your case)

//// Add MySQL DbContext with version configuration
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseMySql(connectionString, serverVersion));

//// Register the DatabaseInitializer service for database creation
//builder.Services.AddSingleton(new DatabaseInitializer(connectionString));

//// Add services to the container
//builder.Services.AddControllers();

//// Configure Swagger/OpenAPI
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    // Customize the OpenAPI metadata
//    c.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "DhruvAPI Project",    // Set your desired title here
//        Version = "1.0",  // Ensure this is explicitly set
//        Description = "API for managing devices, users, licenses, and  operations", // Add a description if needed
//        Contact = new OpenApiContact
//        {
//            Name = "Dhruv Rai",  // Add your name or company name
//            Email = "Dhruv.rai@stellarinfo.com", // Add your contact email
//            //Url = new Uri("https://your-website.com") // Add your website URL if needed
//        },
//        //License = new OpenApiLicense
//        //{
//        //    Name = "MIT",  // Specify the license name if needed
//        //    Url = new Uri("https://opensource.org/licenses/MIT") // URL to license
//        //}
//    });

//    // Additional Swagger settings if needed
//});


//var app = builder.Build();

//// Ensure database initialization on startup
//using (var scope = app.Services.CreateScope())
//{
//    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
//    dbInitializer.InitializeDatabase();
//}

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(options =>
//    {
//        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dhruv API Project v1");
//        options.RoutePrefix = "swagger";  // Keep it accessible under '/swagger'
//    });
//}

//// Enable HTTPS redirection if required (Uncomment if needed)
// app.UseHttpsRedirection();

//// Configure authorization middleware (if using authentication)
//app.UseAuthorization();

//// Map controller routes
//app.MapControllers();

//// Run the application
//app.Run();


using BitRaserApiProject;
//using BitRaserApiProject.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseMySql(builder.Configuration.GetConnectionString("AppDbContextConnection"),
//        new MySqlServerVersion(new Version(8, 0, 21))));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ApplicationDbContextConnection"),
        new MySqlServerVersion(new Version(8, 0, 21))));

//builder.Services.AddSingleton(new DatabaseInitializer(connectionString));

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DhruvAPI Project",
        Version = "1.0",
        Description = "API for managing devices, users, licenses, and operations",
        Contact = new OpenApiContact
        {
            Name = "Dhruv Rai",
            Email = "Dhruv.rai@stellarinfo.com",
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Bearer token **_only_**. Example: `Bearer abcdef12345`",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            new List<string>()
        }
    });
});

builder.WebHost.UseUrls("http://0.0.0.0:4000");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
   // var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    //dbInitializer.InitializeDatabase();
}

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dhruv API Project v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
