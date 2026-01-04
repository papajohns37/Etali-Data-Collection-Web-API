using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Etali.Tests.Services;

public class StatisticsServiceTests
{
    private readonly EtaliDatabase _database;
    private readonly StatisticsService _service;

    public StatisticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _database = new EtaliDatabase(options);
        _service = new StatisticsService(_database);
    }

    [Fact]
    public async Task GetDecklistStatistics_NonExistentDecklist_ReturnsNull()
    {
        // Act
        var result = await _service.GetDecklistStatistics(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDecklistStatistics_DecklistWithNoHands_ReturnsEmptyStatistics()
    {
        // Arrange
        _database.Decklists.Add(new Decklist
        {
            Id = 1,
            Name = "Test Deck",
            Cards = new[]
            {
                new Card { Name = "Card1", Type = "Creature" },
                new Card { Name = "Card2", Type = "Instant" },
                new Card { Name = "Card3", Type = "Sorcery" },
                new Card { Name = "Card4", Type = "Artifact" },
                new Card { Name = "Card5", Type = "Enchantment" },
                new Card { Name = "Card6", Type = "Land" },
                new Card { Name = "Card7", Type = "Planeswalker" }
            }
        });
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetDecklistStatistics(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DecklistId);
        Assert.Equal("Test Deck", result.DecklistName);
        Assert.Equal(0, result.TotalHandsTested);
        Assert.Empty(result.HandSizeStatistics);
        Assert.Equal(0, result.OverallMulliganProbability);
    }

    [Fact]
    public async Task GetDecklistStatistics_SingleHandSize7_CalculatesCorrectProbability()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Test Deck", Cards = CreateTestCards(7) });
        
        // Add 10 hands: 7 successful (minimumCards <= 7), 3 unsuccessful
        for (int i = 1; i <= 7; i++)
        {
            _database.EvaluatedHands.Add(CreateHand(i, 1, true, 5)); // Can cast with 5 cards
        }
        for (int i = 8; i <= 10; i++)
        {
            _database.EvaluatedHands.Add(CreateHand(i, 1, false, 8)); // Cannot cast
        }
        
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetDecklistStatistics(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.TotalHandsTested);
        
        var size7Stats = result.HandSizeStatistics.First(s => s.HandSize == 7);
        Assert.Equal(10, size7Stats.TotalHands);
        Assert.Equal(7, size7Stats.SuccessfulHands);
        Assert.Equal(0.7, size7Stats.Probability, precision: 10);
        Assert.True(size7Stats.ConfidenceIntervalLower < 0.7);
        Assert.True(size7Stats.ConfidenceIntervalUpper > 0.7);
    }

    [Fact]
    public async Task GetDecklistStatistics_MultipleHandSizes_CountsCorrectly()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Test Deck", Cards = CreateTestCards(7) });
        
        // Hand 1: Can cast with 3 cards (counts for 3, 4, 5, 6, 7)
        _database.EvaluatedHands.Add(CreateHand(1, 1, true, 3));
        
        // Hand 2: Can cast with 5 cards (counts for 5, 6, 7)
        _database.EvaluatedHands.Add(CreateHand(2, 1, true, 5));
        
        // Hand 3: Can cast with 7 cards (counts for 7 only)
        _database.EvaluatedHands.Add(CreateHand(3, 1, true, 7));
        
        // Hand 4: Cannot cast (counts for none)
        _database.EvaluatedHands.Add(CreateHand(4, 1, false, 8));
        
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetDecklistStatistics(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalHandsTested);
        
        // Hand size 7: all 4 hands, 3 successful
        var size7 = result.HandSizeStatistics.First(s => s.HandSize == 7);
        Assert.Equal(4, size7.TotalHands);
        Assert.Equal(3, size7.SuccessfulHands);
        Assert.Equal(0.75, size7.Probability, precision: 10);
        
        // Hand size 5: all 4 hands, 2 successful (hands 1 and 2)
        var size5 = result.HandSizeStatistics.First(s => s.HandSize == 5);
        Assert.Equal(4, size5.TotalHands);
        Assert.Equal(2, size5.SuccessfulHands);
        Assert.Equal(0.5, size5.Probability, precision: 10);
        
        // Hand size 3: all 4 hands, 1 successful (hand 1 only)
        var size3 = result.HandSizeStatistics.First(s => s.HandSize == 3);
        Assert.Equal(4, size3.TotalHands);
        Assert.Equal(1, size3.SuccessfulHands);
        Assert.Equal(0.25, size3.Probability, precision: 10);
    }

    [Fact]
    public async Task GetDecklistStatistics_ZeroSuccesses_ReturnsZeroProbability()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Test Deck", Cards = CreateTestCards(7) });
        
        // Add hands that cannot cast at all
        for (int i = 1; i <= 5; i++)
        {
            _database.EvaluatedHands.Add(CreateHand(i, 1, false, 8));
        }
        
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetDecklistStatistics(1);

        // Assert
        Assert.NotNull(result);
        var size7 = result.HandSizeStatistics.First(s => s.HandSize == 7);
        Assert.Equal(0, size7.Probability);
        Assert.Equal(0, size7.ConfidenceIntervalLower);
        Assert.True(size7.ConfidenceIntervalUpper > 0); // Wilson score gives upper bound > 0
    }

    [Fact]
    public async Task GetDecklistStatistics_AllSuccesses_ReturnsOneProbability()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Test Deck", Cards = CreateTestCards(7) });
        
        // Add hands that can all cast
        for (int i = 1; i <= 5; i++)
        {
            _database.EvaluatedHands.Add(CreateHand(i, 1, true, 3));
        }
        
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetDecklistStatistics(1);

        // Assert
        Assert.NotNull(result);
        var size7 = result.HandSizeStatistics.First(s => s.HandSize == 7);
        Assert.Equal(1.0, size7.Probability, precision: 5); // Allow floating point precision
        Assert.True(size7.ConfidenceIntervalLower < 1.0); // Wilson score gives lower bound < 1
        Assert.Equal(1.0, size7.ConfidenceIntervalUpper, precision: 5);
    }

    [Fact]
    public async Task GetDecklistStatistics_ConfidenceIntervals_AreInValidRange()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Test Deck", Cards = CreateTestCards(7) });
        
        for (int i = 1; i <= 20; i++)
        {
            _database.EvaluatedHands.Add(CreateHand(i, 1, i <= 12, 5)); // 12/20 = 60% success
        }
        
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetDecklistStatistics(1);

        // Assert
        Assert.NotNull(result);
        
        foreach (var stats in result.HandSizeStatistics)
        {
            // CI should be within [0, 1]
            Assert.InRange(stats.ConfidenceIntervalLower, 0, 1);
            Assert.InRange(stats.ConfidenceIntervalUpper, 0, 1);
            
            // Lower should be less than upper
            Assert.True(stats.ConfidenceIntervalLower <= stats.ConfidenceIntervalUpper);
            
            // Probability should be within CI
            Assert.True(stats.Probability >= stats.ConfidenceIntervalLower || Math.Abs(stats.Probability - stats.ConfidenceIntervalLower) < 0.0001);
            Assert.True(stats.Probability <= stats.ConfidenceIntervalUpper || Math.Abs(stats.Probability - stats.ConfidenceIntervalUpper) < 0.0001);
        }
    }

    [Fact]
    public async Task GetDecklistStatistics_OverallMulliganProbability_CalculatedCorrectly()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Test Deck", Cards = CreateTestCards(7) });
        
        // Create hands with specific minimum cards to control probabilities
        _database.EvaluatedHands.Add(CreateHand(1, 1, true, 3));  // Counts for 3,4,5,6,7
        _database.EvaluatedHands.Add(CreateHand(2, 1, true, 4));  // Counts for 4,5,6,7
        _database.EvaluatedHands.Add(CreateHand(3, 1, true, 5));  // Counts for 5,6,7
        _database.EvaluatedHands.Add(CreateHand(4, 1, true, 6));  // Counts for 6,7
        _database.EvaluatedHands.Add(CreateHand(5, 1, true, 7));  // Counts for 7
        for (int i = 6; i <= 10; i++)
        {
            _database.EvaluatedHands.Add(CreateHand(i, 1, false, 8));  // Cannot cast
        }
        
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetDecklistStatistics(1);

        // Assert
        Assert.NotNull(result);
        
        // Verify probabilities
        var p7 = result.HandSizeStatistics.First(s => s.HandSize == 7).Probability;
        var p6 = result.HandSizeStatistics.First(s => s.HandSize == 6).Probability;
        var p5 = result.HandSizeStatistics.First(s => s.HandSize == 5).Probability;
        var p4 = result.HandSizeStatistics.First(s => s.HandSize == 4).Probability;
        var p3 = result.HandSizeStatistics.First(s => s.HandSize == 3).Probability;
        
        Assert.Equal(0.5, p7);
        Assert.Equal(0.4, p6);
        Assert.Equal(0.3, p5);
        Assert.Equal(0.2, p4);
        Assert.Equal(0.1, p3);
        
        // Formula: 1 - (1 - p7)^2 * (1 - p6) * (1 - p5) * (1 - p4) * (1 - p3)
        double expected = 1 - Math.Pow(0.5, 2) * 0.6 * 0.7 * 0.8 * 0.9;
        
        Assert.Equal(expected, result.OverallMulliganProbability, precision: 3);
        Assert.True(result.OverallMulliganConfidenceIntervalLower < result.OverallMulliganProbability);
        Assert.True(result.OverallMulliganConfidenceIntervalUpper > result.OverallMulliganProbability);
    }

    [Fact]
    public async Task GetDecklistStatistics_SmallSample_WilsonScoreGivesWiderCI()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Test Deck", Cards = CreateTestCards(7) });
        
        // Small sample: 2 hands, 1 success = 50%
        _database.EvaluatedHands.Add(CreateHand(1, 1, true, 5));
        _database.EvaluatedHands.Add(CreateHand(2, 1, false, 8));
        
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetDecklistStatistics(1);

        // Assert
        var size7 = result.HandSizeStatistics.First(s => s.HandSize == 7);
        Assert.Equal(0.5, size7.Probability);
        
        // With only 2 samples, CI should be quite wide
        double ciWidth = size7.ConfidenceIntervalUpper - size7.ConfidenceIntervalLower;
        Assert.True(ciWidth > 0.5, $"CI width should be > 0.5 for small sample, was {ciWidth}");
    }

    [Fact]
    public async Task GetDecklistStatistics_LargeSample_WilsonScoreGivesNarrowerCI()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Test Deck", Cards = CreateTestCards(7) });
        
        // Large sample: 100 hands, 50 successes = 50%
        for (int i = 1; i <= 100; i++)
        {
            _database.EvaluatedHands.Add(CreateHand(i, 1, i <= 50, 5));
        }
        
        await _database.SaveChangesAsync();

        // Act
        var result = await _service.GetDecklistStatistics(1);

        // Assert
        var size7 = result.HandSizeStatistics.First(s => s.HandSize == 7);
        Assert.Equal(0.5, size7.Probability, precision: 5);
        
        // With 100 samples, CI should be narrow
        double ciWidth = size7.ConfidenceIntervalUpper - size7.ConfidenceIntervalLower;
        Assert.True(ciWidth < 0.17, $"CI width should be < 0.17 for large sample, was {ciWidth}");
    }

    [Fact]
    public async Task GetDecklistStatistics_MultipleDecks_IsolatesCorrectly()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Deck 1", Cards = CreateTestCards(7) });
        _database.Decklists.Add(new Decklist { Id = 2, Name = "Deck 2", Cards = CreateTestCards(7) });
        
        // Deck 1: 3 hands, all successful
        for (int i = 1; i <= 3; i++)
        {
            _database.EvaluatedHands.Add(CreateHand(i, 1, true, 5));
        }
        
        // Deck 2: 2 hands, all fail
        for (int i = 4; i <= 5; i++)
        {
            _database.EvaluatedHands.Add(CreateHand(i, 2, false, 8));
        }
        
        await _database.SaveChangesAsync();

        // Act
        var result1 = await _service.GetDecklistStatistics(1);
        var result2 = await _service.GetDecklistStatistics(2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        
        Assert.Equal(3, result1.TotalHandsTested);
        Assert.Equal(2, result2.TotalHandsTested);
        
        var size7Deck1 = result1.HandSizeStatistics.First(s => s.HandSize == 7);
        var size7Deck2 = result2.HandSizeStatistics.First(s => s.HandSize == 7);
        
        Assert.Equal(1.0, size7Deck1.Probability);
        Assert.Equal(0.0, size7Deck2.Probability);
    }

    private Card[] CreateTestCards(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Card { Name = $"Card{i}", Type = "Creature" })
            .ToArray();
    }

    private EvaluatedHand CreateHand(int id, int decklistId, bool canCastEtali, int minimumCards)
    {
        return new EvaluatedHand(
            id,
            CreateTestCards(7),
            1,
            new[] { 6, 5, 4 },
            canCastEtali,
            minimumCards,
            decklistId,
            "Test Deck",
            0.5f
        );
    }
}
