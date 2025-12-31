using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Etali.Tests.Services;

public class HandGenerationServiceTests
{
    private readonly GameConfiguration _configuration;
    private readonly EtaliDatabase _database;
    private readonly HandGenerationService _service;

    public HandGenerationServiceTests()
    {
        _configuration = new GameConfiguration();
        
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _database = new EtaliDatabase(options);
        
        // Seed test decklist
        _database.Decklists.Add(new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            CardNames = new[]
            {
                "Card1", "Card2", "Card3", "Card4", "Card5",
                "Card6", "Card7", "Card8", "Card9", "Card10"
            }
        });
        _database.SaveChanges();
        
        _service = new HandGenerationService(_configuration, _database);
    }

    [Fact]
    public void GenerateHand_ValidDecklistName_ReturnsHandWith7Cards()
    {
        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert
        Assert.NotNull(hand);
        Assert.Equal(7, hand.Cards.Length);
        Assert.Equal(1, hand.Id);
    }

    [Fact]
    public void GenerateHand_ValidDecklistName_ReturnsSeatOrderBetween1And4()
    {
        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert
        Assert.InRange(hand.SeatOrder, 1, 4);
    }

    [Fact]
    public void GenerateHand_ValidDecklistName_Returns3Stickers()
    {
        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert
        Assert.NotNull(hand.Stickers);
        Assert.Equal(3, hand.Stickers.Length);
    }

    [Fact]
    public void GenerateHand_ValidDecklistName_StickersAreFromAvailablePool()
    {
        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert
        foreach (var sticker in hand.Stickers)
        {
            Assert.Contains(sticker, _configuration.AvailableStickers);
        }
    }

    [Fact]
    public void GenerateHand_ValidDecklistName_CardsAreUnique()
    {
        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert
        Assert.Equal(hand.Cards.Length, hand.Cards.Distinct().Count());
    }

    [Fact]
    public void GenerateHand_ValidDecklistName_IncludesDecklistInfo()
    {
        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert
        Assert.Equal(1, hand.DecklistId);
        Assert.Equal("TestDeck", hand.DecklistName);
    }

    [Fact]
    public void GenerateHand_CaseInsensitiveDecklistName_ReturnsHand()
    {
        // Act
        var hand = _service.GenerateHand(1, "testdeck");

        // Assert
        Assert.NotNull(hand);
        Assert.Equal("TestDeck", hand.DecklistName);
    }

    [Fact]
    public void GenerateHand_NonExistentDecklistName_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _service.GenerateHand(1, "NonExistent"));
        
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void GenerateHand_DecklistWithLessThan7Cards_ThrowsInvalidOperationException()
    {
        // Arrange
        _database.Decklists.Add(new Decklist
        {
            Id = 2,
            Name = "SmallDeck",
            CardNames = new[] { "Card1", "Card2", "Card3" }
        });
        _database.SaveChanges();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _service.GenerateHand(1, "SmallDeck"));
        
        Assert.Contains("insufficient cards", exception.Message);
    }

    [Fact]
    public void GenerateHand_MultipleHands_ProducesRandomResults()
    {
        // Act
        var hand1 = _service.GenerateHand(1, "TestDeck");
        var hand2 = _service.GenerateHand(2, "TestDeck");
        var hand3 = _service.GenerateHand(3, "TestDeck");

        // Assert - At least one hand should be different (extremely unlikely all 3 are identical)
        bool handsAreDifferent = 
            !hand1.Cards.SequenceEqual(hand2.Cards) ||
            !hand2.Cards.SequenceEqual(hand3.Cards) ||
            hand1.SeatOrder != hand2.SeatOrder ||
            hand2.SeatOrder != hand3.SeatOrder;
        
        Assert.True(handsAreDifferent, "Multiple hands should produce varied results");
    }
}
