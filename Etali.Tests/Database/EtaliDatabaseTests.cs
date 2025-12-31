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
}
