using System.Reflection;
using System.Text;
using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Endpoints;
using Etali_Data_Collection_Web_API.Models.Auth;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add CORS services
var allowedOrigins = builder.Configuration["AllowedOrigins"] ?? "*";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins == "*")
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins.Split(','))
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Register application services
builder.Services.AddSingleton<GameConfiguration>();

// Configure Database settings with validation
builder.Services.AddOptions<DatabaseConfiguration>()
    .Bind(builder.Configuration.GetSection("Database"))
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<DatabaseConfiguration>>().Value);

// Configure Moxfield API settings with validation
builder.Services.AddOptions<MoxfieldConfiguration>()
    .Bind(builder.Configuration.GetSection("MoxfieldApi"))
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<MoxfieldConfiguration>>().Value);

// Configure JWT settings with validation
builder.Services.AddOptions<JwtConfiguration>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<JwtConfiguration>>().Value);

// Register HttpClient for Moxfield API
builder.Services.AddHttpClient("Moxfield", (sp, client) =>
{
    var config = sp.GetRequiredService<MoxfieldConfiguration>();
    client.BaseAddress = new Uri(config.BaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
});

// Register rate limiter for Moxfield API (1 request per second)
builder.Services.AddSingleton<IRateLimiter>(new RateLimiter(TimeSpan.FromSeconds(1)));

builder.Services.AddScoped<IHandEvaluationService, HandEvaluationService>();
builder.Services.AddScoped<IHandGenerationService, HandGenerationService>();
builder.Services.AddScoped<IDecklistAddingService, DecklistAddingService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IDeckComparisonService, DeckComparisonService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Get database configuration
var dbConfig = builder.Configuration.GetSection("Database").Get<DatabaseConfiguration>() 
    ?? new DatabaseConfiguration();

// Add database context with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("EtaliDatabase");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'EtaliDatabase' is not configured. " +
        "Please set ConnectionStrings:EtaliDatabase in appsettings.json or environment variables.");
}

builder.Services.AddDbContext<EtaliDatabase>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: dbConfig.MaxRetryCount,
            maxRetryDelay: TimeSpan.FromSeconds(dbConfig.MaxRetryDelay),
            errorCodesToAdd: null);
        
        npgsqlOptions.CommandTimeout(dbConfig.CommandTimeout);
        npgsqlOptions.MaxBatchSize(dbConfig.PoolSize);
    });

    if (dbConfig.EnableSensitiveDataLogging)
    {
        options.EnableSensitiveDataLogging();
    }

    if (dbConfig.EnableDetailedErrors)
    {
        options.EnableDetailedErrors();
    }
});

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    
    // User settings - do not require email
    options.User.RequireUniqueEmail = false;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";
    
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<EtaliDatabase>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfiguration>();
if (jwtConfig == null || string.IsNullOrEmpty(jwtConfig.SecretKey))
{
    throw new InvalidOperationException("JWT configuration is missing or invalid. Please configure Jwt:SecretKey in appsettings.json or environment variables.");
}

if (jwtConfig.SecretKey.Length < 32)
{
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long for security.");
}

var key = Encoding.UTF8.GetBytes(jwtConfig.SecretKey);

var isProduction = builder.Environment.IsProduction();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = isProduction; // Require HTTPS in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig.Issuer,
        ValidAudience = jwtConfig.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // Remove default 5-minute tolerance
    };
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ContributorOrAdmin", policy => policy.RequireRole("Contributor", "Admin"));
});

// Add health checks with PostgreSQL connectivity check
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" },
        timeout: TimeSpan.FromSeconds(5));

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Etali Data Collection API",
        Version = "v1",
        Description = @"API for generating and evaluating Magic: The Gathering opening hands with Etali, Primal Conqueror.

This API helps collect data on how quickly Etali can be cast in Commander games by:
1. Generating random opening hands from uploaded decklists
2. Recording user evaluations of those hands
3. Tracking statistics on minimum cards needed to cast Etali

Key Features:
- Generate random 7-card opening hands with stickers
- Evaluate hands for Etali castability
- Import decklists from Moxfield
- Track evaluation statistics
- Secure authentication with JWT tokens

Authentication:
- Most read operations are public
- Data modifications require authentication
- Admin role required to create new users",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Url = new Uri("https://github.com/papajohns37/Etali-Data-Collection-Web-API")
        }
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Configure schema for records
    options.UseAllOfToExtendReferenceSchemas();
    
    // Add examples and annotations
    options.EnableAnnotations();
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// In production, add JSON logging for better log aggregation
if (isProduction)
{
    builder.Logging.AddJsonConsole();
}

var app = builder.Build();

// Run database migrations and seed data
await InitializeDatabaseAsync(app.Services, dbConfig);

// Enable CORS
app.UseCors();

// Use HTTPS redirection in production
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Enable authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Etali API v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    options.DocumentTitle = "Etali Data Collection API";
    options.EnableDeepLinking();
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
});

// Map health check endpoint
app.MapHealthChecks("/health");

// Map API endpoints
app.MapAuthEndpoints();
app.MapHandEndpoints();
app.MapDecklistEndpoints();
app.MapStatisticsEndpoints();
app.MapDeckComparisonEndpoints();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Etali Data Collection API starting in {Environment} mode", app.Environment.EnvironmentName);
logger.LogInformation("CORS Policy: {CorsPolicy}", allowedOrigins == "*" ? "Allow All Origins" : $"Restricted to: {allowedOrigins}");
logger.LogInformation("HTTPS Enforcement: {HttpsEnabled}", app.Environment.IsProduction() ? "Enabled" : "Disabled (Development)");
logger.LogInformation("Database AutoMigrate: {AutoMigrate}", dbConfig.AutoMigrate ? "Enabled" : "Disabled");

app.Run();

// Initialize database with migrations and seed data
static async Task InitializeDatabaseAsync(IServiceProvider services, DatabaseConfiguration dbConfig)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
        
        // Check if we're using InMemory database (for tests)
        var providerName = db.Database.ProviderName;
        var isInMemoryDatabase = providerName == "Microsoft.EntityFrameworkCore.InMemory";
        
        if (isInMemoryDatabase)
        {
            // For InMemory database (tests), just ensure it's created
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("InMemory database created for testing");
        }
        else
        {
            // For real databases (PostgreSQL), check for pending migrations
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            var hasPendingMigrations = pendingMigrations.Any();
            
            if (hasPendingMigrations)
            {
                logger.LogInformation("Found {Count} pending migrations", pendingMigrations.Count());
                
                if (dbConfig.AutoMigrate)
                {
                    logger.LogInformation("Auto-migrating database...");
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Database migration completed successfully");
                }
                else
                {
                    logger.LogWarning(
                        "Database has pending migrations but AutoMigrate is disabled. " +
                        "Run 'dotnet ef database update' to apply migrations manually.");
                }
            }
            else
            {
                logger.LogInformation("Database is up to date");
            }
        }
        
        // Seed roles and admin user
        await SeedDatabaseAsync(services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

// Seed database with initial data
static async Task SeedDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Create roles
    string[] roles = ["Admin", "Contributor"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            logger.LogInformation("Created role: {Role}", role);
        }
    }

    // Create default admin user if configured
    var adminUsername = configuration["DefaultAdmin:Username"];
    var adminPassword = configuration["DefaultAdmin:Password"];

    if (!string.IsNullOrEmpty(adminUsername) && !string.IsNullOrEmpty(adminPassword))
    {
        var existingAdmin = await userManager.FindByNameAsync(adminUsername);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminUsername,
                Email = null,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Default admin user created: {Username}", adminUsername);
            }
            else
            {
                logger.LogError("Failed to create default admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    else
    {
        logger.LogWarning("Default admin credentials not configured. Please set DefaultAdmin:Username and DefaultAdmin:Password in environment variables or configuration.");
    }
}

// Make Program class accessible for testing
public partial class Program { }public partial class Program { }