using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Models;

public class DeckComparisonTests
{
    [Fact]
    public void DeckComparison_InitialState_HasZeroN()
    {
        // Arrange & Act
        var comparison = new DeckComparison
        {
            Id = 1,
            Deck1Id = 5,
            Deck1Name = "Fast Etali",
            Deck2Id = 8,
            Deck2Name = "Budget Etali"
        };

        // Assert
        Assert.Equal(0, comparison.Deck1N);
        Assert.Equal(0, comparison.Deck2N);
    }

    [Fact]
    public void DeckComparison_CreatedAt_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var comparison = new DeckComparison
        {
            Id = 1,
            Deck1Id = 5,
            Deck1Name = "Fast Etali",
            Deck2Id = 8,
            Deck2Name = "Budget Etali"
        };

        var after = DateTime.UtcNow;

        // Assert
        Assert.True(comparison.CreatedAt >= before);
        Assert.True(comparison.CreatedAt <= after);
    }

    [Theory]
    [InlineData(1, 2, 1, 2, true)]  // Same order
    [InlineData(1, 2, 2, 1, true)]  // Reverse order
    [InlineData(5, 10, 10, 5, true)] // Reverse order with different IDs
    [InlineData(1, 2, 3, 4, false)] // Different decks
    [InlineData(1, 2, 1, 3, false)] // Partial match
    public void IsSameComparison_VariousInputs_ReturnsExpected(
        int compDeck1, int compDeck2, int testDeck1, int testDeck2, bool expected)
    {
        // Arrange
        var comparison = new DeckComparison
        {
            Id = 1,
            Deck1Id = compDeck1,
            Deck1Name = "Deck A",
            Deck2Id = compDeck2,
            Deck2Name = "Deck B"
        };

        // Act
        var result = comparison.IsSameComparison(testDeck1, testDeck2);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSameComparison_OrderIndependent_ReturnsTrue()
    {
        // Arrange
        var comparison = new DeckComparison
        {
            Id = 1,
            Deck1Id = 5,
            Deck1Name = "Fast Etali",
            Deck2Id = 10,
            Deck2Name = "Budget Etali"
        };

        // Act & Assert
        Assert.True(comparison.IsSameComparison(5, 10));
        Assert.True(comparison.IsSameComparison(10, 5));
    }
}

public class ComparisonEvaluatedHandTests
{
    [Fact]
    public void ComparisonEvaluatedHand_AllProperties_SetCorrectly()
    {
        // Arrange
        var cards = CreateTestCards(7);
        var stickers = new[] { 6, 5, 4 };

        // Act
        var hand = new ComparisonEvaluatedHand(
            1,
            5,
            1,
            cards,
            2,
            stickers,
            true,
            3,
            10,
            "Test Deck",
            0.75f,
            3);

        // Assert
        Assert.Equal(1, hand.Id);
        Assert.Equal(5, hand.ComparisonId);
        Assert.Equal(1, hand.WhichDeck);
        Assert.Equal(cards, hand.Cards);
        Assert.Equal(2, hand.SeatOrder);
        Assert.Equal(stickers, hand.Stickers);
        Assert.True(hand.CanCastEtali);
        Assert.Equal(3, hand.MinimumCardsToCastEtali);
        Assert.Equal(10, hand.DecklistId);
        Assert.Equal("Test Deck", hand.DecklistName);
        Assert.Equal(0.75f, hand.LowestGambleOdds);
        Assert.Equal(3, hand.GenerationAttempts);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ComparisonEvaluatedHand_WhichDeck_ValidValues(int whichDeck)
    {
        // Arrange & Act
        var hand = new ComparisonEvaluatedHand(
            1,
            5,
            whichDeck,
            CreateTestCards(7),
            2,
            new[] { 6, 5, 4 },
            true,
            3,
            10,
            "Test Deck",
            0.75f,
            3);

        // Assert
        Assert.Equal(whichDeck, hand.WhichDeck);
    }

    [Fact]
    public void ComparisonEvaluatedHand_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var cards = CreateTestCards(7);
        var stickers = new[] { 6, 5, 4 };
        
        var hand1 = new ComparisonEvaluatedHand(
            1, 5, 1, cards, 2, stickers, true, 3, 10, "Test Deck", 0.75f, 3);
        
        var hand2 = new ComparisonEvaluatedHand(
            1, 5, 1, cards, 2, stickers, true, 3, 10, "Test Deck", 0.75f, 3);
        
        var hand3 = new ComparisonEvaluatedHand(
            1, 5, 2, cards, 2, stickers, true, 3, 10, "Test Deck", 0.75f, 3); // Different WhichDeck

        // Assert
        Assert.Equal(hand1, hand2);
        Assert.NotEqual(hand1, hand3);
    }

    private static Card[] CreateTestCards(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Card { Name = $"Card{i}", Type = "Creature" })
            .ToArray();
    }
}

public class CardDifferencesTests
{
    [Fact]
    public void CardDifferences_AllProperties_SetCorrectly()
    {
        // Arrange
        var onlyInDeck1 = new[] { new Card { Name = "Card1", Type = "Artifact" } };
        var onlyInDeck2 = new[] { new Card { Name = "Card2", Type = "Artifact" } };
        var shared = new[] { new Card { Name = "Card3", Type = "Land" } };

        // Act
        var differences = new CardDifferences(onlyInDeck1, onlyInDeck2, shared);

        // Assert
        Assert.Equal(onlyInDeck1, differences.CardsOnlyInDeck1);
        Assert.Equal(onlyInDeck2, differences.CardsOnlyInDeck2);
        Assert.Equal(shared, differences.SharedCards);
    }

    [Fact]
    public void CardDifferences_EmptyArrays_ValidState()
    {
        // Arrange & Act
        var differences = new CardDifferences(
            Array.Empty<Card>(),
            Array.Empty<Card>(),
            Array.Empty<Card>());

        // Assert
        Assert.Empty(differences.CardsOnlyInDeck1);
        Assert.Empty(differences.CardsOnlyInDeck2);
        Assert.Empty(differences.SharedCards);
    }
}

public class HandSizeDeltaStatisticsTests
{
    [Fact]
    public void HandSizeDeltaStatistics_AllProperties_SetCorrectly()
    {
        // Arrange & Act
        var stats = new HandSizeDeltaStatistics(
            7,
            50,
            40,
            0.71,
            0.60,
            0.11,
            0.02,
            0.20);

        // Assert
        Assert.Equal(7, stats.HandSize);
        Assert.Equal(50, stats.Deck1SuccessfulHands);
        Assert.Equal(40, stats.Deck2SuccessfulHands);
        Assert.Equal(0.71, stats.Deck1Probability, 2);
        Assert.Equal(0.60, stats.Deck2Probability, 2);
        Assert.Equal(0.11, stats.ProbabilityDifference, 2);
        Assert.Equal(0.02, stats.ConfidenceIntervalLower, 2);
        Assert.Equal(0.20, stats.ConfidenceIntervalUpper, 2);
    }

    [Fact]
    public void HandSizeDeltaStatistics_PositiveDifference_Deck1Better()
    {
        // Arrange & Act
        var stats = new HandSizeDeltaStatistics(
            7,
            50,
            30,
            0.70,
            0.50,
            0.20, // Positive difference
            0.10,
            0.30);

        // Assert
        Assert.True(stats.ProbabilityDifference > 0);
        Assert.True(stats.Deck1Probability > stats.Deck2Probability);
    }

    [Fact]
    public void HandSizeDeltaStatistics_NegativeDifference_Deck2Better()
    {
        // Arrange & Act
        var stats = new HandSizeDeltaStatistics(
            7,
            30,
            50,
            0.50,
            0.70,
            -0.20, // Negative difference
            -0.30,
            -0.10);

        // Assert
        Assert.True(stats.ProbabilityDifference < 0);
        Assert.True(stats.Deck2Probability > stats.Deck1Probability);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void HandSizeDeltaStatistics_ValidHandSizes_AllSupported(int handSize)
    {
        // Arrange & Act
        var stats = new HandSizeDeltaStatistics(
            handSize,
            50,
            40,
            0.71,
            0.60,
            0.11,
            0.02,
            0.20);

        // Assert
        Assert.Equal(handSize, stats.HandSize);
    }
}

public class DeckComparisonStatisticsTests
{
    [Fact]
    public void DeckComparisonStatistics_AllProperties_SetCorrectly()
    {
        // Arrange
        var handSizeStats = new[]
        {
            new HandSizeDeltaStatistics(7, 50, 40, 0.71, 0.60, 0.11, 0.02, 0.20),
            new HandSizeDeltaStatistics(6, 45, 35, 0.65, 0.55, 0.10, 0.01, 0.19)
        };

        // Act
        var statistics = new DeckComparisonStatistics(
            1,
            5,
            "Fast Etali",
            8,
            "Budget Etali",
            70,
            65,
            handSizeStats,
            0.08,
            0.01,
            0.15);

        // Assert
        Assert.Equal(1, statistics.ComparisonId);
        Assert.Equal(5, statistics.Deck1Id);
        Assert.Equal("Fast Etali", statistics.Deck1Name);
        Assert.Equal(8, statistics.Deck2Id);
        Assert.Equal("Budget Etali", statistics.Deck2Name);
        Assert.Equal(70, statistics.Deck1TotalHandsEvaluated);
        Assert.Equal(65, statistics.Deck2TotalHandsEvaluated);
        Assert.Equal(handSizeStats, statistics.HandSizeDeltaStatistics);
        Assert.Equal(0.08, statistics.OverallProbabilityDifference, 2);
        Assert.Equal(0.01, statistics.OverallConfidenceIntervalLower, 2);
        Assert.Equal(0.15, statistics.OverallConfidenceIntervalUpper, 2);
    }

    [Fact]
    public void DeckComparisonStatistics_ConfidenceInterval_ContainsDifference()
    {
        // Arrange & Act
        var statistics = new DeckComparisonStatistics(
            1,
            5,
            "Fast Etali",
            8,
            "Budget Etali",
            70,
            65,
            Array.Empty<HandSizeDeltaStatistics>(),
            0.08,
            0.01,
            0.15);

        // Assert
        Assert.True(statistics.OverallConfidenceIntervalLower <= statistics.OverallProbabilityDifference);
        Assert.True(statistics.OverallConfidenceIntervalUpper >= statistics.OverallProbabilityDifference);
    }
}

public class ComparisonHandTests
{
    [Fact]
    public void ComparisonHand_AllProperties_SetCorrectly()
    {
        // Arrange
        var cards = CreateTestCards(7);
        var stickers = new[] { 6, 5, 4 };

        // Act
        var hand = new ComparisonHand(
            1,
            5,
            1,
            cards,
            2,
            stickers,
            10,
            "Test Deck",
            0.75f,
            3);

        // Assert
        Assert.Equal(1, hand.Id);
        Assert.Equal(5, hand.ComparisonId);
        Assert.Equal(1, hand.WhichDeck);
        Assert.Equal(7, hand.Cards.Length);
        Assert.Equal(2, hand.SeatOrder);
        Assert.Equal(3, hand.Stickers.Length);
        Assert.Equal(10, hand.DecklistId);
        Assert.Equal("Test Deck", hand.DecklistName);
        Assert.Equal(0.75f, hand.LowestGambleOdds);
        Assert.Equal(3, hand.GenerationAttempts);
    }

    [Fact]
    public void ComparisonHand_GenerationAttempts_MustBePositive()
    {
        // Arrange
        var cards = CreateTestCards(7);
        var stickers = new[] { 6, 5, 4 };

        // Act
        var hand = new ComparisonHand(
            1, 5, 1, cards, 2, stickers, 10, "Test Deck", 0.75f, 5);

        // Assert
        Assert.True(hand.GenerationAttempts > 0);
    }

    private static Card[] CreateTestCards(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Card { Name = $"Card{i}", Type = "Creature" })
            .ToArray();
    }
}
