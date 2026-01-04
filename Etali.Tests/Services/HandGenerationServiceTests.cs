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
            Cards = new[]
            {
                new Card { Name = "Card1", Type = "Creature" },
                new Card { Name = "Card2", Type = "Instant" },
                new Card { Name = "Card3", Type = "Sorcery" },
                new Card { Name = "Card4", Type = "Artifact" },
                new Card { Name = "Card5", Type = "Enchantment" },
                new Card { Name = "Card6", Type = "Land" },
                new Card { Name = "Card7", Type = "Planeswalker" },
                new Card { Name = "Card8", Type = "Creature" },
                new Card { Name = "Card9", Type = "Instant" },
                new Card { Name = "Card10", Type = "Land" }
            }
        });
        
        _database.Decklists.Add(new Decklist
        {
            Id = 2,
            Name = "SecondDeck",
            Cards = new[]
            {
                new Card { Name = "CardA", Type = "Creature" },
                new Card { Name = "CardB", Type = "Instant" },
                new Card { Name = "CardC", Type = "Sorcery" },
                new Card { Name = "CardD", Type = "Artifact" },
                new Card { Name = "CardE", Type = "Enchantment" },
                new Card { Name = "CardF", Type = "Land" },
                new Card { Name = "CardG", Type = "Planeswalker" },
                new Card { Name = "CardH", Type = "Creature" },
                new Card { Name = "CardI", Type = "Instant" },
                new Card { Name = "CardJ", Type = "Land" }
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
            Cards = new[] 
            { 
                new Card { Name = "Card1", Type = "Creature" },
                new Card { Name = "Card2", Type = "Instant" },
                new Card { Name = "Card3", Type = "Land" }
            }
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
        var expectedCardNames = _database.Decklists.First(d => d.Name == "TestDeck")
            .Cards.Select(c => c.Name).ToArray();

        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert - All cards in hand should be from the specified decklist
        var handCardNames = hand.Cards.Select(c => c.Name).ToArray();
        Assert.All(handCardNames, cardName => Assert.Contains(cardName, expectedCardNames));
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
        Assert.All(hand1.Cards, card => Assert.StartsWith("Card", card.Name));

        Assert.Equal(2, hand2.DecklistId);
        Assert.Equal("SecondDeck", hand2.DecklistName);
        Assert.All(hand2.Cards, card => Assert.StartsWith("Card", card.Name));
    }

    [Fact]
    public void GenerateHand_NeverSelectsCardsFromDifferentDecklist()
    {
        // Arrange
        var testDeckCardNames = _database.Decklists.First(d => d.Name == "TestDeck")
            .Cards.Select(c => c.Name).ToArray();
        var secondDeckCardNames = _database.Decklists.First(d => d.Name == "SecondDeck")
            .Cards.Select(c => c.Name).ToArray();

        // Act
        var hand = _service.GenerateHand(1, "TestDeck");

        // Assert - Should only have cards from TestDeck, none from SecondDeck
        var handCardNames = hand.Cards.Select(c => c.Name).ToArray();
        Assert.All(handCardNames, cardName => Assert.Contains(cardName, testDeckCardNames));
        Assert.All(handCardNames, cardName => Assert.DoesNotContain(cardName, secondDeckCardNames));
    }

    [Fact]
    public void GenerateHand_WithMinimalDecklist_Returns7Cards()
    {
        // Arrange - Decklist with exactly 7 cards
        _database.Decklists.Add(new Decklist
        {
            Id = 4,
            Name = "MinimalDeck",
            Cards = new[]
            {
                new Card { Name = "C1", Type = "Creature" },
                new Card { Name = "C2", Type = "Instant" },
                new Card { Name = "C3", Type = "Sorcery" },
                new Card { Name = "C4", Type = "Artifact" },
                new Card { Name = "C5", Type = "Enchantment" },
                new Card { Name = "C6", Type = "Land" },
                new Card { Name = "C7", Type = "Planeswalker" }
            }
        });
        _database.SaveChanges();

        // Act
        var hand = _service.GenerateHand(1, "MinimalDeck");

        // Assert
        Assert.Equal(7, hand.Cards.Length);
        var cardNames = hand.Cards.Select(c => c.Name).ToArray();
        Assert.Equal(7, cardNames.Distinct().Count()); // All unique
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
        var largeCardList = Enumerable.Range(1, 99)
            .Select(i => new Card { Name = $"Card{i}", Type = "Creature" })
            .ToArray();
        _database.Decklists.Add(new Decklist
        {
            Id = 5,
            Name = "LargeDeck",
            Cards = largeCardList
        });
        _database.SaveChanges();

        // Act
        var hand = _service.GenerateHand(1, "LargeDeck");

        // Assert
        Assert.Equal(7, hand.Cards.Length);
        var cardNames = hand.Cards.Select(c => c.Name).ToArray();
        Assert.Equal(7, cardNames.Distinct().Count());
        var largeCardNames = largeCardList.Select(c => c.Name).ToArray();
        Assert.All(cardNames, cardName => Assert.Contains(cardName, largeCardNames));
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
    public void GenerateHand_LowestGambleOdds_SupportsGambleMechanic_50Percent()
    {
        // This test verifies that with 50% Gamble odds, the card succeeds 50% of the time
        // Gamble succeeds when: gambleOdds < lowestGambleOdds is FALSE (i.e., lowestGambleOdds < gambleOdds)
        
        // Act - Generate many hands to test distribution
        const int sampleSize = 1000;
        const double gambleOdds = 0.5; // 50% success rate
        var successCount = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            var hand = _service.GenerateHand(i + 500, "TestDeck");
            if (hand.LowestGambleOdds < gambleOdds)
            {
                successCount++; // Gamble succeeds
            }
        }

        // Assert - Should be close to 50% (within reasonable statistical margin)
        double successRate = (double)successCount / sampleSize;
        Assert.InRange(successRate, 0.45, 0.55); // 50% ± 5%
    }

    [Fact]
    public void GenerateHand_LowestGambleOdds_SupportsGambleMechanic_10Percent()
    {
        // This test verifies that with 10% Gamble odds, the card succeeds 10% of the time
        
        // Act - Generate many hands
        const int sampleSize = 1000;
        const double gambleOdds = 0.1; // 10% success rate
        var successCount = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            var hand = _service.GenerateHand(i + 1500, "TestDeck");
            if (hand.LowestGambleOdds < gambleOdds)
            {
                successCount++; // Gamble succeeds
            }
        }

        // Assert - Should be close to 10% (within reasonable statistical margin)
        double successRate = (double)successCount / sampleSize;
        Assert.InRange(successRate, 0.06, 0.14); // 10% ± 4%
    }

    [Fact]
    public void GenerateHand_LowestGambleOdds_SupportsGambleMechanic_90Percent()
    {
        // This test verifies that with 90% Gamble odds, the card succeeds 90% of the time
        
        // Act - Generate many hands
        const int sampleSize = 1000;
        const double gambleOdds = 0.9; // 90% success rate
        var successCount = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            var hand = _service.GenerateHand(i + 2500, "TestDeck");
            if (hand.LowestGambleOdds < gambleOdds)
            {
                successCount++; // Gamble succeeds
            }
        }

        // Assert - Should be close to 90% (within reasonable statistical margin)
        double successRate = (double)successCount / sampleSize;
        Assert.InRange(successRate, 0.86, 0.94); // 90% ± 4%
    }

    [Fact]
    public void GenerateHand_LowestGambleOdds_SupportsGambleMechanic_VariousRatios()
    {
        // This test verifies that Gamble works correctly for various odd ratios
        // Testing common Magic: The Gathering scenarios (cards in hand scenarios)
        
        const int sampleSize = 1000;
        
        var testCases = new[]
        {
            (odds: 1.0 / 3.0, label: "1/3", tolerance: 0.05),  // 33.3%
            (odds: 2.0 / 3.0, label: "2/3", tolerance: 0.05),  // 66.7%
            (odds: 3.0 / 7.0, label: "3/7", tolerance: 0.05),  // 42.9%
            (odds: 6.0 / 7.0, label: "6/7", tolerance: 0.04),  // 85.7%
            (odds: 4.0 / 7.0, label: "4/7", tolerance: 0.05)   // 57.1%
        };

        foreach (var (odds, label, tolerance) in testCases)
        {
            var successCount = 0;
            
            for (int i = 0; i < sampleSize; i++)
            {
                var hand = _service.GenerateHand(i + 3500 + (int)(odds * 1000), "TestDeck");
                if (hand.LowestGambleOdds < odds)
                {
                    successCount++;
                }
            }

            double successRate = (double)successCount / sampleSize;
            double expectedRate = odds;
            
            Assert.InRange(successRate, expectedRate - tolerance, expectedRate + tolerance);
        }
    }

    [Fact]
    public void GenerateHand_LowestGambleOdds_UniformDistribution_Verification()
    {
        // Verify that the distribution is truly uniform by checking multiple ranges
        
        const int sampleSize = 10000;
        var values = new List<float>();
        
        for (int i = 0; i < sampleSize; i++)
        {
            var hand = _service.GenerateHand(i + 5000, "TestDeck");
            values.Add(hand.LowestGambleOdds);
        }

        // Check that values are distributed across the entire [0, 1] range
        var below025 = values.Count(v => v < 0.25f);
        var between025and050 = values.Count(v => v >= 0.25f && v < 0.50f);
        var between050and075 = values.Count(v => v >= 0.50f && v < 0.75f);
        var above075 = values.Count(v => v >= 0.75f);

        // Each quartile should have approximately 25% of values (within 3%)
        Assert.InRange(below025, 2200, 2800); // 25% of 10000 = 2500 ± 300
        Assert.InRange(between025and050, 2200, 2800);
        Assert.InRange(between050and075, 2200, 2800);
        Assert.InRange(above075, 2200, 2800);

        // Average should be close to 0.5 for uniform distribution
        var average = values.Average();
        Assert.InRange(average, 0.48, 0.52);
    }
}
