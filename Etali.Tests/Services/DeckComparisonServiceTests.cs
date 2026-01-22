using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Etali.Tests.Services;

public class DeckComparisonServiceTests
{
    private readonly EtaliDatabase _database;
    private readonly DeckComparisonService _service;
    private readonly Mock<IHandGenerationService> _handGenerationMock;

    public DeckComparisonServiceTests()
    {
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _database = new EtaliDatabase(options);
        _handGenerationMock = new Mock<IHandGenerationService>();
        
        _service = new DeckComparisonService(
            _database,
            _handGenerationMock.Object);
    }

    #region AddDeckComparisonAsync Tests

    [Fact]
    public async Task AddDeckComparisonAsync_ValidDecks_CreatesComparison()
    {
        // Arrange
        await SeedTestDecks();

        // Act
        var result = await _service.AddDeckComparisonAsync(1, 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.Deck1Id);
        Assert.Equal("Deck A", result.Deck1Name);
        Assert.Equal(2, result.Deck2Id);
        Assert.Equal("Deck B", result.Deck2Name);
        Assert.Equal(0, result.Deck1N);
        Assert.Equal(0, result.Deck2N);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AddDeckComparisonAsync_OrderIndependent_PreventsDuplicate()
    {
        // Arrange
        await SeedTestDecks();
        await _service.AddDeckComparisonAsync(1, 2);

        // Act & Assert - Try to add in reverse order
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddDeckComparisonAsync(2, 1));
    }

    [Fact]
    public async Task AddDeckComparisonAsync_SameOrder_PreventsDuplicate()
    {
        // Arrange
        await SeedTestDecks();
        await _service.AddDeckComparisonAsync(1, 2);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddDeckComparisonAsync(1, 2));
    }

    [Fact]
    public async Task AddDeckComparisonAsync_SameDeck_ThrowsException()
    {
        // Arrange
        await SeedTestDecks();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddDeckComparisonAsync(1, 1));
    }

    [Fact]
    public async Task AddDeckComparisonAsync_NonExistentDeck1_ThrowsException()
    {
        // Arrange
        await SeedTestDecks();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddDeckComparisonAsync(999, 2));
        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public async Task AddDeckComparisonAsync_NonExistentDeck2_ThrowsException()
    {
        // Arrange
        await SeedTestDecks();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddDeckComparisonAsync(1, 999));
        Assert.Contains("999", ex.Message);
    }

    #endregion

    #region GetCardDifferencesByDeckIdsAsync Tests

    [Fact]
    public async Task GetCardDifferencesByDeckIdsAsync_ValidDecks_ReturnsCorrectDifferences()
    {
        // Arrange
        await SeedTestDecks();

        // Act
        var result = await _service.GetCardDifferencesByDeckIdsAsync(1, 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.CardsOnlyInDeck1.Length); // Sol Ring, Mana Vault
        Assert.Equal(2, result.CardsOnlyInDeck2.Length); // Worn Powerstone, Card4
        Assert.Equal(3, result.SharedCards.Length); // Mountain, Command Tower, Card5
    }

    [Fact]
    public async Task GetCardDifferencesByDeckIdsAsync_NonExistentDeck_ReturnsNull()
    {
        // Arrange
        await SeedTestDecks();

        // Act
        var result = await _service.GetCardDifferencesByDeckIdsAsync(999, 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCardDifferencesByDeckIdsAsync_IdenticalDecks_AllShared()
    {
        // Arrange
        var cards = CreateTestCards(5);
        _database.Decklists.Add(new Decklist
        {
            Id = 1,
            Name = "Deck A",
            Cards = cards
        });
        _database.Decklists.Add(new Decklist
        {
            Id = 2,
            Name = "Deck B",
            Cards = cards.ToArray() // Same cards
        });
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetCardDifferencesByDeckIdsAsync(1, 2);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.CardsOnlyInDeck1);
        Assert.Empty(result.CardsOnlyInDeck2);
        Assert.Equal(5, result.SharedCards.Length);
    }

    #endregion

    #region GenerateHandForComparisonAsync Tests

    [Fact]
    public async Task GenerateHandForComparisonAsync_ValidComparison_ReturnsHandWithUniqueCard()
    {
        // Arrange
        await SeedTestDecks();
        var comparison = await _service.AddDeckComparisonAsync(1, 2);
        
        var generatedHand = new Hand(
            1,
            new[] {
                new Card { Name = "Sol Ring", Type = "Artifact" }, // Unique to deck 1
                new Card { Name = "Mountain", Type = "Land" },
                new Card { Name = "Card3", Type = "Creature" },
                new Card { Name = "Card4", Type = "Creature" },
                new Card { Name = "Card5", Type = "Creature" },
                new Card { Name = "Card6", Type = "Creature" },
                new Card { Name = "Card7", Type = "Creature" }
            },
            2,
            new[] { 6, 5, 4 },
            1,
            "Deck A",
            0.5f);

        _handGenerationMock
            .Setup(x => x.GenerateHand(It.IsAny<int>(), "Deck A"))
            .Returns(generatedHand);

        // Act
        var result = await _service.GenerateHandForComparisonAsync(comparison.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(comparison.Id, result.ComparisonId);
        Assert.Equal(1, result.WhichDeck);
        Assert.Contains(result.Cards, c => c.Name == "Sol Ring");
        Assert.True(result.GenerationAttempts > 0);
    }

    [Fact]
    public async Task GenerateHandForComparisonAsync_SelectsDeckWithLowerN()
    {
        // Arrange
        await SeedTestDecks();
        var comparison = await _service.AddDeckComparisonAsync(1, 2);
        
        // Add comparison hands to make deck 1 have more evaluated hands than deck 2
        // This will cause the service to select deck 2 (the one with fewer evaluated hands)
        await AddComparisonHands(comparison.Id, 1, 3, 2); // 3 hands for deck 1
        await AddComparisonHands(comparison.Id, 2, 1, 1); // 1 hand for deck 2

        var generatedHand = new Hand(
            1,
            new[] {
                new Card { Name = "Worn Powerstone", Type = "Artifact" }, // Unique to deck 2
                new Card { Name = "Mountain", Type = "Land" },
                new Card { Name = "Card3", Type = "Creature" },
                new Card { Name = "Card4", Type = "Creature" },
                new Card { Name = "Card5", Type = "Creature" },
                new Card { Name = "Card6", Type = "Creature" },
                new Card { Name = "Card7", Type = "Creature" }
            },
            2,
            new[] { 6, 5, 4 },
            2,
            "Deck B",
            0.5f);

        // Use It.IsAny for both parameters to ensure the mock always returns a hand
        _handGenerationMock
            .Setup(x => x.GenerateHand(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(generatedHand);

        // Act
        var result = await _service.GenerateHandForComparisonAsync(comparison.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.WhichDeck); // Should select deck 2 (fewer comparison hands)
    }

    [Fact]
    public async Task GenerateHandForComparisonAsync_NoUniqueCards_ThrowsException()
    {
        // Arrange
        var identicalCards = CreateTestCards(7);
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Deck A", Cards = identicalCards });
        _database.Decklists.Add(new Decklist { Id = 2, Name = "Deck B", Cards = identicalCards });
        await _database.SaveChangesAsync();

        var comparison = await _service.AddDeckComparisonAsync(1, 2);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateHandForComparisonAsync(comparison.Id));
    }

    #endregion

    #region SubmitComparisonEvaluationAsync Tests

    [Fact]
    public async Task SubmitComparisonEvaluationAsync_ValidSubmission_IncrementsN()
    {
        // Arrange
        await SeedTestDecks();
        var comparison = await _service.AddDeckComparisonAsync(1, 2);
        
        var evaluatedHand = new EvaluatedHand(
            1,
            CreateTestCards(7),
            2,
            new[] { 6, 5, 4 },
            true,
            3,
            1,
            "Deck A",
            0.5f);

        // Act
        var result = await _service.SubmitComparisonEvaluationAsync(
            comparison.Id,
            1,
            evaluatedHand,
            5); // 5 generation attempts

        // Assert
        Assert.NotNull(result);
        Assert.Equal(comparison.Id, result.ComparisonId);
        Assert.Equal(1, result.WhichDeck);
        Assert.Equal(5, result.GenerationAttempts);

        // Verify N was incremented
        var updatedComparison = await _database.DeckComparisons.FirstAsync();
        Assert.Equal(5, updatedComparison.Deck1N);
        Assert.Equal(0, updatedComparison.Deck2N);
    }

    [Fact]
    public async Task SubmitComparisonEvaluationAsync_InvalidWhichDeck_ThrowsException()
    {
        // Arrange
        await SeedTestDecks();
        var comparison = await _service.AddDeckComparisonAsync(1, 2);
        
        var evaluatedHand = new EvaluatedHand(
            1,
            CreateTestCards(7),
            2,
            new[] { 6, 5, 4 },
            true,
            3,
            1,
            "Deck A",
            0.5f);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SubmitComparisonEvaluationAsync(comparison.Id, 3, evaluatedHand, 5));
    }

    [Fact]
    public async Task SubmitComparisonEvaluationAsync_NonExistentComparison_ReturnsNull()
    {
        // Arrange
        var evaluatedHand = new EvaluatedHand(
            1,
            CreateTestCards(7),
            2,
            new[] { 6, 5, 4 },
            true,
            3,
            1,
            "Deck A",
            0.5f);

        // Act
        var result = await _service.SubmitComparisonEvaluationAsync(999, 1, evaluatedHand, 5);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetComparisonStatisticsAsync Tests

    [Fact]
    public async Task GetComparisonStatisticsAsync_WithComparisonHandsOnly_CalculatesCorrectly()
    {
        // Arrange
        await SeedTestDecks();
        var comparison = await _service.AddDeckComparisonAsync(1, 2);

        // Add comparison hands
        await AddComparisonHands(comparison.Id, 1, 10, 7); // 10 hands, 7 successful
        await AddComparisonHands(comparison.Id, 2, 10, 5); // 10 hands, 5 successful

        // Act
        var result = await _service.GetComparisonStatisticsAsync(comparison.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Deck1TotalHandsEvaluated);
        Assert.Equal(10, result.Deck2TotalHandsEvaluated);
        
        var size7Stats = result.HandSizeDeltaStatistics.First(s => s.HandSize == 7);
        Assert.Equal(0.7, size7Stats.Deck1Probability, 2);
        Assert.Equal(0.5, size7Stats.Deck2Probability, 2);
        Assert.Equal(0.2, size7Stats.ProbabilityDifference, 2);
    }

    [Fact]
    public async Task GetComparisonStatisticsAsync_WithMainDatasetHands_IncludesFiltered()
    {
        // Arrange
        await SeedTestDecksWithDifferences();
        var comparison = await _service.AddDeckComparisonAsync(1, 2);

        // Add comparison hands
        await AddComparisonHands(comparison.Id, 1, 5, 3);
        await AddComparisonHands(comparison.Id, 2, 5, 2);

        // Add main dataset hands (some with unique cards, some without)
        await AddMainDatasetHands(1, 10, 7, true); // Hands with Sol Ring (unique to deck 1)
        await AddMainDatasetHands(2, 10, 5, true); // Hands with Worn Powerstone (unique to deck 2)

        // Act
        var result = await _service.GetComparisonStatisticsAsync(comparison.Id);

        // Assert
        Assert.NotNull(result);
        // Should include both comparison and filtered main dataset hands
        Assert.True(result.Deck1TotalHandsEvaluated > 5);
        Assert.True(result.Deck2TotalHandsEvaluated > 5);
    }

    [Fact]
    public async Task GetComparisonStatisticsAsync_NonExistentComparison_ReturnsNull()
    {
        // Act
        var result = await _service.GetComparisonStatisticsAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestDecks()
    {
        _database.Decklists.Add(new Decklist
        {
            Id = 1,
            Name = "Deck A",
            Cards = new[]
            {
                new Card { Name = "Sol Ring", Type = "Artifact" },
                new Card { Name = "Mana Vault", Type = "Artifact" },
                new Card { Name = "Mountain", Type = "Land" },
                new Card { Name = "Command Tower", Type = "Land" },
                new Card { Name = "Card5", Type = "Creature" }
            }
        });

        _database.Decklists.Add(new Decklist
        {
            Id = 2,
            Name = "Deck B",
            Cards = new[]
            {
                new Card { Name = "Worn Powerstone", Type = "Artifact" },
                new Card { Name = "Mountain", Type = "Land" },
                new Card { Name = "Command Tower", Type = "Land" },
                new Card { Name = "Card4", Type = "Creature" },
                new Card { Name = "Card5", Type = "Creature" }
            }
        });

        await _database.SaveChangesAsync();
    }

    private async Task SeedTestDecksWithDifferences()
    {
        _database.Decklists.Add(new Decklist
        {
            Id = 1,
            Name = "Deck A",
            Cards = new[]
            {
                new Card { Name = "Sol Ring", Type = "Artifact" }, // Unique
                new Card { Name = "Shared1", Type = "Land" },
                new Card { Name = "Shared2", Type = "Land" },
                new Card { Name = "Shared3", Type = "Creature" },
                new Card { Name = "Shared4", Type = "Creature" },
                new Card { Name = "Shared5", Type = "Creature" },
                new Card { Name = "Shared6", Type = "Creature" }
            }
        });

        _database.Decklists.Add(new Decklist
        {
            Id = 2,
            Name = "Deck B",
            Cards = new[]
            {
                new Card { Name = "Worn Powerstone", Type = "Artifact" }, // Unique
                new Card { Name = "Shared1", Type = "Land" },
                new Card { Name = "Shared2", Type = "Land" },
                new Card { Name = "Shared3", Type = "Creature" },
                new Card { Name = "Shared4", Type = "Creature" },
                new Card { Name = "Shared5", Type = "Creature" },
                new Card { Name = "Shared6", Type = "Creature" }
            }
        });

        await _database.SaveChangesAsync();
    }

    private Card[] CreateTestCards(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Card { Name = $"Card{i}", Type = "Creature" })
            .ToArray();
    }

    private async Task AddComparisonHands(int comparisonId, int whichDeck, int total, int successful)
    {
        var existingIds = _database.ComparisonEvaluatedHands.Select(h => h.Id).ToList();
        int nextId = existingIds.Any() ? existingIds.Max() + 1 : 1;

        for (int i = 0; i < total; i++)
        {
            bool canCast = i < successful;
            int minCards = canCast ? 3 : 8;

            _database.ComparisonEvaluatedHands.Add(new ComparisonEvaluatedHand(
                nextId++,
                comparisonId,
                whichDeck,
                CreateTestCards(7),
                2,
                new[] { 6, 5, 4 },
                canCast,
                minCards,
                whichDeck,
                whichDeck == 1 ? "Deck A" : "Deck B",
                0.5f,
                1));
        }

        await _database.SaveChangesAsync();
    }

    private async Task AddMainDatasetHands(int decklistId, int total, int successful, bool withUniqueCard)
    {
        var existingIds = _database.EvaluatedHands.Select(h => h.Id).ToList();
        int nextId = existingIds.Any() ? existingIds.Max() + 1 : 1;

        for (int i = 0; i < total; i++)
        {
            bool canCast = i < successful;
            int minCards = canCast ? 3 : 8;

            var cards = CreateTestCards(7).ToList();
            if (withUniqueCard)
            {
                // Add unique card based on deck
                cards[0] = decklistId == 1
                    ? new Card { Name = "Sol Ring", Type = "Artifact" }
                    : new Card { Name = "Worn Powerstone", Type = "Artifact" };
            }

            _database.EvaluatedHands.Add(new EvaluatedHand(
                nextId++,
                cards.ToArray(),
                2,
                new[] { 6, 5, 4 },
                canCast,
                minCards,
                decklistId,
                decklistId == 1 ? "Deck A" : "Deck B",
                0.5f));
        }

        await _database.SaveChangesAsync();
    }

    #endregion
}
