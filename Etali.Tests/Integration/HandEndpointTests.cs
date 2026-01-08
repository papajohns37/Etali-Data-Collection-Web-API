using System.Net;
using System.Net.Http.Json;
using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for GET /hand endpoint.
/// </summary>
[Trait("Category", "Integration")]
public class HandEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HandEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHand_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHand_WithAuthentication_ValidDecklistName_ReturnsOkWithHand()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var hand = await response.Content.ReadFromJsonAsync<Hand>();
        Assert.NotNull(hand);
        Assert.Equal(7, hand.Cards.Length);
        Assert.InRange(hand.SeatOrder, 1, 4);
        Assert.Equal(3, hand.Stickers.Length);
        Assert.Equal(1, hand.DecklistId);
        Assert.Equal("TestDeck", hand.DecklistName);
    }

    [Fact]
    public async Task GetHand_WithAuthentication_NonExistentDecklistName_ReturnsNotFound()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/hand?decklistName=NonExistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHand_WithAuthentication_MissingDecklistNameParameter_ReturnsBadRequest()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/hand");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task GetHand_WithAuthentication_EmptyOrWhitespaceDecklistName_ReturnsBadRequest(string decklistName)
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync($"/hand?decklistName={Uri.EscapeDataString(decklistName)}");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHand_WithAuthentication_MultipleSequentialRequests_GeneratesUniqueHandData()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act - Make sequential requests
        var hands = new List<Hand>();
        for (int i = 0; i < 3; i++)
        {
            var response = await authenticatedClient.GetAsync("/hand?decklistName=TestDeck");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hand = await response.Content.ReadFromJsonAsync<Hand>();
            Assert.NotNull(hand);
            hands.Add(hand);

            await Task.Delay(50);
        }

        // Assert - At least two hands should have different card selections or seat orders
        var firstHandCardNames = string.Join(",", hands[0].Cards.Select(c => c.Name));
        var allIdentical = hands.All(h =>
            string.Join(",", h.Cards.Select(c => c.Name)) == firstHandCardNames &&
            h.SeatOrder == hands[0].SeatOrder);

        Assert.False(allIdentical, "Multiple hands should produce varied results");
    }

    [Fact]
    public async Task GetHand_WithAuthentication_MultipleRequests_ProducesIndependentRandomness()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act - Generate multiple hands
        var hands = new List<Hand>();
        for (int i = 0; i < 10; i++)
        {
            var response = await authenticatedClient.GetAsync("/hand?decklistName=TestDeck");
            var hand = await response.Content.ReadFromJsonAsync<Hand>();
            Assert.NotNull(hand);
            hands.Add(hand);
        }

        // Assert - Should have variety in cards (not all hands identical)
        var firstHandCards = string.Join(",", hands[0].Cards.Select(c => c.Name));
        var allIdentical = hands.All(h => string.Join(",", h.Cards.Select(c => c.Name)) == firstHandCards);
        Assert.False(allIdentical, "All hands should not have identical card selections");

        // Assert - Should have variety in seat orders
        var uniqueSeatOrders = hands.Select(h => h.SeatOrder).Distinct().Count();
        Assert.True(uniqueSeatOrders > 1, "Should have variety in seat orders");
    }

    [Fact]
    public async Task GetHand_AdminUser_CanAccessEndpoint()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);

        // Act
        var response = await authenticatedClient.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hand = await response.Content.ReadFromJsonAsync<Hand>();
        Assert.NotNull(hand);
    }
}
