using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Models;

public class EvaluatedHandTests
{
    [Fact]
    public void EvaluatedHand_Constructor_SetsAllProperties()
    {
        // Arrange
        var cards = CreateTestCards(7);
        var stickers = new[] { 6, 5, 4 };

        // Act
        var evaluatedHand = new EvaluatedHand(
            1, cards, 2, stickers, true, 5, 10, "TestDeck", 0.75f, null);

        // Assert
        Assert.Equal(1, evaluatedHand.Id);
        Assert.Equal(cards, evaluatedHand.Cards);
        Assert.Equal(2, evaluatedHand.SeatOrder);
        Assert.Equal(stickers, evaluatedHand.Stickers);
        Assert.True(evaluatedHand.CanCastEtali);
        Assert.Equal(5, evaluatedHand.MinimumCardsToCastEtali);
        Assert.Equal(10, evaluatedHand.DecklistId);
        Assert.Equal("TestDeck", evaluatedHand.DecklistName);
        Assert.Equal(0.75f, evaluatedHand.LowestGambleOdds);
        Assert.Null(evaluatedHand.CreatedByUserId);
    }

    [Fact]
    public void EvaluatedHand_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var cards = CreateTestCards(7);
        var stickers = new[] { 6, 5, 4 };
        var hand1 = new EvaluatedHand(1, cards, 2, stickers, true, 5, 10, "TestDeck", 0.75f, null);
        var hand2 = new EvaluatedHand(1, cards, 2, stickers, true, 5, 10, "TestDeck", 0.75f, null);
        var hand3 = new EvaluatedHand(1, cards, 2, stickers, false, 8, 10, "TestDeck", 0.75f, null);

        // Assert
        Assert.Equal(hand1, hand2);
        Assert.NotEqual(hand1, hand3);
    }

    [Fact]
    public void EvaluatedHand_CanCastEtaliFalse_ShouldHaveMinimumCards8()
    {
        // Arrange
        var cards = CreateTestCards(7);
        var stickers = new[] { 6, 5, 4 };

        // Act
        var evaluatedHand = new EvaluatedHand(
            1, cards, 2, stickers, false, 8, 10, "TestDeck", 0.0f, null);

        // Assert
        Assert.False(evaluatedHand.CanCastEtali);
        Assert.Equal(8, evaluatedHand.MinimumCardsToCastEtali);
    }

    [Fact]
    public void EvaluatedHand_CanCastEtaliTrue_MinimumCardsLessThan8()
    {
        // Arrange
        var cards = CreateTestCards(7);
        var stickers = new[] { 6, 5, 4 };

        // Act
        var evaluatedHand = new EvaluatedHand(
            1, cards, 2, stickers, true, 3, 10, "TestDeck", 0.5f, null);

        // Assert
        Assert.True(evaluatedHand.CanCastEtali);
        Assert.True(evaluatedHand.MinimumCardsToCastEtali < 8);
    }

    [Fact]
    public void EvaluatedHand_CanBeDeconstructed()
    {
        // Arrange
        var cards = CreateTestCards(7);
        var stickers = new[] { 6, 5, 4 };
        var hand = new EvaluatedHand(1, cards, 2, stickers, true, 5, 10, "TestDeck", 0.8f, "user123");

        // Act
        var (id, handCards, seatOrder, handStickers, canCast, minCards, decklistId, decklistName, lowestGambleOdds, createdByUserId) = hand;

        // Assert
        Assert.Equal(1, id);
        Assert.Equal(cards, handCards);
        Assert.Equal(2, seatOrder);
        Assert.Equal(stickers, handStickers);
        Assert.True(canCast);
        Assert.Equal(5, minCards);
        Assert.Equal(10, decklistId);
        Assert.Equal("TestDeck", decklistName);
        Assert.Equal(0.8f, lowestGambleOdds);
        Assert.Equal("user123", createdByUserId);
    }

    private Card[] CreateTestCards(int count)
    {
        var cards = new Card[count];
        for (int i = 0; i < count; i++)
        {
            cards[i] = new Card { Name = $"Card{i + 1}", Type = "Creature" };
        }
        return cards;
    }
}
