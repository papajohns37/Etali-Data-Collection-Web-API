using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for complete API workflows combining multiple endpoints.
/// </summary>
[Trait("Category", "Integration")]
public class WorkflowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WorkflowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteWorkflow_GetHandEvaluateAndRetrieve_WorksCorrectly()
    {
        // This test verifies that the API can handle a complete workflow with authentication

        // Arrange - Get authenticated client
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // 1. Get a hand
        var handResponse = await authenticatedClient.GetAsync("/hand?decklistName=TestDeck");
        Assert.Equal(HttpStatusCode.OK, handResponse.StatusCode);
        var hand = await handResponse.Content.ReadFromJsonAsync<Hand>();
        Assert.NotNull(hand);

        // 2. Evaluate the hand
        var evaluation = new EvaluatedHand(
            hand.Id,
            hand.Cards,
            hand.SeatOrder,
            hand.Stickers,
            true,
            5,
            hand.DecklistId,
            hand.DecklistName,
            hand.LowestGambleOdds,
            null
        );
        var evalResponse = await authenticatedClient.PostAsJsonAsync("/hand/evaluate", evaluation);
        Assert.Equal(HttpStatusCode.OK, evalResponse.StatusCode);

        // 3. Get decklists (public endpoint - no auth needed)
        var decklistsResponse = await _client.GetAsync("/decklists");
        Assert.Equal(HttpStatusCode.OK, decklistsResponse.StatusCode);
    }

    [Fact]
    public async Task GetHand_WithoutAuthentication_ErrorResponse_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/hand?decklistName=TestDeck");

        // Assert - Should return Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHand_WithAuthentication_ErrorResponse_ReturnsConsistentFormat()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/hand?decklistName=NonExistent123456");

        // Assert - Should return NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);

        // Should contain error information in ProblemDetails format
        var jsonDoc = JsonDocument.Parse(content);
        // ProblemDetails has properties like "title", "detail", "status"
        Assert.True(
            jsonDoc.RootElement.TryGetProperty("error", out _) || 
            jsonDoc.RootElement.TryGetProperty("title", out _) ||
            jsonDoc.RootElement.TryGetProperty("detail", out _),
            "Response should contain error information (either 'error', 'title', or 'detail' property)");
    }

    [Fact]
    public async Task CompleteWorkflow_AdminUser_CanPerformAllActions()
    {
        // Arrange - Get authenticated admin client
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);

        // 1. Get a hand
        var handResponse = await authenticatedClient.GetAsync("/hand?decklistName=TestDeck");
        Assert.Equal(HttpStatusCode.OK, handResponse.StatusCode);
        var hand = await handResponse.Content.ReadFromJsonAsync<Hand>();
        Assert.NotNull(hand);

        // 2. Evaluate the hand
        var evaluation = new EvaluatedHand(
            hand.Id,
            hand.Cards,
            hand.SeatOrder,
            hand.Stickers,
            true,
            5,
            hand.DecklistId,
            hand.DecklistName,
            hand.LowestGambleOdds,
            null
        );
        var evalResponse = await authenticatedClient.PostAsJsonAsync("/hand/evaluate", evaluation);
        Assert.Equal(HttpStatusCode.OK, evalResponse.StatusCode);

        // 3. Admin can also register new users
        var registerRequest = new
        {
            username = $"newuser{Guid.NewGuid():N}",
            password = "NewUser123!",
            confirmPassword = "NewUser123!",
            isAdmin = false
        };
        var registerResponse = await authenticatedClient.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
    }
}
