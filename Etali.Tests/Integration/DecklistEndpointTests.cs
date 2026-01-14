using System.Net;
using System.Net.Http.Json;
using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Etali.Tests.Integration.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for POST /decklist endpoint.
/// </summary>
[Trait("Category", "Integration")]
public class DecklistEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DecklistEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region POST /decklist Tests

    [Fact]
    public async Task PostDecklist_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/decklist?link=https://test.com&name=NewDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_WithMockService_ReturnsOk()
    {
        // Arrange - Ensure database is seeded first
        await _factory.CreateAuthenticatedClientAsync(); // This seeds the database
        
        // Create a factory with mocked DecklistAddingService
        var mockService = new Mock<IDecklistAddingService>();
        mockService.Setup(s => s.AddDecklist(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var factory = _factory.WithMockedServices(services =>
        {
            services.RemoveAll<IDecklistAddingService>();
            services.AddScoped(_ => mockService.Object);
        });

        // Create authenticated client from modified factory
        var client = factory.CreateClient();
        await client.AuthenticateAs(_factory, CustomWebApplicationFactory.TestUsername);

        // Act
        var response = await client.PostAsync("/decklist?link=https://test.com&name=NewDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Decklist added successfully", result.GetProperty("message").GetString());

        mockService.Verify(s => s.AddDecklist("https://test.com", "NewDeck", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task PostDecklist_MissingParameters_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync("/decklist", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange - Ensure database is seeded first
        await _factory.CreateAuthenticatedClientAsync(); // This seeds the database
        
        // Mock service that throws
        var mockService = new Mock<IDecklistAddingService>();
        mockService.Setup(s => s.AddDecklist(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("Moxfield error"));

        var factory = _factory.WithMockedServices(services =>
        {
            services.RemoveAll<IDecklistAddingService>();
            services.AddScoped(_ => mockService.Object);
        });

        // Create authenticated client from modified factory
        var client = factory.CreateClient();
        await client.AuthenticateAs(_factory, CustomWebApplicationFactory.TestUsername);

        // Act
        var response = await client.PostAsync("/decklist?link=https://test.com&name=TestDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_MissingLinkParameter_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync("/decklist?name=TestDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_MissingNameParameter_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync("/decklist?link=https://test.com", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PATCH /decklist/{id}/rename Tests

    [Fact]
    public async Task RenameDecklist_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PatchAsync("/decklist/1/rename?newName=NewName", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RenameDecklist_NonExistentDecklist_ReturnsNotFound()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PatchAsync("/decklist/99999/rename?newName=NewName", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RenameDecklist_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PatchAsync("/decklist/1/rename?newName=", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RenameDecklist_NameTooLong_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();
        var longName = new string('A', 501); // Exceeds 500 character limit

        // Act
        var response = await client.PatchAsync($"/decklist/1/rename?newName={longName}", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RenameDecklist_AsAdmin_AnyDecklist_ReturnsOk()
    {
        // Arrange - Get test database and create a decklist
        var testDeck = new Decklist
        {
            Id = 9999,
            Name = "Original Name",
            Cards = new[] { new Card { Name = "Sol Ring", Type = "Artifact" } },
            CreatedByUserId = "different-user-id"
        };
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            db.Decklists.Add(testDeck);
            await db.SaveChangesAsync();
        }

        // Act as admin
        var adminClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);
        var response = await adminClient.PatchAsync("/decklist/9999/rename?newName=Admin Updated Name", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Decklist renamed successfully", result.GetProperty("message").GetString());
        Assert.Equal("Admin Updated Name", result.GetProperty("newName").GetString());
        Assert.Equal("Original Name", result.GetProperty("oldName").GetString());

        // Verify database was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            var updatedDeck = await db.Decklists.FindAsync(9999);
            Assert.NotNull(updatedDeck);
            Assert.Equal("Admin Updated Name", updatedDeck.Name);
        }
    }

    [Fact]
    public async Task RenameDecklist_AsCreator_OwnDecklist_ReturnsOk()
    {
        // Arrange - Get the contributor user ID first
        var userId = await _factory.GetUserIdAsync(CustomWebApplicationFactory.TestUsername);
        
        var testDeck = new Decklist
        {
            Id = 8888,
            Name = "My Deck",
            Cards = new[] { new Card { Name = "Sol Ring", Type = "Artifact" } },
            CreatedByUserId = userId
        };
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            db.Decklists.Add(testDeck);
            await db.SaveChangesAsync();
        }

        // Get authenticated client
        var contributorClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: false);

        // Act
        var response = await contributorClient.PatchAsync("/decklist/8888/rename?newName=My Updated Deck", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Decklist renamed successfully", result.GetProperty("message").GetString());
        Assert.Equal("My Updated Deck", result.GetProperty("newName").GetString());

        // Verify database was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            var updatedDeck = await db.Decklists.FindAsync(8888);
            Assert.NotNull(updatedDeck);
            Assert.Equal("My Updated Deck", updatedDeck.Name);
        }
    }

    [Fact]
    public async Task RenameDecklist_AsContributor_OtherUserDecklist_ReturnsUnauthorized()
    {
        // Arrange - Get test database and create a decklist
        var testDeck = new Decklist
        {
            Id = 7777,
            Name = "Other User's Deck",
            Cards = new[] { new Card { Name = "Sol Ring", Type = "Artifact" } },
            CreatedByUserId = "different-user-id"
        };
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            db.Decklists.Add(testDeck);
            await db.SaveChangesAsync();
        }

        // Act as contributor
        var contributorClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: false);
        var response = await contributorClient.PatchAsync("/decklist/7777/rename?newName=Hacked Name", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Verify database was NOT updated
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            var unchangedDeck = await db.Decklists.FindAsync(7777);
            Assert.NotNull(unchangedDeck);
            Assert.Equal("Other User's Deck", unchangedDeck.Name);
        }
    }

    [Fact]
    public async Task RenameDecklist_AsContributor_LegacyDecklist_ReturnsUnauthorized()
    {
        // Arrange - Get test database and create a legacy decklist (no creator)
        var testDeck = new Decklist
        {
            Id = 6666,
            Name = "Legacy Deck",
            Cards = new[] { new Card { Name = "Sol Ring", Type = "Artifact" } },
            CreatedByUserId = null
        };
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            db.Decklists.Add(testDeck);
            await db.SaveChangesAsync();
        }

        // Act as contributor
        var contributorClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: false);
        var response = await contributorClient.PatchAsync("/decklist/6666/rename?newName=Updated Legacy", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Verify database was NOT updated
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            var unchangedDeck = await db.Decklists.FindAsync(6666);
            Assert.NotNull(unchangedDeck);
            Assert.Equal("Legacy Deck", unchangedDeck.Name);
        }
    }

    [Fact]
    public async Task RenameDecklist_AsAdmin_LegacyDecklist_ReturnsOk()
    {
        // Arrange - Get test database and create a legacy decklist (no creator)
        var testDeck = new Decklist
        {
            Id = 5555,
            Name = "Legacy Deck",
            Cards = new[] { new Card { Name = "Sol Ring", Type = "Artifact" } },
            CreatedByUserId = null
        };
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            db.Decklists.Add(testDeck);
            await db.SaveChangesAsync();
        }

        // Act as admin
        var adminClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: true);
        var response = await adminClient.PatchAsync("/decklist/5555/rename?newName=Admin Updated Legacy", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify database was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            var updatedDeck = await db.Decklists.FindAsync(5555);
            Assert.NotNull(updatedDeck);
            Assert.Equal("Admin Updated Legacy", updatedDeck.Name);
        }
    }

    [Fact]
    public async Task RenameDecklist_UpdatesAllRelatedData()
    {
        // Arrange - Create a complete test scenario with all related data
        var userId = await _factory.GetUserIdAsync(CustomWebApplicationFactory.TestUsername);
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();
            
            // Create test decklist
            var testDeck = new Decklist
            {
                Id = 4444,
                Name = "Original Deck Name",
                Cards = new[] 
                { 
                    new Card { Name = "Sol Ring", Type = "Artifact" },
                    new Card { Name = "Mana Vault", Type = "Artifact" }
                },
                CreatedByUserId = userId
            };
            db.Decklists.Add(testDeck);

            // Create another deck for comparisons
            var otherDeck = new Decklist
            {
                Id = 4445,
                Name = "Other Deck",
                Cards = new[] { new Card { Name = "Worn Powerstone", Type = "Artifact" } },
                CreatedByUserId = userId
            };
            db.Decklists.Add(otherDeck);

            // Create EvaluatedHand referencing the test deck
            var evaluatedHand = new EvaluatedHand(
                Id: 9001,
                Cards: testDeck.Cards,
                SeatOrder: 1,
                Stickers: new[] { 6, 5, 4 },
                CanCastEtali: true,
                MinimumCardsToCastEtali: 3,
                DecklistId: 4444,
                DecklistName: "Original Deck Name",
                LowestGambleOdds: 0.75f,
                CreatedByUserId: userId
            );
            db.EvaluatedHands.Add(evaluatedHand);

            // Create DeckComparison with test deck as Deck1
            var comparison1 = new DeckComparison
            {
                Id = 7001,
                Deck1Id = 4444,
                Deck1Name = "Original Deck Name",
                Deck2Id = 4445,
                Deck2Name = "Other Deck"
            };
            db.DeckComparisons.Add(comparison1);

            // Create DeckComparison with test deck as Deck2
            var comparison2 = new DeckComparison
            {
                Id = 7002,
                Deck1Id = 4445,
                Deck1Name = "Other Deck",
                Deck2Id = 4444,
                Deck2Name = "Original Deck Name"
            };
            db.DeckComparisons.Add(comparison2);

            // Create ComparisonEvaluatedHand
            var comparisonHand = new ComparisonEvaluatedHand(
                Id: 9002,
                ComparisonId: 7001,
                WhichDeck: 1,
                Cards: testDeck.Cards,
                SeatOrder: 1,
                Stickers: new[] { 6, 5, 4 },
                CanCastEtali: true,
                MinimumCardsToCastEtali: 3,
                DecklistId: 4444,
                DecklistName: "Original Deck Name",
                LowestGambleOdds: 0.75f,
                GenerationAttempts: 1,
                CreatedByUserId: userId
            );
            db.ComparisonEvaluatedHands.Add(comparisonHand);

            await db.SaveChangesAsync();
        }

        // Act - Rename the deck
        var contributorClient = await _factory.CreateAuthenticatedClientAsync(useAdmin: false);
        var response = await contributorClient.PatchAsync("/decklist/4444/rename?newName=Updated Deck Name", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Decklist renamed successfully", result.GetProperty("message").GetString());
        
        // Verify the response includes update counts
        var updatedRecords = result.GetProperty("updatedRecords");
        Assert.Equal(1, updatedRecords.GetProperty("evaluatedHands").GetInt32());
        Assert.Equal(1, updatedRecords.GetProperty("comparisonEvaluatedHands").GetInt32());
        Assert.Equal(1, updatedRecords.GetProperty("comparisonsAsDeck1").GetInt32());
        Assert.Equal(1, updatedRecords.GetProperty("comparisonsAsDeck2").GetInt32());

        // Verify all data was updated in the database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EtaliDatabase>();

            // Check Decklist
            var updatedDeck = await db.Decklists.FindAsync(4444);
            Assert.NotNull(updatedDeck);
            Assert.Equal("Updated Deck Name", updatedDeck.Name);

            // Check EvaluatedHand
            var updatedEvaluatedHand = await db.EvaluatedHands.FindAsync(9001);
            Assert.NotNull(updatedEvaluatedHand);
            Assert.Equal("Updated Deck Name", updatedEvaluatedHand.DecklistName);

            // Check DeckComparison where deck is Deck1
            var updatedComparison1 = await db.DeckComparisons.FindAsync(7001);
            Assert.NotNull(updatedComparison1);
            Assert.Equal("Updated Deck Name", updatedComparison1.Deck1Name);
            Assert.Equal("Other Deck", updatedComparison1.Deck2Name); // Should not change

            // Check DeckComparison where deck is Deck2
            var updatedComparison2 = await db.DeckComparisons.FindAsync(7002);
            Assert.NotNull(updatedComparison2);
            Assert.Equal("Other Deck", updatedComparison2.Deck1Name); // Should not change
            Assert.Equal("Updated Deck Name", updatedComparison2.Deck2Name);

            // Check ComparisonEvaluatedHand
            var updatedComparisonHand = await db.ComparisonEvaluatedHands.FindAsync(9002);
            Assert.NotNull(updatedComparisonHand);
            Assert.Equal("Updated Deck Name", updatedComparisonHand.DecklistName);
        }
    }

    #endregion
}
