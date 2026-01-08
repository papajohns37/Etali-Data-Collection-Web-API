using System.Net;
using System.Net.Http.Json;
using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for GET /decklists endpoint.
/// </summary>
[Trait("Category", "Integration")]
public class DecklistsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public DecklistsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDecklists_ReturnsOkWithList()
    {
        // Arrange - Ensure database is seeded
        await _factory.CreateAuthenticatedClientAsync(); // This seeds the database
        
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
        // Arrange - Ensure database is seeded
        await _factory.CreateAuthenticatedClientAsync();
        
        // Act
        var response = await _client.GetAsync("/decklists");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetDecklists_ReturnsConsistentOrdering()
    {
        // Arrange - Ensure database is seeded
        await _factory.CreateAuthenticatedClientAsync();
        
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
}
