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
            new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" },
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck"
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
            CardNames = new[] { "Card1", "Card2", "Card3" }
        };

        // Act
        context.Decklists.Add(decklist);
        context.SaveChanges();

        // Assert
        var retrieved = context.Decklists.FirstOrDefault(d => d.Id == 1);
        Assert.NotNull(retrieved);
        Assert.Equal("TestDeck", retrieved.Name);
        Assert.Equal(3, retrieved.CardNames.Length);
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
            CardNames = new[] { "Card1", "Card2" }
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
                new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" },
                i % 4 + 1,
                new[] { 6, 5, 4 },
                i % 2 == 0,
                i % 2 == 0 ? 8 : i,
                1,
                "TestDeck"
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
        context.Decklists.Add(new Decklist { Id = 1, Name = "Deck1", CardNames = new[] { "Card1" } });
        context.Decklists.Add(new Decklist { Id = 2, Name = "Deck2", CardNames = new[] { "Card2" } });
        context.Decklists.Add(new Decklist { Id = 3, Name = "Deck3", CardNames = new[] { "Card3" } });
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
            "Jace, the Mind Sculptor",
            "Teferi, Hero of Dominaria",
            "Oko, Thief of Crowns",
            "Uro, Titan of Nature's Wrath",
            "Card5",
            "Card6",
            "Card7"
        };
        
        var evaluatedHand = new EvaluatedHand(
            1,
            cardsWithCommas,
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck"
        );

        // Act - Save and retrieve
        context.EvaluatedHands.Add(evaluatedHand);
        await context.SaveChangesAsync();

        var retrieved = await context.EvaluatedHands.FirstOrDefaultAsync(e => e.Id == 1);

        // Assert - Cards should be preserved exactly
        Assert.NotNull(retrieved);
        Assert.Equal(cardsWithCommas.Length, retrieved.Cards.Length);
        Assert.Equal("Jace, the Mind Sculptor", retrieved.Cards[0]);
        Assert.Equal("Teferi, Hero of Dominaria", retrieved.Cards[1]);
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
            "Urza's Saga",
            "Serra's Sanctum",
            "_____ Goblin",
            "Déjà Vu",
            "Card: The Gathering",
            "Test//Card",
            "Card (Ver. 2)"
        };
        
        var decklist = new Decklist
        {
            Id = 1,
            Name = "Special Characters Deck",
            CardNames = specialCards
        };

        // Act - Save and retrieve
        context.Decklists.Add(decklist);
        await context.SaveChangesAsync();

        var retrieved = await context.Decklists.FirstOrDefaultAsync(d => d.Id == 1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(specialCards.Length, retrieved.CardNames.Length);
        Assert.Equal("Urza's Saga", retrieved.CardNames[0]);
        Assert.Equal("_____ Goblin", retrieved.CardNames[2]);
        Assert.Equal("Card: The Gathering", retrieved.CardNames[4]);
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
        var cards = new[] { longCardName, "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" };
        
        var evaluatedHand = new EvaluatedHand(1, cards, 1, new[] { 6, 5, 4 }, true, 3, 1, "TestDeck");

        // Act
        context.EvaluatedHands.Add(evaluatedHand);
        await context.SaveChangesAsync();

        var retrieved = await context.EvaluatedHands.FirstOrDefaultAsync(e => e.Id == 1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(longCardName, retrieved.Cards[0]);
        Assert.Equal(200, retrieved.Cards[0].Length);
    }

    [Fact]
    public async Task Decklist_Large99CardDeck_ConvertsCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EtaliDatabase(options);
        
        var cards = Enumerable.Range(1, 99).Select(i => $"Card Number {i}").ToArray();
        var decklist = new Decklist
        {
            Id = 1,
            Name = "Full Commander Deck",
            CardNames = cards
        };

        // Act
        context.Decklists.Add(decklist);
        await context.SaveChangesAsync();

        var retrieved = await context.Decklists.FirstOrDefaultAsync(d => d.Id == 1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(99, retrieved.CardNames.Length);
        Assert.Equal("Card Number 1", retrieved.CardNames[0]);
        Assert.Equal("Card Number 99", retrieved.CardNames[98]);
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
        var evaluatedHand = new EvaluatedHand(1, Array.Empty<string>(), 1, new[] { 6, 5, 4 }, false, 8, 1, "TestDeck");

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
            new[] { "C1", "C2", "C3", "C4", "C5", "C6", "C7" },
            1,
            stickers,
            true,
            4,
            1,
            "TestDeck"
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
        context.Decklists.Add(new Decklist { Id = 1, Name = "Deck1", CardNames = new[] { "C1" } });
        await context.SaveChangesAsync();

        context.EvaluatedHands.Add(new EvaluatedHand(1, new[] { "C1", "C2", "C3", "C4", "C5", "C6", "C7" }, 1, new[] { 6, 5, 4 }, true, 3, 1, "Deck1"));
        await context.SaveChangesAsync();

        context.Decklists.Add(new Decklist { Id = 2, Name = "Deck2", CardNames = new[] { "C2" } });
        await context.SaveChangesAsync();

        context.EvaluatedHands.Add(new EvaluatedHand(2, new[] { "C1", "C2", "C3", "C4", "C5", "C6", "C7" }, 2, new[] { 5, 4, 3 }, false, 8, 2, "Deck2"));
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.Decklists.CountAsync());
        Assert.Equal(2, await context.EvaluatedHands.CountAsync());
    }
}
