using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Models;

public class EvaluatedHandTests
{
    [Fact]
    public void EvaluatedHand_Constructor_SetsAllProperties()
    {
        // Arrange
        var cards = new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" };
        var stickers = new[] { 6, 5, 4 };

        // Act
        var evaluatedHand = new EvaluatedHand(
            1, cards, 2, stickers, true, 5, 10, "TestDeck");

        // Assert
        Assert.Equal(1, evaluatedHand.Id);
        Assert.Equal(cards, evaluatedHand.Cards);
        Assert.Equal(2, evaluatedHand.SeatOrder);
        Assert.Equal(stickers, evaluatedHand.Stickers);
        Assert.True(evaluatedHand.CanCastEtali);
        Assert.Equal(5, evaluatedHand.MinimumCardsToCastEtali);
        Assert.Equal(10, evaluatedHand.DecklistId);
        Assert.Equal("TestDeck", evaluatedHand.DecklistName);
    }

    [Fact]
    public void EvaluatedHand_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var cards = new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" };
        var stickers = new[] { 6, 5, 4 };
        var hand1 = new EvaluatedHand(1, cards, 2, stickers, true, 5, 10, "TestDeck");
        var hand2 = new EvaluatedHand(1, cards, 2, stickers, true, 5, 10, "TestDeck");
        var hand3 = new EvaluatedHand(1, cards, 2, stickers, false, 8, 10, "TestDeck");

        // Assert
        Assert.Equal(hand1, hand2);
        Assert.NotEqual(hand1, hand3);
    }

    [Fact]
    public void EvaluatedHand_CanCastEtaliFalse_ShouldHaveMinimumCards8()
    {
        // Arrange
        var cards = new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" };
        var stickers = new[] { 6, 5, 4 };

        // Act
        var evaluatedHand = new EvaluatedHand(
            1, cards, 2, stickers, false, 8, 10, "TestDeck");

        // Assert
        Assert.False(evaluatedHand.CanCastEtali);
        Assert.Equal(8, evaluatedHand.MinimumCardsToCastEtali);
    }

    [Fact]
    public void EvaluatedHand_CanCastEtaliTrue_MinimumCardsLessThan8()
    {
        // Arrange
        var cards = new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" };
        var stickers = new[] { 6, 5, 4 };

        // Act
        var evaluatedHand = new EvaluatedHand(
            1, cards, 2, stickers, true, 3, 10, "TestDeck");

        // Assert
        Assert.True(evaluatedHand.CanCastEtali);
        Assert.True(evaluatedHand.MinimumCardsToCastEtali < 8);
    }

    [Fact]
    public void EvaluatedHand_CanBeDeconstructed()
    {
        // Arrange
        var cards = new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" };
        var stickers = new[] { 6, 5, 4 };
        var hand = new EvaluatedHand(1, cards, 2, stickers, true, 5, 10, "TestDeck");

        // Act
        var (id, handCards, seatOrder, handStickers, canCast, minCards, decklistId, decklistName) = hand;

        // Assert
        Assert.Equal(1, id);
        Assert.Equal(cards, handCards);
        Assert.Equal(2, seatOrder);
        Assert.Equal(stickers, handStickers);
        Assert.True(canCast);
        Assert.Equal(5, minCards);
        Assert.Equal(10, decklistId);
        Assert.Equal("TestDeck", decklistName);
    }
}
