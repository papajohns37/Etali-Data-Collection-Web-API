using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Etali.Tests.Database;

public class EtaliDatabaseTests
{
    [Fact]
    public void EtaliDatabase_CanAddAndRetrieveEvaluatedHands()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        var evaluatedHand = new EvaluatedHand(
            1,
            CreateTestCards(7),
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck",
            0.0f
        );

        // Act
        context.EvaluatedHands.Add(evaluatedHand);
        context.SaveChanges();

        // Assert
        var retrieved = context.EvaluatedHands.FirstOrDefault(e => e.Id == 1);
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved.Id);
        Assert.True(retrieved.CanCastEtali);
    }

    [Fact]
    public void EtaliDatabase_CanAddAndRetrieveDecklists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            Cards = new[]
            {
                new Card { Name = "Card1", Type = "Creature" },
                new Card { Name = "Card2", Type = "Instant" },
                new Card { Name = "Card3", Type = "Land" }
            }
        };

        // Act
        context.Decklists.Add(decklist);
        context.SaveChanges();

        // Assert
        var retrieved = context.Decklists.FirstOrDefault(d => d.Id == 1);
        Assert.NotNull(retrieved);
        Assert.Equal("TestDeck", retrieved.Name);
        Assert.Equal(3, retrieved.Cards.Length);
    }

    [Fact]
    public async Task EtaliDatabase_SupportsAsyncOperations()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            Cards = new[]
            {
                new Card { Name = "Card1", Type = "Creature" },
                new Card { Name = "Card2", Type = "Instant" }
            }
        };

        // Act
        context.Decklists.Add(decklist);
        await context.SaveChangesAsync();
        var retrieved = await context.Decklists.FirstOrDefaultAsync(d => d.Id == 1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("TestDeck", retrieved.Name);
    }

    [Fact]
    public void EtaliDatabase_CanStoreMultipleEvaluatedHands()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);

        // Act
        for (int i = 1; i <= 5; i++)
        {
            context.EvaluatedHands.Add(new EvaluatedHand(
                i,
                CreateTestCards(7),
                i % 4 + 1,
                new[] { 6, 5, 4 },
                i % 2 == 0,
                i % 2 == 0 ? 8 : i,
                1,
                "TestDeck",
                0.0f
            ));
        }
        context.SaveChanges();

        // Assert
        Assert.Equal(5, context.EvaluatedHands.Count());
    }

    [Fact]
    public void EtaliDatabase_CanStoreMultipleDecklists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);

        // Act
        context.Decklists.Add(new Decklist { Id = 1, Name = "Deck1", Cards = new[] { new Card { Name = "Card1", Type = "Creature" } } });
        context.Decklists.Add(new Decklist { Id = 2, Name = "Deck2", Cards = new[] { new Card { Name = "Card2", Type = "Instant" } } });
        context.Decklists.Add(new Decklist { Id = 3, Name = "Deck3", Cards = new[] { new Card { Name = "Card3", Type = "Land" } } });
        context.SaveChanges();

        // Assert
        Assert.Equal(3, context.Decklists.Count());
    }

    [Fact]
    public async Task EvaluatedHand_ArrayConverters_RoundTripWithCommas()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        
        // Cards with commas in names
        var cardsWithCommas = new[]
        {
            new Card { Name = "Jace, the Mind Sculptor", Type = "Planeswalker" },
            new Card { Name = "Teferi, Hero of Dominaria", Type = "Legendary Planeswalker" },
            new Card { Name = "Oko, Thief of Crowns", Type = "Legendary Planeswalker" },
            new Card { Name = "Uro, Titan of Nature's Wrath", Type = "Legendary Creature" },
            new Card { Name = "Card5", Type = "Creature" },
            new Card { Name = "Card6", Type = "Instant" },
            new Card { Name = "Card7", Type = "Land" }
        };
        
        var evaluatedHand = new EvaluatedHand(
            1,
            cardsWithCommas,
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck",
            0.0f
        );

        // Act - Save and retrieve
        context.EvaluatedHands.Add(evaluatedHand);
        await context.SaveChangesAsync();

        var retrieved = await context.EvaluatedHands.FirstOrDefaultAsync(e => e.Id == 1);

        // Assert - Cards should be preserved exactly
        Assert.NotNull(retrieved);
        Assert.Equal(cardsWithCommas.Length, retrieved.Cards.Length);
        Assert.Equal("Jace, the Mind Sculptor", retrieved.Cards[0].Name);
        Assert.Equal("Teferi, Hero of Dominaria", retrieved.Cards[1].Name);
    }

    [Fact]
    public async Task Decklist_ArrayConverter_RoundTripWithSpecialCharacters()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        
        var specialCards = new[]
        {
            new Card { Name = "Urza's Saga", Type = "Enchantment — Saga" },
            new Card { Name = "Serra's Sanctum", Type = "Legendary Land" },
            new Card { Name = "_____ Goblin", Type = "Creature" },
            new Card { Name = "Déjà Vu", Type = "Sorcery" },
            new Card { Name = "Card: The Gathering", Type = "Instant" },
            new Card { Name = "Test//Card", Type = "Instant // Sorcery" },
            new Card { Name = "Card (Ver. 2)", Type = "Artifact" }
        };
        
        var decklist = new Decklist
        {
            Id = 1,
            Name = "Special Characters Deck",
            Cards = specialCards
        };

        // Act - Save and retrieve
        context.Decklists.Add(decklist);
        await context.SaveChangesAsync();

        var retrieved = await context.Decklists.FirstOrDefaultAsync(d => d.Id == 1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(specialCards.Length, retrieved.Cards.Length);
        Assert.Equal("Urza's Saga", retrieved.Cards[0].Name);
        Assert.Equal("_____ Goblin", retrieved.Cards[2].Name);
        Assert.Equal("Card: The Gathering", retrieved.Cards[4].Name);
    }

    [Fact]
    public async Task EvaluatedHand_VeryLongCardNames_HandledCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        
        var longCardName = new string('A', 200); // 200 character card name
        var cards = new[]
        {
            new Card { Name = longCardName, Type = "Creature" },
            new Card { Name = "Card2", Type = "Instant" },
            new Card { Name = "Card3", Type = "Sorcery" },
            new Card { Name = "Card4", Type = "Artifact" },
            new Card { Name = "Card5", Type = "Enchantment" },
            new Card { Name = "Card6", Type = "Land" },
            new Card { Name = "Card7", Type = "Planeswalker" }
        };
        
        var evaluatedHand = new EvaluatedHand(1, cards, 1, new[] { 6, 5, 4 }, true, 3, 1, "TestDeck", 0.0f);

        // Act
        context.EvaluatedHands.Add(evaluatedHand);
        await context.SaveChangesAsync();

        var retrieved = await context.EvaluatedHands.FirstOrDefaultAsync(e => e.Id == 1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(longCardName, retrieved.Cards[0].Name);
        Assert.Equal(200, retrieved.Cards[0].Name.Length);
    }

    [Fact]
    public async Task Decklist_Large99CardDeck_ConvertsCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        
        var cards = Enumerable.Range(1, 99)
            .Select(i => new Card { Name = $"Card Number {i}", Type = "Creature" })
            .ToArray();
        var decklist = new Decklist
        {
            Id = 1,
            Name = "Full Commander Deck",
            Cards = cards
        };

        // Act
        context.Decklists.Add(decklist);
        await context.SaveChangesAsync();

        var retrieved = await context.Decklists.FirstOrDefaultAsync(d => d.Id == 1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(99, retrieved.Cards.Length);
        Assert.Equal("Card Number 1", retrieved.Cards[0].Name);
        Assert.Equal("Card Number 99", retrieved.Cards[98].Name);
    }

    [Fact]
    public async Task EvaluatedHand_EmptyCardArray_HandledGracefully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        
        // This is an edge case - normally wouldn't happen, but test the converter
        var evaluatedHand = new EvaluatedHand(1, Array.Empty<Card>(), 1, new[] { 6, 5, 4 }, false, 8, 1, "TestDeck", 0.0f);

        // Act
        context.EvaluatedHands.Add(evaluatedHand);
        await context.SaveChangesAsync();

        var retrieved = await context.EvaluatedHands.FirstOrDefaultAsync(e => e.Id == 1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Empty(retrieved.Cards);
    }

    [Fact]
    public async Task Stickers_ArrayConverter_PreservesValues()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        
        var stickers = new[] { 6, 5, 3 }; // Different values
        var evaluatedHand = new EvaluatedHand(
            1,
            CreateTestCards(7, "C"),
            1,
            stickers,
            true,
            4,
            1,
            "TestDeck",
            0.0f
        );

        // Act
        context.EvaluatedHands.Add(evaluatedHand);
        await context.SaveChangesAsync();

        var retrieved = await context.EvaluatedHands.FirstOrDefaultAsync(e => e.Id == 1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(3, retrieved.Stickers.Length);
        Assert.Equal(6, retrieved.Stickers[0]);
        Assert.Equal(5, retrieved.Stickers[1]);
        Assert.Equal(3, retrieved.Stickers[2]);
    }

    [Fact]
    public async Task Database_SupportsMultipleOperations_InSequence()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);

        // Act - Multiple add operations
        context.Decklists.Add(new Decklist { Id = 1, Name = "Deck1", Cards = new[] { new Card { Name = "C1", Type = "Creature" } } });
        await context.SaveChangesAsync();

        context.EvaluatedHands.Add(new EvaluatedHand(1, CreateTestCards(7, "C"), 1, new[] { 6, 5, 4 }, true, 3, 1, "Deck1", 0.0f));
        await context.SaveChangesAsync();

        context.Decklists.Add(new Decklist { Id = 2, Name = "Deck2", Cards = new[] { new Card { Name = "C2", Type = "Instant" } } });
        await context.SaveChangesAsync();

        context.EvaluatedHands.Add(new EvaluatedHand(2, CreateTestCards(7, "C"), 2, new[] { 5, 4, 3 }, false, 8, 2, "Deck2", 0.0f));
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.Decklists.CountAsync());
        Assert.Equal(2, await context.EvaluatedHands.CountAsync());
    }

    /// <summary>
    /// Helper method to create test Card arrays.
    /// </summary>
    private Card[] CreateTestCards(int count, string prefix = "Card")
    {
        return Enumerable.Range(1, count)
            .Select(i => new Card { Name = $"{prefix}{i}", Type = "Creature" })
            .ToArray();
    }
}
