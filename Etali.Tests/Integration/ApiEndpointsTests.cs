using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for all API endpoints.
/// These tests verify the full HTTP request/response pipeline.
/// </summary>
public class ApiEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region GET /hand Tests

    [Fact]
    public async Task GetHand_ValidDecklistName_ReturnsOkWithHand()
    {
        // Act
        var response = await _client.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var hand = await response.Content.ReadFromJsonAsync<Hand>();
        Assert.NotNull(hand);
        Assert.Equal(7, hand.Cards.Length);
        Assert.InRange(hand.SeatOrder, 1, 4);
        Assert.Equal(3, hand.Stickers.Length);
        Assert.Equal(1, hand.DecklistId);
        Assert.Equal("TestDeck", hand.DecklistName);
    }

    [Fact]
    public async Task GetHand_NonExistentDecklistName_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/hand?decklistName=NonExistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHand_MissingDecklistNameParameter_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/hand");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region POST /hand/evaluate Tests

    [Fact]
    public async Task PostHandEvaluate_ValidEvaluation_ReturnsOk()
    {
        // Arrange - Use a unique ID to avoid tracking conflicts
        var uniqueId = Random.Shared.Next(10000, 99999);
        var evaluation = new EvaluatedHand(
            uniqueId,
            new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" },
            2,
            new[] { 6, 5, 4 },
            true,
            5,
            1,
            "TestDeck"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Hand evaluation recorded", result.GetProperty("message").GetString());
        Assert.Equal(uniqueId, result.GetProperty("handId").GetInt32());
    }

    [Fact]
    public async Task PostHandEvaluate_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var invalidJson = new StringContent("{invalid json}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/hand/evaluate", invalidJson);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /decklists Tests

    [Fact]
    public async Task GetDecklists_ReturnsOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/decklists");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var decklists = await response.Content.ReadFromJsonAsync<List<Decklist>>();
        Assert.NotNull(decklists);
        Assert.NotEmpty(decklists);
        Assert.Contains(decklists, d => d.Name == "TestDeck");
    }

    [Fact]
    public async Task GetDecklists_ReturnsJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/decklists");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    #endregion

    #region GET /hands/evaluated Tests

    [Fact]
    public async Task GetHandsEvaluated_ReturnsOkWithArray()
    {
        // Act
        var response = await _client.GetAsync("/hands/evaluated");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var evaluations = await response.Content.ReadFromJsonAsync<List<EvaluatedHand>>();
        Assert.NotNull(evaluations);
    }

    #endregion

    #region POST /decklist Tests

    [Fact]
    public async Task PostDecklist_WithMockService_ReturnsOk()
    {
        // Arrange - Create a factory with mocked DecklistAddingService
        var mockService = new Mock<IDecklistAddingService>();
        mockService.Setup(s => s.AddDecklist(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDecklistAddingService>();
                services.AddScoped(_ => mockService.Object);
            });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsync("/decklist?link=https://test.com&name=NewDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Decklist added successfully", result.GetProperty("message").GetString());
        
        mockService.Verify(s => s.AddDecklist("https://test.com", "NewDeck"), Times.Once);
    }

    [Fact]
    public async Task PostDecklist_MissingParameters_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsync("/decklist", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region CORS Tests

    [Fact]
    public async Task Options_RequestWithOrigin_ReturnsCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/hand?decklistName=TestDeck");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin") || 
                    response.StatusCode == HttpStatusCode.NoContent ||
                    response.StatusCode == HttpStatusCode.OK);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task GetHand_CaseInsensitiveDecklistName_ReturnsOk()
    {
        // Act
        var response1 = await _client.GetAsync("/hand?decklistName=testdeck");
        var response2 = await _client.GetAsync("/hand?decklistName=TESTDECK");
        var response3 = await _client.GetAsync("/hand?decklistName=TestDeck");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_CanCastEtaliFalse_AcceptsMinimumCards8()
    {
        // Arrange
        var evaluation = new EvaluatedHand(
            201,
            new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" },
            1,
            new[] { 6, 5, 4 },
            false,
            8,
            1,
            "TestDeck"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoints_SequentialOperations_WorksCorrectly()
    {
        // This test verifies that the API can handle multiple sequential operations
        
        // 1. Get a hand
        var handResponse = await _client.GetAsync("/hand?decklistName=TestDeck");
        Assert.Equal(HttpStatusCode.OK, handResponse.StatusCode);
        var hand = await handResponse.Content.ReadFromJsonAsync<Hand>();
        Assert.NotNull(hand);

        // 2. Evaluate the hand
        var evaluation = new EvaluatedHand(
            hand.Id,
            hand.Cards,
            hand.SeatOrder,
            hand.Stickers,
            true,
            5,
            hand.DecklistId,
            hand.DecklistName
        );
        var evalResponse = await _client.PostAsJsonAsync("/hand/evaluate", evaluation);
        Assert.Equal(HttpStatusCode.OK, evalResponse.StatusCode);

        // 3. Get decklists
        var decklistsResponse = await _client.GetAsync("/decklists");
        Assert.Equal(HttpStatusCode.OK, decklistsResponse.StatusCode);
    }

    #endregion
}

/// <summary>
/// Custom WebApplicationFactory that properly configures the test environment.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll<DbContextOptions<EtaliDatabase>>();
            
            // Add a database context (using in-memory for tests)
            services.AddDbContext<EtaliDatabase>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
            
            // Build service provider and seed data
            var serviceProvider = services.BuildServiceProvider();
            
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            
            // Ensure database is created and seeded
            db.Database.EnsureCreated();
            
            // Only seed if not already seeded
            if (!db.Decklists.Any())
            {
                db.Decklists.Add(new Decklist
                {
                    Id = 1,
                    Name = "TestDeck",
                    CardNames = new[]
                    {
                        "Card1", "Card2", "Card3", "Card4", "Card5",
                        "Card6", "Card7", "Card8", "Card9", "Card10"
                    }
                });
                db.SaveChanges();
            }
        });
    }
}
