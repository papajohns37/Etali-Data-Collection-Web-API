using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Models;

public class DecklistTests
{
    [Fact]
    public void Decklist_Constructor_SetsProperties()
    {
        // Arrange & Act
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            CardNames = new[] { "Card1", "Card2", "Card3" }
        };

        // Assert
        Assert.Equal(1, decklist.Id);
        Assert.Equal("TestDeck", decklist.Name);
        Assert.Equal(3, decklist.CardNames.Length);
        Assert.NotEqual(default(DateTime), decklist.CreatedAt);
    }

    [Fact]
    public void Decklist_CardNames_CanBeEmpty()
    {
        // Arrange & Act
        var decklist = new Decklist
        {
            Id = 1,
            Name = "EmptyDeck",
            CardNames = Array.Empty<string>()
        };

        // Assert
        Assert.Empty(decklist.CardNames);
    }

    [Fact]
    public void Decklist_Name_CanBeSet()
    {
        // Arrange
        var decklist = new Decklist { Id = 1, Name = "Original", CardNames = Array.Empty<string>() };

        // Act
        decklist.Name = "Updated";

        // Assert
        Assert.Equal("Updated", decklist.Name);
    }

    [Fact]
    public void Decklist_CardNames_CanBeUpdated()
    {
        // Arrange
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            CardNames = new[] { "Card1", "Card2" }
        };

        // Act
        decklist.CardNames = new[] { "CardA", "CardB", "CardC" };

        // Assert
        Assert.Equal(3, decklist.CardNames.Length);
        Assert.Contains("CardA", decklist.CardNames);
    }

    [Fact]
    public void Decklist_MoxfieldUrl_CanBeSet()
    {
        // Arrange & Act
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            CardNames = new[] { "Card1" },
            MoxfieldUrl = "https://www.moxfield.com/decks/abc123"
        };

        // Assert
        Assert.Equal("https://www.moxfield.com/decks/abc123", decklist.MoxfieldUrl);
    }

    [Fact]
    public void Decklist_MoxfieldId_CanBeSet()
    {
        // Arrange & Act
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            CardNames = new[] { "Card1" },
            MoxfieldId = "abc123xyz"
        };

        // Assert
        Assert.Equal("abc123xyz", decklist.MoxfieldId);
    }

    [Fact]
    public void Decklist_MoxfieldProperties_CanBeNull()
    {
        // Arrange & Act
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            CardNames = new[] { "Card1" },
            MoxfieldUrl = null,
            MoxfieldId = null
        };

        // Assert
        Assert.Null(decklist.MoxfieldUrl);
        Assert.Null(decklist.MoxfieldId);
    }

    [Fact]
    public void Decklist_CreatedAt_DefaultsToUtcNow()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            CardNames = new[] { "Card1" }
        };

        var afterCreate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.InRange(decklist.CreatedAt, beforeCreate, afterCreate);
        Assert.Equal(DateTimeKind.Utc, decklist.CreatedAt.Kind);
    }

    [Fact]
    public void Decklist_CreatedAt_CanBeSetExplicitly()
    {
        // Arrange
        var specificTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            CardNames = new[] { "Card1" },
            CreatedAt = specificTime
        };

        // Assert
        Assert.Equal(specificTime, decklist.CreatedAt);
    }

    [Fact]
    public void Decklist_WithAllProperties_SetsCorrectly()
    {
        // Arrange
        var createdTime = DateTime.UtcNow;

        // Act
        var decklist = new Decklist
        {
            Id = 42,
            Name = "Complete Deck",
            CardNames = new[] { "Sol Ring", "Mana Vault", "Chrome Mox" },
            MoxfieldUrl = "https://www.moxfield.com/decks/xyz789",
            MoxfieldId = "xyz789",
            CreatedAt = createdTime
        };

        // Assert
        Assert.Equal(42, decklist.Id);
        Assert.Equal("Complete Deck", decklist.Name);
        Assert.Equal(3, decklist.CardNames.Length);
        Assert.Equal("https://www.moxfield.com/decks/xyz789", decklist.MoxfieldUrl);
        Assert.Equal("xyz789", decklist.MoxfieldId);
        Assert.Equal(createdTime, decklist.CreatedAt);
    }
}
