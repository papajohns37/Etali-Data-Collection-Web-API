using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Etali.Tests.Services;

public class HandEvaluationServiceTests
{
    private readonly EtaliDatabase _database;
    private readonly HandEvaluationService _service;

    public HandEvaluationServiceTests()
    {
        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _database = new EtaliDatabase(options);
        _service = new HandEvaluationService(_database);
    }

    [Fact]
    public void RecordEvaluation_ValidEvaluation_AddsToDatabase()
    {
        // Arrange
        var evaluation = CreateTestEvaluation(1);

        // Act
        _service.RecordEvaluation(evaluation);

        // Assert
        var saved = _database.EvaluatedHands.FirstOrDefault(e => e.Id == 1);
        Assert.NotNull(saved);
        Assert.Equal(1, saved.Id);
        Assert.Equal("TestDeck", saved.DecklistName);
    }

    [Fact]
    public async Task GetAllEvaluations_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var evaluations = await _service.GetAllEvaluations();

        // Assert
        Assert.NotNull(evaluations);
        Assert.Empty(evaluations);
    }

    [Fact]
    public async Task GetAllEvaluations_WithMultipleEvaluations_ReturnsAll()
    {
        // Arrange
        _service.RecordEvaluation(CreateTestEvaluation(1));
        _service.RecordEvaluation(CreateTestEvaluation(2));
        _service.RecordEvaluation(CreateTestEvaluation(3));

        // Act
        var evaluations = await _service.GetAllEvaluations();

        // Assert
        Assert.Equal(3, evaluations.Count);
    }

    [Fact]
    public void GetNextHandId_EmptyDatabase_Returns1()
    {
        // Act
        var nextId = _service.GetNextHandId();

        // Assert
        Assert.Equal(1, nextId);
    }

    [Fact]
    public void GetNextHandId_WithExistingHands_ReturnsMaxPlusOne()
    {
        // Arrange
        _service.RecordEvaluation(CreateTestEvaluation(1));
        _service.RecordEvaluation(CreateTestEvaluation(5));
        _service.RecordEvaluation(CreateTestEvaluation(3));

        // Act
        var nextId = _service.GetNextHandId();

        // Assert
        Assert.Equal(6, nextId); // Max is 5, so next should be 6
    }

    [Fact]
    public void RecordEvaluation_MultipleEvaluations_MaintainsAllData()
    {
        // Arrange
        var eval1 = new EvaluatedHand(
            1,
            new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" },
            2,
            new[] { 6, 5, 4 },
            true,
            3,
            1,
            "Deck1"
        );
        var eval2 = new EvaluatedHand(
            2,
            new[] { "CardA", "CardB", "CardC", "CardD", "CardE", "CardF", "CardG" },
            4,
            new[] { 4, 4, 3 },
            false,
            8,
            2,
            "Deck2"
        );

        // Act
        _service.RecordEvaluation(eval1);
        _service.RecordEvaluation(eval2);

        // Assert
        var saved1 = _database.EvaluatedHands.FirstOrDefault(e => e.Id == 1);
        var saved2 = _database.EvaluatedHands.FirstOrDefault(e => e.Id == 2);
        
        Assert.NotNull(saved1);
        Assert.True(saved1.CanCastEtali);
        Assert.Equal(3, saved1.MinimumCardsToCastEtali);
        Assert.Equal(2, saved1.SeatOrder);
        
        Assert.NotNull(saved2);
        Assert.False(saved2.CanCastEtali);
        Assert.Equal(8, saved2.MinimumCardsToCastEtali);
        Assert.Equal(4, saved2.SeatOrder);
    }

    [Fact]
    public async Task GetNextHandId_Sequential_GeneratesCorrectIds()
    {
        // Note: DbContext is not thread-safe by design
        // In production, each request gets its own scoped DbContext instance
        
        // Arrange - Pre-populate some evaluations
        for (int i = 1; i <= 5; i++)
        {
            _service.RecordEvaluation(CreateTestEvaluation(i));
        }

        // Act - Sequential calls
        var results = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            results.Add(_service.GetNextHandId());
        }

        // Assert - All IDs should be at least 6 (max existing is 5)
        Assert.All(results, id => Assert.True(id >= 6));
        
        // The first call should return 6
        Assert.Equal(6, results[0]);
    }

    [Fact]
    public void RecordEvaluation_DuplicateId_ThrowsException()
    {
        // Arrange
        var evaluation1 = new EvaluatedHand(1, new[] { "C1", "C2", "C3", "C4", "C5", "C6", "C7" }, 1, new[] { 6, 5, 4 }, true, 3, 1, "Deck1");
        var evaluation2 = new EvaluatedHand(1, new[] { "D1", "D2", "D3", "D4", "D5", "D6", "D7" }, 2, new[] { 5, 4, 3 }, false, 8, 1, "Deck1");

        // Act
        _service.RecordEvaluation(evaluation1);
        var exception = Record.Exception(() => _service.RecordEvaluation(evaluation2));

        // Assert - Should throw due to duplicate primary key
        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void RecordEvaluation_CanCastEtaliFalse_AcceptsMinimumCards8()
    {
        // Arrange
        var evaluation = new EvaluatedHand(
            1,
            new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" },
            2,
            new[] { 6, 5, 4 },
            false,
            8,
            1,
            "TestDeck"
        );

        // Act
        _service.RecordEvaluation(evaluation);

        // Assert
        var saved = _database.EvaluatedHands.FirstOrDefault(e => e.Id == 1);
        Assert.NotNull(saved);
        Assert.False(saved.CanCastEtali);
        Assert.Equal(8, saved.MinimumCardsToCastEtali);
    }

    [Fact]
    public void RecordEvaluation_CanCastEtaliTrue_AllowsVariousMinimumCards()
    {
        // Act & Assert - Test various valid values when CanCastEtali is true
        for (int minCards = 1; minCards <= 7; minCards++)
        {
            var evaluation = new EvaluatedHand(
                minCards,
                new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" },
                1,
                new[] { 6, 5, 4 },
                true,
                minCards,
                1,
                "TestDeck"
            );

            _service.RecordEvaluation(evaluation);

            var saved = _database.EvaluatedHands.FirstOrDefault(e => e.Id == minCards);
            Assert.NotNull(saved);
            Assert.True(saved.CanCastEtali);
            Assert.Equal(minCards, saved.MinimumCardsToCastEtali);
        }
    }

    [Fact]
    public async Task GetAllEvaluations_WithManyEvaluations_ReturnsAll()
    {
        // Arrange - Add 50 evaluations
        for (int i = 1; i <= 50; i++)
        {
            _service.RecordEvaluation(CreateTestEvaluation(i));
        }

        // Act
        var evaluations = await _service.GetAllEvaluations();

        // Assert
        Assert.Equal(50, evaluations.Count);
        Assert.All(evaluations, e => Assert.InRange(e.Id, 1, 50));
    }

    [Fact]
    public void GetNextHandId_AfterMultipleAdds_ReturnsCorrectNextId()
    {
        // Arrange - Add evaluations with non-sequential IDs
        _service.RecordEvaluation(CreateTestEvaluation(1));
        _service.RecordEvaluation(CreateTestEvaluation(10));
        _service.RecordEvaluation(CreateTestEvaluation(5));
        _service.RecordEvaluation(CreateTestEvaluation(25));

        // Act
        var nextId = _service.GetNextHandId();

        // Assert - Should return max + 1 = 26
        Assert.Equal(26, nextId);
    }

    [Fact]
    public void RecordEvaluation_PreservesAllFieldsAccurately()
    {
        // Arrange
        var cards = new[] { "Sol Ring", "Mana Vault", "Ancient Tomb", "City of Traitors", "Lotus Petal", "Chrome Mox", "Mox Diamond" };
        var stickers = new[] { 6, 5, 4 };
        var evaluation = new EvaluatedHand(999, cards, 3, stickers, true, 4, 10, "SpecialDeck");

        // Act
        _service.RecordEvaluation(evaluation);

        // Assert
        var saved = _database.EvaluatedHands.FirstOrDefault(e => e.Id == 999);
        Assert.NotNull(saved);
        Assert.Equal(cards, saved.Cards);
        Assert.Equal(stickers, saved.Stickers);
        Assert.Equal(3, saved.SeatOrder);
        Assert.True(saved.CanCastEtali);
        Assert.Equal(4, saved.MinimumCardsToCastEtali);
        Assert.Equal(10, saved.DecklistId);
        Assert.Equal("SpecialDeck", saved.DecklistName);
    }

    private EvaluatedHand CreateTestEvaluation(int id)
    {
        return new EvaluatedHand(
            id,
            new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" },
            1,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck"
        );
    }
}
