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
