using System.Reflection;
using System.Text;
using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Endpoints;
using Etali_Data_Collection_Web_API.Models;
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
                  .AllowAnyHeader();
        }
    });
});

// Register application services
builder.Services.AddSingleton<GameConfiguration>();

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

// Add database context
builder.Services.AddDbContext<EtaliDatabase>(options =>
    options.UseInMemoryDatabase("EtaliDatabase"));

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false; // Simplified for now
})
.AddEntityFrameworkStores<EtaliDatabase>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfiguration>();
if (jwtConfig == null || string.IsNullOrEmpty(jwtConfig.SecretKey))
{
    throw new InvalidOperationException("JWT configuration is missing or invalid. Please configure Jwt:SecretKey in appsettings.json or user secrets.");
}

var key = Encoding.UTF8.GetBytes(jwtConfig.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production with HTTPS
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

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("database", () =>
    {
        // Simple health check that always returns healthy
        // In production, this would check actual database connectivity
        return HealthCheckResult.Healthy("Database is accessible");
    });

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

var app = builder.Build();

// Seed database with roles and admin user
await SeedDatabaseAsync(app.Services);

// Enable CORS
app.UseCors();

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

app.Run();

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
    var adminEmail = configuration["DefaultAdmin:Email"];
    var adminPassword = configuration["DefaultAdmin:Password"];

    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
    {
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Default admin user created: {Email}", adminEmail);
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
        logger.LogWarning("Default admin credentials not configured. Please set DefaultAdmin:Email and DefaultAdmin:Password in user secrets or configuration.");
    }
}

// Make Program class accessible for testing
public partial class Program { }