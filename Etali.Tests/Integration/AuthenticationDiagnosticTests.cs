using System.Net;
using System.Net.Http.Json;
using Etali_Data_Collection_Web_API.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Etali.Tests.Integration;

/// <summary>
/// Diagnostic tests to verify authentication is working properly.
/// </summary>
[Trait("Category", "Integration")]
public class AuthenticationDiagnosticTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public AuthenticationDiagnosticTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Diagnostic_LoginWorks()
    {
        // Arrange - Ensure database is seeded first!
        _output.WriteLine("Starting Diagnostic_LoginWorks test");
        _output.WriteLine("Ensuring database is seeded...");
        
        // Trigger seeding by creating an authenticated client first
        // (we don't need to use it, but this ensures seeding happens)
        try
        {
            await _factory.CreateAuthenticatedClientAsync();
            _output.WriteLine("Database seeding completed via CreateAuthenticatedClientAsync");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Warning during seeding attempt: {ex.Message}");
        }
        
        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        using (var scope = scopeFactory.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByNameAsync(CustomWebApplicationFactory.TestUsername);
            
            if (user == null)
            {
                _output.WriteLine($"ERROR: User {CustomWebApplicationFactory.TestUsername} not found in database!");
            }
            else
            {
                _output.WriteLine($"SUCCESS: User {CustomWebApplicationFactory.TestUsername} exists in database");
                _output.WriteLine($"User ID: {user.Id}");
                _output.WriteLine($"Email Confirmed: {user.EmailConfirmed}");
                
                var roles = await userManager.GetRolesAsync(user);
                _output.WriteLine($"User roles: {string.Join(", ", roles)}");
                
                // Check password
                var passwordCheck = await userManager.CheckPasswordAsync(user, CustomWebApplicationFactory.TestUserPassword);
                _output.WriteLine($"Password check result: {passwordCheck}");
            }
        }

        var loginRequest = new
        {
            username = CustomWebApplicationFactory.TestUsername,
            password = CustomWebApplicationFactory.TestUserPassword
        };

        // Act
        _output.WriteLine($"Attempting login with: {CustomWebApplicationFactory.TestUsername}");
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        
        // Log response
        _output.WriteLine($"Login response status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Login response content: {content}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.Token);
        _output.WriteLine($"Token received: {loginResponse.Token.Substring(0, Math.Min(50, loginResponse.Token.Length))}...");
    }

    [Fact]
    public async Task Diagnostic_CreateAuthenticatedClient()
    {
        // Act
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        
        // Try to access a protected endpoint
        _output.WriteLine("Attempting to access protected endpoint /hand");
        var response = await authenticatedClient.GetAsync("/hand?decklistName=TestDeck");
        
        // Log response
        _output.WriteLine($"Protected endpoint response status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response content preview: {content.Substring(0, Math.Min(200, content.Length))}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostic_TokenInHeaders()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        
        // Check if test auth header is set
        var testAuthHeaders = authenticatedClient.DefaultRequestHeaders.GetValues("X-Test-Auth");
        Assert.NotNull(testAuthHeaders);
        Assert.NotEmpty(testAuthHeaders);
        
        var authHeader = testAuthHeaders.First();
        _output.WriteLine($"X-Test-Auth header: {authHeader}");
        
        // Verify format: "UserId:Username:Roles"
        var parts = authHeader.Split(':');
        Assert.True(parts.Length >= 3, "Auth header should have format UserId:Username:Roles");
        Assert.NotEmpty(parts[0]); // UserId
        Assert.NotEmpty(parts[1]); // Username
        Assert.NotEmpty(parts[2]); // Roles
    }
}
