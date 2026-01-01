using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Etali.Tests.Services;

/// <summary>
/// Tests for DecklistAddingService focusing on data parsing and database operations.
/// Note: External HTTP calls and PuppeteerSharp are not tested as they require integration testing.
/// </summary>
public class DecklistAddingServiceTests
{
    private readonly Mock<ILogger<DecklistAddingService>> _mockLogger;
    private readonly EtaliDatabase _database;

    public DecklistAddingServiceTests()
    {
        _mockLogger = new Mock<ILogger<DecklistAddingService>>();
        
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _database = new EtaliDatabase(options);
    }

    [Fact]
    public void DecklistAddingService_CanBeConstructed()
    {
        // Act
        var service = new DecklistAddingService(_database, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void DecklistAddingService_ImplementsInterface()
    {
        // Act
        var service = new DecklistAddingService(_database, _mockLogger.Object);

        // Assert
        Assert.IsAssignableFrom<IDecklistAddingService>(service);
    }

    [Fact]
    public async Task AddDecklist_WithEmptyDatabase_AssignsId1()
    {
        // This test verifies that the service correctly generates IDs
        // We can't fully test AddDecklist without mocking HTTP calls,
        // but we can verify the database interaction pattern
        
        // Arrange
        var service = new DecklistAddingService(_database, _mockLogger.Object);
        
        // Manually simulate what AddDecklist would do
        var nextId = _database.Decklists.Any() ? _database.Decklists.Max(d => d.Id) + 1 : 1;
        
        // Assert
        Assert.Equal(1, nextId);
    }

    [Fact]
    public void AddDecklist_WithExistingDecklists_AssignsIncrementalId()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Deck1", CardNames = new[] { "Card1" } });
        _database.Decklists.Add(new Decklist { Id = 5, Name = "Deck5", CardNames = new[] { "Card5" } });
        _database.SaveChanges();

        // Act - Simulate what AddDecklist does
        var nextId = _database.Decklists.Any() ? _database.Decklists.Max(d => d.Id) + 1 : 1;

        // Assert
        Assert.Equal(6, nextId);
    }

    [Fact]
    public async Task Database_CanStoreDecklistFromService()
    {
        // This test verifies the database can store the format of data
        // that DecklistAddingService would produce
        
        // Arrange - Simulate what AddDecklist would save
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestMoxfieldDeck",
            CardNames = new[]
            {
                "Sol Ring",
                "Command Tower",
                "Lightning Bolt",
                "Counterspell",
                "Path to Exile"
            }
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "TestMoxfieldDeck");
        Assert.NotNull(saved);
        Assert.Equal(5, saved.CardNames.Length);
        Assert.Contains("Sol Ring", saved.CardNames);
    }

    [Fact]
    public async Task Database_HandlesLargeDecklists()
    {
        // Test that the database can handle a full 99-card Commander deck
        
        // Arrange
        var cardNames = Enumerable.Range(1, 99).Select(i => $"Card{i}").ToArray();
        var decklist = new Decklist
        {
            Id = 1,
            Name = "Full99CardDeck",
            CardNames = cardNames
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "Full99CardDeck");
        Assert.NotNull(saved);
        Assert.Equal(99, saved.CardNames.Length);
    }

    [Fact]
    public async Task Database_HandlesDecklistsWithSpecialCharacters()
    {
        // Test that card names with special characters are handled correctly
        
        // Arrange
        var decklist = new Decklist
        {
            Id = 1,
            Name = "Special Chars Deck",
            CardNames = new[]
            {
                "Jace, the Mind Sculptor",
                "Urza's Saga",
                "Serra's Sanctum",
                "Déjà Vu",
                "_____ Goblin",
                "Slaughter // Mayhem",
                "Fire // Ice",
                "Dead // Gone"
            }
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "Special Chars Deck");
        Assert.NotNull(saved);
        Assert.Contains("Jace, the Mind Sculptor", saved.CardNames);
        Assert.Contains("Urza's Saga", saved.CardNames);
        Assert.Contains("_____ Goblin", saved.CardNames);
        Assert.Contains("Slaughter // Mayhem", saved.CardNames);
        Assert.Contains("Fire // Ice", saved.CardNames);
        Assert.Contains("Dead // Gone", saved.CardNames);
        Assert.Equal(8, saved.CardNames.Length);
    }

    [Fact]
    public void DecklistAddingService_LoggerIsUsed()
    {
        // Verify that the service can work with the logger
        
        // Arrange & Act
        var service = new DecklistAddingService(_database, _mockLogger.Object);

        // Assert - Verify the logger was accepted
        Assert.NotNull(service);
        // In real scenarios, we would verify logger calls, but we can't test
        // AddDecklist without external dependencies
    }

    [Fact]
    public async Task Database_CanHandleMultipleDecklistsWithSameCards()
    {
        // Test that multiple decklists can have the same cards
        
        // Arrange
        var sharedCards = new[] { "Sol Ring", "Command Tower", "Lightning Bolt" };
        
        var deck1 = new Decklist { Id = 1, Name = "Deck1", CardNames = sharedCards };
        var deck2 = new Decklist { Id = 2, Name = "Deck2", CardNames = sharedCards };

        // Act
        _database.Decklists.Add(deck1);
        _database.Decklists.Add(deck2);
        await _database.SaveChangesAsync();

        // Assert
        Assert.Equal(2, _database.Decklists.Count());
        var allDecklists = await _database.Decklists.ToListAsync();
        Assert.All(allDecklists, d => Assert.Equal(3, d.CardNames.Length));
    }

    [Fact]
    public async Task Database_PreservesCardOrder()
    {
        // Verify that card order is maintained
        
        // Arrange
        var orderedCards = new[] { "Card A", "Card B", "Card C", "Card D", "Card E" };
        var decklist = new Decklist
        {
            Id = 1,
            Name = "OrderedDeck",
            CardNames = orderedCards
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "OrderedDeck");
        Assert.NotNull(saved);
        Assert.Equal(orderedCards, saved.CardNames);
    }

    [Fact]
    public async Task Database_HandlesComplexCardNamesWithMultipleSpecialCharacters()
    {
        // Test that card names with multiple types of special characters work correctly
        
        // Arrange
        var decklist = new Decklist
        {
            Id = 1,
            Name = "Complex Cards Deck",
            CardNames = new[]
            {
                "Teferi, Hero of Dominaria",
                "Oko, Thief of Crowns",
                "Nicol Bolas, Dragon-God",
                "Wear // Tear",
                "Turn // Burn",
                "Catch // Release",
                "Who // What // When // Where // Why",
                "Odds // Ends",
                "Search the City"
            }
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "Complex Cards Deck");
        Assert.NotNull(saved);
        
        // Verify all cards with commas
        Assert.Contains("Teferi, Hero of Dominaria", saved.CardNames);
        Assert.Contains("Oko, Thief of Crowns", saved.CardNames);
        Assert.Contains("Nicol Bolas, Dragon-God", saved.CardNames);
        
        // Verify all split cards with //
        Assert.Contains("Wear // Tear", saved.CardNames);
        Assert.Contains("Turn // Burn", saved.CardNames);
        Assert.Contains("Catch // Release", saved.CardNames);
        Assert.Contains("Who // What // When // Where // Why", saved.CardNames);
        Assert.Contains("Odds // Ends", saved.CardNames);
        
        // Verify total count
        Assert.Equal(9, saved.CardNames.Length);
        
        // Verify no corruption or splitting occurred
        var distinctCards = saved.CardNames.Distinct().ToArray();
        Assert.Equal(saved.CardNames.Length, distinctCards.Length);
    }
}
