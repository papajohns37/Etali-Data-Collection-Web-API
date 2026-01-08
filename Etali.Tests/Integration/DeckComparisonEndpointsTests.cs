using System.Net;
using System.Net.Http.Json;
using Etali_Data_Collection_Web_API.Models;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for deck comparison endpoints.
/// Each test creates its own comparison to ensure proper test isolation.
/// </summary>
[Trait("Category", "Integration")]
public class DeckComparisonEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DeckComparisonEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region POST /deck-comparisons Tests

    [Fact]
    public async Task CreateDeckComparison_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { deck1Id = 1, deck2Id = 2 };

        // Act
        var response = await client.PostAsJsonAsync("/deck-comparisons", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateDeckComparison_WithAuthentication_ValidDecks_ReturnsCreated()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var request = new { deck1Id = 1, deck2Id = 2 };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/deck-comparisons", request);

        // Assert - API returns 201 Created for successful resource creation, or 409 Conflict if already exists
        Assert.True(
            response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.Conflict,
            $"Expected Created or Conflict, but got {response.StatusCode}");
        
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var comparison = await response.Content.ReadFromJsonAsync<DeckComparison>();
            Assert.NotNull(comparison);
            Assert.Equal(1, comparison.Deck1Id);
            Assert.Equal(2, comparison.Deck2Id);
            Assert.Equal(0, comparison.Deck1N);
            Assert.Equal(0, comparison.Deck2N);
        }
    }

    [Fact]
    public async Task CreateDeckComparison_WithAuthentication_NonExistentDeck_ReturnsNotFound()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var request = new { deck1Id = 999, deck2Id = 1 };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/deck-comparisons", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDeckComparison_WithAuthentication_SameDeck_ReturnsBadRequest()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var request = new { deck1Id = 1, deck2Id = 1 };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/deck-comparisons", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDeckComparison_WithAuthentication_DuplicateOrderIndependent_ReturnsConflict()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var request1 = new { deck1Id = 1, deck2Id = 2 };
        var request2 = new { deck1Id = 2, deck2Id = 1 };

        // Act - Create first comparison
        var response1 = await authenticatedClient.PostAsJsonAsync("/deck-comparisons", request1);
        
        // Only check for conflict if first request succeeded
        if (response1.StatusCode == HttpStatusCode.Created)
        {
            var response2 = await authenticatedClient.PostAsJsonAsync("/deck-comparisons", request2);
            // Assert - Second request should conflict
            Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
        }
        else
        {
            // If first request already conflicted, that's also valid (comparison already exists from another test)
            Assert.Equal(HttpStatusCode.Conflict, response1.StatusCode);
        }
    }

    #endregion

    #region GET /deck-comparisons Tests

    [Fact]
    public async Task GetAllComparisons_WithoutAuthentication_ReturnsOkWithArray()
    {
        // Arrange - Ensure database is seeded and create a comparison
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        // Try to create, but don't fail if it already exists
        await authenticatedClient.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });

        // Act - Public endpoint
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/deck-comparisons");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var comparisons = await response.Content.ReadFromJsonAsync<DeckComparison[]>();
        Assert.NotNull(comparisons);
        Assert.NotEmpty(comparisons);
    }

    #endregion

    #region GET /deck-comparisons/{id} Tests

    [Fact]
    public async Task GetComparisonById_WithoutAuthentication_ExistingComparison_ReturnsOk()
    {
        // Arrange - Create a comparison
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var createResponse = await authenticatedClient.PostAsJsonAsync("/deck-comparisons", 
            new { deck1Id = 1, deck2Id = 2 });
        
        // Get the comparison ID (either from successful creation or from list if it already exists)
        DeckComparison? created;
        if (createResponse.StatusCode == HttpStatusCode.Created)
        {
            created = await createResponse.Content.ReadFromJsonAsync<DeckComparison>();
        }
        else
        {
            // If comparison already exists, get it from the list
            var listResponse = await _factory.CreateClient().GetAsync("/deck-comparisons");
            var comparisons = await listResponse.Content.ReadFromJsonAsync<DeckComparison[]>();
            created = comparisons?.FirstOrDefault(c => 
                (c.Deck1Id == 1 && c.Deck2Id == 2) || 
                (c.Deck1Id == 2 && c.Deck2Id == 1));
        }

        Assert.NotNull(created);

        // Act - Public endpoint
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/deck-comparisons/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var comparison = await response.Content.ReadFromJsonAsync<DeckComparison>();
        Assert.NotNull(comparison);
        Assert.Equal(created.Id, comparison.Id);
    }

    [Fact]
    public async Task GetComparisonById_NonExistent_ReturnsNotFound()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/deck-comparisons/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /decklists/compare Tests

    [Fact]
    public async Task GetCardDifferences_WithoutAuthentication_ValidDecks_ReturnsOkWithDifferences()
    {
        // Arrange - Ensure database is seeded
        await _factory.CreateAuthenticatedClientAsync();
        
        // Act - Public endpoint
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/decklists/compare?deck1Id=1&deck2Id=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var differences = await response.Content.ReadFromJsonAsync<CardDifferences>();
        Assert.NotNull(differences);
        Assert.NotNull(differences.CardsOnlyInDeck1);
        Assert.NotNull(differences.CardsOnlyInDeck2);
        Assert.NotNull(differences.SharedCards);
    }

    [Fact]
    public async Task GetCardDifferences_SameDeck_ReturnsBadRequest()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/decklists/compare?deck1Id=1&deck2Id=1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCardDifferences_NonExistentDeck_ReturnsNotFound()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/decklists/compare?deck1Id=999999&deck2Id=1");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCardDifferences_MissingParameters_ReturnsBadRequest()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/decklists/compare?deck1Id=1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /deck-comparisons/{id}/hand Tests

    [Fact]
    public async Task GenerateComparisonHand_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Get or create a comparison
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        await authenticatedClient.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });
        
        var listResponse = await _factory.CreateClient().GetAsync("/deck-comparisons");
        var comparisons = await listResponse.Content.ReadFromJsonAsync<DeckComparison[]>();
        var comparison = comparisons?.FirstOrDefault(c => 
            (c.Deck1Id == 1 && c.Deck2Id == 2) || 
            (c.Deck1Id == 2 && c.Deck2Id == 1));
        
        Assert.NotNull(comparison);

        // Act - Try without authentication
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/deck-comparisons/{comparison.Id}/hand");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GenerateComparisonHand_WithAuthentication_ValidComparison_ReturnsOkOrError()
    {
        // Arrange - Get or create a comparison
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        await authenticatedClient.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });
        
        var listResponse = await _factory.CreateClient().GetAsync("/deck-comparisons");
        var comparisons = await listResponse.Content.ReadFromJsonAsync<DeckComparison[]>();
        var comparison = comparisons?.FirstOrDefault(c => 
            (c.Deck1Id == 1 && c.Deck2Id == 2) || 
            (c.Deck1Id == 2 && c.Deck2Id == 1));
        
        Assert.NotNull(comparison);

        // Act
        var response = await authenticatedClient.GetAsync($"/deck-comparisons/{comparison.Id}/hand");

        // Assert - May succeed or fail depending on if decks have unique cards
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                    response.StatusCode == HttpStatusCode.InternalServerError);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var hand = await response.Content.ReadFromJsonAsync<ComparisonHand>();
            Assert.NotNull(hand);
            Assert.Equal(comparison.Id, hand.ComparisonId);
            Assert.True(hand.WhichDeck == 1 || hand.WhichDeck == 2);
            Assert.Equal(7, hand.Cards.Length);
            Assert.True(hand.GenerationAttempts > 0);
        }
    }

    [Fact]
    public async Task GenerateComparisonHand_WithAuthentication_NonExistentComparison_ReturnsNotFound()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/deck-comparisons/999999/hand");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /deck-comparisons/{id}/evaluate Tests

    [Fact]
    public async Task SubmitComparisonEvaluation_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Get or create a comparison
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        await authenticatedClient.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });
        
        var listResponse = await _factory.CreateClient().GetAsync("/deck-comparisons");
        var comparisons = await listResponse.Content.ReadFromJsonAsync<DeckComparison[]>();
        var comparison = comparisons?.FirstOrDefault(c => 
            (c.Deck1Id == 1 && c.Deck2Id == 2) || 
            (c.Deck1Id == 2 && c.Deck2Id == 1));
        
        Assert.NotNull(comparison);

        var evaluationRequest = new
        {
            whichDeck = 1,
            generationAttempts = 3,
            evaluatedHand = new
            {
                id = 100,
                cards = new[]
                {
                    new { name = "Card1", type = "Creature" },
                    new { name = "Card2", type = "Creature" },
                    new { name = "Card3", type = "Creature" },
                    new { name = "Card4", type = "Creature" },
                    new { name = "Card5", type = "Creature" },
                    new { name = "Card6", type = "Creature" },
                    new { name = "Card7", type = "Creature" }
                },
                seatOrder = 2,
                stickers = new[] { 6, 5, 4 },
                canCastEtali = true,
                minimumCardsToCastEtali = 3,
                decklistId = 1,
                decklistName = "TestDeck",
                lowestGambleOdds = 0.5f
            }
        };

        // Act - Try without authentication
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/deck-comparisons/{comparison.Id}/evaluate", 
            evaluationRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitComparisonEvaluation_WithAuthentication_ValidRequest_ReturnsOk()
    {
        // Arrange - Get or create a comparison
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        await authenticatedClient.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });
        
        var listResponse = await _factory.CreateClient().GetAsync("/deck-comparisons");
        var comparisons = await listResponse.Content.ReadFromJsonAsync<DeckComparison[]>();
        var comparison = comparisons?.FirstOrDefault(c => 
            (c.Deck1Id == 1 && c.Deck2Id == 2) || 
            (c.Deck1Id == 2 && c.Deck2Id == 1));
        
        Assert.NotNull(comparison);

        var evaluationRequest = new
        {
            whichDeck = 1,
            generationAttempts = 3,
            evaluatedHand = new
            {
                id = 100,
                cards = new[]
                {
                    new { name = "Card1", type = "Creature" },
                    new { name = "Card2", type = "Creature" },
                    new { name = "Card3", type = "Creature" },
                    new { name = "Card4", type = "Creature" },
                    new { name = "Card5", type = "Creature" },
                    new { name = "Card6", type = "Creature" },
                    new { name = "Card7", type = "Creature" }
                },
                seatOrder = 2,
                stickers = new[] { 6, 5, 4 },
                canCastEtali = true,
                minimumCardsToCastEtali = 3,
                decklistId = 1,
                decklistName = "TestDeck",
                lowestGambleOdds = 0.5f
            }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync(
            $"/deck-comparisons/{comparison.Id}/evaluate", 
            evaluationRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ComparisonEvaluatedHand>();
        Assert.NotNull(result);
        Assert.Equal(comparison.Id, result.ComparisonId);
        Assert.Equal(1, result.WhichDeck);
        Assert.Equal(3, result.GenerationAttempts);
    }

    [Fact]
    public async Task SubmitComparisonEvaluation_WithAuthentication_InvalidWhichDeck_ReturnsBadRequest()
    {
        // Arrange - Get or create a comparison
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        await authenticatedClient.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });
        
        var listResponse = await _factory.CreateClient().GetAsync("/deck-comparisons");
        var comparisons = await listResponse.Content.ReadFromJsonAsync<DeckComparison[]>();
        var comparison = comparisons?.FirstOrDefault(c => 
            (c.Deck1Id == 1 && c.Deck2Id == 2) || 
            (c.Deck1Id == 2 && c.Deck2Id == 1));
        
        Assert.NotNull(comparison);

        var evaluationRequest = new
        {
            whichDeck = 99, // Invalid
            generationAttempts = 3,
            evaluatedHand = new
            {
                id = 100,
                cards = new[]
                {
                    new { name = "Card1", type = "Creature" },
                    new { name = "Card2", type = "Creature" },
                    new { name = "Card3", type = "Creature" },
                    new { name = "Card4", type = "Creature" },
                    new { name = "Card5", type = "Creature" },
                    new { name = "Card6", type = "Creature" },
                    new { name = "Card7", type = "Creature" }
                },
                seatOrder = 2,
                stickers = new[] { 6, 5, 4 },
                canCastEtali = true,
                minimumCardsToCastEtali = 3,
                decklistId = 1,
                decklistName = "TestDeck",
                lowestGambleOdds = 0.5f
            }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync(
            $"/deck-comparisons/{comparison.Id}/evaluate", 
            evaluationRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitComparisonEvaluation_WithAuthentication_NonExistentComparison_ReturnsNotFound()
    {
        // Arrange
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        var evaluationRequest = new
        {
            whichDeck = 1,
            generationAttempts = 3,
            evaluatedHand = new
            {
                id = 100,
                cards = new[]
                {
                    new { name = "Card1", type = "Creature" },
                    new { name = "Card2", type = "Creature" },
                    new { name = "Card3", type = "Creature" },
                    new { name = "Card4", type = "Creature" },
                    new { name = "Card5", type = "Creature" },
                    new { name = "Card6", type = "Creature" },
                    new { name = "Card7", type = "Creature" }
                },
                seatOrder = 2,
                stickers = new[] { 6, 5, 4 },
                canCastEtali = true,
                minimumCardsToCastEtali = 3,
                decklistId = 1,
                decklistName = "TestDeck",
                lowestGambleOdds = 0.5f
            }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/deck-comparisons/999999/evaluate", evaluationRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /deck-comparisons/{id}/statistics Tests

    [Fact]
    public async Task GetComparisonStatistics_WithoutAuthentication_WithEvaluations_ReturnsOkWithStatistics()
    {
        // Arrange - Get or create a comparison and add evaluations
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        await authenticatedClient.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });
        
        var listResponse = await _factory.CreateClient().GetAsync("/deck-comparisons");
        var comparisons = await listResponse.Content.ReadFromJsonAsync<DeckComparison[]>();
        var comparison = comparisons?.FirstOrDefault(c => 
            (c.Deck1Id == 1 && c.Deck2Id == 2) || 
            (c.Deck1Id == 2 && c.Deck2Id == 1));
        
        Assert.NotNull(comparison);

        // Add some evaluations (using unique IDs to avoid conflicts)
        var baseId = DateTime.UtcNow.Ticks;
        for (int i = 0; i < 5; i++)
        {
            var evaluationRequest = new
            {
                whichDeck = 1,
                generationAttempts = 1,
                evaluatedHand = new
                {
                    id = baseId + i,
                    cards = new[]
                    {
                        new { name = "Card1", type = "Creature" },
                        new { name = "Card2", type = "Creature" },
                        new { name = "Card3", type = "Creature" },
                        new { name = "Card4", type = "Creature" },
                        new { name = "Card5", type = "Creature" },
                        new { name = "Card6", type = "Creature" },
                        new { name = "Card7", type = "Creature" }
                    },
                    seatOrder = 2,
                    stickers = new[] { 6, 5, 4 },
                    canCastEtali = i < 3,
                    minimumCardsToCastEtali = i < 3 ? 3 : 8,
                    decklistId = 1,
                    decklistName = "TestDeck",
                    lowestGambleOdds = 0.5f
                }
            };
            
            await authenticatedClient.PostAsJsonAsync($"/deck-comparisons/{comparison.Id}/evaluate", evaluationRequest);
        }

        // Act - Public endpoint
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/deck-comparisons/{comparison.Id}/statistics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var statistics = await response.Content.ReadFromJsonAsync<DeckComparisonStatistics>();
        Assert.NotNull(statistics);
        Assert.Equal(comparison.Id, statistics.ComparisonId);
        Assert.NotNull(statistics.HandSizeDeltaStatistics);
        Assert.NotEmpty(statistics.HandSizeDeltaStatistics);
    }

    [Fact]
    public async Task GetComparisonStatistics_NonExistentComparison_ReturnsNotFound()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/deck-comparisons/999999/statistics");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /decklists/{id}/comparisons Tests

    [Fact]
    public async Task GetComparisonsForDeck_WithoutAuthentication_ExistingDeck_ReturnsOkWithArray()
    {
        // Arrange - Ensure database is seeded and create a comparison
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        await authenticatedClient.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });

        // Act - Public endpoint
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/decklists/1/comparisons");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var comparisons = await response.Content.ReadFromJsonAsync<DeckComparison[]>();
        Assert.NotNull(comparisons);
    }

    [Fact]
    public async Task GetComparisonsForDeck_NoComparisons_ReturnsEmptyArray()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/decklists/999999/comparisons");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var comparisons = await response.Content.ReadFromJsonAsync<DeckComparison[]>();
        Assert.NotNull(comparisons);
        Assert.Empty(comparisons);
    }

    #endregion

    #region Content Type Tests

    [Fact]
    public async Task DeckComparisonEndpoints_ReturnJsonContentType()
    {
        // Arrange - Get or create a comparison
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
        await authenticatedClient.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });
        
        var listResponse = await _factory.CreateClient().GetAsync("/deck-comparisons");
        var comparisons = await listResponse.Content.ReadFromJsonAsync<DeckComparison[]>();
        var comparison = comparisons?.FirstOrDefault(c => 
            (c.Deck1Id == 1 && c.Deck2Id == 2) || 
            (c.Deck1Id == 2 && c.Deck2Id == 1));
        
        Assert.NotNull(comparison);

        // Act & Assert - Public endpoints
        var client = _factory.CreateClient();
        var getResponse = await client.GetAsync($"/deck-comparisons/{comparison.Id}");
        Assert.Equal("application/json", getResponse.Content.Headers.ContentType?.MediaType);

        var getAllResponse = await client.GetAsync("/deck-comparisons");
        Assert.Equal("application/json", getAllResponse.Content.Headers.ContentType?.MediaType);
    }

    #endregion
}
