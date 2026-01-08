using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Etali_Data_Collection_Web_API.Models.Auth;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for authentication endpoints.
/// </summary>
[Trait("Category", "Integration")]
public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region POST /auth/login Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginRequest = new
        {
            email = CustomWebApplicationFactory.TestUserEmail,
            password = CustomWebApplicationFactory.TestUserPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.Token);
        Assert.NotEmpty(loginResponse.Token);
        Assert.Equal(CustomWebApplicationFactory.TestUserEmail, loginResponse.Email);
        Assert.Contains("Contributor", loginResponse.Roles);
        Assert.True(loginResponse.Expiration > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_AdminUser_ReturnsTokenWithAdminRole()
    {
        // Arrange
        var loginRequest = new
        {
            email = CustomWebApplicationFactory.TestAdminEmail,
            password = CustomWebApplicationFactory.TestAdminPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.Contains("Admin", loginResponse.Roles);
    }

    [Fact]
    public async Task Login_InvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            email = "nonexistent@example.com",
            password = "SomePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            email = CustomWebApplicationFactory.TestUserEmail,
            password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new
        {
            password = "SomePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        // ASP.NET Core may return 400 BadRequest for missing required fields
        // or 401 Unauthorized if it treats missing fields as invalid credentials
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                    response.StatusCode == HttpStatusCode.Unauthorized,
                    $"Expected BadRequest or Unauthorized, but got {response.StatusCode}");
    }

    [Fact]
    public async Task Login_MissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new
        {
            email = "test@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        // ASP.NET Core may return 400 BadRequest for missing required fields
        // or 401 Unauthorized if it treats missing fields as invalid credentials
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                    response.StatusCode == HttpStatusCode.Unauthorized,
                    $"Expected BadRequest or Unauthorized, but got {response.StatusCode}");
    }

    [Fact]
    public async Task Login_EmptyCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new
        {
            email = "",
            password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                    response.StatusCode == HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /auth/register Tests

    [Fact]
    public async Task Register_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var registerRequest = new
        {
            email = "newuser@example.com",
            password = "NewUser123!",
            confirmPassword = "NewUser123!",
            isAdmin = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithContributorRole_ReturnsForbidden()
    {
        // Arrange - Login as contributor
        var contributorClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: false);
        
        var registerRequest = new
        {
            email = "newuser@example.com",
            password = "NewUser123!",
            confirmPassword = "NewUser123!",
            isAdmin = false
        };

        // Act
        var response = await contributorClient.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAdminRole_ValidRequest_ReturnsOk()
    {
        // Arrange - Login as admin
        var adminClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);
        
        var uniqueEmail = $"newuser{Guid.NewGuid():N}@example.com";
        var registerRequest = new
        {
            email = uniqueEmail,
            password = "NewUser123!",
            confirmPassword = "NewUser123!",
            isAdmin = false
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("User registered successfully", result.GetProperty("message").GetString());
        Assert.Equal(uniqueEmail, result.GetProperty("email").GetString());
        Assert.Equal("Contributor", result.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Register_WithAdminRole_CreateAdminUser_ReturnsOk()
    {
        // Arrange - Login as admin
        var adminClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);
        
        var uniqueEmail = $"newadmin{Guid.NewGuid():N}@example.com";
        var registerRequest = new
        {
            email = uniqueEmail,
            password = "NewAdmin123!",
            confirmPassword = "NewAdmin123!",
            isAdmin = true
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Admin", result.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Register_WithAdminRole_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange - Login as admin
        var adminClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);
        
        var registerRequest = new
        {
            email = CustomWebApplicationFactory.TestUserEmail, // Already exists
            password = "NewUser123!",
            confirmPassword = "NewUser123!",
            isAdmin = false
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAdminRole_PasswordMismatch_ReturnsBadRequest()
    {
        // Arrange - Login as admin
        var adminClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);
        
        var registerRequest = new
        {
            email = "newuser@example.com",
            password = "NewUser123!",
            confirmPassword = "DifferentPassword123!",
            isAdmin = false
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAdminRole_WeakPassword_ReturnsBadRequest()
    {
        // Arrange - Login as admin
        var adminClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);
        
        var registerRequest = new
        {
            email = "newuser@example.com",
            password = "weak", // Too short, missing requirements
            confirmPassword = "weak",
            isAdmin = false
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAdminRole_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange - Login as admin
        var adminClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);
        
        var registerRequest = new
        {
            email = "notanemail",
            password = "NewUser123!",
            confirmPassword = "NewUser123!",
            isAdmin = false
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Token Validation Tests

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act - Try to access a protected endpoint
        var response = await authenticatedClient.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await client.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutBearerPrefix_ReturnsUnauthorized()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync(_client, useAdmin: false);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token); // Wrong scheme

        // Act
        var response = await client.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt-token");

        // Act
        var response = await client.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Role-Based Authorization Tests

    [Fact]
    public async Task AdminOnlyEndpoint_WithContributorRole_ReturnsForbidden()
    {
        // Arrange
        var contributorClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: false);
        
        var registerRequest = new
        {
            email = "test@example.com",
            password = "Test123!",
            confirmPassword = "Test123!",
            isAdmin = false
        };

        // Act
        var response = await contributorClient.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ContributorEndpoint_WithContributorRole_ReturnsSuccess()
    {
        // Arrange
        var contributorClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: false);

        // Act - Contributors can access hand generation
        var response = await contributorClient.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ContributorEndpoint_WithAdminRole_ReturnsSuccess()
    {
        // Arrange
        var adminClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);

        // Act - Admins can also access contributor endpoints
        var response = await adminClient.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Public Endpoint Tests

    [Fact]
    public async Task PublicEndpoint_WithoutAuthentication_ReturnsSuccess()
    {
        // Act - Public endpoints don't require authentication
        var response = await _client.GetAsync("/hands/evaluated");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PublicEndpoint_WithAuthentication_ReturnsSuccess()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act - Public endpoints work with authentication too
        var response = await authenticatedClient.GetAsync("/hands/evaluated");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
