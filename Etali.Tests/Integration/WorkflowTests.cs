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
    private readonly HttpClient _client;

    public WorkflowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteWorkflow_GetHandEvaluateAndRetrieve_WorksCorrectly()
    {
        // This test verifies that the API can handle a complete workflow

        // 1. Get a hand
        var handResponse = await _client.GetAsync("/hand?decklistName=TestDeck");
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
            hand.LowestGambleOdds
        );
        var evalResponse = await _client.PostAsJsonAsync("/hand/evaluate", evaluation);
        Assert.Equal(HttpStatusCode.OK, evalResponse.StatusCode);

        // 3. Get decklists
        var decklistsResponse = await _client.GetAsync("/decklists");
        Assert.Equal(HttpStatusCode.OK, decklistsResponse.StatusCode);
    }

    [Fact]
    public async Task GetHand_ErrorResponse_ReturnsConsistentFormat()
    {
        // Act
        var response = await _client.GetAsync("/hand?decklistName=NonExistent123456");

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
}
