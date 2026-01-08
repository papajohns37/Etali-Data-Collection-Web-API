using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Etali.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that properly configures the test environment.
/// Each factory instance uses a unique in-memory database to ensure test isolation.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Test user credentials
    public const string TestUsername = "testuser";
    public const string TestUserPassword = "TestPass123!";
    public const string TestAdminUsername = "testadmin";
    public const string TestAdminPassword = "AdminPass123!";

    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";
    private bool _seeded = false;
    private readonly SemaphoreSlim _seedLock = new SemaphoreSlim(1, 1);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration for JWT and Database
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Jwt:SecretKey"] = "TestSecretKeyForJwtTokenGenerationThatIsAtLeast64CharactersLong12345",
                ["Jwt:Issuer"] = "EtaliDataCollectionApi",
                ["Jwt:Audience"] = "EtaliDataCollectionApi",
                ["Jwt:ExpirationInDays"] = "7",
                ["DefaultAdmin:Username"] = TestAdminUsername,
                ["DefaultAdmin:Password"] = TestAdminPassword,
                ["ConnectionStrings:EtaliDatabase"] = "Host=localhost;Database=test_db;Username=test;Password=test",
                ["Database:AutoMigrate"] = "false"
            }!);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the PostgreSQL DbContext registration
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<EtaliDatabase>) ||
                d.ServiceType == typeof(DbContextOptions)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Also remove the EtaliDatabase itself
            services.RemoveAll<EtaliDatabase>();

            // Add InMemory database context with UNIQUE name per factory instance for test isolation
            services.AddDbContext<EtaliDatabase>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Remove existing authentication services and replace with test authentication
            services.RemoveAll<IAuthenticationService>();
            
            // Configure test authentication to replace JWT
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, 
                options => { });
        });
    }

    /// <summary>
    /// Helper method to create test Card arrays for tests that need to create EvaluatedHand objects.
    /// </summary>
    public static Card[] CreateTestCards(params string[] names)
    {
        return names.Select(name => new Card { Name = name, Type = "Creature" }).ToArray();
    }

    /// <summary>
    /// Helper method to get an authenticated HttpClient for tests.
    /// Ensures database is seeded before attempting authentication.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(bool useAdmin = false)
    {
        // Create a client first to ensure the factory is fully initialized
        var client = CreateClient();
        
        // Now seed the database using the initialized factory
        await EnsureDatabaseSeededAsync();
        
        // Get the user from the database to get the actual user ID
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var username = useAdmin ? TestAdminUsername : TestUsername;
        var user = await userManager.FindByNameAsync(username);
        
        if (user == null)
        {
            throw new InvalidOperationException($"Test user {username} not found in database. Seeding may have failed.");
        }
        
        var roles = await userManager.GetRolesAsync(user);
        var rolesString = string.Join(",", roles);
        
        // Set test auth header: "UserId:Username:Roles"
        client.DefaultRequestHeaders.Add("X-Test-Auth", $"{user.Id}:{username}:{rolesString}");
        
        return client;
    }

    private async Task EnsureDatabaseSeededAsync()
    {
        // Use semaphore for thread-safe seeding
        await _seedLock.WaitAsync();
        try
        {
            if (_seeded)
            {
                return; // Already seeded
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created FIRST
            await db.Database.EnsureCreatedAsync();

            // Check if users already exist (double-check after getting lock)
            var existingUser = await userManager.FindByNameAsync(TestUsername);
            if (existingUser != null)
            {
                _seeded = true;
                return; // Already seeded
            }

            // Seed roles
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await roleManager.RoleExistsAsync("Contributor"))
            {
                await roleManager.CreateAsync(new IdentityRole("Contributor"));
            }

            // Seed test users
            var testUser = new ApplicationUser
            {
                UserName = TestUsername,
                Email = null,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };
            var userResult = await userManager.CreateAsync(testUser, TestUserPassword);
            if (userResult.Succeeded)
            {
                await userManager.AddToRoleAsync(testUser, "Contributor");
            }
            else
            {
                throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");
            }

            // Seed admin user - but don't throw if it already exists from Program.cs seeding
            var existingAdmin = await userManager.FindByNameAsync(TestAdminUsername);
            if (existingAdmin == null)
            {
                var testAdmin = new ApplicationUser
                {
                    UserName = TestAdminUsername,
                    Email = null,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow
                };
                var adminResult = await userManager.CreateAsync(testAdmin, TestAdminPassword);
                if (adminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(testAdmin, "Admin");
                }
                // Silently continue if admin creation fails (might be from Program.cs seed)
            }

            // Seed decklists
            db.Decklists.Add(new Decklist
            {
                Id = 1,
                Name = "TestDeck",
                Cards = new[]
                {
                    new Card { Name = "Sol Ring", Type = "Artifact" },
                    new Card { Name = "Mana Vault", Type = "Artifact" },
                    new Card { Name = "Card3", Type = "Sorcery" },
                    new Card { Name = "Card4", Type = "Artifact" },
                    new Card { Name = "Card5", Type = "Enchantment" },
                    new Card { Name = "Mountain", Type = "Land" },
                    new Card { Name = "Command Tower", Type = "Land" },
                    new Card { Name = "Card8", Type = "Creature" },
                    new Card { Name = "Card9", Type = "Instant" },
                    new Card { Name = "Card10", Type = "Land" }
                },
                MoxfieldUrl = "https://www.moxfield.com/decks/test123",
                MoxfieldId = "test123",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = testUser?.Id
            });
            
            db.Decklists.Add(new Decklist
            {
                Id = 2,
                Name = "TestDeck2",
                Cards = new[]
                {
                    new Card { Name = "Worn Powerstone", Type = "Artifact" },
                    new Card { Name = "Thran Dynamo", Type = "Artifact" },
                    new Card { Name = "Card3", Type = "Sorcery" },
                    new Card { Name = "Card4", Type = "Artifact" },
                    new Card { Name = "Card5", Type = "Enchantment" },
                    new Card { Name = "Mountain", Type = "Land" },
                    new Card { Name = "Command Tower", Type = "Land" },
                    new Card { Name = "Card11", Type = "Creature" },
                    new Card { Name = "Card12", Type = "Instant" },
                    new Card { Name = "Card13", Type = "Land" }
                },
                MoxfieldUrl = "https://www.moxfield.com/decks/test456",
                MoxfieldId = "test456",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = testUser?.Id
            });
            
            await db.SaveChangesAsync();

            _seeded = true;
        }
        finally
        {
            _seedLock.Release();
        }
    }

    /// <summary>
    /// Helper method to get a JWT token for tests.
    /// </summary>
    public async Task<string> GetAuthTokenAsync(HttpClient client, bool useAdmin = false)
    {
        var loginRequest = new
        {
            username = useAdmin ? TestAdminUsername : TestUsername,
            password = useAdmin ? TestAdminPassword : TestUserPassword
        };

        var response = await client.PostAsJsonAsync("/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResponse?.Token ?? throw new InvalidOperationException("Failed to get auth token");
    }
}
