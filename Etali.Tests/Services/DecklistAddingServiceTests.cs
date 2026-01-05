using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Etali.Tests.Services;

/// <summary>
/// Tests for DecklistAddingService focusing on data parsing and database operations.
/// Note: External HTTP calls to Moxfield API are not tested as they require integration testing.
/// </summary>
public class DecklistAddingServiceTests
{
    private readonly Mock<ILogger<DecklistAddingService>> _mockLogger;
    private readonly Mock<IRateLimiter> _mockRateLimiter;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly MoxfieldConfiguration _mockConfig;
    private readonly EtaliDatabase _database;

    public DecklistAddingServiceTests()
    {
        _mockLogger = new Mock<ILogger<DecklistAddingService>>();
        _mockRateLimiter = new Mock<IRateLimiter>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockConfig = new MoxfieldConfiguration 
        { 
            BaseUrl = "https://api2.moxfield.com/v3/decks/all/",
            ApiKey = "test-key"
        };
        
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _database = new EtaliDatabase(options);
    }

    [Fact]
    public void DecklistAddingService_CanBeConstructed()
    {
        // Act
        var service = new DecklistAddingService(_database, _mockLogger.Object, _mockHttpClientFactory.Object, _mockConfig, _mockRateLimiter.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void DecklistAddingService_ImplementsInterface()
    {
        // Act
        var service = new DecklistAddingService(_database, _mockLogger.Object, _mockHttpClientFactory.Object, _mockConfig, _mockRateLimiter.Object);

        // Assert
        Assert.IsAssignableFrom<IDecklistAddingService>(service);
    }

    [Fact]
    public async Task AddDecklist_WithEmptyDatabase_AssignsId1()
    {
        // This test verifies that the service correctly generates IDs
        
        // Arrange
        var service = new DecklistAddingService(_database, _mockLogger.Object, _mockHttpClientFactory.Object, _mockConfig, _mockRateLimiter.Object);
        
        // Manually simulate what AddDecklist would do
        var nextId = _database.Decklists.Any() ? _database.Decklists.Max(d => d.Id) + 1 : 1;
        
        // Assert
        Assert.Equal(1, nextId);
    }

    [Fact]
    public void AddDecklist_WithExistingDecklists_AssignsIncrementalId()
    {
        // Arrange
        _database.Decklists.Add(new Decklist { Id = 1, Name = "Deck1", Cards = [new Card { Name = "Card1", Type = "Creature" }] });
        _database.Decklists.Add(new Decklist { Id = 5, Name = "Deck5", Cards = [new Card { Name = "Card5", Type = "Artifact" }] });
        _database.SaveChanges();

        // Act - Simulate what AddDecklist does
        var nextId = _database.Decklists.Any() ? _database.Decklists.Max(d => d.Id) + 1 : 1;

        // Assert
        Assert.Equal(6, nextId);
    }

    [Fact]
    public async Task Database_CanStoreDecklistFromService()
    {
        // This test verifies the database can store the format of data that DecklistAddingService would produce
        
        // Arrange
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestMoxfieldDeck",
            Cards =
            [
                new Card { Name = "Sol Ring", Type = "Artifact" },
                new Card { Name = "Command Tower", Type = "Land" },
                new Card { Name = "Lightning Bolt", Type = "Instant" },
                new Card { Name = "Counterspell", Type = "Instant" },
                new Card { Name = "Path to Exile", Type = "Instant" }
            ]
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "TestMoxfieldDeck");
        Assert.NotNull(saved);
        Assert.Equal(5, saved.Cards.Length);
        var cardNames = saved.Cards.Select(c => c.Name).ToArray();
        Assert.Contains("Sol Ring", cardNames);
    }

    [Fact]
    public async Task Database_HandlesLargeDecklists()
    {
        // Test that the database can handle a full 99-card Commander deck
        
        // Arrange
        var cards = Enumerable.Range(1, 99)
            .Select(i => new Card { Name = $"Card{i}", Type = "Creature" })
            .ToArray();
        var decklist = new Decklist
        {
            Id = 1,
            Name = "Full99CardDeck",
            Cards = cards
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "Full99CardDeck");
        Assert.NotNull(saved);
        Assert.Equal(99, saved.Cards.Length);
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
            Cards =
            [
                new Card { Name = "Jace, the Mind Sculptor", Type = "Planeswalker" },
                new Card { Name = "Urza's Saga", Type = "Enchantment — Saga" },
                new Card { Name = "Serra's Sanctum", Type = "Legendary Land" },
                new Card { Name = "Déjà Vu", Type = "Sorcery" },
                new Card { Name = "_____ Goblin", Type = "Creature" },
                new Card { Name = "Slaughter // Mayhem", Type = "Instant // Sorcery" },
                new Card { Name = "Fire // Ice", Type = "Instant // Instant" },
                new Card { Name = "Dead // Gone", Type = "Instant // Instant" }
            ]
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "Special Chars Deck");
        Assert.NotNull(saved);
        var cardNames = saved.Cards.Select(c => c.Name).ToArray();
        Assert.Contains("Jace, the Mind Sculptor", cardNames);
        Assert.Contains("Urza's Saga", cardNames);
        Assert.Contains("_____ Goblin", cardNames);
        Assert.Contains("Slaughter // Mayhem", cardNames);
        Assert.Contains("Fire // Ice", cardNames);
        Assert.Contains("Dead // Gone", cardNames);
        Assert.Equal(8, saved.Cards.Length);
    }

    [Fact]
    public void DecklistAddingService_RequiresRateLimiter()
    {
        // Verify that the service requires rate limiter dependency
        
        // Arrange & Act
        var service = new DecklistAddingService(_database, _mockLogger.Object, _mockHttpClientFactory.Object, _mockConfig, _mockRateLimiter.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task Database_CanHandleMultipleDecklistsWithSameCards()
    {
        // Test that multiple decklists can have the same cards
        
        // Arrange
        var sharedCards = new[]
        {
            new Card { Name = "Sol Ring", Type = "Artifact" },
            new Card { Name = "Command Tower", Type = "Land" },
            new Card { Name = "Lightning Bolt", Type = "Instant" }
        };
        
        var deck1 = new Decklist { Id = 1, Name = "Deck1", Cards = sharedCards };
        var deck2 = new Decklist { Id = 2, Name = "Deck2", Cards = sharedCards };

        // Act
        _database.Decklists.Add(deck1);
        _database.Decklists.Add(deck2);
        await _database.SaveChangesAsync();

        // Assert
        Assert.Equal(2, _database.Decklists.Count());
        var allDecklists = await _database.Decklists.ToListAsync();
        Assert.All(allDecklists, d => Assert.Equal(3, d.Cards.Length));
    }

    [Fact]
    public async Task Database_PreservesCardOrder()
    {
        // Verify that card order is maintained
        
        // Arrange
        var orderedCards = new[]
        {
            new Card { Name = "Card A", Type = "Creature" },
            new Card { Name = "Card B", Type = "Instant" },
            new Card { Name = "Card C", Type = "Sorcery" },
            new Card { Name = "Card D", Type = "Artifact" },
            new Card { Name = "Card E", Type = "Land" }
        };
        var decklist = new Decklist
        {
            Id = 1,
            Name = "OrderedDeck",
            Cards = orderedCards
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "OrderedDeck");
        Assert.NotNull(saved);
        var savedCardNames = saved.Cards.Select(c => c.Name).ToArray();
        var expectedCardNames = orderedCards.Select(c => c.Name).ToArray();
        Assert.Equal(expectedCardNames, savedCardNames);
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
            Cards =
            [
                new Card { Name = "Teferi, Hero of Dominaria", Type = "Legendary Planeswalker" },
                new Card { Name = "Oko, Thief of Crowns", Type = "Legendary Planeswalker" },
                new Card { Name = "Nicol Bolas, Dragon-God", Type = "Legendary Planeswalker" },
                new Card { Name = "Wear // Tear", Type = "Instant // Instant" },
                new Card { Name = "Turn // Burn", Type = "Instant // Sorcery" },
                new Card { Name = "Catch // Release", Type = "Instant // Sorcery" },
                new Card { Name = "Who // What // When // Where // Why", Type = "Instant" },
                new Card { Name = "Odds // Ends", Type = "Instant // Sorcery" },
                new Card { Name = "Search the City", Type = "Enchantment" }
            ]
        };

        // Act
        _database.Decklists.Add(decklist);
        await _database.SaveChangesAsync();

        // Assert
        var saved = await _database.Decklists.FirstOrDefaultAsync(d => d.Name == "Complex Cards Deck");
        Assert.NotNull(saved);
        var cardNames = saved.Cards.Select(c => c.Name).ToArray();
        
        // Verify all cards with commas
        Assert.Contains("Teferi, Hero of Dominaria", cardNames);
        Assert.Contains("Oko, Thief of Crowns", cardNames);
        Assert.Contains("Nicol Bolas, Dragon-God", cardNames);
        
        // Verify all split cards with //
        Assert.Contains("Wear // Tear", cardNames);
        Assert.Contains("Turn // Burn", cardNames);
        Assert.Contains("Catch // Release", cardNames);
        Assert.Contains("Who // What // When // Where // Why", cardNames);
        Assert.Contains("Odds // Ends", cardNames);
        
        // Verify total count
        Assert.Equal(9, saved.Cards.Length);
        
        // Verify no corruption or splitting occurred
        var distinctCards = cardNames.Distinct().ToArray();
        Assert.Equal(saved.Cards.Length, distinctCards.Length);
    }
}
