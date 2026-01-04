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
    private readonly HttpClient _client;

    public HandEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHand_ValidDecklistName_ReturnsOkWithHand()
    {
        // Act
        var response = await _client.GetAsync("/hand?decklistName=TestDeck");

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
    public async Task GetHand_NonExistentDecklistName_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/hand?decklistName=NonExistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHand_MissingDecklistNameParameter_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/hand");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task GetHand_EmptyOrWhitespaceDecklistName_ReturnsBadRequest(string decklistName)
    {
        // Act
        var response = await _client.GetAsync($"/hand?decklistName={Uri.EscapeDataString(decklistName)}");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHand_MultipleSequentialRequests_GeneratesUniqueHandData()
    {
        // Act - Make sequential requests
        var hands = new List<Hand>();
        for (int i = 0; i < 3; i++)
        {
            var response = await _client.GetAsync("/hand?decklistName=TestDeck");
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
    public async Task GetHand_MultipleRequests_ProducesIndependentRandomness()
    {
        // Act - Generate multiple hands
        var hands = new List<Hand>();
        for (int i = 0; i < 10; i++)
        {
            var response = await _client.GetAsync("/hand?decklistName=TestDeck");
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
}
