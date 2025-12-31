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
}
