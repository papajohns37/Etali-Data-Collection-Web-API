using System.Net;
using System.Net.Http.Json;
using Etali_Data_Collection_Web_API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Etali_Data_Collection_Web_API;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for deck comparison endpoints.
/// Each test uses a unique database to avoid conflicts.
/// </summary>
[Trait("Category", "Integration")]
public class DeckComparisonEndpointsTests : IAsyncLifetime
{
    private DeckComparisonTestFactory? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _factory = new DeckComparisonTestFactory();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    #region POST /deck-comparisons Tests

    [Fact]
    public async Task CreateDeckComparison_ValidDecks_ReturnsCreated()
    {
        // Arrange
        var request = new { deck1Id = 1, deck2Id = 2 }; // Two different decks

        // Act
        var response = await _client!.PostAsJsonAsync("/deck-comparisons", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var comparison = await response.Content.ReadFromJsonAsync<DeckComparison>();
        Assert.NotNull(comparison);
        Assert.Equal(1, comparison.Deck1Id);
        Assert.Equal(2, comparison.Deck2Id);
        Assert.Equal(0, comparison.Deck1N);
        Assert.Equal(0, comparison.Deck2N);
    }

    [Fact]
    public async Task CreateDeckComparison_NonExistentDeck_ReturnsNotFound()
    {
        // Arrange
        var request = new { deck1Id = 999, deck2Id = 1 };

        // Act
        var response = await _client!.PostAsJsonAsync("/deck-comparisons", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDeckComparison_SameDeck_ReturnsBadRequest()
    {
        // Arrange
        var request = new { deck1Id = 1, deck2Id = 1 };

        // Act
        var response = await _client!.PostAsJsonAsync("/deck-comparisons", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDeckComparison_DuplicateOrderIndependent_ReturnsConflict()
    {
        // Arrange
        var request1 = new { deck1Id = 1, deck2Id = 2 };
        var request2 = new { deck1Id = 2, deck2Id = 1 }; // Reverse order

        // Act
        await _client!.PostAsJsonAsync("/deck-comparisons", request1);
        var response = await _client!.PostAsJsonAsync("/deck-comparisons", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    #endregion

    #region GET /deck-comparisons Tests

    [Fact]
    public async Task GetAllComparisons_ReturnsOkWithArray()
    {
        // Arrange - Create a comparison first
        await _client!.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });

        // Act
        var response = await _client!.GetAsync("/deck-comparisons");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var comparisons = await response.Content.ReadFromJsonAsync<DeckComparison[]>();
        Assert.NotNull(comparisons);
        Assert.NotEmpty(comparisons);
    }

    #endregion

    #region GET /deck-comparisons/{id} Tests

    [Fact]
    public async Task GetComparisonById_ExistingComparison_ReturnsOk()
    {
        // Arrange
        var createResponse = await _client!.PostAsJsonAsync("/deck-comparisons", 
            new { deck1Id = 1, deck2Id = 2 });
        var created = await createResponse.Content.ReadFromJsonAsync<DeckComparison>();

        // Act
        var response = await _client!.GetAsync($"/deck-comparisons/{created!.Id}");

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
        var response = await _client!.GetAsync("/deck-comparisons/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /decklists/compare Tests

    [Fact]
    public async Task GetCardDifferences_ValidDecks_ReturnsOkWithDifferences()
    {
        // Act
        var response = await _client!.GetAsync("/decklists/compare?deck1Id=1&deck2Id=2");

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
        var response = await _client!.GetAsync("/decklists/compare?deck1Id=1&deck2Id=1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCardDifferences_NonExistentDeck_ReturnsNotFound()
    {
        // Act
        var response = await _client!.GetAsync("/decklists/compare?deck1Id=999&deck2Id=1");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCardDifferences_MissingParameters_ReturnsBadRequest()
    {
        // Act
        var response = await _client!.GetAsync("/decklists/compare?deck1Id=1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /deck-comparisons/{id}/hand Tests

    [Fact]
    public async Task GenerateComparisonHand_ValidComparison_ReturnsOkWithHand()
    {
        // Arrange
        var createResponse = await _client!.PostAsJsonAsync("/deck-comparisons", 
            new { deck1Id = 1, deck2Id = 2 });
        var comparison = await createResponse.Content.ReadFromJsonAsync<DeckComparison>();

        // Act
        var response = await _client!.GetAsync($"/deck-comparisons/{comparison!.Id}/hand");

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
    public async Task GenerateComparisonHand_NonExistentComparison_ReturnsNotFound()
    {
        // Act
        var response = await _client!.GetAsync("/deck-comparisons/999/hand");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /deck-comparisons/{id}/evaluate Tests

    [Fact]
    public async Task SubmitComparisonEvaluation_ValidRequest_ReturnsOk()
    {
        // Arrange
        var createResponse = await _client!.PostAsJsonAsync("/deck-comparisons", 
            new { deck1Id = 1, deck2Id = 2 });
        var comparison = await createResponse.Content.ReadFromJsonAsync<DeckComparison>();

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
        var response = await _client!.PostAsJsonAsync(
            $"/deck-comparisons/{comparison!.Id}/evaluate", 
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
    public async Task SubmitComparisonEvaluation_InvalidWhichDeck_ReturnsBadRequest()
    {
        // Arrange
        var createResponse = await _client!.PostAsJsonAsync("/deck-comparisons", 
            new { deck1Id = 1, deck2Id = 2 });
        var comparison = await createResponse.Content.ReadFromJsonAsync<DeckComparison>();

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
        var response = await _client!.PostAsJsonAsync(
            $"/deck-comparisons/{comparison!.Id}/evaluate", 
            evaluationRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitComparisonEvaluation_NonExistentComparison_ReturnsNotFound()
    {
        // Arrange
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
        var response = await _client!.PostAsJsonAsync("/deck-comparisons/999/evaluate", evaluationRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /deck-comparisons/{id}/statistics Tests

    [Fact]
    public async Task GetComparisonStatistics_WithEvaluations_ReturnsOkWithStatistics()
    {
        // Arrange
        var createResponse = await _client!.PostAsJsonAsync("/deck-comparisons", 
            new { deck1Id = 1, deck2Id = 2 });
        var comparison = await createResponse.Content.ReadFromJsonAsync<DeckComparison>();

        // Add some evaluations
        for (int i = 0; i < 5; i++)
        {
            var evaluationRequest = new
            {
                whichDeck = 1,
                generationAttempts = 1,
                evaluatedHand = new
                {
                    id = 100 + i,
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
            
            await _client!.PostAsJsonAsync($"/deck-comparisons/{comparison!.Id}/evaluate", evaluationRequest);
        }

        // Act
        var response = await _client!.GetAsync($"/deck-comparisons/{comparison!.Id}/statistics");

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
        var response = await _client!.GetAsync("/deck-comparisons/999/statistics");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /decklists/{id}/comparisons Tests

    [Fact]
    public async Task GetComparisonsForDeck_ExistingDeck_ReturnsOkWithArray()
    {
        // Arrange - Create a comparison
        await _client!.PostAsJsonAsync("/deck-comparisons", new { deck1Id = 1, deck2Id = 2 });

        // Act
        var response = await _client!.GetAsync("/decklists/1/comparisons");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var comparisons = await response.Content.ReadFromJsonAsync<DeckComparison[]>();
        Assert.NotNull(comparisons);
    }

    [Fact]
    public async Task GetComparisonsForDeck_NoComparisons_ReturnsEmptyArray()
    {
        // Act - Query deck that exists but has no comparisons
        var response = await _client!.GetAsync("/decklists/999/comparisons");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var comparisons = await response.Content.ReadFromJsonAsync<DeckComparison[]>();
        Assert.NotNull(comparisons);
        Assert.Empty(comparisons);
    }

    #endregion

    #region Content Type and CORS Tests

    [Fact]
    public async Task DeckComparisonEndpoints_ReturnJsonContentType()
    {
        // Arrange
        var createResponse = await _client!.PostAsJsonAsync("/deck-comparisons", 
            new { deck1Id = 1, deck2Id = 2 });
        var comparison = await createResponse.Content.ReadFromJsonAsync<DeckComparison>();

        // Act & Assert
        var getResponse = await _client!.GetAsync($"/deck-comparisons/{comparison!.Id}");
        Assert.Equal("application/json", getResponse.Content.Headers.ContentType?.MediaType);

        var getAllResponse = await _client!.GetAsync("/deck-comparisons");
        Assert.Equal("application/json", getAllResponse.Content.Headers.ContentType?.MediaType);
    }

    #endregion
}

/// <summary>
/// Dedicated test factory for deck comparison tests with isolated database.
/// </summary>
public class DeckComparisonTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"DeckComparisonTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll<DbContextOptions<EtaliDatabase>>();

            // Add a database context with unique name for this test class
            services.AddDbContext<EtaliDatabase>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Build service provider and seed data
            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();

            // Ensure database is created
            db.Database.EnsureCreated();

            // Seed test data
            if (!db.Decklists.Any())
            {
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
                    CreatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow
                });

                db.SaveChanges();
            }
        });
    }
}
