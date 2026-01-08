using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for POST /hand/evaluate endpoint.
/// </summary>
[Trait("Category", "Integration")]
public class HandEvaluateEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HandEvaluateEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostHandEvaluate_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var uniqueId = Random.Shared.Next(10000, 99999);
        var evaluation = new EvaluatedHand(
            uniqueId,
            CustomWebApplicationFactory.CreateTestCards("Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7"),
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck",
            0.0f,
            null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_WithAuthentication_ValidEvaluation_ReturnsOk()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var uniqueId = Random.Shared.Next(10000, 99999);
        var evaluation = new EvaluatedHand(
            uniqueId,
            CustomWebApplicationFactory.CreateTestCards("Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7"),
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck",
            0.0f,
            null
        );

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Hand evaluation recorded", result.GetProperty("message").GetString());
        Assert.Equal(uniqueId, result.GetProperty("handId").GetInt32());
    }

    [Fact]
    public async Task PostHandEvaluate_WithAuthentication_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var invalidJson = new StringContent("{invalid json}", Encoding.UTF8, "application/json");

        // Act
        var response = await authenticatedClient.PostAsync("/hand/evaluate", invalidJson);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_WithAuthentication_WrongFieldTypes_ReturnsBadRequest()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        // SeatOrder as string instead of int
        var invalidJson = new StringContent(@"{
            ""id"": 1,
            ""cards"": [""Card1"", ""Card2"", ""Card3"", ""Card4"", ""Card5"", ""Card6"", ""Card7""],
            ""seatOrder"": ""two"",
            ""stickers"": [6, 5, 4],
            ""canCastEtali"": true,
            ""minimumCardsToCastEtali"": 5,
            ""decklistId"": 1,
            ""decklistName"": ""TestDeck"",
            ""lowestGambleOdds"": 0.0
        }", Encoding.UTF8, "application/json");

        // Act
        var response = await authenticatedClient.PostAsync("/hand/evaluate", invalidJson);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_WithAuthentication_EmptyStickersArray_AcceptsRequest()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var uniqueId = Random.Shared.Next(10000, 99999);
        var evaluation = new EvaluatedHand(
            uniqueId,
            CustomWebApplicationFactory.CreateTestCards("Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7"),
            1,
            Array.Empty<int>(),
            true,
            5,
            1,
            "TestDeck",
            0.0f,
            null
        );

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_WithAuthentication_CamelCaseJson_DeserializesCorrectly()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var uniqueId = Random.Shared.Next(10000, 99999);
        var jsonContent = new StringContent($@"{{
            ""id"": {uniqueId},
            ""cards"": [
                {{""name"": ""Card1"", ""type"": ""Creature""}},
                {{""name"": ""Card2"", ""type"": ""Instant""}},
                {{""name"": ""Card3"", ""type"": ""Sorcery""}},
                {{""name"": ""Card4"", ""type"": ""Artifact""}},
                {{""name"": ""Card5"", ""type"": ""Enchantment""}},
                {{""name"": ""Card6"", ""type"": ""Land""}},
                {{""name"": ""Card7"", ""type"": ""Planeswalker""}}
            ],
            ""seatOrder"": 2,
            ""stickers"": [6, 5, 4],
            ""canCastEtali"": true,
            ""minimumCardsToCastEtali"": 5,
            ""decklistId"": 1,
            ""decklistName"": ""TestDeck"",
            ""lowestGambleOdds"": 0.5,
            ""createdByUserId"": null
        }}", Encoding.UTF8, "application/json");

        // Act
        var response = await authenticatedClient.PostAsync("/hand/evaluate", jsonContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_WithAuthentication_PascalCaseJson_DeserializesCorrectly()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var uniqueId = Random.Shared.Next(10000, 99999);
        var jsonContent = new StringContent($@"{{
            ""Id"": {uniqueId},
            ""Cards"": [
                {{""Name"": ""Card1"", ""Type"": ""Creature""}},
                {{""Name"": ""Card2"", ""Type"": ""Instant""}},
                {{""Name"": ""Card3"", ""Type"": ""Sorcery""}},
                {{""Name"": ""Card4"", ""Type"": ""Artifact""}},
                {{""Name"": ""Card5"", ""Type"": ""Enchantment""}},
                {{""Name"": ""Card6"", ""Type"": ""Land""}},
                {{""Name"": ""Card7"", ""Type"": ""Planeswalker""}}
            ],
            ""SeatOrder"": 2,
            ""Stickers"": [6, 5, 4],
            ""CanCastEtali"": true,
            ""MinimumCardsToCastEtali"": 5,
            ""DecklistId"": 1,
            ""DecklistName"": ""TestDeck"",
            ""LowestGambleOdds"": 0.5,
            ""CreatedByUserId"": null
        }}", Encoding.UTF8, "application/json");

        // Act
        var response = await authenticatedClient.PostAsync("/hand/evaluate", jsonContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_WithAuthentication_LargePayload_HandlesSuccessfully()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var uniqueId = Random.Shared.Next(10000, 99999);
        var longCardNames = Enumerable.Range(1, 7)
            .Select(i => new string('A', 100) + $" Card {i}")
            .ToArray();

        var evaluation = new EvaluatedHand(
            uniqueId,
            CustomWebApplicationFactory.CreateTestCards(longCardNames),
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck",
            0.0f,
            null
        );

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_WithAuthentication_SuccessResponse_ReturnsConsistentFormat()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var uniqueId = Random.Shared.Next(10000, 99999);
        var evaluation = new EvaluatedHand(
            uniqueId,
            CustomWebApplicationFactory.CreateTestCards("Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7"),
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck",
            0.0f,
            null
        );

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("message", out var message));
        Assert.True(result.TryGetProperty("handId", out var handId));
        Assert.Equal(uniqueId, handId.GetInt32());
    }

    [Fact]
    public async Task PostHandEvaluate_AdminUser_CanAccessEndpoint()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);
        var uniqueId = Random.Shared.Next(10000, 99999);
        var evaluation = new EvaluatedHand(
            uniqueId,
            CustomWebApplicationFactory.CreateTestCards("Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7"),
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck",
            0.0f,
            null
        );

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
