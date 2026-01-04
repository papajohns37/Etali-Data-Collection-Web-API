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
            Cards = new[]
            {
                new Card { Name = "Card1", Type = "Creature" },
                new Card { Name = "Card2", Type = "Instant" },
                new Card { Name = "Card3", Type = "Land" }
            }
        };

        // Assert
        Assert.Equal(1, decklist.Id);
        Assert.Equal("TestDeck", decklist.Name);
        Assert.Equal(3, decklist.Cards.Length);
        Assert.NotEqual(default(DateTime), decklist.CreatedAt);
    }

    [Fact]
    public void Decklist_Cards_CanBeEmpty()
    {
        // Arrange & Act
        var decklist = new Decklist
        {
            Id = 1,
            Name = "EmptyDeck",
            Cards = Array.Empty<Card>()
        };

        // Assert
        Assert.Empty(decklist.Cards);
    }

    [Fact]
    public void Decklist_Name_CanBeSet()
    {
        // Arrange
        var decklist = new Decklist { Id = 1, Name = "Original", Cards = Array.Empty<Card>() };

        // Act
        decklist.Name = "Updated";

        // Assert
        Assert.Equal("Updated", decklist.Name);
    }

    [Fact]
    public void Decklist_Cards_CanBeUpdated()
    {
        // Arrange
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
        decklist.Cards = new[]
        {
            new Card { Name = "CardA", Type = "Artifact" },
            new Card { Name = "CardB", Type = "Enchantment" },
            new Card { Name = "CardC", Type = "Land" }
        };

        // Assert
        Assert.Equal(3, decklist.Cards.Length);
        Assert.Contains(decklist.Cards, c => c.Name == "CardA");
    }

    [Fact]
    public void Decklist_MoxfieldUrl_CanBeSet()
    {
        // Arrange & Act
        var decklist = new Decklist
        {
            Id = 1,
            Name = "TestDeck",
            Cards = new[] { new Card { Name = "Card1", Type = "Creature" } },
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
            Cards = new[] { new Card { Name = "Card1", Type = "Creature" } },
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
            Cards = new[] { new Card { Name = "Card1", Type = "Creature" } },
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
            Cards = new[] { new Card { Name = "Card1", Type = "Creature" } }
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
            Cards = new[] { new Card { Name = "Card1", Type = "Creature" } },
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
            Cards = new[]
            {
                new Card { Name = "Sol Ring", Type = "Artifact" },
                new Card { Name = "Mana Vault", Type = "Artifact" },
                new Card { Name = "Chrome Mox", Type = "Artifact" }
            },
            MoxfieldUrl = "https://www.moxfield.com/decks/xyz789",
            MoxfieldId = "xyz789",
            CreatedAt = createdTime
        };

        // Assert
        Assert.Equal(42, decklist.Id);
        Assert.Equal("Complete Deck", decklist.Name);
        Assert.Equal(3, decklist.Cards.Length);
        Assert.Equal("https://www.moxfield.com/decks/xyz789", decklist.MoxfieldUrl);
        Assert.Equal("xyz789", decklist.MoxfieldId);
        Assert.Equal(createdTime, decklist.CreatedAt);
    }
}
