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
        
        // Seed test decklists
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
        
        _database.Decklists.Add(new Decklist
        {
            Id = 2,
            Name = "SecondDeck",
            CardNames = new[]
            {
                "CardA", "CardB", "CardC", "CardD", "CardE",
                "CardF", "CardG", "CardH", "CardI", "CardJ"
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
            Id = 3,
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

    [Fact]
    public void GenerateHand_Stickers_SelectedFromPool()
    {
        // The sticker pool is [6, 5, 5, 4, 4, 4, 4, 4, 4, 3]
        // When selecting 3, we can get duplicates (e.g., picking two 4s)
        
        // Act - Generate multiple hands
        for (int i = 0; i < 20; i++)
        {
            var hand = _service.GenerateHand(i + 100, "TestDeck");
            
            // Assert - Should have exactly 3 stickers
            Assert.Equal(3, hand.Stickers.Length);
            
            // All stickers should be from the available pool
            Assert.All(hand.Stickers, sticker => 
                Assert.Contains(sticker, _configuration.AvailableStickers));
        }
    }

    [Fact]
    public void GenerateHand_AllCardsAreFromSpecifiedDecklist()
    {
        // Arrange
        var expectedCards = _database.Decklists.First(d => d.Name == "TestDeck").CardNames;

        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert - All cards in hand should be from the specified decklist
        Assert.All(hand.Cards, card => Assert.Contains(card, expectedCards));
    }

    [Fact]
    public void GenerateHand_DifferentDecklists_ReturnCorrectMetadata()
    {
        // Act
        var hand1 = _service.GenerateHand(1, "TestDeck");
        var hand2 = _service.GenerateHand(2, "SecondDeck");

        // Assert
        Assert.Equal(1, hand1.DecklistId);
        Assert.Equal("TestDeck", hand1.DecklistName);
        Assert.All(hand1.Cards, card => Assert.StartsWith("Card", card));

        Assert.Equal(2, hand2.DecklistId);
        Assert.Equal("SecondDeck", hand2.DecklistName);
        Assert.All(hand2.Cards, card => Assert.StartsWith("Card", card));
    }

    [Fact]
    public void GenerateHand_NeverSelectsCardsFromDifferentDecklist()
    {
        // Arrange
        var testDeckCards = _database.Decklists.First(d => d.Name == "TestDeck").CardNames;
        var secondDeckCards = _database.Decklists.First(d => d.Name == "SecondDeck").CardNames;

        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert - Should only have cards from TestDeck, none from SecondDeck
        Assert.All(hand.Cards, card => Assert.Contains(card, testDeckCards));
        Assert.All(hand.Cards, card => Assert.DoesNotContain(card, secondDeckCards));
    }

    [Fact]
    public void GenerateHand_WithMinimalDecklist_Returns7Cards()
    {
        // Arrange - Decklist with exactly 7 cards
        _database.Decklists.Add(new Decklist
        {
            Id = 4,
            Name = "MinimalDeck",
            CardNames = new[] { "C1", "C2", "C3", "C4", "C5", "C6", "C7" }
        });
        _database.SaveChanges();

        // Act
        var hand = _service.GenerateHand(1, "MinimalDeck");

        // Assert
        Assert.Equal(7, hand.Cards.Length);
        Assert.Equal(7, hand.Cards.Distinct().Count()); // All unique
    }

    [Fact]
    public void GenerateHand_StickerValues_AreAllFromAvailablePool()
    {
        // Arrange
        var availableValues = _configuration.AvailableStickers.Distinct().ToList();

        // Act - Generate multiple hands
        for (int i = 0; i < 20; i++)
        {
            var hand = _service.GenerateHand(i + 200, "TestDeck");

            // Assert - Every sticker value must be in the available pool
            Assert.All(hand.Stickers, sticker => Assert.Contains(sticker, availableValues));
        }
    }

    [Fact]
    public void GenerateHand_MultipleHands_ProducesVariedSeatOrders()
    {
        // Act - Generate multiple hands
        var seatOrders = new List<int>();
        for (int i = 0; i < 20; i++)
        {
            var hand = _service.GenerateHand(i + 300, "TestDeck");
            seatOrders.Add(hand.SeatOrder);
        }

        // Assert - Should have some variety in seat orders (not all the same)
        var distinctOrders = seatOrders.Distinct().Count();
        Assert.True(distinctOrders > 1, "Seat orders should vary across multiple hands");
        Assert.All(seatOrders, order => Assert.InRange(order, 1, 4));
    }

    [Fact]
    public void GenerateHand_WithLargeDeck_StillReturns7UniqueCards()
    {
        // Arrange - Large 99-card deck
        var largeCardList = Enumerable.Range(1, 99).Select(i => $"Card{i}").ToArray();
        _database.Decklists.Add(new Decklist
        {
            Id = 5,
            Name = "LargeDeck",
            CardNames = largeCardList
        });
        _database.SaveChanges();

        // Act
        var hand = _service.GenerateHand(1, "LargeDeck");

        // Assert
        Assert.Equal(7, hand.Cards.Length);
        Assert.Equal(7, hand.Cards.Distinct().Count());
        Assert.All(hand.Cards, card => Assert.Contains(card, largeCardList));
    }

    [Fact]
    public void GenerateHand_LowestGambleOdds_IsWithinValidRange()
    {
        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert - LowestGambleOdds should be between 0 and 1 inclusive
        Assert.InRange(hand.LowestGambleOdds, 0.0f, 1.0f);
    }

    [Fact]
    public void GenerateHand_MultipleHands_ProducesVariedGambleOdds()
    {
        // Act - Generate multiple hands
        var gambleOdds = new List<float>();
        for (int i = 0; i < 50; i++)
        {
            var hand = _service.GenerateHand(i + 400, "TestDeck");
            gambleOdds.Add(hand.LowestGambleOdds);
        }

        // Assert - Should have variety in gamble odds (not all the same)
        var distinctOdds = gambleOdds.Distinct().Count();
        Assert.True(distinctOdds > 10, "Gamble odds should vary significantly across multiple hands");
        Assert.All(gambleOdds, odds => Assert.InRange(odds, 0.0f, 1.0f));
    }

    [Fact]
    public void GenerateHand_LowestGambleOdds_FollowsExpectedDistribution()
    {
        // Act - Generate many hands to test distribution
        var gambleOdds = new List<float>();
        for (int i = 0; i < 1000; i++)
        {
            var hand = _service.GenerateHand(i + 500, "TestDeck");
            gambleOdds.Add(hand.LowestGambleOdds);
        }

        // Assert - With triangular distribution where density = x:
        // - Values above 0.7 should be more common than values below 0.3
        var countAbove07 = gambleOdds.Count(x => x >= 0.7f);
        var countBelow03 = gambleOdds.Count(x => x < 0.3f);
        
        // Theoretically: P(X >= 0.7) ? 0.505, P(X < 0.3) ? 0.045
        // So countAbove07 should be much larger than countBelow03
        Assert.True(countAbove07 > countBelow03 * 3, 
            $"Expected higher values to be more common. Above 0.7: {countAbove07}, Below 0.3: {countBelow03}");
        
        // Average should be around 0.816 for this distribution (2/?2 / ?2 ? 0.816)
        var average = gambleOdds.Average();
        Assert.InRange(average, 0.75, 0.87);
    }
}
