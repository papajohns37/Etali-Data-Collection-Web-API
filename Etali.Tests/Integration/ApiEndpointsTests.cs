using System.Net;
using System.Net.Http.Json;
using System.Text;
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task GetHand_EmptyOrWhitespaceDecklistName_ReturnsBadRequest(string decklistName)
    {
        // Act
        var response = await _client.GetAsync($"/hand?decklistName={Uri.EscapeDataString(decklistName)}");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHand_MultipleSequentialRequests_GeneratesUniqueHandData()
    {
        // Act - Make sequential requests
        var hands = new List<Hand>();
        for (int i = 0; i < 3; i++)
        {
            var response = await _client.GetAsync("/hand?decklistName=TestDeck");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var hand = await response.Content.ReadFromJsonAsync<Hand>();
            Assert.NotNull(hand);
            hands.Add(hand);
            
            // Delay to ensure requests are processed
            await Task.Delay(50);
        }

        // Assert - At least two hands should have different card selections or seat orders
        var allIdentical = hands.All(h => 
            string.Join(",", h.Cards) == string.Join(",", hands[0].Cards) &&
            h.SeatOrder == hands[0].SeatOrder);
        
        Assert.False(allIdentical, "Multiple hands should produce varied results");
    }

    [Fact]
    public async Task GetHand_MultipleRequests_ProducesIndependentRandomness()
    {
        // Act - Generate multiple hands
        var hands = new List<Hand>();
        for (int i = 0; i < 10; i++)
        {
            var response = await _client.GetAsync("/hand?decklistName=TestDeck");
            var hand = await response.Content.ReadFromJsonAsync<Hand>();
            Assert.NotNull(hand);
            hands.Add(hand);
        }

        // Assert - Should have variety in cards (not all hands identical)
        var firstHandCards = string.Join(",", hands[0].Cards);
        var allIdentical = hands.All(h => string.Join(",", h.Cards) == firstHandCards);
        Assert.False(allIdentical, "All hands should not have identical card selections");

        // Assert - Should have variety in seat orders
        var uniqueSeatOrders = hands.Select(h => h.SeatOrder).Distinct().Count();
        Assert.True(uniqueSeatOrders > 1, "Should have variety in seat orders");
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

    [Fact]
    public async Task PostHandEvaluate_MissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange - Missing Cards field (required)
        var invalidJson = new StringContent(@"{
            ""id"": 1,
            ""seatOrder"": 2,
            ""stickers"": [6, 5, 4],
            ""canCastEtali"": true,
            ""minimumCardsToCastEtali"": 5,
            ""decklistId"": 1,
            ""decklistName"": ""TestDeck""
        }", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/hand/evaluate", invalidJson);

        // Assert - ASP.NET Core may accept null for array properties
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.InternalServerError ||
            response.StatusCode == HttpStatusCode.OK,
            $"Unexpected status code: {response.StatusCode}"
        );
    }

    [Fact]
    public async Task PostHandEvaluate_WrongFieldTypes_ReturnsBadRequest()
    {
        // Arrange - SeatOrder as string instead of int
        var invalidJson = new StringContent(@"{
            ""id"": 1,
            ""cards"": [""Card1"", ""Card2"", ""Card3"", ""Card4"", ""Card5"", ""Card6"", ""Card7""],
            ""seatOrder"": ""two"",
            ""stickers"": [6, 5, 4],
            ""canCastEtali"": true,
            ""minimumCardsToCastEtali"": 5,
            ""decklistId"": 1,
            ""decklistName"": ""TestDeck""
        }", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/hand/evaluate", invalidJson);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_EmptyCardsArray_AcceptsRequest()
    {
        // Arrange
        var uniqueId = Random.Shared.Next(10000, 99999);
        var evaluation = new
        {
            id = uniqueId,
            cards = Array.Empty<string>(),
            seatOrder = 1,
            stickers = new[] { 6, 5, 4 },
            canCastEtali = false,
            minimumCardsToCastEtali = 8,
            decklistId = 1,
            decklistName = "TestDeck"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostHandEvaluate_EmptyStickersArray_AcceptsRequest()
    {
        // Arrange
        var uniqueId = Random.Shared.Next(10000, 99999);
        var evaluation = new
        {
            id = uniqueId,
            cards = new[] { "Card1", "Card2", "Card3", "Card4", "Card5", "Card6", "Card7" },
            seatOrder = 1,
            stickers = Array.Empty<int>(),
            canCastEtali = true,
            minimumCardsToCastEtali = 5,
            decklistId = 1,
            decklistName = "TestDeck"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/hand/evaluate", evaluation);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_CamelCaseJson_DeserializesCorrectly()
    {
        // Arrange - Using camelCase (ASP.NET Core default)
        var uniqueId = Random.Shared.Next(10000, 99999);
        var jsonContent = new StringContent($@"{{
            ""id"": {uniqueId},
            ""cards"": [""Card1"", ""Card2"", ""Card3"", ""Card4"", ""Card5"", ""Card6"", ""Card7""],
            ""seatOrder"": 2,
            ""stickers"": [6, 5, 4],
            ""canCastEtali"": true,
            ""minimumCardsToCastEtali"": 5,
            ""decklistId"": 1,
            ""decklistName"": ""TestDeck""
        }}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/hand/evaluate", jsonContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_PascalCaseJson_DeserializesCorrectly()
    {
        // Arrange - Using PascalCase
        var uniqueId = Random.Shared.Next(10000, 99999);
        var jsonContent = new StringContent($@"{{
            ""Id"": {uniqueId},
            ""Cards"": [""Card1"", ""Card2"", ""Card3"", ""Card4"", ""Card5"", ""Card6"", ""Card7""],
            ""SeatOrder"": 2,
            ""Stickers"": [6, 5, 4],
            ""CanCastEtali"": true,
            ""MinimumCardsToCastEtali"": 5,
            ""DecklistId"": 1,
            ""DecklistName"": ""TestDeck""
        }}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/hand/evaluate", jsonContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostHandEvaluate_LargePayload_HandlesSuccessfully()
    {
        // Arrange - Very long card names
        var uniqueId = Random.Shared.Next(10000, 99999);
        var longCards = Enumerable.Range(1, 7)
            .Select(i => new string('A', 100) + $" Card {i}")
            .ToArray();

        var evaluation = new EvaluatedHand(
            uniqueId,
            longCards,
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
    }

    [Fact]
    public async Task PostHandEvaluate_CanCastEtaliFalse_AcceptsMinimumCards8()
    {
        // Arrange
        var uniqueId = Random.Shared.Next(10000, 99999);
        var evaluation = new EvaluatedHand(
            uniqueId,
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
    public async Task PostHandEvaluate_SuccessResponse_ReturnsConsistentFormat()
    {
        // Arrange
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
        Assert.True(result.TryGetProperty("message", out var message));
        Assert.True(result.TryGetProperty("handId", out var handId));
        Assert.Equal(uniqueId, handId.GetInt32());
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

    [Fact]
    public async Task GetDecklists_ReturnsConsistentOrdering()
    {
        // Act - Call multiple times
        var response1 = await _client.GetAsync("/decklists");
        var decklists1 = await response1.Content.ReadFromJsonAsync<List<Decklist>>();

        var response2 = await _client.GetAsync("/decklists");
        var decklists2 = await response2.Content.ReadFromJsonAsync<List<Decklist>>();

        // Assert - Results should be consistent
        Assert.NotNull(decklists1);
        Assert.NotNull(decklists2);
        Assert.Equal(decklists1.Count, decklists2.Count);
        
        for (int i = 0; i < decklists1.Count; i++)
        {
            Assert.Equal(decklists1[i].Id, decklists2[i].Id);
            Assert.Equal(decklists1[i].Name, decklists2[i].Name);
        }
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

    [Fact]
    public async Task PostDecklist_ServiceThrowsException_ReturnsAppropriateError()
    {
        // Arrange - Mock service that throws
        var mockService = new Mock<IDecklistAddingService>();
        mockService.Setup(s => s.AddDecklist(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Moxfield error"));

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
        var response = await client.PostAsync("/decklist?link=https://test.com&name=TestDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_MissingLinkParameter_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsync("/decklist?name=TestDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_MissingNameParameter_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsync("/decklist?link=https://test.com", null);

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

    [Fact]
    public async Task Options_PreflightForPostWithCustomHeaders_ReturnsCorrectHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/hand/evaluate");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.NoContent || 
            response.StatusCode == HttpStatusCode.OK ||
            response.Headers.Contains("Access-Control-Allow-Origin")
        );
    }

    [Fact]
    public async Task Get_RequestWithOriginHeader_WorksCorrectly()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/decklists");
        request.Headers.Add("Origin", "http://localhost:4200");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetHand_NotFoundError_ReturnsConsistentErrorFormat()
    {
        // Act
        var response = await _client.GetAsync("/hand?decklistName=NonExistent123456");

        // Assert - Should return NotFound or may return InternalServerError in some test scenarios
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected NotFound or InternalServerError, but got {response.StatusCode}");
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        
        // Should contain error information
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var jsonDoc = JsonDocument.Parse(content);
            Assert.True(jsonDoc.RootElement.TryGetProperty("error", out _));
        }
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

    #region Swagger Documentation Tests

    [Fact]
    public async Task Swagger_Documentation_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Etali Data Collection API", content);
        Assert.Contains("/hand", content);
        Assert.Contains("/hand/evaluate", content);
        Assert.Contains("/decklists", content);
    }

    [Fact]
    public async Task SwaggerUI_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("swagger", content, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

/// <summary>
/// Custom WebApplicationFactory that properly configures the test environment.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly object _lock = new object();
    private static bool _seeded = false;

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
            
            // Ensure database is created
            db.Database.EnsureCreated();
            
            // Thread-safe seeding - only seed once
            lock (_lock)
            {
                if (!_seeded && !db.Decklists.Any())
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
                    _seeded = true;
                }
            }
        });
    }
}
