using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Models;

public class HandTests
{
    [Fact]
    public void Hand_Constructor_SetsAllProperties()
    {
        // Arrange
        var cards = new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" };
        var stickers = new[] { 6, 5, 4 };

        // Act
        var hand = new Hand(1, cards, 2, stickers, 10, "TestDeck", 0.75f);

        // Assert
        Assert.Equal(1, hand.Id);
        Assert.Equal(cards, hand.Cards);
        Assert.Equal(2, hand.SeatOrder);
        Assert.Equal(stickers, hand.Stickers);
        Assert.Equal(10, hand.DecklistId);
        Assert.Equal("TestDeck", hand.DecklistName);
        Assert.Equal(0.75f, hand.LowestGambleOdds);
    }

    [Fact]
    public void Hand_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var cards = new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" };
        var stickers = new[] { 6, 5, 4 };
        var hand1 = new Hand(1, cards, 2, stickers, 10, "TestDeck", 0.5f);
        var hand2 = new Hand(1, cards, 2, stickers, 10, "TestDeck", 0.5f);
        var hand3 = new Hand(2, cards, 2, stickers, 10, "TestDeck", 0.5f);

        // Assert
        Assert.Equal(hand1, hand2);
        Assert.NotEqual(hand1, hand3);
    }

    [Fact]
    public void Hand_CanBeDeconstructed()
    {
        // Arrange
        var cards = new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" };
        var stickers = new[] { 6, 5, 4 };
        var hand = new Hand(1, cards, 2, stickers, 10, "TestDeck", 0.8f);

        // Act
        var (id, handCards, seatOrder, handStickers, decklistId, decklistName, lowestGambleOdds) = hand;

        // Assert
        Assert.Equal(1, id);
        Assert.Equal(cards, handCards);
        Assert.Equal(2, seatOrder);
        Assert.Equal(stickers, handStickers);
        Assert.Equal(10, decklistId);
        Assert.Equal("TestDeck", decklistName);
        Assert.Equal(0.8f, lowestGambleOdds);
    }
}
