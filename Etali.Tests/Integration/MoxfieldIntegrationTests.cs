using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration test that makes actual HTTP calls to Moxfield API.
/// These tests require internet connectivity and a configured Moxfield API key.
/// Mark with [Trait("Category", "External")] to exclude from CI/CD pipelines.
/// </summary>
[Trait("Category", "External")]
public class MoxfieldIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MoxfieldIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Integration test that fetches a real decklist from Moxfield.
    /// This test verifies the complete flow of fetching, parsing, and storing a decklist.
    /// </summary>
    [Fact]
    public async Task AddDecklist_FetchesRealMoxfieldDeck_Has99MainboardCardsAndKnownCards()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<DecklistAddingService>();
        var moxfieldConfig = scope.ServiceProvider.GetRequiredService<MoxfieldConfiguration>();
        var rateLimiter = scope.ServiceProvider.GetRequiredService<IRateLimiter>();

        var service = new DecklistAddingService(database, logger, moxfieldConfig, rateLimiter);

        var moxfieldUrl = "https://moxfield.com/decks/JhWK-MfmY0W1V5feboKLxQ";
        var decklistName = "Moxfield Integration Test Deck";

        // Known cards that should be in this decklist
        var expectedCardNames = new[] { "Treasonous Ogre", "Orcish Lumberjack", "Mana Vault", "Ancient Tomb" };

        var beforeAdd = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await service.AddDecklist(moxfieldUrl, decklistName);

        var afterAdd = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var savedDecklist = await database.Decklists.FirstOrDefaultAsync(d => d.Name == decklistName);
        Assert.NotNull(savedDecklist);

        // Verify 99 cards
        Assert.Equal(99, savedDecklist.Cards.Length);

        // Verify known cards are present in the mainboard
        var cardNames = savedDecklist.Cards.Select(c => c.Name).ToArray();
        foreach (var expectedCardName in expectedCardNames)
        {
            Assert.Contains(expectedCardName, cardNames);
        }

        // Verify no duplicate cards (all unique)
        var distinctCards = savedDecklist.Cards.Select(c => c.Name).Distinct().ToArray();
        Assert.Equal(savedDecklist.Cards.Length, distinctCards.Length);

        // Verify the decklist was assigned a valid ID
        Assert.True(savedDecklist.Id > 0);

        // Verify Moxfield metadata was captured
        Assert.NotNull(savedDecklist.MoxfieldUrl);
        Assert.NotNull(savedDecklist.MoxfieldId);
        Assert.Contains("moxfield.com", savedDecklist.MoxfieldUrl);
        Assert.Equal("JhWK-MfmY0W1V5feboKLxQ", savedDecklist.MoxfieldId);

        // Verify CreatedAt timestamp is set and within reasonable range
        Assert.NotEqual(default(DateTime), savedDecklist.CreatedAt);
        Assert.InRange(savedDecklist.CreatedAt, beforeAdd, afterAdd);
        Assert.Equal(DateTimeKind.Utc, savedDecklist.CreatedAt.Kind);

        // Verify cards have types populated
        Assert.All(savedDecklist.Cards, card =>
        {
            Assert.NotNull(card.Name);
            Assert.NotNull(card.Type);
            Assert.NotEmpty(card.Name);
            Assert.NotEmpty(card.Type);
        });
    }
}
